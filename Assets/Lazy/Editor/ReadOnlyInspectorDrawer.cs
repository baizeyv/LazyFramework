using Lazy.Pool.Attribute;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.Pool
{
    [CustomPropertyDrawer(typeof(ReadOnlyInspectorFieldAttribute))]
    public sealed class ReadOnlyInspectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
}
