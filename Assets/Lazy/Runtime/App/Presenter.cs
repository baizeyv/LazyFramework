namespace Lazy
{
    public interface IPresenter
        : IModule,
            ICanGetSystem,
            ICanGetModel,
            ICanSendCommand,
            ICanSendQuery,
            ICanSendRequest { }
}
