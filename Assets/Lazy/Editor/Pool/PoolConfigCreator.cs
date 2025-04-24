using Lazy;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.Pool
{
    public class PoolConfigCreator
    {
        [MenuItem("Assets/Lazy/Create Pool Config", false, 1024)]
        public static void CreatePoolConfig()
        {
            var config = ScriptableObject.CreateInstance<PoolsConfig>();
            ProjectWindowUtil.CreateAsset(config, "PoolsConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
