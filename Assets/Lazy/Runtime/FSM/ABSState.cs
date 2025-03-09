namespace Lazy.FSM
{
    public abstract class ABSState<TStateKey, TTarget> : IState
    {
        protected FiniteStateMachine<TStateKey> Fsm;

        protected TTarget Target;

        protected ABSState(FiniteStateMachine<TStateKey> fsm, TTarget target)
        {
            Fsm = fsm;
            Target = target;
        }

        public bool Condition()
        {
            return OnCondition();
        }

        public void Enter()
        {
            OnEnter();
        }

        public void Update()
        {
            OnUpdate();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate();
        }

        public void GUI()
        {
            OnGUI();
        }

        public void Exit()
        {
            OnExit();
        }

        protected virtual bool OnCondition() => true;

        protected virtual void OnEnter()
        {
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnFixedUpdate()
        {
        }

        protected virtual void OnGUI()
        {
        }

        protected virtual void OnExit()
        {
        }
    }
}