using System.Collections.Generic;
using Lazy;
using Lazy.Manage;
using Lazy.Singleton;

namespace Lazy.Serializer
{
    [ManagerLateUpdate]
    public class StorageManager : Singleton<StorageManager>, IManager
    {
        /// <summary>
        /// * 本地存储写工具
        /// </summary>
        private IPrefWriter _prefWriter;

        /// <summary>
        /// * 本地存储读工具
        /// </summary>
        private IPrefReader _prefReader;

        /// <summary>
        /// * 是否有脏数据 (未保存的数据)
        /// </summary>
        private bool _dirty;

        private StorageManager() { }

        /// <summary>
        /// * 设置读写工具一体
        /// </summary>
        /// <param name="rw"></param>
        /// <typeparam name="T"></typeparam>
        public void SetReaderWriter<T>(T rw)
            where T : IPrefReader, IPrefWriter
        {
            _prefWriter = rw;
            _prefReader = rw;
        }

        /// <summary>
        /// * 设置读工具
        /// </summary>
        /// <param name="reader"></param>
        /// <typeparam name="T"></typeparam>
        public void SetReader<T>(T reader)
            where T : IPrefReader
        {
            _prefReader = reader;
        }

        /// <summary>
        /// * 设置写工具
        /// </summary>
        /// <param name="writer"></param>
        /// <typeparam name="T"></typeparam>
        public void SetWrite<T>(T writer)
            where T : IPrefWriter
        {
            _prefWriter = writer;
        }

        /// <summary>
        /// * 注册序列化工具
        /// # T 为object的时候为非基本类型的序列化器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RegisterSerializer<T>(Serializer<T> serializer)
        {
            SerializerRegistry<T>.Register(serializer);
        }

        public bool HasKey(string key)
        {
            return _prefReader.HasKey(key);
        }

        /// <summary>
        /// * 使用序列化器后读取到的值
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (!HasKey(key))
                return defaultValue;
            var serializer = SerializerRegistry<T>.Serializer;
            var data = _prefReader.ReadString(key);
            return serializer.Deserialize(data);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return _prefReader.ReadInt(key, defaultValue);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _prefReader.ReadFloat(key, defaultValue);
        }

        public string GetString(string key, string defaultValue = "")
        {
            return _prefReader.ReadString(key, defaultValue);
        }

        /// <summary>
        /// * 使用序列化工具后存储string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public void Set<T>(string key, T value)
        {
            var serializer = SerializerRegistry<T>.Serializer;
            var content = serializer.Serialize(value);
            _prefWriter.WriteString(key, content);
            _dirty = true;
        }

        /// <summary>
        /// * 直接设置Int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetInt(string key, int value)
        {
            _prefWriter.WriteInt(key, value);
            _dirty = true;
        }

        /// <summary>
        /// * 直接设置Float
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetFloat(string key, float value)
        {
            _prefWriter.WriteFloat(key, value);
            _dirty = true;
        }

        /// <summary>
        /// * 直接设置String
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetString(string key, string value)
        {
            _prefWriter.WriteString(key, value);
            _dirty = true;
        }

        public override void OnSingletonInitialize()
        {
            // # 默认的读写工具为PlayersPref
            var rw = new PlayerPrefsRW();
            _prefWriter = rw;
            _prefReader = rw;
            // # 注册默认序列化工具
            RegisterSerializer(new StringSerializer());
            RegisterSerializer(new IntSerializer());
            RegisterSerializer(new FloatSerializer());
            // # 非基本类型的序列化器
            RegisterSerializer(new JsonSerializer<object>());
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate()
        {
            if (_dirty)
            {
                _prefWriter.Save();
                _dirty = false;
            }
        }

        public void OnDestroyRelease() { }

        public void OnGui() { }
    }
}
