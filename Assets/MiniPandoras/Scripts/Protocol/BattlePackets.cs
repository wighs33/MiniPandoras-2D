namespace MiniPandoras.Protocol
{
    /// <summary>캐릭터가 지금 하고 있는 동작. 상대 화면에서 그림을 고르는 데 쓴다.</summary>
    public enum Motion : byte
    {
        Stand = 0,
        Walk = 1,
        Swing = 2,
        Cast = 3,
        Dash = 4,
        Hurt = 5,
        Down = 6,
    }

    /// <summary>
    /// 클라이언트 → 서버, 내 캐릭터의 지금 모습. 초당 20번 보낸다.
    /// 이동은 클라이언트가 정하고, 서버는 속도와 투기장 밖만 검사한다(어기면 Correct).
    /// </summary>
    public struct Move : IPacket
    {
        /// <summary>보낸 차례. Correct가 어느 Move까지 보고 되돌린 것인지 알린다.</summary>
        public uint Seq;
        /// <summary>클라이언트 전투 프레임(60fps).</summary>
        public int Frame;
        /// <summary>발 위치(월드 좌표, 한 칸 = 1).</summary>
        public float X;
        public float Y;
        /// <summary>바라보는 쪽(-1, 0, 1).</summary>
        public sbyte FaceX;
        public sbyte FaceY;
        public Motion Motion;

        public PacketId Id => PacketId.Move;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteUInt(Seq);
            writer.WriteInt(Frame);
            writer.WriteFloat(X);
            writer.WriteFloat(Y);
            writer.WriteSByte(FaceX);
            writer.WriteSByte(FaceY);
            writer.WriteByte((byte)Motion);
        }

        public static Move Read(ref PacketReader reader)
        {
            return new Move
            {
                Seq = reader.ReadUInt(),
                Frame = reader.ReadInt(),
                X = reader.ReadFloat(),
                Y = reader.ReadFloat(),
                FaceX = reader.ReadSByte(),
                FaceY = reader.ReadSByte(),
                Motion = (Motion)reader.ReadByte(),
            };
        }
    }

    /// <summary>서버 → 클라이언트, 검사를 통과한 상대의 Move. 받는 쪽은 받은 위치 둘 사이를 이어 보인다.</summary>
    public struct RivalMove : IPacket
    {
        /// <summary>서버가 받은 때(서버 시계, ms).</summary>
        public long ServerTime;
        public float X;
        public float Y;
        public sbyte FaceX;
        public sbyte FaceY;
        public Motion Motion;

        public PacketId Id => PacketId.RivalMove;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteLong(ServerTime);
            writer.WriteFloat(X);
            writer.WriteFloat(Y);
            writer.WriteSByte(FaceX);
            writer.WriteSByte(FaceY);
            writer.WriteByte((byte)Motion);
        }

        public static RivalMove Read(ref PacketReader reader)
        {
            return new RivalMove
            {
                ServerTime = reader.ReadLong(),
                X = reader.ReadFloat(),
                Y = reader.ReadFloat(),
                FaceX = reader.ReadSByte(),
                FaceY = reader.ReadSByte(),
                Motion = (Motion)reader.ReadByte(),
            };
        }
    }

    public enum ActKind : byte
    {
        /// <summary>기본 공격(방망이 휘두르기).</summary>
        Attack = 0,
        Skill = 1,
        /// <summary>궁극기. 쿨다운과 마나 대신 궁극기 게이지로 쓴다.</summary>
        Ultimate = 2,
    }

    /// <summary>클라이언트 → 서버, 공격이나 스킬을 썼다. 서버는 쓸 수 있는지(쿨다운·마나·게이지) 보고 상대에게 넘긴다.</summary>
    public struct Act : IPacket
    {
        /// <summary>보낸 차례. 이 공격이 맞으면 Hit가 이 값을 가리킨다.</summary>
        public uint Seq;
        public ActKind Kind;
        /// <summary>스킬 id(Data/Battle/Skills). 기본 공격이면 빈 글이다.</summary>
        public string Skill;
        /// <summary>겨냥한 쪽(길이 1).</summary>
        public float AimX;
        public float AimY;

        public PacketId Id => PacketId.Act;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteUInt(Seq);
            writer.WriteByte((byte)Kind);
            writer.WriteString(Skill);
            writer.WriteFloat(AimX);
            writer.WriteFloat(AimY);
        }

        public static Act Read(ref PacketReader reader)
        {
            return new Act
            {
                Seq = reader.ReadUInt(),
                Kind = (ActKind)reader.ReadByte(),
                Skill = reader.ReadString(),
                AimX = reader.ReadFloat(),
                AimY = reader.ReadFloat(),
            };
        }
    }

    /// <summary>서버 → 클라이언트, 검사를 통과한 상대의 Act. 받는 쪽은 그 스킬의 그림만 튼다(피해는 Damage로 온다).</summary>
    public struct RivalAct : IPacket
    {
        public ActKind Kind;
        public string Skill;
        public float AimX;
        public float AimY;

        public PacketId Id => PacketId.RivalAct;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteByte((byte)Kind);
            writer.WriteString(Skill);
            writer.WriteFloat(AimX);
            writer.WriteFloat(AimY);
        }

        public static RivalAct Read(ref PacketReader reader)
        {
            return new RivalAct
            {
                Kind = (ActKind)reader.ReadByte(),
                Skill = reader.ReadString(),
                AimX = reader.ReadFloat(),
                AimY = reader.ReadFloat(),
            };
        }
    }

    /// <summary>
    /// 클라이언트 → 서버, 내 공격이 상대에게 닿았다(때린 쪽이 알린다).
    /// 서버는 사거리, 스킬이 살아 있는 시간, 타수를 보고 통과시키면 피해를 계산해 Damage를 보낸다.
    /// </summary>
    public struct Hit : IPacket
    {
        /// <summary>닿은 공격의 Act.Seq.</summary>
        public uint ActSeq;
        /// <summary>그 공격의 몇 번째 타격인지(장판처럼 여러 번 치는 스킬). 0부터.</summary>
        public byte HitIndex;

        public PacketId Id => PacketId.Hit;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteUInt(ActSeq);
            writer.WriteByte(HitIndex);
        }

        public static Hit Read(ref PacketReader reader)
        {
            return new Hit
            {
                ActSeq = reader.ReadUInt(),
                HitIndex = reader.ReadByte(),
            };
        }
    }

    public enum HitOutcome : byte
    {
        Hit = 0,
        Critical = 1,
        /// <summary>피했다(회피 확률). 피해 0.</summary>
        Evaded = 2,
        /// <summary>차원 판도라의 흘려보내기로 비켜 갔다. 피해 0.</summary>
        Phased = 3,
    }

    /// <summary>서버 → 두 클라이언트, 누군가 맞았다. 체력은 이 패킷으로만 깎인다.</summary>
    public struct Damage : IPacket
    {
        /// <summary>맞은 플레이어(Welcome.PlayerId).</summary>
        public int TargetId;
        public HitOutcome Outcome;
        public int Amount;
        public int HpLeft;
        /// <summary>밀려나는 속도(한 프레임에 가는 월드 길이). 6프레임 동안 밀린다.</summary>
        public float KnockX;
        public float KnockY;
        /// <summary>상태이상(Data의 SkillStatus와 같다). 프레임 수.</summary>
        public ushort SlowFrames;
        public ushort FreezeFrames;
        /// <summary>30프레임마다 4번, 최대 체력의 이 비율만큼 깎는다. 0이면 없다.</summary>
        public float Dot;

        public PacketId Id => PacketId.Damage;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteInt(TargetId);
            writer.WriteByte((byte)Outcome);
            writer.WriteInt(Amount);
            writer.WriteInt(HpLeft);
            writer.WriteFloat(KnockX);
            writer.WriteFloat(KnockY);
            writer.WriteUShort(SlowFrames);
            writer.WriteUShort(FreezeFrames);
            writer.WriteFloat(Dot);
        }

        public static Damage Read(ref PacketReader reader)
        {
            return new Damage
            {
                TargetId = reader.ReadInt(),
                Outcome = (HitOutcome)reader.ReadByte(),
                Amount = reader.ReadInt(),
                HpLeft = reader.ReadInt(),
                KnockX = reader.ReadFloat(),
                KnockY = reader.ReadFloat(),
                SlowFrames = reader.ReadUShort(),
                FreezeFrames = reader.ReadUShort(),
                Dot = reader.ReadFloat(),
            };
        }
    }

    /// <summary>서버 → 클라이언트, 보낸 Move가 검사에 걸렸다. 이 위치로 돌아간다.</summary>
    public struct Correct : IPacket
    {
        /// <summary>서버가 마지막으로 본 Move.Seq.</summary>
        public uint Seq;
        public float X;
        public float Y;

        public PacketId Id => PacketId.Correct;

        public void Write(ref PacketWriter writer)
        {
            writer.WriteUInt(Seq);
            writer.WriteFloat(X);
            writer.WriteFloat(Y);
        }

        public static Correct Read(ref PacketReader reader)
        {
            return new Correct
            {
                Seq = reader.ReadUInt(),
                X = reader.ReadFloat(),
                Y = reader.ReadFloat(),
            };
        }
    }
}
