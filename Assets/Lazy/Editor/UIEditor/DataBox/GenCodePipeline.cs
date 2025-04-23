using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lazy;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Lazy.Editor.UIEditor
{
    /// <summary>
    /// * 代码生成管道
    /// </summary>
    public class GenCodePipeline : ScriptableObject
    {
        private const string Dir = "Assets/Lazy/Data/";
        private const string FileName = "GenCodePipeline.asset";
        private static GenCodePipeline _instance;

        /// <summary>
        /// * 当前代码生成任务
        /// </summary>
        [SerializeField]
        private GenCodeTask currentTask;

        public static GenCodePipeline Instance
        {
            get
            {
                if (_instance)
                    return _instance;
                const string filePath = Dir + FileName;
                if (File.Exists(filePath))
                    return _instance = AssetDatabase.LoadAssetAtPath<GenCodePipeline>(filePath);

                return _instance = CreateInstance<GenCodePipeline>();
            }
        }

        public void Generate(GenCodeTask task)
        {
            currentTask = task;

            currentTask.status = GenCodeTaskStatus.Search;
            var propertyNameMap = new Dictionary<string, int>();
            BindSearchHelper.Search(task, propertyNameMap);
            currentTask.status = GenCodeTaskStatus.Generating;
            var viewPresenter = task.gameObject.GetComponent<ViewPresenter>();

            var writer = TemplateUtility.GetScriptTemplateString("ViewPresenterTemplate.cs.txt");

            // # 替换命名空间
            writer = writer.Replace(
                "#NAMESPACE#",
                string.IsNullOrEmpty(task.nameSpace)
                    ? GenCodeSetting.Instance.nameSpace
                    : task.nameSpace
            );
            writer = writer.Replace("#CLASSNAME#", task.className);
            if (!string.IsNullOrEmpty(viewPresenter.viewPresenterFullTypeName))
                writer = writer.Replace("#INHERIT#", viewPresenter.viewPresenterFullTypeName);
            else
                writer = writer.Replace("#INHERIT#", "ViewPresenter");

            writer = writer.Replace("#TIME#", DateTimeOffset.Now.ToLocalTime().ToString("F"));
            writer = writer.Replace("#GUID#", Guid.NewGuid().ToString());

            task.mainCode = writer;

            var designerWriter = TemplateUtility.GetScriptTemplateString(
                "ViewPresenterTemplate.designer.cs.txt"
            );
            // # 替换命名空间
            designerWriter = designerWriter.Replace(
                "#NAMESPACE#",
                string.IsNullOrEmpty(task.nameSpace)
                    ? GenCodeSetting.Instance.nameSpace
                    : task.nameSpace
            );
            designerWriter = designerWriter.Replace("#CLASSNAME#", task.className);
            if (!string.IsNullOrEmpty(viewPresenter.viewPresenterFullTypeName))
                designerWriter = designerWriter.Replace("#INHERIT#", ": Lazy.IPresenter");
            else
                designerWriter = designerWriter.Replace("#INHERIT#", "");

            designerWriter = designerWriter.Replace(
                "#TIME#",
                DateTimeOffset.Now.ToLocalTime().ToString("F")
            );
            designerWriter = designerWriter.Replace("#GUID#", Guid.NewGuid().ToString());

            StringBuilder sb = new();
            foreach (var bindData in task.bindInfos)
            {
                if (!string.IsNullOrEmpty(bindData.bindScript.Comment))
                {
                    sb.AppendLine("\t\t/// <summary>");
                    foreach (var comment in bindData.bindScript.Comment.Split('\n'))
                        sb.AppendLine($"\t\t/// {comment}");
                    sb.AppendLine("\t\t/// </summary>");
                }

                sb.AppendLine($"\t\tpublic {bindData.typeName} {bindData.memberName};");
            }

            if (task.gameObject.GetComponent<ExtraBinds>())
            {
                var referenceBinds = task.gameObject.GetComponent<ExtraBinds>();
                foreach (var referenceBind in referenceBinds.binds)
                {
                    var newName = CodeGenUtility.GetPropertyName(
                        referenceBind.memberName,
                        propertyNameMap
                    );
                    referenceBind.propertyName = newName;
                    sb.AppendLine();
                    sb.AppendLine($"\t\tpublic {referenceBind.obj.GetType().FullName} {newName};");
                }
            }

            sb.AppendLine();

            if (!string.IsNullOrEmpty(viewPresenter.appFullTypeName))
                sb.AppendLine($"\t\tpublic Lazy.IApp App => {viewPresenter.appFullTypeName}.Gate;");

            designerWriter = designerWriter.Replace("#FIELD#", sb.ToString());
            task.designerCode = designerWriter;
            sb.Clear();

            var scriptFile = string.Format($"{task.scriptsFolder}/{task.className}.cs");
            if (!File.Exists(scriptFile))
            {
                var folder = Path.GetDirectoryName(scriptFile);
                FileUtility.CheckOrCreateDir(folder);
                File.WriteAllText(scriptFile, currentTask.mainCode);
            }

            var designerFile = string.Format($"{task.scriptsFolder}/{task.className}.Designer.cs");
            File.WriteAllText(designerFile, currentTask.designerCode);

            Save();
            currentTask.status = GenCodeTaskStatus.Compile;
        }

        private void Save()
        {
            const string filePath = Dir + FileName;
            if (!File.Exists(filePath))
                AssetDatabase.CreateAsset(this, filePath);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnCompile()
        {
            if (currentTask == null)
                return;
            if (currentTask.status == GenCodeTaskStatus.Compile)
            {
                var generateClassName = currentTask.className;
                var generateNamespace = currentTask.nameSpace;
                var assemblies = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .Where(x => !x.FullName.StartsWith("Unity"));
                var typeName = generateNamespace + "." + generateClassName;
                var type = assemblies
                    .Where(x => x.GetType(typeName) != null)
                    .Select(x => x.GetType(typeName))
                    .FirstOrDefault();
                if (type == null)
                {
                    Log.Log.MsgE("Compile ERROR !");
                    return;
                }

                Log.Log.MsgI($"Compile {type}");
                var gameObject = currentTask.gameObject;
                var scriptComponent = gameObject.GetComponent(type);
                if (!scriptComponent)
                    scriptComponent = gameObject.AddComponent(type);

                var serializedObject = new SerializedObject(scriptComponent);
                foreach (var bindInfo in currentTask.bindInfos)
                {
                    var componentName = bindInfo.typeName.Split('.').Last();
                    var serializedProperty = serializedObject.FindProperty(bindInfo.memberName);
                    var component = gameObject
                        .transform.Find(bindInfo.pathToRoot)
                        .GetComponent(componentName);

                    if (!component)
                        component = gameObject
                            .transform.Find(bindInfo.pathToRoot)
                            .GetComponent(bindInfo.typeName);

                    serializedProperty.objectReferenceValue = component;
                }

                var referenceBinds = gameObject.GetComponent<ExtraBinds>();
                if (referenceBinds)
                    foreach (var bind in referenceBinds.binds)
                    {
                        var serializedProperty = serializedObject.FindProperty(bind.propertyName);
                        serializedProperty.objectReferenceValue = bind.obj;
                    }

                var codeGenerateInfo = gameObject.GetComponent<ViewPresenter>();

                if (codeGenerateInfo)
                {
                    serializedObject.FindProperty("scriptsFolder").stringValue =
                        codeGenerateInfo.scriptsFolder;
                    serializedObject.FindProperty("prefabFolder").stringValue =
                        codeGenerateInfo.prefabFolder;
                    serializedObject.FindProperty("generatePrefab").boolValue =
                        codeGenerateInfo.generatePrefab;
                    serializedObject.FindProperty("scriptName").stringValue =
                        codeGenerateInfo.scriptName;
                    serializedObject.FindProperty("nameSpace").stringValue =
                        codeGenerateInfo.nameSpace;
                    serializedObject.FindProperty("appFullTypeName").stringValue =
                        codeGenerateInfo.appFullTypeName;

                    var generatePrefab = codeGenerateInfo.generatePrefab;
                    var prefabFolder = codeGenerateInfo.prefabFolder;

                    if (codeGenerateInfo.GetType() != type)
                        DestroyImmediate(codeGenerateInfo, true);

                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();

                    if (generatePrefab)
                    {
                        FileUtility.CheckOrCreateDir(prefabFolder);

                        var generatePrefabPath = prefabFolder + "/" + gameObject.name + ".prefab";

                        if (File.Exists(generatePrefabPath))
                        {
                            // PrefabUtility.SavePrefabAsset(gameObject);
                        }
                        else
                        {
                            PrefabUtility.SaveAsPrefabAssetAndConnect(
                                gameObject,
                                generatePrefabPath,
                                InteractionMode.AutomatedAction
                            );
                        }
                    }
                }
                else
                {
                    serializedObject.FindProperty("ScriptsFolder").stringValue = "Assets/Scripts";
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.UpdateIfRequiredOrScript();
                }

                EditorUtility.SetDirty(gameObject);

                // EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                currentTask.status = GenCodeTaskStatus.Complete;
                currentTask = null;
            }
        }

        [DidReloadScripts]
        private static void Compile()
        {
            Instance.OnCompile();
        }
    }
}
