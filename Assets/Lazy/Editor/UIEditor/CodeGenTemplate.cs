using System;
using System.IO;
using System.Text;
using Lazy;

namespace Lazy.Editor.UIEditor
{
    public static class CodeGenTemplate
    {
        public static class UIPanelTemplate
        {
            public static void Generate(CodeGenInfo info)
            {
                if (!File.Exists(info.ScriptFilePath))
                    WriteMainCode(info);
                WriteDesignerCode(info);
            }

            private static void WriteMainCode(CodeGenInfo info)
            {
                FileUtility.CheckFileAndCreateDirWhenNeeded(info.ScriptFilePath);
                var writer = TemplateUtility.GetScriptTemplateString("UIPanelTemplate.cs.txt");
                writer = writer.Replace("#NAMESPACE#", info.Namespace);
                writer = writer.Replace("#CLASSNAME#", info.ClassName);
                writer = writer.Replace("#TIME#", DateTimeOffset.Now.ToLocalTime().ToString("F"));
                writer = writer.Replace("#GUID#", Guid.NewGuid().ToString());
                var sw = File.CreateText(info.ScriptFilePath);
                sw.Write(writer);
                sw.Flush();
                sw.Close();
                sw.Dispose();
            }

            private static void WriteDesignerCode(CodeGenInfo info)
            {
                var scriptFile = info.ScriptFilePath.Replace(".cs", ".designer.cs");
                FileUtility.CheckFileAndCreateDirWhenNeeded(scriptFile);
                var sw = File.CreateText(scriptFile);
                var writer = TemplateUtility.GetScriptTemplateString(
                    "UIPanelTemplate.designer.cs.txt"
                );
                writer = writer.Replace("#NAMESPACE#", info.Namespace);
                writer = writer.Replace("#CLASSNAME#", info.ClassName);
                writer = writer.Replace("#INHERIT#", info.IsDialog ? "UIDialog" : "UIPanel");
                writer = writer.Replace("#TIME#", DateTimeOffset.Now.ToLocalTime().ToString("F"));
                writer = writer.Replace("#GUID#", Guid.NewGuid().ToString());
                StringBuilder sb = new();
                foreach (var property in info.Properties)
                {
                    if (!string.IsNullOrEmpty(property.Comment))
                    {
                        sb.AppendLine("\t\t/// <summary>");
                        sb.AppendLine("\t\t/// " + property.Comment);
                        sb.AppendLine("\t\t/// </summary>");
                    }

                    sb.AppendLine("\t\t[SerializeField]");
                    sb.AppendLine($"\t\tpublic {property.TypeName} {property.PropertyName};");
                }

                writer = writer.Replace("#FIELD#", sb.ToString());
                sb.Clear();
                foreach (var property in info.Properties)
                    sb.AppendLine($"\t\t\t{property.PropertyName} = null;");
                writer = writer.Replace("#CLEAR#", sb.ToString());
                sw.Write(writer);
                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
        }

        public static class UIWidgetTemplate
        {
            public static void Generate(CodeGenInfo info)
            {
                if (!File.Exists(info.ScriptFilePath))
                    WriteMainCode(info);
                WriteDesignerCode(info);
            }

            private static void WriteMainCode(CodeGenInfo info)
            {
                FileUtility.CheckFileAndCreateDirWhenNeeded(info.ScriptFilePath);
                var sw = new StreamWriter(info.ScriptFilePath, false, new UTF8Encoding(false));
                var writer = TemplateUtility.GetScriptTemplateString("UIWidgetTemplate.cs.txt");
                writer = writer.Replace("#NAMESPACE#", info.Namespace);
                writer = writer.Replace("#CLASSNAME#", info.ClassName);
                writer = writer.Replace("#TIME#", DateTimeOffset.Now.ToLocalTime().ToString("F"));
                writer = writer.Replace("#GUID#", Guid.NewGuid().ToString());
                sw.Write(writer);
                sw.Flush();
                sw.Close();
            }

            private static void WriteDesignerCode(CodeGenInfo info)
            {
                var scriptFile = info.ScriptFilePath.Replace(".cs", ".designer.cs");
                FileUtility.CheckFileAndCreateDirWhenNeeded(scriptFile);
                var sw = new StreamWriter(scriptFile, false, Encoding.UTF8);
                var writer = TemplateUtility.GetScriptTemplateString(
                    "UIWidgetTemplate.designer.cs.txt"
                );
                writer = writer.Replace("#NAMESPACE#", info.Namespace);
                writer = writer.Replace("#CLASSNAME#", info.ClassName);
                writer = writer.Replace("#TIME#", DateTimeOffset.Now.ToLocalTime().ToString("F"));
                writer = writer.Replace("#GUID#", Guid.NewGuid().ToString());
                StringBuilder sb = new();
                foreach (var property in info.Properties)
                {
                    if (!string.IsNullOrEmpty(property.Comment))
                    {
                        sb.AppendLine("\t\t/// <summary>");
                        sb.AppendLine("\t\t/// " + property.Comment);
                        sb.AppendLine("\t\t/// </summary>");
                    }

                    sb.AppendLine("\t\t[SerializeField]");
                    sb.AppendLine($"\t\tpublic {property.TypeName} {property.PropertyName};");
                }

                writer = writer.Replace("#FIELD#", sb.ToString());
                sb.Clear();
                foreach (var property in info.Properties)
                    sb.AppendLine($"\t\t\t{property.PropertyName} = null;");
                writer = writer.Replace("#CLEAR#", sb.ToString());
                sw.Write(writer);
                sw.Flush();
                sw.Close();
            }
        }
    }
}
