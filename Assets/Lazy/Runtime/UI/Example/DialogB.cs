using UnityEngine.UI;

namespace Lazy.UI.Example
{
    public class DialogB : UIDialog
    {

        public Button btn;

        protected override void OnSetup()
        {
            btn.onClick.AddListener(() =>
            {
                UIManager.Instance.Close();
            });
        }
    }
}