using Lazy;
using UnityEngine;

namespace Lazy.Melody
{
    public partial class MyNestedWidget : UIWidget
    {
        public override string TypeName => "MyNestedWidget";

		[SerializeField]
		public UnityEngine.UI.Image mImage7;
		[SerializeField]
		public UnityEngine.UI.Image mImage8;


        protected override void ClearUIComponents()
        {
			mImage7 = null;
			mImage8 = null;

        }
    }
}
// Generation Time: 2025年3月23日 17:22:16
// Generation ID: c138cabb-7cbb-4abf-8bad-67de493ef681
