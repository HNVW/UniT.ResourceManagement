#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetScopedKey(object key) => key is string
            ? $"{this.keyPrefix}{key}"
            : throw new NotSupportedException("Resources only supports loading assets from string paths");

        #region Sync

        #if !UNITY_WEBGL
        bool IAssetsManager.Contains<T>(object key)
        {
            if (this.cacheSingle.ContainsKey(key) || this.cacheMultiple.ContainsKey(key)) return true;
            this.logger.Warning("Resources does not support checking key exists. Use `LoadAsync` or `LoadAllAsync` directly.");
            var asset = Resources.Load<T>(this.GetScopedKey(key));
            if (!asset) return false;
            Unload(asset);
            return true;
        }

        T IAssetsManager.Load<T>(object key)
        {
            return (T)this.cacheSingle.GetOrAdd(key, static state =>
            {
                var asset = Resources.Load<T>(state.@this.GetScopedKey(state.key));
                if (!asset) throw new ArgumentOutOfRangeException(nameof(state.key), state.key, $"{state.key} not found in resources");
                state.@this.logger.Debug($"Loaded {state.key}");
                return asset;
            }, (@this: this, key));
        }

        IEnumerable<T> IAssetsManager.LoadAll<T>(object key) => this.LoadAll<T>(key);
        #endif

        private IEnumerable<T> LoadAll<T>(object key) where T : Object
        {
            return this.cacheMultiple.GetOrAdd(key, static state =>
            {
                var assets = Resources.LoadAll<T>(state.@this.GetScopedKey(state.key));
                state.@this.logger.Debug($"Loaded {state.key}");
                return assets;
            }, (@this: this, key)).Cast<T>();
        }

        #endregion

        #region Async

        #if UNIT_UNITASK
        async UniTask<bool> IAssetsManager.ContainsAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (this.cacheSingle.ContainsKey(key) || this.cacheMultiple.ContainsKey(key)) return true;
            this.logger.Warning("Resources does not support checking key exists. Use `LoadAsync` or `LoadAllAsync` directly.");
            var asset = await Resources.LoadAsync<T>(this.GetScopedKey(key)).ToUniTask(progress: progress, cancellationToken: cancellationToken);
            if (!asset) return false;
            Unload(asset);
            return true;
        }

        async UniTask<T> IAssetsManager.LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, static async state =>
            {
                var asset = await Resources.LoadAsync<T>(state.@this.GetScopedKey(state.key)).ToUniTask(progress: state.progress, cancellationToken: state.cancellationToken);
                if (!asset) throw new ArgumentOutOfRangeException(nameof(state.key), state.key, $"{state.key} not found in resources");
                state.@this.logger.Debug($"Loaded {state.key}");
                return asset;
            }, (@this: this, key, progress, cancellationToken));
        }

        UniTask<IEnumerable<T>> IAssetsManager.LoadAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            this.logger.Warning("Resources does not support loading all asynchronously");
            return UniTask.FromResult(this.LoadAll<T>(key));
        }
        #else
        IEnumerator IAssetsManager.ContainsAsync<T>(object key, Action<bool> callback, IProgress<float>? progress)
        {
            if (this.cacheSingle.ContainsKey(key) || this.cacheMultiple.ContainsKey(key))
            {
                callback(true);
                yield break;
            }
            this.logger.Warning("Resources does not support checking key exists. Use `LoadAsync` or `LoadAllAsync` directly.");
            yield return Resources.LoadAsync<T>(this.GetScopedKey(key)).ToCoroutine(operation =>
            {
                if (!operation.asset)
                {
                    callback(false);
                    return;
                }
                Unload(operation.asset);
                callback(true);
            }, progress);
        }

        IEnumerator IAssetsManager.LoadAsync<T>(object key, Action<T> callback, IProgress<float>? progress)
        {
            return this.cacheSingle.GetOrAddAsync(
                key,
                callback => Resources.LoadAsync<T>(this.GetScopedKey(key)).ToCoroutine(operation =>
                {
                    if (!operation.asset) throw new ArgumentOutOfRangeException(nameof(key), key, $"{key} not found in resources");
                    this.logger.Debug($"Loaded {key}");
                    callback(operation.asset);
                }, progress),
                asset => callback((T)asset)
            );
        }

        IEnumerator IAssetsManager.LoadAllAsync<T>(object key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            this.logger.Warning("Resources does not support loading all asynchronously");
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
            Unload(asset);
            this.logger.Debug($"Unloaded {key}");
        }

        void IAssetsManager.UnloadAll(object key)
        {
            if (!this.cacheMultiple.Remove(key, out var assets))
            {
                this.logger.Warning($"Trying to unload all {key} that was not loaded");
                return;
            }
            assets.ForEach(Unload);
            this.logger.Debug($"Unloaded {key}");
        }

        private void Dispose()
        {
            this.cacheSingle.Clear(Unload);
            this.cacheMultiple.Clear(static assets => assets.ForEach(Unload));
            Resources.UnloadUnusedAssets();
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

        private static void Unload(Object asset)
        {
            if (asset is GameObject) return;
            Resources.UnloadAsset(asset);
        }

        #endregion
    }
}