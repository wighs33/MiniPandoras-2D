"""힐리아 서쪽 부두(port)를 새로 짠다(집이 옛 그림이고, 외곽 남쪽 길은 서쪽 부두로 이어진다).

배편이 끊긴 쓸쓸한 부두다. 외곽에서 내려온 흙길이 낡은 집 세 채 사이를 지나 모래밭에 닿고, 긴 나무 잔교가 남쪽 바다로 뻗는다.
잔교는 판자가 썩고 난간이 군데군데 없으며, 끝의 돌 받침에 레비아탄의 바다로 들어가는 공간의 문(sea_gate)이 서 있다.
문으로 들어가는 포탈은 입장을 묻는 창이 있어 이야기가 건다(지점 pierGate). 돌아오면 gateArrive에 선다.

지점 arrive, north, northArrive, erus, buddy, largo는 옛 맵과 같은 칸이다(6장 도착 장면이 쓴다).

사용: python Tools/PixelGen/west_port.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/port/…    맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/port_preview.png       미리보기
"""
import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

import forest
import harbor_props as hp
import houses
import pixelgen as pg
import ship
import side_roads
from layout import LOGICAL, MapLayout

T = pg.TILE
WIDTH, HEIGHT = 32, 38
PIER_X = (14, 17)               # 잔교가 놓이는 칸(가운데 15.5)
PIER_FIRST, PIER_END = 17, 31   # 잔교 칸 줄(끝 줄은 돌 받침 앞이다)
PLATFORM = (13, 31, 18, 33)     # 문 앞 받침의 걸을 수 있는 칸
GATE_GROUND = 33.6
ROAD = [(14, -1), (14, 6), (13.4, 10), (14.6, 14)]
ROAD_WIDTH = 2.2
SEA_LIGHT = "#6ae0e8"
DESIGNS = {design.name: design for design in houses.CATALOG}
YARDS = ((3, 4, 9, 10), (20, 8, 29, 14), (22, 3, 29, 8))       # 집 자리(수관이 집을 덮지 않게 큰 나무를 심지 않는다)
# 망가진 난간 칸(남쪽에서 몇 번째 기둥인지). 빠진 칸이 있어 오래 버려진 잔교로 보인다.
BROKEN_RAILS = {-1: (3, 4, 9), 1: (6, 7, 8, 11)}


def worn_house(base, name, **changes):
    return replace(DESIGNS[base], name=name, worn=True, lit=False, lanterns=False, flowers=False, **changes)


