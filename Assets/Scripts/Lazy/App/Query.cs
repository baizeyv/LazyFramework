namespace Lazy.App
{
    public interface IQuery<T> : IModule, ICanSetApp, ICanSendQuery, ICanGetModel
    {
        T Fire();
    }

    public interface ICanSendQuery
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
}