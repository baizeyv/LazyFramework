using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lazy.Download;
using Lazy.Manage;
using Lazy.Res.HotUpdate;
using Lazy.Runtime.Utility;
using Lazy.Singleton;
using Lazy.Utility;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Lazy.Res
{
    public class HotUpdateManager : Singleton<HotUpdateManager>, IManager
    {
        /// <summary>
        /// * 分隔符
        /// </summary>
        public const string Separator = "_";

        /// <summary>
        /// * 分包的分割
        /// </summary>
        public const string PackageSplitter = "Package" + Separator;

        /// <summary>
        /// * 远程目录名称
        /// </summary>
        public static string RemoteDirName = "/Remote/" + PathSetting.GetPlatformName();

        /// <summary>
        /// * 热更目录名称
        /// </summary>
        public const string HotUpdateDirName = "/HotUpdate";

        /// <summary>
        /// * 分包目录名称
        /// </summary>
        public const string PackageDirName = "/Package";

        /// <summary>
        /// * 热更资源下载器
        /// </summary>
        private Downloader _hotUpdateDownloader;

        /// <summary>
        /// * 分包下载器
        /// </summary>
        private Downloader _packageDownloader;

        /// <summary>
        /// * 部分热更资源下载器
        /// </summary>
        private Downloader _hotUpdatePartDownloader;

        private HotUpdateManager() { }

        /// <summary>
        /// * 初始化本地版本 AppConfig.LocalVersion
        /// </summary>
        public void InitLocalVersion()
        {
            JsonSerializerSettings settings =
                new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
            if (File.Exists(Application.persistentDataPath + "/" + nameof(AppVersion) + ".json"))
            {
                var json = FileUtility.SafeReadAllText(
                    Application.persistentDataPath + "/" + nameof(AppVersion) + ".json"
                );
                AppConfig.LocalVersion = JsonConvert.DeserializeObject<AppVersion>(json, settings);
            }
            else
            {
                FileUtility.SafeWriteAllText(
                    Application.persistentDataPath + "/" + nameof(AppVersion) + ".json",
                    JsonConvert.SerializeObject(AppConfig.LocalVersion, settings)
                );
            }
        }

        /// <summary>
        /// * 初始化远程版本 AppConfig.RemoteVersion
        /// </summary>
        /// <returns></returns>
        public IEnumerator InitRemoteVersion()
        {
            if (!AppConfig.LocalVersion.EnableHotUpdate && !AppConfig.LocalVersion.EnablePackage)
                // # 没有热更的资源，也没有要分包的资源，直接结束远程版本初始化
                yield break;

            var path = $"{AppConfig.LocalVersion.AssetRemoteAddress}/{nameof(AppVersion)}.json";
            Log.Log.MsgD($"Initialize remote version: {path}");

            var request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Log.Log.MsgE($"获取远程版本失败: {path}, ERROR: {request.error}");
            }
            else
            {
                var text = request.downloadHandler.text;
                var appVersion = JsonConvert.DeserializeObject<AppVersion>(
                    text,
                    Constant.JsonSetting
                );
                AppConfig.RemoteVersion = appVersion;
            }

            request.Dispose();
            request = null;
        }

        /// <summary>
        /// * 初始化资源版本
        /// </summary>
        /// <returns></returns>
        public IEnumerator InitAssetVersion()
        {
            if (!AppConfig.LocalVersion.EnableHotUpdate && !AppConfig.LocalVersion.EnablePackage)
                // # 没有热更的资源，也没有要分包的资源，直接结束远程版本初始化
                yield break;
            var path =
                $"{AppConfig.LocalVersion.AssetRemoteAddress}/HotUpdate{Separator}{nameof(AssetBundleMapping)}.json";
            Log.Log.MsgD($"初始化资源版本: {path}");
            var request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Log.Log.MsgE($"获取资源版本失败: {path}, ERROR: {request.error}");
            }
            else
            {
                var text = request.downloadHandler.text;
                JsonSerializerSettings settings =
                    new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
                var mapping = JsonConvert.DeserializeObject<Dictionary<string, AssetMapping>>(
                    text,
                    settings
                );
                AppConfig.RemoteAssetBundleMapping = mapping;
            }

            request.Dispose();
            request = null;
        }

        /// <summary>
        /// * 游戏资源修复
        /// </summary>
        public void RepairAssetClean()
        {
            FileUtility.SafeClearDir(Application.persistentDataPath + HotUpdateDirName);
            FileUtility.SafeClearDir(Application.persistentDataPath + PackageDirName);
            FileUtility.SafeDeleteFile(
                Application.persistentDataPath + "/" + nameof(AppVersion) + ".json"
            );
            FileUtility.SafeDeleteFile(
                Application.persistentDataPath + "/" + nameof(AssetBundleMapping) + ".json"
            );
        }

        /// <summary>
        /// * 检查指定需要热更的资源
        /// </summary>
        /// <param name="allSize"></param>
        /// <param name="bundles"></param>
        /// <returns></returns>
        public Dictionary<string, string> CheckHotUpdate(out long allSize, params string[] bundles)
        {
            allSize = 0;
            Dictionary<string, string> hotUpdateAssetUrl = new();
            if (!AppConfig.LocalVersion.EnableHotUpdate)
                // # 未开启资源热更
                return hotUpdateAssetUrl;
            if (AppConfig.RemoteAssetBundleMapping.Count <= 0)
                // # 不存在远程热更资源
                return hotUpdateAssetUrl;
            var result = AppConfig.CompareVersion(
                AppConfig.LocalVersion.Version,
                AppConfig.RemoteVersion.Version
            );
            if (result >= 0)
                // # 不需要热更
                return hotUpdateAssetUrl;

            // # 远程的ab包map
            var resAssetBundleMappings = AppConfig.RemoteAssetBundleMapping;
            var assetBundleMappings = AssetBundleMapping.Mappings;
            var specificMappings = resAssetBundleMappings.Where(x =>
                bundles.Contains(x.Value.AssetBundleName)
            );
            foreach (var resAssetMapping in specificMappings)
            {
                // # 尝试获取当前需要热更的资源的 AssetMapping
                assetBundleMappings.TryGetValue(resAssetMapping.Key, out var assetMapping);
                if (assetMapping == null || resAssetMapping.Value.MD5 != assetMapping.MD5)
                {
                    // # 本地没有该资源或本地资源MD5与远程资源的MD5不一致的情况
                    // # MD5不同，新增资源
                    var abPath =
                        $"{resAssetMapping.Value.VersionName}/{PathSetting.AssetBundlesName}/{PathSetting.GetPlatformName()}/{resAssetMapping.Value.AssetBundleName}";
                    var persistentAbPath =
                        Application.persistentDataPath + HotUpdateDirName + Separator + abPath;
                    // # 校验本地热更资源文件MD5
                    if (
                        File.Exists(persistentAbPath)
                        && FileUtility.CreateMD5ForFile(persistentAbPath)
                            == resAssetMapping.Value.MD5
                    )
                        continue;
                    if (
                        AssetBundleMapping.Mappings.TryGetValue(resAssetMapping.Key, out var am)
                        && am != null
                        && am.MD5 == resAssetMapping.Value.MD5
                    )
                        continue;
                    allSize += resAssetMapping.Value.Size;
                    hotUpdateAssetUrl.TryAdd(resAssetMapping.Key, abPath);
                }
                else if (
                    AppConfig.CompareVersion(
                        assetMapping.VersionName,
                        resAssetMapping.Value.VersionName
                    ) < 0
                )
                {
                    // # 版本修正
                    resAssetMapping.Value.VersionName = assetMapping.VersionName;
                }
            }

            return hotUpdateAssetUrl;
        }

        /// <summary>
        /// * 检查全部需要热更的资源
        /// </summary>
        /// <param name="allSize"></param>
        /// <returns></returns>
        public Dictionary<string, string> CheckAllHotUpdate(out long allSize)
        {
            allSize = 0;
            Dictionary<string, string> hotUpdateAssetUrl = new();
            if (!AppConfig.LocalVersion.EnableHotUpdate)
                // # 未开启资源热更
                return hotUpdateAssetUrl;

            if (AppConfig.RemoteAssetBundleMapping.Count <= 0)
                // # 不存在远程热更资源
                return hotUpdateAssetUrl;

            var result = AppConfig.CompareVersion(
                AppConfig.LocalVersion.Version,
                AppConfig.RemoteVersion.Version
            );
            if (result >= 0)
                // # 不需要热更
                return hotUpdateAssetUrl;

            var resAssetBundleMappings = AppConfig.RemoteAssetBundleMapping;
            var assetBundleMappings = AssetBundleMapping.Mappings;
            foreach (var resAssetMapping in resAssetBundleMappings)
            {
                assetBundleMappings.TryGetValue(resAssetMapping.Key, out var assetMapping);
                if (assetMapping == null || resAssetMapping.Value.MD5 != assetMapping.MD5)
                {
                    // # MD5不同，新增资源
                    var abPath =
                        $"{resAssetMapping.Value.VersionName}/{PathSetting.AssetBundlesName}/{PathSetting.GetPlatformName()}/{resAssetMapping.Value.AssetBundleName}";
                    var persistentAbPath =
                        Application.persistentDataPath + HotUpdateDirName + Separator + abPath;
                    // # 校验本地热更资源文件MD5
                    if (
                        File.Exists(persistentAbPath)
                        && FileUtility.CreateMD5ForFile(persistentAbPath)
                            == resAssetMapping.Value.MD5
                    )
                        continue;
                    if (
                        AssetBundleMapping.Mappings.TryGetValue(resAssetMapping.Key, out var am)
                        && am != null
                        && am.MD5 == resAssetMapping.Value.MD5
                    )
                        continue;

                    allSize += resAssetMapping.Value.Size;
                    hotUpdateAssetUrl.TryAdd(resAssetMapping.Key, abPath);
                }
                else if (
                    AppConfig.CompareVersion(
                        assetMapping.VersionName,
                        resAssetMapping.Value.VersionName
                    ) < 0
                )
                {
                    // # 版本修正
                    resAssetMapping.Value.VersionName = assetMapping.VersionName;
                }
            }

            return hotUpdateAssetUrl;
        }

        /// <summary>
        /// * 开始下载指定的部分热更资源,指定内容已放入 hotUpdateAssetUrl 中
        /// </summary>
        /// <param name="hotUpdateAssetUrl"></param>
        /// <param name="onCompleted"></param>
        /// <param name="onFailed"></param>
        /// <param name="overallProgress"></param>
        public void StartHotUpdate(
            Dictionary<string, string> hotUpdateAssetUrl,
            Action onCompleted = null,
            Action onFailed = null,
            Action<float> overallProgress = null
        )
        {
            if (!AppConfig.LocalVersion.EnableHotUpdate)
            {
                // # 没有启用热更新
                onCompleted.Fire();
                return;
            }

            if (hotUpdateAssetUrl.Count <= 0)
            {
                // # 不存在需要热更的资源
                onCompleted.Fire();
                return;
            }

            // # 存在正在下载的,直接取消下载
            _hotUpdatePartDownloader?.CancelDownload();

            _hotUpdatePartDownloader = DownloadManager.Instance.CreateDownloader(
                "hotUpdatePartDownloader"
            );
            _hotUpdatePartDownloader.OnDownloadSuccess += (evt) =>
            {
                Log.Log.MsgD($"获取部分热更资源完成: {evt.DownloadInfo.DownloadURL}");
            };
            _hotUpdatePartDownloader.OnDownloadFailure += (evt) =>
            {
                Log.Log.MsgD(
                    $"获取部分热更资源失败: {evt.DownloadInfo.DownloadURL}\n{evt.ErrorMessage}"
                );
                onFailed.Fire();
            };
            _hotUpdatePartDownloader.OnDownloadStart += (evt) =>
            {
                Log.Log.MsgD($"开始获取部分热更资源: {evt.DownloadInfo.DownloadURL}");
            };
            _hotUpdatePartDownloader.OnDownloadUpdate += (evt) =>
            {
                float currentTaskIndex = evt.CurrentDownloadTaskIndex;
                float taskCount = evt.DownloadTaskCount;
                var progress = currentTaskIndex / taskCount * 100f;
                overallProgress.Fire(progress);
            };
            _hotUpdatePartDownloader.OnDownloadTasksCompleted += (evt) =>
            {
                Log.Log.MsgD($"指定的所有热更资源获取完成, 用时: {evt.TimeSpan}");
                foreach (var assetName in hotUpdateAssetUrl.Keys)
                    if (
                        AppConfig.RemoteAssetBundleMapping.TryGetValue(
                            assetName,
                            out var assetMapping
                        )
                    )
                        if (assetMapping != null)
                        {
                            assetMapping.Updated = true;
                            AssetBundleMapping.Mappings[assetName] = assetMapping;
                        }

                FileUtility.SafeWriteAllText(
                    Application.persistentDataPath + "/" + nameof(AssetBundleMapping) + ".json",
                    JsonConvert.SerializeObject(AssetBundleMapping.Mappings, Constant.JsonSetting)
                );
                if (
                    AssetBundleMapping.Compare(
                        AssetBundleMapping.Mappings,
                        AppConfig.RemoteAssetBundleMapping
                    )
                )
                {
                    AppConfig.LocalVersion.Version = AppConfig.RemoteVersion.Version;
                    AppConfig.LocalVersion.HotUpdateVersions = new List<string>();
                    FileUtility.SafeWriteAllText(
                        Application.persistentDataPath + "/" + nameof(AppVersion) + ".json",
                        JsonConvert.SerializeObject(AppConfig.LocalVersion, Constant.JsonSetting)
                    );
                }

                onCompleted.Fire();
            };

            // # 添加下载清单
            foreach (var assetUrl in hotUpdateAssetUrl.Values)
            {
                var index = assetUrl.IndexOf("/", StringComparison.Ordinal);
                var result = assetUrl.Substring(index + 1);
                _hotUpdatePartDownloader.AddDownload(
                    $"{AppConfig.LocalVersion.AssetRemoteAddress}{HotUpdateDirName}{Separator}{assetUrl}",
                    Application.persistentDataPath + "/HotUpdate/" + result
                );
            }

            _hotUpdatePartDownloader.StartDownload();
        }

        /// <summary>
        /// * 开始下载全部热更资源
        /// </summary>
        /// <param name="hotUpdateAssetUrl"></param>
        /// <param name="onCompleted"></param>
        /// <param name="onFailed"></param>
        /// <param name="overallProgress"></param>
        public void StartAllHotUpdate(
            Dictionary<string, string> hotUpdateAssetUrl,
            Action onCompleted = null,
            Action onFailed = null,
            Action<float> overallProgress = null
        )
        {
            if (!AppConfig.LocalVersion.EnableHotUpdate)
            {
                onCompleted.Fire();
                return;
            }

            if (hotUpdateAssetUrl.Count <= 0)
            {
                onCompleted.Fire();
                return;
            }

            // # 创建资源热更下载器
            _hotUpdateDownloader = DownloadManager.Instance.CreateDownloader("hotUpdateDownloader");
            _hotUpdateDownloader.OnDownloadSuccess += (evt) =>
            {
                Log.Log.MsgD($"获取热更资源完成: {evt.DownloadInfo.DownloadURL}");
            };
            _hotUpdateDownloader.OnDownloadFailure += (evt) =>
            {
                Log.Log.MsgD(
                    $"获取热更资源失败: {evt.DownloadInfo.DownloadURL}\n{evt.ErrorMessage}"
                );
                onFailed.Fire();
            };
            _hotUpdateDownloader.OnDownloadStart += (evt) =>
            {
                Log.Log.MsgD($"开始获取热更资源: {evt.DownloadInfo.DownloadURL}");
            };
            _hotUpdateDownloader.OnDownloadUpdate += (evt) =>
            {
                float currentTaskIndex = evt.CurrentDownloadTaskIndex;
                float taskCount = evt.DownloadTaskCount;
                var progress = currentTaskIndex / taskCount * 100f;
                overallProgress.Fire(progress);
            };
            var jsonSettings = Constant.JsonSetting;
            _hotUpdateDownloader.OnDownloadTasksCompleted += (evt) =>
            {
                Log.Log.MsgD($"所有热更资源获取完成, 用时: {evt.TimeSpan}");
                AppConfig.LocalVersion.Version = AppConfig.RemoteVersion.Version;
                AppConfig.LocalVersion.HotUpdateVersions = new List<string>();
                FileUtility.SafeWriteAllText(
                    Application.persistentDataPath + "/" + nameof(AppVersion) + ".json",
                    JsonConvert.SerializeObject(AppConfig.LocalVersion, jsonSettings)
                );
                foreach (var assetName in hotUpdateAssetUrl.Keys)
                    if (
                        AppConfig.RemoteAssetBundleMapping.TryGetValue(
                            assetName,
                            out var assetMapping
                        )
                    )
                        if (assetMapping != null)
                            assetMapping.Updated = true;

                AssetBundleMapping.Mappings = AppConfig.RemoteAssetBundleMapping;
                FileUtility.SafeWriteAllText(
                    Application.persistentDataPath + "/" + nameof(AssetBundleMapping) + ".json",
                    JsonConvert.SerializeObject(AssetBundleMapping.Mappings, jsonSettings)
                );
                onCompleted.Fire();
            };

            // # 添加下载清单
            foreach (var assetUrl in hotUpdateAssetUrl.Values)
            {
                var index = assetUrl.IndexOf("/", StringComparison.Ordinal);
                var result = assetUrl.Substring(index + 1);
                _hotUpdateDownloader.AddDownload(
                    $"{AppConfig.LocalVersion.AssetRemoteAddress}{HotUpdateDirName}{Separator}{assetUrl}",
                    Application.persistentDataPath + "/HotUpdate/" + result
                );
            }

            // # 开始下载
            _hotUpdateDownloader.StartDownload();
        }

        /// <summary>
        /// * 检查需要下载的分包
        /// </summary>
        /// <returns></returns>
        public List<string> CheckPackageUpdate(List<string> subPackage)
        {
            return subPackage
                .Where(package => AppConfig.LocalVersion.SubPackages.Contains(package))
                .ToList();
        }

        public void StartPackageUpdate(
            List<string> subPackages,
            Action onCompleted = null,
            Action onFailure = null,
            Action<float> overallProgress = null
        )
        {
            if (!AppConfig.LocalVersion.EnablePackage)
            {
                // # 未开启分包
                onCompleted.Fire();
                return;
            }

            if (subPackages.Count <= 0)
            {
                // # 不存在分包
                onCompleted.Fire();
                return;
            }

            List<string> downloadPaths = new();
            // # 创建分包下载器
            _packageDownloader = DownloadManager.Instance.CreateDownloader("PackageDownloader");
            _packageDownloader.OnDownloadSuccess += (evt) =>
            {
                Log.Log.MsgD($"获取分包资源完成: {evt.DownloadInfo.DownloadURL}");
                downloadPaths.Add(evt.DownloadInfo.DownloadPath);
            };
            _packageDownloader.OnDownloadFailure += (evt) =>
            {
                Log.Log.MsgE(
                    $"获取分包资源失败: {evt.DownloadInfo.DownloadURL}\n{evt.ErrorMessage}"
                );
                onFailure.Fire();
            };
            _packageDownloader.OnDownloadStart += (evt) =>
            {
                Log.Log.MsgD($"开始获取分包资源: {evt.DownloadInfo.DownloadURL}");
            };
            _packageDownloader.OnDownloadUpdate += (evt) =>
            {
                float currentTaskIndex = evt.CurrentDownloadTaskIndex;
                float taskCount = evt.DownloadTaskCount;
                var progress = currentTaskIndex / taskCount * 100f;
                overallProgress.Fire(progress);
            };
            _packageDownloader.OnDownloadTasksCompleted += (evt) =>
            {
                Log.Log.MsgD($"所有分包资源获取完成! 用时 {evt.TimeSpan}");
#if UNITY_WEBGL
                CoroutineCenter.StartCoroutine(UnZipPackagePathsCo(downloadPaths, onCompleted));
#else
                _ = UnZipPackagePaths(downloadPaths, onCompleted);
#endif
            };
            // # 添加下载清单
            foreach (var package in subPackages)
            {
                var persistentPackagePath =
                    Application.persistentDataPath + "/" + PackageSplitter + package + ".zip";
                long fileSizeInBytes = 0;
                if (File.Exists(persistentPackagePath))
                {
                    var fileInfo = new FileInfo(persistentPackagePath);
                    fileSizeInBytes = fileInfo.Length;
                }

                // # 断点续传
                _packageDownloader.AddDownload(
                    $"{AppConfig.LocalVersion.AssetRemoteAddress}/{PackageSplitter}{package}.zip",
                    persistentPackagePath,
                    fileSizeInBytes,
                    true
                );
            }

            _packageDownloader.StartDownload();
        }

        public async Task UnZipPackagePaths(List<string> downloadPaths, Action onCompleted = null)
        {
            JsonSerializerSettings jsonSettings =
                new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
            foreach (var downloadPath in downloadPaths)
            {
                await ZipUtility.UnZipFileAsync(
                    downloadPath,
                    Application.persistentDataPath,
                    null,
                    true
                );
                var package = Path.GetFileNameWithoutExtension(downloadPath)
                    .Replace(PackageSplitter, "");
                var subPackageCount = AppConfig.LocalVersion.SubPackages.Count;
                for (var i = subPackageCount - 1; i >= 0; i--)
                    if (AppConfig.LocalVersion.SubPackages[i] == package)
                        AppConfig.LocalVersion.SubPackages.RemoveAt(i);

                FileUtility.SafeWriteAllText(
                    Application.persistentDataPath + "/" + nameof(AppVersion) + ".json",
                    JsonConvert.SerializeObject(AppConfig.LocalVersion, jsonSettings)
                );
            }

            onCompleted.Fire();
        }

        /// <summary>
        /// * 解压分包协程
        /// </summary>
        /// <param name="downloadPaths"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        public IEnumerator UnZipPackagePathsCo(List<string> downloadPaths, Action completed = null)
        {
            JsonSerializerSettings jsonSettings =
                new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
            foreach (var downloadPath in downloadPaths)
            {
                // 使用协程
                yield return ZipUtility.UnZipFileCoroutine(
                    downloadPath,
                    Application.persistentDataPath,
                    null,
                    true
                );
                var package = Path.GetFileNameWithoutExtension(downloadPath)
                    .Replace(PackageSplitter, "");
                var subPackageCount = AppConfig.LocalVersion.SubPackages.Count;
                for (var i = subPackageCount - 1; i >= 0; i--)
                    if (AppConfig.LocalVersion.SubPackages[i] == package)
                        AppConfig.LocalVersion.SubPackages.RemoveAt(i);

                FileUtility.SafeWriteAllText(
                    Application.persistentDataPath + "/" + nameof(AppVersion) + ".json",
                    JsonConvert.SerializeObject(AppConfig.LocalVersion, jsonSettings)
                );
            }

            completed?.Invoke();
        }

        public void Launch(
            Action hotUpdateCompleted = null,
            Action hotUpdateFailed = null,
            Action<float> hotUpdateProgress = null,
            Action packageCompleted = null,
            Action packageFailed = null,
            Action<float> packageProgress = null
        )
        {
            CoroutineCenter.StartCoroutine(
                DoAll(
                    hotUpdateCompleted,
                    hotUpdateFailed,
                    hotUpdateProgress,
                    packageCompleted,
                    packageFailed,
                    packageProgress
                )
            );
        }

        /// <summary>
        /// * 检查所有需要热更新的资源并进行下载
        /// </summary>
        /// <param name="hotUpdateCompleted"></param>
        /// <param name="hotUpdateFailed"></param>
        /// <param name="hotUpdateProgress"></param>
        /// <param name="packageCompleted"></param>
        /// <param name="packageFailed"></param>
        /// <param name="packageProgress"></param>
        /// <returns></returns>
        private IEnumerator DoAll(
            Action hotUpdateCompleted = null,
            Action hotUpdateFailed = null,
            Action<float> hotUpdateProgress = null,
            Action packageCompleted = null,
            Action packageFailed = null,
            Action<float> packageProgress = null
        )
        {
            InitLocalVersion();
            yield return InitRemoteVersion();
            yield return InitAssetVersion();
            // # 检查热更
            var urlDic = CheckAllHotUpdate(out var size);
            StartAllHotUpdate(urlDic, hotUpdateCompleted, hotUpdateFailed, hotUpdateProgress);
            // # 检查分包
            var subPackages = CheckPackageUpdate(AppConfig.LocalVersion.SubPackages);
            StartPackageUpdate(subPackages, packageCompleted, packageFailed, packageProgress);
        }

        /// <summary>
        /// * 热更新指定的一些 AssetBundle
        /// ! 注意依赖管理
        /// </summary>
        /// <param name="bundles"></param>
        /// <param name="hotUpdateCompleted"></param>
        /// <param name="hotUpdateFailed"></param>
        /// <param name="hotUpdateProgress"></param>
        /// <returns></returns>
        private IEnumerator Do(
            Action hotUpdateCompleted = null,
            Action hotUpdateFailed = null,
            Action<float> hotUpdateProgress = null,
            params string[] bundles
        )
        {
            InitLocalVersion();
            yield return InitRemoteVersion();
            yield return InitAssetVersion();
            // # 检查热更
            var urlDic = CheckHotUpdate(out var size, bundles);
            StartHotUpdate(urlDic, hotUpdateCompleted, hotUpdateFailed, hotUpdateProgress);
        }

        public override void OnSingletonInitialize()
        {
            var appVersion = JsonConvert.DeserializeObject<AppVersion>(
                Resources.Load<TextAsset>(nameof(AppVersion)).ToString(),
                Constant.JsonSetting
            );
            AppConfig.LocalVersion = appVersion;
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease()
        {
            _hotUpdateDownloader?.CancelDownload();
            _hotUpdateDownloader = null;
            _packageDownloader?.CancelDownload();
            _packageDownloader = null;
            _hotUpdatePartDownloader?.CancelDownload();
            _hotUpdatePartDownloader = null;
        }

        public void OnGui() { }
    }
}
