using System;

namespace Lazy.Rx
{
    internal sealed class Empty<T> : Observable<T>
    {
        public static readonly Empty<T> Instance = new();

        protected override IDisposable SubscribeCore(Observer<T> observer)
        {
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}
