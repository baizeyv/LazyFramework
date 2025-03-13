#if UNITY_EDITOR
using System;

namespace Lazy.Excel
{
    /// <summary>
    /// * Excel表格资产信息
    /// </summary>
    public class ExcelAssetInfo
    {
        /// <summary>
        /// * 资产类型
        /// </summary>
        public Type AssetType { get; set; }

        /// <summary>
        /// * 类属性
        /// </summary>
        public ExcelAssetAttribute Attribute { get; set; }

        /// <summary>
        /// * Excel表格名称
        /// </summary>
        public string ExcelName =>
            string.IsNullOrEmpty(Attribute.ExcelName) ? AssetType.Name : Attribute.ExcelName;
    }
}
#endif
