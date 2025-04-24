#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NPOI.SS.UserModel;
using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// !!!!! 不建议在运行时中使用 (但还是留了方法)
    /// </summary>
    public static class ExcelParser
    {
        /// <summary>
        /// * 从Excel第一行获取所有字段名称
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        private static List<string> GetFieldNamesFromSheetHeader(ISheet sheet)
        {
            // # 第一行是Header
            var headerRow = sheet.GetRow(0);

            var fieldNames = new List<string>();
            for (var i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell == null || cell.CellType == CellType.Blank)
                    break;
                fieldNames.Add(cell.StringCellValue);
            }

            return fieldNames;
        }

        /// <summary>
        /// * 将一个格子中的元素转为对应类型
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="fieldInfo"></param>
        /// <param name="isFormulaEvalute"></param>
        /// <returns></returns>
        private static object CellToFieldObject(
            ICell cell,
            FieldInfo fieldInfo,
            bool isFormulaEvalute = false
        )
        {
            var type = isFormulaEvalute ? cell.CachedFormulaResultType : cell.CellType;
            if (
                (
                    fieldInfo.FieldType == typeof(int[])
                    || fieldInfo.FieldType == typeof(float[])
                    || fieldInfo.FieldType == typeof(bool[])
                )
                && type == CellType.String
            )
            {
                var str = cell.StringCellValue;
                if (str.EndsWith(","))
                    str = str.Substring(0, str.Length - 1);
                var arr = str.Replace(" ", "").Split(',');
                var elementType = fieldInfo.FieldType.GetElementType();
                var arrayInstance = Array.CreateInstance(elementType, arr.Length);
                for (var i = 0; i < arr.Length; i++)
                    arrayInstance.SetValue(Convert.ChangeType(arr[i], elementType), i);
                return arrayInstance;
            }
            else if (fieldInfo.FieldType == typeof(string[]) && type == CellType.String)
            {
                var str = cell.StringCellValue;
                if (str.EndsWith(","))
                    str = str.Substring(0, str.Length - 1);
                var arr = str.Split("\",\"");
                if (arr != null)
                {
                    arr[0] = arr[0].Substring(1);
                    arr[^1] = arr[^1].Substring(0, arr[^1].Length - 1);
                }

                var elementType = fieldInfo.FieldType.GetElementType();
                var arrayInstance = Array.CreateInstance(elementType, arr.Length);
                for (var i = 0; i < arr.Length; i++)
                    arrayInstance.SetValue(Convert.ChangeType(arr[i], elementType), i);
                return arrayInstance;
            }

            switch (type)
            {
                case CellType.String:
                    return fieldInfo.FieldType.IsEnum
                        ? Enum.Parse(fieldInfo.FieldType, cell.StringCellValue)
                        : cell.StringCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Numeric:
                    return Convert.ChangeType(cell.NumericCellValue, fieldInfo.FieldType);
                case CellType.Formula:
                    if (isFormulaEvalute)
                        return null;
                    return CellToFieldObject(cell, fieldInfo, true);
                default:
                    if (fieldInfo.FieldType.IsValueType)
                        return Activator.CreateInstance(fieldInfo.FieldType);
                    return null;
            }
        }

        /// <summary>
        /// * 创建指定一行的实体
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnNames"></param>
        /// <param name="entityType"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public static object CreateEntityFromRow(
            IRow row,
            List<string> columnNames,
            Type entityType,
            string sheetName
        )
        {
            var entity = Activator.CreateInstance(entityType);
            var fields = entityType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            for (var i = 0; i < columnNames.Count; i++)
            {
                var entityField = fields.FirstOrDefault(f => f.Name.Equals(columnNames[i]));
                if (entityField == null)
                    continue;
                if (
                    !entityField.IsPublic
                    && entityField.GetCustomAttributes(typeof(SerializeField), false).Length == 0
                )
                    continue;

                var cell = row.GetCell(i);
                if (cell == null)
                    continue;

                try
                {
                    var fieldValue = CellToFieldObject(cell, entityField);
                    entityField.SetValue(entity, fieldValue);
                }
                catch (Exception)
                {
                    throw new Exception(
                        $"Invalid excel cell type at row {row.RowNum}, column {cell.ColumnIndex}, {sheetName} sheet."
                    );
                }
            }

            return entity;
        }

        public static object GetEntityListFromSheet(ISheet sheet, Type entityType)
        {
            // # 所有列名
            var excelColumnNames = GetFieldNamesFromSheetHeader(sheet);
            var listType = typeof(List<>).MakeGenericType(entityType);
            var listAddMethod = listType.GetMethod("Add", new Type[] { entityType });
            var list = Activator.CreateInstance(listType);

            var foundFirstRow = false;
            var searchFirstRowCount = 100;

            for (var i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                    break;

                var entryCell = row.GetCell(0);
                if (entryCell == null || entryCell.CellType == CellType.Blank)
                {
                    if (foundFirstRow)
                    {
                        break;
                    }
                    else
                    {
                        searchFirstRowCount--;
                        if (searchFirstRowCount == 0)
                            break;
                        else
                            continue;
                    }
                }

                // skip comment row
                if (
                    entryCell.CellType == CellType.String
                    && (
                        entryCell.StringCellValue.StartsWith("#")
                        || entryCell.StringCellValue.StartsWith("//")
                    )
                )
                    continue;

                foundFirstRow = true;

                var entity = CreateEntityFromRow(
                    row,
                    excelColumnNames,
                    entityType,
                    sheet.SheetName
                );
                listAddMethod.Invoke(list, new object[] { entity });
            }

            return list;
        }

        private static List<TEntityType> GetEntityListFromSheet<TEntityType>(ISheet sheet)
            where TEntityType : class
        {
            // # 所有列名
            var excelColumnNames = GetFieldNamesFromSheetHeader(sheet);

            var list = new List<TEntityType>();
            var entityType = typeof(TEntityType);

            var foundFirstRow = false;
            var searchFirstRowCount = 100;

            for (var i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                    break;

                var entryCell = row.GetCell(0);
                if (entryCell == null || entryCell.CellType == CellType.Blank)
                {
                    if (foundFirstRow)
                    {
                        break;
                    }
                    else
                    {
                        searchFirstRowCount--;
                        if (searchFirstRowCount == 0)
                            break;
                        else
                            continue;
                    }
                }

                // skip comment row
                if (
                    entryCell.CellType == CellType.String
                    && (
                        entryCell.StringCellValue.StartsWith("#")
                        || entryCell.StringCellValue.StartsWith("//")
                    )
                )
                    continue;

                foundFirstRow = true;

                var entity = CreateEntityFromRow(
                    row,
                    excelColumnNames,
                    entityType,
                    sheet.SheetName
                );
                list.Add((TEntityType)entity);
            }

            return list;
        }

        private static void SheetToList<TEntityType>(List<TEntityType> list, ISheet sheet)
        {
            // # 所有列名
            var excelColumnNames = GetFieldNamesFromSheetHeader(sheet);

            var entityType = typeof(TEntityType);

            var foundFirstRow = false;
            var searchFirstRowCount = 100;

            for (var i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                    break;

                var entryCell = row.GetCell(0);
                if (entryCell == null || entryCell.CellType == CellType.Blank)
                {
                    if (foundFirstRow)
                    {
                        break;
                    }
                    else
                    {
                        searchFirstRowCount--;
                        if (searchFirstRowCount == 0)
                            break;
                        else
                            continue;
                    }
                }

                // skip comment row
                if (
                    entryCell.CellType == CellType.String
                    && (
                        entryCell.StringCellValue.StartsWith("#")
                        || entryCell.StringCellValue.StartsWith("//")
                    )
                )
                    continue;

                foundFirstRow = true;

                var entity = CreateEntityFromRow(
                    row,
                    excelColumnNames,
                    entityType,
                    sheet.SheetName
                );
                list.Add((TEntityType)entity);
            }
        }

        private static void SheetToDictionary<TEntityType, Tkey>(
            Dictionary<Tkey, TEntityType> dic,
            ISheet sheet,
            string keyFieldName = null
        )
        {
            // # 所有列名
            var excelColumnNames = GetFieldNamesFromSheetHeader(sheet);

            var entityType = typeof(TEntityType);

            var foundFirstRow = false;
            var searchFirstRowCount = 100;

            for (var i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                    break;

                var entryCell = row.GetCell(0);
                if (entryCell == null || entryCell.CellType == CellType.Blank)
                {
                    if (foundFirstRow)
                    {
                        break;
                    }
                    else
                    {
                        searchFirstRowCount--;
                        if (searchFirstRowCount == 0)
                            break;
                        else
                            continue;
                    }
                }

                // skip comment row
                if (
                    entryCell.CellType == CellType.String
                    && (
                        entryCell.StringCellValue.StartsWith("#")
                        || entryCell.StringCellValue.StartsWith("//")
                    )
                )
                    continue;

                foundFirstRow = true;

                var entity = CreateEntityFromRow(
                    row,
                    excelColumnNames,
                    entityType,
                    sheet.SheetName
                );
                var keyField = entityType.GetField(keyFieldName);
                var key = (Tkey)keyField.GetValue(entity);
                dic.Add(key, (TEntityType)entity);
            }
        }

        public static T LoadExcel<T>(string excelPath)
            where T : class
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
                return LoadExcel<T>(stream);
            }
        }

        public static T LoadExcel<T>(Stream stream)
            where T : class
        {
            var asset = Activator.CreateInstance<T>();
            var book = WorkbookFactory.Create(stream);
            var targetType = typeof(T);
            var attribute = targetType.GetCustomAttribute<ExcelAssetAttribute>();
            var info = new ExcelAssetInfo { AssetType = targetType, Attribute = attribute };
            var assetFields = info.AssetType.GetFields();
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

                var entities = GetEntityListFromSheet(sheet, entityType);
                assetField.SetValue(asset, entities);
                sheetCount++;
            }

            if (info.Attribute.LogOnImport)
                Log.MsgD($"Imported {sheetCount} sheets.");

            return asset;
        }

        public static T LoadExcel<T, TEntityType>(Stream stream)
            where T : class
            where TEntityType : class
        {
            var asset = Activator.CreateInstance<T>();
            var book = WorkbookFactory.Create(stream);
            var targetType = typeof(T);
            var attribute = targetType.GetCustomAttribute<ExcelAssetAttribute>();
            var info = new ExcelAssetInfo { AssetType = targetType, Attribute = attribute };
            var assetFields = info.AssetType.GetFields();
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

                var entities = GetEntityListFromSheet<TEntityType>(sheet);
                assetField.SetValue(asset, entities);
                sheetCount++;
            }

            if (info.Attribute.LogOnImport)
                Debug.Log(string.Format("Imported {0} sheets.", sheetCount));

            return asset;
        }

        public static void LoadToDictionary<TEntityType, TKey>(
            Dictionary<TKey, TEntityType> dic,
            Stream stream,
            string sheetName = null,
            string keyFieldName = null
        )
        {
            var book = WorkbookFactory.Create(stream);
            if (string.IsNullOrEmpty(sheetName))
                sheetName = book.GetSheetName(0);
            var sheet = book.GetSheet(sheetName);
            if (sheet == null)
                return;
            SheetToDictionary(dic, sheet, keyFieldName);
        }

        public static void LoadToDictionary<TEntityType, TKey>(
            Dictionary<TKey, TEntityType> dic,
            byte[] bytes,
            string sheetName = null,
            string keyFieldName = null
        )
        {
            using (var stream = new MemoryStream(bytes))
            {
                LoadToDictionary(dic, stream, sheetName, keyFieldName);
            }
        }

        public static void LoadToDictionary<TEntityType, TKey>(
            Dictionary<TKey, TEntityType> dic,
            string excelPath,
            string sheetName = null,
            string keyFieldName = null
        )
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
                LoadToDictionary(dic, stream, sheetName, keyFieldName);
            }
        }

        public static bool LoadToList<T>(List<T> values, Stream stream, string sheetName = null)
            where T : class
        {
            var book = WorkbookFactory.Create(stream);
            if (string.IsNullOrEmpty(sheetName))
                sheetName = book.GetSheetName(0);
            var sheet = book.GetSheet(sheetName);
            if (sheet == null)
                return false;
            SheetToList(values, sheet);
            return true;
        }

        public static bool LoadToList<T>(List<T> values, byte[] bytes, string sheetName = null)
            where T : class
        {
            using (var stream = new MemoryStream(bytes))
            {
                return LoadToList(values, stream, sheetName);
            }
        }

        public static bool LoadToList<T>(List<T> values, string excelPath, string sheetName = null)
            where T : class
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
                return LoadToList(values, stream, sheetName);
            }
        }
    }
}
#endif
