"""거대고목의 등반로(giantTree)와 꼭대기 잎 덮개(treeCrown)를 새로 짠다(10장 맵. 뱀성처럼 여러 층이다.
1~4층 안쪽 방은 tree_floors.py).

  등반로(giantTree, 나무 기둥을 보여 주지 않고 거대한 나무의 등산 경로처럼, 뱀성처럼 단면으로 보인다):
     줄기를 세로로 보여 주지 않고, 뱀성 앞 산처럼 양 끝이 휘어 오르는 나무껍질 절벽 세 단을 지그재그로 오른다.
     단 위는 이끼 낀 넓은 턱이고, 단마다 뿌리 계단이 있다. 오른쪽 가지 끝 전망대에 케이트가 나타나고, 왼쪽 가지 끝에 등불이 있다.
     가장자리는 잎 덩이가 막고 그 너머는 노을 하늘과 구름이다. 맨 아래 사다리가 서고로(down), 맨 위 사다리가 꼭대기로(up) 이어진다.
  꼭대기 잎 덮개(treeCrown): 잎 덩이로 둘러싼 넓은 이끼 덮개(촌장·케이트, 11장 나리온 정거장), 꽃과 빛나는 버섯.
     가운데 남쪽의 사다리 구멍(down)으로 등반로에 내려간다.

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
WIDTH, HEIGHT = 44, 46
CENTER = WIDTH / 2
TERRACES = ((12.5, 27), (24.0, 15), (35.0, 29))     # 단 절벽의 윗줄(가운데 기준)과 계단 가운데 칸, 위 단부터
WALL_HEIGHT = 3.2                                    # 절벽 높이(칸)
WALL_LIFT = 4.0                                      # 절벽 양 끝이 휘어 오르는 칸 수
STAIR_HALF = 1.6
LADDER_X = 22
CANOPY_ROWS = 3.5                                    # 맨 위 잎 천장
# 오르는 길: 아래에서 위로, 단마다 한 번씩 꺾인다. 뒤의 둘은 양옆 가지로 나가는 샛길이다.
TRAILS = (
    [(22, 44), (24, 42), (28, 40), (29, 38)],
    [(29, 34), (24, 31), (16, 29), (15, 27)],
    [(15, 23), (21, 20), (26, 18), (27, 16)],
    [(27, 12), (24, 9), (22, 6), (22, CANOPY_ROWS)],
    [(26, 18), (33, 19), (37, 18)],
    [(18, 30), (8, 31), (5, 30)],
)
LOOKOUT = (37, 16, 4.2)                              # 오른쪽 가지 끝 전망대(가운데 칸, 반지름)
LEFT_END = (5, 29, 3.4)                              # 왼쪽 가지 끝
ARMS = (([(30, 21), (36, 18)], 2.4), ([(12, 32), (5, 30)], 2.4))
LANTERNS = ((26.6, 41.0), (31.4, 33.8), (12.6, 28.2), (17.4, 23.0), (24.6, 17.0), (29.4, 12.0), (37.0, 13.2), (5.0, 27.0))
GLOW_PATCHES = ((9, 33), (33, 30), (12, 20), (31, 21), (17, 8), (30, 8))
CROWN_W, CROWN_H = 24, 14
CROWN_CENTER, CROWN_RADII = (12.0, 6.6), (10.6, 5.6)
SKY = pg.hexes("#1c1838", "#2c2450", "#4a3266", "#6e3e70", "#9a4e6a", "#c86a5e", "#e8925a", "#f6b870")
CLOUD = pg.hexes("#7a5a8a", "#b47a8a", "#e8a88a", "#f8d0a0")
HAZE = pg.hexes("#3a3a5a", "#4a4a66", "#5e5a72", "#76687a")
LEAF = pg.LEAF
PLANK = pg.hexes("#3a2616", "#5a3c24", "#7a5434", "#966c46", "#b08458")
ROPE = pg.hexes("#5a4628", "#8a7044", "#b89a64")
CURSE = pg.hexes("#120a1e", "#2a1240", "#5a2a8a")


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


def terrace_top(x, base):
    """단 절벽의 윗줄. 뱀성 앞 산처럼 양 끝이 휘어 오른다."""
    return base - WALL_LIFT * ((x - CENTER) / CENTER) ** 2


def bark_wall(rng, mask):
    """단 절벽: 세로 결의 나무껍질에 이끼, 위 끝의 밝은 턱, 아래로 갈수록 짙은 그늘, 선반 버섯과 빛나는 옹이 구멍, 요정 창."""
    h, w = mask.shape
    c = Canvas(w, h)
    cols = np.nonzero(mask.any(axis=0))[0]
    fp.bark_texture(c, mask, rng, cols.min(), cols.max())
    fp.moss_patches(c, mask, rng, amount=0.7)
    for base, stair in TERRACES:
        for x in range(4, WIDTH - 3, 5):
            if abs(x - stair) < 4:
                continue
            y = (terrace_top(x, base) + WALL_HEIGHT * rng.uniform(0.35, 0.7)) * T
            kind = rng.integers(0, 4)
            if kind == 0:
                fp.shelf_fungus(c, x * T, y, 16)
            elif kind == 1:
                r = 4
                hole = c.ellipse(x * T, y, r + 2, r * 1.4 + 2) & mask
                c.paint(hole, pg.hexes("#2a1e14")[0])
                c.paint(c.ellipse(x * T, y + 1, r, r * 1.4) & mask, fp.GLOW[1])
                c.paint(c.ellipse(x * T - 1, y, r * 0.5, r * 0.8) & mask, fp.GLOW[3])
            elif kind == 2:
                fp.fairy_window(c, x * T, y, 5, rng)
    image = c.image
    lip = mask & ~np.roll(mask, 2, axis=0)
    image[lip, :3] = fp.BARK[5]
    foot = mask & ~np.roll(mask, -7, axis=0)
    image[foot, :3] = (image[foot, :3] * 0.62).astype(np.uint8)
    return image


def mossy_ledge(rng, mask):
    """단 위의 턱: 짙은 이끼에 나무껍질이 드문드문 드러난다."""
    h, w = mask.shape
    image = terrain.grass(h, w, rng)
    image = (image * np.array([0.82, 0.92, 0.74])).astype(np.uint8)
    patches = pg.fractal_noise(h, w, rng, ((40, 0.7), (14, 0.3))) > 0.68
    grain = pg.noise(h, w, 6, rng)
    level = 0.5 + 0.12 * np.sin(np.arange(w)[None, :] / 3.0 + grain * 4)
    shown = pg.quantize(np.clip(level, 0, 0.999), fp.BARK[1:5], dither=0.1)
    image[patches] = shown[patches]
    litter = rng.random((h, w))
    image[(litter > 0.992) & ~patches] = pg.FLOWERS[0]
    return image


def root_stairs(mask):
    """절벽을 끊고 오르는 뿌리 계단: 층층이 박은 두꺼운 판자, 양옆을 감싼 뿌리."""
    h, w = mask.shape
    c = Canvas(w, h)
    step = np.mod(c.yy, 9)
    c.paint(mask, PLANK[2])
    c.paint(mask & (step < 2), PLANK[4])
    c.paint(mask & (step >= 7), PLANK[0])
    roots = mask & ~(np.roll(mask, 7, axis=1) & np.roll(mask, -7, axis=1))
    c.paint(roots, fp.BARK[2])
    c.paint(roots & (mask & ~(np.roll(mask, 3, axis=1) & np.roll(mask, -3, axis=1))), fp.BARK[4])
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
    m = MapLayout("giantTree", "거대고목 등반로", WIDTH, HEIGHT, theme="cave", music="field", background="dusk", seed=221)
    rng = np.random.default_rng(222)
    h, w = HEIGHT * T, WIDTH * T
    wobble = pg.fractal_noise(h, w, rng, ((12, 0.7), (5, 0.3))) - 0.5

    # ── 칸 나누기: 나무 몸체, 절벽, 계단, 사다리, 잎 ──
    half_width = 9 + m.cy * 0.28 + 1.2 * np.sin(m.cy * 0.4)
    body = (np.abs(m.cx - CENTER) < half_width) & (m.cy >= CANOPY_ROWS)
    for points, reach in ARMS:
        body |= m.near_line(points, reach)
    for cx, cy, radius in (LOOKOUT, LEFT_END):
        body |= np.hypot(m.cx - cx, m.cy - cy) <= radius
    walls = np.zeros_like(body)
    stairs = np.zeros_like(body)
    for base, stair in TERRACES:
        top = terrace_top(m.cx, base)
        band = (m.cy >= top) & (m.cy < top + WALL_HEIGHT)
        gap = np.abs(m.cx + 0.0 - stair) < STAIR_HALF
        walls |= band & ~gap
        stairs |= (m.cy >= top - 1) & (m.cy < top + WALL_HEIGHT + 1) & gap
    walls &= body
    ladders_at = np.zeros_like(body)
    ladders_at[:int(CANOPY_ROWS) + 1, LADDER_X] = True
    ladders_at[-2:, LADDER_X] = True
    ring = body.copy()
    for _ in range(2):
        ring = ring | pg.neighbours(ring)
    leaf_cells = (ring & ~body) | (m.cy < CANOPY_ROWS)
    leaf_cells &= ~ladders_at
    trails = np.zeros_like(body)
    for trail in TRAILS:
        trails |= m.near_line(trail, 1.1)
    trails &= body & ~walls

    def paint():
        image = sky(np.random.default_rng(223), h, w)
        ledge = terrain.region(body, wobble, 0.3)
        layers = [(terrain.region(leaf_cells, wobble, 0.9), None, foliage),
                  (ledge, None, mossy_ledge),
                  (terrain.region(trails, wobble, 0.4), None, branch_paths)]
        for k, (mask, _, draw) in enumerate(layers):
            drawn = draw(np.random.default_rng(226 + k), mask)
            shown = mask & (drawn[..., 3] > 0) if drawn.shape[2] == 4 else mask
            image[shown] = drawn[shown, :3]
        wall = terrain.region(walls, wobble, 0.22)
        under = np.roll(wall, 9, axis=0) & ~wall & ledge
        image[under] = (image[under] * 0.72).astype(np.uint8)
        drawn = bark_wall(np.random.default_rng(230), wall)
        shown = wall & (drawn[..., 3] > 0)
        image[shown] = drawn[shown, :3]
        steps = terrain.cell_field(stairs) > 0.5
        drawn = root_stairs(steps)
        image[steps] = drawn[steps, :3]
        rungs = terrain.cell_field(ladders_at) > 0.5
        drawn = ladders(np.random.default_rng(231), rungs)
        shown = rungs & (drawn[..., 3] > 0)
        image[shown] = drawn[shown, :3]
        return image

    m.paint_ground = paint
    walkable = (body & ~walls & ~leaf_cells) | stairs | ladders_at
    m.block_where(~walkable)
    for i, (x, ground) in enumerate(LANTERNS):
        m.stand(f"lantern_{i}", fp.fairy_lantern(np.random.default_rng(250 + i)), x, ground)
        m.light((x + 0.3) * LOGICAL, ground * LOGICAL - 26, 30, "#7ff0dc")
    for i, (x, ground) in enumerate(GLOW_PATCHES):
        m.stand(f"glow_patch_{i}", fp.glow_shrooms(np.random.default_rng(260 + i)), x, ground)
    m.point("up", (LADDER_X, 0))
    m.point("down", (LADDER_X, HEIGHT - 1))
    m.point("warpSpot", (37, 19))
    m.point("kate", (37, 16))
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
