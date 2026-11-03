"""고목의 뿌리(treeRoots, 10장)를 새로 짠다(사용자 요청 2026-11-03: 10장 맵을 생성기로).

거대고목 밑동 아래 뿌리 동굴. 웹게임은 돌 동굴이지만, 벽이 서로 얽힌 굵은 뿌리로 된 굴로 다시 짰다(칸 26×18, 지점 자리 같다):
  - 벽: 위에서 본 얽힌 뿌리 다발(뿌리마다 둥글게 빛을 받고 사이는 어둡다). 바닥 북쪽 가장자리는 드러난 뿌리 벽면.
  - 바닥: 축축한 흙과 실뿌리, 자갈. 북쪽 가운데 뿌리 계단이 고목으로 올라가는 굴(up)이다.
  - 가운데 큰 웅덩이: 북쪽 물가에서 뻗은 뿌리 곶 끝에 저주받은 요정 아피아가 선다(apia, core). 서쪽 벽가에 작은 웅덩이.
모습 둘: 저주(cursed, 기본: 검보랏빛 물과 시든 뿌리, 보랏빛 실핏줄)와 맑아진 뿌리(clean, 10장 싸움 뒤: 맑은 물, 이끼, 빛나는 버섯).

사용: python Tools/PixelGen/tree_roots.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/treeRoots/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/treeRoots_preview.png, treeRoots_clean_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import fairy_props as fp
import pixelgen as pg
import terrain
from houses import Canvas
from layout import LOGICAL, MapLayout

T = pg.TILE
ROWS = [
    "           #U#            ",
    "           #e#            ",
    "   #########.##########   ",
    "   #########.##########   ",
    "  ####..............####  ",
    "  ###................###  ",
    "  #....................#  ",
    "  #~....~~~~..~~~~.....#  ",
    "  #....~~~~~AC~~~~~...##  ",
    "  #....~~~~~~~~~~~~....#  ",
    "  #....~~~~~~~~~~~~....#  ",
    "  #....~~~~~~~~~~~~....#  ",
    "  ##....~~~~~~~~~~.....#  ",
    "  ##..................##  ",
    "  ##..................##  ",
    "   ###..............###   ",
    "   ####################   ",
    "                          ",
]
HEIGHT, WIDTH = len(ROWS), len(ROWS[0])
FACE = 26                    # 바닥 북쪽 가장자리에 드러난 뿌리 벽면 높이(점)
ROOT = pg.hexes("#0e0a08", "#1a120e", "#281c14", "#38281c", "#4a3726", "#5e4832")
SOIL = pg.hexes("#221a15", "#2e241d", "#3b2f26", "#4a3b30", "#5a493b")
CURSED_WATER = pg.hexes("#07040c", "#120a1e", "#1e1032", "#2e1a48", "#4a2a6e")
CLEAN_WATER = pg.hexes("#0c2230", "#123446", "#1a4a5e", "#286476", "#4a8a9a", "#86c0c8")
VEIN = pg.hexes("#3a1a5a", "#6a3aa0")


def cells_of(chars):
    return np.array([[ch in chars for ch in row] for row in ROWS])


def root_tangle(mask, rng, palette):
    """얽힌 뿌리 다발: mask 안에 굽은 뿌리 수백 가닥을 원으로 찍어 쌓는다. 가닥마다 둥글게 빛을 받고, 사이 틈은 어둡다."""
    h, w = mask.shape
    image = np.zeros((h, w, 3), dtype=np.uint8)
    image[:] = palette[0]
    ys, xs = np.nonzero(mask)
    yy, xx = np.mgrid[0:h, 0:w]
    for _ in range(int(len(xs) / 160)):
        i = rng.integers(0, len(xs))
        x, y = float(xs[i]), float(ys[i])
        angle = rng.uniform(0, np.pi * 2)
        r = rng.uniform(3, 7.5)
        for _ in range(int(rng.integers(14, 40))):
            angle += rng.uniform(-0.35, 0.35)
            x += np.cos(angle) * 2.2
            y += np.sin(angle) * 1.6
            x0, x1, y0, y1 = int(max(0, x - r - 1)), int(min(w, x + r + 2)), int(max(0, y - r - 1)), int(min(h, y + r + 2))
            if x0 >= x1 or y0 >= y1:
                break
            dx, dy = xx[y0:y1, x0:x1] - x, yy[y0:y1, x0:x1] - y
            disc = dx * dx + dy * dy <= r * r
            shade = 0.45 - (dx * 0.5 + dy * 0.7) / (r * 1.6) - (dx * dx + dy * dy) / (r * r) * 0.25
            level = np.clip((shade * len(palette)).astype(int) + 1, 1, len(palette) - 1)
            image[y0:y1, x0:x1][disc] = palette[level][disc]
    return image


def build_floor(rng):
    """바닥 점과 북쪽 가장자리 벽면 점(위에서 비스듬히 본 시점이라 바닥 위쪽 끝 위로 벽면이 드러난다)."""
    h, w = HEIGHT * T, WIDTH * T
    floor_cells = cells_of(".~AeUC")
    wobble = pg.fractal_noise(h, w, rng, ((20, 0.7), (7, 0.3))) - 0.5
    floor = terrain.region(floor_cells, wobble, 1.6)
    face = np.zeros_like(floor)
    for k in range(1, FACE + 1):
        face |= np.roll(floor, -k, axis=0)
    face &= ~floor
    return floor, face


def paint(rng, floor, face, pool, small_pool, clean):
    h, w = floor.shape
    wall = ~floor & ~face
    walls = root_tangle(wall | face, np.random.default_rng(301), ROOT)
    image = walls.copy()
    # 바닥에서 멀어질수록 뿌리 다발이 어둠에 가라앉는다(맵 가장자리가 네모나게 끊겨 보이지 않게).
    far = terrain.distance_inside(~floor, 72).astype(float)
    dim = np.clip(1.0 - (far - 10) / 70, 0.25, 1.0)
    image[:] = (image * dim[..., None]).astype(np.uint8)
    # 벽면: 아래로 갈수록 그늘, 아래 끝에 축축한 줄.
    v = np.zeros((h, w))
    for k in range(FACE, 0, -1):
        v[np.roll(floor, -k, axis=0) & face] = k / FACE
    image[face] = (image[face] * (0.55 + 0.45 * v[face, None])).astype(np.uint8)
    # 바닥: 축축한 흙, 실뿌리, 자갈.
    soil = pg.quantize(np.clip(0.45 + (pg.fractal_noise(h, w, rng, ((24, 0.6), (6, 0.4))) - 0.5) * 0.7, 0, 0.999), SOIL, dither=0.15)
    image[floor] = soil[floor]
    for _ in range(140):
        ys, xs = np.nonzero(floor & ~pool)
        i = rng.integers(0, len(xs))
        x, y = float(xs[i]), float(ys[i])
        angle = rng.uniform(0, np.pi * 2)
        for _ in range(int(rng.integers(6, 18))):
            angle += rng.uniform(-0.5, 0.5)
            x, y = x + np.cos(angle), y + np.sin(angle) * 0.7
            if 0 <= int(y) < h and 0 <= int(x) < w and floor[int(y), int(x)]:
                image[int(y), int(x)] = ROOT[3] if not clean else fp.MOSS[1]
    pebbles = floor & ~pool & (rng.random((h, w)) < 0.004)
    image[pebbles] = pg.STONE[2]
    # 북쪽 굴의 뿌리 계단.
    stairs = terrain.cell_field(cells_of("eU")) > 0.5
    image[stairs & (np.mod(np.mgrid[0:h, 0:w][0], 8) < 3)] = ROOT[4]
    # 저주: 바닥과 벽을 타고 번진 보랏빛 실핏줄. 맑아지면 이끼와 빛나는 버섯 자리.
    if not clean:
        veins = pg.fractal_noise(h, w, rng, ((16, 0.6), (5, 0.4)))
        line = (np.abs(veins - 0.5) < 0.012) & (floor | face) & ~pool
        image[line] = VEIN[0]
        image[line & (pg.bayer(h, w) > 0.6)] = VEIN[1]
    else:
        blot = pg.fractal_noise(h, w, rng, ((12, 0.7), (4, 0.3)))
        moss = (blot > 0.7) & (floor | face) & ~pool
        image[moss] = fp.MOSS[0]
        image[moss & (blot > 0.76) & (pg.bayer(h, w) > 0.5)] = fp.MOSS[1]
    return image


def water_layer(pool, rng, clean):
    """웅덩이 물(모습마다 다른 바닥 그림): 가장자리가 밝고 안쪽이 깊다. 저주는 검보랏빛에 기름 같은 윤기, 맑으면 청록빛 물결."""
    h, w = pool.shape
    image = np.zeros((h, w, 4), dtype=np.uint8)
    depth = terrain.distance_inside(pool, 12) + (pg.fractal_noise(h, w, rng, ((20, 0.7), (7, 0.3))) - 0.5) * 3
    palette = CLEAN_WATER if clean else CURSED_WATER
    level = len(palette) - 2 - pg.dither_levels(depth, (1.5, 4, 8), 0.8)
    image[pool, :3] = palette[np.clip(level, 0, len(palette) - 1)][pool]
    ripple = np.sin(np.mgrid[0:h, 0:w][0] / 2.2 + pg.noise(h, w, 18, rng) * 9)
    sheen = pool & (ripple > 0.96) & (depth > 2) & (pg.noise(h, w, 9, rng) > 0.55)
    image[sheen, :3] = palette[-1] if clean else VEIN[0]
    edge = pool & (depth < 1.2)
    image[edge, :3] = palette[-2] if clean else VEIN[0]
    image[pool, 3] = 255
    return image


def build():
    m = MapLayout("treeRoots", "고목의 뿌리", WIDTH, HEIGHT, theme="cave", music="sorrow",
                  ambient={"color": "#06080c", "alpha": 0.42}, seed=311)
    rng = np.random.default_rng(312)
    floor, face = build_floor(rng)
    h, w = floor.shape
    wobble = pg.fractal_noise(h, w, np.random.default_rng(313), ((10, 0.7), (4, 0.3))) - 0.5
    water_cells = cells_of("~")
    pool = terrain.region(water_cells, wobble, 0.9) & floor
    ground = paint(np.random.default_rng(314), floor, face, pool, None, clean=False)
    m.paint_ground = lambda: ground

    for name, clean in (("cursed", False), ("clean", True)):
        with m.look(name):
            water = water_layer(pool, np.random.default_rng(320), clean)
            ys, xs = np.nonzero(water[..., 3])
            top, left = ys.min(), xs.min()
            m.decal(f"pool_{name}", water[top:ys.max() + 1, left:xs.max() + 1], left, top)
            if clean:
                # 맑아진 뒤의 이끼와 빛나는 버섯.
                patch = paint(np.random.default_rng(314), floor, face, pool, None, clean=True)
                over = np.zeros((h, w, 4), dtype=np.uint8)
                changed = (patch != ground).any(axis=2)
                over[changed, :3] = patch[changed]
                over[changed, 3] = 255
                m.decal("moss", over, 0, 0)
                # 맑아진 웅덩이가 은은하게 빛난다.
                m.light(12.5 * LOGICAL, 9.5 * LOGICAL, 80, "#9ae6ec")
                for i, (x, y) in enumerate(((5.2, 6.8), (19.8, 6.6), (4.6, 13.6), (20.6, 13.4), (9.0, 14.4), (16.4, 14.6))):
                    m.stand(f"shrooms_{i}", fp.glow_shrooms(np.random.default_rng(330 + i)), x, y)
                    m.light(x * LOGICAL, y * LOGICAL - 4, 22, "#7ff0dc")
            else:
                for x, y in ((12.5, 8.5), (6.5, 9.5), (18.5, 10.5)):
                    m.light(x * LOGICAL, y * LOGICAL, 52, "#7a3ab8")

    # 벽가의 빛나는 버섯(늘 있다): 어두운 굴에서 길을 알려 준다.
    for i, (x, y) in enumerate(((12.0, 3.6), (14.2, 3.4), (3.6, 9.4), (22.4, 11.6))):
        m.stand(f"wall_glow_{i}", fp.glow_shrooms(np.random.default_rng(340 + i)), x, y)
        m.light(x * LOGICAL, y * LOGICAL - 4, 26, "#7ff0dc")
    # 고목에서 내려오는 굴 입구로 바깥빛이 조금 든다.
    m.light(12.5 * LOGICAL, 2.5 * LOGICAL, 40, "#c8b890")

    marks = {"U": "up", "e": "enter", "A": "apia", "C": "core"}
    for y, row in enumerate(ROWS):
        for x, ch in enumerate(row):
            if ch in marks:
                m.point(marks[ch], (x, y))
    m.block_where(~cells_of(".AeUC"))
    m.warp("up", "treeHall", "rootsArrive", "down")
    return m


def main(project):
    m = build()
    start = m.points["enter"][0]
    for name, cells in m.points.items():
        for cell in cells:
            if not m.reachable(start, [cell]):
                print(f"입구에서 {name} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
