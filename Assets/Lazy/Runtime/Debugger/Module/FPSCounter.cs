namespace Lazy.Debugger.Module
{
    /// <summary>
    /// * 帧率计数器
    /// </summary>
    public class FPSCounter
    {
        private readonly float _updateInterval;

        /// <summary>
        /// * 帧总数
        /// </summary>
        private int _frames;

        public FPSCounter(float updateInterval)
        {
            if (updateInterval <= 0)
                return;
            _updateInterval = updateInterval;
            Reset();
        }

        public void Reset() { }
    }
}
