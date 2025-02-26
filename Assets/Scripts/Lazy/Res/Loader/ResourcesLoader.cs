using UnityEngine;

namespace Lazy.Res.Loader
{
    public class ResourcesLoader : ABSLoader
    {
        /// <summary>
        /// * Resources 资源路径
        /// </summary>
        private string _resourcePath = "";

        /// <summary>
        /// * 子资源名称
        /// </summary>
        private string subAssetName = "";

        /// <summary>
        /// * 资源对象
        /// </summary>
        private Object _resourceObject;
    }
}