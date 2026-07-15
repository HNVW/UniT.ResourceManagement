#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;

    public interface IRemoteAssetDownloader
    {
        public UniTask<long> GetDownloadSizeAsync(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<long> GetAllDownloadSizeAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadAsync(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadAllAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<long> GetDownloadSizeAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : notnull => this.GetDownloadSizeAsync(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask DownloadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : notnull => this.DownloadAsync(typeof(T).GetKey(), progress, cancellationToken);
    }
}