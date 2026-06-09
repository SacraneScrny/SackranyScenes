using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SackranyScenes
{
    public class SceneData
    {
        public readonly string SceneName;
        public Scene Scene { get; private set; }
        public bool IsLoaded { get; private set; }

        UniTaskCompletionSource _pendingTcs;

        public SceneData(string sceneName)
        {
            SceneName = sceneName;
            var scene = SceneManager.GetSceneByName(SceneName);
            IsLoaded = scene.isLoaded;
            if (IsLoaded)
                Scene = scene;
        }

        public async UniTask Load(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (IsLoaded) return;

            if (_pendingTcs != null)
            {
                await _pendingTcs.Task;
                return;
            }

            _pendingTcs = new UniTaskCompletionSource();
            try
            {
                await SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive)
                    .ToUniTask(progress: progress, cancellationToken: cancellationToken);

                var loaded = SceneManager.GetSceneByName(SceneName);
                if (loaded.IsValid())
                    SceneManager.SetActiveScene(loaded);
                else
                    Debug.LogError($"[Scenes] Loaded scene '{SceneName}' resolved to an invalid handle.");

                Scene = loaded;
                IsLoaded = loaded.isLoaded;
                _pendingTcs.TrySetResult();
            }
            catch (OperationCanceledException e)
            {
                _pendingTcs.TrySetCanceled(e.CancellationToken);
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scenes] Failed to load scene '{SceneName}': {e}");
                _pendingTcs.TrySetException(e);
                throw;
            }
            finally
            {
                _pendingTcs = null;
            }
        }

        public async UniTask Unload(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (!IsLoaded) return;

            if (_pendingTcs != null)
            {
                await _pendingTcs.Task;
                return;
            }

            _pendingTcs = new UniTaskCompletionSource();
            try
            {
                await SceneManager.UnloadSceneAsync(SceneName)
                    .ToUniTask(progress: progress, cancellationToken: cancellationToken);

                IsLoaded = false;
                Scene = default;
                _pendingTcs.TrySetResult();
            }
            catch (OperationCanceledException e)
            {
                _pendingTcs.TrySetCanceled(e.CancellationToken);
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scenes] Failed to unload scene '{SceneName}': {e}");
                _pendingTcs.TrySetException(e);
                throw;
            }
            finally
            {
                _pendingTcs = null;
            }
        }

        // Reload by loading a fresh additive instance first, then unloading the old
        // handle. This keeps the scene continuously present (never zero loaded scenes)
        // and avoids Unity refusing to unload the last remaining scene.
        public async UniTask Reload(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (_pendingTcs != null)
                await _pendingTcs.Task;

            _pendingTcs = new UniTaskCompletionSource();
            var old = Scene;
            try
            {
                await SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive)
                    .ToUniTask(progress: progress, cancellationToken: cancellationToken);

                var fresh = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                Scene = fresh;
                IsLoaded = fresh.isLoaded;
                if (fresh.IsValid())
                    SceneManager.SetActiveScene(fresh);

                if (old.IsValid() && old.isLoaded && old != fresh)
                    await SceneManager.UnloadSceneAsync(old)
                        .ToUniTask(cancellationToken: cancellationToken);

                _pendingTcs.TrySetResult();
            }
            catch (OperationCanceledException e)
            {
                _pendingTcs.TrySetCanceled(e.CancellationToken);
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scenes] Failed to reload scene '{SceneName}': {e}");
                _pendingTcs.TrySetException(e);
                throw;
            }
            finally
            {
                _pendingTcs = null;
            }
        }

        public UniTask WaitForPending() => _pendingTcs?.Task ?? UniTask.CompletedTask;
    }
}
