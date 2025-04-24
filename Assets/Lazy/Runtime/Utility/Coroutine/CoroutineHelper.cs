using System;
using System.Collections;
using UnityEngine;

namespace Lazy
{
    [DisallowMultipleComponent]
    public class CoroutineHelper : MonoBehaviour
    {
        public Coroutine StartCoroutine(Coroutine coroutine, Action callback = null)
        {
            return StartCoroutine(NormalCoroutine(coroutine, callback));
        }

        private IEnumerator NormalCoroutine(Coroutine coroutine, Action callback = null)
        {
            yield return coroutine;
            callback?.Invoke();
        }
    }
}
