using System;
using UnityEngine;

namespace Lazy
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReadOnlyInspectorFieldAttribute : PropertyAttribute { }
}
