using System;

namespace Lazy
{
    public class DownloadFailureEventArgs : IReference
    {
        /// <summary>
        /// * 下载信息
        /// </summary>
        public DownloadInfo DownloadInfo { get; private set; }

        /// <summary>
        /// * 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; }

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

        public static DownloadFailureEventArgs Obtain(
            DownloadInfo info,
            int currentTaskIndex,
            int taskCount,
            string errorMessage,
            TimeSpan timeSpan
        )
        {
            var args = ReferencePool.Instance.Obtain<DownloadFailureEventArgs>();
            args.DownloadInfo = info;
            args.CurrentDownloadTaskIndex = currentTaskIndex;
            args.DownloadTaskCount = taskCount;
            args.ErrorMessage = errorMessage;
            args.TimeSpan = timeSpan;
            return args;
        }

        public static void Free(DownloadFailureEventArgs args)
        {
            ReferencePool.Instance.Free(args);
        }

        public void Clear()
        {
            DownloadInfo = default;
            CurrentDownloadTaskIndex = 0;
            DownloadTaskCount = 0;
            ErrorMessage = null;
            TimeSpan = TimeSpan.Zero;
        }
    }
}
