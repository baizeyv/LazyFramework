namespace Lazy.Serializer
{
    public interface ISerialize
    {
        /// <summary>
        /// * 存储键
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// * 序列化后的内容
        /// </summary>
        public string Serialized { get; }

        public void SerializedLoad(string value);

        public void SerializedSave();
    }
}