using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lazy.Runtime.UI.Basic
{
    [Serializable]
    public class ExtraBind
    {
        public string memberName;

        public UnityEngine.Object obj;
    }

    [RequireComponent(typeof(ViewPresenter))]
    public class ExtraBinds : MonoBehaviour
    {
        public List<ExtraBind> binds = new();
    }
}
