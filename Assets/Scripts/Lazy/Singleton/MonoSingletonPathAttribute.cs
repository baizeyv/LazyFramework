using System;

namespace Lazy.Singleton
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MonoSingletonPathAttribute : Attribute
    {
        public string PathInHierarchy { get; private set; }

        public MonoSingletonPathAttribute(string pathInHierarchy)
        {
            PathInHierarchy = pathInHierarchy;
        }
    }
}