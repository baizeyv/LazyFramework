using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lazy.Manage;
using Lazy.Runtime.Utility;
using Lazy.Singleton;
using Lazy.Utility;
using UnityEngine.Networking;

namespace Lazy.Download
{
    public class DownloadManager : Singleton<DownloadManager>, IManager
    {
        /// <summary>
        /// * 下载器字典
        /// </summary>
        private Dictionary<string, Downloader> _downloaders = new();

        private DownloadManager()
        {
        }

        /// <summary>
        /// * 获取URI单个文件的大小
        /// # 若获取到,则回调传入正确的数值,否则-1
        /// </summary>
        /// <param name="uri">统一资源名称</param>
        /// <param name="callback"></param>
        public void GetUriFileSizeAsync(string uri, Action<long> callback)
        {
            CoroutineCenter.StartCoroutine(GetFileSize(uri, callback));
        }

        /// <summary>
        /// * 获取多个URL地址下的所有文件的大小总和
        /// </summary>
        /// <param name="uris"></param>
        /// <param name="callback"></param>
        public void GetUriFilesSizeAsync(string[] uris, Action<long> callback)
        {
            CoroutineCenter.StartCoroutine(GetMultiFilesSize(uris, callback));
        }

        private IEnumerator GetMultiFilesSize(string[] uris, Action<long> callback)
        {
            long ret = 0;
            foreach (var uri in uris)
                yield return GetFileSize(uri, size =>
                {
                    if (size >= 0)
                        ret += size;
                });
            callback?.Invoke(ret);
        }

        private IEnumerator GetFileSize(string uri, Action<long> callback)
        {
            using (var request = UnityWebRequest.Head(uri))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                if (
                    request.result != UnityWebRequest.Result.ConnectionError
                    && request.result != UnityWebRequest.Result.ProtocolError
                )
#elif UNITY_2018_1_OR_NEWER
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    var contentLengthHeader = request.GetResponseHeader("Content-Length");
                    if (
                        string.IsNullOrEmpty(contentLengthHeader)
                        || !long.TryParse(contentLengthHeader, out var fileLength)
                    )
                    {
                        Log.Log.MsgE("Content-Length 标头找不到或无效: " + request.error);
                        callback?.Invoke(-1);
                    }
                    else
                    {
                        callback.Invoke(fileLength);
                    }
                }
                else
                {
                    Log.Log.MsgE("检索文件大小时出错: " + request.error);
                    callback?.Invoke(-1);
                }
            }
        }

        /// <summary>
        /// * 创建下载器
        /// </summary>
        /// <param name="downloaderName"></param>
        /// <returns></returns>
        public Downloader CreateDownloader(string downloaderName)
        {
            Downloader downloader;
            if (_downloaders.TryAdd(downloaderName, downloader = new Downloader()))
                return downloader;
            Log.Log.MsgE($"已存在同名的下载器:{downloaderName}");
            return null;
        }

        /// <summary>
        /// * 会验证文件大小的安全下载
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <param name="callback"></param>
        /// <param name="overrideOld">本地文件异常时覆盖或断点续传</param>
        public void SafeDownload(string url, string savePath, Action<bool> callback = null, bool overrideOld = false)
        {
            GetUriFileSizeAsync(url, size =>
            {
                if (size < 0)
                {
                    callback.Fire(false);
                    return;
                }

                if (File.Exists(savePath))
                {
                    // # 文件存在
                    var len = new FileInfo(savePath).Length;
                    if (len == size)
                    {
                        // # 文件有效
                        callback.Fire(true);
                    }
                    else if (size < len && !overrideOld)
                    {
                        // # 断点续传
                        var downloader = CreateDownloader(savePath);
                        downloader.OnDownloadSuccess += _ => callback.Fire(true);
                        downloader.OnDownloadFailure += _ => callback.Fire(false);
                        downloader.AddDownload(url, savePath, size, true);
                        downloader.StartDownload();
                    }
                    else
                    {
                        // # 本地文件大于远程的,一定是本地资源错误
                        var downloader = CreateDownloader(savePath);
                        downloader.OnDownloadSuccess += _ => callback.Fire(true);
                        downloader.OnDownloadFailure += _ => callback.Fire(false);
                        downloader.AddDownload(url, savePath);
                        downloader.StartDownload();
                    }
                }
                else
                {
                    // # 文件不存在
                    var downloader = CreateDownloader(savePath);
                    downloader.OnDownloadSuccess += _ => callback.Fire(true);
                    downloader.OnDownloadFailure += _ => callback.Fire(false);
                    downloader.AddDownload(url, savePath);
                    downloader.StartDownload();
                }
            });
        }

        public override void OnSingletonInitialize()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnDestroy()
        {
            foreach (var item in _downloaders.Values)
                item.CancelDownload();
        }
    }
}