"""거대고목의 5층 가지 회랑(giantTree)과 꼭대기 잎 덮개(treeCrown)를 새로 짠다(사용자 요청 2026-11-03: 10장 맵을 생성기로,
「뱀성처럼 여러 층으로」. 1~4층 안쪽 방은 tree_floors.py).

  5층 가지 회랑(giantTree): 줄기 밖으로 나와 하늘 아래 가지를 타고 오르는 층. 위에서 비스듬히 본 거대한 줄기의 앞면(세로 껍질 골,
     이끼, 선반 버섯, 빛나는 옹이 구멍, 요정 창)을 굵은 가지 위 나무 길(n)·밧줄 판자 다리(N)·사다리(^)로 감아 오른다.
     가지 가장자리에는 잎 덩이가 붙어 있다(b, 막힘). 바깥은 높이가 느껴지게 노을 하늘과 구름, 아래쪽은 안개 낀 먼 숲 우듬지다.
     맨 아래 사다리가 4층 서고로(down), 맨 위 사다리가 꼭대기로(up) 이어진다. 길·지점은 웹게임 giantTree의 가운데 토막과 같다.
  꼭대기 잎 덮개(treeCrown): 잎 덩이로 둘러싼 넓은 이끼 덮개(촌장·케이트, 11장 나리온 정거장), 꽃과 빛나는 버섯,
     가운데 남쪽의 사다리 구멍(down)으로 가지 회랑에 내려간다.

사용: python Tools/PixelGen/giant_tree.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/giantTree/…, treeCrown/…   맵 데이터와 그림(layout.py)
"""
import sys
from pathlib import Path

import numpy as np

import fairy_props as fp
import hidden_garden
import pixelgen as pg
import terrain
from houses import Canvas, WOOD
from layout import LOGICAL, MapLayout

T = pg.TILE
# 5층 가지 회랑 칸(웹게임 giantTree 6~24줄): u 꼭대기로 오르는 사다리, d 4층으로 내려가는 사다리.
ROWS = [
    "obbbbbbbbbbbubbbbbbbbbbo",
    "ooooobbb####^###oooooooo",
    "ooobbnnnNNNNN###oooooooo",
    "ooobbnno########oooooooo",
    "ooobbnmb########bbbbbooo",
    "ooobbnno########nnKnbooo",
    "ooobbnnb########oooooooo",
    "ooobbnno########oooobooo",
    "ooooonnnNNNNNNNNnnWnbooo",
    "oooobbbb########bbnnbooo",
    "oooooooo########oonnbooo",
    "oooooooo########oonmbooo",
    "oooooooo########oonnbooo",
    "oooooooo########oonnbooo",
    "oooooooo####NNNNnnnnbooo",
    "oooooooo####^###bbbbbooo",
    "oooooo######^#####oooooo",
    "oooooo######^#####oooooo",
    "oooooo######d#####oooooo",
]
HEIGHT, WIDTH = len(ROWS), len(ROWS[0])
CROWN_W, CROWN_H = 24, 14
CROWN_CENTER, CROWN_RADII = (12.0, 6.6), (10.6, 5.6)
SKY = pg.hexes("#1c1838", "#2c2450", "#4a3266", "#6e3e70", "#9a4e6a", "#c86a5e", "#e8925a", "#f6b870")
CLOUD = pg.hexes("#7a5a8a", "#b47a8a", "#e8a88a", "#f8d0a0")
HAZE = pg.hexes("#3a3a5a", "#4a4a66", "#5e5a72", "#76687a")
LEAF = pg.LEAF
PLANK = pg.hexes("#3a2616", "#5a3c24", "#7a5434", "#966c46", "#b08458")
ROPE = pg.hexes("#5a4628", "#8a7044", "#b89a64")
CURSE = pg.hexes("#120a1e", "#2a1240", "#5a2a8a")


def cells_of(chars):
    return np.array([[ch in chars for ch in row] for row in ROWS])


def tile_field(height, width):
    """칸 가운데 좌표(칸 단위) 격자."""
    yy, xx = np.mgrid[0:height, 0:width]
    return xx + 0.5, yy + 0.5


