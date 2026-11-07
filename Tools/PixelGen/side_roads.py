"""이야기 밖으로 이어지는 길과 던전 맵을 그린다(세계가 지도대로 이어지게 한다).

  hobbitTrail      호빗 소굴 가는 산길: 종착역 동쪽 길에서 호빗 소굴 앞까지 마차 없이 걷는다. 소굴을 나오면 이 산길이고,
                   아래쪽 갈림길로 내려가면 선착장 가는 산길이다.
  leviathanCoast   레비아탄의 바다: 힐리아 서부와 동부 사이 바다 가운데의 바위섬. 서쪽 부두 끝의 문으로만 들어온다(west_port.py).
                   이 바다만 건너면 사막이지만 레비아탄 때문에 배가 다니지 못해 다들 동쪽 선착장에서 멀리 돌아간다.
                   부서진 배가 그 까닭을 보여 준다.
  leviathanDen     레비아탄의 둥지: 바위섬 절벽의 바다 동굴 안쪽. 물로 둘러싸인 돌 마당에서 싸운다.
  dragonRoad       드래곤 고원 가는 길: 힐리아 서부 북쪽 고갯길(미노가 달아난 길)에서 이어지는 산길.
  dragonPlateau    드래곤 고원: 옛 용의 뼈가 널린 고원. 미노가 뼈 왕좌에서 왕 행세를 하고,
                   남동쪽 길로 나가면 해안을 따라 철갑 동굴로 간다(west_coast.py).

사용: python Tools/PixelGen/side_roads.py <프로젝트 루트> [맵 id ...]
  Assets/MiniPandoras/Art/PixelGen/Maps/<맵 id>/…    맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/<맵 id>_preview.png       미리보기
"""
import math
import sys
from pathlib import Path

import numpy as np

import cave
import forest
import harbor_props as hp
import pixelgen as pg
import props
import west_sands
from houses import WOOD, Canvas, outline
from layout import MapLayout

T = pg.TILE
ROCK = pg.hexes("#22242c", "#363a46", "#4e5362", "#6c7282", "#8e95a4")
WET_ROCK = pg.hexes("#16242c", "#24404a", "#36606a", "#4e8088", "#7aa8a8")
BONE = pg.hexes("#5a5040", "#8a7e64", "#b8ac8c", "#dcd2b4", "#f2ead2")
STONE_SEAL = pg.hexes("#3e3a34", "#5c564c", "#7a7264")
BANNER = pg.hexes("#5a1a1a", "#8a2a24")
GOLD = pg.hexes("#c8982c")[0]
DARK = pg.hexes("#07070c")[0]


# ── 소품 ─────────────────────────────────────────────────

def rock_arch(palette, seed, w=132, h=108):
    """바위 벽에 뚫린 굴 입구."""
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    rng = np.random.default_rng(seed)
    body = c.polygon([(2, ground), (10, 44), (30, 16), (66, 6), (102, 16), (122, 44), (w - 2, ground)])
    c.paint(body, palette[2])
    for _ in range(46):
        x, y = rng.uniform(6, w - 6), rng.uniform(10, ground)
        rx, ry = rng.uniform(5, 13), rng.uniform(3, 8)
        c.paint(body & c.ellipse(x, y, rx, ry), palette[int(rng.integers(1, 4))])
    c.paint(body & (c.yy > ground - 8), palette[1])
    rim = c.polygon([(cx - 32, ground), (cx - 30, 48), (cx - 16, 28), (cx + 16, 28), (cx + 30, 48), (cx + 32, ground)])
    c.paint(rim, palette[3])
    hole = c.polygon([(cx - 26, ground), (cx - 24, 50), (cx - 12, 34), (cx + 12, 34), (cx + 24, 50), (cx + 26, ground)])
    c.paint(hole, DARK)
    return outline(c.image), (cx, ground)


