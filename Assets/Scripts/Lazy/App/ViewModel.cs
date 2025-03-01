namespace Lazy.App
{
    public interface IViewModel : IModule, ICanGetSystem, ICanGetModel, ICanSendCommand, ICanSendQuery, ICanSendRequest
    {

    }

}