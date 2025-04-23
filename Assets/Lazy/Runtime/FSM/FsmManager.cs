using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Manage;
using Lazy.Pool;
using Lazy.Singleton;

namespace Lazy
{
    [ManagerUpdate]
    [ManagerFixedUpdate]
    [ManagerGUI]
    public class FsmManager : Singleton<FsmManager>, IManager
    {
        private readonly Dictionary<Type, List<IStateMachine>> _machines = new();

        private FsmManager() { }

        public FiniteStateMachine<T> Create<T>()
        {
            var fsm = SafeObjectPool<FiniteStateMachine<T>>.Instance.Obtain();
            if (_machines.ContainsKey(typeof(T)))
            {
                _machines[typeof(T)].Add(fsm);
            }
            else
            {
                var list = new List<IStateMachine>();
                list.Add(fsm);
                _machines.Add(typeof(T), list);
            }

            return fsm;
        }

        public void Destroy<T>(FiniteStateMachine<T> fsm)
        {
            fsm.End();
            if (_machines.ContainsKey(typeof(T)))
                _machines[typeof(T)].Remove(fsm);
        }

        public void OnUpdate()
        {
            foreach (var fsm in _machines.Values.SelectMany(list => list))
                fsm.Update();
        }

        public void OnFixedUpdate()
        {
            foreach (var fsm in _machines.Values.SelectMany(list => list))
                fsm.FixedUpdate();
        }

        public void OnLateUpdate() { }

        public void OnDestroyRelease()
        {
            foreach (var fsm in _machines.Values.SelectMany(list => list))
                fsm.End();
            _machines.Clear();
        }

        public void OnGui()
        {
            foreach (var fsm in _machines.Values.SelectMany(list => list))
                fsm.GUI();
        }
    }
}
