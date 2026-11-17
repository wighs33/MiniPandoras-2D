using System;
using System.Collections.Generic;
using MiniPandoras.Protocol;
using Xunit;

namespace MiniPandoras.Server.Tests
{
    /// <summary>패킷을 쓰고 다시 읽으면 같은 값이 나오고, TCP처럼 쪼개지거나 붙어 온 바이트도 한 패킷씩 잘라 내는지 본다.</summary>
    public sealed class PacketTests
    {
        private delegate T ReadPacket<T>(ref PacketReader reader);

        [Fact]
        public void BattlePacketsRoundTrip()
        {
            var move = new Move { Seq = 7, Frame = 1234, X = 12.5f, Y = -8.25f, FaceX = -1, FaceY = 0, Motion = Motion.Dash };
            Assert.Equal(move, RoundTrip(move, Move.Read));

            var rivalMove = new RivalMove { ServerTime = 1_794_749_860_006, X = 3f, Y = -4f, FaceX = 0, FaceY = 1, Motion = Motion.Walk };
            Assert.Equal(rivalMove, RoundTrip(rivalMove, RivalMove.Read));

            var act = new Act { Seq = 3, Kind = ActKind.Skill, Skill = "sandStorm", AimX = 0.6f, AimY = -0.8f };
            Assert.Equal(act, RoundTrip(act, Act.Read));

            var rivalAct = new RivalAct { Kind = ActKind.Attack, Skill = "", AimX = 1f, AimY = 0f };
            Assert.Equal(rivalAct, RoundTrip(rivalAct, RivalAct.Read));

            var hit = new Hit { ActSeq = 3, HitIndex = 2 };
            Assert.Equal(hit, RoundTrip(hit, Hit.Read));

            var damage = new Damage
            {
                TargetId = 2, Outcome = HitOutcome.Critical, Amount = 41, HpLeft = 159, KnockX = 0.3f, KnockY = -0.1f,
                SlowFrames = 90, FreezeFrames = 0, Dot = 0.04f,
            };
            Assert.Equal(damage, RoundTrip(damage, Damage.Read));

            var correct = new Correct { Seq = 9, X = 1.5f, Y = -2.5f };
            Assert.Equal(correct, RoundTrip(correct, Correct.Read));
        }

        [Fact]
        public void ConnectionAndMatchPacketsRoundTrip()
        {
            var hello = new Hello { Version = Packet.Version, Token = "", Nickname = "판도라즈" };
            Assert.Equal(hello, RoundTrip(hello, Hello.Read));

            var welcome = new Welcome { PlayerId = 42, Nickname = "판도라즈" };
            Assert.Equal(welcome, RoundTrip(welcome, Welcome.Read));

            var pong = new Pong { ClientTime = 1000, ServerTime = 2000 };
            Assert.Equal(pong, RoundTrip(pong, Pong.Read));

            var error = new Error { Code = ErrorCode.BadLoadout };
            Assert.Equal(error, RoundTrip(error, Error.Read));

            var end = new End { Won = true, Reason = EndReason.TimeUp, Wins = 10, Losses = 4 };
            Assert.Equal(end, RoundTrip(end, End.Read));

            JoinQueue join = RoundTrip(new JoinQueue { Pandoras = new[] { "phase", "darkPower", "sandStorm" } }, JoinQueue.Read);
            Assert.Equal(new[] { "phase", "darkPower", "sandStorm" }, join.Pandoras);

            var found = new MatchFound
            {
                MatchId = 99, RivalId = 5, RivalNickname = "모래늑대", RivalPandoras = new[] { "flame" }, RivalWins = 3, RivalLosses = 1,
            };
            MatchFound read = RoundTrip(found, MatchFound.Read);
            Assert.Equal((found.MatchId, found.RivalId, found.RivalNickname, found.RivalWins, found.RivalLosses),
                (read.MatchId, read.RivalId, read.RivalNickname, read.RivalWins, read.RivalLosses));
            Assert.Equal(found.RivalPandoras, read.RivalPandoras);
        }

