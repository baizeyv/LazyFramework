using System;
using UnityEngine;

namespace Lazy.FSM
{
    public class FSMExample1 : MonoBehaviour
    {
        public enum States
        {
            A,
            B
        }

        public FiniteStateMachine<States> fsm = new();

        private void Start()
        {
            fsm.AddStateToggleEvent((prev, next) => { Debug.Log($"State: {prev} -> {next}"); });

            fsm.DefineState(States.A).OnCondition(() => fsm.CurrentStateKey == States.B)
                .OnEnter(() => { Debug.Log("ENTER A", this); }).OnUpdate(() => { }).OnFixedUpdate(() => { }).OnGUI(() =>
                {
                    GUILayout.Label("State A");
                    if (GUILayout.Button("TO B"))
                    {
                        fsm.ToggleState(States.B);
                    }
                }).OnExit(() => { Debug.Log("EXIT A"); });
            fsm.DefineState(States.B).OnCondition(() => fsm.CurrentStateKey == States.A).OnGUI(() =>
            {
                GUILayout.Label("State B");
                if (GUILayout.Button("TO A"))
                {
                    fsm.ToggleState(States.A);
                }
            });
            fsm.StartState(States.A);
        }

        private void Update()
        {
            fsm.Update();
        }

        private void FixedUpdate()
        {
            fsm.FixedUpdate();
        }

        private void OnGUI()
        {
            fsm.GUI();
        }

        private void OnDestroy()
        {
            fsm.End();
        }
    }

    public class FSMExample2 : MonoBehaviour
    {
        public enum States
        {
            A,
            B,
            C
        }

        public FiniteStateMachine<States> fsm = new();

        public class StateA : ABSState<States, FSMExample2>
        {
            public StateA(FiniteStateMachine<States> fsm, FSMExample2 target) : base(fsm, target)
            {
            }

            protected override bool OnCondition()
            {
                return Fsm.CurrentStateKey == States.B;
            }

            protected override void OnGUI()
            {
                GUILayout.Label("State A");
                if (GUILayout.Button("TO B"))
                {
                    Fsm.ToggleState(States.B);
                }
            }
        }

        public class StateB : ABSState<States, FSMExample2>
        {
            public StateB(FiniteStateMachine<States> fsm, FSMExample2 target) : base(fsm, target)
            {
            }

            protected override bool OnCondition()
            {
                return Fsm.CurrentStateKey == States.A;
            }
            protected override void OnGUI()
            {
                GUILayout.Label("State B");
                if (GUILayout.Button("TO A"))
                {
                    Fsm.ToggleState(States.A);
                }
            }
        }

        private void Start()
        {
            fsm.AddState(States.A, new StateA(fsm, this));
            fsm.AddState(States.B, new StateB(fsm, this));
            fsm.DefineState(States.C).OnEnter(() => { });
            fsm.StartState(States.A);
        }
    }
}