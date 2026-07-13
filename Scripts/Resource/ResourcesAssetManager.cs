#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Logging;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = Logging.ILogger;
    using Object = UnityEngine.Object;

    public sealed class ResourcesAssetManager : IAssetManager, IDisposable
    {
        #region Constructor

        private readonly string keyPrefix;
        private readonly ILogger logger;

        private readonly Dictionary<object, Object> cacheSingle = new();
        private readonly Dictionary<object, IReadOnlyCollection<Object>> cacheMultiple = new();

        [Preserve]
        public ResourcesAssetManager(ILoggerManager loggerManager, string? scope = null)
        {
            this.keyPrefix = scope.IsNullOrWhiteSpace() ? string.Empty : $"{scope}/";
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Load

        async UniTask<bool> IAssetManager.ContainsAsync(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (this.cacheSingle.ContainsKey(key) || this.cacheMultiple.ContainsKey(key)) return true;
            this.logger.Warning("Resources does not support checking key exists. Use `LoadAsync` or `LoadAllAsync` directly.");
            var asset = await Resources.LoadAsync<Object>(this.GetScopedKey(key)).ToUniTask(progress: progress, cancellationToken: cancellationToken);
            if (!asset) return false;
            Unload(asset);
            return true;
        }

        async UniTask<T> IAssetManager.LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, static async state =>
            {
                var (@this, key, progress, cancellationToken) = state;
                var asset = await Resources.LoadAsync<T>(@this.GetScopedKey(key)).ToUniTask(progress: progress, cancellationToken: cancellationToken);
                if (!asset) throw new KeyNotFoundException($"{key} not found in Resources");
                @this.logger.Debug($"Loaded {key}");
                return asset;
            }, (@this: this, key, progress, cancellationToken));
        }

        UniTask<IReadOnlyCollection<T>> IAssetManager.LoadAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            this.logger.Warning("Resources does not support loading all asynchronously");
            return UniTask.FromResult((IReadOnlyCollection<T>)this.cacheMultiple.GetOrAdd(key, static state =>
            {
                var (@this, key) = state;
                var assets = Resources.LoadAll<T>(@this.GetScopedKey(key));
                @this.logger.Debug($"Loaded {assets.Length} assets for {key}");
                return assets;
            }, (@this: this, key)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetScopedKey(object key) => key is string
            ? $"{this.keyPrefix}{key}"
            : throw new NotSupportedException("Resources only supports loading assets from string paths");

        #endregion

        #region Unload

        void IAssetManager.Unload(object key)
        {
            if (!this.cacheSingle.Remove(key, out var asset))
            {
                this.logger.Warning($"Trying to unload {key} that was not loaded");
                return;
            }
            Unload(asset);
            this.logger.Debug($"Unloaded {key}");
        }

        void IAssetManager.UnloadAll(object key)
        {
            if (!this.cacheMultiple.Remove(key, out var assets))
            {
                this.logger.Warning($"Trying to unload all {key} that was not loaded");
                return;
            }
            assets.ForEach(Unload);
            this.logger.Debug($"Unloaded {assets.Count} assets for {key}");
        }

        void IDisposable.Dispose()
        {
            this.cacheSingle.Clear(Unload);
            this.cacheMultiple.Clear(static assets => assets.ForEach(Unload));
            Resources.UnloadUnusedAssets();
            this.logger.Debug("Disposed");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Unload(Object asset)
        {
            if (asset is GameObject) return;
            Resources.UnloadAsset(asset);
        }

        #endregion
    }
}