using System;

namespace Lazy.Rx.Operator
{
    /// <summary>
    /// * 筛选过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class Where<T> : Observable<T>
    {
        internal readonly Observable<T> Source;
        internal readonly Func<T, bool> Predicate;

        internal Where(Observable<T> source, Func<T, bool> predicate)
        {
            Source = source;
            Predicate = predicate;
        }

        protected override IDisposable SubscribeCore(Observer<T> observer)
        {
            return Source.Subscribe(new WhereObserver(observer, Predicate));
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

    public static class RxWhereExtensions
    {
        public static Observable<T> Where<T>(this Observable<T> source, Func<T, bool> predicate)
        {
            if (source is Where<T> where)
                // # 多个where叠加
                return new Where<T>(where.Source, (x) => where.Predicate(x) && predicate(x));
            else
                return new Where<T>(source, predicate);
        }
    }
}
