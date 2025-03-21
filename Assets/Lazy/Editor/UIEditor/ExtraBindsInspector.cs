using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Runtime.UI.Basic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Editor.UIEditor
{
    [CustomEditor(typeof(ExtraBinds))]
    public class ExtraBindsInspector : UnityEditor.Editor
    {
        private ExtraBinds _extraBinds;

        private void OnEnable()
        {
            _extraBinds = (ExtraBinds)target;
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(_extraBinds, "Changed Settings");
            var dataProperty = serializedObject.FindProperty("binds");

            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.Label(
                    "<color=#7bed9f>Extra Binds</color>",
                    new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 12,
                        richText = true,
                    }
                );
                GUILayout.Label("Drag GameObject To Here");
                var pathRect = EditorGUILayout.GetControlRect();
                pathRect.height = 35;
                GUI.Box(pathRect, string.Empty);
                EditorGUILayout.LabelField(string.Empty);
                if (
                    (
                        UnityEngine.Event.current.type == EventType.DragUpdated
                        || UnityEngine.Event.current.type == EventType.DragPerform
                    ) && pathRect.Contains(UnityEngine.Event.current.mousePosition)
                )
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (UnityEngine.Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var o in DragAndDrop.objectReferences)
                            if (o.name.Equals(target.name))
                                AddReference(dataProperty, "Self" + o.GetType().Name, o);
                            else
                                AddReference(dataProperty, RemoveString(o.name, " ", "-", "@"), o);
                    }

                    UnityEngine.Event.current.Use();
                }

                var delList = new List<int>();
                SerializedProperty property;
                for (var i = _extraBinds.binds.Count - 1; i >= 0; i--)
                {
                    GUILayout.BeginHorizontal();
                    {
                        property = dataProperty
                            .GetArrayElementAtIndex(i)
                            .FindPropertyRelative("memberName");
                        property.stringValue = EditorGUILayout.TextField(
                            property.stringValue,
                            GUILayout.Width(150)
                        );
                        property = dataProperty
                            .GetArrayElementAtIndex(i)
                            .FindPropertyRelative("obj");
                        property.objectReferenceValue = EditorGUILayout.ObjectField(
                            property.objectReferenceValue,
                            typeof(Object),
                            true
                        );
                        if (property.objectReferenceValue is Component component)
                        {
                            var objects = new List<Object>();
                            objects.AddRange(component.gameObject.GetComponents<Component>());
                            objects.Add(component.gameObject);

                            var index = objects.FindIndex(x =>
                                x.GetType() == property.objectReferenceValue.GetType()
                            );
                            var newIndex = EditorGUILayout.Popup(
                                index,
                                objects.Select(x => x.GetType().FullName).ToArray()
                            );
                            if (index != newIndex)
                                property.objectReferenceValue = objects[newIndex];
                        }
                        else if (property.objectReferenceValue is GameObject gameObject)
                        {
                            var objects = new List<Object>();
                            objects.AddRange(gameObject.GetComponents<Component>());
                            objects.Add(gameObject);

                            var index = objects.FindIndex(x =>
                                x.GetType() == property.objectReferenceValue.GetType()
                            );
                            var newIndex = EditorGUILayout.Popup(
                                index,
                                objects.Select(x => x.GetType().FullName).ToArray()
                            );
                            if (index != newIndex)
                                property.objectReferenceValue = objects[newIndex];
                        }

                        if (GUILayout.Button("✖"))
                            delList.Add(i);
                    }
                    GUILayout.EndHorizontal();
                }

                foreach (var i in delList)
                    dataProperty.DeleteArrayElementAtIndex(i);
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }

        private string RemoveString(string str, params string[] targets)
        {
            return targets.Aggregate(str, (current, t) => current.Replace(t, string.Empty));
        }

        private void AddReference(SerializedProperty dataProperty, string key, Object obj)
        {
            var index = dataProperty.arraySize;
            dataProperty.InsertArrayElementAtIndex(index);
            var element = dataProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("memberName").stringValue = key;
            element.FindPropertyRelative("obj").objectReferenceValue = obj;
        }
    }
}
