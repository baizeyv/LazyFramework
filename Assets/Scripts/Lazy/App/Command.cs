namespace Lazy.App
{
    public interface ICommand : ICanSetApp, ICanSendQuery, ICanSendCommand, ICanSendRequest, ICanGetModel
    {
        void Fire();
    }

    public interface ICommand<TArg> : ICanSetApp, ICanSendQuery, ICanSendCommand, ICanSendRequest, ICanGetModel where TArg : struct
    {
        void Fire(TArg arg);
    }

    public interface IStructCommand : ICanSetApp, ICanSendQuery, ICanSendCommand, ICanSendRequest, ICanGetModel
    {
        void Fire();
    }

    public interface ICanSendCommand: IModule { }

    public abstract class ABSCommand : ICommand
    {
        public void Fire()
        {
            OnFire();
        }

        protected abstract void OnFire();

        public IApp App { get; private set; }

        public void SetApp(IApp app)
        {
            App = app;
        }
    }

    public abstract class ABSCommand<TArg> : ICommand<TArg> where TArg : struct
    {
        public void Fire(TArg arg)
        {
            OnFire(arg);
        }

        protected abstract void OnFire(TArg arg);

        public IApp App { get; private set; }

        public void SetApp(IApp app)
        {
            App = app;
        }
    }

    public static class CanSendCommandExtensions
    {
        public static void LazyCommand<T>(this ICanSendCommand source) where T : class, ICommand, new()
        {
            source.App.SendCommand<T>();
        }

        public static void LazyCommand<T, TArg>(this ICanSendCommand source, TArg arg)
            where T : class, ICommand<TArg>, new() where TArg : struct
        {
            source.App.SendCommand<T, TArg>(arg);
        }

        public static void LazyStructCommand(this ICanSendCommand source, IStructCommand cmd)
        {
            source.App.SendStructCommand(cmd);
        }
    }
}