def sky(rng, h, w, haze=True):
    """노을 하늘: 위는 짙은 남보라, 아래로 갈수록 붉고 노랗다. 결 따라 흐르는 구름 띠, 아래쪽에는 안개 낀 먼 숲 우듬지."""
    yy, xx = np.mgrid[0:h, 0:w]
    v = yy / h
    light = 0.08 + v * 0.95 + (pg.noise(h, w, 40, rng) - 0.5) * 0.08
    image = pg.quantize(np.clip(light, 0, 0.999), SKY, dither=0.12)
    band = pg.fractal_noise(h, w, rng, ((90, 0.6), (30, 0.3), (9, 0.1)))
    stripe = np.sin(yy / 23.0 + band * 7)
    cloud = (stripe > 0.72) & (band > 0.5)
    shade = np.clip(0.35 + v * 0.6 + (stripe - 0.72) * 1.2, 0, 0.999)
    image[cloud] = pg.quantize(shade, CLOUD, dither=0.1)[cloud]
    # 먼 숲 우듬지: 아래 셋째 줄쯤에서 둥근 수관이 이어진 안개빛 띠(멀수록 흐리다).
    for layer, (top, color) in enumerate(((0.62, HAZE[0]), (0.72, HAZE[1]), (0.8, HAZE[2])) if haze else ()):
        crowns = top * h + np.sin(xx / (11.0 + layer * 3) + layer) * 6 + (pg.noise(1, w, 14, rng)[0] - 0.5) * 18
        image[yy >= crowns] = color
    return image


def bark(rng, mask):
    """줄기 앞면: fairy_props의 껍질 결에 이끼, 빛나는 옹이 구멍, 선반 버섯, 요정 창."""
    h, w = mask.shape
    c = Canvas(w, h)
    cols = np.nonzero(mask.any(axis=0))[0]
    fp.bark_texture(c, mask, rng, cols.min(), cols.max())
    fp.moss_patches(c, mask, rng, amount=0.64)
    for x, y, r in ((9.3, 4.5, 4), (14.2, 11.2, 5), (10.6, 16.6, 4), (13.8, 3.2, 3.5)):
        hole = c.ellipse(x * T, y * T, r + 2, r * 1.4 + 2) & mask
        c.paint(hole, pg.hexes("#2a1e14")[0])
        c.paint(c.ellipse(x * T, y * T + 1, r, r * 1.4) & mask, fp.GLOW[1])
        c.paint(c.ellipse(x * T - 1, y * T, r * 0.5, r * 0.8) & mask, fp.GLOW[3])
    for x, y, wd in ((8.6, 6.6, 16), (15.2, 5.4, 18), (9.0, 12.8, 20), (15.0, 16.4, 16), (11.0, 10.0, 14)):
        fp.shelf_fungus(c, x * T, y * T, wd)
    for x, y in ((14.0, 2.0), (10.0, 9.2), (15.0, 13.0)):
        fp.fairy_window(c, x * T, y * T, 5, rng)
    return c.image


def branch_paths(rng, walk):
    """가지 위 나무 길: 길이 방향 결이 난 밝은 껍질 윗면, 남쪽 가장자리에 가지 두께(그늘), 군데군데 이끼."""
    h, w = walk.shape
    c = Canvas(w, h)
    grain = pg.noise(h, w, 6, rng)
    level = 0.55 + 0.12 * np.sin(c.xx / 3.0 + grain * 4) + (grain - 0.5) * 0.2
    c.paint(walk, pg.quantize(np.clip(level, 0, 0.999), fp.BARK[2:], dither=0.1))
    below = walk & ~np.roll(walk, -5, axis=0)
    c.paint(below, fp.BARK[1])
    c.paint(walk & ~np.roll(walk, -1, axis=0), fp.BARK[0])
    moss = walk & (pg.fractal_noise(h, w, rng, ((10, 0.7), (4, 0.3))) > 0.7) & ~below
    c.paint(moss, fp.MOSS[2])
    return c.image


def bridges(rng, deck):
    """줄기 앞을 가로지르는 판자 다리: 걷는 방향을 가로지른 판자, 남북 가장자리 밧줄과 기둥, 줄기에 드리운 그림자."""
    h, w = deck.shape
    c = Canvas(w, h)
    shadow = np.roll(deck, 7, axis=0) & ~deck
    c.paint(shadow, fp.BARK[0])
    plank = np.floor(c.xx / 7)
    level = 0.5 + ((plank * 37) % 5) / 14 - 0.12 * (np.mod(c.xx, 7) < 1)
    c.paint(deck, pg.quantize(np.clip(level, 0, 0.999), PLANK, dither=0.06))
    edge_n = deck & ~np.roll(deck, 2, axis=0)
    edge_s = deck & ~np.roll(deck, -2, axis=0)
    c.paint(edge_n | edge_s, ROPE[1])
    c.paint((edge_n | edge_s) & (np.mod(c.xx, 6) < 2), ROPE[2])
    posts = deck & (np.mod(c.xx, 32) < 3) & (edge_n | edge_s)
    c.paint(posts, PLANK[0])
    return c.image


