using System.IO;

namespace LazyEditor
{
    public static class TemplateUtility
    {
        /// <summary>
        /// * 获取脚本代码模板
        /// </summary>
        /// <returns></returns>
        public static string GetScriptTemplateString(string templateFileName)
        {
            // # 当前目录
            var currentDirectory = Directory.GetCurrentDirectory();
            var filePath = Directory.GetFiles(
                currentDirectory,
                templateFileName,
                SearchOption.AllDirectories
            );
            if (filePath.Length == 0)
                throw new FileNotFoundException("Script template not found.");

            var templateString = File.ReadAllText(filePath[0]);
            return templateString;
        }
    }
}
