using UnityEngine;
using UnityEngine.UI;
using Lazy;

namespace Lazy.Melody
{
    public class MyPanelData : IPanelData { }

    public partial class MyPanel
    {
        #region Life Cycle

        protected override void OnSetup() { }

        protected override void OnOpen() { }

        protected override void OnShow() { }

        protected override void OnHide() { }

        protected override void OnShowTweenEnd() { }

        protected override void OnEndTweenBegin() { }

        protected override void OnClose() { }

        protected override void OnBack() { }

        protected override void OnUIDestroy() { }

        #endregion
    }
}
// Generation Time: 2025年3月22日 23:56:00
// Generation ID: fa4236b3-f316-4465-9176-adfd807a7a6f
