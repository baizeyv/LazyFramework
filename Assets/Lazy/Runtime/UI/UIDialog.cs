using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.UI
{
    public class UIDialog : UIPanel, IDialog
    {
        private const int SortingOrder = 888;

        public override void Setup(IPanelData panelData)
        {
            base.Setup(panelData);
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = SortingOrder;
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            var canvas = GetComponent<Canvas>();
            base.OnValidate();
            if (!canvas.overrideSorting)
            {
                EditorApplication.delayCall += () =>
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = SortingOrder;
                };
            }
            else
            {
                if (canvas.sortingOrder != SortingOrder)
                    EditorApplication.delayCall += () =>
                    {
                        canvas.sortingOrder = SortingOrder;
                    };
            }
        }

#endif
    }
}
