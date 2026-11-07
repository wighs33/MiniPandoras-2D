namespace MiniPandoras.Editor.Monsters
{
    /// <summary>몬스터의 몸놀림. 레벨 수치에 곱해, 같은 레벨이라도 생김새대로 싸우게 한다.</summary>
    internal enum MonsterBuild
    {
        /// <summary>귀여운 생물. 덜 아프게 때리고 덜 버틴다.</summary>
        Gentle,
        /// <summary>사나운 것. 더 세게 때린다.</summary>
        Fierce,
        /// <summary>돌·껍질·갑주를 두른 것. 단단하고 느리다.</summary>
        Sturdy,
        /// <summary>날거나 뛰는 것. 빠르고 가끔 피한다.</summary>
        Nimble,
        /// <summary>던전 보스. 체력이 훨씬 많다.</summary>
        Abyssal,
        /// <summary>거대한 것. 오래 버티고 세다.</summary>
        Colossal,
    }

    /// <summary>
    /// 몬스터가 쓰는 기술 하나. fx는 웹게임 FxKind 이름이고, 필드에서 근접(bite·claw·slash·bash·thrust)·돌진(charge)·
    /// 순간이동(blink)·과녁 자리 폭발(light·dark·sand·ice·fire)·넓은 폭발(…Wave, darkWide) 가운데 무엇으로 나갈지를 정한다.
    /// </summary>
    internal readonly struct MonsterMove
    {
        public MonsterMove(string name, string fx, float power)
        {
            Name = name;
            Fx = fx;
            Power = power;
        }

        public string Name { get; }
        public string Fx { get; }
        public float Power { get; }
    }

    internal readonly struct MonsterEntry
    {
        public MonsterEntry(string id, string name, int level, MonsterBuild build, params MonsterMove[] moves)
        {
            Id = id;
            Name = name;
            Level = level;
            Build = build;
            Moves = moves;
        }

        /// <summary>시트 폴더 이름(Art/PixelGen/Monsters/id)이자 적 id.</summary>
        public string Id { get; }
        public string Name { get; }
        public int Level { get; }
        public MonsterBuild Build { get; }
        public MonsterMove[] Moves { get; }
    }

    /// <summary>
    /// 몬스터 도감 두 벌(Docs/MONSTERS.md)의 전투 정보. 그림은 Tools/PixelGen/monsters.py가 그리고,
    /// 기술은 도감의 「버릇」을 웹게임 적의 공격 모양으로 옮겼다. 필드 어디에 사는지는 Story/Wildlife가 정한다.
    /// </summary>
    internal static class MonsterRoster
    {
        public static readonly MonsterEntry[] Entries =
        {
            // 소울더스트가 사람이 아닌 것에 깃든 몬스터
            new MonsterEntry("dustPuff", "솜먼지", 1, MonsterBuild.Fierce, Move("몸통 박치기", "bash", 1.3f)),
            new MonsterEntry("sheafImp", "이삭도깨비", 2, MonsterBuild.Fierce, Move("짚 팔 휘두르기", "slash", 1.4f), Move("낟알 흩뿌리기", "sand", 1.3f)),
            new MonsterEntry("pebbleBundle", "깍지돌", 3, MonsterBuild.Sturdy, Move("데굴 들이받기", "charge", 1.5f)),
            new MonsterEntry("lanternCap", "등롱버섯", 4, MonsterBuild.Fierce, Move("등롱 포자", "light", 1.3f)),
            new MonsterEntry("ragWraith", "넝마귀", 5, MonsterBuild.Nimble, Move("천 휘감기", "claw", 1.5f)),
            new MonsterEntry("brambleWarden", "덤불지기", 6, MonsterBuild.Fierce, Move("가시 갈퀴", "claw", 1.6f)),
            new MonsterEntry("sandUrn", "모래단지", 7, MonsterBuild.Sturdy, Move("모래 뿜기", "sand", 1.4f)),
            new MonsterEntry("stonecoil", "돌비늘뱀", 8, MonsterBuild.Nimble, Move("튀어 물기", "charge", 1.7f)),
            new MonsterEntry("hollowHelm", "빈 갑주", 9, MonsterBuild.Sturdy, Move("녹슨 창 찌르기", "thrust", 1.5f)),
            new MonsterEntry("foamWisp", "물거품령", 10, MonsterBuild.Nimble, Move("물줄기 채찍", "slash", 1.4f), Move("거품 터뜨리기", "waterWave", 1.3f)),
            new MonsterEntry("rootStalker", "뿌리걸음이", 11, MonsterBuild.Fierce, Move("뿌리 다리 덮치기", "charge", 1.7f), Move("저주 가시", "dark", 1.4f)),
            new MonsterEntry("glassMoth", "유리나방", 12, MonsterBuild.Nimble, Move("빛가루", "light", 1.4f)),
            new MonsterEntry("bitingTome", "물어뜯는 서책", 13, MonsterBuild.Fierce, Move("책장 물어뜯기", "bite", 1.6f), Move("낱장 날리기", "windWave", 1.3f)),
            new MonsterEntry("cinderPlume", "불씨깃", 14, MonsterBuild.Nimble, Move("불씨 깃털", "fire", 1.6f)),
            new MonsterEntry("frostShell", "서리껍질", 15, MonsterBuild.Sturdy, Move("서리 입김", "ice", 1.6f)),
            new MonsterEntry("thunderKite", "뇌명연", 16, MonsterBuild.Nimble, Move("번개 떨구기", "light", 1.8f)),
            new MonsterEntry("clockWarden", "태엽파수꾼", 17, MonsterBuild.Sturdy, Move("반달 날", "slash", 1.8f), Move("시간 건너뛰기", "blink", 1.6f)),
            new MonsterEntry("riftEye", "틈새눈", 18, MonsterBuild.Nimble, Move("공간 조각", "dark", 1.7f), Move("틈새 넘기", "blink", 1.7f)),
            new MonsterEntry("titanHand", "거신의 손", 19, MonsterBuild.Colossal, Move("내리찍기", "bash", 2f), Move("땅 울림", "sandWave", 1.6f)),
            new MonsterEntry("calamityCrown", "재앙의 왕관", 20, MonsterBuild.Colossal, Move("금빛 발톱", "claw", 1.9f), Move("검은 더스트 폭풍", "darkWide", 1.9f)),

            // 던전 보스(Tools/PixelGen/monsters_boss.py). 도감 밖이다.
            new MonsterEntry("leviathan", "레비아탄", 30, MonsterBuild.Abyssal, Move("집어삼키기", "bite", 2.2f), Move("해일", "waterWave", 1.8f), Move("몸통 돌진", "charge", 2f)),

            // 훈련장의 허수아비(Tools/PixelGen/training_ground.py). 도감 밖이고 덤비지 않는다.
            new MonsterEntry("trainingDummy", "허수아비", 1, MonsterBuild.Gentle),

            // 지형마다 사는 생물
            new MonsterEntry("fluffTail", "솜꼬리", 1, MonsterBuild.Gentle, Move("깡충 박치기", "charge", 1.3f)),
            new MonsterEntry("leaflet", "잎마디", 2, MonsterBuild.Gentle, Move("덩굴손 채찍", "slash", 1.3f)),
            new MonsterEntry("sunCub", "햇갈기", 3, MonsterBuild.Gentle, Move("햇살 갈기", "light", 1.3f)),
            new MonsterEntry("mossCap", "이끼갓", 4, MonsterBuild.Gentle, Move("포자 뿌리기", "dark", 1.3f)),
            new MonsterEntry("knotHorn", "옹이뿔", 5, MonsterBuild.Fierce, Move("옹이뿔 들이받기", "charge", 1.7f), Move("큰 턱 물기", "bite", 1.5f)),
            new MonsterEntry("mossDrop", "물이끼방울", 6, MonsterBuild.Gentle, Move("말랑 누르기", "bash", 1.4f)),
            new MonsterEntry("pebbleShell", "조약갑", 7, MonsterBuild.Sturdy, Move("굴러 들이받기", "charge", 1.6f)),
            new MonsterEntry("stoneImp", "돌날개", 8, MonsterBuild.Fierce, Move("돌 발톱", "claw", 1.6f), Move("돌 날개 바람", "windWave", 1.3f)),
            new MonsterEntry("puddleHopper", "웅덩발", 9, MonsterBuild.Gentle, Move("뒷다리 뛰어차기", "charge", 1.4f), Move("웅덩이 첨벙", "waterWave", 1.2f)),
            new MonsterEntry("crystalMole", "수정등", 10, MonsterBuild.Sturdy, Move("넓적 발톱", "claw", 1.6f), Move("수정 섬광", "light", 1.5f)),
            new MonsterEntry("dripEar", "종유귀", 11, MonsterBuild.Nimble, Move("송곳니", "bite", 1.5f), Move("종유 물방울", "ice", 1.4f)),
            new MonsterEntry("cloudHorn", "구름뿔", 12, MonsterBuild.Gentle, Move("구름뿔 치기", "charge", 1.5f)),
            new MonsterEntry("sandSail", "모래돛", 13, MonsterBuild.Gentle, Move("모래 끼얹기", "sand", 1.4f)),
            new MonsterEntry("thornBloom", "가시꽃도마", 14, MonsterBuild.Sturdy, Move("가시 꼬리", "slash", 1.7f), Move("꽃가시 흩뿌리기", "steelWave", 1.3f)),
            new MonsterEntry("coralOtter", "산호뿔", 15, MonsterBuild.Gentle, Move("진주 튕기기", "light", 1.4f)),
            new MonsterEntry("petalTail", "꽃잎꼬리", 16, MonsterBuild.Gentle, Move("꽃가루 바람", "windWave", 1.3f)),
            new MonsterEntry("pitcherFang", "주머니풀", 17, MonsterBuild.Fierce, Move("통째로 삼키기", "bite", 1.9f), Move("꿀물 뿌리기", "waterWave", 1.3f)),
            new MonsterEntry("leafGlider", "잎망토", 18, MonsterBuild.Gentle, Move("활강 차기", "charge", 1.5f)),
            new MonsterEntry("rootOcto", "뿌리문어", 19, MonsterBuild.Fierce, Move("뿌리 촉수", "claw", 1.8f), Move("보랏빛 포자", "darkWide", 1.4f)),
            new MonsterEntry("starWhale", "별고래", 20, MonsterBuild.Colossal, Move("별빛 숨결", "lightWave", 1.6f), Move("별의 돌진", "charge", 1.8f)),
        };

        private static MonsterMove Move(string name, string fx, float power)
        {
            return new MonsterMove(name, fx, power);
        }
    }
}
