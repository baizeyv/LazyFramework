using System;
using System.Collections;
using System.Collections.Generic;
using Lazy;
using Lazy.Runtime.Utility;
using UnityEngine.Networking;

namespace Lazy
{
    public class Downloader
    {
        /// <summary>
        /// * 当前排序到的下载ID
        /// </summary>
        private long _currentId = 0;

        /// <summary>
        /// * 下载超时时间
        /// </summary>
        public int DownloadTimeout { get; set; }

        public bool Downloading { get; private set; }

        public int PendingCount { get; }

        /// <summary>
        /// * 下载任务数量
        /// </summary>
        private int _downloadTaskCount = 0;

        /// <summary>
        /// * 挂起的下载任务
        /// </summary>
        private readonly Queue<DownloadTask> _pendingTasks = new();

        /// <summary>
        /// * 挂起的下载任务字典缓存
        /// </summary>
        private Dictionary<long, DownloadTask> _pendingTaskDic = new();

        /// <summary>
        /// * 下载成功的任务
        /// </summary>
        private readonly List<DownloadInfo> _successInfos = new();

        /// <summary>
        /// * 下载失败的任务
        /// </summary>
        private readonly List<DownloadInfo> _failInfos = new();

        /// <summary>
        /// * 下载起始时间
        /// </summary>
        private DateTime _downloadStartTime;

        /// <summary>
        /// * 下载结束时间
        /// </summary>
        private DateTime _downloadEndTime;

        /// <summary>
        /// * 下载请求
        /// </summary>
        private UnityWebRequest _unityWebRequest;

        /// <summary>
        /// * 当前下载任务的索引
        /// </summary>
        private int _currentDownloadTaskIndex = 0;

        /// <summary>
        /// * 当前是否可下载
        /// </summary>
        private bool _canDownload;

        /// <summary>
        /// * 总共需要下载的文件大小
        /// </summary>
        private long _totalRequirementDownloadLength;

        /// <summary>
        /// * 已经下载的文件大小
        /// </summary>
        private long _completedDownloadLength;

        public event Action<DownloadStartEventArgs> OnDownloadStart;
        public event Action<DownloadSuccessEventArgs> OnDownloadSuccess;
        public event Action<DownloadFailureEventArgs> OnDownloadFailure;
        public event Action<DownloadUpdateEventArgs> OnDownloadUpdate;
        public event Action<DownloadTasksCompletedEventArgs> OnDownloadTasksCompleted;

        /// <summary>
        /// * 重定向限制次数 (unity默认32)
        /// </summary>
        public int RedirectLimit { get; set; } = 32;

        /// <summary>
        /// * 终止下载时删除已下载的部分 (默认false)
        /// </summary>
        public bool DeleteFileOnAbort { get; set; } = false;

        internal Downloader()
        {
        }

        /// <summary>
        /// * 添加下载任务,并返回下载任务的唯一标识
        /// </summary>
        /// <param name="downloadUri"></param>
        /// <param name="downloadPath"></param>
        /// <param name="downloadByteOffset"></param>
        /// <param name="downloadAppend"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public long AddDownload(
            string downloadUri,
            string downloadPath,
            long downloadByteOffset = 0,
            bool downloadAppend = false
        )
        {
            if (string.IsNullOrEmpty(downloadUri))
                throw new ArgumentNullException(nameof(downloadUri) + " URI is invalid!");
            if (string.IsNullOrEmpty(downloadPath))
                throw new ArgumentNullException(nameof(downloadPath) + " DownloadPath is invalid!");
            // # 创建新的下载任务
            var task = new DownloadTask(
                _currentId++,
                downloadUri,
                downloadPath,
                downloadByteOffset,
                downloadAppend
            );
            // # 将下载任务添加到待处理中
            _pendingTasks.Enqueue(task);
            _pendingTaskDic.Add(task.DownloadID, task);
            _downloadTaskCount++;
            return task.DownloadID;
        }

        /// <summary>
        /// * 移除指定下载任务
        /// </summary>
        /// <param name="downloadIds"></param>
        public void RemoveDownloads(long[] downloadIds)
        {
            foreach (var item in downloadIds)
                RemoveDownload(item);
        }

