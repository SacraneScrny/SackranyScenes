using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cysharp.Threading.Tasks;

using SackranyConfig;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sackrany.Scenes.SackranyScenes
{
    /// <summary>
    /// Чистый аддитивный загрузчик сцен. Никаких «системных» сцен и внешних
    /// сериализаторов — единственная зависимость это ConfigSystem (для DefaultScene).
    ///
    /// Все сцены из Build Settings регистрируются на старте. В любой момент времени
    /// загружена одна «текущая» сцена; <see cref="Load"/> аддитивно подгружает новую
    /// и выгружает предыдущую.
    /// </summary>
    public static class SceneLoader
    {
        static readonly Dictionary<string, SceneData> _scenes = new();
        static string _currentScene;
        static bool _isTransitioning;

        public static string CurrentScene => _currentScene;
        public static IReadOnlyDictionary<string, SceneData> Scenes => _scenes;
        public static bool IsTransitioning => _isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            _scenes.Clear();
            _currentScene = null;

            if (SceneManager.sceneCountInBuildSettings == 0) return;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = Path.GetFileNameWithoutExtension(path);
                _scenes[name] = new SceneData(name);
            }

            _currentScene = _scenes.Values.FirstOrDefault(x => x.IsLoaded)?.SceneName;

            LoadDefaultScene().Forget();
        }

        static async UniTask LoadDefaultScene()
        {
#if !UNITY_EDITOR
            var defaultScene = ConfigGet<SceneConfig>.Value.DefaultScene;
            if (string.IsNullOrEmpty(defaultScene)) return;
            if (!_scenes.TryGetValue(defaultScene, out var sceneData)) return;
            if (sceneData.IsLoaded) return;

            await Transition(defaultScene);
#else
            await UniTask.CompletedTask;
#endif
        }

        public static void Load(string sceneName) => Transition(sceneName).Forget();

        public static UniTask LoadAsync(string sceneName) => Transition(sceneName);

        static async UniTask Transition(string sceneName)
        {
            if (_isTransitioning) return;
            if (string.IsNullOrEmpty(sceneName)) return;
            if (!_scenes.TryGetValue(sceneName, out var next)) return;
            if (sceneName == _currentScene) return;

            _isTransitioning = true;
            try
            {
                SceneData current = _currentScene != null && _scenes.TryGetValue(_currentScene, out var c)
                    ? c
                    : null;

                if (current != null)
                    await current.WaitForLoad();

                // Грузим новую сцену до выгрузки старой, чтобы никогда не оставаться
                // без единственной загруженной сцены.
                await next.Load();

                if (current != null && current != next)
                    await current.Unload();

                _currentScene = sceneName;
            }
            finally
            {
                _isTransitioning = false;
            }
        }
    }
}
