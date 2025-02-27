using System;

namespace Lazy.Editor.NinePatchSlicer
{
    [Serializable]
    public class SliceOptions
    {
        public int tolerate = 0;

        public int centerSize = 2;

        public int margin = 2;

        public static SliceOptions Default => new SliceOptions();
    }
}