using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Lazy.Debugger.Tool
{
    /// <summary>
    /// * 使用这个脚本可以捕捉到哪些元素在何时会导致Canvas重建
    /// </summary>
    public class DebugCanvasRebuild : MonoBehaviour
    {
        private IList<ICanvasElement> m_LayoutRebuildQueue;
        private IList<ICanvasElement> m_GraphicRebuildQueue;

        private void Awake()
        {
            System.Type type = typeof(CanvasUpdateRegistry);
            FieldInfo field = type.GetField(
                "m_LayoutRebuildQueue",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            m_LayoutRebuildQueue =
                (IList<ICanvasElement>)field.GetValue(CanvasUpdateRegistry.instance);
            field = type.GetField(
                "m_GraphicRebuildQueue",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            m_GraphicRebuildQueue =
                (IList<ICanvasElement>)field.GetValue(CanvasUpdateRegistry.instance);
        }

        private void Update()
        {
            for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
            {
                var rebuild = m_LayoutRebuildQueue[j];
                if (ObjectValidFofUpdate(rebuild))
                {
                    Debug.LogFormat(
                        "m_LayoutRebuildQueue中{0}引起{1}网格重建",
                        rebuild.transform.name,
                        rebuild.transform.GetComponent<Graphic>()?.canvas.name
                    );
                }
            }

            for (int j = 0; j < m_GraphicRebuildQueue.Count; j++)
            {
                var element = m_GraphicRebuildQueue[j];
                if (ObjectValidFofUpdate(element))
                {
                    Debug.LogFormat(
                        "m_GraphicRebuildQueue中{0}引起{1}网格重建",
                        element.transform.name,
                        element.transform.GetComponent<Graphic>().canvas.name
                    );
                }
            }
        }

        private bool ObjectValidFofUpdate(ICanvasElement element)
        {
            var valid = element != null;
            var isUnityObject = element is Object;
            if (isUnityObject)
                valid = (element as Object) != null;
            return valid;
        }
    }
}