using UnityEngine;

namespace Lazy
{
    public class RightExpandSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.SubscribeRightExpand(_rectTransform);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.UnsubscribeRightExpand(_rectTransform);
        }
    }
}
