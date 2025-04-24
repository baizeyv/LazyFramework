using System;
using Lazy;

namespace Lazy
{
    public class DownloadTasksCompletedEventArgs : IReference
    {
        /// <summary>
        /// * 下载成功信息
        /// </summary>
        public DownloadInfo[] SuccessInfos { get; private set; }

        /// <summary>
        /// * 下载失败信息
        /// </summary>
        public DownloadInfo[] FailInfos { get; private set; }

        /// <summary>
        /// * 下载耗时
        /// </summary>
        public TimeSpan TimeSpan { get; private set; }

        /// <summary>
        /// * 下载任务数量
        /// </summary>
        public int DownloadTaskCount { get; private set; }

        public static DownloadTasksCompletedEventArgs Obtain(
            DownloadInfo[] successInfos,
            DownloadInfo[] failInfos,
            TimeSpan timeSpan,
            int downloadedCount
        )
        {
            var args = ReferencePool.Instance.Obtain<DownloadTasksCompletedEventArgs>();
            args.SuccessInfos = successInfos;
            args.FailInfos = failInfos;
            args.TimeSpan = timeSpan;
            args.DownloadTaskCount = downloadedCount;
            return args;
        }

        public static void Free(DownloadTasksCompletedEventArgs args)
        {
            ReferencePool.Instance.Free(args);
        }

        public void Clear()
        {
            SuccessInfos = null;
            FailInfos = null;
            TimeSpan = TimeSpan.Zero;
            DownloadTaskCount = 0;
        }
    }
}
