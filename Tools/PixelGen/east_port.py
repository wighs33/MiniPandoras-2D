"""힐리아 동쪽 선착장(eastPort)을 새로 짠다(진짜 활성화된 항구로, 배도 사람도 많게).

서쪽 부두(west_port.py)와 달리 살아 있는 항구다. 산길에서 내려온 길이 주점과 창고 사이를 지나 돌을 깐 부둣가에 닿는다.
서쪽에 어시장 천막이 늘어서고, 동쪽에 기중기와 짐더미가 있다. 잔교 넷이 남쪽 바다로 뻗어 크고 작은 배가 매여 있고,
가운데 잔교 끝의 T자 부두에 힐리아 서부행 큰 배가 닿는다. 동쪽 돌 방파제 끝에 등대가 있고, 북동쪽 언덕에서 항구가 내려다보인다.

사람은 맵에 그리지 않는다. 이야기가 지점 crowd(서 있는 사람), sailor(말 걸 뱃사람)에 세운다.

사용: python Tools/PixelGen/east_port.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/eastPort/…    맵 데이터와 그림(layout.py)
  Assets/MiniPandoras/Art/PixelGen/Maps/caveRoad/map.json   남쪽 출구를 여기로 돌리고 돌아오는 길을 더한 산길
  Docs/ArtUpgrade/PixelGen/eastPort_preview.png       미리보기
"""
import sys
from dataclasses import replace
from pathlib import Path

import numpy as np

import cave
import forest
import harbor_props as hp
import houses
import objects
import pixelgen as pg
import ship
import station
import web_map
import yard
from layout import LOGICAL, MapLayout

T = pg.TILE
WIDTH, HEIGHT = 56, 41
QUAY = 22                       # 이 줄부터 바다다
STREET_TOP = 16                 # 부둣가 돌바닥이 시작하는 줄
ROAD_X = 28
PLATEAU = (42, 11)              # 북동쪽 언덕(칸 x 이상, 칸 y 미만)
STAIR_X = (52, 54)              # 언덕을 오르는 돌계단(칸)
FISH_PIER = (6, 9, 32)          # 잔교 (왼쪽 칸, 오른쪽 칸, 끝 줄): 고기잡이
TRADE_PIER = (18, 21, 34)       # 상선
MAIN_PIER = (31, 36, 33)        # 큰 배가 닿는 가운데 잔교
CARGO_PIER = (46, 49, 30)       # 짐 내리는 잔교
T_HEAD = (24, 33, 44, 36)       # 가운데 잔교 끝의 T자 부두
MOLE = (51, 54, 33)             # 돌 방파제
SEA_LIGHT = "#ffd27a"
DESIGNS = {design.name: design for design in houses.CATALOG}

WAREHOUSE_A = houses.Design("port_warehouse_a", 8, roof="side", roof_material="slate", walls="brick", door="double", door_at=0.3,
                            shutters="", flowers=False, chimney_at=-1, plot="paved", seed=71)
WAREHOUSE_B = houses.Design("port_warehouse_b", 7, roof="front", roof_material="clay", walls="planks", door="double", shutters="",
                            flowers=False, chimney_at=-1, plot="paved", seed=72)
WAREHOUSE_C = houses.Design("port_warehouse_c", 8, roof="side", roof_material="copper", walls="stone", door="double", door_at=0.7,
                            shutters="", flowers=False, chimney_at=0.15, plot="paved", seed=73)
TAVERN = replace(DESIGNS["inn"], name="port_tavern", plot="paved", door_at=0.45, seed=74)
OFFICE = houses.Design("port_office", 4, stories=2, roof="front", roof_material="slate", walls="stone", upper="timber", shutters="blue",
                       chimney_at=-1, flowers=False, plot="dirt", seed=75)
COTTAGE_A = replace(DESIGNS["cottage_red"], name="port_cottage_a", seed=76)
COTTAGE_B = replace(DESIGNS["cottage_thatch"], name="port_cottage_b", seed=77)
COTTAGE_C = replace(DESIGNS["farmhouse"], name="port_cottage_c", seed=78)

