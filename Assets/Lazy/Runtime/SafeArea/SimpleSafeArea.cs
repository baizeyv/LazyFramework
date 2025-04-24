using System;
using UnityEngine;

namespace Lazy
{
    public class SimpleSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaManager.Instance.Subscribe(_rectTransform);
        }

        private void OnDisable()
        {
            SafeAreaManager.Instance.Unsubscribe(_rectTransform);
        }
    }
}
