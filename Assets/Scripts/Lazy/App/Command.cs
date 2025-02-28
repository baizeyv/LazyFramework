namespace Lazy.App
{
    public interface ICommand : IModule, ICanSetApp, ICanSendQuery, ICanSendCommand, ICanGetModel
    {
        void Fire();
    }

    public interface ICommand<T> : IModule, ICanSetApp, ICanSendQuery, ICanSendCommand, ICanGetModel
    {
        T Fire();
    }

    public interface ICanSendCommand { }

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
}
