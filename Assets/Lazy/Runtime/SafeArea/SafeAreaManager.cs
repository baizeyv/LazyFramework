using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ! 扩展安全区必须在 SimpleSafeArea 的层级之下,否则会有问题

namespace Lazy
{
    [ManagerUpdate]
    public class SafeAreaManager : Singleton<SafeAreaManager>, IManager
    {
        private static readonly HashSet<RectTransform> TransformSet = new(10);

        private static readonly Dictionary<RectTransform, Vector2> BottomExpandTransforms = new();

        private static readonly Dictionary<RectTransform, Vector2> TopExpandTransforms = new();

        private static readonly Dictionary<RectTransform, Vector2> LeftExpandTransforms = new();

        private static readonly Dictionary<RectTransform, Vector2> RightExpandTransforms = new();

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

        /// <summary>
        /// * 订阅安全区
        /// </summary>
        /// <param name="transform"></param>
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

        public void SubscribeBottomExpand(RectTransform transform)
        {
            BottomExpandTransforms.Add(transform, transform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyBottomExpand(transform);
        }

        public void UnsubscribeBottomExpand(RectTransform transform)
        {
            BottomExpandTransforms.Remove(transform);
        }

        public void SubscribeTopExpand(RectTransform transform)
        {
            TopExpandTransforms.Add(transform, transform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyTopExpand(transform);
        }

        public void UnsubscribeTopExpand(RectTransform transform)
        {
            TopExpandTransforms.Remove(transform);
        }

        public void SubscribeLeftExpand(RectTransform transform)
        {
            LeftExpandTransforms.Add(transform, transform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyLeftExpand(transform);
        }

        public void UnsubscribeLeftExpand(RectTransform transform)
        {
            LeftExpandTransforms.Remove(transform);
        }

        public void SubscribeRightExpand(RectTransform transform)
        {
            RightExpandTransforms.Add(transform, transform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyRightExpand(transform);
        }

        public void UnsubscribeRightExpand(RectTransform transform)
        {
            RightExpandTransforms.Remove(transform);
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
            ApplyTopExpand(TopExpandTransforms.Keys.ToArray());
            ApplyBottomExpand(BottomExpandTransforms.Keys.ToArray());
            ApplyLeftExpand(LeftExpandTransforms.Keys.ToArray());
            ApplyRightExpand(RightExpandTransforms.Keys.ToArray());
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

        private void ApplyBottomExpand(params RectTransform[] trans)
        {
            var yMin = _lastSafeArea.yMin;
            var xMin = _lastSafeArea.xMin;
            var rightOffset = _lastScreenWidth - _lastSafeArea.xMax;
            foreach (var rectTransform in trans)
            {
                // # 设置锚点
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.right;

                // # 设置pivot
                var pvt = rectTransform.pivot;
                pvt.x = 0.5f;
                pvt.y = 1;
                rectTransform.pivot = pvt;
                var v = BottomExpandTransforms[rectTransform];
                var ap = rectTransform.anchoredPosition;
                ap.y = v.y;
                rectTransform.anchoredPosition = ap;
                v.y += yMin;
                rectTransform.sizeDelta = v;

                // # 设置 Left Right
                var tmpOffsetMin = rectTransform.offsetMin;
                tmpOffsetMin.x = -xMin;
                rectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = rectTransform.offsetMax;
                tmpOffsetMax.x = rightOffset;
                rectTransform.offsetMax = tmpOffsetMax;
            }
        }

        private void ApplyTopExpand(params RectTransform[] trans)
        {
            var topOffset = _lastScreenHeight - _lastSafeArea.yMax;
            var xMin = _lastSafeArea.xMin;
            var rightOffset = _lastScreenWidth - _lastSafeArea.xMax;
            foreach (var rectTransform in trans)
            {
                // # 设置锚点
                rectTransform.anchorMin = Vector2.up;
                rectTransform.anchorMax = Vector2.one;

                // # 设置pivot
                var pvt = rectTransform.pivot;
                pvt.x = 0.5f;
                pvt.y = 0;
                rectTransform.pivot = pvt;
                var v = TopExpandTransforms[rectTransform];
                var ap = rectTransform.anchoredPosition;
                ap.y = -v.y;
                rectTransform.anchoredPosition = ap;
                v.y += topOffset;
                rectTransform.sizeDelta = v;

                // # 设置 Left Right
                var tmpOffsetMin = rectTransform.offsetMin;
                tmpOffsetMin.x = -xMin;
                rectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = rectTransform.offsetMax;
                tmpOffsetMax.x = rightOffset;
                rectTransform.offsetMax = tmpOffsetMax;
            }
        }

        private void ApplyLeftExpand(params RectTransform[] trans)
        {
            var xMin = _lastSafeArea.xMin;
            var yMin = _lastSafeArea.yMin;
            var topOffset = _lastScreenHeight - _lastSafeArea.yMax;
            foreach (var rectTransform in trans)
            {
                // # 设置锚点
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.up;

                // # 设置pivot
                var pvt = rectTransform.pivot;
                pvt.x = 1;
                pvt.y = 0.5f;
                rectTransform.pivot = pvt;
                var v = LeftExpandTransforms[rectTransform];
                var ap = rectTransform.anchoredPosition;
                ap.x = v.x;
                rectTransform.anchoredPosition = ap;
                v.x += xMin;
                rectTransform.sizeDelta = v;

                // # 设置 top bottom
                var tmpOffsetMin = rectTransform.offsetMin;
                tmpOffsetMin.y = -yMin;
                rectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = rectTransform.offsetMax;
                tmpOffsetMax.y = topOffset;
                rectTransform.offsetMax = tmpOffsetMax;
            }
        }

        private void ApplyRightExpand(params RectTransform[] trans)
        {
            var rightOffset = _lastScreenWidth - _lastSafeArea.xMax;
            var yMin = _lastSafeArea.yMin;
            var topOffset = _lastScreenHeight - _lastSafeArea.yMax;
            foreach (var rectTransform in trans)
            {
                // # 设置锚点
                rectTransform.anchorMin = Vector2.right;
                rectTransform.anchorMax = Vector2.one;

                // # 设置pivot
                var pvt = rectTransform.pivot;
                pvt.x = 0;
                pvt.y = 0.5f;
                rectTransform.pivot = pvt;
                var v = RightExpandTransforms[rectTransform];
                var ap = rectTransform.anchoredPosition;
                ap.x = -v.x;
                rectTransform.anchoredPosition = ap;
                v.x += rightOffset;
                rectTransform.sizeDelta = v;

                // # 设置 Top Bottom
                var tmpOffsetMin = rectTransform.offsetMin;
                tmpOffsetMin.y = -yMin;
                rectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = rectTransform.offsetMax;
                tmpOffsetMax.y = topOffset;
                rectTransform.offsetMax = tmpOffsetMax;
            }
        }

        public void OnUpdate()
        {
            UpdateRect();
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease()
        {
            TransformSet.Clear();
            TopExpandTransforms.Clear();
            BottomExpandTransforms.Clear();
            LeftExpandTransforms.Clear();
            RightExpandTransforms.Clear();
        }

        public void OnGui() { }
    }
}
