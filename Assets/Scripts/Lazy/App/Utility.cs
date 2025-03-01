namespace Lazy.App
{
    public interface IUtility : ICanSetup
    {
    }

    public interface ICanGetUtility : IModule
    {
    }

    public static class CanGetUtilityExtensions
    {
        public static T GetUtility<T>(this ICanGetUtility source) where T : class, IUtility =>
            source.App.GetUtility<T>();
    }
}