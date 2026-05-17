#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using UniT.Extensions;
    using UnityEngine;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif
    #if !UNITY_WEBGL
    using System.Diagnostics.CodeAnalysis;
    #endif

    public interface IAssetsManager : IDisposable
    {
        #region Sync

        #if !UNITY_WEBGL
        public bool Contains<T>(object key) where T : Object;

        public T Load<T>(object key) where T : Object;

        public IEnumerable<T> LoadAll<T>(object key) where T : Object;

        #region Default Implementation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad<T>(object key, [MaybeNullWhen(false)] out T asset) where T : Object
        {
            try
            {
                asset = this.Load<T>(key);
                return true;
            }
            catch
            {
                asset = null;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LoadComponent<T>(object key) => this.Load<GameObject>(key).GetComponentOrThrow<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> LoadAllComponents<T>(object key) => GetAllComponents<T>(this.LoadAll<GameObject>(key));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadComponent<T>(object key, [MaybeNullWhen(false)] out T component)
        {
            component = default;
            return this.TryLoad<GameObject>(key, out var gameObject) && gameObject.TryGetComponent(out component);
        }

        #endregion

        #region Implicit Key

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<T>() where T : Object => this.Contains<T>(typeof(T).GetKey());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Load<T>() where T : Object => this.Load<T>(typeof(T).GetKey());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> LoadAll<T>() where T : Object => this.LoadAll<T>(typeof(T).GetKey());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad<T>([MaybeNullWhen(false)] out T asset) where T : Object => this.TryLoad(typeof(T).GetKey(), out asset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LoadComponent<T>() => this.LoadComponent<T>(typeof(T).GetKey());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> LoadAllComponents<T>() => this.LoadAllComponents<T>(typeof(T).GetKey());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoadComponent<T>([MaybeNullWhen(false)] out T component) => this.TryLoadComponent(typeof(T).GetKey(), out component);

        #endregion

        #endif

        #endregion

        #region Async

        #if UNIT_UNITASK
        public UniTask<bool> ContainsAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask<T> LoadAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask<IEnumerable<T>> LoadAllAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        #region Default Implementation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask<(bool IsSucceeded, T Asset)> TryLoadAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object
        {
            try
            {
                return (true, await this.LoadAsync<T>(key, progress, cancellationToken));
            }
            catch
            {
                return (false, null!);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<T> LoadComponentAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAsync<GameObject>(key, progress, cancellationToken).ContinueWith(gameObject => gameObject.GetComponentOrThrow<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<IEnumerable<T>> LoadAllComponentsAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAllAsync<GameObject>(key, progress, cancellationToken).ContinueWith(GetAllComponents<T>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<(bool IsSucceeded, T Component)> TryLoadComponentAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
        {
            return this.TryLoadAsync<GameObject>(key, progress, cancellationToken)
                .ContinueWith((isSucceeded, asset) =>
                {
                    var component = default(T)!;
                    return (isSucceeded && asset.TryGetComponent<T>(out component), component);
                });
        }

        #endregion

        #region Implicit Key

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<bool> ContainsAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.ContainsAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<T> LoadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.LoadAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<IEnumerable<T>> LoadAllAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.LoadAllAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<(bool IsSucceeded, T Asset)> TryLoadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.TryLoadAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<T> LoadComponentAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadComponentAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<IEnumerable<T>> LoadAllComponentsAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAllComponentsAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask<(bool IsSucceeded, T Component)> TryLoadComponentAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.TryLoadComponentAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        #endregion

        #else
        public IEnumerator ContainsAsync<T>(object key, Action<bool> callback, IProgress<float>? progress = null) where T : Object;

        public IEnumerator LoadAsync<T>(object key, Action<T> callback, IProgress<float>? progress = null) where T : Object;

        public IEnumerator LoadAllAsync<T>(object key, Action<IEnumerable<T>> callback, IProgress<float>? progress = null) where T : Object;

        #region Default Implementation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator TryLoadAsync<T>(object key, Action<(bool IsSucceeded, T Asset)> callback, IProgress<float>? progress = null) where T : Object
        {
            return this.LoadAsync<T>(
                key,
                asset => callback((true, asset)),
                progress
            ).Catch(() => callback((false, null!)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator LoadComponentAsync<T>(object key, Action<T> callback, IProgress<float>? progress = null) => this.LoadAsync<GameObject>(key, gameObject => callback(gameObject.GetComponentOrThrow<T>()), progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator LoadAllComponentsAsync<T>(object key, Action<IEnumerable<T>> callback, IProgress<float>? progress = null) => this.LoadAllAsync<GameObject>(key, gameObjects => callback(GetAllComponents<T>(gameObjects)), progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator TryLoadComponentAsync<T>(object key, Action<(bool IsSucceeded, T Component)> callback, IProgress<float>? progress = null)
        {
            return this.TryLoadAsync<GameObject>(
                key,
                result =>
                {
                    var component = default(T)!;
                    callback((result.IsSucceeded && result.Asset.TryGetComponent<T>(out component), component));
                },
                progress
            );
        }

        #endregion

        #region Implicit Key

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator ContainsAsync<T>(Action<bool> callback, IProgress<float>? progress = null) where T : Object => this.ContainsAsync<T>(typeof(T).GetKey(), callback, progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator LoadAsync<T>(Action<T> callback, IProgress<float>? progress = null) where T : Object => this.LoadAsync(typeof(T).GetKey(), callback, progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator LoadAllAsync<T>(Action<IEnumerable<T>> callback, IProgress<float>? progress = null) where T : Object => this.LoadAllAsync(typeof(T).GetKey(), callback, progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator TryLoadAsync<T>(Action<(bool IsSucceeded, T Asset)> callback, IProgress<float>? progress = null) where T : Object => this.TryLoadAsync(typeof(T).GetKey(), callback, progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator LoadComponentAsync<T>(Action<T> callback, IProgress<float>? progress = null) => this.LoadComponentAsync(typeof(T).GetKey(), callback, progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator LoadAllComponentsAsync<T>(Action<IEnumerable<T>> callback, IProgress<float>? progress = null) => this.LoadAllComponentsAsync(typeof(T).GetKey(), callback, progress);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator TryLoadComponentAsync<T>(Action<(bool IsSucceeded, T Component)> callback, IProgress<float>? progress = null) => this.TryLoadComponentAsync(typeof(T).GetKey(), callback, progress);

        #endregion

        #endif

        #endregion

        public void Unload(object key);

        public void UnloadAll(object key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unload<T>() => this.Unload(typeof(T).GetKey());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<T> GetAllComponents<T>(IEnumerable<GameObject> gameObjects) => gameObjects.Select(gameObject => gameObject.GetComponent<T>()).OfType<T>();
    }
}