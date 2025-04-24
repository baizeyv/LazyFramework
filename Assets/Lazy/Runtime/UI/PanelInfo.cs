using System;
using Lazy;

namespace Lazy
{
    public class PanelInfo : IReference
    {
        public IPanelData Data;

        public UILayer Layer = UILayer.PanelLow;

        public Type PanelType;

        public static PanelInfo Obtain(UILayer layer, IPanelData data, Type panelType)
        {
            var info = ReferencePool.Instance.Obtain<PanelInfo>();
            info.Data = data;
            info.Layer = layer;
            info.PanelType = panelType;
            return info;
        }

        public void Free()
        {
            ReferencePool.Instance.Free(this);
        }

        public void Clear()
        {
            Data = null;
            Layer = UILayer.PanelLow;
            PanelType = null;
        }
    }
}
