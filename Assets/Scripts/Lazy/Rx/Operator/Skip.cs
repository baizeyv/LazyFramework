using System;

namespace Lazy.Rx.Operator
{
    internal sealed class Skip<T> : Observable<T>
    {
        private readonly Observable<T> _source;
        private readonly int _count;

        internal Skip(Observable<T> source, int count)
        {
            _source = source;
            _count = count;
        }

        protected override IDisposable SubscribeCore(Observer<T> observer)
        {
            return _source.Subscribe(new SkipObserver(observer, _count));
        }

        private sealed class SkipObserver : Observer<T>
        {
            private readonly Observer<T> _observer;
            private int _count;

            internal SkipObserver(Observer<T> observer, int count)
            {
                _observer = observer;
                _count = count;
            }

            protected override void OnCompletedCore(Result result)
            {
                _observer.OnCompleted(result);
            }

            protected override void OnNextCore(T value)
            {
                if (_count > 0)
                    --_count;
                else
                    _observer.OnNext(value);
            }

            protected override void OnErrorCore(Exception error)
            {
                _observer.OnError(error);
            }
        }
    }

    public static class RxSkipExtensions
    {
        public static Observable<T> Skip<T>(this Observable<T> source, int count)
        {
            if (count >= 0)
                return new Skip<T>(source, count);

            throw new ArgumentOutOfRangeException(nameof(count));
        }
    }
}
