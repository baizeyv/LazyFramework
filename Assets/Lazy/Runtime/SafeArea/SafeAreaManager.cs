using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lazy
{
    [ManagerUpdate]
    public class SafeAreaManager : Singleton<SafeAreaManager>, IManager
    {
        private static readonly HashSet<RectTransform> TransformSet = new(10);

        /// <summary>
        /// * 上一次的安全区
        /// </summary>
        private Rect _lastSafeArea;

        /// <summary>
        /// * 上一次的屏幕宽度
        /// </summary>
        private int _lastScreenWidth;

        /// <summary>
        /// * 上一次的屏幕高度
        /// </summary>
        private int _lastScreenHeight;

        /// <summary>
        /// * 是否应用过一次安全区了
        /// </summary>
        private bool _initFlag;

        private SafeAreaManager() { }

        public void Subscribe(RectTransform transform)
        {
            TransformSet.Add(transform);
            if (!_initFlag)
                UpdateRect();
            else
                // # 订阅时先执行一次
                ApplySafeArea(_lastSafeArea, _lastScreenWidth, _lastScreenHeight, transform);
        }

        public void Unsubscribe(RectTransform transform)
        {
            TransformSet.Remove(transform);
        }

        private void UpdateRect()
        {
            var safeArea = Screen.safeArea;
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            // # 判断是否是相同值
            if (
                safeArea.Equals(_lastSafeArea)
                && _lastScreenWidth == screenWidth
                && _lastScreenHeight == screenHeight
            )
                // # 与上一次的值相同,直接返回
                return;
            // # 应用安全区
            ApplySafeArea(safeArea, screenWidth, screenHeight, TransformSet.ToArray());
            _lastSafeArea = safeArea;
            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
            _initFlag = true;
        }

        /// <summary>
        /// * 应用安全区
        /// </summary>
        /// <param name="safeArea"></param>
        /// <param name="screenWidth"></param>
        /// <param name="screenHeight"></param>
        /// <param name="trans"></param>
        private void ApplySafeArea(
            Rect safeArea,
            int screenWidth,
            int screenHeight,
            params RectTransform[] trans
        )
        {
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            foreach (var rectTransform in trans)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchorMin = anchorMin.IsFinite() ? anchorMin : Vector2.zero;
                rectTransform.anchorMax = anchorMax.IsFinite() ? anchorMax : Vector2.zero;
            }
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease()
        {
            TransformSet.Clear();
        }

        public void OnGui() { }
    }
}
