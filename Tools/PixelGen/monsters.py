"""몬스터 도감 두 벌: 1레벨부터 20레벨까지 한 종씩(사용자 요청 2026-11-03 「세계관에 맞는 판타지 몬스터, 개·늑대·멧돼지 같은 건 진부」,
이어서 「귀여운 것과 안 귀여운 것을 섞어, 포켓몬·팰월드처럼 처음부터 특이하게 생긴 생물로, 지형 컨셉에 맞게」).

에라디온의 몬스터는 동물이 아니다. 죽은 신들의 힘과 원념이 흩어진 소울더스트가 사람이 아닌 것(털뭉치·볏단·돌·버섯·천·덤불·토기·
조형물·갑주·거품·뿌리·수정·책·깃털·얼음·연·시계·공간·석상·왕관)에 깃들어 생긴다(Docs/STORY.md 4장 「소울더스트와 판도라의 탄생」).
레벨이 오를수록 더 큰 힘에서 나온다:
   1~5   주인 없는 더스트와 사람의 원념(monsters_dust.py)
   6~10  땅·바다에 스민 십이장의 더스트와 옛 싸움의 흔적(monsters_land.py)
  11~15  저주·요정의 빛·기록, 십이장 불과 빙결(monsters_relic.py)
  16~20  십이장 번개·시간, 태초의 신 차원·거인·재앙의 잔재(monsters_divine.py)
생물 도감(CREATURES)은 우리 맵 지형마다 사는 생물이다(creatures_<지형>.py). 귀여운 것과 사나운 것이 섞여 있다. 메이플스토리 몬스터처럼
한 마리마다 분명한 컨셉을 하나씩 갖고, 얼굴은 주황버섯·슬라임처럼 작은 눈과 가는 ω 입(또는 입 없음)이다. 초롱초롱한 빛점 눈은 쓰지 않는다
(사용자 지적: 프로젝트와 안 맞는다). 사나운 것은 그 색으로 빛나는 작은 눈과 이빨을 둔다(creature_kit.small_face).
몬스터마다 웹게임 인물과 같은 시트(아래·옆·위 세 장씩, sheet.json)를 그린다.

  Assets/MiniPandoras/Art/PixelGen/Monsters/<id>/down_0.png … up_2.png, sheet.json
  Docs/ArtUpgrade/PixelGen/monsters_preview.png, creatures_preview.png            도감 한 장(레벨·이름, 세 방향)
  Docs/ArtUpgrade/PixelGen/monsters_walk_preview.png, creatures_walk_preview.png  걸음 세 장씩

사용: python Tools/PixelGen/monsters.py <프로젝트 루트> [id ...]   (id를 주면 그 몬스터만 다시 그린다)
"""
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import creatures_desert as desert
import creatures_forest as forest
import creatures_garden as garden
import creatures_meadow as meadow
import creatures_stone as stone
import creatures_void as void
import creatures_water as water
import monsters_dust as dust
import monsters_land as land
import monsters_divine as divine
import monsters_relic as relic
from monster_kit import VIEWS, save_sheet


@dataclass(frozen=True)
class Monster:
    level: int
    id: str
    name: str
    size: tuple          # 논리 px (가로, 세로)
    outline: tuple       # 테두리 색
    draw: Callable
    origin: str          # 깃든 더스트
    home: str            # 사는 곳
    outline_width: int = 1   # 테두리 두께(그 그림의 px). 곱게 칠한 생물 도감은 2


