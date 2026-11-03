"""힐리아 서부 모래땅(westSands, 8장)을 새로 짠다(사용자 요청 2026-11-03: 다음 장들의 맵과 소품을 생성기로).

웹게임은 풀밭 마을에 모래를 깐 정도였지만, 라르고가 「여긴 사막이다. 발밑이 전부 내 모래라고」 하는 곳이라 사막 바닷가 마을로 다시 짰다:
남쪽 바다와 나무 부두(배가 닿는 곳), 젖은 모래와 물거품, 서쪽과 북쪽의 모래 언덕(바람 물결)과 사암 절벽, 마른 땅의 갈라진 흙,
북서쪽 오아시스(작은 못과 풀, 야자수 둘 사이에 걸린 라르고의 해먹), 부두에서 북쪽으로 올라가 동쪽 광장으로 가는 흙길,
돌을 깐 동쪽 광장(우물, 노점, 항아리)과 둘레의 흙벽돌 평지붕 집 넷(houses.py adobe·flat), 동쪽 끝 마을 밖 길.
지점은 웹게임 칸 그대로다(dock, largo, square, mino, villager ×3, prey ×4, beast ×4, east, eastArrive).
8장 연출이 걷는 길은 비운다: 디엘이 해먹에서 광장으로 걷는 길(R1 D2 R12 D1 R6), 미노가 북쪽으로 달아나는 길(U6).

사용: python Tools/PixelGen/west_sands.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/westSands/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/westSands_preview.png     미리보기
"""
import sys
from pathlib import Path

import numpy as np

import interior
import objects
import pixelgen as pg
import props
import terrain
from houses import ADOBE, CLOTH, POTTERY, WOOD, Canvas, Design, outline
from layout import MapLayout
from ship import lifeboat, net_pile

WIDTH, HEIGHT = 36, 24
T = pg.TILE
SANDSTONE = np.array([1.18, 1.0, 0.76])

HOUSES = [
    (Design("ws_house_nw", 5, roof="flat", walls="adobe", door="curtain", cloth="red", shutters="wood", flowers=False, chimney_at=-1,
            ladder=True, plot="sand", seed=61), 21, 6),
    (Design("ws_house_ne", 6, roof="flat", walls="adobe", door="curtain", cloth="teal", shutters="wood", flowers=False,
            chimney_at=-1, door_at=0.4, plot="sand", seed=62), 30.5, 6),
    (Design("ws_shop_sw", 5, roof="flat", walls="adobe", door="curtain", cloth="blue", shutters="", flowers=False, chimney_at=-1,
            awning="blue", door_at=0.7, plot="sand", seed=63), 22, 17),
    (Design("ws_house_se", 6, roof="flat", walls="adobe", door="curtain", cloth="green", shutters="wood", flowers=False, chimney_at=-1,
            ladder=True, door_at=0.35, plot="sand", seed=64), 30, 17),
]


# ── 바닥 ───────────────────────────────────────────────

