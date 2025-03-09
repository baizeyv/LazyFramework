using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lazy.Editor.NinePatchSlicer
{
    public class NinePatchSlicer : UnityEditor.Editor
    {
        private static List<string> _texture2DList;

        public static bool CreateBackup { get; } = false;

        public static SliceOptions Options { get; } = new();

        [MenuItem("Assets/Lazy/AutoNinePatchSlice", false, 1030)]
        public static void NinePatchSlice()
        {
            _texture2DList = new();

            // 获取所有选中 文件、文件夹的 GUID
            var guids = Selection.assetGUIDs;
            foreach (var guid in guids)
            {
                // 将 GUID 转换为 路径
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadMainAssetAtPath(assetPath) is Texture2D)
                {
                    if (!assetPath.Contains(".original"))
                    {
                        _texture2DList.Add(assetPath);
                    }
                }
            }

            foreach (var target in _texture2DList)
            {
                var importer = AssetImporter.GetAtPath(target);
                if (importer is TextureImporter textureImporter)
                {
                    if (textureImporter.spriteBorder != Vector4.zero)
                    {
                        Debug.Log($"已设置九宫格，跳过 {Path.GetFileName(target)}");
                        continue;
                    }

                    var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", target);
                    var bytes = File.ReadAllBytes(fullPath);

                    if (CreateBackup)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(fullPath);
                        File.WriteAllBytes(
                            Path.Combine(Path.GetDirectoryName(fullPath) ?? "",
                                fileName + ".original" + Path.GetExtension(fullPath)), bytes);
                    }

                    var targetTexture = new Texture2D(2, 2);
                    targetTexture.LoadImage(bytes);

                    var slicedTexture = Slicer.Slice(targetTexture, Options);
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.spriteBorder = slicedTexture.Border.ToVector4();
                    if (fullPath.EndsWith(".png")) File.WriteAllBytes(fullPath, slicedTexture.Texture.EncodeToPNG());
                    if (fullPath.EndsWith(".jpg")) File.WriteAllBytes(fullPath, slicedTexture.Texture.EncodeToJPG());
                    if (fullPath.EndsWith(".jpeg")) File.WriteAllBytes(fullPath, slicedTexture.Texture.EncodeToJPG());

                    Debug.Log($"图片九宫格切割完成！{Path.GetFileName(target)} = {textureImporter.spriteBorder}");
                }
            }

            AssetDatabase.Refresh();

            ////////////////////////

            // 获取所有选中的图片
            var selectedTextures = Selection.objects
                .Where(obj => obj is Texture2D) // 过滤出 Texture2D
                .Cast<Texture2D>()
                .ToList();

            if (selectedTextures.Count == 0)
            {
                Debug.LogWarning("No textures selected.");
                return;
            }

            // 遍历所有选中的图片
            foreach (var texture in selectedTextures)
            {
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    // 计算九宫格边界
                    Vector4 border = CalculateNineSliceBorder(texture);

                    // 设置九宫格边界
                    importer.spriteBorder = border;
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;

                    // 保存修改
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();

                    Debug.Log($"Set 9-slice border for {texture.name}: {border}");
                }
            }

            Debug.Log($"Auto set 9-slice for {selectedTextures.Count} textures.");
        }

        // 计算九宫格边界
        private static Vector4 CalculateNineSliceBorder(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;

            // 计算中间区域的边界
            var borderLeft = width / 2 - 1; // 左边界到中心左侧
            var borderRight = width / 2 - 1; // 右边界到中心右侧
            var borderBottom = height / 2 - 1; // 下边界到中心下侧
            var borderTop = height / 2 - 1; // 上边界到中心上侧

            return new Vector4(borderLeft, borderBottom, borderRight, borderTop);
        }
    }
}