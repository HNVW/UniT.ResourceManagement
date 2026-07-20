#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public sealed class AddressablesAssetManager : IAssetManager, IRemoteAssetDownloader, IDisposable
    {
        #region Constructor

        private readonly string keyPrefix;
        private readonly ILogger logger;

        private readonly Dictionary<object, Object> cacheSingle = new();
        private readonly Dictionary<object, IReadOnlyList<Object>> cacheMultiple = new();

        [Preserve]
        public AddressablesAssetManager(ILoggerManager loggerManager, string? scope = null)
        {
            this.keyPrefix = scope.IsNullOrWhiteSpace() ? string.Empty : $"{scope}/";
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Download

        UniTask<long> IRemoteAssetDownloader.GetDownloadSizeAsync(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Addressables.GetDownloadSizeAsync(this.GetScopedKey(key)).ToUniTask(progress, cancellationToken);
        }

        async UniTask<long> IRemoteAssetDownloader.GetAllDownloadSizeAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var keys = await this.GetAllKeysAsync(cancellationToken);
            return await Addressables.GetDownloadSizeAsync(keys).ToUniTask(progress, cancellationToken);

        }

        UniTask IRemoteAssetDownloader.DownloadAsync(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Addressables.DownloadDependenciesAsync(this.GetScopedKey(key), autoReleaseHandle: true).ToUniTask(progress, cancellationToken);
        }

        async UniTask IRemoteAssetDownloader.DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var keys = await this.GetAllKeysAsync(cancellationToken);
            await Addressables.DownloadDependenciesAsync(keys, autoReleaseHandle: true).ToUniTask(progress, cancellationToken);
        }

        private async UniTask<IEnumerable<object>> GetAllKeysAsync(CancellationToken cancellationToken)
        {
            await Addressables.InitializeAsync(autoReleaseHandle: true).ToUniTask(cancellationToken: cancellationToken);
            return Addressables.ResourceLocators.SelectMany(static locator => locator.Keys);
        }

        #endregion

        #region Load

        async UniTask<bool> IAssetManager.ContainsAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (this.cacheSingle.ContainsKey(key)) return true;
            var locations = await this.LoadResourceLocationsAsync<T>(key, cancellationToken);
            if (locations.Count > 1) throw new InvalidOperationException($"Multiple assets found for {key} in Addressables");
            return locations.Count > 0;
        }

        async UniTask<bool> IAssetManager.ContainsAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (this.cacheMultiple.ContainsKey(key)) return true;
            var locations = await this.LoadResourceLocationsAsync<T>(key, cancellationToken);
            return locations.Count > 0;
        }

        async UniTask<T> IAssetManager.LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, static async state =>
            {
                var (@this, key, progress, cancellationToken) = state;
                var locations = await @this.LoadResourceLocationsAsync<T>(key, cancellationToken);
                if (locations.Count is 0) throw new KeyNotFoundException($"{key} not found in Addressables");
                if (locations.Count > 1) throw new InvalidOperationException($"Multiple assets found for {key} in Addressables");
                var asset = await Addressables.LoadAssetAsync<T>(locations[0]).ToUniTask(progress, cancellationToken);
                @this.logger.Debug($"Loaded {key}");
                return (Object)asset;
            }, (@this: this, key, progress, cancellationToken));
        }

        async UniTask<IReadOnlyList<T>> IAssetManager.LoadAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (IReadOnlyList<T>)await this.cacheMultiple.GetOrAddAsync(key, static async state =>
            {
                var (@this, key, progress, cancellationToken) = state;
                var locations = await @this.LoadResourceLocationsAsync<T>(key, cancellationToken);
                var assets = await Addressables.LoadAssetsAsync<T>(locations, null).ToUniTask(progress, cancellationToken);
                @this.logger.Debug($"Loaded {assets.Count} assets for {key}");
                return (IReadOnlyList<Object>)assets;
            }, (@this: this, key, progress, cancellationToken));
        }

        private async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync<T>(object key, CancellationToken cancellationToken) where T : Object
        {
            var handle = Addressables.LoadResourceLocationsAsync(this.GetScopedKey(key), typeof(T));
            var result = await handle.ToUniTask(cancellationToken: cancellationToken);
            handle.Release();
            return result;
        }

        private object GetScopedKey(object key) => key is string ? $"{this.keyPrefix}{key}" : key;

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
            Unload(assets);
            this.logger.Debug($"Unloaded {assets.Count} assets for {key}");
        }

        void IDisposable.Dispose()
        {
            this.cacheSingle.Clear(Unload);
            this.cacheMultiple.Clear(Unload);
            this.logger.Debug("Disposed");
        }

        private static void Unload(object asset)
        {
#if UNITY_EDITOR
            if (IgnoreUnload) return;
#endif
            Addressables.Release(asset);
        }

#if UNITY_EDITOR
        private static bool IgnoreUnload;

        static AddressablesAssetManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            IgnoreUnload = stateChange is PlayModeStateChange.EnteredEditMode or PlayModeStateChange.ExitingPlayMode;
        }
#endif

        #endregion
    }
}