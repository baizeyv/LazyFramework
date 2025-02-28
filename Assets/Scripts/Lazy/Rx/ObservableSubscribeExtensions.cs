using System;
using Lazy.Utility;

namespace Lazy.Rx
{
    public static class ObservableSubscribeExtensions
    {
        public static IDisposable Subscribe<T>(this Observable<T> source, Action<T> onNext)
        {
            return source.Subscribe(
                new AnonymousObserver<T>(
                    onNext,
                    ObservableSystem.GetUnhandledExceptionHandler(),
                    Stubs.HandleResult
                )
            );
        }

        public static IDisposable Subscribe<T, TState>(this Observable<T> source, TState state,
            Action<T, TState> onNext)
        {
            return source.Subscribe(new AnonymousObserver<T, TState>(onNext, Stubs<TState>.HandleException,
                Stubs<TState>.HandleResult, state));
        }
    }

    internal sealed class AnonymousObserver<T> : Observer<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action<Result> _onCompleted;

        public AnonymousObserver(
            Action<T> onNext,
            Action<Exception> onError,
            Action<Result> onCompleted
        )
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        protected override void OnCompletedCore(Result result)
        {
            _onCompleted?.Invoke(result);
        }

        protected override void OnNextCore(T value)
        {
            _onNext?.Invoke(value);
        }

        protected override void OnErrorCore(Exception error)
        {
            _onError?.Invoke(error);
        }
    }

    internal sealed class AnonymousObserver<T, TState> : Observer<T>
    {
        private readonly Action<T, TState> _onNext;
        private readonly Action<Exception, TState> _onError;
        private readonly Action<Result, TState> _onCompleted;
        private readonly TState _state;

        public AnonymousObserver(
            Action<T, TState> onNext,
            Action<Exception, TState> onError,
            Action<Result, TState> onCompleted,
            TState state
        )
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
            _state = state;
        }

        protected override void OnCompletedCore(Result result)
        {
            _onCompleted.Fire(result, _state);
        }

        protected override void OnNextCore(T value)
        {
            _onNext.Fire(value, _state);
        }

        protected override void OnErrorCore(Exception error)
        {
            _onError.Fire(error, _state);
        }
    }
}