using System;

namespace Lazy
{
    /// <summary>
    /// * 下载相关信息
    /// </summary>
    public struct DownloadInfo : IEquatable<DownloadInfo>
    {
        /// <summary>
        /// * 下载ID
        /// </summary>
        public long DownloadID { get; private set; }

        /// <summary>
        /// * 资源地址
        /// </summary>
        public string DownloadURL { get; private set; }

        /// <summary>
        /// * 下载后保存的地址
        /// </summary>
        public string DownloadPath { get; private set; }

        /// <summary>
        /// * 已下载的长度
        /// </summary>
        public ulong DownloadedLength { get; private set; }

        /// <summary>
        /// * 下载进度
        /// </summary>
        public float DownloadProgress { get; private set; }

        /// <summary>
        /// * 下载用时
        /// </summary>
        public TimeSpan DownloadTimeSpan { get; private set; }

        public DownloadInfo(
            long downloadID,
            string downloadURL,
            string downloadPath,
            ulong downloadedLength,
            float downloadProgress,
            TimeSpan downloadTimeSpan
        )
        {
            DownloadID = downloadID;
            DownloadURL = downloadURL;
            DownloadPath = downloadPath;
            DownloadedLength = downloadedLength;
            DownloadProgress = downloadProgress;
            DownloadTimeSpan = downloadTimeSpan;
        }

        public bool Equals(DownloadInfo other)
        {
            return DownloadURL.Equals(other.DownloadURL) && DownloadPath.Equals(other.DownloadPath);
        }

        public override string ToString()
        {
            return $"DownloadId:{DownloadID} ;URL: {DownloadURL}; DownloadPath: {DownloadPath}; DownloadedLength: {DownloadedLength};DownloadProgress: {DownloadProgress} ; DownloadTimeSpan: {DownloadTimeSpan}";
        }
    }
}
