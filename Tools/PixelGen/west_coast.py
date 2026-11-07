"""힐리아 서부의 남쪽 해안과 철갑 동굴을 짠다(서부도 동부처럼 갈 수 있는 곳을 넓힌다).

드래곤 고원에서 산줄기 남쪽 끝을 돌아 해안선을 따라 U자로 간다. 동굴은 산 바깥(동쪽) 자락에 있다.

    드래곤 고원 → 모래바람 들판 → 바닷바람 해안길 → 갈림 해안 ┬ 철갑 동굴 통로 → 철갑 동굴 깊은 곳(철갑 드래곤의 던전 문)
                                                         └ 아이로스 가도

  duneField     모래바람 들판: 고원 아래 사구 벌판. 남쪽은 바다다.
  coastRoad     바닷바람 해안길: 남쪽 바닷가를 동쪽으로 가다가 북쪽으로 굽는다. 난파선이 밀려와 있다.
  coastFork     갈림 해안: 왼쪽 산 벽에 동굴 입구가, 오른쪽 위로 아이로스 가도가 있다. 팻말이 선 곳에서 길이 갈린다.
  ironCave      철갑 동굴 통로: 녹슨 쇳덩이가 박힌 구불구불한 굴. 군데군데 빈 갑주가 돌아다닌다.
  ironDepths    철갑 동굴 깊은 곳: 북쪽 끝의 쇠문이 철갑 드래곤의 던전 입구다. 아직 쇠사슬로 잠겨 있다.

사용: python Tools/PixelGen/west_coast.py <프로젝트 루트> [맵 id ...]
  Assets/MiniPandoras/Art/PixelGen/Maps/<맵 id>/…    맵 데이터와 그림(layout.py)
  Assets/MiniPandoras/Art/PixelGen/Maps/airosRoad/map.json   갈림 해안으로 돌아오는 출구를 더한 아이로스 가도
  Docs/ArtUpgrade/PixelGen/<맵 id>_preview.png       미리보기
"""
import sys
from pathlib import Path

import numpy as np

import cave
import pixelgen as pg
import ship
import side_roads
import terrain
import web_map
from houses import Canvas, outline
from layout import SUB, MapLayout

T = pg.TILE
PLATE = pg.hexes("#191c24", "#2c313c", "#454c5a", "#677080", "#939cab")
RUST = pg.hexes("#3e2014", "#6a3820", "#94552c")
ROCK_FRAME = pg.hexes("#22242c", "#363a46", "#4e5362")
DARK = pg.hexes("#07070c")[0]
CAVE_AMBIENT = {"color": "#0a0c14", "alpha": 0.45}
CAVE_LIGHT = "#ffab4a"


# ── 소품 ──────────────────────────────────────

