using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Editor.UIEditor
{
    [CustomEditor(typeof(ABSBind), true)]
    [CanEditMultipleObjects]
    public class BindInspector : UnityEditor.Editor
    {
        private ABSBind BindScript => target as ABSBind;

        private string[] _componentNames;

        private int _componentNameIndex;

        private SerializedProperty _componentNameProperty;

        private SerializedProperty _customComponentNameProperty;

        private void OnEnable()
        {
            var components = BindScript.GetComponents<Component>();
            _componentNames = components
                .Where(x => !(x is ABSBind))
                .Select(x => x.GetType().FullName)
                .ToArray();
            _componentNameIndex = _componentNames
                .ToList()
                .FindIndex(x => x.Contains(BindScript.TypeName));
            if (_componentNameIndex == -1 || _componentNameIndex >= _componentNames.Length)
                _componentNameIndex = 0;

            _componentNameProperty = serializedObject.FindProperty("componentName");
            _customComponentNameProperty = serializedObject.FindProperty("customComponentName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.Label(
                    "<color=#7bed9f>Bind Setting</color>",
                    new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 12,
                        richText = true,
                    }
                );
                var rootGameObj = GetBindParentGameObject(BindScript);
                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label(
                        "Bind",
                        new GUIStyle(GUI.skin.label) { fontSize = 10 },
                        GUILayout.Width(60)
                    );
                    if (rootGameObj)
                    {
                        GUI.enabled = false;
                        BindScript.markType = BindType.Default;
                    }

                    EditorGUI.BeginChangeCheck();
                    BindScript.markType = (BindType)EditorGUILayout.EnumPopup(BindScript.markType);
                    if (EditorGUI.EndChangeCheck())
                        EditorUtility.SetDirty(target);
                    if (rootGameObj)
                        GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                if (
                    string.IsNullOrEmpty(_customComponentNameProperty.stringValue)
                    || string.IsNullOrEmpty(_customComponentNameProperty.stringValue.Trim())
                )
                    _customComponentNameProperty.stringValue = BindScript.name;

                if (BindScript.markType == BindType.Default)
                {
                    GUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label(
                            "Type",
                            new GUIStyle(GUI.skin.label) { fontSize = 10 },
                            GUILayout.Width(60)
                        );
                        EditorGUI.BeginChangeCheck();
                        _componentNameIndex = EditorGUILayout.Popup(
                            _componentNameIndex,
                            _componentNames
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            _componentNameProperty.stringValue = _componentNames[
                                _componentNameIndex
                            ];
                            EditorUtility.SetDirty(target);
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label(
                        "Parent",
                        new GUIStyle(GUI.skin.label) { fontSize = 10 },
                        GUILayout.Width(60)
                    );
                    GUILayout.Label(
                        GetBindParentName(BindScript),
                        new GUIStyle(GUI.skin.label) { fontSize = 10 }
                    );
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.objects = new Object[]
                        {
                            GetBindParentGameObject(target as ABSBind),
                        };
                    if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        EditorGUIUtility.PingObject(GetBindParentGameObject(target as ABSBind));
                }
                GUILayout.EndHorizontal();

                if (BindScript.markType != BindType.Default)
                {
                    GUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label(
                            "ClassName",
                            new GUIStyle(GUI.skin.label) { fontSize = 10 },
                            GUILayout.Width(60)
                        );
                        _customComponentNameProperty.stringValue = EditorGUILayout.TextField(
                            _customComponentNameProperty.stringValue
                        );
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginVertical("box");
                {
                    GUILayout.Label("Comment", new GUIStyle(GUI.skin.label) { fontSize = 10 });
                    BindScript.customComment = EditorGUILayout.TextArea(
                        BindScript.Comment,
                        GUILayout.Height(35)
                    );
                }
                GUILayout.EndVertical();

                if (rootGameObj)
                    if (GUILayout.Button($"Generate ({rootGameObj.name})", GUILayout.Height(30)))
                        GenViewPresenterCodeUtility.Generate(rootGameObj.GetComponent<IBindGroup>());
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private static GameObject GetBindParentGameObject(ABSBind bind)
        {
            var trans = bind.transform;
            while (trans.parent != null)
            {
                if (trans.parent.GetComponent<IBindGroup>() != null)
                    return trans.parent.gameObject;
                trans = trans.parent;
            }

            return null;
        }

        private static string GetBindParentName(ABSBind bind)
        {
            var trans = bind.transform;
            while (trans.parent != null)
            {
                if (trans.parent.GetComponent<ViewPresenter>())
                    return $"{trans.parent.name}({trans.parent.GetComponent<ViewPresenter>().scriptName})";
                trans = trans.parent;
            }

            return trans.name;
        }
    }
}
