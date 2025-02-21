
using System;

namespace Lazy.FSM
{
    public class SimpleState : IState
    {

        #region Property

        private Func<bool> _onCondition;

        private Action _onEnter;

        private Action _onUpdate;

        private Action _onFixedUpdate;

        private Action _onGUI;

        private Action _onExit;

        #endregion

        #region Interface Method

        public bool Condition()
        {
            var result = _onCondition?.Invoke();
            return result == null || result.Value;
        }

        public void Enter()
        {
            _onEnter?.Invoke();
        }

        public void Update()
        {
            _onUpdate?.Invoke();
        }

        public void FixedUpdate()
        {
            _onFixedUpdate?.Invoke();
        }

        public void GUI()
        {
            _onGUI?.Invoke();
        }

        public void Exit()
        {
            _onExit?.Invoke();
        }

        #endregion

        #region Custom Set Method

        public SimpleState OnCondition(Func<bool> onCondition)
        {
            _onCondition = onCondition;
            return this;
        }

        public SimpleState OnEnter(Action onEnter)
        {
            _onEnter = onEnter;
            return this;
        }

        public SimpleState OnUpdate(Action onUpdate)
        {
            _onUpdate = onUpdate;
            return this;
        }

        public SimpleState OnFixedUpdate(Action onFixedUpdate)
        {
            _onFixedUpdate = onFixedUpdate;
            return this;
        }

        public SimpleState OnGUI(Action onGUI)
        {
            _onGUI = onGUI;
            return this;
        }

        public SimpleState OnExit(Action onExit)
        {
            _onExit = onExit;
            return this;
        }

        #endregion
    }
}