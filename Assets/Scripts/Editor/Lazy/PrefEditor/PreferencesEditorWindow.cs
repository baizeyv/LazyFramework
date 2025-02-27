using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Editor.Lazy.PrefEditor.SearchField;
using Lazy.Editor;
using Lazy.Editor.EditorRes;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Editor.Lazy.PrefEditor
{
    public class PreferencesEditorWindow : EditorWindow
    {
        private const string WindowName = "Preferences Editor";

        private const string ErrorValueString = "<bgTool_error_24072017>";

        private const int ErrorValueINT = int.MinValue;

#if UNITY_EDITOR_LINUX
        private readonly char[] invalidFilenameChars = { '"', '\\', '*', '/', ':', '<', '>', '?', '|' };
#elif UNITY_EDITOR_OSX
        private readonly char[] invalidFilenameChars = { '$', '%', '&', '\\', '/', ':', '<', '>', '|', '~' };
#else
        private readonly char[] invalidFilenameChars = { };
#endif

        /// <summary>
        /// * 本地存储路径
        /// </summary>
        private static string pathToPrefs = string.Empty;

        /// <summary>
        /// * 存储路径前缀 (不同平台的前缀不同)
        /// </summary>
        private static string platformPathPrefix = "~";

        private ABSPreferencesStorageAccessor _entryAccessor;

        private bool _updateView = false;

        private bool _monitoring = false;

        private PreferencesEntrySortOrderType _sortOrder = PreferencesEntrySortOrderType.None;

        private PreferencesSearchField _searchField;

        private string _searchText;

        private ReorderableList _userDefList;

        private ReorderableList _unityDefList;

        private string[] _userDef;

        private string[] _unityDef;

        private SerializedProperty[] _userDefListCache = new SerializedProperty[0];

        private PreferenceEntrySet _prefEntrySet;

        private SerializedObject _serializedObject;

        private float _relativeSplitterPos;

        private bool _showLoadingIndicatorOverlay = false;

        private bool _moveSplitterPos = false;

        private int _loadingSpinnerFrame;

        private Vector2 _scrollPos;

        private bool _showSystemGroup = false;

        private readonly List<TextValidator> _prefKeyValidatorList = new()
        {
            new TextValidator(TextValidator.ErrorType.Error,
                @"Invalid character detected. Only letters, numbers, space and ,.;:<>_|!§$%&/()=?*+~#-]+$ are allowed",
                @"(^$)|(^[a-zA-Z0-9 ,.;:<>_|!§$%&/()=?*+~#-]+$)"),
            new TextValidator(TextValidator.ErrorType.Warning,
                @"The given key already exist. The existing entry would be overwritten!",
                (key) => { return !PlayerPrefs.HasKey(key); })
        };

        [MenuItem("Lazy/Preferences Editor", false, 100)]
        public static void ShowWindow()
        {
            if (HasOpenInstances<PreferencesEditorWindow>())
            {
                // # 如果已经打开了就关闭
                GetWindow<PreferencesEditorWindow>(WindowName).Close();
            }
            else
            {
                var window = GetWindow<PreferencesEditorWindow>(WindowName);
                window.minSize = new Vector2(270f, 300f);
                window.name = WindowName;
                window.Show();
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR_WIN
            pathToPrefs = @"SOFTWARE\Unity\UnityEditor\" + PlayerSettings.companyName + @"\" +
                          PlayerSettings.productName;
            platformPathPrefix = "<CurrentUser>";
            _entryAccessor = new WindowsPreferencesStorageAccessor(pathToPrefs);
#elif UNITY_EDITOR_OSX
            pathToPrefs = @"Library/Preferences/unity." + MakeValidFileName(PlayerSettings.companyName) + "." +
                          MakeValidFileName(PlayerSettings.productName) + ".plist";
            _entryAccessor = new OSXPreferencesStorageAccessor(pathToPrefs);
            _entryAccessor.onStartLoading = () => { _showLoadingIndicatorOverlay = true; };
            _entryAccessor.onStopLoading = () => { _showLoadingIndicatorOverlay = false };
#elif UNITY_EDITOR_LINUX
            pathToPrefs = @".config/unity3d/" + MakeValidFileName(PlayerSettings.companyName) + "/" +
                          MakeValidFileName(PlayerSettings.productName) + "/prefs";
            _entryAccessor = new LinuxPreferencesStorageAccessor(pathToPrefs);
#endif

            _entryAccessor.onPrefEntryChanged += () => { _updateView = true; };
            _monitoring = LazyEditorPrefs.GetBool(EditorConstant.KeyWatchingForChanges, true);
            if (_monitoring)
                _entryAccessor.StartMonitoring();

            _sortOrder = (PreferencesEntrySortOrderType)LazyEditorPrefs.GetInt(EditorConstant.KeySortOrder, 0);
            _searchField = new();
            _searchField.OnDropdownSelection = () => { PrepareData(); };

            // Fix for serialisation issue of static fields
            if (_userDefList == null)
            {
                InitializeReorderedList();
                PrepareData();
            }
        }

        private void OnDisable()
        {
            _entryAccessor.StopMonitoring();
        }

        private void InitializeReorderedList()
        {
            if (_prefEntrySet == null)
            {
                var tmp = Resources.FindObjectsOfTypeAll<PreferenceEntrySet>();
                if (tmp.Length > 0)
                {
                    _prefEntrySet = tmp[0];
                }
                else
                {
                    _prefEntrySet = ScriptableObject.CreateInstance<PreferenceEntrySet>();
                }
            }

            if (_serializedObject == null)
            {
                _serializedObject = new(_prefEntrySet);
            }

            _userDefList = new(_serializedObject, _serializedObject.FindProperty("userDefList"), false, true, true,
                true);
            _unityDefList = new(_serializedObject, _serializedObject.FindProperty("unityDefList"), false, true, false,
                false);

            _relativeSplitterPos =
                LazyEditorPrefs.GetFloat(EditorConstant.KeyRelativeSplitterPosition, 100 / position.width);

            _userDefList.drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, "User Defined"); };
            _userDefList.drawElementBackgroundCallback = OnDrawElementBackgroundCallback;
            _userDefList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = GetUserDefListElementAtIndex(index, _userDefList.serializedProperty);
                var key = element.FindPropertyRelative("key");
                var type = element.FindPropertyRelative("typeSelection");

                SerializedProperty value;

                // Load only necessary type
                switch ((PreferenceEntry.PrefType)type.enumValueIndex)
                {
                    case PreferenceEntry.PrefType.Float:
                        value = element.FindPropertyRelative("floatValue");
                        break;
                    case PreferenceEntry.PrefType.Int:
                        value = element.FindPropertyRelative("intValue");
                        break;
                    case PreferenceEntry.PrefType.String:
                        value = element.FindPropertyRelative("stringValue");
                        break;
                    default:
                        value = element.FindPropertyRelative("This should never happen");
                        break;
                }

                var splitterPos = _relativeSplitterPos * rect.width;
                rect.y += 2;

                EditorGUI.BeginChangeCheck();
                var prefKeyName = key.stringValue;
                EditorGUI.LabelField(new(rect.x, rect.y, splitterPos - 1, EditorGUIUtility.singleLineHeight),
                    new GUIContent(prefKeyName, prefKeyName));
                GUI.enabled = false;
                EditorGUI.EnumPopup(new(rect.x + splitterPos + 1, rect.y, 60, EditorGUIUtility.singleLineHeight),
                    (PreferenceEntry.PrefType)type.enumValueIndex);
                GUI.enabled = !_showLoadingIndicatorOverlay;

                switch ((PreferenceEntry.PrefType)type.enumValueIndex)
                {
                    case PreferenceEntry.PrefType.Float:
                        EditorGUI.DelayedFloatField(
                            new(rect.x + splitterPos + 62, rect.y, rect.width - splitterPos - 60,
                                EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntry.PrefType.Int:
                        EditorGUI.DelayedIntField(
                            new(rect.x + splitterPos + 62, rect.y, rect.width - splitterPos - 60,
                                EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntry.PrefType.String:
                        EditorGUI.DelayedTextField(
                            new(rect.x + splitterPos + 62, rect.y, rect.width - splitterPos - 60,
                                EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _entryAccessor.IgnoreNextChange();
                    switch ((PreferenceEntry.PrefType)type.enumValueIndex)
                    {
                        case PreferenceEntry.PrefType.Float:
                            PlayerPrefs.SetFloat(key.stringValue, value.floatValue);
                            break;
                        case PreferenceEntry.PrefType.Int:
                            PlayerPrefs.SetInt(key.stringValue, value.intValue);
                            break;
                        case PreferenceEntry.PrefType.String:
                            PlayerPrefs.SetString(key.stringValue, value.stringValue);
                            break;
                    }

                    PlayerPrefs.Save();
                }
            };

            _userDefList.onRemoveCallback = (l) =>
            {
                _userDefList.ReleaseKeyboardFocus();
                _unityDefList.ReleaseKeyboardFocus();

                var prefKey = l.serializedProperty.GetArrayElementAtIndex(l.index).FindPropertyRelative("key")
                    .stringValue;
                if (EditorUtility.DisplayDialog("Warning!",
                        $"Are you sure you want to delete this entry from PlayerPrefs?\n\nEntry: {prefKey}", "Yes",
                        "No"))
                {
                    _entryAccessor.IgnoreNextChange();
                    PlayerPrefs.DeleteKey(prefKey);
                    PlayerPrefs.Save();
                    ReorderableList.defaultBehaviours.DoRemoveButton(l);
                    PrepareData();
                    GUIUtility.ExitGUI();
                }
            };

            _userDefList.onAddDropdownCallback = (buttonRect, l) =>
            {
                var menu = new GenericMenu();
                foreach (PreferenceEntry.PrefType type in Enum.GetValues(typeof(PreferenceEntry.PrefType)))
                {
                    menu.AddItem(new GUIContent(type.ToString()), false, () =>
                    {
                        TextFieldDialog.OpenDialog("Create new property", "Key for the new property:",
                            _prefKeyValidatorList,
                            key =>
                            {
                                _entryAccessor.IgnoreNextChange();

                                switch (type)
                                {
                                    case PreferenceEntry.PrefType.Float:
                                        PlayerPrefs.SetFloat(key, 0f);
                                        break;
                                    case PreferenceEntry.PrefType.Int:
                                        PlayerPrefs.SetInt(key, 0);
                                        break;
                                    case PreferenceEntry.PrefType.String:
                                        PlayerPrefs.SetString(key, string.Empty);
                                        break;
                                }

                                PlayerPrefs.Save();
                                PrepareData();
                                Focus();
                            }, this);
                    });
                }

                menu.ShowAsContext();
            };

            _unityDefList.drawElementCallback = OnDrawElementBackgroundCallback;
            _unityDefList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = _unityDefList.serializedProperty.GetArrayElementAtIndex(index);
                var key = element.FindPropertyRelative("key");
                var type = element.FindPropertyRelative("typeSelection");

                SerializedProperty value;

                // Load only necessary type
                switch ((PreferenceEntry.PrefType)type.enumValueIndex)
                {
                    case PreferenceEntry.PrefType.Float:
                        value = element.FindPropertyRelative("floatValue");
                        break;
                    case PreferenceEntry.PrefType.Int:
                        value = element.FindPropertyRelative("intValue");
                        break;
                    case PreferenceEntry.PrefType.String:
                        value = element.FindPropertyRelative("stringValue");
                        break;
                    default:
                        value = element.FindPropertyRelative("This should never happen");
                        break;
                }

                var splitterPos = _relativeSplitterPos * rect.width;
                rect.y += 2;

                GUI.enabled = false;
                var prefKeyName = key.stringValue;
                EditorGUI.LabelField(new(rect.x, rect.y, splitterPos - 1, EditorGUIUtility.singleLineHeight),
                    new GUIContent(prefKeyName, prefKeyName));
                EditorGUI.EnumPopup(new(rect.x + splitterPos + 1, rect.y, 60, EditorGUIUtility.singleLineHeight),
                    (PreferenceEntry.PrefType)type.enumValueIndex);
                switch ((PreferenceEntry.PrefType)type.enumValueIndex)
                {
                    case PreferenceEntry.PrefType.Float:
                        EditorGUI.DelayedFloatField(
                            new Rect(rect.x + splitterPos + 62, rect.y, rect.width - splitterPos - 60,
                                EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntry.PrefType.Int:
                        EditorGUI.DelayedIntField(
                            new Rect(rect.x + splitterPos + 62, rect.y, rect.width - splitterPos - 60,
                                EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntry.PrefType.String:
                        EditorGUI.DelayedTextField(
                            new Rect(rect.x + splitterPos + 62, rect.y, rect.width - splitterPos - 60,
                                EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                }

                GUI.enabled = !_showLoadingIndicatorOverlay;
            };
            _unityDefList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Unity Defined"); };
        }

        private void PrepareData(bool reloadKeys = true)
        {
            _prefEntrySet.ClearLists();

            LoadKeys(out _userDef, out _unityDef, reloadKeys);

            CreatePrefEntries(_userDef, ref _prefEntrySet.userDefList);
            CreatePrefEntries(_unityDef, ref _prefEntrySet.unityDefList);

            // # clear cache
            _userDefListCache = new SerializedProperty[_prefEntrySet.userDefList.Count];
        }

        private void CreatePrefEntries(string[] keySource, ref List<PreferenceEntry> listDest)
        {
            if (!string.IsNullOrEmpty(_searchText) && _searchField.SearchMode ==
                PreferencesSearchField.SearchModePreferencesEditorWindow.Key)
            {
                keySource = keySource.Where(keyEntry => keyEntry.ToLower().Contains(_searchText.ToLower())).ToArray();
            }

            foreach (var key in keySource)
            {
                var entry = new PreferenceEntry();
                entry.key = key;

                var s = PlayerPrefs.GetString(key, ErrorValueString);
                if (s != ErrorValueString)
                {
                    entry.stringValue = s;
                    entry.typeSelection = PreferenceEntry.PrefType.String;
                    listDest.Add(entry);
                    continue;
                }

                var f = PlayerPrefs.GetFloat(key, float.NaN);
                if (!float.IsNaN(f))
                {
                    entry.floatValue = f;
                    entry.typeSelection = PreferenceEntry.PrefType.Float;
                    listDest.Add(entry);
                    continue;
                }

                int i = PlayerPrefs.GetInt(key, ErrorValueINT);
                if (i != ErrorValueINT)
                {
                    entry.intValue = i;
                    entry.typeSelection = PreferenceEntry.PrefType.Int;
                    listDest.Add(entry);
                    continue;
                }
            }

            if (!string.IsNullOrEmpty(_searchText) && _searchField.SearchMode ==
                PreferencesSearchField.SearchModePreferencesEditorWindow.Value)
            {
                listDest = listDest.Where(entry => entry.ValueAsString().ToLower().Contains(_searchText.ToLower()))
                    .ToList();
            }

            switch (_sortOrder)
            {
                case PreferencesEntrySortOrderType.Ascending:
                    listDest.Sort((x, y) => string.Compare(x.key, y.key, StringComparison.Ordinal));
                    break;
                case PreferencesEntrySortOrderType.Descending:
                    listDest.Sort((x, y) => string.Compare(y.key, x.key, StringComparison.Ordinal));
                    break;
            }
        }

        private SerializedProperty GetUserDefListElementAtIndex(int index, SerializedProperty listProperty)
        {
            Assert.IsTrue(listProperty.isArray, "Given 'listProperties' is not type of array");
            if (_userDefListCache[index] == null)
            {
                _userDefListCache[index] = listProperty.GetArrayElementAtIndex(index);
            }

            return _userDefListCache[index];
        }

        private void OnDrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (Event.current.type == EventType.Repaint)
            {
                ReorderableList.defaultBehaviours.elementBackground.Draw(rect, false, isActive, isActive, isFocused);
            }

            var splitterRect = new Rect(rect.x + _relativeSplitterPos * rect.width, rect.y, 2, rect.height);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _moveSplitterPos = true;
            }

            if (_moveSplitterPos)
            {
                if (Event.current.mousePosition.x > 100 && Event.current.mousePosition.x < rect.width - 120)
                {
                    _relativeSplitterPos = Event.current.mousePosition.x / rect.width;
                    Repaint();
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                _moveSplitterPos = false;
                LazyEditorPrefs.SetFloat(EditorConstant.KeyRelativeSplitterPosition, _relativeSplitterPos);
            }
        }

        private void LoadKeys(out string[] userDef, out string[] unityDef, bool reloadKeys)
        {
            var keys = _entryAccessor.GetKeys(reloadKeys);
            var groups = keys.GroupBy(key => key.StartsWith("unity.") || key.StartsWith("UnityGraphicsQuality"))
                .ToDictionary(g => g.Key, g => g.ToList());
            userDef = groups.ContainsKey(false) ? groups[false].ToArray() : Array.Empty<string>();
            unityDef = groups.ContainsKey(true) ? groups[true].ToArray() : Array.Empty<string>();
        }

        private void Update()
        {
            if (_showLoadingIndicatorOverlay)
            {
                _loadingSpinnerFrame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                PrepareData();
                Repaint();
            }

            if (_updateView)
            {
                _updateView = false;
                PrepareData();
                Repaint();
            }
        }

        private void OnGUI()
        {
            try
            {
                if (_showLoadingIndicatorOverlay)
                {
                    GUI.enabled = false;
                }

                var defaultColor = GUI.contentColor;
                if (!EditorGUIUtility.isProSkin)
                {
                    GUI.contentColor = Styles.Colors.DarkGray;
                }

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUI.BeginChangeCheck();
                _searchText = _searchField.OnToolbarGUI(_searchText);
                if (EditorGUI.EndChangeCheck())
                {
                    PrepareData(false);
                }

                GUILayout.FlexibleSpace();
                EditorGUIUtility.SetIconSize(new Vector2(14f, 14f));

                GUIContent sortOrderContent;
                switch (_sortOrder)
                {
                    case PreferencesEntrySortOrderType.Ascending:
                        sortOrderContent = new GUIContent(EditorImageManager.SortAsscending, "Ascending sorted");
                        break;
                    case PreferencesEntrySortOrderType.Descending:
                        sortOrderContent = new GUIContent(EditorImageManager.SortDescending, "Descending sorted");
                        break;
                    case PreferencesEntrySortOrderType.None:
                    default:
                        sortOrderContent = new GUIContent(EditorImageManager.SortDisabled, "Not sorted");
                        break;
                }

                if (GUILayout.Button(sortOrderContent, EditorStyles.toolbarButton))
                {
                    _sortOrder++;
                    if ((int)_sortOrder >= Enum.GetValues(typeof(PreferencesEntrySortOrderType)).Length)
                    {
                        _sortOrder = 0;
                    }

                    LazyEditorPrefs.SetInt(EditorConstant.KeySortOrder, (int)_sortOrder);
                    PrepareData(false);
                }

                var watcherContent = (_entryAccessor.IsMonitoring())
                    ? new GUIContent(EditorImageManager.Watching, "Watching changes")
                    : new GUIContent(EditorImageManager.NotWatching, "Not watching changes");
                if (GUILayout.Button(watcherContent, EditorStyles.toolbarButton))
                {
                    _monitoring = !_monitoring;
                    LazyEditorPrefs.SetBool(EditorConstant.KeyWatchingForChanges, _monitoring);
                    if (_monitoring)
                    {
                        _entryAccessor.StartMonitoring();
                    }
                    else
                    {
                        _entryAccessor.StopMonitoring();
                    }

                    Repaint();
                }

                if (GUILayout.Button(new GUIContent(EditorImageManager.Refresh, "Refresh"), EditorStyles.toolbarButton))
                {
                    PlayerPrefs.Save();
                    PrepareData();
                }

                if (GUILayout.Button(new GUIContent(EditorImageManager.Trash, "Delete all"),
                        EditorStyles.toolbarButton))
                {
                    if (EditorUtility.DisplayDialog("Warning!",
                            "Are you sure you want to delete ALL entries from PlayerPrefs?\n\nUse with caution! Unity defined keys are affected too.",
                            "YES", "NO"))
                    {
                        PlayerPrefs.DeleteAll();
                        PrepareData();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUIUtility.SetIconSize(new Vector2(0f, 0f));
                GUILayout.EndHorizontal();

                GUILayout.Space(3);

                GUILayout.BeginHorizontal();

                GUILayout.Box(EditorImageManager.GetOsIcon(), Styles.icon);
                GUILayout.TextField(platformPathPrefix + Path.DirectorySeparatorChar + pathToPrefs,
                    GUILayout.MinWidth(200));
                GUILayout.EndHorizontal();

                GUILayout.Space(3);

                _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                _serializedObject.Update();
                _userDefList.DoLayoutList();
                _serializedObject.ApplyModifiedProperties();

                GUILayout.FlexibleSpace();

                _showSystemGroup = EditorGUILayout.Foldout(_showSystemGroup, new GUIContent("Show System"));
                if (_showSystemGroup)
                {
                    _unityDefList.DoLayoutList();
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUI.enabled = true;

                if (_showLoadingIndicatorOverlay)
                {
                    GUILayout.BeginArea(new Rect(position.size.x * 0.5f - 30, position.size.y * 0.5f - 25, 60, 50),
                        GUI.skin.box);
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Box(EditorImageManager.SpinWheelIcons[_loadingSpinnerFrame], Styles.icon);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Loading");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.FlexibleSpace();
                    GUILayout.EndArea();
                }

                GUI.contentColor = defaultColor;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private string MakeValidFileName(string unsafeFileName)
        {
            string normalizedFileName = unsafeFileName.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            // We need to use a TextElementEmumerator in order to support UTF16 characters that may take up more than one char(case 1169358)
            TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(normalizedFileName);
            while (charEnum.MoveNext())
            {
                string c = charEnum.GetTextElement();
                if (c.Length == 1 && invalidFilenameChars.Contains(c[0]))
                {
                    stringBuilder.Append('_');
                    continue;
                }

                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c, 0);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}