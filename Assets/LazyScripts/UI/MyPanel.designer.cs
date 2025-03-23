using System;
using UnityEngine;
using UnityEngine.UI;
using Lazy;

namespace Lazy.Melody
{
    public partial class MyPanel : UIPanel
    {
        #region Generation
        public const string Name = "MyPanel";

        private readonly MyPanelData _defaultData = new();

        protected MyPanelData Data => PanelData as MyPanelData ?? _defaultData;
        #endregion

        #region Field

		[SerializeField]
		public UnityEngine.UI.Image mImage1;
		[SerializeField]
		public TestWidget mImage5;

        #endregion

        protected override void ClearUIComponents()
        {
			mImage1 = null;
			mImage5 = null;

        }
    }
}
// Generation Time: 2025年3月23日 17:22:16
// Generation ID: de685dab-24ef-4191-8cbb-f1a834cea3f0
