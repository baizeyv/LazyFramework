using UnityEngine.UI;

namespace Lazy.UI.Common
{
	/// <summary>
	/// 替换空 Image 扩大点击区域，降低 Drawcall。
	/// </summary>
    public class Empty4Raycast : MaskableGraphic
    {
		protected Empty4Raycast()
		{
			useLegacyMeshGeneration = false;
		}

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			toFill.Clear();
		}
    }
}