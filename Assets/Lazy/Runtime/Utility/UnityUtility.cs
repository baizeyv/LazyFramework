using System;
using System.Collections;
using Lazy;
using Lazy.Runtime.Utility.Coroutine;

namespace Lazy.Runtime.Utility
{
    public static class CoroutineCenter
    {
        private static CoroutineHelper _coroutineHelper;

        private static CoroutineHelper CoroutineHelper
        {
            get
            {
                if (_coroutineHelper != null)
                    return _coroutineHelper;
                var behaviour = ManagerCenter.GetBehaviour();
                _coroutineHelper = behaviour.gameObject.GetOrAddComponent<CoroutineHelper>();

                return _coroutineHelper;
            }
        }

        public static UnityEngine.Coroutine StartCoroutine(
            UnityEngine.Coroutine coroutine,
            Action callback = null
        )
        {
            return CoroutineHelper.StartCoroutine(coroutine, callback);
        }

        public static UnityEngine.Coroutine StartCoroutine(IEnumerator coroutine)
        {
            return CoroutineHelper.StartCoroutine(coroutine);
        }
    }
}
