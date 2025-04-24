using System;
using System.Collections.Generic;

namespace Lazy
{
    public class IntEvent : IDisposable
    {
        private readonly Dictionary<int, ISimpleEvent> _events = new();

        private readonly object _gate = new();

        public IDisposable Subscribe(int intEvent, Observer<Unit> observer)
        {
            try
            {
                var subscription = SubscribeCore(intEvent, observer);
                observer.SourceSubscription.Disposable = subscription;
                return observer; // return observer to make subscription chain
            }
            catch
            {
                observer.Dispose();
                throw;
            }
        }

        private IDisposable SubscribeCore(int intEvent, Observer<Unit> observer)
        {
            if (_events.TryGetValue(intEvent, out var evt))
            {
                var simpleEvent = evt as SimpleEvent;
                return simpleEvent?.Subscribe(
                    new AnonymousObserver<Unit>(
                        observer.OnNext,
                        observer.OnError,
                        observer.OnCompleted
                    )
                );
            }
            else
            {
                var simpleEvent = new SimpleEvent();
                _events.Add(intEvent, simpleEvent);
                return simpleEvent.Subscribe(
                    new AnonymousObserver<Unit>(
                        observer.OnNext,
                        observer.OnError,
                        observer.OnCompleted
                    )
                );
            }
        }

        /// <summary>
        /// * 是否存在订阅
        /// </summary>
        public bool HasSubscriptions => _events.Count > 0;

        public IDisposable Subscribe<T>(int intEvent, Observer<T> observer)
        {
            try
            {
                var subscription = SubscribeCore(intEvent, observer);
                observer.SourceSubscription.Disposable = subscription;
                return observer; // return observer to make subscription chain
            }
            catch
            {
                observer.Dispose();
                throw;
            }
        }

        private IDisposable SubscribeCore<T>(int intEvent, Observer<T> observer)
        {
            if (_events.TryGetValue(intEvent, out var evt))
            {
                var simpleEvent = evt as SimpleEvent<T>;
                return simpleEvent?.Subscribe(
                    new AnonymousObserver<T>(
                        observer.OnNext,
                        observer.OnError,
                        observer.OnCompleted
                    )
                );
            }
            else
            {
                var simpleEvent = new SimpleEvent<T>();
                _events.Add(intEvent, simpleEvent);
                return simpleEvent?.Subscribe(
                    new AnonymousObserver<T>(
                        observer.OnNext,
                        observer.OnError,
                        observer.OnCompleted
                    )
                );
            }
        }

        public void Fire(int intEvent)
        {
            if (!_events.TryGetValue(intEvent, out var evt))
                return;
            var simpleEvent = evt as SimpleEvent;
            simpleEvent?.Fire();
        }

        public void Fire<T>(int intEvent, T data)
        {
            if (!_events.TryGetValue(intEvent, out var evt))
                return;
            var simpleEvent = evt as SimpleEvent<T>;
            simpleEvent?.Fire(data);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                foreach (var keyValuePair in _events)
                    keyValuePair.Value?.Dispose();

                _events.Clear();
            }
        }
    }
}
