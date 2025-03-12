namespace Lazy.Download
{
    /// <summary>
    /// * 下载器接口
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// * 是否正在下载
        /// </summary>
        bool Downloading { get; }

        /// <summary>
        /// * 可下载的资源总数 (正在挂起的)
        /// </summary>
        int PendingCount { get; }

        /// <summary>
        /// * 添加下载
        /// </summary>
        /// <param name="downloadUri"></param>
        /// <param name="downloadPath">保存绝对地址</param>
        /// <param name="downloadByteOffset">下载byte的偏移量,用于断点续传</param>
        /// <param name="downloadAppend">当本地存在时,下载时追加写入</param>
        /// <returns>DownloadID</returns>
        long AddDownload(
            string downloadUri,
            string downloadPath,
            long downloadByteOffset = 0,
            bool downloadAppend = false
        );

        /// <summary>
        /// * 移除一个下载
        /// </summary>
        /// <param name="downloadID"></param>
        /// <returns></returns>
        bool RemoveDownload(long downloadID);

        /// <summary>
        /// * 移除所有下载
        /// </summary>
        void RemoveAllDownloads();

        /// <summary>
        /// * 启动下载
        /// </summary>
        void StartDownload();

        /// <summary>
        /// * 取消下载
        /// </summary>
        void CancelDownload();
    }
}
