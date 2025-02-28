namespace Lazy.App
{
    public interface IModel : IModule, ICanSetApp, ICanSetup, ICanGetUtility { }

    public interface ICanGetModel : IModule { }

    public abstract class ABSModel : IModel
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
