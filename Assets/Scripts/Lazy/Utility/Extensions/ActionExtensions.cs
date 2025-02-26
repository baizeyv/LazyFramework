using System;

namespace Lazy.Utility
{
    public static class ActionExtensions
    {
        public static void Fire(this Action self)
        {
            self?.Invoke();
        }
    }
}