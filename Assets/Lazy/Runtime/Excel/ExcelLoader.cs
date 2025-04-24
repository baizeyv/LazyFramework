#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Lazy
{
    internal interface Reloadable
    {
        void Reload();
        void ReloadAsync();
    }

    public class ExcelLoader<T> : Reloadable
        where T : class
    {
        public bool showDebugLog = true;
        public Dictionary<string, T> ExcelDataDic { get; private set; } = new();

        private FileChangeDetector fileChangeDetector;
        private string keyFieldName;

        public ExcelLoader(string path, string keyFieldName = "Name", bool showDebugLog = false)
        {
            fileChangeDetector = new FileChangeDetector(path);
            this.keyFieldName = keyFieldName;
            this.showDebugLog = showDebugLog;
            Reload();
        }

        public void Reload()
        {
            ExcelParser.LoadToDictionary(
                ExcelDataDic,
                fileChangeDetector.Bytes,
                keyFieldName: keyFieldName
            );
            if (showDebugLog)
            {
                Debug.Log($"??????????{ExcelDataDic.Count}??????");
                foreach (var item in ExcelDataDic)
                    Debug.Log(item);
            }
        }

        public async void ReloadAsync()
        {
            var temp = new Dictionary<string, T>();
            var isFileChanged = false;
            await Task.Run(() =>
            {
                isFileChanged = fileChangeDetector.Detect();
                if (isFileChanged)
                    ExcelParser.LoadToDictionary(
                        temp,
                        fileChangeDetector.Bytes,
                        keyFieldName: keyFieldName
                    );
            });
            if (isFileChanged)
            {
                ExcelDataDic = temp;
                if (showDebugLog)
                {
                    Debug.Log($"????????????????????{ExcelDataDic.Count}??????");
                    foreach (var item in ExcelDataDic)
                        Debug.Log(item);
                }
            }
        }
    }
}
#endif
