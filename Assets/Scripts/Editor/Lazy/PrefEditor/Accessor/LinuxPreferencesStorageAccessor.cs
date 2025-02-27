using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Editor.Lazy.PrefEditor
{
    public class LinuxPreferencesStorageAccessor : ABSPreferencesStorageAccessor
    {
        private FileSystemWatcher _fileWatcher;

        public LinuxPreferencesStorageAccessor(string pathToPrefs) : base(Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/home", pathToPrefs))
        {
            _fileWatcher = new FileSystemWatcher();
            _fileWatcher.Path = Path.GetDirectoryName(prefPath);
            _fileWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
            _fileWatcher.Filter = "prefs";
            _fileWatcher.Changed += OnWatchedFileChanged;
        }

        protected override void FetchKeysFromSystem()
        {
            cachedData = Array.Empty<string>();
            if (File.Exists(prefPath))
            {
                XmlReaderSettings settings = new();
                XmlReader reader = XmlReader.Create(prefPath, settings);
                XDocument doc = XDocument.Load(reader);
                cachedData = doc.Element("unity_prefs").Elements().Select(e => e.Attribute("name").Value).ToArray();
            }
        }

        public override void StartMonitoring()
        {
            _fileWatcher.EnableRaisingEvents = true;
        }

        public override void StopMonitoring()
        {
            _fileWatcher.EnableRaisingEvents = false;
        }

        public override bool IsMonitoring()
        {
            return _fileWatcher.EnableRaisingEvents;
        }

        private void OnWatchedFileChanged(object source, FileSystemEventArgs e)
        {
            OnPrefEntryChanged();
        }
    }
}