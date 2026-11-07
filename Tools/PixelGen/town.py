"""힐리아 마을(town)을 새로 짠다(일자 마을이 아니라 커도 된다, 집은 따로 디자인한 소품으로).

마을은 64×46칸이다. 서쪽 어귀에서 흙길이 들어와 돌길이 되고, 가운데 광장(분수, 북쪽에 모험가 길드)을 지나
개울 다리를 건너 동쪽 출구로 나간다. 다리 건너에서 흙길이 북쪽 숲(마을 끝 숲길)으로 갈라진다.
광장 서쪽은 앞뒤 두 줄 주택가, 길 남쪽은 농가와 텃밭이다.

지점 이름은 웹게임 마을과 같다(arrive, east, forest, forestArrive, npc). guildDoor·guildArrive는 모험가 길드 문이다. 3장 인물 자리는 지점으로 옮겼다
(townWoman, elder, kid, guard 칸들). 순찰병의 자리와 길은 GUARD_ROUTES와 함께 고치고, town_guards.py로 지나갈 틈을 확인한다.

사용: python Tools/PixelGen/town.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/town/…       맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/town_preview.png         Unity에서 보이는 모습 미리보기(축소)
"""
import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

import forest
import houses
from layout import MapLayout

DESIGNS = {design.name: design for design in houses.CATALOG}

# 3장 순찰병: (자리, 길). 길은 Chapter3.cs의 GuardRoutes와 같아야 한다.
GUARDS = (
    ((10, 21), "D7 U7"),
    ((26, 21), "R8 D6 L8 U6"),
    ((16, 34), "R20 L20"),
    ((44, 21), "D13 U13"),
    ((53, 23), "R3 L3"),
    ((56, 11), "R1 D1 L1 U1"),
)


def design(base, **changes):
    """견본 집(houses.CATALOG)을 바탕으로 이름·색 등을 바꾼 설계."""
    return replace(DESIGNS[base], **changes)


