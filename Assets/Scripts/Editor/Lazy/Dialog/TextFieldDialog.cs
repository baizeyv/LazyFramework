using System;
using System.Collections.Generic;
using Lazy.Editor.EditorRes;
using Lazy.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor
{
    public class TextFieldDialog : EditorWindow
    {
        [NonSerialized]
        private string _resultString = string.Empty;

        [NonSerialized]
        private Action<string> _callback;

        [NonSerialized]
        private string _description;

        [NonSerialized]
        private List<TextValidator> _validatorList = new();

        [NonSerialized]
        private TextValidator _errorValidator = null;

        public static void OpenDialog(string title, string description, List<TextValidator> validatorList,
            Action<string> callback, EditorWindow targetWin = null)
        {
            TextFieldDialog window = ScriptableObject.CreateInstance<TextFieldDialog>();

            window.name = "TextFieldDialog '" + title + "'";
            window.titleContent = new GUIContent(title);
            window._description = description;
            window._callback = callback;
            window._validatorList = validatorList;
            window.position = new Rect(0, 0, 350, 140);

            window.ShowUtility();

            window.CenterOnWindow(targetWin);
            window.Focus();
            FocusWindowIfItsOpen<TextFieldDialog>();
        }

        private void OnGUI()
        {
            _errorValidator = null;

            Color defaultColor = GUI.contentColor;

            GUILayout.Space(20);
            EditorGUILayout.LabelField(_description);
            GUILayout.Space(20);

            GUI.SetNextControlName(name + "_textInput");
            _resultString = EditorGUILayout.TextField(_resultString, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            foreach (var val in _validatorList)
            {
                if (!val.Validate(_resultString))
                {
                    _errorValidator = val;
                    break;
                }
            }

            var lockOkButton = !(_errorValidator != null && _errorValidator.errorType == TextValidator.ErrorType.Error);

            GUILayout.BeginHorizontal();
            if (_errorValidator != null)
            {
                switch (_errorValidator.errorType)
                {
                    case TextValidator.ErrorType.Info:
                        GUI.contentColor = Styles.Colors.Blue;
                        GUILayout.Box(new GUIContent(EditorImageManager.Info, _errorValidator.failureMsg), Styles.icon);
                        break;
                    case TextValidator.ErrorType.Warning:
                        GUI.contentColor = Styles.Colors.Yellow;
                        GUILayout.Box(new GUIContent(EditorImageManager.Exclamation, _errorValidator.failureMsg), Styles.icon);
                        break;
                    case TextValidator.ErrorType.Error:
                        GUI.contentColor = Styles.Colors.Red;
                        GUILayout.Box(new GUIContent(EditorImageManager.Exclamation, _errorValidator.failureMsg), Styles.icon);
                        break;
                }

                GUI.contentColor = defaultColor;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(75f)))
            {
                Close();
            }

            GUI.enabled = lockOkButton;
            if (GUILayout.Button("OK", GUILayout.Width(75f)))
            {
                _callback?.Invoke(_resultString);
                Close();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // set focus only if element exist
            try
            {
                EditorGUI.FocusTextInControl(name+"_textInput");
            }
            catch (MissingReferenceException)
            {
            }

            if (UnityEngine.Event.current != null && UnityEngine.Event.current.isKey)
            {
                switch (UnityEngine.Event.current.keyCode)
                {
                    case KeyCode.Return:
                        if (lockOkButton)
                        {
                            _callback?.Invoke(_resultString);
                            Close();
                        }
                        break;
                    case KeyCode.Escape:
                        Close();
                        break;
                }
            }
        }
    }
}