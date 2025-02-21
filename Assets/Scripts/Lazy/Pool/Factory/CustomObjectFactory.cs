using System;

namespace Lazy.Pool.Factory
{
    public class CustomObjectFactory<T> : IObjectFactory<T>
    {
        private readonly Func<T> _factoryMethod;

        public CustomObjectFactory(Func<T> factoryMethod)
        {
            _factoryMethod = factoryMethod;
        }

        public T Create()
        {
            return _factoryMethod();
        }
    }
}