using System;
using System.Buffers.Binary;

namespace MiniPandoras.Protocol
{
    /// <summary>
    /// 온라인 대전에서 주고받는 패킷의 틀(Docs/DESIGN_OnlineDuel.md 3장).
    /// 이 폴더는 Unity와 서버(Server/src/DuelServer)가 같은 파일을 함께 컴파일한다. 그래서 UnityEngine을 쓰지 않고(asmdef의 noEngineReferences),
    /// Unity가 컴파일하는 C# 9 문법만 쓴다.
    ///
    /// 패킷 하나 = 머리말 4바이트(본문 길이 ushort, 번호 ushort, 리틀 엔디언) + 본문.
    /// TCP는 바이트가 쪼개지거나 붙어서 오므로 받는 쪽은 쌓인 바이트에서 <see cref="TryReadFrame"/>으로 한 패킷씩 잘라 낸다.
    /// </summary>
    public static class Packet
    {
        /// <summary>패킷 모양이 바뀌면 올린다. 서버는 Hello의 버전이 다르면 받지 않는다.</summary>
        public const ushort Version = 1;

        public const int HeaderSize = 4;

        /// <summary>이보다 긴 본문은 잘못 온 것으로 본다. 가장 큰 MatchFound도 수백 바이트다.</summary>
        public const int MaxBodySize = 1024;

        /// <summary>패킷 하나가 차지할 수 있는 가장 큰 크기. 보낼 버퍼를 이만큼 잡는다.</summary>
        public const int MaxSize = HeaderSize + MaxBodySize;

        /// <summary>패킷을 destination 앞쪽에 머리말과 함께 쓰고 쓴 바이트 수를 돌려준다.</summary>
        public static int Encode<T>(T packet, Span<byte> destination) where T : struct, IPacket
        {
            int room = Math.Min(MaxBodySize, destination.Length - HeaderSize);
            var writer = new PacketWriter(destination.Slice(HeaderSize, room));
            packet.Write(ref writer);
            BinaryPrimitives.WriteUInt16LittleEndian(destination, (ushort)writer.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(2), (ushort)packet.Id);
            return HeaderSize + writer.Length;
        }

        /// <summary>
        /// 쌓인 바이트 맨 앞에서 패킷 하나를 꺼낸다. 머리말이나 본문이 아직 다 오지 않았으면 false다.
        /// consumed만큼 버퍼 앞을 버리고 다시 부르면 다음 패킷이 나온다.
        /// </summary>
        /// <exception cref="PacketFormatException">머리말에 적힌 본문 길이가 <see cref="MaxBodySize"/>보다 길다.</exception>
        public static bool TryReadFrame(ReadOnlySpan<byte> buffer, out PacketId id, out ReadOnlySpan<byte> body, out int consumed)
        {
            id = default;
            body = default;
            consumed = 0;
            if (buffer.Length < HeaderSize)
            {
                return false;
            }

            int size = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            if (size > MaxBodySize)
            {
                throw new PacketFormatException($"본문 길이 {size}바이트는 최대 {MaxBodySize}바이트보다 길다.");
            }

            if (buffer.Length < HeaderSize + size)
            {
                return false;
            }

            id = (PacketId)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2));
            body = buffer.Slice(HeaderSize, size);
            consumed = HeaderSize + size;
            return true;
        }
    }

    /// <summary>
    /// 패킷 하나. 패킷은 주고받는 값의 묶음이라 공개 필드 구조체다(전투의 Swing·Dash와 같다).
    /// 읽기는 패킷마다 정적 메서드 Read(ref PacketReader)로 한다.
    /// </summary>
    public interface IPacket
    {
        PacketId Id { get; }

        void Write(ref PacketWriter writer);
    }

    /// <summary>패킷 번호. 저장되거나 오가는 값이라 번호를 바꾸지 않는다. 새 패킷은 빈 번호를 쓴다.</summary>
    public enum PacketId : ushort
    {
        Hello = 1,
        Welcome = 2,
        Ping = 3,
        Pong = 4,
        Error = 5,

        JoinQueue = 20,
        LeaveQueue = 21,
        MatchFound = 22,
        Ready = 23,
        Start = 24,
        End = 25,

        Move = 40,
        RivalMove = 41,
        Act = 42,
        RivalAct = 43,
        Hit = 44,
        Damage = 45,
        Correct = 46,
    }

    /// <summary>받은 바이트가 패킷 모양에 맞지 않는다. 서버는 이 연결을 끊는다.</summary>
    public sealed class PacketFormatException : Exception
    {
        public PacketFormatException(string message)
            : base(message)
        {
        }
    }
}
