using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// ! Bottom-Center
    /// </summary>
    public class BottomExpandSafeArea : MonoBehaviour
    {
        /// <summary>
        /// * 左右边缘扩展至安全区外
        /// </summary>
        public bool edgeExpand = true;

        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.SubscribeBottomExpand(this);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeBottomExpand(this);
        }
    }
}
