using System;
using UnityEditor;
using UnityEngine;

namespace Editor.Lazy.PrefEditor.SearchField
{
    public class PreferencesSearchField : UnityEditor.IMGUI.Controls.SearchField
    {
        public enum SearchModePreferencesEditorWindow
        {
            Key,
            Value
        }

        public SearchModePreferencesEditorWindow SearchMode { get; private set; }

        public Action OnDropdownSelection;

        public new string OnGUI(Rect rect,
            string text,
            GUIStyle style,
            GUIStyle cancelButtonStyle,
            GUIStyle emptyCancelButtonStyle)
        {
            style.padding.left = 17;
            var contextMenuRect = new Rect(rect.x, rect.y, 10, rect.height);

            // Add interactive area
            EditorGUIUtility.AddCursorRect(contextMenuRect, MouseCursor.Text);
            if (Event.current.type == EventType.MouseDown && contextMenuRect.Contains(Event.current.mousePosition))
            {
                void OnDropdownSelection(object parameter)
                {
                    SearchMode =
                        (SearchModePreferencesEditorWindow)Enum.Parse(typeof(SearchModePreferencesEditorWindow),
                            parameter.ToString());
                    this.OnDropdownSelection?.Invoke();
                }

                GenericMenu menu = new();
                foreach (SearchModePreferencesEditorWindow item in Enum.GetValues(
                             typeof(SearchModePreferencesEditorWindow)))
                {
                    var enumName = Enum.GetName(typeof(SearchModePreferencesEditorWindow), item);
                    menu.AddItem(new GUIContent(enumName), SearchMode == item, OnDropdownSelection, enumName);
                }

                menu.DropDown(rect);
            }

            // Render original search field
            var result = base.OnGUI(rect, text, style, cancelButtonStyle, emptyCancelButtonStyle);

            // Render additional images
            GUIStyle contextMenuOverlayStyle = GUIStyle.none;
            contextMenuOverlayStyle.contentOffset = new(9, 5);
            GUI.Box(new Rect(rect.x, rect.y, 5, 5), EditorGUIUtility.IconContent("d_ProfilerTimelineDigDownArrow@2x"), contextMenuOverlayStyle);

            if (!HasFocus() && string.IsNullOrEmpty(text))
            {
                GUI.enabled = false;
                GUI.Label(new Rect(rect.x + 14, rect.y, 40, rect.height), Enum.GetName(typeof(SearchModePreferencesEditorWindow), SearchMode));
                GUI.enabled = true;
            }

            contextMenuOverlayStyle.contentOffset = new();
            return result;
        }

        public new string OnToolbarGUI(string text, params GUILayoutOption[] options) =>
            this.OnToolbarGUI(GUILayoutUtility.GetRect(29f, 200f, 18f, 18f, EditorStyles.toolbarSearchField, options),
                text);

        public new string OnToolbarGUI(Rect rect, string text) => this.OnGUI(rect, text,
            EditorStyles.toolbarSearchField, EditorStyles.toolbarButton, EditorStyles.toolbarButton);
    }
}