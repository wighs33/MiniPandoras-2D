"""요정 마을(fairyGarden)과 그 밤(fairyNight)을 새로 짠다(10장 맵을 생성기로).

숲속 빈터, 서쪽에 맵 위 끝까지 솟은 거대고목의 밑동이 선 요정 마을(칸 34×24).
세계지도 배치판에서 사용자가 정한 대로(2026-11-09) 고목은 마을 서쪽, 요정의 샘은 북쪽, 달빛 버섯 숲은 남쪽이다.
  - 거대고목 밑동(fairy_props.giant_trunk): 고목 안 1층(고목 로비)으로 드는 뿌리 아치 문(treeGate). 문 앞으로 뿌리가 기어 나온다.
    쉼터는 고목 3층의 방이다(tree_floors.py).
  - 연회 자리에서 네 갈래 길: 서북쪽 고목 문, 북쪽 요정의 샘, 남쪽 달빛 버섯 숲(fairy_wilds.py), 동쪽 나무다리 건너 냇가 집들.
  - 이끼 돌 고리를 두른 연회 자리(가운데 그루터기 탁자, 연회 지점 넷이 둘레).
  - 요정의 버섯 집 여섯(빨강·파랑·보라), 덩굴 기둥 등불, 빛나는 버섯 무리, 키 큰 풀숲과 꽃밭. 둘레는 숲이다.
밤(fairyNight)은 같은 배치에 밤빛·떠도는 빛 알갱이를 얹고, 등불과 창에 불을 켜고, 탁자에 연회 음식을 차린다.

사용: python Tools/PixelGen/fairy_garden.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/fairyGarden/…, fairyNight/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/fairyGarden_preview.png, fairyNight_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import fairy_props as fp
import forest
import pixelgen as pg
from layout import LOGICAL, MapLayout

T = pg.TILE
WIDTH, HEIGHT = 34, 24
TRUNK_X, TRUNK_GROUND = 9.0, 6           # 밑동 가운데와 땅에 닿는 줄(칸): 마을 서쪽
STREAM = (26, 28)                         # 냇물 열(칸, 끝은 들지 않는다)
ROAD_ROW = 12
FEAST = (15.5, 12.5)                      # 연회 자리 가운데(칸 경계 기준 좌표)
SEATS = [(15, 10), (12, 12), (18, 12), (15, 14)]   # 연회 지점(칸): 탁자 둘레
# 연회 자리에서 뻗는 길(칸 경계 기준 좌표). 고목 문 길은 문 자리를 알아야 해서 build에서 잇는다.
NORTH_ROAD = [(16.6, 10.4), (19.5, 9.4), (22.5, 8.4), (24, 4.5), (24.2, -1)]     # 요정의 샘
SOUTH_ROAD = [(15.6, 14.6), (16.4, 18), (16.8, 22), (17, 25)]                    # 달빛 버섯 숲
EAST_ROAD = [(18.0, ROAD_ROW + 0.5), (25, ROAD_ROW + 0.5), (31.5, ROAD_ROW + 0.5)]  # 다리 건너 냇가 집들
WEST_LANE = [(5, ROAD_ROW + 0.7), (9, ROAD_ROW + 0.5), (13.2, ROAD_ROW + 0.6)]    # 서쪽 집 골목
# 버섯 집: 가운데 x, 땅 줄, 갓 색, 집 앞길이 닿는 곳
HOUSES = [(4.0, 17, "blue", (5.5, 12.8)), (11.5, 19, "red", (15.9, 16.5)), (20.0, 6, "violet", (18.6, 9.7)),
          (22.5, 19, "red", (16.6, 20.4)), (30.5, 6, "blue", (30.8, 12.4)), (30.5, 18, "violet", (30.8, 12.6))]
LANTERNS = [(3, 11), (9, 11), (12, 8), (21, 10), (21, 14), (24, 11), (29, 13), (32, 11), (10, 15), (19, 17)]
SHROOMS = [(2.5, 8.6), (17.2, 3.6), (14.0, 20.5), (25.0, 20.6), (31.5, 9.4), (6.3, 13.8), (8.0, 21.0)]
NIGHT_AMBIENT = {"color": "#101838", "alpha": 0.5}
WINDOW_LIGHT = "#ffc870"
GLOW_LIGHT = "#7ff0dc"


def build(night):
    map_id = "fairyNight" if night else "fairyGarden"
    m = MapLayout(map_id, "요정 마을", WIDTH, HEIGHT, music="sorrow" if night else "field",
                  weather="motes" if night else None, ambient=NIGHT_AMBIENT if night else None, seed=151)
    rng = np.random.default_rng(152)

    # ── 바닥: 빈터, 흙길, 냇물과 다리, 풀숲·꽃밭 ──
    glade = ((m.cx - 16.5) / 16.2) ** 2 + ((m.cy - 12.5) / 10.6) ** 2 <= 1
    # 가운데 띠는 열어 두되 서쪽·동쪽 끝은 숲으로 닫는다(그쪽으로 나가는 길이 없다).
    glade |= (m.cy >= 9) & (m.cy <= 15) & (m.cx >= 2.5) & (m.cx <= 32)
    # 남쪽 숲가의 요정 자리와, 11장에 디엘이 혼자 서 있는 냇가 동쪽 끝.
    glade |= np.hypot(m.cx - 5.5, m.cy - 20.3) <= 2.2
    glade |= np.hypot((m.cx - 30.8) / 1.4, m.cy - 20.8) <= 2.0
    gate_x = int(TRUNK_X - fp.WIDTH_TILES / 2 + fp.GATE_X)
    gate_road = [(gate_x + 0.5, TRUNK_GROUND + 0.2), (10.2, 9), (13.5, 12), (FEAST[0] - 1.5, FEAST[1])]
    roads = (gate_road, NORTH_ROAD, SOUTH_ROAD, EAST_ROAD, WEST_LANE)
    for road in roads:
        m.line("path", road, 1.6)
    for x, ground, _, (tx, ty) in HOUSES:
        m.line("path", [(x, ground + 0.4), (x + 0.3, ground + 1.6), (tx, ty)], 1.0)
    m.disc("path", FEAST[0], FEAST[1], 2.4)
    m.rect("water", STREAM[0], 0, STREAM[1], HEIGHT)
    m.rect("bridge", STREAM[0], ROAD_ROW, STREAM[1], ROAD_ROW + 1)
    for x0, y0, x1, y1 in ((6, 2, 10, 5), (2, 15, 7, 19), (29, 2, 33, 6), (29, 15, 32, 18), (12, 20, 16, 22)):
        tall = ((m.cx - (x0 + x1) / 2) / ((x1 - x0) / 2)) ** 2 + ((m.cy - (y0 + y1) / 2) / ((y1 - y0) / 2)) ** 2 <= 1
        m.kinds[tall & (m.kinds == "grass")] = "tallgrass"
    flowers = (pg.noise(HEIGHT, WIDTH, 3, rng) > 0.68) & (m.kinds == "grass")
    m.kinds[flowers] = "flowers"

    # ── 숲: 빈터 밖, 밑동 양옆 ──
    woods = ~glade & (m.kinds != "water") & (m.kinds != "bridge") & (m.kinds != "path")
    woods[:TRUNK_GROUND, int(TRUNK_X - fp.WIDTH_TILES / 2):int(TRUNK_X + fp.WIDTH_TILES / 2) + 1] = False
    keep = np.zeros_like(woods)
    for x, ground, _, _ in HOUSES:
        # 집 둘레와, 수관이 집을 가리지 않게 집 앞(남쪽) 세 줄도 비운다.
        keep[max(0, ground - 5):ground + 3, int(x) - 3:int(x) + 4] = True
    for x, y in LANTERNS:
        keep[y, x] = True
    woods &= ~keep
    shades = forest.canopy_over(~woods & (m.kinds == "path"), side=1, rows=3)
    forest.plant(m, woods & ~shades, np.random.default_rng(153))
    for y, x in zip(*np.nonzero(woods & shades)):
        m.stamp("bush", (x, y))
    m.block_where(woods)

    # ── 거대고목 밑동과 기어 나온 뿌리 ──
    trunk = fp.giant_trunk(np.random.default_rng(154))
    m.stand("giant_trunk", trunk, TRUNK_X, TRUNK_GROUND)
    m.block(int(TRUNK_X) - 6, 0, int(TRUNK_X) + 6, TRUNK_GROUND)
    m.block(int(TRUNK_X) - 7, TRUNK_GROUND - 1, int(TRUNK_X) + 7, TRUNK_GROUND)
    roots = fp.trunk_roots(np.random.default_rng(155))
    m.decal("trunk_roots", roots, (TRUNK_X - fp.WIDTH_TILES / 2) * T, TRUNK_GROUND * T - 3)
    for x, y, radius in ((TRUNK_X - fp.WIDTH_TILES / 2 + 4.4, 1.2, 26), (TRUNK_X - fp.WIDTH_TILES / 2 + 8.6, 0.7, 26),
                         (TRUNK_X - fp.WIDTH_TILES / 2 + 11.6, 2.6, 22), (gate_x + 0.5, TRUNK_GROUND - 1.3, 40)):
        if night or radius >= 40:
            m.light(x * LOGICAL, y * LOGICAL, radius, WINDOW_LIGHT, flicker=True)

    # ── 버섯 집, 등불, 빛나는 버섯, 연회 탁자, 연잎 ──
    for i, (x, ground, cap, _) in enumerate(HOUSES):
        m.stand(f"mushroom_{i}", fp.mushroom_house(np.random.default_rng(160 + i), cap), x, ground, block=(2.6, 1.6))
        if night:
            for side in (-1, 1):
                m.light((x + side * 0.7) * LOGICAL, (ground - 1.1) * LOGICAL, 18, WINDOW_LIGHT, flicker=True)
    for i, (x, y) in enumerate(LANTERNS):
        m.stand(f"lantern_{i}", fp.fairy_lantern(np.random.default_rng(170 + i)), x + 0.4, y + 1, block=(0.3, 0.3))
        m.light((x + 0.75) * LOGICAL, (y + 1) * LOGICAL - 26, 34 if night else 20, GLOW_LIGHT)
    for i, (x, y) in enumerate(SHROOMS):
        m.stand(f"shrooms_{i}", fp.glow_shrooms(np.random.default_rng(180 + i)), x, y)
        if night:
            m.light(x * LOGICAL, y * LOGICAL - 4, 14, GLOW_LIGHT)
    table = fp.feast_table(np.random.default_rng(190), night)
    m.stand("feast_table", table, FEAST[0], FEAST[1] + 0.4, block=(1.2, 0.7))
    if night:
        m.light(FEAST[0] * LOGICAL + 3, (FEAST[1] - 1) * LOGICAL, 48, WINDOW_LIGHT, flicker=True)
    # 연회 자리 둘레의 빛나는 버섯 고리. 길이 지나는 자리는 비운다.
    on_road = np.zeros_like(m.kinds, dtype=bool)
    for road in roads:
        on_road |= m.near_line(road, 1.4)
    for k in range(10):
        a = k / 10 * np.pi * 2 + 0.3
        x, y = FEAST[0] + np.cos(a) * 3.0, FEAST[1] + np.sin(a) * 2.4
        if on_road[int(y), int(x)]:
            continue
        if any(np.hypot(x - (fx + 0.5), y - (fy + 0.5)) < 1.1 for fx, fy in SEATS):
            continue
        m.stand(f"ring_{k}", fp.glow_shrooms(np.random.default_rng(200 + k)), x, y)
    m.decal("lily_pads", fp.lily_pads(STREAM[1] - STREAM[0], HEIGHT, np.random.default_rng(210)), STREAM[0] * T, 0)

    m.border()

    # ── 지점과 출구 ──
    m.point("treeGate", (gate_x, TRUNK_GROUND - 1))
    m.point("treeArrive", (gate_x, TRUNK_GROUND))
    m.point("fairy", (7, 7), (22, 11), (20, 19), (5, 20))
    m.point("buddy", (22, 15))
    m.point("feast", *SEATS)
    m.point("north", (23, 0), (24, 0), (25, 0))
    m.point("northArrive", (24, 2))
    m.point("south", (16, HEIGHT - 1), (17, HEIGHT - 1))
    m.point("southArrive", (17, HEIGHT - 3))
    m.point("dielAlone", (31, 21))
    m.warp("treeGate", "treeHall", "enter", "up")
    m.warp("north", "fairySpring", "southArrive", "up")
    m.warp("south", "glowWood", "northArrive", "down")
    return m


def main(project):
    for night in (False, True):
        m = build(night)
        start = m.points["southArrive"][0]
        for name, cells in m.points.items():
            for cell in cells:
                if name not in ("north", "south", "treeGate") and not m.reachable(start, [cell]):
                    print(f"[{m.id}] 도착에서 {name} {cell}에 닿지 못한다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
