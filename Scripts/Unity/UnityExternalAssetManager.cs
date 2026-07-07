#nullable enable
namespace UniT.ResourceManagement.Unity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Scripting;
    using ILogger = Logging.ILogger;
    using Object = UnityEngine.Object;

    public sealed class UnityExternalAssetManager : IExternalAssetManager, IDisposable
    {
        #region Constructor

        private readonly ILogger logger;

        private readonly Dictionary<string, object> cache = new();

        [Preserve]
        public UnityExternalAssetManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        async UniTask<string> IExternalAssetManager.DownloadTextAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (string)await DownloadTextAsync();
            return (string)await this.cache.GetOrAddAsync(url, DownloadTextAsync);

            async UniTask<object> DownloadTextAsync()
            {
                using var request = UnityWebRequest.Get(url);
                await this.DownloadAsync(request, progress, cancellationToken);
                return request.downloadHandler.text;
            }
        }

        async UniTask<byte[]> IExternalAssetManager.DownloadBufferAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (byte[])await DownloadBufferAsync();
            return (byte[])await this.cache.GetOrAddAsync(url, DownloadBufferAsync);

            async UniTask<object> DownloadBufferAsync()
            {
                using var request = UnityWebRequest.Get(url);
                await this.DownloadAsync(request, progress, cancellationToken);
                return request.downloadHandler.data;
            }
        }

        async UniTask<Texture2D> IExternalAssetManager.DownloadTextureAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (Texture2D)await DownloadTextureAsync();
            return (Texture2D)await this.cache.GetOrAddAsync(url, DownloadTextureAsync);

            async UniTask<object> DownloadTextureAsync()
            {
                using var request = UnityWebRequestTexture.GetTexture(url);
                await this.DownloadAsync(request, progress, cancellationToken);
                return DownloadHandlerTexture.GetContent(request);
            }
        }

        async UniTask<AudioClip> IExternalAssetManager.DownloadAudioClipAsync(string url, AudioType audioType, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (AudioClip)await DownloadAudioClipAsync();
            return (AudioClip)await this.cache.GetOrAddAsync(url, DownloadAudioClipAsync);

            async UniTask<object> DownloadAudioClipAsync()
            {
                using var request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
                await this.DownloadAsync(request, progress, cancellationToken);
                return DownloadHandlerAudioClip.GetContent(request);
            }
        }

        async UniTask IExternalAssetManager.DownloadFileAsync(string url, string savePath, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (cache && File.Exists(savePath)) return;
            this.logger.Debug($"Saving {url} to {savePath}");
            using var request = new UnityWebRequest(url);
            request.downloadHandler = new DownloadHandlerFile(savePath);
            await this.DownloadAsync(request, progress, cancellationToken);
        }

        void IExternalAssetManager.DeleteCache(string url)
        {
            if (!this.cache.Remove(url, out var obj))
            {
                this.logger.Warning($"{url} not downloaded");
                return;
            }
            if (obj is Object unityObj) Object.Destroy(unityObj);
            this.logger.Debug($"Deleted {url}");
        }

        void IDisposable.Dispose()
        {
            this.cache.Clear(static obj =>
            {
                if (obj is Object unityObj) Object.Destroy(unityObj);
            });
            this.logger.Debug("Disposed");
        }

        private async UniTask DownloadAsync(UnityWebRequest request, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            this.logger.Debug($"Downloading {request.url}");
            await request.SendWebRequest().ToUniTask(progress: progress, cancellationToken: cancellationToken);
            this.logger.Debug($"Downloaded {request.url}");
        }
    }
}