namespace Lazy
{
    public interface IUtility : ICanSetup { }

    public interface ICanGetUtility : IModule { }

    public static class CanGetUtilityExtensions
    {
        public static T GetUtility<T>(this ICanGetUtility source)
            where T : class, IUtility
        {
            return source.App.GetUtility<T>();
        }
    }
}
