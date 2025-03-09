using System;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Editor.Lazy.PrefEditor
{
    public class WindowsPreferencesStorageAccessor : ABSPreferencesStorageAccessor
    {
        /// <summary>
        /// * Windows 注册表监视器
        /// </summary>
        private WindowsRegistryMonitor _monitor;

        public WindowsPreferencesStorageAccessor(string pathToPrefs) : base(pathToPrefs)
        {
            _monitor = new(RegistryHive.CurrentUser, prefPath);
            // # 监听注册表改变事件
            _monitor.RegChanged += OnRegChanged;
        }

        private void OnRegChanged(object sender, EventArgs e)
        {
            OnPrefEntryChanged();
        }

        protected override void FetchKeysFromSystem()
        {
            cachedData = new string[0];

            using (RegistryKey rootKey = Registry.CurrentUser.OpenSubKey(prefPath))
            {
                if (rootKey != null)
                {
                    cachedData = rootKey.GetValueNames();
                    rootKey.Close();
                }
            }
            // Clean <key>_h3320113488 nameing
            cachedData = cachedData.Select(key => key.Substring(0, key.LastIndexOf("_h", StringComparison.Ordinal))).ToArray();

            EncodeAnsiInPlace();
        }

        private void EncodeAnsiInPlace()
        {
            var utf8 = Encoding.UTF8;
            var ansi = Encoding.GetEncoding(1252);

            for (var i = 0; i < cachedData.Length; i++)
            {
                cachedData[i] = utf8.GetString(ansi.GetBytes(cachedData[i]));
            }
        }

        public override void StartMonitoring()
        {
            _monitor.Start();
        }

        public override void StopMonitoring()
        {
            _monitor.Stop();
        }

        public override bool IsMonitoring()
        {
            return _monitor.IsMonitoring;
        }
        // TODO:
    }
}