#if UNIT_ADDRESSABLES
#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;
    using UnityEngine.Scripting;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class AddressableScenesManager : IScenesManager
    {
        #region Constructor

        private readonly ILogger logger;

        private readonly Dictionary<string, SceneInstance> loadedScenes = new();

        [Preserve]
        public AddressableScenesManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        void IScenesManager.Load(string name, LoadSceneMode mode)
        {
            var instance = Addressables.LoadSceneAsync(name, mode).WaitForResultOrThrow();
            this.OnSceneLoaded(name, mode, instance);
        }

        #if UNIT_UNITASK
        UniTask IScenesManager.LoadAsync(string name, LoadSceneMode mode, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Addressables.LoadSceneAsync(name, mode)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(scene => this.OnSceneLoaded(name, mode, scene));
        }

        UniTask IScenesManager.UnloadAsync(string name, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!this.loadedScenes.Remove(name, out var scene))
            {
                this.logger.Warning($"{name} not loaded");
                return UniTask.CompletedTask;
            }
            return Addressables.UnloadSceneAsync(scene)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(_ => this.logger.Debug($"Unloaded {name}"));
        }
        #else
        IEnumerator IScenesManager.LoadAsync(string name, LoadSceneMode mode, Action? callback, IProgress<float>? progress)
        {
            return Addressables.LoadSceneAsync(name, mode).ToCoroutine(scene =>
            {
                this.OnSceneLoaded(name, mode, scene);
                callback?.Invoke();
            }, progress);
        }

        IEnumerator IScenesManager.UnloadAsync(string name, Action? callback, IProgress<float>? progress)
        {
            if (!this.loadedScenes.Remove(name, out var scene))
            {
                this.logger.Warning($"{name} not loaded");
                callback?.Invoke();
                yield break;
            }
            yield return Addressables.UnloadSceneAsync(scene).ToCoroutine(_ =>
            {
                this.logger.Debug($"Unloaded {name}");
                callback?.Invoke();
            }, progress);
        }
        #endif

        private void OnSceneLoaded(string name, LoadSceneMode mode, SceneInstance scene)
        {
            if (mode is LoadSceneMode.Single)
            {
                this.loadedScenes.Clear();
            }
            this.loadedScenes.Add(name, scene);
            this.logger.Debug($"Loaded {name}");
        }
    }
}
#endif