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

        void SendCommand<T>() where T : class, ICommand, new();

        void SendCommand<T, TArgument>(TArgument arg) where T : class, ICommand<TArgument>, new()
            where TArgument : struct;

        TResult SendQuery<TQuery, TResult>() where TQuery : class, IQuery<TResult>, new();

        TResult SendQuery<TQuery, TArgument, TResult>(TArgument arg)
            where TQuery : class, IQuery<TArgument, TResult>, new() where TArgument : struct;

        TResult SendRequest<TRequest, TResult>() where TRequest : class, IRequest<TResult>, new();

        TResult SendRequest<TRequest, TArgument, TResult>(TArgument arg) where TRequest : class, IRequest<TArgument, TResult>, new()
            where TArgument : struct;
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

        /// <summary>
        /// * Command Query Request IOC Container
        /// </summary>
        private readonly IOCContainer _cqrIOC = new();

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

        public void SendCommand<T1>() where T1 : class, ICommand, new()
        {
            var cmd = _cqrIOC.Get<T1>();
            if (cmd == null)
            {
                cmd = new T1();
                _cqrIOC.Register(cmd);
            }

            FireCommand(cmd);
        }

        public void SendCommand<T1, TArgument>(TArgument arg) where T1 : class, ICommand<TArgument>, new()
            where TArgument : struct
        {
            var cmd = _cqrIOC.Get<T1>();
            if (cmd == null)
            {
                cmd = new T1();
                _cqrIOC.Register(cmd);
            }

            FireCommand(cmd, arg);
        }

        public TResult SendQuery<TQuery, TResult>() where TQuery : class, IQuery<TResult>, new()
        {
            var query = _cqrIOC.Get<TQuery>();
            if (query == null)
            {
                query = new TQuery();
                _cqrIOC.Register(query);
            }
            return FireQuery(query);
        }

        public TResult SendQuery<TQuery, TArgument, TResult>(TArgument arg) where TQuery : class, IQuery<TArgument, TResult>, new()
            where TArgument : struct
        {
            var query = _cqrIOC.Get<TQuery>();
            if (query == null)
            {
                query = new TQuery();
                _cqrIOC.Register(query);
            }
            return FireQuery(query, arg);
        }

        public TResult SendRequest<TRequest, TResult>() where TRequest : class, IRequest<TResult>, new()
        {
            var request = _cqrIOC.Get<TRequest>();
            if (request == null)
            {
                request = new TRequest();
                _cqrIOC.Register(request);
            }
            return FireRequest(request);
        }

        public TResult SendRequest<TRequest, TArgument, TResult>(TArgument arg) where TRequest : class, IRequest<TArgument, TResult>, new()
            where TArgument : struct
        {
            var request = _cqrIOC.Get<TRequest>();
            if (request == null)
            {
                request = new TRequest();
                _cqrIOC.Register(request);
            }
            return FireRequest(request, arg);
        }

        protected virtual void FireCommand(ICommand command)
        {
            command.SetApp(this);
            command.Fire();
        }

        protected virtual void FireCommand<TA>(ICommand<TA> command, TA arg) where TA : struct
        {
            command.SetApp(this);
            command.Fire(arg);
        }

        protected virtual TR FireQuery<TR>(IQuery<TR> query)
        {
            query.SetApp(this);
            return query.Fire();
        }

        protected virtual TR FireQuery<TA, TR>(IQuery<TA, TR> query, TA arg) where TA : struct
        {
            query.SetApp(this);
            return query.Fire(arg);
        }

        protected virtual TR FireRequest<TR>(IRequest<TR> request)
        {
            request.SetApp(this);
            return request.Fire();
        }

        protected virtual TR FireRequest<TA, TR>(IRequest<TA, TR> request, TA arg) where TA : struct
        {
            request.SetApp(this);
            return request.Fire(arg);
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