def build():
    m = MapLayout("port", "힐리아 서쪽 부두", WIDTH, HEIGHT, music="hope", seed=261)
    rng = np.random.default_rng(262)

    # ── 바닥: 들판 → 모래밭 → 바다 ──
    shore = 17.4 + 0.7 * np.sin(m.cx * 0.42) + 0.3 * np.sin(m.cx * 1.1 + 1)
    beach = 13.2 + 0.6 * np.sin(m.cx * 0.3 + 2)
    m.kinds[m.cy > beach] = "sand"
    m.kinds[m.cy > shore] = "water"
    m.line("path", ROAD, ROAD_WIDTH)
    m.stamp("tallgrass", (10, 3), (11, 3), (10, 4), (19, 2), (20, 2), (20, 3), (27, 9), (28, 9), (2, 11), (3, 11), (7, 11), (8, 12))
    m.stamp("flowers", (17, 8), (18, 9), (10, 11), (26, 11))
    m.stamp("bush", (19, 11), (10, 8), (16, 3))

    # ── 숲: 맵 가장자리만. 길과 집 둘레에는 심지 않는다 ──
    keep = m.near_line(ROAD, 2.6) | (m.cy > beach - 0.5)
    edge = ((m.cy < 3.2) | (m.cx < 2.6) | (m.cx > WIDTH - 2.6)) & ~keep & (m.kinds != "water")
    yards = np.zeros(m.kinds.shape, dtype=bool)
    for x0, y0, x1, y1 in YARDS:
        yards[y0:y1, x0:x1] = True
    forest.plant(m, edge, np.random.default_rng(263), low=edge & forest.canopy_over((m.kinds == "path") | yards))
    m.block_where(edge)
    m.border()

    # ── 낡은 집 셋 ──
    m.house(worn_house("stone_house", "port_house_stone"), 5.5, 9)
    m.house(worn_house("longhouse", "port_house_long", roof_material="moss", ivy=True), 23, 13)
    m.house(worn_house("cottage_red", "port_house_small", chimney_at=-1), 25, 7)

    # ── 잔교: 썩은 판자, 군데군데 빠진 난간, 끝의 돌 받침과 공간의 문 ──
    columns = PIER_X[1] - PIER_X[0]
    rows = PIER_END - PIER_FIRST + 0.5
    m.decal("pier_platform", hp.pier(PLATFORM[2] - PLATFORM[0], 3, rng, worn=True), PLATFORM[0] * T - hp.PIER_MARGIN, (PLATFORM[1] - 0.2) * T)
    m.decal("pier", hp.pier(columns, rows, rng, worn=True), PIER_X[0] * T - hp.PIER_MARGIN, (PIER_FIRST - 0.5) * T)
    m.keep_open(PIER_X[0], PIER_FIRST, PIER_X[1], PIER_END + 1)
    m.keep_open(*PLATFORM)
    for side, x in ((-1, PIER_X[0] + 0.05), (1, PIER_X[1] - 0.05)):
        posts = np.arange(PIER_FIRST + 0.75, PIER_END - 0.4, 1.25)
        for k, y in enumerate(posts):
            broken = k in BROKEN_RAILS[side]
            if k % 5 == 2 and broken:
                continue
            piece = hp.pier_rail(40, rng, worn=True, broken=broken)
            m.stand(f"pier_rail_{'b' if broken else 'a'}{k % 2}", piece, x, y)
    m.stand("sea_gate", hp.sea_gate(264), np.mean(PIER_X), GATE_GROUND)
    m.block(PLATFORM[0], PLATFORM[3], PLATFORM[2], PLATFORM[3] + 1)
    m.light(np.mean(PIER_X) * LOGICAL, 31.5 * LOGICAL, 64, SEA_LIGHT, flicker=True)
    m.stand("port_bollard_a", hp.bollard(265), 13.4, 17.2, block=(0.6, 0.3))
    m.stand("port_bollard_b", hp.bollard(266, rope=False), 17.6, 17.2, block=(0.6, 0.3))

    # ── 모래밭과 물가의 버려진 것들 ──
    m.stand("port_notice", hp.notice_board(267), 18.5, 15.6, block=(1.2, 0.4))
    m.stand("port_nets", ship.net_pile(np.random.default_rng(268)), 6, 15.6, block=(1.2, 0.4))
    m.stand("port_wreck", side_roads.wreck(269), 26.5, 16.6, block=(4.6, 0.8))
    m.stand("port_skiff", hp.sunken_boat(270), 9.5, 20.4)
    m.stand("port_skiff_b", hp.sunken_boat(271), 20.5, 23.5)
    m.stand("port_crates", hp.crate_stack(272, 2, 2), 11.2, 15.8, block=(1.4, 0.6))
    m.thing("barrel", 20, 14)
    m.lamp(11, 13)
    for k, (x, y) in enumerate(((9, 14), (3, 15), (29, 14))):
        m.stand(f"beach_rock_{k}", hp.shore_rock(273 + k), x + 0.5, y + 0.9, block=(1.0, 0.5))
    for k, (x, y) in enumerate(((5, 22), (27, 21), (23, 30), (7, 31))):
        m.stand(f"sea_rock_{k}", hp.shore_rock(280 + k, wet=True), x + 0.5, y + 0.9)

    # ── 이야기 지점과 포탈 ──
    m.point("north", (13, 0), (14, 0))
    m.point("northArrive", (13, 1))
    m.point("npc", (8, 8), (21, 13))
    m.point("erus", (15, 14))
    m.point("buddy", (13, 15))
    m.point("largo", (17, 15))
    m.point("arrive", (15, 16))
    m.point("notice", (18, 15), blocked=True)
    m.point("pierStart", *[(x, PIER_FIRST) for x in range(*PIER_X)])
    m.point("gateArrive", (15, 30))
    m.point("pierGate", *[(x, PLATFORM[3] - 1) for x in range(*PIER_X)])
    m.warp("north", "outskirts", "southArrive", "up")
    return m


def main(project):
    m = build()
    start = m.points["northArrive"][0]
    for name in ("arrive", "erus", "buddy", "largo", "npc", "pierStart", "gateArrive", "pierGate"):
        for cell in m.points[name]:
            if not m.reachable(start, [cell]):
                print(f"northArrive에서 {name} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
