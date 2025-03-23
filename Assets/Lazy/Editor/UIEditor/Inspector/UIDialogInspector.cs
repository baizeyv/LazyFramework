using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.UIEditor
{
    [CustomEditor(typeof(UIDialog), true)]
    public class UIDialogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label(
                        $"<color=#7bed9f>{target.GetType()}</color>",
                        new GUIStyle(GUI.skin.label)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 12,
                            richText = true,
                        }
                    );
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        $"<color=#0984e3>[PANEL]</color>",
                        new GUIStyle(GUI.skin.label)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 12,
                            richText = true,
                        }
                    );
                }
                GUILayout.EndHorizontal();
                serializedObject.Update();
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
                GUILayout.Space(2);
            }
            GUILayout.EndVertical();
        }
    }
}