def build():
    # 맵은 맑은 낮이다. 비와 밤은 쫓기는 장면에서 이야기가 켠다(Chapter3.RainyNightOverTheTown).
    town = MapLayout("town", "힐리아 마을", 64, 46, theme="town", music="town", seed=41)

    # ── 바닥: 길, 광장, 개울, 다리 ──
    town.line("path", [(-1, 25), (9, 25)], 3)
    town.line("cobble", [(8, 25), (14, 24.6), (24, 24.6)], 3)
    town.disc("cobble", 30, 25, 5.6)
    main_east = [(35, 24.5), (44, 23.5), (52, 23), (64, 22)]
    town.line("cobble", main_east, 3)
    town.line("path", [(55, 23), (55, 15), (56, 8), (56, -1)], 2.4)
    town.line("path", [(15, 24), (15, 11)], 2)
    town.line("path", [(2, 11.6), (24, 11.6)], 2)
    town.line("path", [(2, 34.6), (41, 34.6)], 2)
    town.line("path", [(30, 30), (30, 35)], 2)
    town.line("path", [(57, 23), (57, 34)], 2)
    stream = [(49, -1), (50, 7), (48, 14), (49, 20), (49, 27), (47, 34), (48, 40), (47, 47)]
    town.line("water", stream, 3.2)
    town.line("bridge", main_east, 3, only=("water",))
    # 모험가 길드 뒤 연못과 남쪽 풀밭 연못
    for cx, cy, radius in ((39.5, 6.5, 2.8), (42, 7.4, 1.9), (30.5, 40, 2.8), (33, 40.6, 1.9)):
        town.disc("water", cx, cy, radius)
    # 텃밭: 흙 고랑과 작물 줄
    for row in range(38, 42):
        town.rect("path" if row % 2 == 0 else "tallgrass", 4, row, 13, row + 1)
    town.stamp("flowers", (24, 30), (25, 30), (24, 31), (35, 30), (36, 31), (36, 30), (9, 29), (10, 29), (40, 28), (41, 29),
               (60, 26), (61, 26), (53, 18), (52, 12), (44, 30), (45, 31), (37, 5), (37, 6), (43, 7), (42, 4), (28, 40),
               (34, 41), (33, 38), (29, 38))
    town.stamp("bush", (2, 20), (13, 15), (23, 16), (44, 17), (51, 31), (62, 31), (26, 38), (39, 37), (3, 30))
    town.stamp("rock", (45, 12), (51, 24), (46, 36), (61, 11), (18, 29), (38, 8), (34, 39))
    town.stamp("sign", (5, 23), (53, 21))

    # ── 집 ──
    # 북서 뒷줄(뒷골목을 마주 본다)
    town.house(design("cottage_red", lit=True), 6, 11)
    town.house(design("cottage_thatch", door_at=0.3), 12, 11)
    town.house(design("stone_house", lit=True), 19.5, 11)
    # 큰길 북쪽 앞줄
    town.house(design("townhouse", lit=True), 6, 21)
    town.house(design("narrow_house"), 11.5, 21)
    town.house(design("bakery", lit=True), 20, 21)
    town.house(design("adventurers_guild"), 30, 19)
    town.house(design("inn"), 42, 21)
    # 큰길 남쪽(남쪽 골목을 마주 본다)
    town.house(design("farmhouse"), 8, 33)
    town.house(design("longhouse", lit=True), 20, 33)
    town.house(design("stone_house", name="stone_cottage", roof_material="clay", shutters="green", dormers=0, chimney_at=0.8,
                      seed=31), 37, 33)
    # 개울 건너
    town.house(design("cottage_thatch", name="thatch_east", roof_material="moss", walls="timber", shutters="red", ivy=False,
                      seed=33), 60, 15)
    town.house(design("narrow_house", name="narrow_east", roof_material="slate", walls="stone", shutters="green", lit=True,
                      seed=35), 58, 33)

    # ── 광장, 우물, 물건 ──
    town.fountain(28, 23, 4)
    town.thing("well", 19, 14)
    for kind, x, y in (("barrel", 16, 21), ("crate", 16, 22), ("crate", 6, 27), ("barrel", 7, 27), ("crate", 6, 28),
                       ("barrel", 37, 22), ("crate", 46, 22), ("barrel", 25, 21), ("crate", 3, 33)):
        town.thing(kind, x, y)
    field = [(3, y) for y in range(37, 43)] + [(13, y) for y in range(37, 43)] + [(x, 36) for x in range(3, 14) if x not in (8, 9)] \
        + [(x, 42) for x in range(4, 13)]
    town.fence(field)
    for x, y in ((11, 27), (20, 26), (25, 29), (35, 29), (40, 26), (53, 25), (58, 20), (24, 20), (36, 20)):
        town.lamp(x, y)

    # ── 나무: 테두리 숲과 마을 안 몇 그루. 자리는 길·집과 맞물려 있어 그대로 두고 종류만 무리대로 섞는다(forest.mixed) ──
    border = [(x, 1 + x % 2) for x in range(1, 63, 3) if not 53 <= x <= 58]
    border += [(x, 45 - x % 2) for x in range(2, 63, 3) if not 2 <= x <= 15 and not 26 <= x <= 36]
    border += [(1, y) for y in range(4, 43, 3) if not 21 <= y <= 28]
    border += [(62, y) for y in range(4, 43, 3) if not 19 <= y <= 25]
    rng = np.random.default_rng(42)
    forest.mixed(town, border, rng)
    inside = [(44, 10), (52, 9), (60, 8), (14, 30), (26, 32), (42, 31), (52, 30), (53, 37), (60, 39), (20, 40), (41, 40),
              (61, 28), (45, 17), (23, 14), (2, 15), (9, 15), (51, 15), (27, 5), (32, 3), (34, 7), (45, 4), (24, 7)]
    forest.mixed(town, inside, rng, mixes=(forest.BIRCHES, forest.BROADLEAF, forest.BROADLEAF))
    town.border()

    # ── 이야기 지점과 포탈 ──
    town.point("arrive", (2, 25))
    town.point("east", (63, 21), (63, 22), (63, 23))
    town.point("eastArrive", (61, 22))
    town.point("west", (0, 24), (0, 25), (0, 26))
    town.point("forest", (54, 0), (55, 0))
    town.point("forestArrive", (55, 3))
    town.point("npc", (27, 28))
    town.point("townWoman", (19, 23))
    town.point("elder", (33, 28))
    town.point("kid", (12, 23))
    town.point("guard", *(cell for cell, _ in GUARDS))
    town.point("guildDoor", (29, 18), (30, 18))
    town.point("guildArrive", (30, 19))
    town.warp("forest", "hideoutPath", "westArrive", "up")
    town.warp("east", "outskirts", "enter", "right")
    town.warp("west", "cliffBridge", "eastArrive", "left")
    town.warp("guildDoor", "adventurersGuild", "enter", "up")
    return town


def main(project):
    town = build()
    for start, route in GUARDS:
        if not town.route_is_clear(start, route):
            print(f"순찰 길 {route}({start})이 막힌 칸을 지난다")
    print(town.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
