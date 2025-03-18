using System;

namespace Lazy.Timer
{
    public class Timer
    {
        /// <summary>
        /// * 计时器 ID
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// * 是否是帧计时器
        /// </summary>
        public bool IsFrameTimer { get; private set; }

        /// <summary>
        /// * 是否计时完成了
        /// </summary>
        public bool IsFinished { get; set; }

        /// <summary>
        /// * 执行次数
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// * 完成事件
        /// </summary>
        public Action OnCompleted { get; private set; }

        /// <summary>
        /// * 进程中事件
        /// </summary>
        public Action OnProcess { get; private set; }

        /// <summary>
        /// * 步长
        /// </summary>
        private float _step = 1f;

        /// <summary>
        /// * 执行延迟
        /// </summary>
        private float _delay;

        /// <summary>
        /// * 经过的时间
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// * 延迟是否完成了
        /// </summary>
        private bool _isDelayCompleted = false;

        public Timer(
            float step = 1f,
            float delay = 0f,
            int count = 0,
            Action onProcess = null,
            Action onCompleted = null,
            bool isFrameTimer = false
        )
        {
            // # 生成唯一ID
            ID = Guid.NewGuid().GetHashCode();
            _step = step;
            _delay = delay;
            Count = count;
            OnProcess = onProcess;
            OnCompleted = onCompleted;
            IsFrameTimer = isFrameTimer;
            if (_delay <= 0)
                _isDelayCompleted = true;
        }

        /// <summary>
        /// * update
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <returns>执行几次回调</returns>
        public int Update(float deltaTime)
        {
            // # 记录触发次数
            var triggerCount = 0;

            if (!_isDelayCompleted)
            {
                // # 延迟未完成
                _delay -= deltaTime;
                if (_delay <= 0f)
                {
                    _isDelayCompleted = true;
                    _elapsedTime = -_delay;
                    triggerCount++; // # 延迟结束,触发第一次
                    _delay = 0f;
                }
                else
                {
                    return triggerCount;
                }
            }
            else
            {
                _elapsedTime += deltaTime;
            }

            // # 计算摇触发的次数
            while (_elapsedTime >= _step)
            {
                _elapsedTime -= _step;
                triggerCount++;
            }

            return triggerCount;
        }
    }
}
