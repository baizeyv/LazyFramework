using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lazy.Rx
{
    public static class CustomEqualityComparer
    {
        public static ColorEqualityComparer Color = new ColorEqualityComparer();
    }

    public class ColorEqualityComparer : IEqualityComparer<Color>
    {
        public bool Equals(Color x, Color y)
        {
            return x.r.Equals(y.r)
                   && x.g.Equals(y.g)
                   && x.b.Equals(y.b)
                   && x.a.Equals(y.a);
        }

        public int GetHashCode(Color obj)
        {
            var hashCode = new HashCode();
            hashCode.Add(obj.r);
            hashCode.Add(obj.g);
            hashCode.Add(obj.b);
            hashCode.Add(obj.a);
            return hashCode.ToHashCode();
        }
    }
}