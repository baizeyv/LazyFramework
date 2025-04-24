using UnityEngine.UI;

namespace Lazy.Example
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
