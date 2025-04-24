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

        private static readonly Dictionary<BottomExpandSafeArea, Vector2> BottomExpandTransforms =
            new();

        private static readonly Dictionary<TopExpandSafeArea, Vector2> TopExpandTransforms = new();

        private static readonly Dictionary<LeftExpandSafeArea, Vector2> LeftExpandTransforms =
            new();

        private static readonly Dictionary<RightExpandSafeArea, Vector2> RightExpandTransforms =
            new();

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

        public void SubscribeBottomExpand(BottomExpandSafeArea transform)
        {
            BottomExpandTransforms.Add(transform, transform.RectTransform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyBottomExpand(transform);
        }

        public void UnsubscribeBottomExpand(BottomExpandSafeArea transform)
        {
            BottomExpandTransforms.Remove(transform);
        }

        public void SubscribeTopExpand(TopExpandSafeArea transform)
        {
            TopExpandTransforms.Add(transform, transform.RectTransform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyTopExpand(transform);
        }

        public void UnsubscribeTopExpand(TopExpandSafeArea transform)
        {
            TopExpandTransforms.Remove(transform);
        }

        public void SubscribeLeftExpand(LeftExpandSafeArea transform)
        {
            LeftExpandTransforms.Add(transform, transform.RectTransform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyLeftExpand(transform);
        }

        public void UnsubscribeLeftExpand(LeftExpandSafeArea transform)
        {
            LeftExpandTransforms.Remove(transform);
        }

        public void SubscribeRightExpand(RightExpandSafeArea transform)
        {
            RightExpandTransforms.Add(transform, transform.RectTransform.sizeDelta);
            if (!_initFlag)
                UpdateRect();
            else
                ApplyRightExpand(transform);
        }

        public void UnsubscribeRightExpand(RightExpandSafeArea transform)
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

        private void ApplyBottomExpand(params BottomExpandSafeArea[] trans)
        {
            var yMin = _lastSafeArea.yMin;
            var xMin = _lastSafeArea.xMin;
            var rightOffset = _lastScreenWidth - _lastSafeArea.xMax;
            foreach (var expandSafeArea in trans)
            {
                // # 设置锚点
                expandSafeArea.RectTransform.anchorMin = Vector2.zero;
                expandSafeArea.RectTransform.anchorMax = Vector2.right;

                // # 设置pivot
                var pvt = expandSafeArea.RectTransform.pivot;
                pvt.x = 0.5f;
                pvt.y = 1;
                expandSafeArea.RectTransform.pivot = pvt;
                var v = BottomExpandTransforms[expandSafeArea];
                var ap = expandSafeArea.RectTransform.anchoredPosition;
                ap.y = v.y;
                expandSafeArea.RectTransform.anchoredPosition = ap;
                v.y += yMin;
                expandSafeArea.RectTransform.sizeDelta = v;

                // # 设置 Left Right
                var tmpOffsetMin = expandSafeArea.RectTransform.offsetMin;
                tmpOffsetMin.x = expandSafeArea.edgeExpand ? -xMin : 0;
                expandSafeArea.RectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = expandSafeArea.RectTransform.offsetMax;
                tmpOffsetMax.x = expandSafeArea.edgeExpand ? rightOffset : 0;
                expandSafeArea.RectTransform.offsetMax = tmpOffsetMax;
            }
        }

        private void ApplyTopExpand(params TopExpandSafeArea[] trans)
        {
            var topOffset = _lastScreenHeight - _lastSafeArea.yMax;
            var xMin = _lastSafeArea.xMin;
            var rightOffset = _lastScreenWidth - _lastSafeArea.xMax;
            foreach (var expandSafeArea in trans)
            {
                // # 设置锚点
                expandSafeArea.RectTransform.anchorMin = Vector2.up;
                expandSafeArea.RectTransform.anchorMax = Vector2.one;

                // # 设置pivot
                var pvt = expandSafeArea.RectTransform.pivot;
                pvt.x = 0.5f;
                pvt.y = 0;
                expandSafeArea.RectTransform.pivot = pvt;
                var v = TopExpandTransforms[expandSafeArea];
                var ap = expandSafeArea.RectTransform.anchoredPosition;
                ap.y = -v.y;
                expandSafeArea.RectTransform.anchoredPosition = ap;
                v.y += topOffset;
                expandSafeArea.RectTransform.sizeDelta = v;

                // # 设置 Left Right
                var tmpOffsetMin = expandSafeArea.RectTransform.offsetMin;
                tmpOffsetMin.x = expandSafeArea.edgeExpand ? -xMin : 0;
                expandSafeArea.RectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = expandSafeArea.RectTransform.offsetMax;
                tmpOffsetMax.x = expandSafeArea.edgeExpand ? rightOffset : 0;
                expandSafeArea.RectTransform.offsetMax = tmpOffsetMax;
            }
        }

        private void ApplyLeftExpand(params LeftExpandSafeArea[] trans)
        {
            var xMin = _lastSafeArea.xMin;
            var yMin = _lastSafeArea.yMin;
            var topOffset = _lastScreenHeight - _lastSafeArea.yMax;
            foreach (var expandSafeArea in trans)
            {
                // # 设置锚点
                expandSafeArea.RectTransform.anchorMin = Vector2.zero;
                expandSafeArea.RectTransform.anchorMax = Vector2.up;

                // # 设置pivot
                var pvt = expandSafeArea.RectTransform.pivot;
                pvt.x = 1;
                pvt.y = 0.5f;
                expandSafeArea.RectTransform.pivot = pvt;
                var v = LeftExpandTransforms[expandSafeArea];
                var ap = expandSafeArea.RectTransform.anchoredPosition;
                ap.x = v.x;
                expandSafeArea.RectTransform.anchoredPosition = ap;
                v.x += xMin;
                expandSafeArea.RectTransform.sizeDelta = v;

                // # 设置 top bottom
                var tmpOffsetMin = expandSafeArea.RectTransform.offsetMin;
                tmpOffsetMin.y = expandSafeArea.edgeExpand ? -yMin : 0;
                expandSafeArea.RectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = expandSafeArea.RectTransform.offsetMax;
                tmpOffsetMax.y = expandSafeArea.edgeExpand ? topOffset : 0;
                expandSafeArea.RectTransform.offsetMax = tmpOffsetMax;
            }
        }

        private void ApplyRightExpand(params RightExpandSafeArea[] trans)
        {
            var rightOffset = _lastScreenWidth - _lastSafeArea.xMax;
            var yMin = _lastSafeArea.yMin;
            var topOffset = _lastScreenHeight - _lastSafeArea.yMax;
            foreach (var expandSafeArea in trans)
            {
                // # 设置锚点
                expandSafeArea.RectTransform.anchorMin = Vector2.right;
                expandSafeArea.RectTransform.anchorMax = Vector2.one;

                // # 设置pivot
                var pvt = expandSafeArea.RectTransform.pivot;
                pvt.x = 0;
                pvt.y = 0.5f;
                expandSafeArea.RectTransform.pivot = pvt;
                var v = RightExpandTransforms[expandSafeArea];
                var ap = expandSafeArea.RectTransform.anchoredPosition;
                ap.x = -v.x;
                expandSafeArea.RectTransform.anchoredPosition = ap;
                v.x += rightOffset;
                expandSafeArea.RectTransform.sizeDelta = v;

                // # 设置 Top Bottom
                var tmpOffsetMin = expandSafeArea.RectTransform.offsetMin;
                tmpOffsetMin.y = expandSafeArea.edgeExpand ? -yMin : 0;
                expandSafeArea.RectTransform.offsetMin = tmpOffsetMin;
                var tmpOffsetMax = expandSafeArea.RectTransform.offsetMax;
                tmpOffsetMax.y = expandSafeArea.edgeExpand ? topOffset : 0;
                expandSafeArea.RectTransform.offsetMax = tmpOffsetMax;
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