# (이름, 조각, 가운데 칸 x, 발밑 칸 y). 물에 뜬 것이라 막을 칸이 따로 없다.
SHIPS = (
    ("ship_trade_a", hp.moored_ship(801, 6.5, 2.0, 1, "ochre", cabin=False), 13.6, 29.6),
    ("ship_trade_b", hp.moored_ship(802, 7.5, 2.2, 1, "red", flip=True, cargo=1), 26.4, 30.8),
    ("ship_trade_c", hp.moored_ship(803, 9, 2.6, 2, "teal", cargo=2), 40.6, 30.2),
    ("ship_trade_d", hp.moored_ship(804, 12, 2.9, 2, "black", flip=True, cargo=2), 11.0, 39.4),
    ("ship_liner", hp.moored_ship(805, 16, 3.3, 3, "navy", cargo=3, pennant_color="blue"), 34.0, 40.9),
)
SKIFFS = ((3.0, 26.0, "teal", False), (2.4, 30.0, "red", True), (10.8, 25.0, "red", True), (16.4, 24.8, "navy", True), (22.8, 25.6, "ochre", True),
          (29.4, 24.8, "teal", True), (37.8, 24.8, "navy", False), (44.4, 24.8, "red", True), (50.0, 35.8, "teal", False))
LAMPS = ((14, 20), (23, 20), (38, 20), (47, 20), (30, 13), (25, 13))
STALLS = (("red", "fish", 4.2), ("blue", "crab", 8.2), ("green", "fruit", 12.2), ("teal", "rope", 16.2), ("red", "fish", 20.2))
# 사람이 설 빈 칸(부둣가, 시장 앞, 잔교, T자 부두, 언덕길). 지점 crowd가 된다.
CROWD = ((6, 21), (10, 20), (13, 21), (17, 21), (21, 20), (24, 18), (26, 21), (30, 18), (31, 20), (36, 20), (40, 19), (43, 18),
         (45, 21), (50, 20), (7, 26), (20, 29), (33, 29), (47, 26), (27, 34), (31, 34), (38, 34), (41, 34))
SAILORS = ((7, 30), (19, 32), (42, 35))


