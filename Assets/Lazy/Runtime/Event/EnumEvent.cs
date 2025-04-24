using System;
using System.Collections.Generic;

namespace Lazy
{
    public class EnumEvent<T> : IDisposable
        where T : Enum
    {
        private readonly Dictionary<T, ISimpleEvent> _events = new();

        private readonly object _gate = new();

        public IDisposable Subscribe(T enumEvent, Observer<Unit> observer)
        {
            try
            {
                var subscription = SubscribeCore(enumEvent, observer);
                observer.SourceSubscription.Disposable = subscription;
                return observer;
            }
            catch
            {
                observer.Dispose();
                throw;
            }
        }

        public IDisposable Subscribe<TU>(T enumEvent, Observer<TU> observer)
        {
            try
            {
                var subscription = SubscribeCore(enumEvent, observer);
                observer.SourceSubscription.Disposable = subscription;
                return observer; // return observer to make subscription chain
            }
            catch
            {
                observer.Dispose();
                throw;
            }
        }

        private IDisposable SubscribeCore<TU>(T enumEvent, Observer<TU> observer)
        {
            if (_events.TryGetValue(enumEvent, out var evt))
            {
                var simpleEvent = evt as SimpleEvent<TU>;
                return simpleEvent?.Subscribe(
                    new AnonymousObserver<TU>(
                        observer.OnNext,
                        observer.OnError,
                        observer.OnCompleted
                    )
                );
            }
            else
            {
                var simpleEvent = new SimpleEvent<TU>();
                _events.Add(enumEvent, simpleEvent);
                return simpleEvent.Subscribe(
                    new AnonymousObserver<TU>(
                        observer.OnNext,
                        observer.OnError,
                        observer.OnCompleted
                    )
                );
            }
        }

        private IDisposable SubscribeCore(T enumEvent, Observer<Unit> observer)
        {
            if (_events.TryGetValue(enumEvent, out var evt))
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
                _events.Add(enumEvent, simpleEvent);
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

        public void Fire(T enumEvent)
        {
            if (!_events.TryGetValue(enumEvent, out var evt))
                return;
            var simpleEvent = evt as SimpleEvent;
            simpleEvent?.Fire();
        }

        public void Fire<TU>(T enumEvent, TU data)
        {
            if (!_events.TryGetValue(enumEvent, out var evt))
                return;
            var simpleEvent = evt as SimpleEvent<TU>;
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
