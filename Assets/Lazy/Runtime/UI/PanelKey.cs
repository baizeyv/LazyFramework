using System;

namespace Lazy
{
    public class PanelKey : IReference
    {
        public Type PanelType;

        public string GameObjectName;

        public UILayer Layer = UILayer.PanelLow;

        public IPanelData Data;

        public IPanel Panel;

        public PanelOpenType OpenType = PanelOpenType.Single;

        public static PanelKey Obtain()
        {
            return ReferencePool.Instance.Obtain<PanelKey>();
        }

        public void Free()
        {
            ReferencePool.Instance.Free(this);
        }

        public void Clear()
        {
            PanelType = null;
            Layer = UILayer.PanelLow;
            Data = null;
            Panel = null;
            OpenType = PanelOpenType.Single;
            GameObjectName = null;
        }
    }
}
