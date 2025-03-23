using UnityEngine;

namespace Lazy
{
    public abstract class UIWidget : UIMonoBehaviour, IBind
    {
        public virtual string TypeName { get; }
        public string Comment => string.Empty;
        public Transform Transform => transform;
        public BindType BindType => BindType.UIWidget;

        protected override void OnBeforeDestroy()
        {
            OnUIDestroy();
            ClearUIComponents();
        }

        protected virtual void OnUIDestroy(){ }

        protected abstract void ClearUIComponents();

        protected sealed override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}