def rib_pair(seed):
    """모래에 박힌 큰 갈비뼈 한 쌍."""
    w, h = 104, 88
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for side in (-1, 1):
        points = [(cx + side * (10 + 36 * math.sin(t / 8 * math.pi * 0.86)), ground - t * 10) for t in range(9)]
        for k in range(8):
            c.line(points[k][0], points[k][1], points[k + 1][0], points[k + 1][1], BONE[2], 6 - k // 3)
            c.line(points[k][0] - side, points[k][1], points[k + 1][0] - side, points[k + 1][1], BONE[3], 2)
    return outline(c.image), (cx, ground)


def bone_throne():
    """미노의 왕좌: 뼈를 쌓고 찢어진 깃발을 달았다."""
    w, h = 112, 112
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for x in (cx - 44, cx + 40):
        c.paint(c.rect(x, 12, x + 4, ground - 10), WOOD[2])
        c.paint(c.polygon([(x + 4, 14), (x + 26 if x < cx else x - 22, 22), (x + 4, 44)]), BANNER[1])
    for k, x in enumerate(range(-30, 31, 12)):
        c.paint(c.polygon([(cx + x - 6, 64), (cx + x + 6, 64), (cx + x * 1.3, 18 + abs(x) // 2)]), BONE[2 + k % 2])
    c.paint(c.rect(cx - 30, 60, cx + 31, 74), BONE[1])
    c.paint(c.rect(cx - 32, 72, cx + 33, 82), BONE[3])
    c.paint(c.rect(cx - 36, 82, cx + 37, ground), STONE_SEAL[1])
    c.paint(c.rect(cx - 36, 82, cx + 37, 85), STONE_SEAL[2])
    c.paint(c.ellipse(cx, 40, 10, 9), BONE[4])
    for side in (-1, 1):
        c.paint(c.ellipse(cx + side * 4, 39, 2.4, 2), DARK)
    c.paint(c.rect(cx - 12, 30, cx + 13, 33), GOLD)
    return outline(c.image), (cx, ground)


def wreck(seed, flip=False):
    """부서져 뒤집힌 배의 뱃머리."""
    w, h = 128, 76
    c = Canvas(w, h)
    ground = h - 2
    rng = np.random.default_rng(seed)
    hull = c.polygon([(6, ground), (14, 34), (58, 16), (118, 8), (96, 42), (90, ground)])
    c.paint(hull, WOOD[2])
    for k in range(7):
        y = 16 + k * 9
        c.paint(hull & (c.yy >= y) & (c.yy < y + 2), WOOD[1])
    hole = c.polygon([(34, ground), (42, 44), (60, 38), (72, 52), (66, ground)])
    c.paint(hole, DARK)
    for x in (44, 54, 64):
        c.line(x, ground, x + 4, 40, WOOD[3], 2)
    for _ in range(14):
        x, y = rng.uniform(16, 88), rng.uniform(24, ground - 4)
        c.paint(hull & c.ellipse(x, y, 3, 2), pg.hexes("#3e5a44")[0])
    image = outline(c.image)
    return (image[:, ::-1] if flip else image), (w // 2, ground)


def sea_shadow():
    """물속을 지나가는 거대한 그림자."""
    w, h = 300, 150
    image = np.zeros((h, w, 4), np.uint8)
    yy, xx = np.mgrid[0:h, 0:w]
    mask = np.zeros((h, w), bool)
    for k in range(26):
        t = k / 25
        x = 20 + t * 250
        y = h / 2 + math.sin(t * math.pi * 2.2) * 34
        r = 8 + 22 * math.sin(t * math.pi) ** 0.8
        mask |= (xx - x) ** 2 + ((yy - y) * 1.3) ** 2 < r ** 2
    image[mask] = (8, 20, 34, 110)
    return image


# ── 맵 ───────────────────────────────────────────────────

def edge_bushes(m, woods, trunks):
    """나무를 세우지 못한 숲 가장자리는 덤불로 둘러 막힌 곳임이 보이게 한다."""
    edge = woods & ~trunks & (np.roll(~woods, 1, 0) | np.roll(~woods, -1, 0) | np.roll(~woods, 1, 1) | np.roll(~woods, -1, 1))
    for y, x in zip(*np.nonzero(edge)):
        if m.kinds[y, x] == "woods" or m.kinds[y, x] == "grass":
            m.stamp("bush", (int(x), int(y)))


def planted(m, region, seed, keep_clear):
    trunks = np.zeros_like(region)
    for x, y, _ in forest.plant(m, region & ~forest.canopy_over(keep_clear, side=1, rows=3), np.random.default_rng(seed)):
        trunks[y, x] = True
    return trunks


def hobbit_trail():
    width, height = 36, 24
    m = MapLayout("hobbitTrail", "호빗 소굴 가는 산길", width, height, music="field", seed=301)
    route = [(-1, 17.5), (6, 17.5), (11, 14.5), (17, 15), (21, 18.5), (27, 18), (30, 13), (25, 10), (26.5, 6)]
    down_to_port = [(26, 18), (26, 25)]
    walk = m.near_line(route, 3.4) | m.near_line(down_to_port, 2.6)
    m.rect("cliff", 0, 0, width, 4)
    woods = ~walk & (m.cy > 4)
    m.line("path", route, 2.0)
    m.line("path", down_to_port, 2.0)
    m.stamp("tallgrass", (9, 11), (10, 11), (15, 18), (16, 18), (29, 8), (23, 16))
    m.stamp("rock", (14, 12), (24, 13), (20, 20))
    trunks = planted(m, woods, 302, walk)
    edge_bushes(m, woods, trunks)
    m.block_where(woods)
    m.border()
    m.stand("cave_mouth", rock_arch(ROCK, 303), 26.5, 4.6, block=(3.8, 0.6))
    m.stand("cart_wreck", cave.hand_cart(np.random.default_rng(304)), 22.2, 11.6, block=(2.2, 0.7))
    m.point("west", (0, 17), (0, 18))
    m.point("westArrive", (1, 17))
    m.point("mouth", (26, 5))
    m.point("mouthArrive", (26, 7))
    m.point("south", (25, 23), (26, 23))
    m.point("southArrive", (26, 21))
    m.warp("west", "station", "arrive", "left")
    m.warp("south", "caveRoad", "enter", "down")
    return m


def broken_mast(seed):
    """난파선 돛대: 물 위로 비스듬히 솟은 부러진 기둥과 찢어진 돛 자락."""
    rng = np.random.default_rng(seed)
    w, h = 60, 90
    c = Canvas(w, h)
    ground = h - 8
    c.line(20, ground, 34, 8, WOOD[2], 5)
    c.line(20, ground, 34, 8, WOOD[4], 1)
    c.line(14, 30, 50, 22, WOOD[2], 3)
    cloth = c.polygon([(18, 31), (46, 25), (42, 58), (24, 50)])
    c.paint(cloth, pg.hexes("#8e8772")[0])
    c.paint(cloth & (pg.noise(h, w, 4, rng) > 0.6), pg.hexes("#6a6452")[0])
    c.image[(pg.noise(h, w, 5, rng) > 0.7) & cloth, 3] = 0
    image = outline(c.image)
    image[h - 8:h - 5, 8:34, :3] = pg.WATER[6]
    image[h - 8:h - 5, 8:34, 3] = (rng.random((3, 26)) < 0.5) * 180
    return image, (22, h - 6)


def leviathan_coast():
    """서쪽 부두의 문으로만 들어오는 바다 가운데 바위섬. 부서진 배들이 밀려와 있고 절벽 아래 바다 동굴이 둥지로 이어진다."""
    width, height = 38, 24
    m = MapLayout("leviathanCoast", "레비아탄의 바다", width, height, music="field",
                  ambient={"color": "#0a2034", "alpha": 0.3}, seed=311)
    rng = np.random.default_rng(312)
    wobble = (pg.noise(height * 8, width * 8, 28, rng)[::8, ::8] - 0.5) * 0.5
    gate_rock = ((m.cx - 8.5) / 5.5) ** 2 + ((m.cy - 5.5) / 3.6) ** 2 + wobble < 1
    main_rock = ((m.cx - 25) / 12.6) ** 2 + ((m.cy - 9) / 7.4) ** 2 + wobble < 1
    spit = m.near_line([(10, 7), (15, 9.5)], 1.6)
    m.kinds[:, :] = "water"
    m.kinds[gate_rock | main_rock | spit] = "sand"
    m.rect("cliff", 15, 0, 35, 3)
    m.kinds[(m.cx > 34) & (m.cy < 3)] = "water"
    m.border()
    for k, (x, y) in enumerate(((32, 7), (18, 8), (30, 13), (20, 13), (13, 6), (5, 4), (12, 4), (35, 10))):
        m.stand(f"sand_rock_{k}", hp.shore_rock(330 + k), x + 0.5, y + 0.9, block=(1.0, 0.5))
    for k, (x, y) in enumerate(((3, 13), (9, 15), (15, 19), (27, 20), (34, 18), (2, 2), (20, 21), (36, 15))):
        m.stand(f"sea_rock_{k}", hp.shore_rock(340 + k, wet=True), x + 0.5, y + 0.9)

    m.stand("sea_cave", rock_arch(WET_ROCK, 314), 24.5, 3.8, block=(3.8, 0.5))
    m.stand("wreck_a", wreck(315), 19.5, 15.8, block=(4.6, 1.0))
    m.stand("wreck_b", wreck(316, flip=True), 30.5, 11.6, block=(4.6, 1.0))
    m.stand("wreck_c", wreck(317), 8.0, 19.5)
    m.stand("wreck_mast_a", broken_mast(318), 12.5, 15.5)
    m.stand("wreck_mast_b", broken_mast(319), 31.0, 20.5)
    m.stand("wreck_boat_a", hp.sunken_boat(320), 25.0, 19.0)
    m.stand("wreck_boat_b", hp.sunken_boat(321), 3.0, 10.0)
    m.decal("sea_shadow", sea_shadow(), 1 * T, 12 * T)
    for kind, x, y in (("barrel", 27, 14), ("crate", 21, 11), ("barrel", 15, 8)):
        m.thing(kind, x, y)
    m.stand("coast_sign", props.sign(np.random.default_rng(323)), 28.5, 10.9, block=(1.0, 0.5))

    # 돌아가는 문: 서쪽 부두의 문과 같은 그림이다. 북쪽으로 나가므로 들어올 때는 문 아래에 선다.
    m.stand("sea_gate", hp.sea_gate(322), 8.5, 4.2)
    m.block(6.2, 3.8, 7.2, 4.2)
    m.block(9.8, 3.8, 10.8, 4.2)
    m.light(8.5 * 16, 2.5 * 16, 60, "#6ae0e8", flicker=True)
    m.point("sign", (28, 10), blocked=True)
    m.point("fisher", (24, 12))
    m.point("gate", (24, 4))
    m.point("gateArrive", (24, 5))
    m.point("back", (7, 4), (8, 4), (9, 4))
    m.point("pierArrive", (8, 6))
    m.warp("back", "port", "gateArrive", "up")
    return m


def leviathan_den():
    width, height = 28, 18
    m = MapLayout("leviathanDen", "레비아탄의 둥지", width, height, music="castle",
                  ambient={"color": "#061a2e", "alpha": 0.42}, seed=321)
    m.kinds[:, :] = "water"
    floor = ((m.cx - 14) / 11.0) ** 2 + ((m.cy - 8.0) / 6.2) ** 2 < 1
    m.kinds[floor] = "cobble"
    m.rect("bridge", 13, 14, 15, 18)
    m.rect("cliff", 0, 0, width, 1)
    m.border()
    for k, (x, ground) in enumerate(((6.0, 5.0), (22.0, 5.0), (5.6, 11.0), (22.4, 11.0))):
        m.stand(f"pillar_{k}", cave.stalagmite(np.random.default_rng(330 + k), 42), x, ground, block=(0.7, 0.4))
        m.light(x * 16, (ground - 1.2) * 16, 44, "#6ae0e8", flicker=True)
    m.stand("wreck_den", wreck(331), 14, 2.8, block=(4.6, 0.8))
    m.point("boss", (13, 6))
    m.point("enter", (13, 14))
    m.point("south", (13, 17), (14, 17))
    m.warp("south", "leviathanCoast", "gateArrive", "down")
    return m


def desert_ground(m):
    m.paint_ground = lambda: west_sands.desert(m.kinds.tolist(), m.seed)


def dragon_road():
    width, height = 30, 26
    m = MapLayout("dragonRoad", "드래곤 고원 가는 길", width, height, music="field", seed=341)
    route = [(14.5, 27), (14, 21), (8, 17), (10, 12), (19, 10), (21, 5), (15.5, -1)]
    m.kinds[:, :] = "sand"
    walk = m.near_line(route, 3.0)
    near = m.near_line(route, 6.0)
    m.kinds[~near] = "cliff"
    m.kinds[near & ~walk] = "dune"
    m.line("path", route, 2.4)
    m.stamp("dry", (11, 19), (12, 19), (16, 12), (17, 12), (20, 8))
    m.stamp("rock", (6, 14), (13, 8), (22, 12), (17, 21))
    for x, y in ((5, 19), (12, 15), (23, 8), (18, 15)):
        m.tree(x, y, "cactus")
    m.stand("ribs_road", rib_pair(342), 15.5, 7.4, block=(0.8, 0.3))
    m.border()
    m.point("south", (14, 25), (15, 25))
    m.point("southArrive", (14, 23))
    m.point("north", (15, 0), (16, 0))
    m.point("northArrive", (15, 2))
    m.warp("south", "westSands", "northArrive", "down")
    m.warp("north", "dragonPlateau", "southArrive", "up")
    desert_ground(m)
    return m


def dragon_plateau():
    width, height = 40, 28
    m = MapLayout("dragonPlateau", "드래곤 고원", width, height, music="field", seed=351)
    m.kinds[:, :] = "sand"
    bowl = ((m.cx - 20) / 18.0) ** 2 + ((m.cy - 15.0) / 11.5) ** 2 < 1
    m.kinds[~bowl] = "cliff"
    m.kinds[bowl & (((m.cx - 20) / 15.0) ** 2 + ((m.cy - 15.0) / 9.5) ** 2 > 1)] = "dune"
    route = [(20, 29), (20, 22), (19.5, 15), (20, 7)]
    south_east = [(20, 19), (28, 20), (32, 24), (32, 29)]
    m.kinds[m.near_line(south_east, 3)] = "sand"
    m.line("path", route, 2.4)
    m.line("path", south_east, 2.2)
    m.rect("sand", 19, 24, 22, 28)
    m.stamp("dry", (13, 12), (14, 12), (27, 18), (28, 18), (12, 19), (26, 10))
    m.stamp("rock", (10, 15), (31, 13), (14, 22), (27, 22))
    for k, (x, ground) in enumerate(((11.5, 10.6), (29.5, 11.2), (8.5, 18.6), (31, 19.6), (15, 19.5))):
        m.stand(f"ribs_{k}", rib_pair(352 + k), x, ground, block=(0.8, 0.3))
    m.border()
    m.stand("bone_throne", bone_throne(), 20, 11.6, block=(3.2, 0.8))
    for x, y in ((6, 15), (34, 16), (24, 24)):
        m.tree(x, y, "cactus")
    m.point("mino", (20, 13))
    m.point("chest", (23, 12))
    m.point("south", (20, 27), (21, 27))
    m.point("southArrive", (20, 25))
    m.point("southEast", (31, 27), (32, 27))
    m.point("southEastArrive", (32, 25))
    m.warp("south", "dragonRoad", "northArrive", "down")
    m.warp("southEast", "duneField", "northArrive", "down")
    desert_ground(m)
    return m


MAPS = {
    "hobbitTrail": (hobbit_trail, "westArrive", ("mouth", "south")),
    "leviathanCoast": (leviathan_coast, "pierArrive", ("back", "gateArrive", "fisher")),
    "leviathanDen": (leviathan_den, "enter", ("boss",)),
    "dragonRoad": (dragon_road, "southArrive", ("northArrive",)),
    "dragonPlateau": (dragon_plateau, "southArrive", ("mino", "southEastArrive")),
}


def main(project, only):
    for map_id, (build, start, goals) in MAPS.items():
        if only and map_id not in only:
            continue
        m = build()
        for goal in goals:
            for cell in m.points[goal]:
                if not m.reachable(m.points[start][0], [cell]):
                    print(f"{map_id}: {start}에서 {goal} {cell}에 닿지 못한다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), set(sys.argv[2:]))
