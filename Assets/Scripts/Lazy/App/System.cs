namespace Lazy.App
{
    public interface ISystem
        : IModule,
            ICanSetApp,
            ICanSetup,
            ICanGetUtility,
            ICanSendCommand,
            ICanSendQuery { }

    public interface ICanGetSystem { }

    public abstract class ABSSystem : ISystem
    {
        public IApp App { get; private set; }

        public void SetApp(IApp app)
        {
            App = app;
        }

        public bool IsSetup { get; set; }

        public void Setup()
        {
            OnSetup();
        }

        protected abstract void OnSetup();
    }
}
