using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.Lazy.PrefEditor
{
    /// <summary>
    /// * 本地存储条目集合
    /// </summary>
    [Serializable]
    public class PreferenceEntrySet : ScriptableObject
    {
        /// <summary>
        /// * 用户定义的本地存储列表
        /// </summary>
        public List<PreferenceEntry> userDefList;

        /// <summary>
        /// * Unity 定义的本地存储列表
        /// </summary>
        public List<PreferenceEntry> unityDefList;

        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            userDefList ??= new List<PreferenceEntry>();
            unityDefList ??= new List<PreferenceEntry>();
        }

        public void ClearLists()
        {
            userDefList?.Clear();
            unityDefList?.Clear();
        }
    }
}