using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// ! Top-Center
    /// </summary>
    public class TopExpandSafeArea : MonoBehaviour
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
            SafeAreaManager.Instance.SubscribeTopExpand(this);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeTopExpand(this);
        }
    }
}
