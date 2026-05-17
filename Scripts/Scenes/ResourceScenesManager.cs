#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using UniT.Logging;
    using UnityEngine.SceneManagement;
    using UnityEngine.Scripting;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    using UniT.Extensions;
    #endif

    public sealed class ResourceScenesManager : IScenesManager
    {
        #region Constructor

        private readonly ILogger logger;

        [Preserve]
        public ResourceScenesManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        void IScenesManager.Load(string name, LoadSceneMode mode)
        {
            SceneManager.LoadScene(name, mode);
            this.logger.Debug($"Loaded {name}");
        }

        #if UNIT_UNITASK
        UniTask IScenesManager.LoadAsync(string name, LoadSceneMode mode, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return SceneManager.LoadSceneAsync(name, mode)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(() => this.logger.Debug($"Loaded {name}"));
        }

        UniTask IScenesManager.UnloadAsync(string name, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return SceneManager.UnloadSceneAsync(name)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(() => this.logger.Debug($"Unloaded {name}"));
        }
        #else
        IEnumerator IScenesManager.LoadAsync(string name, LoadSceneMode mode, Action? callback, IProgress<float>? progress)
        {
            return SceneManager.LoadSceneAsync(name, mode)!.ToCoroutine(() =>
            {
                this.logger.Debug($"Loaded {name}");
                callback?.Invoke();
            }, progress: progress);
        }

        IEnumerator IScenesManager.UnloadAsync(string name, Action? callback, IProgress<float>? progress)
        {
            return SceneManager.UnloadSceneAsync(name)!.ToCoroutine(() =>
            {
                this.logger.Debug($"Unloaded {name}");
                callback?.Invoke();
            }, progress: progress);
        }
        #endif
    }
}