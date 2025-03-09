using System;

namespace Editor.Lazy.PrefEditor
{
    [Serializable]
    public class PreferenceEntry
    {
        public enum PrefType
        {
            String = 0,
            Int = 1,
            Float = 2
        }

        public PrefType typeSelection;

        public string key;

        public string stringValue;

        public int intValue;

        public float floatValue;

        public string ValueAsString()
        {
            return typeSelection switch
            {
                PrefType.String => stringValue,
                PrefType.Int => intValue.ToString(),
                PrefType.Float => floatValue.ToString(),
                _ => string.Empty
            };
        }
    }
}