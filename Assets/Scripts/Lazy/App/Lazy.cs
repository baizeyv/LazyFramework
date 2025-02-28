using System.Linq;
using Lazy.IOC;

namespace Lazy.App
{
    public interface IApp
    {
        void RegisterSystem<T>(T system)
            where T : ISystem;

        void RegisterModel<T>(T model)
            where T : IModel;

        void RegisterUtility<T>(T utility)
            where T : IUtility;

        T GetModel<T>()
            where T : class, IModel;

        T GetSystem<T>()
            where T : class, ISystem;

        T GetUtility<T>()
            where T : class, IUtility;

        void SendCommand<T>(T command)
            where T : ICommand;

        T SendCommand<T>(ICommand<T> command);

        T SendQuery<T>(IQuery<T> query);
    }

    public abstract class ABSApp<T> : IApp
        where T : ABSApp<T>, new()
    {
        /// <summary>
        /// * 是否已经初始化了
        /// </summary>
        private bool _initialized;

        protected static T App;

        private readonly IOCContainer _ioc = new();

        public static IApp Gate
        {
            get
            {
                if (App == null)
                    ValidApp();

                return App;
            }
        }

        private static void ValidApp()
        {
            if (App != null)
                return;

            App = new T();
            App.Setup();

            // # 遍历所有未初始化的Model,对其进行初始化
            foreach (
                var model in App._ioc.GetInstancesByType<IModel>().Where(item => !item.IsSetup)
            )
            {
                model.Setup();
                model.IsSetup = true;
            }

            // # 遍历所有未初始化的System,对其进行初始化
            foreach (
                var system in App._ioc.GetInstancesByType<ISystem>().Where(item => !item.IsSetup)
            )
            {
                system.Setup();
                system.IsSetup = true;
            }

            // # 遍历所有未初始化的Utility,对其进行初始化
            foreach (
                var utility in App._ioc.GetInstancesByType<IUtility>().Where(item => !item.IsSetup)
            )
            {
                utility.Setup();
                utility.IsSetup = true;
            }

            App._initialized = true;
        }

        protected abstract void Setup();

        public void RegisterSystem<T1>(T1 system)
            where T1 : ISystem
        {
            system.SetApp(this);
            _ioc.Register(system);
            if (!_initialized)
                return;
            // # 如果app已经初始化过了,则直接初始化system
            system.Setup();
            system.IsSetup = true;
        }

        public void RegisterModel<T1>(T1 model)
            where T1 : IModel
        {
            model.SetApp(this);
            _ioc.Register(model);
            if (!_initialized)
                return;
            // # 如果app已经初始化过了,则直接初始化model
            model.Setup();
            model.IsSetup = true;
        }

        public void RegisterUtility<T1>(T1 utility)
            where T1 : IUtility
        {
            _ioc.Register(utility);
            if (!_initialized)
                return;
            // # 如果app已经初始化过了,则直接初始化utility
            utility.Setup();
            utility.IsSetup = true;
        }

        public T1 GetModel<T1>()
            where T1 : class, IModel
        {
            return _ioc.Get<T1>();
        }

        public T1 GetSystem<T1>()
            where T1 : class, ISystem
        {
            return _ioc.Get<T1>();
        }

        public T1 GetUtility<T1>()
            where T1 : class, IUtility
        {
            return _ioc.Get<T1>();
        }

        public void SendCommand<T1>(T1 command)
            where T1 : ICommand
        {
            FireCommand(command);
        }

        public T1 SendCommand<T1>(ICommand<T1> command)
        {
            return FireCommand(command);
        }

        public T1 SendQuery<T1>(IQuery<T1> query)
        {
            return FireQuery(query);
        }

        protected virtual TR FireCommand<TR>(ICommand<TR> command)
        {
            command.SetApp(this);
            return command.Fire();
        }

        protected virtual void FireCommand(ICommand command)
        {
            command.SetApp(this);
            command.Fire();
        }

        protected virtual TR FireQuery<TR>(IQuery<TR> query)
        {
            query.SetApp(this);
            return query.Fire();
        }
    }

    public interface ICanSetApp
    {
        void SetApp(IApp app);
    }

    public interface IModule
    {
        IApp App { get; }
    }

    public interface ICanSetup
    {
        bool IsSetup { get; set; }

        void Setup();
    }
}
