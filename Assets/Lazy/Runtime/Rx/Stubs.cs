using System;
using Lazy.Utility;

namespace Lazy.Rx
{
    internal static class Stubs
    {
        internal static readonly Action<Result> HandleResult = (result) =>
        {
            if (result.IsFailure)
            {
                ObservableSystem.GetUnhandledExceptionHandler().Invoke(result.Exception);
            }
        };
    }

    internal static class Stubs<T>
    {
        internal static readonly Func<T, T> ReturnSelf = x => x;

        internal static readonly Action<Exception, T> HandleException =
            (x, _) => ObservableSystem.GetUnhandledExceptionHandler().Fire(x);

        internal static readonly Action<Result, T> HandleResult = (x, _) =>
        {
            if (!x.IsFailure)
                return;
            ObservableSystem.GetUnhandledExceptionHandler().Fire(x.Exception);
        };
    }
}