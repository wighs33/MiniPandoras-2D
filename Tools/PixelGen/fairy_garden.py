"""페어리가든(fairyGarden)과 그 밤(fairyNight)을 새로 짠다(사용자 요청 2026-11-03: 10장 맵을 생성기로).

숲속 빈터, 맵 위 끝까지 솟은 거대고목의 밑동 아래 요정 마을. 웹게임 맵(칸 34×24)과 지점·출구 자리가 같다.
  - 거대고목 밑동(fairy_props.giant_trunk): 고목 안 1층 뿌리 홀로 드는 뿌리 아치 문(treeGate). 문 앞으로 뿌리가 기어 나온다.
    쉼터는 고목 3층의 방이다(tree_floors.py, 사용자 요청 2026-11-03).
  - 서쪽 사막에서 동쪽 아이로스 가도로 빈터를 가로지르는 흙길, 그 길을 남북으로 끊는 맑은 냇물과 나무다리(연잎·꽃).
  - 밑동에서 내려오는 길이 큰길과 만나는 곳에 이끼 돌 고리를 두른 연회 자리(가운데 그루터기 탁자, 연회 지점 넷이 둘레).
  - 요정의 버섯 집 넷(빨강·파랑·보라), 덩굴 기둥 등불, 빛나는 버섯 무리, 키 큰 풀숲과 꽃밭. 둘레는 숲이다.
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
TRUNK_X, TRUNK_GROUND = 17.0, 6          # 밑동 가운데와 땅에 닿는 줄(칸)
STREAM = (26, 28)                         # 냇물 열(칸, 끝은 들지 않는다)
ROAD_ROW = 12
FEAST = (15.5, 12.5)                      # 연회 자리 가운데(칸 경계 기준 좌표)
SEATS = [(15, 10), (12, 12), (18, 12), (15, 14)]   # 연회 지점(칸): 탁자 둘레
# 버섯 집: 가운데 x, 땅 줄, 갓 색
HOUSES = [(5.0, 6, "red"), (7.5, 17, "blue"), (30.5, 6, "violet"), (22.5, 19, "red"), (30.5, 18, "blue")]
LANTERNS = [(3, 11), (9, 11), (12, 8), (20, 8), (21, 13), (24, 11), (29, 13), (32, 11), (10, 15), (19, 17)]
SHROOMS = [(2.5, 8.6), (11.4, 4.8), (23.6, 7.2), (13.2, 18.4), (25.0, 20.6), (31.5, 9.4), (6.3, 13.8), (18.8, 3.6)]
NIGHT_AMBIENT = {"color": "#101838", "alpha": 0.5}
WINDOW_LIGHT = "#ffc870"
GLOW_LIGHT = "#7ff0dc"


def build(night):
    map_id = "fairyNight" if night else "fairyGarden"
    m = MapLayout(map_id, "페어리가든", WIDTH, HEIGHT, music="sorrow" if night else "field",
                  weather="motes" if night else None, ambient=NIGHT_AMBIENT if night else None, seed=151)
    rng = np.random.default_rng(152)

    # ── 바닥: 빈터, 흙길, 냇물과 다리, 풀숲·꽃밭 ──
    glade = ((m.cx - 16.5) / 16.2) ** 2 + ((m.cy - 12.5) / 10.6) ** 2 <= 1
    glade |= (m.cy >= 9) & (m.cy <= 15)
    # 남쪽 숲가의 요정 자리와, 11장에 디엘이 혼자 서 있는 냇가 동쪽 끝.
    glade |= np.hypot(m.cx - 5.5, m.cy - 20.3) <= 2.2
    glade |= np.hypot((m.cx - 30.8) / 1.4, m.cy - 20.8) <= 2.0
    m.line("path", [(0, ROAD_ROW + 0.5), (8, ROAD_ROW + 0.4), (13, ROAD_ROW + 0.6), (18, ROAD_ROW + 0.4), (25, ROAD_ROW + 0.5), (34, ROAD_ROW + 0.5)], 1.6)
    m.line("path", [(16.5, 6), (16.4, 9), (16.0, ROAD_ROW)], 1.4)
    for x, ground, _ in HOUSES:
        m.line("path", [(x, ground + 0.4), (x + 0.3, ground + 1.6), (x + (16 - x) * 0.25, ROAD_ROW + 0.4)], 1.0)
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
    woods[:TRUNK_GROUND, 10:24] = False
    keep = np.zeros_like(woods)
    for x, ground, _ in HOUSES:
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
    m.block(11, 0, 23, TRUNK_GROUND)
    m.block(10, TRUNK_GROUND - 1, 24, TRUNK_GROUND)
    roots = fp.trunk_roots(np.random.default_rng(155))
    m.decal("trunk_roots", roots, (TRUNK_X - fp.WIDTH_TILES / 2) * T, TRUNK_GROUND * T - 3)
    gate_x = int(TRUNK_X - fp.WIDTH_TILES / 2 + fp.GATE_X)
    for x, y, radius in ((TRUNK_X - fp.WIDTH_TILES / 2 + 4.4, 1.2, 26), (TRUNK_X - fp.WIDTH_TILES / 2 + 8.6, 0.7, 26),
                         (TRUNK_X - fp.WIDTH_TILES / 2 + 11.6, 2.6, 22), (gate_x + 0.5, TRUNK_GROUND - 1.3, 40)):
        if night or radius >= 40:
            m.light(x * LOGICAL, y * LOGICAL, radius, WINDOW_LIGHT, flicker=True)

    # ── 버섯 집, 등불, 빛나는 버섯, 연회 탁자, 연잎 ──
    for i, (x, ground, cap) in enumerate(HOUSES):
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
    # 연회 자리 둘레의 빛나는 버섯 고리.
    for k in range(10):
        a = k / 10 * np.pi * 2 + 0.3
        x, y = FEAST[0] + np.cos(a) * 3.0, FEAST[1] + np.sin(a) * 2.4
        if abs(y - (ROAD_ROW + 0.5)) < 0.9 or (abs(x - 16.2) < 0.9 and y < FEAST[1]):
            continue
        if any(np.hypot(x - (fx + 0.5), y - (fy + 0.5)) < 1.1 for fx, fy in SEATS):
            continue
        m.stand(f"ring_{k}", fp.glow_shrooms(np.random.default_rng(200 + k)), x, y)
    m.decal("lily_pads", fp.lily_pads(STREAM[1] - STREAM[0], HEIGHT, np.random.default_rng(210)), STREAM[0] * T, 0)

    m.border()

    # ── 지점과 출구: 웹게임 페어리가든과 같다 ──
    m.point("treeGate", (gate_x, TRUNK_GROUND - 1))
    m.point("treeArrive", (gate_x, TRUNK_GROUND))
    m.point("fairy", (7, 7), (22, 10), (20, 19), (5, 20))
    m.point("buddy", (20, 7))
    m.point("feast", *SEATS)
    m.point("west", (0, ROAD_ROW))
    m.point("arrive", (1, ROAD_ROW))
    m.point("roadArrive", (32, ROAD_ROW))
    m.point("road", (33, ROAD_ROW))
    m.point("dielAlone", (31, 21))
    m.warp("west", "westSands", "eastArrive", "left")
    m.warp("treeGate", "treeHall", "enter", "up")
    m.warp("road", "airosRoad", "start", "right")
    return m


def main(project):
    for night in (False, True):
        m = build(night)
        start = m.points["arrive"][0]
        for name, cells in m.points.items():
            for cell in cells:
                if name not in ("west", "road", "treeGate") and not m.reachable(start, [cell]):
                    print(f"[{m.id}] 도착에서 {name} {cell}에 닿지 못한다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
