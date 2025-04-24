using UnityEngine;

namespace Lazy
{
    public class LeftExpandSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.SubscribeLeftExpand(_rectTransform);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeLeftExpand(_rectTransform);
        }
    }
}
