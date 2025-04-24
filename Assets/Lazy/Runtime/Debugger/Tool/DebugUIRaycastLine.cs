using UnityEngine;
using UnityEngine.UI;

namespace Lazy
{
    /// <summary>
    /// * 使用这个来在Scene中显示激活了raycastTarget的对象 (将被蓝线框住)
    /// </summary>
    public class DebugUIRaycastLine : MonoBehaviour
    {
        public Color customColor = Color.blue;

        private static Vector3[] fourCorners = new Vector3[4];

        private void OnDrawGizmos()
        {
#if UNITY_6000_0_OR_NEWER
            foreach (
                var g in FindObjectsByType<MaskableGraphic>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
#else
            foreach (MaskableGraphic g in FindObjectsOfType<MaskableGraphic>())
#endif
            {
                if (!g.raycastTarget)
                    continue;
                var rectTransform = g.transform as RectTransform;
                rectTransform.GetWorldCorners(fourCorners);
                Gizmos.color = customColor;
                for (var i = 0; i < 4; i++)
                    Gizmos.DrawLine(fourCorners[i], fourCorners[(i + 1) % 4]);
            }
        }
    }
}