def ladders(rng, rungs):
    """줄기에 기댄 나무 사다리: 양옆 기둥과 가로대, 기둥을 감은 덩굴."""
    h, w = rungs.shape
    c = Canvas(w, h)
    cols = (c.xx % T)
    rails = rungs & ((np.abs(cols - 8) < 2) | (np.abs(cols - 23) < 2))
    steps = rungs & (np.mod(c.yy, 8) < 3) & (cols > 8) & (cols < 23)
    c.paint(c.rect(0, 0, w, h) & rungs & ~rails & ~steps, fp.BARK[1])
    c.paint(steps, PLANK[3])
    c.paint(steps & (np.mod(c.yy, 8) == 0), PLANK[4])
    c.paint(rails, PLANK[2])
    c.paint(rails & (np.mod(c.yy, 9) < 2), fp.MOSS[2])
    return c.image


def foliage(rng, leaves):
    """잎 덩이: 둥글게 부푼 잎 다발이 왼쪽 위에서 빛을 받는다. 가장자리는 울퉁불퉁하다."""
    h, w = leaves.shape
    c = Canvas(w, h)
    blob = pg.fractal_noise(h, w, rng, ((11, 0.6), (4, 0.4)))
    inside = terrain.distance_inside(leaves, 10)
    level = 0.25 + inside / 22 + (blob - 0.5) * 0.6
    c.paint(leaves, pg.quantize(np.clip(level, 0, 0.999), LEAF[1:], dither=0.15))
    sparkle = leaves & (blob > 0.72) & (inside > 3) & (pg.bayer(h, w) > 0.6)
    c.paint(sparkle, LEAF[6])
    c.paint(leaves & (inside <= 1), LEAF[0])
    return c.image


def mossy_floor(rng, floor, canopy):
    """덮개 위 이끼 바닥(떨어진 잎·꽃)과 밑동 둘레 숲 바닥."""
    h, w = floor.shape
    image = terrain.grass(h, w, rng)
    litter = rng.random((h, w))
    image[canopy & (litter < 0.03)] = pg.LEAF[5]
    image[canopy & (litter > 0.985)] = pg.FLOWERS[0]
    return image


