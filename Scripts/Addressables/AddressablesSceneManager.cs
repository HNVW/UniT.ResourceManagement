#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;
    using UnityEngine.Scripting;

    public sealed class AddressablesSceneManager : ISceneManager
    {
        #region Constructor

        private readonly ILogger logger;

        private readonly Dictionary<string, SceneInstance> loadedScenes = new();

        [Preserve]
        public AddressablesSceneManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        async UniTask ISceneManager.LoadAsync(string name, LoadSceneMode mode, bool activateOnLoad, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (mode is LoadSceneMode.Single) this.loadedScenes.Clear();
            await this.loadedScenes.TryAddAsync(
                name,
                static async state =>
                {
                    var (name, mode, activateOnLoad, progress, cancellationToken) = state;
                    try
                    {
                        return await Addressables.LoadSceneAsync(name, mode, activateOnLoad).ToUniTask(progress, cancellationToken);
                    }
                    catch (InvalidKeyException)
                    {
                        throw new KeyNotFoundException($"{name} not found in Addressables");
                    }
                },
                (name, mode, activateOnLoad, progress, cancellationToken)
            );
            this.logger.Debug($"Loaded {name}, mode: {mode}, activateOnLoad: {activateOnLoad}");
        }

        async UniTask ISceneManager.ActivateAsync(string name, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!this.loadedScenes.TryGetValue(name, out var sceneInstance))
            {
                throw new InvalidOperationException($"Scene {name} not loaded");
            }
            await sceneInstance.ActivateAsync().ToUniTask(progress: progress, cancellationToken: cancellationToken);
            this.logger.Debug($"Activated {name}");
        }

        async UniTask ISceneManager.UnloadAsync(string name, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!this.loadedScenes.Remove(name, out var sceneInstance))
            {
                this.logger.Warning($"{name} not loaded");
                return;
            }
            await Addressables.UnloadSceneAsync(sceneInstance).ToUniTask(progress, cancellationToken);
            this.logger.Debug($"Unloaded {name}");
        }
    }
}