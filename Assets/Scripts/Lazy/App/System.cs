namespace Lazy.App
{
    public interface ISystem
        : ICanSetApp,
            ICanSetup,
            ICanGetUtility,
            ICanSendCommand,
            ICanSendQuery,
            ICanSendRequest
    {
    }

    public interface ICanGetSystem : IModule
    {
    }

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

    public static class CanGetSystemExtensions
    {
        public static T GetSystem<T>(this ICanGetSystem source) where T : class, ISystem => source.App.GetSystem<T>();
    }
}