        /// <summary>
        /// * 移除指定下载
        /// </summary>
        /// <param name="downloadID"></param>
        /// <returns></returns>
        public bool RemoveDownload(long downloadID)
        {
            if (_pendingTaskDic.Remove(downloadID, out var task))
            {
                _pendingTasks.Remove(task);
                _downloadTaskCount--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// * 移除所有下载
        /// </summary>
        public void RemoveAllDownloads()
        {
            OnAbortDownload();
        }

        /// <summary>
        /// * 开始下载
        /// </summary>
        public void StartDownload()
        {
            // # 若正在下载,则不执行任何操作
            if (Downloading)
                return;
            // # 标记可以开始下载
            _canDownload = true;
            // # 若没有待处理的任务,则不执行
            if (_pendingTasks.Count == 0 || !_canDownload)
            {
                _canDownload = false;
                return;
            }

            // # 记录下载开始时间
            _downloadStartTime = DateTime.Now;
            // # 开始下载多个文件
            CoroutineCenter.StartCoroutine(DownloadMultipleFiles());
        }

        /// <summary>
        /// * 取消下载
        /// </summary>
        public void CancelDownload()
        {
            OnAbortDownload();
        }

        /// <summary>
        /// * 下载多文件协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadMultipleFiles()
        {
            Downloading = true;
            while (_pendingTasks.Count > 0)
            {
                var task = _pendingTasks.Dequeue();
                _currentDownloadTaskIndex = _downloadTaskCount - _pendingTasks.Count;
                yield return DownloadFile(task);
                _pendingTaskDic.Remove(task.DownloadID);
            }

            OnPendingTasksCompleted();
            Downloading = false;
        }

        /// <summary>
        /// * 下载单文件协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadFile(DownloadTask downloadTask)
        {
            // # 下载URL路径
            var downloadUrl = downloadTask.DownloadURL;
            // # 保存目录
            var downloadPath = downloadTask.DownloadPath;
            var startTime = DateTime.Now;
            using (var request = UnityWebRequest.Get(downloadUrl))
            {
                var append = downloadTask.DownloadAppend;
#if UNITY_2019_1_OR_NEWER
                var handler = new DownloadHandlerFile(downloadTask.DownloadPath, append)
                {
                    removeFileOnAbort = DeleteFileOnAbort
                };
#elif UNITY_2018_1_OR_NEWER
                var handler = new DownloadHandlerFile(downloadTask.DownloadPath)
                {
                    removeFileOnAbort = DeleteFileOnAbort,
                };
#endif
                request.SetRequestHeader("Range", "bytes=" + downloadTask.DownloadByteOffset + "-");
                request.downloadHandler = handler;
                _unityWebRequest = request;
                request.timeout = DownloadTimeout;
                request.redirectLimit = RedirectLimit;

                var timeSpan = DateTime.Now - startTime;
                var downloadInfo = new DownloadInfo(
                    downloadTask.DownloadID,
                    downloadUrl,
                    downloadPath,
                    0,
                    0,
                    timeSpan
                );
                var args = DownloadStartEventArgs.Obtain(
                    downloadInfo,
                    _currentDownloadTaskIndex,
                    _downloadTaskCount
                );
                OnDownloadStart?.Invoke(args);
                DownloadStartEventArgs.Free(args);

                var operation = request.SendWebRequest();
                while (!operation.isDone && _canDownload)
                {
                    var ts = DateTime.Now - startTime;
                    var info = new DownloadInfo(
                        downloadTask.DownloadID,
                        downloadUrl,
                        downloadPath,
                        request.downloadedBytes,
                        operation.progress,
                        ts
                    );
                    OnFileDownloading(info);
                    yield return null;
                }
#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.ConnectionError &&
                    request.result != UnityWebRequest.Result.ProtocolError && _canDownload)
#elif UNITY_2018_1_OR_NEWER
                if (!request.isNetworkError && !request.isHttpError && _canDownload)
#endif
                {
                    if (request.isDone)
                    {
                        var ts = DateTime.Now - startTime;
                        var info = new DownloadInfo(downloadTask.DownloadID, downloadUrl, downloadPath,
                            request.downloadedBytes, 1, ts);
                        var successArgs =
                            DownloadSuccessEventArgs.Obtain(info, _currentDownloadTaskIndex, _downloadTaskCount, ts);
                        OnFileDownloading(downloadInfo);
                        OnDownloadSuccess?.Invoke(successArgs);
                        DownloadSuccessEventArgs.Free(successArgs);
                        _successInfos.Add(downloadInfo);
                    }
                }
                else
                {
                    var ts = DateTime.Now - startTime;
                    var info = new DownloadInfo(downloadTask.DownloadID, downloadUrl, downloadPath,
                        request.downloadedBytes, operation.progress, ts);
                    var failArgs = DownloadFailureEventArgs.Obtain(info, _currentDownloadTaskIndex, _downloadTaskCount,
                        request.error, ts);
                    OnFileDownloading(downloadInfo);
                    OnDownloadFailure?.Invoke(failArgs);
                    DownloadFailureEventArgs.Free(failArgs);
                    _failInfos.Add(info);
                    _unityWebRequest = null;
                }
            }
        }

        private void OnFileDownloading(DownloadInfo info)
        {
            var timeSpan = DateTime.Now - _downloadStartTime;
            var args = DownloadUpdateEventArgs.Obtain(
                info,
                _currentDownloadTaskIndex,
                _downloadTaskCount,
                timeSpan
            );
            OnDownloadUpdate?.Invoke(args);
            DownloadUpdateEventArgs.Free(args);
        }

        /// <summary>
        /// * 所有待下载的任务全部下载完成了
        /// </summary>
        private void OnPendingTasksCompleted()
        {
            _canDownload = false;
            _downloadEndTime = DateTime.Now;
            var args = DownloadTasksCompletedEventArgs.Obtain(
                _successInfos.ToArray(),
                _failInfos.ToArray(),
                _downloadEndTime - _downloadStartTime,
                _downloadTaskCount
            );
            OnDownloadTasksCompleted?.Invoke(args);
            DownloadTasksCompletedEventArgs.Free(args);
            // # 清理下载配置缓存
            _failInfos.Clear();
            _successInfos.Clear();
            _pendingTasks.Clear();
            _pendingTaskDic.Clear();
            _downloadTaskCount = 0;
        }

        private void OnAbortDownload()
        {
            if (Downloading)
                // # 正在下载中
                _unityWebRequest?.Abort();
            else
                _unityWebRequest?.Dispose();

            _pendingTasks.Clear();
            _pendingTaskDic.Clear();
            _downloadTaskCount = 0;
            _failInfos.Clear();
            _successInfos.Clear();
            _canDownload = false;
            Downloading = false;
        }
    }
}