def desert(kinds, seed):
    """
    사막 바닥: 온통 모래 위에 언덕 쪽은 바람 물결, 곳곳에 갈라진 마른 흙, 흙길, 광장 돌바닥, 오아시스 풀, 바다와 못, 부두, 사암 절벽.
    풀밭 바닥(draw_outdoor)과 달리 모래가 바탕이라 경계에 풀 테두리를 두르지 않는다.
    """
    rng = np.random.default_rng(seed)
    kinds = np.array(kinds, dtype=object)
    rows, cols = kinds.shape
    h, w = rows * T, cols * T
    yy, xx = np.mgrid[0:h, 0:w]
    wobble = pg.fractal_noise(h, w, rng, ((12, 0.7), (5, 0.3))) - 0.5
    image = terrain.sand(h, w, rng)

    # 모래 언덕의 바람 물결: 비스듬한 물결 마루는 밝고 바로 아래는 그늘
    dunes = terrain.region(kinds == "dune", wobble, 0.6)
    swell = pg.fractal_noise(h, w, rng, ((60, 1.0),)) * 9
    ripple = np.sin(xx * 0.05 + yy * 0.42 + swell)
    image[dunes & (ripple > 0.9)] = pg.SAND[4]
    image[dunes & (np.roll(ripple, -2, axis=0) > 0.9) & (ripple < 0.6)] = pg.SAND[1]
    # 마른 땅: 거북등처럼 갈라진 흙
    dry = terrain.region(kinds == "dry", wobble, 0.6)
    cracked = terrain.dirt(h, w, rng)
    image[dry] = (cracked[dry] * 0.55 + image[dry] * 0.45).astype(np.uint8)
    _, seams = terrain.cells(h, w, 9, rng)
    image[dry & seams] = pg.DIRT[1]

    # 흙길: 다진 흙이 모래로 번진다
    path = terrain.region(kinds == "path", wobble, 0.35)
    dirt = terrain.dirt(h, w, rng)
    # 흙길은 모래와 반씩 섞어 사막에서 너무 짙지 않게 한다.
    image[path] = (dirt[path] * 0.5 + image[path] * 0.5).astype(np.uint8)
    image[pg.neighbours(path) & ~path] = pg.SAND[2]
    # 광장 돌바닥: 모랫빛 판석
    plaza = terrain.region(kinds == "cobble", wobble, 0.2)
    stones = terrain.cobble(h, w, rng)
    warm = np.clip(stones * SANDSTONE, 0, 255).astype(np.uint8)
    image[plaza] = warm[plaza]
    image[plaza & pg.neighbours(~plaza)] = ADOBE[1]
    # 오아시스 풀
    oasis = terrain.region(kinds == "grass", wobble, 0.8)
    green = terrain.grass(h, w, rng)
    image[oasis] = green[oasis]
    image[pg.neighbours(oasis) & ~oasis] = pg.SAND[1]

    # 바다·못과 젖은 모래, 물거품
    wet_area = terrain.region(np.isin(kinds, ("water", "bridge")), wobble, 0.5)
    near = np.zeros_like(wet_area)
    reach = wet_area.copy()
    for _ in range(14):
        reach = reach | pg.neighbours(reach)
    near = reach & ~wet_area
    image[near] = (image[near] * 0.82).astype(np.uint8)
    terrain.water(image, wet_area, rng)
    foam = wet_area & pg.neighbours(~wet_area)
    image[foam] = pg.WATER[6]
    image[pg.neighbours(foam) & wet_area & (rng.random((h, w)) < 0.5)] = pg.WATER[5]
    terrain.bridge(image, terrain.cell_field(kinds == "bridge") > 0.5, rng)

    # 사암 절벽: 바위 절벽을 그린 뒤 모랫빛으로 물들인다
    cliffs = terrain.region(kinds == "cliff", wobble, 0.25)
    stone = image.copy()
    terrain.cliff(stone, cliffs, rng)
    tinted = np.clip(stone * SANDSTONE, 0, 255).astype(np.uint8)
    changed = (stone != image).any(axis=2)
    # 절벽 윗면의 이끼(풀빛)는 사막에 없으니 모래로 바꾼다.
    mossy = changed & (stone[..., 1].astype(int) > stone[..., 0].astype(int) + 6)
    tinted[mossy] = pg.SAND[2]
    image[changed] = tinted[changed]

    # 바위
    for row, column in zip(*np.nonzero(kinds == "rock")):
        sprite, pivot = props.rock(np.random.default_rng(seed + row * 131 + column))
        sprite[..., :3] = np.clip(sprite[..., :3] * SANDSTONE, 0, 255).astype(np.uint8)
        foot_x, foot_y = column * T + T / 2, row * T + T - 2
        under = props.shadow(int(sprite.shape[1] * 0.85), 7)
        pg.stamp(image, under, foot_x + 2, foot_y + 1, (under.shape[1] / 2, under.shape[0] / 2))
        pg.stamp(image, sprite, foot_x, foot_y, pivot)
    return image


# ── 소품 ───────────────────────────────────────────────

