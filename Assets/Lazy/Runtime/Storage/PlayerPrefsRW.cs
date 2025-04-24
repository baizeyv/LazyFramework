using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// * PlayerPrefs Reader And Writer
    /// </summary>
    public class PlayerPrefsRW : IPrefReader, IPrefWriter
    {
        public string ReadString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public int ReadInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public float ReadFloat(string key, float defaultValue = 0)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void WriteString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void WriteInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void WriteFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
