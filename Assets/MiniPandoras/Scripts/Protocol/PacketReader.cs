using System;
using System.Buffers.Binary;
using System.Text;

namespace MiniPandoras.Protocol
{
    /// <summary>
    /// 패킷 본문을 <see cref="PacketWriter"/>가 쓴 차례대로 읽는다. 본문이 모자라면 <see cref="PacketFormatException"/>을 던진다
    /// (상대가 보낸 바이트는 믿지 않는다).
    /// </summary>
    public ref struct PacketReader
    {
        private readonly ReadOnlySpan<byte> body;
        private int position;

        public PacketReader(ReadOnlySpan<byte> body)
        {
            this.body = body;
            position = 0;
        }

        /// <summary>아직 읽지 않은 바이트 수.</summary>
        public int Remaining => body.Length - position;

        public byte ReadByte()
        {
            return Take(1)[0];
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public ushort ReadUShort()
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        }

        public int ReadInt()
        {
            return BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        }

        public uint ReadUInt()
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
        }

        public long ReadLong()
        {
            return BinaryPrimitives.ReadInt64LittleEndian(Take(8));
        }

        public float ReadFloat()
        {
            return BitConverter.Int32BitsToSingle(ReadInt());
        }

        public string ReadString()
        {
            int size = ReadUShort();
            return Encoding.UTF8.GetString(Take(size));
        }

        public string[] ReadStrings()
        {
            var values = new string[ReadByte()];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ReadString();
            }

            return values;
        }

        private ReadOnlySpan<byte> Take(int size)
        {
            if (size > Remaining)
            {
                throw new PacketFormatException($"본문에 {size}바이트가 더 있어야 하는데 {Remaining}바이트만 남았다.");
            }

            ReadOnlySpan<byte> slice = body.Slice(position, size);
            position += size;
            return slice;
        }
    }
}
