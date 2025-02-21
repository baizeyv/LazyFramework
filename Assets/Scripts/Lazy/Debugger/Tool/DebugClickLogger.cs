using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lazy.Debugger.Tool
{
    /// <summary>
    /// * 使用这个脚本可以输出当前点击的对象的名称
    /// </summary>
    public class DebugClickLogger : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) // 检测鼠标左键点击
            {
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                pointerEventData.position = Input.mousePosition;

                // 存储射线检测结果
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerEventData, results);

                // 遍历所有检测到的对象
                foreach (var result in results)
                {
                    Debug.Log("Clicked on: " + result.gameObject.name);
                }

                // 如果未检测到任何对象
                if (results.Count == 0)
                {
                    Debug.Log("No UI object clicked.");
                }
            }
        }
    }
}