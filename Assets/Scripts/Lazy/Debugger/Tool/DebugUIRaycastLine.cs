using UnityEngine;
using UnityEngine.UI;

namespace Lazy.Debugger.Tool
{
    /// <summary>
    /// * 使用这个来在Scene中显示激活了raycastTarget的对象 (将被蓝线框住)
    /// </summary>
    public class DebugUIRaycastLine : MonoBehaviour
    {
        public Color customColor = Color.blue;

        static Vector3[] fourCorners = new Vector3[4];

        private void OnDrawGizmos()
        {
            foreach (MaskableGraphic g in FindObjectsOfType<MaskableGraphic>())
            {
                if (!g.raycastTarget)
                    continue;
                RectTransform rectTransform = g.transform as RectTransform;
                rectTransform.GetWorldCorners(fourCorners);
                Gizmos.color = customColor;
                for (int i = 0; i < 4; i++)
                    Gizmos.DrawLine(fourCorners[i], fourCorners[(i + 1) % 4]);
            }
        }
    }
}