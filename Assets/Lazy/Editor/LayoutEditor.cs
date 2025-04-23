using Lazy;
using UnityEditor;

namespace Editor.Lazy
{
    [CustomEditor(typeof(GridCenterLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class GridCenterLayoutGroupEditor : UnityEditor.Editor
    {
        private SerializedProperty m_Padding;
        private SerializedProperty m_CellSize;
        private SerializedProperty m_Spacing;
        private SerializedProperty m_StartCorner;
        private SerializedProperty m_StartAxis;
        private SerializedProperty m_ChildAlignment;
        private SerializedProperty m_Constraint;
        private SerializedProperty m_ConstraintCount;

        private void OnEnable()
        {
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_CellSize = serializedObject.FindProperty("mCellSize");
            m_Spacing = serializedObject.FindProperty("mSpacing");
            m_StartCorner = serializedObject.FindProperty("mStartCorner");
            m_StartAxis = serializedObject.FindProperty("mStartAxis");
            m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_Constraint = serializedObject.FindProperty("mConstraint");
            m_ConstraintCount = serializedObject.FindProperty("mConstraintCount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_CellSize, true);
            EditorGUILayout.PropertyField(m_Spacing, true);
            EditorGUILayout.PropertyField(m_StartCorner, true);
            EditorGUILayout.PropertyField(m_StartAxis, true);
            EditorGUILayout.PropertyField(m_ChildAlignment, true);
            EditorGUILayout.PropertyField(m_Constraint, true);
            if (m_Constraint.enumValueIndex > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ConstraintCount, true);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
