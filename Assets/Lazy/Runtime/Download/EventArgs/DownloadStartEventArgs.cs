using Lazy.Pool;
using Lazy.Ref;

namespace Lazy.Download.Args
{
    public class DownloadStartEventArgs : IReference
    {
        /// <summary>
        /// * 下载信息
        /// </summary>
        public DownloadInfo DownloadInfo { get; private set; }

        /// <summary>
        /// * 当前下载任务的索引
        /// </summary>
        public int CurrentDownloadTaskIndex { get; private set; }

        /// <summary>
        /// * 下载任务的数量
        /// </summary>
        public int DownloadTaskCount { get; private set; }

        /// <summary>
        /// * 获取
        /// </summary>
        /// <param name="info"></param>
        /// <param name="currentDownloadTaskIndex"></param>
        /// <param name="downloadTaskCount"></param>
        /// <returns></returns>
        public static DownloadStartEventArgs Obtain(
            DownloadInfo info,
            int currentDownloadTaskIndex,
            int downloadTaskCount
        )
        {
            var args = ReferencePool.Instance.Obtain<DownloadStartEventArgs>();
            args.DownloadInfo = info;
            args.CurrentDownloadTaskIndex = currentDownloadTaskIndex;
            args.DownloadTaskCount = downloadTaskCount;
            return args;
        }

        /// <summary>
        /// * 归还
        /// </summary>
        /// <param name="args"></param>
        public static void Free(DownloadStartEventArgs args)
        {
            ReferencePool.Instance.Free(args);
        }

        public void Clear()
        {
            DownloadInfo = default;
            CurrentDownloadTaskIndex = 0;
            DownloadTaskCount = 0;
        }
    }
}
