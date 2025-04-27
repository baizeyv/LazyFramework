using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy
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
        private static readonly List<ManagerWrapper> AllManagers = new(100);

        /// <summary>
        /// * 使用Update()的管理器列表
        /// </summary>
        private static readonly List<ManagerWrapper> UpdateManagers = new(100);

        /// <summary>
        /// * 使用LateUpdate()的管理器列表
        /// </summary>
        private static readonly List<ManagerWrapper> LateUpdateManagers = new(100);

        /// <summary>
        /// * 使用FixedUpdate()的管理器列表
        /// </summary>
        private static readonly List<ManagerWrapper> FixedUpdateManagers = new(100);

        /// <summary>
        /// * 使用OnGUI()的管理器列表
        /// </summary>
        private static readonly List<ManagerWrapper> GuiManagers = new(100);

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
                Log.MsgE("MonoBehaviour Is Null !");

            if (_behaviour != null)
            {
                Log.MsgE($"{nameof(ManagerCenter)} is already setup.");
                return;
            }

            if (behaviour.gameObject.transform.parent == null)
                Object.DontDestroyOnLoad(behaviour.gameObject);
            _behaviour = behaviour;
        }

        /// <summary>
        /// * 创建管理器
        /// </summary>
        /// <param name="createMethod"></param>
        /// <param name="priority">优先级,越小越先执行</param>
        /// <typeparam name="T">管理器类</typeparam>
        /// <returns></returns>
        public static T Create<T>(Func<T> createMethod, int priority = 0)
            where T : Singleton<T>, IManager
        {
            if (priority < 0)
            {
                Log.MsgW("Priority can not is negative. Auto switch to 0");
                priority = 0;
            }

            if (TryGet<T>(out var manager))
            {
                Log.MsgW($"{typeof(T)} is already registered.");
                return manager;
            }

            if (priority == 0)
            {
                // # 没有设置优先级
                var maxPriority = GetMaxPriority();
                priority = ++maxPriority;
            }

            Log.MsgD($"Create Manager {typeof(T)} with priority {priority}");

            var mgr = createMethod.Fire();
            var wrapper = new ManagerWrapper(mgr, priority);
            AllManagers.Add(wrapper);
            _ioc.Register(mgr);

            AllManagers.Sort(
                (left, right) =>
                {
                    if (left.priority < right.priority)
                        return -1;
                    return left.priority > right.priority ? 1 : 0;
                }
            );

            if (typeof(T).GetCustomAttributes(typeof(ManagerUpdateAttribute), false).Length > 0)
            {
                UpdateManagers.Add(wrapper);
                UpdateManagers.Sort(
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
                LateUpdateManagers.Add(wrapper);
                LateUpdateManagers.Sort(
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
                FixedUpdateManagers.Add(wrapper);
                FixedUpdateManagers.Sort(
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

        /// <summary>
        /// * 创建管理器
        /// </summary>
        /// <param name="createMethod"></param>
        /// <param name="priority">优先级,越小越先执行</param>
        /// <typeparam name="T">管理器类</typeparam>
        /// <returns></returns>
        public static T CreateMono<T>(Func<T> createMethod, int priority = 0)
            where T : MonoSingleton<T>, IManager
        {
            if (priority < 0)
            {
                Log.MsgW("Priority can not is negative. Auto switch to 0");
                priority = 0;
            }

            if (TryGetMono<T>(out var manager))
            {
                Log.MsgW($"{typeof(T)} is already registered.");
                return manager;
            }

            if (priority == 0)
            {
                // # 没有设置优先级
                var maxPriority = GetMaxPriority();
                priority = ++maxPriority;
            }

            Log.MsgD($"Create Manager {typeof(T)} with priority {priority}");

            var mgr = createMethod.Fire();
            var wrapper = new ManagerWrapper(mgr, priority);
            AllManagers.Add(wrapper);
            _ioc.Register(mgr);

            AllManagers.Sort(
                (left, right) =>
                {
                    if (left.priority < right.priority)
                        return -1;
                    return left.priority > right.priority ? 1 : 0;
                }
            );

            if (typeof(T).GetCustomAttributes(typeof(ManagerUpdateAttribute), false).Length > 0)
            {
                UpdateManagers.Add(wrapper);
                UpdateManagers.Sort(
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
                LateUpdateManagers.Add(wrapper);
                LateUpdateManagers.Sort(
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
                FixedUpdateManagers.Add(wrapper);
                FixedUpdateManagers.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }

            if (typeof(T).GetCustomAttributes(typeof(ManagerGUIAttribute), false).Length > 0)
            {
                GuiManagers.Add(wrapper);
                GuiManagers.Sort(
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
            foreach (var wrapper in AllManagers.Where(item => item.manager.GetType() == type))
            {
                wrapper.readyToBeRemoved = true;
                wrapper.manager.OnDestroyRelease();
                flag = true;
            }

            _ioc.Unregister<T>();

            return flag;
        }

        public static bool DestroyMono<T>()
            where T : MonoSingleton<T>, IManager, new()
        {
            var type = typeof(T);
            var flag = false;
            foreach (var wrapper in AllManagers.Where(item => item.manager.GetType() == type))
            {
                wrapper.readyToBeRemoved = true;
                wrapper.manager.OnDestroyRelease();
                flag = true;
            }

            _ioc.Unregister<T>();

            return flag;
        }

        public static void Destroy()
        {
            for (var i = AllManagers.Count - 1; i >= 0; i--)
                AllManagers[i].manager.OnDestroyRelease();
            AllManagers.Clear();
            UpdateManagers.Clear();
            LateUpdateManagers.Clear();
            FixedUpdateManagers.Clear();
            _ioc.Clear();
        }

        public static bool TryGet<T>(out T manager)
            where T : Singleton<T>, IManager
        {
            var ret = _ioc.Get<T>();
            manager = ret;
            return ret != null;
        }

        public static bool TryGetMono<T>(out T manager)
            where T : MonoBehaviour, IManager
        {
            var ret = _ioc.Get<T>();
            manager = ret;
            return ret != null;
        }

        public static void Update()
        {
            for (var i = 0; i < UpdateManagers.Count; i++)
            {
                if (UpdateManagers[i]?.manager == null || UpdateManagers[i].readyToBeRemoved)
                {
                    UpdateManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                UpdateManagers[i].manager.OnUpdate();
            }
        }

        public static void LateUpdate()
        {
            for (var i = 0; i < LateUpdateManagers.Count; i++)
            {
                if (
                    LateUpdateManagers[i]?.manager == null
                    || LateUpdateManagers[i].readyToBeRemoved
                )
                {
                    LateUpdateManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                LateUpdateManagers[i].manager.OnLateUpdate();
            }
        }

        public static void FixedUpdate()
        {
            for (var i = 0; i < FixedUpdateManagers.Count; i++)
            {
                if (
                    FixedUpdateManagers[i]?.manager == null
                    || FixedUpdateManagers[i].readyToBeRemoved
                )
                {
                    FixedUpdateManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                FixedUpdateManagers[i].manager.OnFixedUpdate();
            }
        }

        public static void GUI()
        {
            for (var i = 0; i < GuiManagers.Count; i++)
            {
                if (GuiManagers[i]?.manager == null || GuiManagers[i].readyToBeRemoved)
                {
                    GuiManagers.RemoveAt(i);
                    i--;
                    continue;
                }

                GuiManagers[i].manager.OnGui();
            }
        }

        /// <summary>
        /// * 获取当前最大的优先级
        /// </summary>
        /// <returns></returns>
        private static int GetMaxPriority()
        {
            return AllManagers.Select(item => item.priority).Prepend(int.MinValue).Max();
        }

        public static MonoBehaviour GetBehaviour()
        {
            if (_behaviour == null)
                Log.MsgE($"{nameof(ManagerCenter)} 未初始化。");
            return _behaviour;
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