ROSTER = [
    Monster(1, "dustPuff", "솜먼지", (18, 20), (40, 36, 32), dust.dust_puff, "주인 없는 소울더스트", "힐리아 초원"),
    Monster(2, "sheafImp", "이삭도깨비", (22, 30), (44, 28, 10), dust.sheaf_imp, "밭에 떨어진 더스트", "힐리아 마을 들판"),
    Monster(3, "pebbleBundle", "깍지돌", (24, 22), (22, 24, 30), dust.pebble_bundle, "강물에 실려 온 더스트", "뱀성 앞 강가"),
    Monster(4, "lanternCap", "등롱버섯", (22, 30), (44, 14, 10), dust.lantern_cap, "숲 그늘에 고인 더스트", "마을 끝 숲길"),
    Monster(5, "ragWraith", "넝마귀", (24, 34), (22, 18, 20), dust.rag_wraith, "버려진 천에 남은 원념", "비 오는 마을 골목"),
    Monster(6, "brambleWarden", "덤불지기", (26, 36), (16, 22, 10), land.bramble_warden, "대자연(청룡)의 더스트 찌꺼기", "숲길"),
    Monster(7, "sandUrn", "모래단지", (26, 30), (40, 18, 8), land.sand_urn, "모래(라)의 더스트", "힐리아 서부 사막"),
    Monster(8, "stonecoil", "돌비늘뱀", (34, 26), (20, 22, 30), land.stonecoil, "돌뱀 조형물에 스민 더스트", "뱀성"),
    Monster(9, "hollowHelm", "빈 갑주", (26, 40), (16, 16, 22), land.hollow_helm, "에라드 패잔병의 원념", "옛 전장·뱀성"),
    Monster(10, "foamWisp", "물거품령", (26, 32), (14, 34, 46), land.foam_wisp, "바다(아쿠아)의 더스트", "바다 위·배"),
    Monster(11, "rootStalker", "뿌리걸음이", (30, 30), (12, 8, 6), relic.root_stalker, "저주받은 뿌리의 원념", "거대고목의 뿌리"),
    Monster(12, "glassMoth", "유리나방", (34, 28), (40, 46, 80), relic.glass_moth, "수정꽃에 맺힌 더스트", "판도라의 정원"),
    Monster(13, "bitingTome", "물어뜯는 서책", (30, 26), (20, 6, 8), relic.biting_tome, "옛 기록에 남은 원념", "고목 서고"),
    Monster(14, "cinderPlume", "불씨깃", (28, 34), (60, 10, 6), relic.cinder_plume, "불(주작)의 더스트", "에라드 화로·화산"),
    Monster(15, "frostShell", "서리껍질", (36, 28), (16, 30, 52), relic.frost_shell, "빙결(현무)의 더스트", "북쪽 얼음 벌판"),
    Monster(16, "thunderKite", "뇌명연", (30, 38), (40, 34, 24), divine.thunder_kite, "번개(래니)의 더스트", "폭풍이 이는 고원"),
    Monster(17, "clockWarden", "태엽파수꾼", (30, 44), (30, 18, 8), divine.clock_warden, "시간(톰)의 더스트", "멈춘 시계탑"),
    Monster(18, "riftEye", "틈새눈", (32, 32), (14, 8, 26), divine.rift_eye, "차원(디바인)이 남긴 틈", "차원공간의 가장자리"),
    Monster(19, "titanHand", "거신의 손", (42, 38), (22, 18, 16), divine.titan_hand, "거신 헤토스의 봉인", "무너진 거신상"),
    Monster(20, "calamityCrown", "재앙의 왕관", (38, 50), (10, 4, 12), divine.calamity_crown, "재앙신 칼립스의 원념", "신인전쟁의 폐허"),
]

# 생물 도감: 레벨, id, 이름, 크기, 테두리, 그리기, 귀여움/사나움, 지형. 테두리는 그 몸 색의 어두운 쪽이다.
CREATURES = [
    Monster(1, "fluffTail", "솜꼬리", (28, 28), (46, 32, 16), meadow.fluff_tail, "귀여움", "힐리아 초원", 2),
    Monster(2, "leaflet", "잎마디", (28, 22), (16, 32, 14), meadow.leaflet, "귀여움", "힐리아 초원", 2),
    Monster(3, "sunCub", "햇갈기", (28, 26), (48, 30, 12), meadow.sun_cub, "귀여움", "초원 언덕", 2),
    Monster(4, "mossCap", "이끼갓", (22, 24), (36, 22, 12), forest.moss_cap, "귀여움", "마을 끝 숲길", 2),
    Monster(5, "knotHorn", "옹이뿔", (28, 26), (10, 6, 4), forest.knot_horn, "사나움", "숲길", 2),
    Monster(6, "mossDrop", "물이끼방울", (20, 22), (12, 30, 26), water.moss_drop, "귀여움", "뱀성 앞 강가", 2),
    Monster(7, "pebbleShell", "조약갑", (30, 24), (22, 20, 18), water.pebble_shell, "사나움", "강가", 2),
    Monster(8, "stoneImp", "돌날개", (26, 26), (14, 14, 18), stone.stone_imp, "사나움", "뱀성 성가퀴", 2),
    Monster(9, "puddleHopper", "웅덩발", (24, 22), (16, 24, 30), water.puddle_hopper, "귀여움", "비 오는 마을 외곽", 2),
    Monster(10, "crystalMole", "수정등", (28, 26), (10, 8, 14), stone.crystal_mole, "사나움", "호빗 동굴", 2),
    Monster(11, "dripEar", "종유귀", (28, 30), (12, 12, 16), stone.drip_ear, "사나움", "동굴 천장", 2),
    Monster(12, "cloudHorn", "구름뿔", (28, 28), (44, 42, 38), stone.cloud_horn, "귀여움", "산길", 2),
    Monster(13, "sandSail", "모래돛", (28, 28), (40, 26, 12), desert.sand_sail, "귀여움", "서부 사막", 2),
    Monster(14, "thornBloom", "가시꽃도마", (30, 24), (26, 22, 10), desert.thorn_bloom, "사나움", "오아시스", 2),
    Monster(15, "coralOtter", "산호뿔", (26, 30), (22, 14, 10), water.coral_otter, "귀여움", "서부 바닷가", 2),
    Monster(16, "petalTail", "꽃잎꼬리", (30, 28), (40, 20, 22), garden.petal_tail, "귀여움", "페어리가든 꽃밭", 2),
    Monster(17, "pitcherFang", "주머니풀", (26, 34), (14, 26, 12), garden.pitcher_fang, "사나움", "페어리가든 냇물가", 2),
    Monster(18, "leafGlider", "잎망토", (32, 28), (22, 30, 16), forest.leaf_glider, "귀여움", "거대고목 가지", 2),
    Monster(19, "rootOcto", "뿌리문어", (30, 26), (12, 6, 10), forest.root_octo, "사나움", "고목의 뿌리", 2),
    Monster(20, "starWhale", "별고래", (42, 30), (6, 8, 20), void.star_whale, "사나움", "차원공간의 가장자리", 2),
]

