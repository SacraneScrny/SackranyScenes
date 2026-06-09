using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine.SceneManagement;

namespace SackranyScenes
{
    public class SceneData
    {
        public readonly string SceneName;
        public Scene Scene { get; private set; }
        public bool IsLoaded { get; private set; }
        
        UniTaskCompletionSource _loadingTcs;
        
        public SceneData(string sceneName)
        {
            SceneName = sceneName;
            IsLoaded = SceneManager.GetSceneByName(SceneName).isLoaded;
        }

        public async UniTask Load(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (IsLoaded) return;

            if (_loadingTcs != null)
            {
                await _loadingTcs.Task;
                return;
            }
            
            _loadingTcs = new UniTaskCompletionSource();
            try
            {
                await SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive)
                    .ToUniTask(
                        progress: progress,
                        cancellationToken: cancellationToken
                    );
                IsLoaded = true;
                Scene loadedScene = SceneManager.GetSceneByName(SceneName);
                SceneManager.SetActiveScene(loadedScene);
                _loadingTcs.TrySetResult();
            }
            catch (OperationCanceledException e)
            {
                _loadingTcs.TrySetCanceled(e.CancellationToken);
            }
            finally
            {
                _loadingTcs = null;
                Scene = SceneManager.GetSceneByName(SceneName);
            }
        }
        public async UniTask Unload(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (!IsLoaded) return;
            
            if (_loadingTcs != null)
            {
                await _loadingTcs.Task;
                return;
            }
            
            _loadingTcs = new UniTaskCompletionSource();
            try
            {
                await SceneManager.UnloadSceneAsync(SceneName)
                    .ToUniTask(
                        progress: progress,
                        cancellationToken: cancellationToken
                    );
                IsLoaded = false;
                _loadingTcs.TrySetResult();
            }
            catch (OperationCanceledException e)
            {
                _loadingTcs.TrySetCanceled(e.CancellationToken);
            }
            finally
            {
                _loadingTcs = null;
            }
        }
        
        public UniTask WaitForLoad() => _loadingTcs?.Task ?? UniTask.CompletedTask;
    }
}