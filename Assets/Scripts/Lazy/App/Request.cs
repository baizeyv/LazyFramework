namespace Lazy.App
{
    // ! Command 和 Query 的结合体
    public interface IRequest<TResult> : ICanSetApp, ICanSendRequest, ICanSendQuery, ICanSendCommand, ICanGetModel
    {
        TResult Fire();
    }

    public interface IRequest<TArgument, TResult> : ICanSetApp, ICanSendRequest, ICanSendQuery, ICanSendCommand,
        ICanGetModel where TArgument : struct
    {
        TResult Fire(TArgument arg);
    }

    public interface ICanSendRequest : IModule
    {
    }

    public abstract class ABSRequest<TResult> : IRequest<TResult>
    {
        public TResult Fire()
        {
            return OnFire();
        }

        protected abstract TResult OnFire();

        public IApp App { get; private set; }

        public void SetApp(IApp app)
        {
            App = app;
        }
    }

    public abstract class ABSRequest<TArgument, TResult> : IRequest<TArgument, TResult> where TArgument : struct
    {
        public TResult Fire(TArgument arg)
        {
            return OnFire(arg);
        }

        protected abstract TResult OnFire(TArgument arg);

        public IApp App { get; private set; }

        public void SetApp(IApp app)
        {
            App = app;
        }
    }

    public static class RequestExtensions
    {
        public static TR LazyRequest<T, TR>(this ICanSendRequest source) where T : class, IRequest<TR>, new()
        {
            return source.App.SendRequest<T, TR>();
        }

        public static TR LazyRequest<T, TArg, TR>(this ICanSendRequest source, TArg arg)
            where T : class, IRequest<TArg, TR>, new() where TArg : struct
        {
            return source.App.SendRequest<T, TArg, TR>(arg);
        }
    }
}