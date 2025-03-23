using UnityEngine;

namespace Lazy
{
    public interface IPanel
    {
        /// <summary>
        /// * 初始化设置
        /// </summary>
        /// <param name="panelData"></param>
        void Setup(IPanelData panelData);

        void Open(IPanelData panelData = null);

        void Close(bool destroy = true);

        void Show();

        void Hide();

        void Back();

        /// <summary>
        /// * 界面状态 TODO:
        /// </summary>
        PanelState State { get; set; }

        /// <summary>
        /// * 界面信息
        /// </summary>
        PanelInfo Info { get; set; }

        /// <summary>
        /// * 该界面的GameObject的transform
        /// </summary>
        Transform Transform { get; }

        int Order { get; }

        /// <summary>
        /// * Close 的时候是否使用Hide, 不使用Hide则使用Close
        /// </summary>
        bool CloseDestroy { get; }
    }
}
