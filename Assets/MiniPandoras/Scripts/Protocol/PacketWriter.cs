using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace MiniPandoras.Protocol
{
    /// <summary>
    /// 패킷 본문을 바이트로 쓴다. 숫자는 리틀 엔디언, 글은 UTF-8 앞에 바이트 수(ushort)를 붙이고, 글 목록은 앞에 개수(byte)를 붙인다.
    /// 받은 버퍼에 바로 쓰므로 메모리를 할당하지 않는다(초당 20번 보내는 Move도 그대로 보낼 수 있다).
    /// </summary>
    public ref struct PacketWriter
    {
        private readonly Span<byte> buffer;
        private int length;

        public PacketWriter(Span<byte> buffer)
        {
            this.buffer = buffer;
            length = 0;
        }

        /// <summary>지금까지 쓴 바이트 수.</summary>
        public int Length => length;

        public void WriteByte(byte value)
        {
            Take(1)[0] = value;
        }

        public void WriteSByte(sbyte value)
        {
            WriteByte((byte)value);
        }

        public void WriteBool(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteUShort(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(Take(2), value);
        }

        public void WriteInt(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(Take(4), value);
        }

        public void WriteUInt(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(Take(4), value);
        }

        public void WriteLong(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(Take(8), value);
        }

        public void WriteFloat(float value)
        {
            // Unity가 쓰는 .NET Standard 2.1에는 BinaryPrimitives.WriteSingleLittleEndian이 없어서 비트를 int로 옮겨 쓴다.
            WriteInt(BitConverter.SingleToInt32Bits(value));
        }

        public void WriteString(string value)
        {
            int size = Encoding.UTF8.GetByteCount(value);
            if (size > ushort.MaxValue)
            {
                throw new ArgumentException($"글이 {size}바이트라 패킷에 넣을 수 없다.", nameof(value));
            }

            WriteUShort((ushort)size);
            Encoding.UTF8.GetBytes(value.AsSpan(), Take(size));
        }

        public void WriteStrings(IReadOnlyList<string> values)
        {
            if (values.Count > byte.MaxValue)
            {
                throw new ArgumentException($"글 {values.Count}개는 패킷에 넣을 수 없다.", nameof(values));
            }

            WriteByte((byte)values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                WriteString(values[i]);
            }
        }

        private Span<byte> Take(int size)
        {
            if (length + size > buffer.Length)
            {
                throw new InvalidOperationException($"패킷 본문이 버퍼({buffer.Length}바이트)보다 길다.");
            }

            Span<byte> slice = buffer.Slice(length, size);
            length += size;
            return slice;
        }
    }
}