def hammock(rng):
    """야자수 두 그루 사이에 매단 줄무늬 해먹(라르고의 낮잠 자리). 너비는 네 칸."""
    w, h = 4 * T, 40
    c = Canvas(w, h)
    for step in range(w):
        t = step / (w - 1)
        sag = int(round(18 * np.sin(t * np.pi)))
        top = 8 + sag
        thick = 2 if t < 0.08 or t > 0.92 else 7
        color = CLOTH["red"][1] if (step // 6) % 2 else pg.hexes("#e8d8b8")[0]
        c.paint(c.rect(step, top, step + 1, top + thick), color if thick > 2 else WOOD[2])
        c.paint(c.rect(step, top + thick - 1, step + 1, top + thick), CLOTH["red"][0] if thick > 2 else WOOD[1])
    return outline(c.image), (w // 2, h - 2)


def market_stall(rng):
    """노점: 기둥 넷에 줄무늬 차양, 판자 탁자 위에 과일 바구니와 항아리."""
    w, h = 72, 70
    c = Canvas(w, h)
    ground = h - 2
    for x in (6, w - 8):
        c.paint(c.rect(x, 18, x + 3, ground), WOOD[2])
    roof = c.polygon([(0, 22), (w, 22), (w - 6, 8), (6, 8)])
    c.paint(roof, CLOTH["red"][1])
    c.paint(roof & ((c.xx // 8) % 2 == 0), pg.hexes("#efe2c3")[0])
    for x in range(2, w - 2, 8):
        c.paint(c.ellipse(x + 4, 23, 4, 3) & (c.yy > 22), CLOTH["red"][1] if (x // 8) % 2 else pg.hexes("#efe2c3")[0])
    table = c.rect(3, 40, w - 3, 48)
    c.paint(table, WOOD[3])
    c.paint(c.rect(3, 40, w - 3, 42), WOOD[4])
    c.paint(c.rect(5, 48, w - 5, ground), WOOD[1])
    for k, fx in enumerate((14, 28, 44, 58)):
        basket = c.ellipse(fx, 38, 6, 4) & (c.yy > 35)
        c.paint(basket, WOOD[2])
        fruit = pg.hexes("#e0a030", "#c03a2a", "#6aa040", "#e07a30")[k]
        for dx in (-3, 0, 3):
            c.paint(c.ellipse(fx + dx, 34, 2.4, 2.4), fruit)
    return outline(c.image), (w // 2, ground)


def pots(rng, count=3):
    """흙 항아리 몇 개를 모아 둔 것."""
    w, h = 18 + count * 12, 30
    c = Canvas(w, h)
    for k in range(count):
        px = 10 + k * 12
        tall = 9 + (k % 2) * 4
        body = c.ellipse(px, h - 2 - tall * 0.6, 6, tall * 0.6)
        c.paint(body, POTTERY[2])
        c.paint(body & (c.xx < px - 1), POTTERY[3])
        c.paint(c.rect(px - 3, h - 2 - tall * 1.2 - 2, px + 3, h - 2 - tall * 1.1), POTTERY[1])
    return outline(c.image), (w // 2, h - 2)


def drying_rack(rng):
    """생선을 너는 나무 시렁(부두 옆 어부의 것)."""
    w, h = 64, 48
    c = Canvas(w, h)
    ground = h - 2
    for x in (4, w - 7):
        c.paint(c.rect(x, 10, x + 3, ground), WOOD[2])
    c.paint(c.rect(2, 10, w - 2, 13), WOOD[3])
    for k in range(6):
        fx = 10 + k * 9
        fish = c.ellipse(fx, 22, 2.5, 7)
        c.paint(fish, pg.hexes("#9aa4b0")[0])
        c.paint(fish & (c.xx < fx), pg.hexes("#c8d0d8")[0])
        c.line(fx, 13, fx, 15, WOOD[1])
    return outline(c.image), (w // 2, ground)


# ── 맵 ────────────────────────────────────────────────

def build():
    m = MapLayout("westSands", "힐리아 서부", WIDTH, HEIGHT, music="field", seed=151)
    rng = np.random.default_rng(152)
    m.kinds[:, :] = "sand"

    # 남쪽 바다(굽은 바닷가)와 부두
    shore = 20.6 + 0.5 * np.sin(m.cx * 0.45) + 0.3 * np.sin(m.cx * 1.3 + 1)
    m.kinds[m.cy > shore] = "water"
    m.rect("bridge", 13, 21, 16, 24)
    m.rect("sand", 13, 20, 16, 21)
    # 서쪽·북쪽 언덕과 사암 절벽(미노가 달아나는 북쪽 고갯길 x 24~28만 트인다)
    m.kinds[(m.cx < 9) & (m.cy < 3)] = "dune"
    m.kinds[(m.cx > 9) & (m.cx < 20) & (m.cy < 2.5)] = "dune"
    m.rect("cliff", 0, 0, 2, 20)
    m.rect("cliff", 2, 0, 9, 1)
    m.rect("cliff", 9, 0, 24, 1)
    m.rect("cliff", 29, 0, 36, 1)
    m.kinds[(m.cx > 1) & (m.cx < 12) & (m.cy > 11) & (m.cy < 19)] = "dune"
    m.stamp("dry", (9, 6), (10, 6), (11, 7), (12, 7), (16, 4), (17, 4), (17, 5), (9, 15), (10, 15), (10, 16), (3, 17), (4, 17))
    # 오아시스: 작은 못과 풀
    m.kinds[np.hypot((m.cx - 2.9) / 1.6, (m.cy - 7.6) / 1.4) < 1] = "water"
    ring = (np.hypot((m.cx - 3.2) / 3.2, (m.cy - 7.0) / 3.0) < 1) & (m.kinds != "water") & (m.kinds != "cliff")
    m.kinds[ring] = "grass"
    # 흙길: 부두에서 북쪽으로, 큰길을 따라 동쪽 광장으로, 광장에서 동쪽 마을 밖으로
    m.line("path", [(14.5, 20.5), (14.4, 15), (14.0, 10.0)], 2.0)
    m.line("path", [(5.5, 9.6), (12, 9.6), (20, 9.8)], 2.0)
    m.line("path", [(33, 9.5), (36, 9.5)], 2.2)
    m.rect("cobble", 20, 7, 34, 14)
    # 동쪽 끝: 사암 절벽(마을 밖 길만 트인다)
    m.rect("cliff", 35, 0, 36, 8)
    m.rect("cliff", 35, 11, 36, 20)

    # 광장 둘레 흙벽돌 집
    for design, cx, gy in HOUSES:
        m.house(design, cx, gy)
    m.thing("well", 33, 7)
    m.stand("stall", market_stall(rng), 32.6, 13.4, block=(2.2, 0.8))
    m.stand("pots_plaza", pots(rng, 3), 26.8, 13.6, block=(1.2, 0.4))
    m.stand("pots_house", pots(rng, 2), 18.6, 6.2, block=(0.8, 0.4))
    for kind, x, y in (("barrel", 34, 13), ("crate", 19, 13)):
        m.thing(kind, x, y)

    # 오아시스의 야자수와 해먹(라르고는 해먹 위 6, 5에 눕는다)
    m.tree(4, 5, "palm")
    m.tree(8, 5, "palm")
    m.stand("hammock", hammock(rng), 6.5, 5.6)
    for x, y in ((1, 11), (3, 13), (18, 19), (11, 19)):
        m.tree(x, y, "palm")
    # 부두 옆 어부의 살림: 모래에 끌어 올린 배, 그물, 생선 시렁
    m.stand("boat", lifeboat(rng), 18.6, 20.4, block=(3.6, 0.8))
    m.stand("nets", net_pile(rng), 11.6, 20.2, block=(1.2, 0.4))
    m.stand("rack", drying_rack(rng), 9.0, 19.2, block=(2, 0.4))
    # 사막의 선인장과 바위
    for x, y in ((13, 3), (16, 7), (5, 14), (9, 12), (3, 19), (16, 16), (12, 17)):
        m.tree(x, y, "cactus")
    m.stamp("rock", (15, 6), (6, 15), (2, 15), (17, 13))
    m.border()

    # ── 8장 지점(웹게임 칸) ──
    m.point("dock", (14, 20))
    m.point("largo", (6, 5))
    m.point("square", (26, 10))
    m.point("mino", (26, 7))
    m.point("villager", (30, 8), (21, 11), (29, 11))
    m.point("prey", (10, 3), (25, 6), (22, 12), (25, 17))
    m.point("beast", (11, 4), (5, 11), (11, 13), (7, 17))
    m.point("eastArrive", (34, 9))
    m.point("east", (35, 9))
    m.warp("east", "fairyGarden", "arrive", "right")
    m.paint_ground = lambda: desert(m.kinds.tolist(), m.seed)
    return m


def walk_cells(start, route):
    x, y = start
    cells = []
    for move in route.split():
        dx, dy = {"R": (1, 0), "L": (-1, 0), "U": (0, -1), "D": (0, 1)}[move[0]]
        for _ in range(int(move[1:])):
            x, y = x + dx, y + dy
            cells.append((x, y))
    return cells


def main(project):
    m = build()
    for goal in ("largo", "square", "mino", "villager", "prey", "beast", "east"):
        for cell in m.points[goal]:
            if not m.reachable(m.points["dock"][0], [cell]):
                print(f"부두에서 {goal} {cell}에 닿지 못한다")
    # 8장 연출의 걸음이 막힌 칸을 지나지 않는지(디엘: 해먹 아래에서 광장으로, 미노: 북쪽으로)
    for name, start, route in (("디엘", (6, 7), "R1 D2 R12 D1 R6"), ("미노", (26, 7), "U6")):
        if not m.route_is_clear(start, route):
            print(f"{name}의 걸음이 막힌다: {[c for c in walk_cells(start, route)]}")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
