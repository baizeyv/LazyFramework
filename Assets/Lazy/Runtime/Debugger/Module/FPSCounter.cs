using System;

namespace Lazy.Debugger.Module
{
    /// <summary>
    /// * 帧率计数器
    /// </summary>
    [Serializable]
    public class FPSCounter
    {
        private readonly float _updateInterval;

        /// <summary>
        /// * 帧总数
        /// </summary>
        private int _frames;

        private float _accumulator;

        private float _timeLeft;

        /// <summary>
        /// * 当前FPS
        /// </summary>
        public float CurrentFPS { get; private set; }

        public FPSCounter(float updateInterval)
        {
            if (updateInterval <= 0)
                return;
            _updateInterval = updateInterval;
            Reset();
        }

        public void Update(float realElapsedSeconds)
        {
            _frames++;
            _accumulator += realElapsedSeconds;
            _timeLeft -= realElapsedSeconds;

            if (!(_timeLeft <= 0))
                return;
            CurrentFPS = _accumulator > 0f ? _frames / _accumulator : 0f;
            _frames = 0;
            _accumulator = 0;
            _timeLeft += _updateInterval;
        }

        public void Reset()
        {
            CurrentFPS = 0;
            _frames = 0;
            _accumulator = 0;
            _timeLeft = 0;
        }
    }
}
