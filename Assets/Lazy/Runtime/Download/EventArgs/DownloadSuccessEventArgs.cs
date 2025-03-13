using System;
using Lazy.Ref;

namespace Lazy.Download.Args
{
    public class DownloadSuccessEventArgs : IReference
    {
        /// <summary>
        /// * 下载信息
        /// </summary>
        public DownloadInfo DownloadInfo { get; private set; }

        /// <summary>
        /// * 当前下载任务索引
        /// </summary>
        public int CurrentDownloadTaskIndex { get; private set; }

        /// <summary>
        /// * 下载任务数量
        /// </summary>
        public int DownloadTaskCount { get; private set; }

        /// <summary>
        /// * 下载耗时
        /// </summary>
        public TimeSpan TimeSpan { get; private set; }

        public static DownloadSuccessEventArgs Obtain(
            DownloadInfo info,
            int currentDownloadTaskIndex,
            int downloadTaskCount,
            TimeSpan timeSpan
        )
        {
            var args = ReferencePool.Instance.Obtain<DownloadSuccessEventArgs>();
            args.DownloadInfo = info;
            args.CurrentDownloadTaskIndex = currentDownloadTaskIndex;
            args.DownloadTaskCount = downloadTaskCount;
            args.TimeSpan = timeSpan;
            return args;
        }

        public static void Free(DownloadSuccessEventArgs args)
        {
            ReferencePool.Instance.Free(args);
        }

        public void Clear()
        {
            DownloadInfo = default;
            CurrentDownloadTaskIndex = 0;
            DownloadTaskCount = 0;
            TimeSpan = TimeSpan.Zero;
        }
    }
}
