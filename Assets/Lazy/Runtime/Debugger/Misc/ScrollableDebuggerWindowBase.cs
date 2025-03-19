using System;
using Lazy.Debugger.Module;
using Lazy.Runtime.Utility;
using Lazy.Utility;
using UnityEngine;

namespace Lazy.Debugger.Misc
{
    public abstract class ScrollableDebuggerWindowBase : IDebuggerWindow
    {
        private const float TitleWidth = 240f;

        private Vector2 _scrollPosition = Vector2.zero;

        public virtual void Initialize(params object[] args) { }

        public virtual void Shutdown() { }

        public virtual void OnEnter() { }

        public virtual void OnLeave() { }

        public virtual void OnProcess(float elapsedSeconds, float realElapsedSeconds) { }

        public void OnDraw()
        {
            OnBeforeDrawScroll();
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            {
                OnDrawScrollableWindow();
            }
            GUILayout.EndScrollView();
            OnAfterDrawScroll();
        }

        protected virtual void OnBeforeDrawScroll()
        {
            GUILayout.Space(5);
        }

        protected virtual void OnAfterDrawScroll() { }

        protected abstract void OnDrawScrollableWindow();

        protected static void DrawTagLabel(string tag, string content)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(tag, GUILayout.Width(TitleWidth));
                if (GUILayout.Button(content, "label"))
                    Debugger.CopyToClipboard(content);
            }
            GUILayout.EndHorizontal();
        }

        protected static void DrawButton(
            string title,
            Action callback,
            string color = "#ffffff",
            float height = 40f
        )
        {
            if (
                GUILayout.Button($"<color={color}><b>{title}</b></color>", GUILayout.Height(height))
            )
                callback.Fire();
        }

        protected static void DrawToggle(
            string title,
            bool condition,
            Action trueCallback,
            Action falseCallback,
            string color = "#ffffff",
            float height = 40f
        )
        {
            var style = new GUIStyle(GUI.skin.button);
            style.fixedHeight = height;
            if (GUILayout.Toggle(condition, $"<color={color}><b>{title}</b></color>", style))
                trueCallback.Fire();
            else
                falseCallback.Fire();
        }

        protected static string GetByteLengthString(long byteLength)
        {
            if (byteLength < 1024L) // 2 ^ 10
                return TextUtility.Format("{0} Bytes", byteLength.ToString());

            if (byteLength < 1048576L) // 2 ^ 20
                return TextUtility.Format("{0} KB", (byteLength / 1024f).ToString("F2"));

            if (byteLength < 1073741824L) // 2 ^ 30
                return TextUtility.Format("{0} MB", (byteLength / 1048576f).ToString("F2"));

            if (byteLength < 1099511627776L) // 2 ^ 40
                return TextUtility.Format("{0} GB", (byteLength / 1073741824f).ToString("F2"));

            if (byteLength < 1125899906842624L) // 2 ^ 50
                return TextUtility.Format("{0} TB", (byteLength / 1099511627776f).ToString("F2"));

            if (byteLength < 1152921504606846976L) // 2 ^ 60
                return TextUtility.Format("{0} PB", (byteLength / 1125899906842624f).ToString("F2"));

            return TextUtility.Format("{0} EB", (byteLength / 1152921504606846976f).ToString("F2"));
        }
    }
}
