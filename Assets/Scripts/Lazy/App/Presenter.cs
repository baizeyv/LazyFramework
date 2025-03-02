namespace Lazy.App
{
    public interface IPresenter : IModule, ICanGetSystem, ICanGetModel, ICanSendCommand, ICanSendQuery, ICanSendRequest
    {

    }

}