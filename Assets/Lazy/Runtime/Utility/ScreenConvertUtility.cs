namespace Lazy
{
    public static class ScreenConvertUtility
    {
        public static float ScreenDpi { get; set; }

        /// <summary>将像素转换为英寸。</summary>
        /// <param name="pixels">像素。</param>
        /// <returns>英寸。</returns>
        public static float GetInchesFromPixels(float pixels)
        {
            if ((double)ScreenDpi <= 0.0)
                return 0;
            return pixels / ScreenDpi;
        }

        /// <summary>将英寸转换为像素。</summary>
        /// <param name="inches">英寸。</param>
        /// <returns>像素。</returns>
        public static float GetPixelsFromInches(float inches)
        {
            if ((double)ScreenDpi <= 0.0)
                return 0;
            return inches * ScreenDpi;
        }

        /// <summary>将像素转换为厘米。</summary>
        /// <param name="pixels">像素。</param>
        /// <returns>厘米。</returns>
        public static float GetCentimetersFromPixels(float pixels)
        {
            if ((double)ScreenDpi <= 0.0)
                return 0;
            return 2.54f * pixels / ScreenDpi;
        }
    }
}