def build():
    m = MapLayout("eastPort", "힐리아 동쪽 선착장", WIDTH, HEIGHT, theme="town", music="town", seed=791)
    rng = np.random.default_rng(792)

    # ── 바닥: 윗마을 풀밭과 길, 돌을 깐 부둣가, 바다 ──
    m.rect("cobble", 0, STREET_TOP, WIDTH, QUAY)
    m.rect("water", 0, QUAY, WIDTH, HEIGHT)
    m.rect("cobble", MOLE[0], QUAY, MOLE[1], MOLE[2])
    m.line("path", [(ROAD_X, -1), (ROAD_X, 10), (27.4, 13), (ROAD_X, STREET_TOP + 1)], 2.4)
    m.line("path", [(ROAD_X, 7.5), (41, 7.5)], 1.6)
    m.line("path", [(ROAD_X, 7.5), (7, 7.5)], 1.6)
    m.line("cobble", [(STAIR_X[0] + 1, 13), (STAIR_X[0] + 1, STREET_TOP + 1)], 2)

    # 북동쪽 언덕: 절벽 위 풀밭. 돌계단으로 오른다.
    x0, y0 = PLATEAU
    m.rect("grass", x0, 0, WIDTH, y0)
    m.rect("cliff", x0 - 2, y0, WIDTH, y0 + 2)
    m.rect("stairs", *(STAIR_X[0], y0), *(STAIR_X[1], y0 + 2))
    m.stamp("flowers", (46, 7), (47, 7), (49, 8), (51, 7), (15, 13), (23, 14), (8, 14), (12, 5), (36, 9))
    m.stamp("tallgrass", (44, 8), (45, 8), (50, 9), (10, 9), (11, 9), (20, 8), (34, 8), (35, 8), (8, 9), (9, 9), (31, 4), (32, 4))
    m.stamp("bush", (44, 6), (53, 7), (2, 12), (19, 9), (26, 12))
    m.stamp("rock", (41, 14), (6, 13), (18, 6))

    # ── 윗마을 숲: 맨 위와 왼쪽 끝만. 길과 집을 덮지 않는다 ──
    open_ground = (m.kinds == "path") | (m.kinds == "cobble")
    woods = (((m.cy < 3.6) | (m.cx < 2)) & (m.cy < STREET_TOP - 1)) & (m.kinds == "grass") & ~m.near_line([(ROAD_X, -1), (ROAD_X, 10)], 2.4)
    forest.plant(m, woods, np.random.default_rng(793), low=woods & forest.canopy_over(open_ground))
    m.block_where(woods)
    m.border()

    forest.mixed(m, [(42, 5), (44, 9), (45, 5), (54, 5)], np.random.default_rng(794))

    # ── 집: 부둣가를 마주 보는 창고와 주점, 그 뒤 윗마을 ──
    m.house(WAREHOUSE_A, 7, STREET_TOP)
    m.house(WAREHOUSE_B, 17.5, STREET_TOP)
    m.house(TAVERN, 35, STREET_TOP)
    m.house(WAREHOUSE_C, 45.5, STREET_TOP)
    m.house(OFFICE, 23.5, STREET_TOP - 0.5)
    m.house(COTTAGE_A, 16, 7)
    m.house(COTTAGE_B, 9, 7)
    m.house(COTTAGE_C, 33.5, 7)
    m.house(replace(COTTAGE_A, name="port_cottage_d", roof_material="moss", seed=79), 39.5, 7)

    # ── 안벽과 잔교 ──
    m.decal("quay_wall", hp.quay_wall(WIDTH, rng), 0, QUAY * T - 4)
    m.decal("mole_wall_left", side_wall(MOLE[2] - QUAY, rng), MOLE[0] * T - 8, QUAY * T)
    m.decal("mole_wall_right", side_wall(MOLE[2] - QUAY, rng), MOLE[1] * T, QUAY * T)
    m.decal("mole_end", hp.quay_wall(MOLE[1] - MOLE[0] + 0.5, rng), MOLE[0] * T - 8, MOLE[2] * T - 4)
    for name, (left, right, end) in (("pier_fish", FISH_PIER), ("pier_trade", TRADE_PIER), ("pier_main", MAIN_PIER), ("pier_cargo", CARGO_PIER)):
        m.decal(name, hp.pier(right - left, end - QUAY + 0.5, rng), left * T - hp.PIER_MARGIN, (QUAY - 0.5) * T)
        m.keep_open(left, QUAY, right, end)
    x0, y0, x1, y1 = T_HEAD
    m.decal("pier_head", hp.pier(x1 - x0, y1 - y0, rng, across=False), x0 * T - hp.PIER_MARGIN, y0 * T)
    m.keep_open(*T_HEAD)
    m.decal("gangway", hp.gangway(40, 30), 33 * T - 4, 35.9 * T)

    # ── 배 ──
    for name, piece, x, ground in SHIPS:
        m.stand(name, piece, x, ground)
    for k, (x, ground, paint, upright) in enumerate(SKIFFS):
        m.stand(f"skiff_{k}", hp.skiff(810 + k, paint, upright), x, ground)

    # 부두에서 배로 건 밧줄
    for k, (x, y, dx, dy) in enumerate(((9, 28, 28, 12), (18, 28, -24, 12), (21, 29, 40, 6), (36, 28, 30, 12), (46, 28, -36, 12), (24, 35, -70, 24))):
        line, (ox, oy) = hp.mooring_line(dx, dy)
        m.decal(f"mooring_{k}", line, x * T - ox, y * T - oy)

    # ── 시장: 어시장 천막이 부둣가 서쪽에 늘어선다 ──
    for k, (cloth, goods, x) in enumerate(STALLS):
        m.stand(f"stall_{k}", hp.market_stall(820 + k, cloth, goods), x, 19.6, block=(3.2, 0.9))
    for k, (x, ground) in enumerate(((6.0, 20.8), (14.4, 18.6))):
        m.stand(f"fish_{k}", ship.fish_basket(np.random.default_rng(830 + k)), x, ground, block=(0.6, 0.4))

    # ── 짐: 창고 앞과 동쪽 부둣가의 상자·통 더미, 손수레, 천 덮은 짐 ──
    for k, (columns, layers, x, ground) in enumerate(((3, 3, 30.5, 17.8), (2, 2, 41.6, 17.6), (3, 2, 44.6, 20.0), (2, 2, 39.2, 21.4), (2, 3, 49.4, 18.2),
                                                       (2, 2, 25.4, 17.8))):
        m.stand(f"crates_{k}", hp.crate_stack(840 + k, columns, layers), x, ground, block=(columns * 0.7, 0.7))
    for k, (count, x, ground) in enumerate(((3, 33.6, 21.4), (3, 12.6, 17.0), (4, 48.6, 21.4), (2, 28.0, 21.4))):
        m.stand(f"barrels_{k}", hp.barrel_stack(850 + k, count), x, ground, block=(count * 0.55, 0.6))
    m.stand("port_cargo_a", ship.cargo(rng), 22.6, 18.6, block=(2.2, 0.8))
    m.stand("port_cargo_b", ship.cargo(rng), 41.0, 21.4, block=(2.2, 0.8))
    m.stand("port_cart", cave.hand_cart(np.random.default_rng(860)), 37.5, 18.4, block=(2.2, 0.7))
    m.stand("port_luggage", station.luggage(rng), 29.5, 34.9, block=(2, 0.6))
    m.stand("port_luggage_b", station.luggage(rng), 39.0, 18.0, block=(2, 0.6))
    m.stand("port_nets_a", ship.net_pile(np.random.default_rng(861)), 8.6, 24.4, block=(1.2, 0.4))
    m.stand("port_nets_b", ship.net_pile(np.random.default_rng(862)), 19.6, 24.0, block=(1.2, 0.4))
    m.stand("pier_crates_a", hp.crate_stack(863, 2, 2), 47.4, 29.4, block=(1.4, 0.7))
    m.stand("pier_barrels_a", hp.barrel_stack(864, 2), 26.0, 34.8, block=(1.1, 0.6))
    m.stand("pier_crates_b", hp.crate_stack(865, 3, 2), 42.4, 35.4, block=(2, 0.7))
    m.decal("anchor_a", ship.anchor(rng), 2.4 * T, 20.0 * T)
    for k, (x, y) in enumerate(((31.2, 31.4), (34.6, 27.0), (18.6, 31.4), (6.4, 28.0), (46.6, 27.0), (27.0, 35.0))):
        m.decal(f"coil_{k}", ship.coil(rng), x * T, y * T)

    # ── 기중기, 등대, 깃발, 가로등 ──
    m.stand("quay_crane", hp.quay_crane(870), 43.0, 21.7, block=(2.2, 0.8))
    m.stand("lighthouse", hp.lighthouse(871), (MOLE[0] + MOLE[1]) / 2, MOLE[2] - 0.4, block=(2.2, 1.0))
    m.light((MOLE[0] + MOLE[1]) / 2 * LOGICAL, (MOLE[2] - 5.6) * LOGICAL, 70, SEA_LIGHT)
    m.stand("bunting", hp.bunting(7, 872), ROAD_X, STREET_TOP + 0.4)
    m.block(ROAD_X - 3.6, STREET_TOP, ROAD_X - 3.3, STREET_TOP + 0.4)
    m.block(ROAD_X + 3.3, STREET_TOP, ROAD_X + 3.6, STREET_TOP + 0.4)
    for k, (color, x, ground) in enumerate((("blue", 2.2, 21.7), ("red", 24.0, 21.7), ("blue", 30.0, 21.7), ("red", 37.0, 21.7), ("blue", 50.0, 21.7),
                                            ("red", 43.6, 35.9), ("blue", 24.4, 35.9))):
        m.stand(f"pennant_{k}", hp.pennant(880 + k, color), x, ground, block=(0.5, 0.3))
    for x, y in LAMPS:
        m.lamp(x, y)
    for k, (x, ground) in enumerate(((4.4, QUAY + 0.2), (15.8, QUAY + 0.2), (26.6, QUAY + 0.2), (39.0, QUAY + 0.2))):
        m.stand(f"bollard_{k}", hp.bollard(890 + k), x, ground, block=(0.6, 0.3))
    m.stand("bench_port", yard.bench(rng), 19.2, 20.8, block=(1.8, 0.4))

    # ── 이야기 지점과 포탈 ──
    m.point("north", (ROAD_X - 1, 0), (ROAD_X, 0))
    m.point("northArrive", (ROAD_X, 2))
    m.point("arrive", (ROAD_X, 19))
    m.point("board", (33, 23), (32, 23), (34, 23))
    m.point("ship", (33, 35))
    m.point("hill", (46, 9), (48, 8), (50, 9))
    m.point("crowd", *CROWD)
    m.point("sailor", *SAILORS)
    m.warp("north", "caveRoad", "southArrive", "up")
    return m


