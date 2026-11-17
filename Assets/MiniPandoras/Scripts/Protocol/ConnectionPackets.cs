namespace MiniPandoras.Protocol
{
    /// <summary>클라이언트 → 서버, 연결하고 맨 처음 보낸다.</summary>
    public struct Hello : IPacket
    {
        /// <summary><see cref="Packet.Version"/>. 다르면 서버가 <see cref="ErrorCode.VersionMismatch"/>로 닫는다.</summary>
        public ushort Version;
        /// <summary>로그인 토큰. 계정이 없는 1단계에서는 빈 글이다.</summary>
        public string Token;
        public string Nickname;

        public PacketId Id => PacketId.Hello;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteUShort(Version);
            writer.WriteString(Token);
            writer.WriteString(Nickname);
        }

        public static Hello Read(ref PacketReader reader)
        {
            return new Hello
            {
                Version = reader.ReadUShort(),
                Token = reader.ReadString(),
                Nickname = reader.ReadString(),
            };
        }
    }

    /// <summary>서버 → 클라이언트, Hello를 받아들였다.</summary>
    public struct Welcome : IPacket
    {
        public int PlayerId;
        public string Nickname;

        public PacketId Id => PacketId.Welcome;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteInt(PlayerId);
            writer.WriteString(Nickname);
        }

        public static Welcome Read(ref PacketReader reader)
        {
            return new Welcome
            {
                PlayerId = reader.ReadInt(),
                Nickname = reader.ReadString(),
            };
        }
    }

    /// <summary>클라이언트 → 서버, 왕복 시간을 잰다.</summary>
    public struct Ping : IPacket
    {
        /// <summary>보낸 때(클라이언트 시계, ms). Pong에 그대로 돌아온다.</summary>
        public long ClientTime;

        public PacketId Id => PacketId.Ping;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteLong(ClientTime);
        }

        public static Ping Read(ref PacketReader reader)
        {
            return new Ping { ClientTime = reader.ReadLong() };
        }
    }

    /// <summary>서버 → 클라이언트, Ping의 답. 클라이언트는 왕복 시간의 절반으로 서버 시계와의 차이를 잰다.</summary>
    public struct Pong : IPacket
    {
        public long ClientTime;
        /// <summary>답한 때(서버 시계, ms).</summary>
        public long ServerTime;

        public PacketId Id => PacketId.Pong;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteLong(ClientTime);
            writer.WriteLong(ServerTime);
        }

        public static Pong Read(ref PacketReader reader)
        {
            return new Pong
            {
                ClientTime = reader.ReadLong(),
                ServerTime = reader.ReadLong(),
            };
        }
    }

    public enum ErrorCode : ushort
    {
        /// <summary>프로토콜 버전이 다르다. 앱을 업데이트해야 한다.</summary>
        VersionMismatch = 1,
        /// <summary>로그인 토큰이 없거나 만료되었다.</summary>
        BadToken = 2,
        /// <summary>패킷 모양이 틀렸다. 서버가 곧 연결을 끊는다.</summary>
        BadPacket = 3,
        /// <summary>대전에 들고 갈 수 없는 판도라다.</summary>
        BadLoadout = 4,
    }

    /// <summary>서버 → 클라이언트, 요청을 받지 않았다.</summary>
    public struct Error : IPacket
    {
        public ErrorCode Code;

        public PacketId Id => PacketId.Error;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteUShort((ushort)Code);
        }

        public static Error Read(ref PacketReader reader)
        {
            return new Error { Code = (ErrorCode)reader.ReadUShort() };
        }
    }
}
