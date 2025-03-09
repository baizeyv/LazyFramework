using UnityEngine;

namespace Lazy.Editor
{
    public class Styles
    {
        public class Colors
        {
            public static readonly Color Rosewater = new(244f / 255f, 219f / 255f, 214f / 255f, 1);
            public static readonly Color Flamingo = new(240f / 255f, 198f / 255f, 198f / 255f, 1);
            public static readonly Color Pink = new(245f / 255f, 189f / 255f, 230f / 255f, 1);
            public static readonly Color Mauve = new(198f / 255f, 160f / 255f, 246f / 255f, 1);
            public static readonly Color Red = new(237f / 255f, 135f / 255f, 150f / 255f, 1);
            public static readonly Color Maroon = new(238f / 255f, 153f / 255f, 160f / 255f, 1);
            public static readonly Color Peach = new(245f / 255f, 169f / 255f, 127f / 255f, 1);
            public static readonly Color Yellow = new(238f / 255f, 212f / 255f, 159f / 255f, 1);
            public static readonly Color Green = new(166f / 255f, 218f / 255f, 149f / 255f, 1);
            public static readonly Color Teal = new(139f / 255f, 213f / 255f, 202f / 255f, 1);
            public static readonly Color Sky = new(145f / 255f, 215f / 255f, 227f / 255f, 1);
            public static readonly Color Sapphire = new(125f / 255f, 196f / 255f, 228f / 255f, 1);
            public static readonly Color Blue = new(138f / 255f, 173f / 255f, 244f / 255f, 1);
            public static readonly Color Lavender = new(183f / 255f, 189f / 255f, 248f / 255f, 1);
            public static readonly Color Text = new(202f / 255f, 211f / 255f, 245f / 255f, 1);
            public static readonly Color DarkGray = new(0.09f,0.09f,0.09f,1f);
        }

        private static GUIStyle Icon;

        public static GUIStyle icon
        {
            get
            {
                if (Icon == null)
                {
                    Icon = new GUIStyle();
                    Icon.fixedWidth = 15f;
                    Icon.fixedHeight = 15f;
                    Icon.margin = new(2, 2, 2, 2);
                }
                return Icon;
            }
        }
    }
}