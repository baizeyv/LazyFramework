using System.IO;
using System.Text;

namespace LazyEditor
{
    public class ByteArray
    {
        /// <summary>
        /// * 内存流
        /// </summary>
        private MemoryStream _stream;

        /// <summary>
        /// * 二进制读取器
        /// </summary>
        private BinaryReader _reader;

        public ByteArray(byte[] buffer)
        {
            _stream = new MemoryStream(buffer);
            _reader = new BinaryReader(_stream);
        }

        /// <summary>
        /// * 判断是否可读取
        /// </summary>
        /// <returns></returns>
        public bool ReadAvailable()
        {
            return _reader?.PeekChar() != -1;
        }

        /// <summary>
        /// * 读取Byte数据
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public bool ReadBool()
        {
            return _reader.ReadBoolean();
        }

        public sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        public short ReadShort()
        {
            return _reader.ReadInt16();
        }

        public ushort ReadUShort()
        {
            return _reader.ReadUInt16();
        }

        public int ReadInt()
        {
            return _reader.ReadInt32();
        }

        public uint ReadUInt()
        {
            return _reader.ReadUInt32();
        }

        public long ReadLong()
        {
            return _reader.ReadInt64();
        }

        public ulong ReadULong()
        {
            return _reader.ReadUInt64();
        }

        public float ReadFloat()
        {
            return _reader.ReadSingle();
        }

        public double ReadDouble()
        {
            return _reader.ReadDouble();
        }

        public string ReadString()
        {
            return _reader.ReadString();
        }

        public string ReadUTF()
        {
            var len = _reader.ReadInt32();
            var stringBytes = _reader.ReadBytes(len);
            return Encoding.UTF8.GetString(stringBytes);
        }

        public byte[] ReadBytes(int len)
        {
            return _reader.ReadBytes(len);
        }

        public int[] ReadIntArray()
        {
            var len = _reader.ReadInt32();
            var result = new int[len];
            for (var i = 0; i < len; i++)
                result[i] = _reader.ReadInt32();

            return result;
        }

        public float[] ReadFloatArray()
        {
            var len = _reader.ReadInt32();
            var result = new float[len];
            for (var i = 0; i < len; i++)
                result[i] = _reader.ReadSingle();

            return result;
        }

        public string[] ReadStringArray()
        {
            var len = _reader.ReadInt32();
            var result = new string[len];
            for (var i = 0; i < len; i++)
                result[i] = ReadUTF();

            return result;
        }
    }
}
