#nullable enable
namespace UniT.ResourceManagement.Addressables
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine.AddressableAssets;
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
        private readonly Dictionary<object, IReadOnlyCollection<Object>> cacheMultiple = new();

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
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            var keys = await this.GetAllKeysAsync(subProgresses[0], cancellationToken);
            return await Addressables.GetDownloadSizeAsync(keys).ToUniTask(subProgresses[1], cancellationToken);

        }

        UniTask IRemoteAssetDownloader.DownloadAsync(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Addressables.DownloadDependenciesAsync(this.GetScopedKey(key), autoReleaseHandle: true).ToUniTask(progress, cancellationToken);
        }

        async UniTask IRemoteAssetDownloader.DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            var keys = await this.GetAllKeysAsync(subProgresses[0], cancellationToken);
            await Addressables.DownloadDependenciesAsync(keys, autoReleaseHandle: true).ToUniTask(subProgresses[1], cancellationToken);
        }

        private async UniTask<IEnumerable<object>> GetAllKeysAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            await Addressables.InitializeAsync(autoReleaseHandle: true).ToUniTask(progress, cancellationToken);
            return Addressables.ResourceLocators.SelectMany(static locator => locator.Keys);
        }

        #endregion

        #region Load

        async UniTask<bool> IAssetManager.ContainsAsync(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (this.cacheSingle.ContainsKey(key) || this.cacheMultiple.ContainsKey(key)) return true;
            var handle = Addressables.LoadResourceLocationsAsync(this.GetScopedKey(key), typeof(Object));
            var resourceLocations = await handle.ToUniTask(progress, cancellationToken);
            var contains = resourceLocations.Count > 0;
            handle.Release();
            return contains;
        }

        async UniTask<T> IAssetManager.LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, static async state =>
            {
                var (@this, key, progress, cancellationToken) = state;
                try
                {
                    var asset = await Addressables.LoadAssetAsync<T>(@this.GetScopedKey(key)).ToUniTask(progress, cancellationToken);
                    @this.logger.Debug($"Loaded {key}");
                    return (Object)asset;
                }
                catch (InvalidKeyException)
                {
                    throw new KeyNotFoundException($"{key} not found in Addressables");
                }
            }, (@this: this, key, progress, cancellationToken));
        }

        async UniTask<IReadOnlyCollection<T>> IAssetManager.LoadAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (IReadOnlyCollection<T>)await this.cacheMultiple.GetOrAddAsync(key, static async state =>
            {
                var (@this, key, progress, cancellationToken) = state;
                var assets = await Addressables.LoadAssetsAsync<T>(@this.GetScopedKey(key), null).ToUniTask(progress, cancellationToken);
                @this.logger.Debug($"Loaded {assets.Count} assets for {key}");
                return (IReadOnlyCollection<Object>)assets;
            }, (@this: this, key, progress, cancellationToken));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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