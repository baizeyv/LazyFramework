using System;

namespace Editor.Lazy.PrefEditor
{
    /// <summary>
    /// * 本地存储访问器
    /// </summary>
    public abstract class ABSPreferencesStorageAccessor
    {
        /// <summary>
        /// * 本地存储路径
        /// </summary>
        protected string prefPath;

        /// <summary>
        /// * 缓存数据
        /// </summary>
        protected string[] cachedData = new string[0];

        /// <summary>
        /// * 存储Entry改变事件
        /// </summary>
        public event Action onPrefEntryChanged;

        public Action onStartLoading;

        public Action onStopLoading;

        protected bool ignoreNextChange = false;

        /// <summary>
        /// * 获取数据Key值
        /// </summary>
        protected abstract void FetchKeysFromSystem();

        /// <summary>
        /// * Constructor
        /// </summary>
        /// <param name="pathToPrefs"></param>
        protected ABSPreferencesStorageAccessor(string pathToPrefs)
        {
            prefPath = pathToPrefs;
        }

        /// <summary>
        /// * 获取键值数组
        /// </summary>
        /// <param name="reloadData"></param>
        /// <returns></returns>
        public string[] GetKeys(bool reloadData = true)
        {
            if (reloadData || cachedData.Length == 0)
            {
                FetchKeysFromSystem();
            }

            return cachedData;
        }

        public void IgnoreNextChange()
        {
            ignoreNextChange = true;
        }

        protected virtual void OnPrefEntryChanged()
        {
            if (ignoreNextChange)
            {
                ignoreNextChange = false;
                return;
            }
            onPrefEntryChanged?.Invoke();
        }

        /// <summary>
        /// * 开始监视
        /// </summary>
        public abstract void StartMonitoring();

        /// <summary>
        /// * 停止监视
        /// </summary>
        public abstract void StopMonitoring();

        /// <summary>
        /// * 是否正在监视
        /// </summary>
        /// <returns></returns>
        public abstract bool IsMonitoring();

    }
}