using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cysharp.Threading.Tasks;

using SackranyConfig;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SackranyScenes
{
    public static class SceneLoader
    {
        readonly struct Request
        {
            public readonly string SceneName;
            public readonly string Transition;
            public readonly bool Reload;
            public readonly UniTaskCompletionSource Tcs;

            public Request(string sceneName, string transition, bool reload, UniTaskCompletionSource tcs)
            {
                SceneName = sceneName;
                Transition = transition;
                Reload = reload;
                Tcs = tcs;
            }
        }

        static readonly Dictionary<string, SceneData> _scenes = new();
        static readonly Queue<Request> _queue = new();

        static string _currentScene;
        static string _currentTransition;
        static bool _processing;

        static SceneTransitionLibrary _library;
        static bool _libraryResolved;

        public static string CurrentScene => _currentScene;
        public static string CurrentTransition => _currentTransition;
        public static IReadOnlyDictionary<string, SceneData> Scenes => _scenes;

        /// <summary>True while the queue is draining (one or more transitions pending/in-flight).</summary>
        public static bool IsTransitioning => _processing;

        static SceneTransitionLibrary Library
        {
            get
            {
                if (!_libraryResolved)
                {
                    _library = Resources.Load<SceneTransitionLibrary>(SceneTransitionLibrary.ResourcePath);
                    _libraryResolved = true;
                }

                return _library;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            _scenes.Clear();
            _queue.Clear();
            _currentScene = null;
            _currentTransition = null;
            _processing = false;
            _library = null;
            _libraryResolved = false;

            if (SceneManager.sceneCountInBuildSettings == 0) return;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = Path.GetFileNameWithoutExtension(path);

                if (_scenes.ContainsKey(name))
                {
                    Debug.LogWarning($"[Scenes] Duplicate scene name '{name}' in Build Settings; it cannot be addressed unambiguously by name and was skipped.");
                    continue;
                }

                _scenes[name] = new SceneData(name);
            }

            _currentScene = _scenes.Values.FirstOrDefault(x => x.IsLoaded)?.SceneName;

            var lib = Library;
            if (lib != null)
                _currentTransition = lib.DefaultTransition;

#if !UNITY_EDITOR
            var defaultScene = ConfigGet<SceneConfig>.Value.DefaultScene;
            if (!string.IsNullOrEmpty(defaultScene))
                Enqueue(defaultScene, null, false).Forget();
#endif
        }

        // ---- Public API ---------------------------------------------------

        /// <summary>Sets the transition used when none is passed explicitly. Pass null for "no transition".</summary>
        public static void SetTransition(string transition) => _currentTransition = transition;

        public static void Load(string sceneName) => Enqueue(sceneName, _currentTransition, false).Forget();
        public static void Load(string sceneName, string transition) => Enqueue(sceneName, transition, false).Forget();

        public static UniTask LoadAsync(string sceneName) => Enqueue(sceneName, _currentTransition, false);
        public static UniTask LoadAsync(string sceneName, string transition) => Enqueue(sceneName, transition, false);

        public static void Reload() => Enqueue(null, _currentTransition, true).Forget();
        public static void Reload(string transition) => Enqueue(null, transition, true).Forget();

        public static UniTask ReloadAsync() => Enqueue(null, _currentTransition, true);
        public static UniTask ReloadAsync(string transition) => Enqueue(null, transition, true);

        // ---- Queue --------------------------------------------------------

        static UniTask Enqueue(string sceneName, string transition, bool reload)
        {
            var tcs = new UniTaskCompletionSource();
            _queue.Enqueue(new Request(sceneName, transition, reload, tcs));

            if (!_processing)
                Process().Forget();

            return tcs.Task;
        }

        static async UniTaskVoid Process()
        {
            _processing = true;
            try
            {
                while (_queue.Count > 0)
                {
                    var req = _queue.Dequeue();
                    try
                    {
                        await RunTransition(req.SceneName, req.Transition, req.Reload);
                        req.Tcs.TrySetResult();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Scenes] Transition to '{req.SceneName ?? _currentScene}' failed: {e}");
                        req.Tcs.TrySetException(e);
                    }
                }
            }
            finally
            {
                _processing = false;
            }
        }

        // ---- Core ---------------------------------------------------------

        static async UniTask RunTransition(string sceneName, string transitionName, bool reload)
        {
            if (reload)
            {
                if (_currentScene == null || !_scenes.ContainsKey(_currentScene))
                {
                    Debug.LogWarning("[Scenes] Reload requested but there is no current scene.");
                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(sceneName)) return;
                if (!_scenes.ContainsKey(sceneName))
                {
                    Debug.LogWarning($"[Scenes] Scene '{sceneName}' is not registered in Build Settings; load skipped.");
                    return;
                }
                if (sceneName == _currentScene) return;
            }

            var prefab = ResolveTransition(transitionName);
            SceneTransition instance = null;
            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);

                try
                {
                    await instance.PlayOut();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Scenes] Transition '{transitionName}' PlayOut threw: {e}");
                }
            }

            await Swap(sceneName, reload);

            if (instance != null)
            {
                try
                {
                    await instance.PlayIn();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Scenes] Transition '{transitionName}' PlayIn threw: {e}");
                }

                UnityEngine.Object.Destroy(instance.gameObject);
            }
        }

        static async UniTask Swap(string sceneName, bool reload)
        {
            if (reload)
            {
                await _scenes[_currentScene].Reload();
                return;
            }

            SceneData current = _currentScene != null && _scenes.TryGetValue(_currentScene, out var c)
                ? c
                : null;
            var next = _scenes[sceneName];

            // Load next before unloading current to never have zero loaded scenes.
            await next.Load();

            if (current != null && current != next)
                await current.Unload();

            _currentScene = sceneName;
        }

        static SceneTransition ResolveTransition(string transitionName)
        {
            var lib = Library;
            if (lib == null) return null;       // No transitions in the project at all -> null, no error.
            return lib.Get(transitionName);     // Unknown / null name -> null.
        }
    }
}
