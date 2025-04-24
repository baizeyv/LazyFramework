using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Lazy
{
    /// <summary>
    /// * 资源文件下载器
    /// </summary>
    public class DownloadRequest
    {
        /// <summary>
        /// * 禁用Unity缓存系统在WebGL平台(微信小游戏使用)
        /// </summary>
        public static bool DisableUnityCacheOnWebGL = false;

        /// <summary>
        /// * 下载类型
        /// </summary>
        private DownloadType _downloadType;

        /// <summary>
        /// * Unity网络请求
        /// </summary>
        private UnityWebRequest _request;

        /// <summary>
        /// * 请求是否完成了
        /// </summary>
        public bool IsFinished => _request == null || _request.isDone;

        /// <summary>
        /// * 下载进度
        /// </summary>
        public float Progress => _request?.downloadProgress ?? 0;

        /// <summary>
        /// * 获取已下载的文件的文本
        /// </summary>
        public string DownloadedFileText =>
            IsFinished && _downloadType == DownloadType.File && _request != null
                ? _request.downloadHandler.text
                : null;

        /// <summary>
        /// * 获取已下载的AssetBundle
        /// </summary>
        public AssetBundle DownloadedAssetBundle =>
            IsFinished && _downloadType == DownloadType.AssetBundle && _request != null
                ? DownloadHandlerAssetBundle.GetContent(_request)
                : null;

        /// <summary>
        /// * 获取已下载的数据
        /// </summary>
        public byte[] DownloadedBytes =>
            IsFinished && _downloadType != DownloadType.None && _request != null
                ? _request.downloadHandler.data
                : null;

        public DownloadRequest(string uri)
        {
            _downloadType = DownloadType.File;
            SendFileDownloadRequest(uri);
        }

        /// <summary>
        /// * 发送文件下载请求
        /// </summary>
        /// <param name="uri"></param>
        private void SendFileDownloadRequest(string uri)
        {
            try
            {
                if (URLUtility.IsLegalUri(uri))
                {
                    // # uri 合法,发送请求
                    _request = new UnityWebRequest(
                        uri,
                        UnityWebRequest.kHttpVerbGET,
                        new DownloadHandlerBuffer(),
                        null
                    );
                    _request.SendWebRequest();
                }
                else
                {
                    LoadFail();
                }
            }
            catch (Exception e)
            {
                Log.MsgE($"无法发送URI:{uri}文件下载请求,Exception:{e.Message}");
                LoadFail();
            }
        }

        /// <summary>
        /// * 创建一个AssetBundle下载请求
        /// </summary>
        /// <param name="uri">请求的URI</param>
        /// <param name="hash">
        /// * 一个整数版本号,将与AssetBundle的缓存版本进行比较以确定是否下载
        /// * 将此数字递增以强制Unity重新下载缓存的AssetBundle
        /// * 如果为0则忽略版本分配
        /// </param>
        /// <param name="crc">
        /// * 如果非0，将与已下载的AssetBundle数据的校验和进行比较
        /// * 如果CRC不匹配,将记录错误并不加载AssetBundle.
        /// * 如果设置为0，则跳过CRC检查
        /// </param>
        public DownloadRequest(string uri, Hash128 hash, uint crc = 0)
        {
            _downloadType = DownloadType.AssetBundle;
            SendAssetBundleDownloadRequest(uri, hash, crc);
        }

        /// <summary>
        /// * 发送AssetBundle下载请求
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="hash"></param>
        /// <param name="crc"></param>
        private void SendAssetBundleDownloadRequest(
            string uri,
            Hash128 hash = default,
            uint crc = 0
        )
        {
            try
            {
                if (URLUtility.IsLegalUri(uri))
                {
                    if (hash == default || DisableUnityCacheOnWebGL)
                        _request = UnityWebRequestAssetBundle.GetAssetBundle(uri, crc);
                    else
                        _request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, crc);

                    _request.SendWebRequest();
                }
                else
                {
                    LoadFail();
                }
            }
            catch (Exception e)
            {
                Log.MsgE($"无法为URI:{uri} AssetBundle下载请求,Exception:{e.Message}");
                LoadFail();
            }
        }

        /// <summary>
        /// * 发送AssetBundle下载请求协程
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="hash"></param>
        /// <param name="crc"></param>
        /// <returns></returns>
        public IEnumerator SendAssetBundleDownloadRequestCoroutine(
            string uri,
            Hash128 hash = default,
            uint crc = 0
        )
        {
            if (!URLUtility.IsLegalUri(uri))
            {
                Log.MsgE($"无法为URI:{uri} AssetBundle下载请求。无效的URI");
                LoadFail();
                yield break;
            }

            try
            {
                if (hash == default || DisableUnityCacheOnWebGL)
                    _request = UnityWebRequestAssetBundle.GetAssetBundle(uri, crc);
                else
                    _request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, crc);
            }
            catch (Exception e)
            {
                Log.MsgE($"无法创建UnityWebRequest,URI:{uri}.Exception:{e.Message}");
                LoadFail();
                yield break;
            }

            yield return _request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (_request.result != UnityWebRequest.Result.Success)
#else
            if (_request.isNetworkError || _request.isHttpError)
#endif
            {
                Log.MsgE($"无法对URI:{uri}发起资源包下载请求。Error: {_request.error}");
                LoadFail();
            }
        }

        /// <summary>
        /// * 加载失败
        /// </summary>
        private void LoadFail()
        {
            _request?.Dispose();
            _request = null;
        }

        /// <summary>
        /// * 中断正在进行的下载任务
        /// </summary>
        public void Abort()
        {
            if (!IsFinished)
                _request?.Abort();
        }

        public void Dispose()
        {
            _request?.Dispose();
            _request = null;
        }
    }
}
