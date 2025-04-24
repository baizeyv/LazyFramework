using System;
using Lazy;
using UnityEditor;
using UnityEngine;

namespace LazyEditor
{
    public class CaptureEditorWindow : EditorWindow
    {
        private const string SaveDirPathKey = "CaptureSaveDirPathKey";

        [MenuItem("Lazy/Capture/Capture Editor", false, 100)]
        private static void Capture()
        {
            if (HasOpenInstances<CaptureEditorWindow>())
                GetWindow<CaptureEditorWindow>("Capture Editor").Close();
            else
                GetWindow<CaptureEditorWindow>("Capture Editor");
        }

        [MenuItem("Lazy/Capture/Capture &`", false, 100)]
        private static void CaptureNow()
        {
            var buildPath = EditorPrefs.GetString(SaveDirPathKey, Application.dataPath);
            var resolution = GetMainGameViewSize();
            var x = (int)resolution.x;
            var y = (int)resolution.y;
            var outputPath =
                buildPath
                + "/"
                + Application.productName
                + "_"
                + DateTime.Now.ToString($"{x}x{y}_yyyy_MM_dd_HH_mm_ss")
                + ".png";
            ScreenCapture.CaptureScreenshot(outputPath);
            Log.I().Tag("Capture").Msg($"Save Path: {outputPath}").Do();
        }

        private void OnGUI()
        {
            var buildPath = EditorPrefs.GetString(SaveDirPathKey, Application.dataPath);
            EditorGUILayout.LabelField("Output Directory: ");
            EditorGUILayout.LabelField(buildPath);
            if (GUILayout.Button("Select Directory"))
            {
                var path = EditorUtility.OpenFolderPanel(
                    "Select Output Directory",
                    buildPath,
                    Application.dataPath
                );
                if (!string.IsNullOrEmpty(path))
                    EditorPrefs.SetString(SaveDirPathKey, path);
            }

            if (GUILayout.Button("Open Directory"))
            {
                var openPath = EditorPrefs.GetString(SaveDirPathKey, Application.dataPath);
                System.Diagnostics.Process.Start(openPath);
            }

            GUILayout.Space(5);
            var cameraContent = new GUIContent(
                " Capture",
                EditorImageManager.CameraCapture,
                "Capture Now"
            );
            if (GUILayout.Button(cameraContent, GUILayout.Height(25)))
            {
                var resolution = GetMainGameViewSize();
                var x = (int)resolution.x;
                var y = (int)resolution.y;
                var outputPath =
                    buildPath
                    + "/"
                    + DateTime.Now.ToString($"{x}x{y}_yyyy_MM_dd_HH_mm_ss")
                    + ".png";
                ScreenCapture.CaptureScreenshot(outputPath);
                Log.I().Tag(this).Msg($"Save Path: {outputPath}").Do();
            }
        }

        public static Vector2 GetMainGameViewSize()
        {
            var T = Type.GetType("UnityEditor.GameView,UnityEditor");
            var getSizeOfMainGameView = T.GetMethod(
                "GetSizeOfMainGameView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );
            var res = getSizeOfMainGameView?.Invoke(null, null);
            if (res != null)
                return (Vector2)res;
            return new Vector2(720, 1280);
        }
    }
}
