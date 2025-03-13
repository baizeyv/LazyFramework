using System;
using System.Collections;
using UnityEngine;

namespace Lazy.Runtime.Utility.Coroutine
{
    [DisallowMultipleComponent]
    public class CoroutineHelper : MonoBehaviour
    {
        public UnityEngine.Coroutine StartCoroutine(
            UnityEngine.Coroutine coroutine,
            Action callback = null
        )
        {
            return StartCoroutine(NormalCoroutine(coroutine, callback));
        }

        private IEnumerator NormalCoroutine(UnityEngine.Coroutine coroutine, Action callback = null)
        {
            yield return coroutine;
            callback?.Invoke();
        }
    }
}
