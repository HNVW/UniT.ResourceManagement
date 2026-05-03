#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = UniT.Logging.ILogger;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif
    #if !UNITY_WEBGL
    using System.Linq;
    #endif

    public sealed class ResourceAssetsManager : IAssetsManager
    {
        #region Constructor

        private readonly string  keyPrefix;
        private readonly ILogger logger;

        private readonly Dictionary<object, Object>                      cacheSingle   = new();
        private readonly Dictionary<object, IReadOnlyCollection<Object>> cacheMultiple = new();

        [Preserve]
        public ResourceAssetsManager(ILoggerManager loggerManager, string? scope = null)
        {
            this.keyPrefix = scope.IsNullOrWhiteSpace() ? string.Empty : $"{scope}/";
            this.logger    = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        private string GetScopedKey(object key) => key is string
            ? $"{this.keyPrefix}{key}"
            : throw new NotSupportedException("Resources only supports loading assets from string paths");

        #region Sync

        #if !UNITY_WEBGL
        T IAssetsManager.Load<T>(object key)
        {
            return (T)this.cacheSingle.GetOrAdd(key, static state =>
            {
                var asset = Resources.Load<T>(state.@this.GetScopedKey(state.key))
                    ?? throw new ArgumentOutOfRangeException($"{state.key} not found in resources");
                state.@this.logger.Debug($"Loaded {state.key}");
                return asset;
            }, (@this: this, key));
        }

        IEnumerable<T> IAssetsManager.LoadAll<T>(object key) => this.LoadAll<T>(key);

        private IEnumerable<T> LoadAll<T>(object key) where T : Object
        {
            return this.cacheMultiple.GetOrAdd(key, static state =>
            {
                var assets = Resources.LoadAll<T>(state.@this.GetScopedKey(state.key));
                state.@this.logger.Debug($"Loaded {state.key}");
                return assets;
            }, (@this: this, key)).Cast<T>();
        }
        #endif

        #endregion

        #region Async

        #if UNIT_UNITASK
        async UniTask<T> IAssetsManager.LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, static async state =>
            {
                var asset = await Resources.LoadAsync<T>(state.@this.GetScopedKey(state.key)).ToUniTask(progress: state.progress, cancellationToken: state.cancellationToken)
                    ?? throw new ArgumentOutOfRangeException($"{state.key} not found in resources");
                state.@this.logger.Debug($"Loaded {state.key}");
                return asset;
            }, (@this: this, key, progress, cancellationToken));
        }

        UniTask<IEnumerable<T>> IAssetsManager.LoadAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            #if !UNITY_WEBGL
            this.logger.Warning("Unity does not support loading all from resources asynchronously");
            return UniTask.FromResult(this.LoadAll<T>(key));
            #else
            throw new NotSupportedException("Unity does not support loading all from resources asynchronously");
            #endif
        }
        #else
        IEnumerator IAssetsManager.LoadAsync<T>(object key, Action<T> callback, IProgress<float>? progress)
        {
            return this.cacheSingle.GetOrAddAsync(
                key,
                callback =>
                {
                    var operation = Resources.LoadAsync<T>(this.GetScopedKey(key));
                    return operation.ToCoroutine(
                        () => callback(operation.asset ?? throw new ArgumentOutOfRangeException($"{key} not found in resources")),
                        progress
                    );
                },
                asset => callback((T)asset)
            );
        }

        IEnumerator IAssetsManager.LoadAllAsync<T>(object key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            this.logger.Warning("Unity does not support loading all from resources asynchronously");
            callback(this.LoadAll<T>(key));
            yield break;
        }
        #endif

        #endregion

        #region Finalizer

        void IAssetsManager.Unload(object key)
        {
            if (!this.cacheSingle.Remove(key, out var asset))
            {
                this.logger.Warning($"Trying to unload {key} that was not loaded");
                return;
            }
            Resources.UnloadAsset(asset);
            this.logger.Debug($"Unloaded {key}");
        }

        void IAssetsManager.UnloadAll(object key)
        {
            if (!this.cacheMultiple.Remove(key, out var assets))
            {
                this.logger.Warning($"Trying to unload all {key} that was not loaded");
                return;
            }
            assets.ForEach(Resources.UnloadAsset);
            this.logger.Debug($"Unloaded {key}");
        }

        private void Dispose()
        {
            this.cacheSingle.Clear(Resources.UnloadAsset);
            this.cacheMultiple.Clear(static assets => assets.ForEach(Resources.UnloadAsset));
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
            this.logger.Debug("Disposed");
        }

        ~ResourceAssetsManager()
        {
            this.Dispose();
            this.logger.Debug("Finalized");
        }

        #endregion
    }
}