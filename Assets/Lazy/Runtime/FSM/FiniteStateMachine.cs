using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lazy.FSM
{
    /// <summary>
    /// * 有限状态机
    /// </summary>
    public class FiniteStateMachine<TState>
    {
        /// <summary>
        /// * 状态字典
        /// </summary>
        private Dictionary<TState, IState> _states = new();

        /// <summary>
        /// * 当前状态
        /// </summary>
        private IState _currentState;

        /// <summary>
        /// * 当前状态键值
        /// </summary>
        private TState _currentStateKey;

        public IState CurrentState => _currentState;

        public TState CurrentStateKey => _currentStateKey;

        /// <summary>
        /// * 上一个状态键值
        /// </summary>
        public TState PreviousStateKey { get; private set; }

        /// <summary>
        /// * 当前状态的帧总数
        /// </summary>
        public long FrameCountOfCurrentState = 1;

        /// <summary>
        /// * 当前状态的秒数
        /// </summary>
        public float SecondsOfCurrentState = 0.0f;

        /// <summary>
        /// * 状态切换 event Action
        /// </summary>
        private event Action<TState, TState> OnStateToggled = (_, __) => { };

        /// <summary>
        /// * 定义状态
        /// </summary>
        /// <param name="stateKey"></param>
        /// <returns></returns>
        public SimpleState DefineState(TState stateKey)
        {
            if (_states.TryGetValue(stateKey, out var val))
            {
                return val as SimpleState;
            }

            var simpleState = new SimpleState();
            _states.Add(stateKey, simpleState);
            return simpleState;
        }

        /// <summary>
        /// * 添加状态
        /// </summary>
        /// <param name="stateKey"></param>
        /// <param name="state"></param>
        public void AddState(TState stateKey, IState state)
        {
            _states.Add(stateKey, state);
        }

        /// <summary>
        /// * 启动状态
        /// </summary>
        /// <param name="stateKey"></param>
        public void StartState(TState stateKey)
        {
            if (_states.TryGetValue(stateKey, out var state))
            {
                PreviousStateKey = stateKey;
                _currentState = state;
                _currentStateKey = stateKey;
                FrameCountOfCurrentState = 0;
                SecondsOfCurrentState = 0.0f;
                state.Enter();
            }
        }

        public void ToggleState(TState stateKey)
        {
            if (stateKey.Equals(CurrentStateKey))
                return;

            if (_states.TryGetValue(stateKey, out var state))
            {
                if (_currentState != null && state.Condition())
                {
                    // # 退出当前状态
                    _currentState.Exit();
                    PreviousStateKey = _currentStateKey;
                    // # 进入新状态
                    _currentState = state;
                    _currentStateKey = stateKey;
                    OnStateToggled?.Invoke(PreviousStateKey, CurrentStateKey);
                    FrameCountOfCurrentState = 1;
                    SecondsOfCurrentState = 0.0f;
                    _currentState.Enter();
                }
            }
        }

        public void AddStateToggleEvent(Action<TState, TState> action)
        {
            OnStateToggled += action;
        }

        public void Update()
        {
            _currentState?.Update();
            FrameCountOfCurrentState++;
            SecondsOfCurrentState += Time.deltaTime;
        }

        public void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        public void GUI()
        {
            _currentState?.GUI();
        }

        public void End()
        {
            // # 退出当前状态
            _currentState?.Exit();
            _currentState = null;
            _currentStateKey = default;
            _states.Clear();
        }
    }
}