        [Fact]
        public void MoveIsSmall()
        {
            // 초당 20번, 두 사람 몫을 보낸다. 머리말 4바이트 + 본문 19바이트.
            Assert.Equal(23, Packet.Encode(new Move(), new byte[Packet.MaxSize]));
        }

        [Fact]
        public void BytesArrivingOneAtATimeBecomeWholePackets()
        {
            byte[] stream = Concat(
                Encode(new Hello { Version = Packet.Version, Token = "t", Nickname = "가나다" }),
                Encode(new Move { Seq = 1, X = 2f }));

            var buffer = new List<byte>();
            var ids = new List<PacketId>();
            foreach (byte b in stream)
            {
                buffer.Add(b);
                while (Packet.TryReadFrame(buffer.ToArray(), out PacketId id, out ReadOnlySpan<byte> body, out int consumed))
                {
                    ids.Add(id);
                    buffer.RemoveRange(0, consumed);
                }
            }

            Assert.Equal(new[] { PacketId.Hello, PacketId.Move }, ids);
            Assert.Empty(buffer);
        }

        [Fact]
        public void TwoPacketsInOneReadComeOutInOrder()
        {
            byte[] stream = Concat(Encode(new Ping { ClientTime = 1 }), Encode(new Ping { ClientTime = 2 }));

            Assert.True(Packet.TryReadFrame(stream, out _, out ReadOnlySpan<byte> first, out int consumed));
            var reader = new PacketReader(first);
            Assert.Equal(1, Ping.Read(ref reader).ClientTime);

            Assert.True(Packet.TryReadFrame(stream.AsSpan(consumed), out _, out ReadOnlySpan<byte> second, out _));
            reader = new PacketReader(second);
            Assert.Equal(2, Ping.Read(ref reader).ClientTime);
        }

        [Fact]
        public void HeaderClaimingTooLongABodyIsRejected()
        {
            byte[] header = { 0xFF, 0xFF, (byte)PacketId.Move, 0 };
            Assert.Throws<PacketFormatException>(() => Packet.TryReadFrame(header, out _, out _, out _));
        }

        [Fact]
        public void BodyShorterThanThePacketIsRejected()
        {
            // Ping(8바이트)을 Pong(16바이트)으로 읽으면 본문이 모자란다.
            byte[] ping = Encode(new Ping { ClientTime = 1 });
            Assert.Throws<PacketFormatException>(() =>
            {
                Packet.TryReadFrame(ping, out _, out ReadOnlySpan<byte> body, out _);
                var reader = new PacketReader(body);
                Pong.Read(ref reader);
            });
        }

        [Fact]
        public void BodyLongerThanTheLimitCannotBeSent()
        {
            var tooLong = new JoinQueue { Pandoras = new[] { new string('가', Packet.MaxBodySize) } };
            Assert.Throws<InvalidOperationException>(() => Packet.Encode(tooLong, new byte[Packet.MaxSize]));
        }

        private static T RoundTrip<T>(T packet, ReadPacket<T> read) where T : struct, IPacket
        {
            byte[] bytes = Encode(packet);
            Assert.True(Packet.TryReadFrame(bytes, out PacketId id, out ReadOnlySpan<byte> body, out int consumed));
            Assert.Equal(packet.Id, id);
            Assert.Equal(bytes.Length, consumed);

            var reader = new PacketReader(body);
            T result = read(ref reader);
            Assert.Equal(0, reader.Remaining);
            return result;
        }

        private static byte[] Encode<T>(T packet) where T : struct, IPacket
        {
            var buffer = new byte[Packet.MaxSize];
            int length = Packet.Encode(packet, buffer);
            return buffer.AsSpan(0, length).ToArray();
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var all = new byte[first.Length + second.Length];
            first.CopyTo(all, 0);
            second.CopyTo(all, first.Length);
            return all;
        }
    }
}
