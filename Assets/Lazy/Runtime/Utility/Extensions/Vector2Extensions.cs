using UnityEngine;

namespace Lazy
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// * 判断其是否无限
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool IsFinite(this Vector2 v)
        {
            return v.x.IsFinite() && v.y.IsFinite();
        }

        public static bool IsFinite(this float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
    }
}
