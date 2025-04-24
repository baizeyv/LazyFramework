using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// ! Bottom-Center
    /// </summary>
    public class BottomExpandSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.SubscribeBottomExpand(_rectTransform);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeBottomExpand(_rectTransform);
        }
    }
}
