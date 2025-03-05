using System;

namespace Lazy.Rx.Operator
{
    /// <summary>
    /// * 前几次生效
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class Take<T> : Observable<T>
    {
        private readonly Observable<T> _source;
        private readonly int _count;

        internal Take(Observable<T> source, int count)
        {
            _source = source;
            _count = count;
        }

        protected override IDisposable SubscribeCore(Observer<T> observer)
        {
            return _source.Subscribe(new TakeObserver(observer, _count));
        }

        private sealed class TakeObserver : Observer<T>
        {
            private readonly Observer<T> _observer;
            private int _count;

            internal TakeObserver(Observer<T> observer, int count)
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
                if (_count <= 0)
                    return;
                --_count;
                _observer.OnNext(value);
                if (_count != 0)
                    return;
                _observer.OnCompleted();
            }

            protected override void OnErrorCore(Exception error)
            {
                _observer.OnError(error);
            }
        }
    }

    public static class RxTakeExtensions
    {
        public static Observable<T> Take<T>(this Observable<T> source, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return Empty<T>.Instance;
            return new Take<T>(source, count);
        }
    }
}
