using TMPro;
using UnityEngine;

namespace Lazy
{
    public class RedDot : MonoBehaviour
    {
        [Header("红点前缀名称")]
        public string trieName;

        [Header("数量文本(可为空)")]
        public TMP_Text text;

        private void Start()
        {
            // # 一开始先调用一次,来矫正初始显示
            OnCountChange(App.RedDot.GetValue(trieName));
            App.RedDot.AddListener(trieName, OnCountChange);
        }

        private void OnDestroy()
        {
            App.RedDot.RemoveListener(trieName, OnCountChange);
        }

        private void OnCountChange(int count)
        {
            if (count > 0)
            {
                this.SetVisible(true);
                if (text != null)
                    text.text = count.ToString();
            }
            else
            {
                this.SetVisible(false);
            }
        }
    }
}
