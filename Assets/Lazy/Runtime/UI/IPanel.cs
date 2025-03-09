namespace Lazy.UI
{
    public interface IPanel
    {
        /// <summary>
        /// * 初始化设置
        /// </summary>
        /// <param name="panelData"></param>
        void Setup(IPanelData panelData);

        void Open(IPanelData panelData = null);

        void Close();

        void Show();

        void Hide();
    }

}