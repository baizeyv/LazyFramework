using System;

namespace Lazy
{
    public readonly struct Result
    {
        public static Result Success => default;

        public static Result Failure(Exception exception)
        {
            return new Result(exception);
        }

        public Exception Exception { get; }

        public bool IsSuccess => Exception == null;

        public bool IsFailure => Exception != null;

        public Result(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            Exception = exception;
        }

        public override string ToString()
        {
            if (IsSuccess)
                return $"Success";
            else
                return $"Failure{{{Exception.Message}}}";
        }
    }
}
