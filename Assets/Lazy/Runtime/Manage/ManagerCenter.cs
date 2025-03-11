using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.IOC;
using Lazy.Singleton;
using Lazy.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Manage
{
    public static class ManagerCenter
    {
        /// <summary>
        /// * 根MonoBehaviour
        /// </summary>
        private static MonoBehaviour _behaviour;

        /// <summary>
        /// * 所有管理器的列表
        /// </summary>
        private static List<ManagerWrapper> _allManagers = new(100);

        /// <summary>
        /// * 使用Update()的管理器列表
        /// </summary>
        private static List<ManagerWrapper> _updateManagers = new(100);

        /// <summary>
        /// * 使用LateUpdate()的管理器列表
        /// </summary>
        private static List<ManagerWrapper> _lateUpdateManagers = new(100);

        /// <summary>
        /// * 使用FixedUpdate()的管理器列表
        /// </summary>
        private static List<ManagerWrapper> _fixedUpdateManagers = new(100);

        /// <summary>
        /// * 控制翻转容器
        /// </summary>
        private static IOCContainer _ioc = new();

        /// <summary>
        /// * 初始设置管理器中心
        /// </summary>
        /// <param name="behaviour"></param>
        public static void Setup(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                Log.Log.MsgE("MonoBehaviour Is Null !");

            if (_behaviour != null)
            {
                Log.Log.MsgE($"{nameof(ManagerCenter)} is already setup.");
                return;
            }

            Object.DontDestroyOnLoad(behaviour.gameObject);
            _behaviour = behaviour;

            // TODO:
        }

        /// <summary>
        /// * 创建管理器
        /// </summary>
        /// <param name="createMethod"></param>
        /// <param name="argument">创建参数</param>
        /// <param name="priority">优先级,越小越先执行</param>
        /// <typeparam name="T">管理器类</typeparam>
        /// <returns></returns>
        public static T Create<T>(Func<T> createMethod, object argument = null, int priority = 0)
            where T : Singleton<T>, IManager, new()
        {
            if (priority < 0)
            {
                Log.Log.MsgW("Priority can not is negative. Auto switch to 0");
                priority = 0;
            }

            if (TryGet<T>(out var manager))
            {
                Log.Log.MsgW($"{typeof(T)} is already registered.");
                return manager;
            }

            if (priority == 0)
            {
                // # 没有设置优先级
                var maxPriority = GetMaxPriority();
                priority = ++maxPriority;
            }

            Log.Log.MsgD($"Create Manager {typeof(T)} with priority {priority}");

            var mgr = createMethod.Fire();
            var wrapper = new ManagerWrapper(mgr, priority);
            _allManagers.Add(wrapper);
            _ioc.Register(mgr);

            _allManagers.Sort(
                (left, right) =>
                {
                    if (left.priority < right.priority)
                        return -1;
                    return left.priority > right.priority ? 1 : 0;
                }
            );

            if (typeof(T).GetCustomAttributes(typeof(ManagerUpdateAttribute), false).Length > 0)
            {
                _updateManagers.Add(wrapper);
                _updateManagers.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }

            if (typeof(T).GetCustomAttributes(typeof(ManagerLateUpdateAttribute), false).Length > 0)
            {
                _lateUpdateManagers.Add(wrapper);
                _lateUpdateManagers.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }

            if (
                typeof(T).GetCustomAttributes(typeof(ManagerFixedUpdateAttribute), false).Length > 0
            )
            {
                _fixedUpdateManagers.Add(wrapper);
                _fixedUpdateManagers.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }

            return mgr;
        }

        public static bool Destroy<T>()
            where T : Singleton<T>, IManager, new()
        {
            var type = typeof(T);
            var flag = false;
            foreach (var wrapper in _allManagers.Where(item => item.manager.GetType() == type))
            {
                wrapper.readyToBeRemoved = true;
                wrapper.manager.OnDestroy();
                flag = true;
            }

            _ioc.Unregister<T>();

            return flag;
        }

        public static bool TryGet<T>(out T manager)
            where T : Singleton<T>, IManager, new()
        {
            var ret = _ioc.Get<T>();
            manager = ret;
            return ret != null;
        }

        public static void Update()
        {
            for (var i = 0; i < _updateManagers.Count; i++)
            {
                if (_updateManagers[i]?.manager == null || _updateManagers[i].readyToBeRemoved)
                {
                    _updateManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                _updateManagers[i].manager.OnUpdate();
            }
        }

        public static void LateUpdate()
        {
            for (var i = 0; i < _lateUpdateManagers.Count; i++)
            {
                if (
                    _lateUpdateManagers[i]?.manager == null
                    || _lateUpdateManagers[i].readyToBeRemoved
                )
                {
                    _lateUpdateManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                _lateUpdateManagers[i].manager.OnUpdate();
            }
        }

        public static void FixedUpdate()
        {
            for (var i = 0; i < _fixedUpdateManagers.Count; i++)
            {
                if (
                    _fixedUpdateManagers[i]?.manager == null
                    || _fixedUpdateManagers[i].readyToBeRemoved
                )
                {
                    _fixedUpdateManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                _fixedUpdateManagers[i].manager.OnUpdate();
            }
        }

        /// <summary>
        /// * 获取当前最大的优先级
        /// </summary>
        /// <returns></returns>
        private static int GetMaxPriority()
        {
            return _allManagers.Select(item => item.priority).Max();
        }

        private class ManagerWrapper
        {
            /// <summary>
            /// * 优先级
            /// </summary>
            public int priority = 0;

            public IManager manager;

            /// <summary>
            /// * 准备移除
            /// </summary>
            public bool readyToBeRemoved = false;

            public ManagerWrapper(IManager manager, int priority)
            {
                this.manager = manager;
                this.priority = priority;
            }
        }
    }
}
