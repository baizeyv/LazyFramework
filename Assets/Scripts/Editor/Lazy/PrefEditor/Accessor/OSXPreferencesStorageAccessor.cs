using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Editor.Lazy.PrefEditor
{
    public class OSXPreferencesStorageAccessor : ABSPreferencesStorageAccessor
    {
        private FileSystemWatcher _fileWatcher;

        private DirectoryInfo _prefsDirInfo;

        private string _prefsFileNameWithoutExtension;

        public OSXPreferencesStorageAccessor(string pathToPrefs) : base(Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/home", pathToPrefs))
        {
            _prefsDirInfo = new DirectoryInfo(Path.GetDirectoryName(prefPath));
            _prefsFileNameWithoutExtension = Path.GetFileNameWithoutExtension(prefPath);

            _fileWatcher = new FileSystemWatcher();
            _fileWatcher.Path = Path.GetDirectoryName(prefPath);
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileWatcher.Filter = Path.GetFileName(prefPath);

            // MAC delete the old and create a new file instead of updating
            _fileWatcher.Created += OnWatchedFileChanged;
        }

        protected override void FetchKeysFromSystem()
        {
            // Workaround to avoid incomplete tmp phase from MAC OS
            foreach (FileInfo info in _prefsDirInfo.GetFiles())
            {
                // Check if tmp PlayerPrefs file exist
                if (info.FullName.Contains(_prefsFileNameWithoutExtension) && !info.FullName.EndsWith(".plist"))
                {
                    onStartLoading?.Invoke();
                    return;
                }
            }
            onStopLoading?.Invoke();

            cachedData = new string[0];

            if (File.Exists(prefPath))
            {
                string fixedPrefsPath = prefPath.Replace("\"", "\\\"").Replace("'", "\\'").Replace("`", "\\`");
                var cmdStr = string.Format(@"-p '{0}'", fixedPrefsPath);

                string stdOut = String.Empty;
                string errOut = String.Empty;

                var process = new System.Diagnostics.Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "plutil";
                process.StartInfo.Arguments = cmdStr;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (_, evt) => { stdOut += evt.Data + "\n"; };
                process.ErrorDataReceived += (_, evt) => { errOut += evt.Data + "\n"; };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                MatchCollection matches = Regex.Matches(stdOut, @"(?: "")(.*)(?:"" =>.*)");
                cachedData = matches.Select((e) => e.Groups[1].Value).ToArray();
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