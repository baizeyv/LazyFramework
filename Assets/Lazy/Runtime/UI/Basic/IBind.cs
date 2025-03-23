using UnityEngine;

namespace Lazy
{
    public interface IBind
    {
        string TypeName { get; }

        string Comment { get; }

        Transform Transform { get; }

        BindType BindType { get; }
    }
}
