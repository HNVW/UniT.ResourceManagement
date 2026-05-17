#if UNIT_ADDRESSABLES
#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public sealed class AddressableAssetsManager : IAssetsManager, IRemoteAssetsDownloader
    {
        #region Constructor

        private readonly string  keyPrefix;
        private readonly ILogger logger;

        private readonly Dictionary<object, Object>                      cache  = new();
        private readonly Dictionary<object, IReadOnlyCollection<string>> keyMap = new();

        [Preserve]
        public AddressableAssetsManager(ILoggerManager loggerManager, string? scope = null)
        {
            this.keyPrefix = scope.IsNullOrWhiteSpace() ? string.Empty : $"{scope}/";
            this.logger    = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");

            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += this.OnPlayModeStateChanged;
            #endif
        }

        #endregion

        #region Sync

        #if !UNITY_WEBGL
        bool IAssetsManager.Contains<T>(object key)
        {
            if (this.cache.ContainsKey(key) || this.keyMap.ContainsKey(key)) return true;
            var handle            = this.GetAllResourceLocationsInternal<T>(key);
            var resourceLocations = handle.WaitForResultOrThrow();
            var contains          = resourceLocations.Count > 0;
            handle.Release();
            return contains;
        }

        T IAssetsManager.Load<T>(object key) => this.Load<T>(key);

        IEnumerable<T> IAssetsManager.LoadAll<T>(object key)
        {
            return this.keyMap.GetOrAdd(key, static state =>
            {
                var handle            = state.@this.GetAllResourceLocationsInternal<T>(state.key);
                var resourceLocations = handle.WaitForResultOrThrow();
                var keys              = state.@this.GetAllKeys(resourceLocations);
                handle.Release();
                state.@this.logger.Debug($"Found {keys.Count} keys for {state.key}");
                return keys;
            }, (@this: this, key)).Select(this.Load<T>).ToArray();
        }

        private T Load<T>(object key) where T : Object
        {
            return (T)this.cache.GetOrAdd(key, static state =>
            {
                var asset = state.@this.LoadInternal<T>(state.key).WaitForResultOrThrow();
                state.@this.logger.Debug($"Loaded {state.key}");
                return asset;
            }, (@this: this, key));
        }
        #endif

        #endregion

        #region Async

        #if UNIT_UNITASK
        async UniTask<bool> IAssetsManager.ContainsAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (this.cache.ContainsKey(key) || this.keyMap.ContainsKey(key)) return true;
            var handle            = this.GetAllResourceLocationsInternal<T>(key);
            var resourceLocations = await handle.ToUniTask(progress, cancellationToken);
            var contains          = resourceLocations.Count > 0;
            handle.Release();
            return contains;
        }

        UniTask<T> IAssetsManager.LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken) => this.LoadAsync<T>(key, progress, cancellationToken);

        async UniTask<IEnumerable<T>> IAssetsManager.LoadAllAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var keys = await this.keyMap.GetOrAddAsync(key, static async state =>
            {
                var handle            = state.@this.GetAllResourceLocationsInternal<T>(state.key);
                var resourceLocations = await handle.ToUniTask(cancellationToken: state.cancellationToken);
                var keys              = state.@this.GetAllKeys(resourceLocations);
                handle.Release();
                state.@this.logger.Debug($"Found {keys.Count} keys for {state.key}");
                return keys;
            }, (@this: this, key, cancellationToken));
            return await keys.SelectAsync(this.LoadAsync<T>, progress, cancellationToken).ToArrayAsync();
        }

        UniTask IRemoteAssetsDownloader.DownloadAsync(object key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return this.DownloadInternal(key).ToUniTask(progress, cancellationToken);
        }

        async UniTask IRemoteAssetsDownloader.DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            await InitializeInternal().ToUniTask(subProgresses[0], cancellationToken);
            await DownloadAllInternal().ToUniTask(subProgresses[1], cancellationToken);
        }

        private async UniTask<T> LoadAsync<T>(object key, IProgress<float>? progress, CancellationToken cancellationToken) where T : Object
        {
            return (T)await this.cache.GetOrAddAsync(key, static async state =>
            {
                var asset = await state.@this.LoadInternal<T>(state.key).ToUniTask(state.progress, state.cancellationToken);
                state.@this.logger.Debug($"Loaded {state.key}");
                return (Object)asset;
            }, (@this: this, key, progress, cancellationToken));
        }
        #else
        IEnumerator IAssetsManager.ContainsAsync<T>(object key, Action<bool> callback, IProgress<float>? progress)
        {
            if (this.cache.ContainsKey(key) || this.keyMap.ContainsKey(key))
            {
                callback(true);
                yield break;
            }
            var handle = this.GetAllResourceLocationsInternal<T>(key);
            yield return handle.ToCoroutine(resourceLocations =>
            {
                var contains = resourceLocations.Count > 0;
                handle.Release();
                callback(contains);
            });
        }

        IEnumerator IAssetsManager.LoadAsync<T>(object key, Action<T> callback, IProgress<float>? progress) => this.LoadAsync(key, callback, progress);

        IEnumerator IAssetsManager.LoadAllAsync<T>(object key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            var keys = default(IReadOnlyCollection<string>)!;
            yield return this.keyMap.GetOrAddAsync(
                key,
                callback => this.GetAllResourceLocationsInternal<T>(key).ToCoroutine(resourceLocations =>
                {
                    var keys = this.GetAllKeys(resourceLocations);
                    this.logger.Debug($"Found {keys.Count} keys for {key}");
                    callback(keys);
                }),
                result => keys = result
            );
            this.logger.Debug($"Found {keys.Count} keys for {key}");
            yield return keys.SelectAsync<string, T>(this.LoadAsync, result => callback(result.ToArray()), progress);
        }

        IEnumerator IRemoteAssetsDownloader.DownloadAsync(object key, Action? callback, IProgress<float>? progress)
        {
            return this.DownloadInternal(key).ToCoroutine(callback, progress);
        }

        IEnumerator IRemoteAssetsDownloader.DownloadAllAsync(Action? callback, IProgress<float>? progress)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            yield return InitializeInternal().ToCoroutine(progress: subProgresses[0]);
            yield return DownloadAllInternal().ToCoroutine(progress: subProgresses[1]);
            callback?.Invoke();
        }

        private IEnumerator LoadAsync<T>(object key, Action<T> callback, IProgress<float>? progress) where T : Object
        {
            return this.cache.GetOrAddAsync(
                key,
                callback => this.LoadInternal<T>(key).ToCoroutine(
                    asset =>
                    {
                        this.logger.Debug($"Loaded {key}");
                        callback(asset);
                    },
                    progress
                ),
                asset => callback((T)asset)
            );
        }
        #endif

        #endregion

        #region Finalizer

        void IAssetsManager.Unload(object key) => this.Unload(key);

        void IAssetsManager.UnloadAll(object key) => this.UnloadAll(key);

        private void Unload(object key)
        {
            if (!this.cache.Remove(key, out var asset))
            {
                this.logger.Warning($"Trying to unload {key} that was not loaded");
                return;
            }
            #if UNITY_EDITOR
            if (this.playModeStateChanged) return;
            #endif
            Addressables.Release(asset);
            this.logger.Debug($"Unloaded {key}");
        }

        private void UnloadAll(object key)
        {
            if (!this.keyMap.Remove(key, out var keys))
            {
                this.logger.Warning($"Trying to unload all {key} that was not loaded");
                return;
            }
            keys.ForEach(this.Unload);
        }

        private void Dispose()
        {
            this.keyMap.Keys.SafeForEach(this.UnloadAll);
            this.cache.Keys.SafeForEach(this.Unload);
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
            this.logger.Debug("Disposed");
        }

        ~AddressableAssetsManager()
        {
            this.Dispose();
            this.logger.Debug("Finalized");
        }

        #if UNITY_EDITOR
        private bool playModeStateChanged;

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            EditorApplication.playModeStateChanged -= this.OnPlayModeStateChanged;

            this.playModeStateChanged = true;
        }
        #endif

        #endregion

        #region Internal

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object GetScopedKey(object key) => key is string ? $"{this.keyPrefix}{key}" : key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AsyncOperationHandle<T> LoadInternal<T>(object key)
        {
            return Addressables.LoadAssetAsync<T>(this.GetScopedKey(key));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AsyncOperationHandle<IList<IResourceLocation>> GetAllResourceLocationsInternal<T>(object key)
        {
            return Addressables.LoadResourceLocationsAsync(this.GetScopedKey(key), typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AsyncOperationHandle DownloadInternal(object key)
        {
            return Addressables.DownloadDependenciesAsync(this.GetScopedKey(key), autoReleaseHandle: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AsyncOperationHandle DownloadAllInternal()
        {
            return Addressables.DownloadDependenciesAsync(Addressables.ResourceLocators.SelectMany(locator => locator.Keys), autoReleaseHandle: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AsyncOperationHandle InitializeInternal()
        {
            return Addressables.InitializeAsync(autoReleaseHandle: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyCollection<string> GetAllKeys(IList<IResourceLocation> resourceLocations)
        {
            return resourceLocations.Select(static (resourceLocation, keyPrefix) => resourceLocation.PrimaryKey.TrimStart(keyPrefix), this.keyPrefix).ToArray();
        }

        #endregion
    }
}
#endif