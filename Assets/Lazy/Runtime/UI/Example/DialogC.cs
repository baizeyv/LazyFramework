using UnityEngine;
using UnityEngine.UI;

namespace Lazy.UI.Example
{
    public class DialogC : UIDialog
    {

        [SerializeField]
        public Button mBtn;

        protected override void OnSetup()
        {
            mBtn.onClick.AddListener(() =>
            {
                UIManager.Instance.Close();
            });
        }
    }
}