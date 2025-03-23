using Lazy;
using UnityEngine;

namespace Lazy.Melody
{
    public partial class TestWidget : UIWidget
    {
        public override string TypeName => "TestWidget";

		[SerializeField]
		public UnityEngine.UI.Image mImage6;
		[SerializeField]
		public UnityEngine.UI.Image mImage7;
		[SerializeField]
		public UnityEngine.UI.Image mImage8;
		[SerializeField]
		public MyNestedWidget mImage6a;


        protected override void ClearUIComponents()
        {
			mImage6 = null;
			mImage7 = null;
			mImage8 = null;
			mImage6a = null;

        }
    }
}
// Generation Time: 2025年3月23日 17:22:16
// Generation ID: ca0c9fd3-aa52-4151-a700-fc806ef0b7e8
