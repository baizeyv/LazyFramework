using System;

namespace Lazy.Rx
{
    public class Disposable
    {
        public static readonly IDisposable Empty = new EmptyDisposable();
    }

    internal sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}