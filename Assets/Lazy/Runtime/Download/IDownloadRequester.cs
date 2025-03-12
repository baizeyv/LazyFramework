using System;

namespace Lazy.Download
{
    /// <summary>
    /// * 下载请求器,用于下载前获取文件大小
    /// </summary>
    public interface IDownloadRequester
    {
        /// <summary>
        /// * 获取URI单个文件的大小
        /// # 若获取到,则回调传入正确的数值,否则-1
        /// </summary>
        /// <param name="uri">统一资源名称</param>
        /// <param name="callback"></param>
        void GetUriFileSizeAsync(string uri, Action<long> callback);

        /// <summary>
        /// * 获取多个URL地址下的所有文件的大小总和
        /// </summary>
        /// <param name="uris"></param>
        /// <param name="callback"></param>
        void GetUriFilesSizeAsync(string[] uris, Action<long> callback);
    }
}
