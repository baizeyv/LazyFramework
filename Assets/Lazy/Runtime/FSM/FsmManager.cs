using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Manage;
using Lazy.Pool;
using Lazy.Singleton;

namespace Lazy.FSM
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
                _machines[typeof(T)] = new List<IStateMachine>();
                _machines.Add(typeof(T), _machines[typeof(T)]);
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