BACKDROP = (58, 72, 62, 255)
FONT = "Assets/ThirdParty/Pretendard/Pretendard-Bold.otf"


def preview(project, roster, sheets):
    """도감 한 장: 한 줄에 다섯 종, 칸마다 레벨·이름과 아래·옆·위 선 모습."""
    cell_w, cell_h, per_row = 560, 300, 5
    rows = (len(roster) + per_row - 1) // per_row
    sheet = Image.new("RGBA", (cell_w * per_row, cell_h * rows), BACKDROP)
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype(str(project / FONT), 26)
    small = ImageFont.truetype(str(project / FONT).replace("Bold", "Regular"), 18)
    for i, monster in enumerate(roster):
        x0, y0 = (i % per_row) * cell_w, (i // per_row) * cell_h
        draw.rectangle((x0 + 4, y0 + 4, x0 + cell_w - 4, y0 + cell_h - 4), outline=(90, 110, 96, 255), width=2)
        draw.text((x0 + 16, y0 + 12), f"Lv{monster.level}  {monster.name}", font=font, fill=(250, 238, 200, 255))
        draw.text((x0 + 16, y0 + 46), f"{monster.origin} · {monster.home}", font=small, fill=(200, 214, 196, 255))
        ground = y0 + cell_h - 24
        for k, view in enumerate(VIEWS):
            image = Image.fromarray(sheets[monster.id][view][0], "RGBA")
            cx = x0 + 95 + k * 180
            sheet.alpha_composite(image, (int(cx - image.width / 2), ground - image.height))
    return sheet


def walk_preview(roster, sheets):
    """몬스터마다 아래·옆 걸음 세 장을 한 줄에."""
    cell_w, cell_h = 220, 240
    sheet = Image.new("RGBA", (cell_w * 6, cell_h * len(roster)), BACKDROP)
    for i, monster in enumerate(roster):
        for k, (view, frame) in enumerate((v, f) for v in ("down", "right") for f in range(3)):
            image = Image.fromarray(sheets[monster.id][view][frame], "RGBA")
            sheet.alpha_composite(image, (k * cell_w + (cell_w - image.width) // 2, i * cell_h + cell_h - 16 - image.height))
    return sheet


def main(project, only):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Monsters"
    sheets = {}
    for monster in ROSTER + CREATURES:
        if only and monster.id not in only:
            continue
        sheets[monster.id] = save_sheet(out / monster.id, monster.id, monster.size, monster.draw, np.array(monster.outline, dtype=np.uint8),
                                        monster.outline_width)
    if only:
        return
    docs = project / "Docs/ArtUpgrade/PixelGen"
    for roster, name in ((ROSTER, "monsters"), (CREATURES, "creatures")):
        preview(project, roster, sheets).convert("RGB").save(docs / f"{name}_preview.png")
        walk_preview(roster, sheets).convert("RGB").save(docs / f"{name}_walk_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."), set(sys.argv[2:]))
