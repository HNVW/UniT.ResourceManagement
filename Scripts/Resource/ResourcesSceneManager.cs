#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Logging;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.Scripting;
    using ILogger = Logging.ILogger;

    public sealed class ResourcesSceneManager : ISceneManager
    {
        #region Constructor

        private readonly ILogger logger;

        private readonly Dictionary<string, AsyncOperation> loadedScenes = new();

        [Preserve]
        public ResourcesSceneManager(ILoggerManager loggerManager)
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
                    var asyncOperation = SceneManager.LoadSceneAsync(name, mode)
                        ?? throw new KeyNotFoundException($"{name} not found in Resources");
                    if (activateOnLoad)
                    {
                        await asyncOperation.ToUniTask(progress: progress, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        asyncOperation.allowSceneActivation = false;
                        while (asyncOperation.progress < .9f)
                        {
                            await UniTask.Yield(cancellationToken);
                            progress?.Report(asyncOperation.progress * 10 / 9);
                        }
                    }
                    return asyncOperation;
                },
                (name, mode, activateOnLoad, progress, cancellationToken)
            );
            this.logger.Debug($"Loaded {name}, mode: {mode}, activateOnLoad: {activateOnLoad}");
        }

        async UniTask ISceneManager.ActivateAsync(string name, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!this.loadedScenes.Remove(name, out var asyncOperation))
            {
                throw new InvalidOperationException($"{name} not loaded");
            }
            asyncOperation.allowSceneActivation = true;
            await asyncOperation.ToUniTask(progress: progress, cancellationToken: cancellationToken);
            this.logger.Debug($"Activated {name}");
        }

        async UniTask ISceneManager.UnloadAsync(string name, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!this.loadedScenes.Remove(name))
            {
                this.logger.Warning($"{name} not loaded");
                return;
            }
            await SceneManager.UnloadSceneAsync(name).ToUniTask(progress: progress, cancellationToken: cancellationToken);
            this.logger.Debug($"Unloaded {name}");
        }
    }
}