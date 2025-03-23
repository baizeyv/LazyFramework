#if UNITY_EDITOR
using System.IO;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// * 生成代码设置
    /// </summary>
    public class GenCodeSetting : ScriptableObject
    {
        private const string DefaultNameSpace = "Lazy.Melody";
        private const string Dir = "Assets/Lazy/Data/";
        private const string FileName = "GenCodeSetting.asset";

        public string nameSpace = DefaultNameSpace;
        public string scriptDirectory = "Assets/Scripts/CustomComponents";
        public string prefabDirectory = "Assets/Prefabs";

        // # UI代码生成的默认配置
        [Header("UI代码生成的默认配置")]
        public string uiNamespace = DefaultNameSpace;
        public string uiScriptDir = "Scripts/UI";
        public string uiPrefabDir = "Prefabs/UI";

        /// ////////////////////////////////////////

        public bool IsDefaultNameSpace => nameSpace.Equals(DefaultNameSpace);

        private static GenCodeSetting _instance;

        public static GenCodeSetting Instance
        {
            get
            {
                if (_instance)
                    return _instance;
                FileUtility.CheckOrCreateDir(Dir);
                var filePath = Dir + FileName;
                if (File.Exists(filePath))
                    return _instance = AssetDatabase.LoadAssetAtPath<GenCodeSetting>(filePath);
                return _instance = CreateInstance<GenCodeSetting>();
            }
        }

        public void Save()
        {
            var filePath = Dir + FileName;
            if (!File.Exists(filePath))
                AssetDatabase.CreateAsset(this, filePath);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
