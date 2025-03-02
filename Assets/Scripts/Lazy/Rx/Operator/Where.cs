using System;

namespace Lazy.Rx.Operator
{
    internal sealed class Where<T> : Observable<T>
    {
        internal readonly Observable<T> source;
        internal readonly Func<T, bool> predicate;

        internal Where(Observable<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        protected override IDisposable SubscribeCore(Observer<T> observer)
        {
            return source.Subscribe(new WhereObserver(observer, predicate));
        }

        private class WhereObserver : Observer<T>
        {
            private readonly Observer<T> _observer;
            private readonly Func<T, bool> _predicate;

            internal WhereObserver(Observer<T> observer, Func<T, bool> predicate)
            {
                _observer = observer;
                _predicate = predicate;
            }

            protected override void OnCompletedCore(Result result)
            {
                _observer.OnCompleted(result);
            }

            protected override void OnNextCore(T value)
            {
                if (!_predicate(value))
                    return;
                _observer.OnNext(value);
            }

            protected override void OnErrorCore(Exception error)
            {
                _observer.OnError(error);
            }
        }
    }

    public static class WhereExtensions
    {
        public static Observable<T> Where<T>(this Observable<T> source, Func<T, bool> predicate)
        {
            if (source is Where<T> where)
            {
                // # 多个where叠加
                return new Where<T>(where.source, (x) => where.predicate(x) && predicate(x));
            }
            else
            {
                return new Where<T>(source, predicate);
            }
        }
    }
}