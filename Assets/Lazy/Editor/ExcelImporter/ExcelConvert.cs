using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.ExcelImporter
{
    public class ExcelConvert
    {
        private const string ScriptTemplateName = "ExcelAssetScriptTemplate.cs.txt";
        private const string FieldTemplateName = "ExcelFieldTemplate.txt";

        [MenuItem("Assets/Lazy/Excel/CreateScript", false)]
        public static void CreateScript()
        {
            var savePath = EditorUtility.SaveFolderPanel(
                "Save ExcelAssetScript",
                Application.dataPath,
                ""
            );
            if (string.IsNullOrEmpty(savePath))
                return;
            var selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

            var excelPath = AssetDatabase.GetAssetPath(selectedAssets[0]);
            var excelName = Path.GetFileNameWithoutExtension(excelPath);
            var sheetNames = GetSheetNames(excelPath);

            var scriptString = BuildScriptString(excelName, sheetNames);

            var path = Path.ChangeExtension(Path.Combine(savePath, excelName), "cs");
            File.WriteAllText(path, scriptString);
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Lazy/Excel/CreateScript", true)]
        public static bool CreateScriptValidation()
        {
            var selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedAssets.Length != 1)
                return false;
            var path = AssetDatabase.GetAssetPath(selectedAssets[0]);
            return Path.GetExtension(path).Equals(".xls")
                || Path.GetExtension(path).Equals(".xlsx");
        }

        /// <summary>
        /// * 获取一个excel表中的所有sheet名称
        /// </summary>
        /// <param name="excelPath"></param>
        /// <returns></returns>
        private static List<string> GetSheetNames(string excelPath)
        {
            var sheetNames = new List<string>();
            using (
                var stream = File.Open(
                    excelPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                )
            )
            {
                IWorkbook book = null;
                if (Path.GetExtension(excelPath).Equals(".xls"))
                    book = new HSSFWorkbook(stream);
                else
                    book = new XSSFWorkbook(stream);

                for (var i = 0; i < book.NumberOfSheets; i++)
                {
                    var sheet = book.GetSheetAt(i);
                    sheetNames.Add(sheet.SheetName);
                }
            }

            return sheetNames;
        }

        private static string BuildScriptString(string excelName, List<string> sheetNames)
        {
            var scriptString = TemplateUtility.GetScriptTemplateString(ScriptTemplateName);
            var fieldStringTemplate = TemplateUtility.GetScriptTemplateString(FieldTemplateName);

            scriptString = scriptString.Replace("#ASSETSCRIPTNAME#", excelName);

            foreach (var sheetName in sheetNames)
            {
                Log.Log.MsgD($"{sheetName} ->name");
                var fieldString = string.Copy(fieldStringTemplate);
                fieldString = fieldString.Replace("#FIELDNAME#", sheetName);
                fieldString += "\n#ENTITYFIELDS#";
                scriptString = scriptString.Replace("#ENTITYFIELDS#", fieldString);
            }

            scriptString = scriptString.Replace("#ENTITYFIELDS#", "");
            return scriptString;
        }
    }
}
