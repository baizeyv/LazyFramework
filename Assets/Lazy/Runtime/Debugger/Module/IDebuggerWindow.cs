namespace Lazy
{
    public interface IDebuggerWindow
    {
        /// <summary>
        /// * 初始化窗口
        /// </summary>
        /// <param name="args"></param>
        void Initialize(params object[] args);

        /// <summary>
        /// * 关闭窗口
        /// </summary>
        void Shutdown();

        /// <summary>
        /// * 进入窗口
        /// </summary>
        void OnEnter();

        /// <summary>
        /// * 离开窗口
        /// </summary>
        void OnLeave();

        /// <summary>
        /// * 窗口轮询
        /// </summary>
        /// <param name="elapsedSeconds">逻辑经过的时间 (unit:s)</param>
        /// <param name="realElapsedSeconds">实际经过的时间 (unit:s)</param>
        void OnProcess(float elapsedSeconds, float realElapsedSeconds);

        /// <summary>
        /// * 窗口绘制
        /// </summary>
        void OnDraw();
    }
}
