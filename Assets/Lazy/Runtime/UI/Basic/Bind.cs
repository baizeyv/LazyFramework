using Lazy;
using UnityEngine;

namespace Lazy
{
    [AddComponentMenu("Lazy/Bind")]
    public class Bind : ABSBind
    {
        [ReadOnlyInspectorField]
        public string propertyName;
    }
}
