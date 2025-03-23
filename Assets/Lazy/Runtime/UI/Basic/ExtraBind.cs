using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lazy
{
    [Serializable]
    public class ExtraBind
    {
        public string memberName;

        [HideInInspector]
        public string propertyName;

        public UnityEngine.Object obj;
    }

    [RequireComponent(typeof(ViewPresenter))]
    public class ExtraBinds : MonoBehaviour
    {
        public List<ExtraBind> binds = new();
    }
}
