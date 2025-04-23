using System;

namespace Lazy
{
    /// <summary>
    /// * 下载任务
    /// </summary>
    public struct DownloadTask : IEquatable<DownloadTask>
    {
        /// <summary>
        /// * 下载任务ID
        /// </summary>
        public long DownloadID { get; private set; }

        /// <summary>
        /// * URL绝对路径
        /// </summary>
        public string DownloadURL { get; private set; }

        /// <summary>
        /// * 保存地址(绝对路径)
        /// </summary>
        public string DownloadPath { get; private set; }

        /// <summary>
        /// * 下载byte的偏移量,用于断点续传
        /// </summary>
        public long DownloadByteOffset { get; private set; }

        /// <summary>
        /// * 当本地存在时,下载时追加写入
        /// </summary>
        public bool DownloadAppend { get; private set; }

        public DownloadTask(
            long downloadID,
            string downloadURL,
            string downloadPath,
            long downloadByteOffset,
            bool downloadAppend
        )
        {
            DownloadID = downloadID;
            DownloadURL = downloadURL;
            DownloadPath = downloadPath;
            DownloadByteOffset = downloadByteOffset;
            DownloadAppend = downloadAppend;
        }

        public bool Equals(DownloadTask other)
        {
            var result = false;
            if (GetType() == other.GetType())
                result =
                    DownloadURL == other.DownloadURL
                    && DownloadPath == other.DownloadPath
                    && DownloadID == other.DownloadID
                    && DownloadByteOffset == other.DownloadByteOffset
                    && DownloadAppend == other.DownloadAppend;

            return result;
        }
    }
}
