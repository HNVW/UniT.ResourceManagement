#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using UnityEngine;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public interface IExternalAssetsManager
    {
        #if UNIT_UNITASK
        public UniTask<string> DownloadTextAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<byte[]> DownloadBufferAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<Texture2D> DownloadTextureAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<Sprite> DownloadSpriteAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<AudioClip> DownloadAudioClipAsync(string url, AudioType audioType, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadFileAsync(string url, string savePath, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        #region Uri

        public UniTask<string> DownloadTextAsync(Uri uri, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadTextAsync(uri.ToString(), cache, progress, cancellationToken);

        public UniTask<byte[]> DownloadBufferAsync(Uri uri, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadBufferAsync(uri.ToString(), cache, progress, cancellationToken);

        public UniTask<Texture2D> DownloadTextureAsync(Uri uri, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadTextureAsync(uri.ToString(), cache, progress, cancellationToken);

        public UniTask<Sprite> DownloadSpriteAsync(Uri uri, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadSpriteAsync(uri.ToString(), cache, progress, cancellationToken);

        public UniTask<AudioClip> DownloadAudioClipAsync(Uri uri, AudioType audioType, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadAudioClipAsync(uri.ToString(), audioType, cache, progress, cancellationToken);

        public UniTask DownloadFileAsync(Uri uri, string savePath, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadFileAsync(uri.ToString(), savePath, cache, progress, cancellationToken);

        #endregion

        #else
        public IEnumerator DownloadTextAsync(string url, Action<string> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadBufferAsync(string url, Action<byte[]> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadTextureAsync(string url, Action<Texture2D> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadSpriteAsync(string url, Action<Sprite> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadAudioClipAsync(string url, AudioType audioType, Action<AudioClip> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadFileAsync(string url, string savePath, Action? callback = null, bool cache = true, IProgress<float>? progress = null);

        #region Uri

        public IEnumerator DownloadTextAsync(Uri uri, Action<string> callback, bool cache = true, IProgress<float>? progress = null) => this.DownloadTextAsync(uri.ToString(), callback, cache, progress);

        public IEnumerator DownloadBufferAsync(Uri uri, Action<byte[]> callback, bool cache = true, IProgress<float>? progress = null) => this.DownloadBufferAsync(uri.ToString(), callback, cache, progress);

        public IEnumerator DownloadTextureAsync(Uri uri, Action<Texture2D> callback, bool cache = true, IProgress<float>? progress = null) => this.DownloadTextureAsync(uri.ToString(), callback, cache, progress);

        public IEnumerator DownloadSpriteAsync(Uri uri, Action<Sprite> callback, bool cache = true, IProgress<float>? progress = null) => this.DownloadSpriteAsync(uri.ToString(), callback, cache, progress);

        public IEnumerator DownloadAudioClipAsync(Uri uri, AudioType audioType, Action<AudioClip> callback, bool cache = true, IProgress<float>? progress = null) => this.DownloadAudioClipAsync(uri.ToString(), audioType, callback, cache, progress);

        public IEnumerator DownloadFileAsync(Uri uri, string savePath, Action? callback = null, bool cache = true, IProgress<float>? progress = null) => this.DownloadFileAsync(uri.ToString(), savePath, callback, cache, progress);

        #endregion

        #endif

        public void DeleteCache(string key);
    }
}