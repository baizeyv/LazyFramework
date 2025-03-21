using System;
using System.Collections.Generic;
using Lazy.UI.Basic;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.UIEditor
{
    /// <summary>
    /// * 代码生成工具
    /// </summary>
    public class GenCodeUtility
    {
        private static readonly Dictionary<string, IGenCodeTemplate> Templates = new();

        public static void RegisterTemplate(string templateName, IGenCodeTemplate template)
        {
            Templates[templateName] = template;
        }

        public static IGenCodeTemplate GetTemplate(string templateName)
        {
            return Templates.GetValueOrDefault(templateName);
        }

        public static void Generate(IBindGroup bindGroup)
        {
            var task = GetTemplate(bindGroup.TemplateName).CreateTask(bindGroup);
            Generate(task);
        }

        private static void Generate(GenCodeTask task)
        {
            GenCodePipeline.Instance.Generate(task);
        }
    }

    /// <summary>
    /// * 代码生成任务
    /// </summary>
    [Serializable]
    public class GenCodeTask
    {
        /// <summary>
        /// * 任务状态
        /// </summary>
        public GenCodeTaskStatus status;

        /// <summary>
        /// * 目标对象
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// * 绑定信息列表
        /// </summary>
        public List<BindInfo> bindInfos = new();

        /// <summary>
        /// * 脚本目录
        /// </summary>
        public string scriptsFolder;

        /// <summary>
        /// * 类名
        /// </summary>
        public string className;

        /// <summary>
        /// * 命名空间
        /// </summary>
        public string nameSpace;

        /// <summary>
        /// * 主要代码
        /// </summary>
        public string mainCode;

        /// <summary>
        /// * designer代码
        /// </summary>
        public string designerCode;
    }

    /// <summary>
    /// * 代码生成任务状态
    /// </summary>
    public enum GenCodeTaskStatus
    {
        Search, // # 搜索中
        Generating, // # 正在生成
        Compile, // # 编译中
        Complete // # 完成
        ,
    }

    [Serializable]
    public class BindInfo
    {
        public string typeName;

        public string pathToRoot;

        public IBind bindScript;

        public string memberName;
    }

    /// <summary>
    /// * 模板接口
    /// </summary>
    public interface IGenCodeTemplate
    {
        GenCodeTask CreateTask(IBindGroup bindGroup);
    }

    [InitializeOnLoad]
    public class ViewPresenterTemplate : IGenCodeTemplate
    {
        static ViewPresenterTemplate()
        {
            GenCodeUtility.RegisterTemplate("ViewPresenter", new ViewPresenterTemplate());
        }

        public GenCodeTask CreateTask(IBindGroup bindGroup)
        {
            var viewPresenter = bindGroup as ViewPresenter;
            return new GenCodeTask()
            {
                gameObject = viewPresenter.gameObject,
                className = viewPresenter.scriptName,
                scriptsFolder = viewPresenter.scriptsFolder,
                nameSpace = viewPresenter.nameSpace,
            };
        }
    }
}
