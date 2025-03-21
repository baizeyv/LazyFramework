using System;
using Lazy.UI.Basic;
using UnityEngine;

namespace Lazy
{
    public class ViewPresenter : MonoBehaviour, IBindGroup
    {
        [HideInInspector]
        public string nameSpace = string.Empty;

        [HideInInspector]
        public string scriptName;

        [HideInInspector]
        public string scriptsFolder = string.Empty;

        [HideInInspector]
        public bool generatePrefab = false;

        [HideInInspector]
        public string prefabFolder = string.Empty;

        [HideInInspector]
        public string appFullTypeName = string.Empty;

        [HideInInspector]
        public string viewPresenterFullTypeName = string.Empty;

        public string TemplateName => nameof(ViewPresenter);
    }

    public class ViewPresenterChildAttribute : Attribute { }
}
