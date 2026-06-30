#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public interface IExternalAssetManager
    {
        public UniTask<string> DownloadTextAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<byte[]> DownloadBufferAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<Texture2D> DownloadTextureAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<AudioClip> DownloadAudioClipAsync(string url, AudioType audioType, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadFileAsync(string url, string savePath, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public void DeleteCache(string url);
    }
}