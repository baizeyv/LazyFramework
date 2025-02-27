using UnityEngine;

namespace Lazy.Editor.NinePatchSlicer
{
    public class SlicedTexture
    {
        public SlicedTexture(Texture2D texture, Border border)
        {
            Texture = texture;
            Border = border;
        }

        public Texture2D Texture { get; }

        public Border Border { get; }
    }

    public readonly struct Border
    {
        public Border(int left, int bottom, int right, int top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        public Vector4 ToVector4()
        {
            return new Vector4(Left, Bottom, Right, Top);
        }

        private int Left { get; }
        private int Top { get; }
        private int Right { get; }
        private int Bottom { get; }
    }
}