def iron_scale(seed):
    """떨어져 나온 철갑 비늘: 쇠판 한 장. 철갑 드래곤이 산다는 첫 단서다."""
    rng = np.random.default_rng(seed)
    w, h = 44, 26
    c = Canvas(w, h)
    lean = int(rng.integers(-3, 4))
    shape = c.polygon([(4, 20), (20 + lean, 3), (40, 12), (34, 23), (12, 24)])
    c.paint(shape, PLATE[2])
    c.paint(shape & (c.yy < 12), PLATE[3])
    c.paint(shape & (c.xx + c.yy * 2 > 62), PLATE[1])
    c.paint(shape & (c.yy > 19) & (c.xx < 22), RUST[1])
    for x, y in ((16, 17), (24, 11), (31, 16)):
        c.paint(c.ellipse(x, y, 1.6, 1.6), PLATE[4])
    return outline(c.image), (w // 2, h - 2)


def iron_gate():
    """철갑 드래곤의 던전 문: 바위 문틀 안의 쇠문 두 짝, 가운데 용 머리 문장, 가로지른 쇠사슬과 자물쇠."""
    w, h = 248, 184
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    frame = c.arch(cx - 112, 6, cx + 112, ground)
    c.paint(frame, ROCK_FRAME[1])
    c.paint(frame & (c.yy < 30), ROCK_FRAME[2])
    door = c.arch(cx - 92, 24, cx + 92, ground)
    c.paint(door, PLATE[1])
    for y in range(44, ground, 26):
        c.paint(door & (c.yy >= y) & (c.yy < y + 3), PLATE[0])
        c.paint(door & (c.yy >= y + 3) & (c.yy < y + 5), PLATE[3])
    for x in (-62, -30, 30, 62):
        for y in range(38, ground - 4, 13):
            c.paint(door & c.ellipse(cx + x, y, 2.2, 2.2), PLATE[4])
    c.paint(door & (np.abs(c.xx - cx) < 3), DARK)
    c.paint(door & (c.yy > ground - 26) & (c.xx + c.yy * 0.6 > cx + 130), RUST[1])
    # 용 머리 문장: 뿔과 턱을 쇠판으로 덧댔다.
    for side in (-1, 1):
        c.paint(c.polygon([(cx + side * 14, 72), (cx + side * 56, 40), (cx + side * 34, 82)]), PLATE[3])
        c.paint(c.polygon([(cx + side * 16, 74), (cx + side * 46, 50), (cx + side * 32, 80)]), PLATE[4])
    head = c.polygon([(cx - 30, 66), (cx + 30, 66), (cx + 40, 96), (cx + 16, 128), (cx - 16, 128), (cx - 40, 96)])
    c.paint(head, PLATE[2])
    c.paint(head & (c.yy < 86), PLATE[3])
    for side in (-1, 1):
        c.paint(c.polygon([(cx + side * 10, 88), (cx + side * 30, 80), (cx + side * 26, 92), (cx + side * 12, 96)]), RUST[2])
    c.paint(c.rect(cx - 12, 112, cx + 13, 116), DARK)
    # 가로지른 쇠사슬과 자물쇠
    chain_y = 142
    for x in range(cx - 104, cx + 104, 12):
        c.paint(c.ellipse(x, chain_y + (2 if (x // 12) % 2 else -1), 6, 3.4) & ~c.ellipse(x, chain_y + (2 if (x // 12) % 2 else -1), 3.2, 1.2), PLATE[3])
    c.paint(c.rect(cx - 13, chain_y - 8, cx + 14, chain_y + 18), RUST[1])
    c.paint(c.rect(cx - 13, chain_y - 8, cx + 14, chain_y - 4), RUST[2])
    c.paint(c.ellipse(cx, chain_y + 6, 3, 4), DARK)
    return outline(c.image), (cx, ground)


# ── 동굴 공통 ──────────────────────────────────

def cave_floor(width, height, rooms, lanes, rng):
    """방(타원)과 굴(꺾은선과 반폭)을 이은 바닥(점 마스크). 맵 밖으로 나가는 굴은 출구가 된다."""
    h, w = height * T, width * T
    yy, xx = np.mgrid[0:h, 0:w]
    x, y = (xx + 0.5) / T, (yy + 0.5) / T
    wobble = (pg.fractal_noise(h, w, rng, ((72, 0.7), (28, 0.3))) - 0.5) * 0.16
    inside = np.full((h, w), -1.0)
    for cx, cy, rx, ry in rooms:
        inside = np.maximum(inside, 1 - np.hypot((x - cx) / rx, (y - cy) / ry))
    for points, half in lanes:
        for (ax, ay), (bx, by) in zip(points, points[1:]):
            dx, dy = bx - ax, by - ay
            t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
            inside = np.maximum(inside, (1 - np.hypot(x - (ax + t * dx), y - (ay + t * dy)) / half) * 0.5)
    return terrain.box_blur((inside + wobble > 0).astype(float), 4) > 0.5


def iron_ore(image, floor, rng):
    """바위에 박힌 녹슨 쇠맥."""
    h, w = floor.shape
    veins = (pg.fractal_noise(h, w, rng, ((26, 0.6), (8, 0.4))) > 0.7) & ~floor
    image[veins] = RUST[0]
    image[veins & (pg.fractal_noise(h, w, rng, ((5, 1.0),)) > 0.62)] = RUST[1]


def carve(m, rooms, lanes, torches, rng):
    """방과 굴을 파서 바닥 그림과 충돌을 만든다. 돌려주는 값: 바닥 마스크."""
    floor = cave_floor(m.width, m.height, rooms, lanes, rng)
    face, face_height = cave.faces(floor)
    image = cave.paint_ground(floor, face, face_height, rng)
    iron_ore(image, floor, rng)
    piece = T // SUB
    rock_share = (~floor).reshape(m.height * SUB, piece, m.width * SUB, piece).mean(axis=(1, 3))
    m.solid |= rock_share > 0.4
    m.border()
    for x in torches:
        cave.torch_bracket(m, image, face_height, x, rng)
    m.paint_ground = lambda: image
    return floor


def scatter_scales(m, spots, seed):
    for k, (x, ground) in enumerate(spots):
        m.stand(f"iron_scale_{k}", iron_scale(seed + k), x, ground, block=(1.2, 0.4))


# ── 맵 ─────────────────────────────────────────

def dune_field():
    width, height = 44, 30
    m = MapLayout("duneField", "모래바람 들판", width, height, music="field", seed=371)
    m.kinds[:, :] = "sand"
    road = [(14, -1), (14, 8), (19, 14), (29, 15), (35, 19), (width + 1, 19)]
    shore = 25.4 + 0.9 * np.sin(m.cx * 0.34) + 0.4 * np.sin(m.cx * 1.1 + 1)
    m.kinds[m.cy > shore] = "water"
    for cx, cy, rx, ry in ((7, 12, 6, 4), (33, 7, 8, 3.6), (24, 21, 7, 2.6), (39, 13, 3.6, 3)):
        m.kinds[(((m.cx - cx) / rx) ** 2 + ((m.cy - cy) / ry) ** 2 < 1) & (m.kinds == "sand")] = "dune"
    m.rect("cliff", 0, 0, 11, 3)
    m.rect("cliff", 18, 0, width, 3)
    m.line("path", road, 2.2)
    m.stamp("dry", (8, 18), (9, 18), (10, 19), (24, 9), (25, 9), (26, 10), (31, 12), (5, 21), (6, 21), (41, 23))
    m.stamp("rock", (6, 7), (22, 6), (38, 8), (11, 22), (30, 22))
    for x, y in ((9, 8), (20, 19), (26, 5), (37, 15), (5, 15), (15, 20), (41, 6)):
        m.tree(x, y, "cactus")
    for k, (x, ground) in enumerate(((30, 11), (12, 15.6), (22, 23.2))):
        m.stand(f"dune_ribs_{k}", side_roads.rib_pair(372 + k), x, ground, block=(0.8, 0.3))
    m.border()
    m.point("north", (13, 0), (14, 0))
    m.point("northArrive", (14, 2))
    m.point("east", (width - 1, 18), (width - 1, 19), (width - 1, 20))
    m.point("eastArrive", (width - 3, 19))
    m.warp("north", "dragonPlateau", "southEastArrive", "up")
    m.warp("east", "coastRoad", "westArrive", "right")
    side_roads.desert_ground(m)
    return m


def coast_road():
    width, height = 48, 24
    m = MapLayout("coastRoad", "바닷바람 해안길", width, height, music="field", seed=381)
    m.kinds[:, :] = "sand"
    road = [(-1, 11), (12, 11.5), (24, 13), (33, 11), (38, 6), (38, -1)]
    shore = 17.4 + 0.8 * np.sin(m.cx * 0.3) + 0.4 * np.sin(m.cx * 0.9 + 2)
    east_shore = 43.5 + 1.2 * np.sin(m.cy * 0.5)
    m.kinds[(m.cy > shore) | (m.cx > east_shore)] = "water"
    m.line("cliff", [(-1, 2.6), (12, 3.6), (24, 4.2), (33, 3.2)], 6)
    m.kinds[(m.cy < 3) & (m.cx < 34)] = "cliff"
    m.line("path", road, 2.2)
    m.kinds[(m.cx > 13) & (m.cx < 22) & (m.cy > 7) & (m.cy < 10) & (m.kinds == "sand")] = "dune"
    m.stamp("dry", (6, 8), (7, 8), (27, 8), (28, 8), (41, 12), (17, 15), (18, 15))
    m.stamp("rock", (4, 14), (20, 16), (33, 15), (41, 7), (29, 7))
    for x, y in ((9, 8), (26, 16), (41, 10), (3, 8), (36, 15)):
        m.tree(x, y, "palm")
    m.stand("coast_wreck_a", side_roads.wreck(382), 14, 16.8, block=(4.6, 0.8))
    m.stand("coast_wreck_b", side_roads.wreck(383, flip=True), 30, 17.2, block=(4.6, 0.8))
    m.stand("coast_nets", ship.net_pile(np.random.default_rng(384)), 21, 15.2, block=(1.2, 0.4))
    m.border()
    m.point("west", (0, 10), (0, 11), (0, 12))
    m.point("westArrive", (2, 11))
    m.point("north", (37, 0), (38, 0))
    m.point("northArrive", (38, 2))
    m.warp("west", "duneField", "eastArrive", "left")
    m.warp("north", "coastFork", "southArrive", "up")
    side_roads.desert_ground(m)
    return m


def coast_fork():
    width, height = 44, 32
    m = MapLayout("coastFork", "갈림 해안", width, height, music="field", seed=391)
    m.kinds[:, :] = "sand"
    south_road = [(21, height + 1), (21, 22), (21, 19)]
    cave_road = [(21, 19), (15, 16), (14, 11)]
    airos_road = [(21, 19), (28, 14), (34, 8.4), (width + 1, 8)]
    wall = 9.2 + 0.6 * np.sin(m.cx * 0.5)
    side_wall = 6 + 1.2 * np.sin(m.cy * 0.45)
    m.kinds[((m.cy < wall) & (m.cx < 30)) | (m.cx < side_wall) | (m.cy < 3.4)] = "cliff"
    sea = (m.cx > 36 + 1.6 * np.sin(m.cy * 0.42)) & (m.cy > 12.4)
    m.kinds[sea] = "water"
    m.line("path", south_road, 2.2)
    m.line("path", cave_road, 2.2)
    m.line("path", airos_road, 2.2)
    m.stamp("dry", (10, 22), (11, 22), (26, 24), (27, 24), (31, 19), (14, 26))
    m.stamp("rock", (11, 14), (27, 11), (30, 26), (9, 27), (33, 16))
    for x, y in ((12, 18), (16, 25), (28, 21), (31, 12), (9, 13)):
        m.tree(x, y, "palm")
    m.stamp("sign", (24, 20))
    m.border()
    m.stand("cave_mouth", side_roads.rock_arch(side_roads.ROCK, 392), 14, 9.8, block=(3.8, 0.6))
    scatter_scales(m, ((18.5, 11.2), (10.5, 11.8)), 393)
    m.point("cave", (13, 10), (14, 10))
    m.point("caveArrive", (14, 12))
    m.point("south", (20, height - 1), (21, height - 1))
    m.point("southArrive", (21, height - 3))
    m.point("east", (width - 1, 7), (width - 1, 8), (width - 1, 9))
    m.point("eastArrive", (width - 3, 8))
    m.point("sign", (24, 20), blocked=True)
    m.warp("south", "coastRoad", "northArrive", "down")
    m.warp("cave", "ironCave", "southArrive", "up")
    m.warp("east", "airosRoad", "start", "right")
    side_roads.desert_ground(m)
    return m


def iron_cave():
    width, height = 34, 26
    m = MapLayout("ironCave", "철갑 동굴 통로", width, height, theme="cave", music="castle", ambient=CAVE_AMBIENT, seed=401)
    rng = np.random.default_rng(402)
    rooms = [(9, 19, 6.5, 4.4), (20, 13, 7.5, 4.4), (27, 5.6, 5.2, 3.6)]
    lanes = [([(9, height + 2), (9, 20)], 2.0), ([(10, 18), (19, 14)], 1.8), ([(21, 12), (27, 6)], 1.8), ([(27, 6), (27, -2)], 2.0)]
    carve(m, rooms, lanes, (7, 11, 18, 23, 28), rng)
    m.stand("cave_pillar_a", cave.stalagmite(np.random.default_rng(403), 36), 5, 20.4, block=(0.6, 0.4))
    m.stand("cave_pillar_b", cave.stalagmite(np.random.default_rng(404), 30), 24.5, 14.6, block=(0.6, 0.4))
    scatter_scales(m, ((13, 20.4), (16.5, 15.6), (30, 7.4)), 405)
    m.point("south", (8, height - 1), (9, height - 1))
    m.point("southArrive", (9, height - 3))
    m.point("north", (26, 0), (27, 0))
    m.point("northArrive", (27, 2))
    m.warp("south", "coastFork", "caveArrive", "down")
    m.warp("north", "ironDepths", "southArrive", "up")
    return m


def iron_depths():
    width, height = 32, 24
    m = MapLayout("ironDepths", "철갑 동굴 깊은 곳", width, height, theme="cave", music="castle", ambient=CAVE_AMBIENT, seed=411)
    rng = np.random.default_rng(412)
    rooms = [(16, 11, 12.5, 7.4), (16, 5, 6.5, 3.4)]
    lanes = [([(16, height + 2), (16, 16)], 2.2)]
    carve(m, rooms, lanes, (8, 11, 20, 23), rng)
    gate_ground = 5.6
    m.stand("iron_gate", iron_gate(), 16, gate_ground, block=(9, 1.2))
    for k, (x, ground) in enumerate(((6.6, 8.6), (25.4, 8.6), (7.6, 14.4), (24.4, 14.4))):
        m.stand(f"depth_pillar_{k}", cave.stalagmite(np.random.default_rng(413 + k), 42), x, ground, block=(0.7, 0.4))
    scatter_scales(m, ((11.5, 9.4), (20.5, 10.4), (14, 14.6), (22, 15), (9, 12)), 417)
    m.point("dragonGate", (16, 6))
    m.point("south", (15, height - 1), (16, height - 1))
    m.point("southArrive", (16, height - 3))
    m.warp("south", "ironCave", "northArrive", "down")
    return m


MAPS = {
    "duneField": (dune_field, "northArrive", ("eastArrive",)),
    "coastRoad": (coast_road, "westArrive", ("northArrive",)),
    "coastFork": (coast_fork, "southArrive", ("caveArrive", "eastArrive")),
    "ironCave": (iron_cave, "southArrive", ("northArrive",)),
    "ironDepths": (iron_depths, "southArrive", ("dragonGate",)),
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
    web_map.reroute(project, "airosRoad", [{"point": "behind", "map": "coastFork", "target": "eastArrive", "dir": "left"}])


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), set(sys.argv[2:]))
