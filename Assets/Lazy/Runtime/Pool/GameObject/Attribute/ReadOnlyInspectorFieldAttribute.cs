using System;
using UnityEngine;

namespace Lazy.Pool.Attribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReadOnlyInspectorFieldAttribute : PropertyAttribute { }
}
