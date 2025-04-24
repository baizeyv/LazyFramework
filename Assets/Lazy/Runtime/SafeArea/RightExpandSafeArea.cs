using UnityEngine;

namespace Lazy
{
    public class RightExpandSafeArea : MonoBehaviour
    {
        /// <summary>
        /// * 上下边缘扩展至安全区外
        /// </summary>
        public bool edgeExpand = true;

        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.SubscribeRightExpand(this);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeRightExpand(this);
        }
    }
}