def ladder_hole(c, x, y):
    """잎 덮개 바닥의 사다리 구멍: 껍질 테를 두른 어두운 구멍으로 사다리가 내려간다(칸 x, y)."""
    cx, cy = (x + 0.5) * T, (y + 0.5) * T
    c.paint(c.ellipse(cx, cy, 24, 16), fp.BARK[4])
    c.paint(c.ellipse(cx, cy, 24, 16) & (c.yy < cy - 8), fp.BARK[5])
    hole = c.ellipse(cx, cy + 1, 18, 12)
    c.paint(hole, fp.BARK[0])
    for k in range(3):
        c.paint(c.rect(cx - 9, cy - 6 + k * 6, cx + 9, cy - 4 + k * 6) & hole, PLANK[2 - k // 2])
    c.paint(c.rect(cx - 10, cy - 10, cx - 7, cy + 12) & hole, PLANK[3])
    c.paint(c.rect(cx + 7, cy - 10, cx + 10, cy + 12) & hole, PLANK[3])


def branches():
    m = MapLayout("giantTree", "거대고목 5층 · 가지 회랑", WIDTH, HEIGHT, theme="cave", music="field", background="dusk", seed=221)
    rng = np.random.default_rng(222)
    h, w = HEIGHT * T, WIDTH * T
    wobble = pg.fractal_noise(h, w, rng, ((12, 0.7), (5, 0.3))) - 0.5
    trunk_cells = cells_of("#^Nud")
    trunk_cells[0] = False
    walk_cells = cells_of("nmKW")
    bridge_cells = cells_of("N")
    ladder_cells = cells_of("^ud")
    leaf_cells = cells_of("b")

    def paint():
        image = sky(np.random.default_rng(223), h, w)
        trunk = terrain.region(trunk_cells, wobble, 0.3)
        # 줄기는 맵 아래 끝 밖으로 이어진다(아래 끝 줄은 흔들지 않는다).
        trunk[-T:] |= terrain.cell_field(trunk_cells)[-T:] > 0.5
        layers = [(trunk, bark(np.random.default_rng(225), trunk))]
        walk = terrain.region(walk_cells, wobble, 0.4)
        layers.append((walk, branch_paths(np.random.default_rng(226), walk)))
        leaves = terrain.region(leaf_cells, wobble, 0.9) & ~walk
        layers.insert(0, (leaves, foliage(np.random.default_rng(227), leaves)))
        deck = terrain.cell_field(bridge_cells) > 0.5
        layers.append((deck | np.roll(deck, 7, axis=0) & trunk, bridges(np.random.default_rng(228), deck)))
        rungs = terrain.cell_field(ladder_cells) > 0.5
        layers.append((rungs, ladders(np.random.default_rng(229), rungs)))
        for mask, layer in layers:
            shown = mask & (layer[..., 3] > 0)
            image[shown] = layer[shown, :3]
        return image

    marks = {"K": "kate", "W": "warpSpot", "u": "up", "d": "down"}
    slimes = []
    for y, row in enumerate(ROWS):
        for x, ch in enumerate(row):
            if ch in marks:
                m.point(marks[ch], (x, y))
            if ch == "m":
                slimes.append((x, y))
    m.point("slime", *slimes)
    m.paint_ground = paint
    m.block_where(~(walk_cells | bridge_cells | ladder_cells))
    for i, (x, y) in enumerate(((4.6, 3.0), (20.6, 6.0), (20.6, 15.0), (11.4, 1.0))):
        m.stand(f"lantern_{i}", fp.fairy_lantern(np.random.default_rng(250 + i)), x, y)
        m.light((x + 0.3) * LOGICAL, y * LOGICAL - 26, 30, "#7ff0dc")
    m.warp("up", "treeCrown", "down", "up")
    m.warp("down", "treeLibrary", "up", "down")
    return m


def crown():
    m = MapLayout("treeCrown", "거대고목 꼭대기 · 잎 덮개", CROWN_W, CROWN_H, theme="cave", music="field", background="dusk", seed=231)
    rng = np.random.default_rng(232)
    h, w = CROWN_H * T, CROWN_W * T
    tx, ty = tile_field(CROWN_H, CROWN_W)
    floor_cells = ((tx - CROWN_CENTER[0]) / CROWN_RADII[0]) ** 2 + ((ty - CROWN_CENTER[1]) / CROWN_RADII[1]) ** 2 <= 1
    rim = ((tx - CROWN_CENTER[0]) / (CROWN_RADII[0] + 2.4)) ** 2 + ((ty - CROWN_CENTER[1]) / (CROWN_RADII[1] + 2.0)) ** 2 <= 1
    leaf_cells = rim & ~floor_cells
    wobble = pg.fractal_noise(h, w, rng, ((12, 0.7), (5, 0.3))) - 0.5

    def paint():
        image = sky(np.random.default_rng(233), h, w, haze=False)
        leaves = terrain.region(leaf_cells | floor_cells, wobble, 0.9)
        image[leaves] = foliage(np.random.default_rng(234), leaves)[leaves, :3]
        floor = terrain.region(floor_cells, wobble, 0.6)
        image[floor] = mossy_floor(np.random.default_rng(235), floor, np.ones((h, w), dtype=bool))[floor]
        c = Canvas(w, h)
        ladder_hole(c, 12, 12)
        drawn = c.image[..., 3] > 0
        image[drawn] = c.image[drawn, :3]
        return image

    m.paint_ground = paint
    m.block_where(~floor_cells)
    m.keep_open(12, 12, 13, 13)
    for i, (x, y) in enumerate(((4.4, 5.6), (19.6, 6.4), (7.2, 10.2), (16.6, 2.8), (9.0, 2.6), (18.2, 10.4))):
        m.stand(f"shrooms_{i}", fp.glow_shrooms(np.random.default_rng(240 + i)), x, y)
        m.light(x * LOGICAL, y * LOGICAL - 4, 16, "#7ff0dc")
    for i, (x, y) in enumerate(((4.4, 8.6), (20.0, 8.6))):
        m.stand(f"lantern_{i}", fp.fairy_lantern(np.random.default_rng(260 + i)), x, y)
        m.light((x + 0.3) * LOGICAL, y * LOGICAL - 26, 30, "#7ff0dc")
    for i, (x, y, cols, rows) in enumerate(((3.6, 4.2, 3, 2), (17.2, 3.4, 3, 2), (6.0, 8.6, 3, 2), (15.6, 8.8, 3, 2))):
        m.decal(f"flower_bed_{i}", hidden_garden.flower_bed(cols, rows, np.random.default_rng(270 + i)), x * T, y * T)
    m.point("elder", (12, 3))
    m.point("top", (12, 7))
    m.point("kateTop", (15, 6))
    m.point("down", (12, 12))
    m.warp("down", "giantTree", "up", "down")
    return m


def main(project):
    for build in (branches, crown):
        m = build()
        origin = m.points["down"][0]
        for name, cells in m.points.items():
            for cell in cells:
                if name != "kate" and not m.reachable(origin, [cell]):
                    print(f"{m.id}: {name} {cell}에 닿지 못한다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
