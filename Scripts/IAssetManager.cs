#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Object = UnityEngine.Object;

    public interface IAssetManager
    {
        public UniTask<bool> ContainsAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask<bool> ContainsAllAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask<T> LoadAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask<IReadOnlyList<T>> LoadAllAsync<T>(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public void Unload(object key);

        public void UnloadAll(object key);

        #region Implicit Key

        public UniTask<bool> ContainsAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.ContainsAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<bool> ContainsAllAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.ContainsAllAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<T> LoadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.LoadAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<IReadOnlyList<T>> LoadAllAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.LoadAllAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public void Unload<T>() where T : Object => this.Unload(typeof(T).GetKey());

        public void UnloadAll<T>() where T : Object => this.UnloadAll(typeof(T).GetKey());

        #endregion
    }
}