def side_wall(rows, rng):
    """돌 방파제의 옆 벽(세로 띠): 쌓은 돌과 물거품. 옆에서 물결이 닿는다."""
    h, w = int(rows * T), 8
    image = np.zeros((h, w, 4), dtype=np.uint8)
    for y in range(0, h, 7):
        tone = int(rng.choice([2, 3]))
        image[y:y + 7, :, :3] = hp.WALL[tone]
        image[y + 6:y + 7, :, :3] = hp.WALL[0]
    image[..., 3] = 255
    return image


def main(project):
    m = build()
    start = m.points["northArrive"][0]
    goals = ("arrive", "board", "ship", "hill", "crowd", "sailor")
    for name in goals:
        for cell in m.points[name]:
            if not m.reachable(start, [cell]):
                print(f"northArrive에서 {name} {cell}에 닿지 못한다")
    print(m.publish(project))
    # 산길 남쪽 출구는 서쪽 부두가 아니라 이 항구로 간다. 항구에서 돌아가는 칸과 산길 위쪽 호빗 산길로 돌아가는 길을 더한다.
    web_map.reroute(project, "caveRoad", [{"point": "south", "map": "eastPort", "target": "northArrive", "dir": "down"},
                                          {"point": "north", "map": "hobbitTrail", "target": "southArrive", "dir": "up"}],
                    points={"southArrive": [(18, 17)], "north": [(14, 0), (15, 0)]})


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
