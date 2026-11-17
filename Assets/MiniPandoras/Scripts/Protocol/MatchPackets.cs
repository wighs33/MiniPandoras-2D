namespace MiniPandoras.Protocol
{
    /// <summary>클라이언트 → 서버, 상대를 찾기 시작한다.</summary>
    public struct JoinQueue : IPacket
    {
        /// <summary>대전에 들고 갈 판도라 id(한~세 개).</summary>
        public string[] Pandoras;

        public PacketId Id => PacketId.JoinQueue;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteStrings(Pandoras);
        }

        public static JoinQueue Read(ref PacketReader reader)
        {
            return new JoinQueue { Pandoras = reader.ReadStrings() };
        }
    }

    /// <summary>클라이언트 → 서버, 상대 찾기를 그만둔다.</summary>
    public struct LeaveQueue : IPacket
    {
        public PacketId Id => PacketId.LeaveQueue;

        public void Write(ref PacketWriter writer)
        {
        }

        public static LeaveQueue Read(ref PacketReader reader)
        {
            return new LeaveQueue();
        }
    }

    /// <summary>서버 → 클라이언트, 상대를 찾았다. 받으면 VS 화면을 띄우고 투기장을 불러 Ready를 보낸다.</summary>
    public struct MatchFound : IPacket
    {
        public long MatchId;
        public int RivalId;
        public string RivalNickname;
        public string[] RivalPandoras;
        public int RivalWins;
        public int RivalLosses;

        public PacketId Id => PacketId.MatchFound;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteLong(MatchId);
            writer.WriteInt(RivalId);
            writer.WriteString(RivalNickname);
            writer.WriteStrings(RivalPandoras);
            writer.WriteInt(RivalWins);
            writer.WriteInt(RivalLosses);
        }

        public static MatchFound Read(ref PacketReader reader)
        {
            return new MatchFound
            {
                MatchId = reader.ReadLong(),
                RivalId = reader.ReadInt(),
                RivalNickname = reader.ReadString(),
                RivalPandoras = reader.ReadStrings(),
                RivalWins = reader.ReadInt(),
                RivalLosses = reader.ReadInt(),
            };
        }
    }

    /// <summary>클라이언트 → 서버, 투기장을 다 불렀다. 둘 다 보내면 서버가 Start를 보낸다.</summary>
    public struct Ready : IPacket
    {
        public PacketId Id => PacketId.Ready;

        public void Write(ref PacketWriter writer)
        {
        }

        public static Ready Read(ref PacketReader reader)
        {
            return new Ready();
        }
    }

    /// <summary>서버 → 클라이언트, 이 서버 시각에 싸움이 시작된다(그때까지 3초를 센다).</summary>
    public struct Start : IPacket
    {
        /// <summary>서버 시계, ms.</summary>
        public long StartAt;

        public PacketId Id => PacketId.Start;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteLong(StartAt);
        }

        public static Start Read(ref PacketReader reader)
        {
            return new Start { StartAt = reader.ReadLong() };
        }
    }

    public enum EndReason : byte
    {
        /// <summary>한쪽 체력이 바닥났다.</summary>
        Knockout = 0,
        /// <summary>시간이 다 되어 남은 체력 비율로 정했다.</summary>
        TimeUp = 1,
        /// <summary>한쪽이 나갔거나 끊겼다.</summary>
        Left = 2,
    }

    /// <summary>서버 → 클라이언트, 싸움이 끝났다. 받는 사람 쪽에서 본 승패와 바뀐 전적이다.</summary>
    public struct End : IPacket
    {
        public bool Won;
        public EndReason Reason;
        public int Wins;
        public int Losses;

        public PacketId Id => PacketId.End;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteBool(Won);
            writer.WriteByte((byte)Reason);
            writer.WriteInt(Wins);
            writer.WriteInt(Losses);
        }

        public static End Read(ref PacketReader reader)
        {
            return new End
            {
                Won = reader.ReadBool(),
                Reason = (EndReason)reader.ReadByte(),
                Wins = reader.ReadInt(),
                Losses = reader.ReadInt(),
            };
        }
    }
}
