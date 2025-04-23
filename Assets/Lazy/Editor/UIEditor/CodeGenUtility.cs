using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lazy;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Editor.UIEditor
{
    public static class CodeGenUtility
    {
        [MenuItem("Assets/Generate LazyUI Panel Code(Alt+3) &3")]
        public static void GenerateUIPanelCode()
        {
            Map.Clear();
            var objs = Selection.GetFiltered(
                typeof(GameObject),
                SelectionMode.Assets | SelectionMode.TopLevel
            );
            foreach (var item in objs)
            {
                DoUI(item, AssetDatabase.GetAssetPath(item), false, true);
                StartAddComponent2PrefabAfterCompile(item as GameObject);
            }

            EditorPrefs.SetString(AfterMethodKey, "UIPanel");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Generate LazyUI Dialog Code(Alt+4) &4")]
        public static void GenerateUIDialogCode()
        {
            Map.Clear();
            var objs = Selection.GetFiltered(
                typeof(GameObject),
                SelectionMode.Assets | SelectionMode.TopLevel
            );
            foreach (var item in objs)
            {
                DoUI(item, AssetDatabase.GetAssetPath(item), true, true);
                StartAddComponent2PrefabAfterCompile(item as GameObject);
            }

            EditorPrefs.SetString(AfterMethodKey, "UIDialog");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// * 编译后执行
        /// </summary>
        [DidReloadScripts]
        private static void DoAddComponent2Prefab()
        {
            var pathStr = EditorPrefs.GetString(AutoGenKey);
            if (string.IsNullOrEmpty(pathStr))
                return;

            EditorPrefs.DeleteKey(AutoGenKey);
            Debug.Log(">>>>>>>SerializeUIPrefab: " + pathStr);

            var afterMethodType = EditorPrefs.GetString(AfterMethodKey);
            if (string.IsNullOrEmpty(afterMethodType))
                return;
            EditorPrefs.DeleteKey(AfterMethodKey);

            // # 重新生成一次Map, 在编译后会被清空
            var objs = Selection.GetFiltered(
                typeof(GameObject),
                SelectionMode.Assets | SelectionMode.TopLevel
            );
            foreach (var item in objs)
                DoUI(
                    item,
                    AssetDatabase.GetAssetPath(item),
                    !afterMethodType.Equals("UIPanel"),
                    false
                );

            var assembly = GetAssemblyCSharp();
            var paths = pathStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var displayProgress = paths.Length > 3;
            if (displayProgress)
                EditorUtility.DisplayProgressBar("", "Serialize UIPrefab...", 0);

            for (var i = 0; i < paths.Length; i++)
            {
                var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                var map = Map[paths[i]];
                SetObjectRef2Property(uiPrefab, uiPrefab.name, assembly, map.RootBindNodes);

                foreach (var x in map.WidgetBindNodes)
                    SetObjectRef2Property(x.Key.GameObject, x.Key.TypeName, assembly, x.Value);

                // uibehaviour
                if (displayProgress)
                    EditorUtility.DisplayProgressBar(
                        "",
                        "Serialize UIPrefab..." + uiPrefab.name,
                        (float)(i + 1) / paths.Length
                    );
                Debug.Log(">>>>>>>Success Serialize UIPrefab: " + uiPrefab.name);
            }

            if (displayProgress)
                EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// * 设置脚本上的对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="behaviourName"></param>
        /// <param name="assembly"></param>
        /// <param name="marks"></param>
        private static void SetObjectRef2Property(
            GameObject obj,
            string behaviourName,
            Assembly assembly,
            List<BindNode> marks
        )
        {
            var iBind = obj.GetComponent<ABSBind>();
            var className = string.Empty;
            if (iBind != null)
                className = GenCodeSetting.Instance.uiNamespace + "." + iBind.TypeName;
            else
                className = GenCodeSetting.Instance.uiNamespace + "." + behaviourName;

            var tp = assembly.GetType(className);
            var com = obj.GetOrAddComponent(tp);
            var sObj = new SerializedObject(com);
            foreach (var mark in marks)
            {
                var cpt = mark.GameObject.GetComponent(mark.TypeName);
                if (cpt == null)
                {
                    var tmpType = assembly.GetType(
                        GenCodeSetting.Instance.uiNamespace + "." + mark.TypeName
                    );
                    mark.GameObject.AddComponent(tmpType);
                }

                sObj.FindProperty(mark.PropertyName).objectReferenceValue = mark.GameObject;
            }

            sObj.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// * 获取Assembly
        /// </summary>
        /// <returns></returns>
        public static Assembly GetAssemblyCSharp()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in assemblies)
                if (a.FullName.StartsWith("Assembly-CSharp,"))
                    return a;

            //            Log.E(">>>>>>>Error: Can\'t find Assembly-CSharp.dll");
            return null;
        }

        private const string AutoGenKey = "AutoGenerateUIPrefabPath";

        private const string AfterMethodKey = "AutoAfterMethod";

        /// <summary>
        /// * 准备添加新生成的脚本
        /// </summary>
        /// <param name="prefab"></param>
        private static void StartAddComponent2PrefabAfterCompile(GameObject prefab)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
                return;

            var pathStr = EditorPrefs.GetString(AutoGenKey);
            if (string.IsNullOrEmpty(pathStr))
                pathStr = prefabPath;
            else
                pathStr += ";" + prefabPath;

            EditorPrefs.SetString(AutoGenKey, pathStr);
        }

        private static readonly Dictionary<string, DataMap> Map = new();

        private static void DoUI(Object obj, string uiPrefabPath, bool isDialog, bool generation)
        {
            if (obj != null)
            {
#pragma warning disable CS0618
                var prefabType = PrefabUtility.GetPrefabType(obj);
#pragma warning restore CS0618
#pragma warning disable CS0618
                if (PrefabType.Prefab != prefabType)
#pragma warning restore CS0618
                    return;

                var prefab = obj as GameObject;

                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                Map.Add(prefabPath, new DataMap());
                var map = Map[prefabPath];

                var binds = prefab?.GetComponentsInChildren<ABSBind>();
                if (binds != null)
                    foreach (var bind in binds)
                    {
                        var parentBind = GetUIParentBind(bind.Transform, out var except);
                        if (except)
                            continue;
                        if (parentBind == null)
                        {
                            map.RootBinds.Add(bind);
                        }
                        else
                        {
                            if (map.WidgetBinds.TryGetValue(parentBind, out var widgetBind))
                                widgetBind.Add(bind);
                            else
                                map.WidgetBinds.Add(parentBind, new List<ABSBind> { bind });
                        }
                    }

                foreach (
                    var node in map.RootBinds.Select(item => new BindNode
                    {
                        Comment = item.Comment,
                        TypeName = item.TypeName,
                        GameObject = item.Transform.gameObject,
                        PropertyName = GetPropertyName(
                            item.Transform.gameObject.name,
                            map._propertyNameMap
                        ),
                        Bind = item,
                    })
                )
                    map.RootBindNodes.Add(node);

                map._propertyNameMap.Clear();

                foreach (var item in map.WidgetBinds)
                {
                    var keyNode = new BindNode
                    {
                        Bind = item.Key,
                        TypeName = item.Key.TypeName,
                        Comment = item.Key.Comment,
                        GameObject = item.Key.Transform.gameObject,
                    };
                    map.WidgetBindNodes.Add(keyNode, new List<BindNode>());
                    foreach (
                        var childNode in item.Value.Select(x => new BindNode
                        {
                            Comment = x.Comment,
                            TypeName = x.TypeName,
                            GameObject = x.Transform.gameObject,
                            PropertyName = GetPropertyName(
                                x.Transform.gameObject.name,
                                map._propertyNameMap
                            ),
                            Bind = x,
                        })
                    )
                        map.WidgetBindNodes[keyNode].Add(childNode);

                    map._propertyNameMap.Clear();
                }

                if (generation)
                {
                    var srcPath = GenSourceFilePathFromPrefabPath(uiPrefabPath, obj.name);
                    // # 生成 CodeGenInfo
                    var rootCodeGenInfo = new CodeGenInfo
                    {
                        ClassName = obj.name,
                        IsDialog = isDialog,
                        Namespace = GenCodeSetting.Instance.uiNamespace,
                        Properties = map.RootBindNodes,
                        ScriptFilePath = srcPath,
                    };
                    CodeGenTemplate.UIPanelTemplate.Generate(rootCodeGenInfo);
                    var dir = srcPath.Replace(obj.name + ".cs", "");
                    foreach (var item in map.WidgetBindNodes)
                    {
                        var bind = item.Key.Bind;
                        var suffix = GetWidgetPath(obj.name, bind);
                        var info = new CodeGenInfo
                        {
                            ClassName = bind.TypeName,
                            IsDialog = isDialog,
                            Namespace = GenCodeSetting.Instance.uiNamespace,
                            Properties = item.Value,
                            ScriptFilePath = $"{dir}{suffix}{bind.TypeName}.cs",
                        };
                        CodeGenTemplate.UIWidgetTemplate.Generate(info);
                    }
                }
            }
        }

        private static string GetWidgetPath(string rootName, ABSBind bind)
        {
            var transform = bind.Transform;
            var ret = "";
            while (transform.parent != null)
            {
                var bd = transform.parent.GetComponent<ABSBind>();
                if (bd != null)
                    if (bind.BindType == BindType.UIWidget)
                        ret = $"{bd.TypeName}/{ret}";

                transform = transform.parent;
            }

            ret = $"{rootName}/{ret}";
            return ret;
        }

        private static ABSBind GetUIParentBind(Transform transform, out bool except)
        {
            while (transform.parent != null)
            {
                if (transform.parent.GetComponent<IBindGroup>() != null)
                {
                    except = true;
                    return null;
                }

                var bind = transform.parent.GetComponent<ABSBind>();
                if (bind != null)
                    if (bind.BindType == BindType.UIWidget)
                    {
                        // # 父级中的UIWidget
                        except = false;
                        return bind;
                    }

                transform = transform.parent;
            }

            except = false;
            return null;
        }

        /// <summary>
        /// * 生成Panel Dialog主代码文件完整路径
        /// </summary>
        /// <param name="uiPrefabPath"></param>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        private static string GenSourceFilePathFromPrefabPath(
            string uiPrefabPath,
            string prefabName
        )
        {
            var strFilePath = string.Empty;

            var prefabDirPattern = GenCodeSetting.Instance.uiPrefabDir;

            if (uiPrefabPath.Contains(prefabDirPattern))
                strFilePath = uiPrefabPath.Replace(
                    prefabDirPattern,
                    GenCodeSetting.Instance.uiScriptDir
                );
            else if (uiPrefabPath.Contains("/Resources"))
                strFilePath = uiPrefabPath.Replace(
                    "/Resources",
                    GenCodeSetting.Instance.uiScriptDir
                );
            else
                strFilePath = uiPrefabPath.Replace(
                    "/" + GetLastDirName(uiPrefabPath),
                    GenCodeSetting.Instance.uiScriptDir
                );

            var str = strFilePath.Replace(prefabName + ".prefab", string.Empty);
            FileUtility.CheckOrCreateDir(str);

            strFilePath = strFilePath.Replace(".prefab", ".cs");

            return strFilePath;
        }

        private static string GetLastDirName(string absOrAssetsPath)
        {
            var name = absOrAssetsPath.Replace("\\", "/");
            var dirs = name.Split('/');

            return dirs[dirs.Length - 2];
        }

        public static string GetPropertyName(string name, Dictionary<string, int> _propertyNameMap)
        {
            string newName;
            if (_propertyNameMap.ContainsKey(name))
            {
                var cnt = _propertyNameMap[name];
                var tmp = GetNewName(name, ref cnt);
                while (_propertyNameMap.ContainsKey(tmp))
                    tmp = GetNewName(name, ref cnt);

                _propertyNameMap.Add(tmp, 1);
                newName = ToPascalCase(tmp);
            }
            else
            {
                _propertyNameMap.Add(name, 1);
                newName = ToPascalCase(name);
            }

            return "m" + newName;
        }

        private static string GetNewName(string source, ref int cnt)
        {
            source += $"-{NumberToLetter(cnt)}";
            cnt++;
            return source;
        }

        private static string NumberToLetter(int number)
        {
            var result = "";
            while (number > 0)
            {
                number--; // 调整为 0-based 索引
                result = (char)('A' + number % 26) + result;
                number /= 26;
            }

            return result;
        }

        private static string ToPascalCase(string str)
        {
            var newStr = string.Concat(
                Regex
                    .Split(str, @"^a-zA-Z0-9")
                    .Where(w => w.Length > 0)
                    .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower())
            );
            return Regex.Replace(newStr, @"[^a-zA-Z0-9]", "");
        }
    }

    public class CodeGenInfo
    {
        public string ScriptFilePath;

        public string ClassName;

        public string Namespace;

        public List<BindNode> Properties;

        public bool IsDialog;
    }

    /// <summary>
    /// * 绑定的属性节点
    /// </summary>
    public class BindNode
    {
        /// <summary>
        /// * 注释
        /// </summary>
        public string Comment;

        /// <summary>
        /// * 属性类型
        /// </summary>
        public string TypeName;

        /// <summary>
        /// * 属性名称
        /// </summary>
        public string PropertyName;

        /// <summary>
        /// * 要绑定的内容GameObject
        /// </summary>
        public GameObject GameObject;

        public ABSBind Bind;
    }

    [Serializable]
    public class DataMap
    {
        /// <summary>
        /// * 根的绑定
        /// </summary>
        public List<ABSBind> RootBinds = new();

        /// <summary>
        /// * 组件的绑定
        /// </summary>
        public Dictionary<ABSBind, List<ABSBind>> WidgetBinds = new();

        public List<BindNode> RootBindNodes = new();

        public Dictionary<BindNode, List<BindNode>> WidgetBindNodes = new();

        public Dictionary<string, int> _propertyNameMap = new();
    }
}
