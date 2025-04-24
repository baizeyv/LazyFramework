using UnityEngine.UI;

namespace Lazy.Example
{
    public class DialogA : UIDialog
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
