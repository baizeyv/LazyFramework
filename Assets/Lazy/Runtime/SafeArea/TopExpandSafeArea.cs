using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// ! Top-Center (Alt+Shift)
    /// </summary>
    public class TopExpandSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.SubscribeTopExpand(_rectTransform);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeTopExpand(_rectTransform);
        }
    }
}
