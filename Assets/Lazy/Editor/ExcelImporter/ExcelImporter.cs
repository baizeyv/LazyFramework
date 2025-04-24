using System;
using System.Collections.Generic;
using System.IO;
using Lazy;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LazyEditor
{
    /// <summary>
    /// ! 对于数组类型的, string[] 分隔符是"," 应该这样填表: "test","hello","which"
    /// ! 其他类型的分隔符是,  应该这样填: 1,2,3  2.2,3.2,3  true,false,true
    /// </summary>
    public class ExcelImporter : AssetPostprocessor
    {
        /// <summary>
        /// * Excel资产缓存
        /// </summary>
        private static List<ExcelAssetInfo> _cachedInfos = null; // clear on compile

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            var imported = false;
            foreach (var path in importedAssets)
                if (
                    Path.GetExtension(path).Equals(".xls")
                    || Path.GetExtension(path).Equals(".xlsx")
                )
                {
                    if (_cachedInfos == null)
                        _cachedInfos = FindExcelAssetInfos();

                    var execlName = Path.GetFileNameWithoutExtension(path);
                    if (execlName.StartsWith("~$"))
                        continue;
                    if (path.Contains("StreamingAssets"))
                        continue;

                    var info = _cachedInfos.Find(x => x.ExcelName.Equals(execlName));
                    if (info == null)
                        continue;
                    ImportExcel(path, info);
                    imported = true;
                }

            if (imported)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static List<ExcelAssetInfo> FindExcelAssetInfos()
        {
            var list = new List<ExcelAssetInfo>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(ExcelAssetAttribute), false);
                if (attributes.Length == 0)
                    continue;
                var attribute = (ExcelAssetAttribute)attributes[0];
                var info = new ExcelAssetInfo { AssetType = type, Attribute = attribute };
                list.Add(info);
            }

            return list;
        }

        /// <summary>
        /// * 加载或创建对应路径类型的ScriptableObject
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="assetType"></param>
        /// <returns></returns>
        private static Object LoadOrCreateAsset(string assetPath, Type assetType)
        {
            FileUtility.CheckOrCreateDir(Path.GetDirectoryName(assetPath));
            var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance(assetType.Name);
                AssetDatabase.CreateAsset((ScriptableObject)asset, assetPath);
                // # 不可编辑的SO数据盒
                asset.hideFlags = HideFlags.NotEditable;
            }

            return asset;
        }

        /// <summary>
        /// * 加载Excel Book
        /// </summary>
        /// <param name="excelPath"></param>
        /// <returns></returns>
        private static IWorkbook LoadBook(string excelPath)
        {
            using (
                var stream = File.Open(
                    excelPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                )
            )
            {
                if (Path.GetExtension(excelPath).Equals(".xls"))
                    return new HSSFWorkbook(stream);
                return new XSSFWorkbook(stream);
            }
        }

        private static void ImportExcel(string excelPath, ExcelAssetInfo info)
        {
            var assetPath = "";
            var assetName = info.AssetType.Name + ".asset";
            if (string.IsNullOrEmpty(info.Attribute.AssetPath))
            {
                var basePath = Path.GetDirectoryName(excelPath);
                assetPath = Path.Combine(basePath, assetName);
            }
            else
            {
                var path = Path.Combine("Assets", info.Attribute.AssetPath);
                assetPath = Path.Combine(path, assetName);
            }

            var asset = LoadOrCreateAsset(assetPath, info.AssetType);
            var book = LoadBook(excelPath);

            // # 该类型的所有字段
            var assetFields = info.AssetType.GetFields();
            // assetFields.Print(x => x.FieldType.Name + " " + x.Name);
            var sheetCount = 0;
            foreach (var assetField in assetFields)
            {
                var sheet = book.GetSheet(assetField.Name);
                if (sheet == null)
                    continue;

                var fieldType = assetField.FieldType;
                if (
                    !fieldType.IsGenericType
                    || fieldType.GetGenericTypeDefinition() != typeof(List<>)
                )
                    continue;
                var types = fieldType.GetGenericArguments();
                var entityType = types[0];

                var entities = ExcelParser.GetEntityListFromSheet(sheet, entityType);
                assetField.SetValue(asset, entities);
                sheetCount++;
            }

            if (info.Attribute.LogOnImport)
                Log.MsgD($"Imported {sheetCount} sheets form {excelPath}.");

            EditorUtility.SetDirty(asset);
        }
    }
}
