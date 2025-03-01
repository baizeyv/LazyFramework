namespace Lazy.App
{
    public interface IQuery<T> : ICanSetApp, ICanSendQuery, ICanGetModel
    {
        T Fire();
    }

    public interface IQuery<T, TR> : ICanSetApp, ICanSendQuery, ICanGetModel where T : struct
    {
        TR Fire(T arg);
    }

    public interface ICanSendQuery : IModule
    {
    }

    public abstract class ABSQuery<T> : IQuery<T>
    {
        public T Fire()
        {
            return OnFire();
        }

        protected abstract T OnFire();

        public IApp App { get; private set; }

        public void SetApp(IApp app)
        {
            App = app;
        }
    }

    public abstract class ABSQuery<TArgument, TResult> : IQuery<TArgument, TResult> where TArgument : struct
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

    public static class QueryExtensions
    {
        public static TR LazyQuery<T, TR>(this ICanSendQuery source) where T : class, IQuery<TR>, new()
        {
            return source.App.SendQuery<T, TR>();
        }

        public static TR LazyQuery<T, TArg, TR>(this ICanSendQuery source, TArg arg)
            where T : class, IQuery<TArg, TR>, new() where TArg : struct
        {
            return source.App.SendQuery<T, TArg, TR>(arg);
        }
    }
}