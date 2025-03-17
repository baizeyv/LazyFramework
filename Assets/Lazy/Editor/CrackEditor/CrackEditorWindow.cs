using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.CrackEditor
{
    public class CrackEditorWindow : EditorWindow
    {
        private const string WindowName = "Crack Editor";

        private string _inputHexText = "";

        private ValueType _selectedValueType = ValueType.String;

        private int _dimension = 1;

        private string _result;

        private Vector2 _scrollPos;

        private int _totalLen;

        [MenuItem("Lazy/Crack Editor", false, 100)]
        public static void ShowWindow()
        {
            if (HasOpenInstances<CrackEditorWindow>())
            {
                // # 如果已经打开了就关闭
                GetWindow<CrackEditorWindow>(WindowName)
                    .Close();
            }
            else
            {
                var window = GetWindow<CrackEditorWindow>(WindowName);
                window.minSize = new Vector2(200f, 200f);
                window.name = WindowName;
                window.Show();
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("HexString:");
            _inputHexText = EditorGUILayout.TextField(_inputHexText);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("ValueType:");
            _selectedValueType = (ValueType)EditorGUILayout.EnumPopup(_selectedValueType);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dimension:");
            _dimension = EditorGUILayout.IntField(_dimension);
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_result))
                GUILayout.Label($"Length: {_totalLen}", EditorStyles.boldLabel);

            if (_dimension < 1)
                _dimension = 1;

            GUILayout.Space(5);
            if (GUILayout.Button("Convert"))
            {
                if (string.IsNullOrEmpty(_inputHexText))
                {
                    EditorUtility.DisplayDialog("Error", "Input Can not be null! (ODD)", "OK");
                    _result = "";
                }
                else
                {
                    try
                    {
                        _result = Handle(
                            _inputHexText,
                            _selectedValueType,
                            _dimension,
                            out _totalLen
                        );
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error", "ValueType Error", "OK");
                    }
                }
            }

            if (!string.IsNullOrEmpty(_result))
            {
                GUILayout.Space(10);
                GUILayout.Label("Result:", EditorStyles.boldLabel);
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                GUILayout.Label(_result, EditorStyles.wordWrappedLabel);
                GUILayout.EndScrollView();
                if (GUILayout.Button("Clear"))
                {
                    _result = "";
                    _selectedValueType = ValueType.String;
                    _inputHexText = "";
                    _dimension = 1;
                }
            }
        }

        /// <summary>
        /// 主处理方法
        /// </summary>
        /// <param name="content"></param>
        /// <param name="valueType"></param>
        /// * <param name="dimension">维数 (多少个数据为一组)</param>
        /// <param name="totalLen">总长度</param>
        private static string Handle(
            string content,
            ValueType valueType,
            int dimension,
            out int totalLen
        )
        {
            var byteArray = HandleByteArray(HandleHexStringArray(HandleHexString(content)));

            if (byteArray.Length % dimension != 0)
            {
                // # 错误信息输出
                EditorUtility.DisplayDialog("Error", $"dimension {dimension} error !", "OK");
                totalLen = 0;
                return "";
            }

            var ret = "";
            var len = byteArray.Length / dimension;
            var method = GetReadMethod(valueType);
            for (var d = 0; d < dimension; d++)
            {
                ret += "[";
                for (var i = 0; i < len; i++)
                {
                    ret += method(byteArray[i + d * len]);

                    if (i + 1 != len)
                        ret += ", ";
                }

                ret += "]\n";
            }

            totalLen = byteArray.Length;
            Debug.Log(ret);
            return ret;
        }

        private static Func<ByteArray, object> GetReadMethod(ValueType valueType)
        {
            return valueType switch
            {
                ValueType.Int => x => x.ReadInt(),
                ValueType.String => x => x.ReadString(),
                ValueType.Bool => x => x.ReadBool(),
                ValueType.Float => x => x.ReadFloat(),
                ValueType.Double => x => x.ReadDouble(),
                ValueType.Short => x => x.ReadShort(),
                ValueType.Long => x => x.ReadLong(),
                ValueType.Uint => x => x.ReadUInt(),
                ValueType.Ulong => x => x.ReadULong(),
                ValueType.Ushort => x => x.ReadUShort(),
                ValueType.Byte => x => x.ReadByte(),
                ValueType.Sbyte => x => x.ReadSByte(),
                ValueType.UTF => x => x.ReadUTF(),
                _ => _ => null,
            };
        }

        /// <summary>
        /// * 处理16进制的字符串
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private static string[] HandleHexString(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            return Enumerable
                .Range(0, hexString.Length / 2)
                .Select(i => hexString.Substring(i * 2, 2))
                .ToArray();
        }

        /// <summary>
        /// * 处理字符串数组
        /// </summary>
        /// <param name="hexStrings"></param>
        /// <returns></returns>
        private static byte[] HandleHexStringArray(string[] hexStrings)
        {
            var ret = new byte[hexStrings.Length];
            for (var i = 0; i < ret.Length; i++)
                ret[i] = Convert.ToByte(hexStrings[i], 16);

            return ret;
        }

        /// <summary>
        /// * 处理byte数组
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        private static ByteArray[] HandleByteArray(byte[] byteArray)
        {
            var ret = new ByteArray[byteArray.Length / 4]; // # 4位一组
            for (var i = 0; i < ret.Length; i++)
            {
                var idx = i * 4;
                var item = new ByteArray(
                    new[]
                    {
                        byteArray[idx],
                        byteArray[idx + 1],
                        byteArray[idx + 2],
                        byteArray[idx + 3],
                    }
                );
                ret[i] = item;
            }

            return ret;
        }

        private enum ValueType
        {
            Int,
            String,
            Bool,
            Float,
            Double,
            Short,
            Long,
            Uint,
            Ulong,
            Ushort,
            Byte,
            Sbyte,
            UTF,
        }
    }
}
