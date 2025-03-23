using System.Collections.Generic;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
namespace AutoReloadDemo
{
    interface Reloadable
    {
        void Reload();
        void ReloadAsync();
    }

    public class ExcelLoader<T> : Reloadable where T : class
    {
        public bool showDebugLog = true;
        public Dictionary<string, T> ExcelDataDic { get; private set; } = new Dictionary<string, T>();
        FileChangeDetector fileChangeDetector;
        string keyFieldName;
        
        public ExcelLoader(string path, string keyFieldName = "Name", bool showDebugLog = false)
        {
            fileChangeDetector = new FileChangeDetector(path);
            this.keyFieldName = keyFieldName;
            this.showDebugLog = showDebugLog;
            Reload();
        }

        public void Reload()
        {
            ExcelRuntimeTools.ExcelLoader.LoadToDictionary(ExcelDataDic, fileChangeDetector.bytes, keyFieldName: keyFieldName);
            if(showDebugLog)
            {
                Debug.Log($"加载完毕，共{ExcelDataDic.Count}条数据");
                foreach (var item in ExcelDataDic)
                {
                    Debug.Log(item);
                }
            }
        }

        public async void ReloadAsync()
        {

            Dictionary<string, T> temp = new Dictionary<string, T>();
            var isFileChanged = false;
            await Task.Run(() =>
            {
                isFileChanged = fileChangeDetector.Detect();
                if (isFileChanged)
                    ExcelRuntimeTools.ExcelLoader.LoadToDictionary(temp, fileChangeDetector.bytes, keyFieldName: keyFieldName);
            });
            if (isFileChanged)
            {
                ExcelDataDic = temp;
                if (showDebugLog)
                {
                    Debug.Log($"检测到数据变动，重新加载，共{ExcelDataDic.Count}条数据");
                    foreach (var item in ExcelDataDic)
                    {
                        Debug.Log(item);
                    }
                }
            }
        }
    }
}