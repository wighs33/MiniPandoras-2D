"""꿈(dream, 11장)을 새로 짠다(사용자 요청 2026-11-03: 11장 맵을 생성기로).

고목의 쉼터에서 잠든 주인공이 꾸는 꿈: 차원공간 허공에 뜬 작은 꽃 섬에서 판도라의 그림자를 만난다.
웹게임 맵(칸 20×14)과 섬 모양·지점 자리가 같다(hero, pandora).
  - 윗면: 옅은 빛을 머금은 꽃밭(흰·분홍·금빛·연보라 꽃), 키 큰 풀 두 무더기, 판도라가 서는 자리의 둥근 빛.
  - 섬 아래: 흙과 뿌리, 박힌 분홍·청록 수정이 뾰족하게 매달려 허공으로 사라진다.
  - 서쪽의 흰 꽃나무 한 그루(분홍 꽃잎 수관).
섬 밖은 투명해 맵 배경(차원공간)이 비친다. 떠도는 빛 알갱이는 맵 날씨(motes)다.

사용: python Tools/PixelGen/dream.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/dream/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/dream_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import pixelgen as pg
import terrain
import trees
from layout import LOGICAL, MapLayout

T = pg.TILE
ROWS = [
    "                    ",
    "                    ",
    "                    ",
    "      ,,.,,,,.      ",
    "     ,T,,.,,,,.     ",
    "    ,.,,,,.,,,,.    ",
    "    ,,.,,,P.,,,,    ",
    "   ,,,,.,,,,.,,,,   ",
    '    ,",,.,,,,.",    ',
    "    .,,,,.H,,,.,    ",
    "     .,,,,.,,,,     ",
    "       ,,,,.,       ",
    "                    ",
    "                    ",
]
HEIGHT, WIDTH = len(ROWS), len(ROWS[0])
MEADOW = pg.hexes("#2c4a3e", "#3a5e4c", "#4e765c", "#66906c", "#82aa7e", "#a6c69a")
BLOOM = pg.hexes("#ffffff", "#ffd6e8", "#f6e08a", "#d6c6ff", "#ffeef6")
EARTH = pg.hexes("#1c1626", "#2a2036", "#3a2e48", "#4c3e5a")
CRYSTAL = pg.hexes("#7a4a8a", "#e08ac0", "#ffd0ec", "#6ad0c8", "#c8fff4")
BLOSSOM = pg.hexes("#6a3a52", "#9a5272", "#c87896", "#e8a2bc", "#f6c6d8", "#ffe2ec", "#fff4f8")
DEPTH = 46                        # 섬 아래로 매달린 흙·수정의 깊이(점)


def cells_of(chars):
    return np.array([[ch in chars for ch in row] for row in ROWS])


def paint(rng):
    h, w = HEIGHT * T, WIDTH * T
    yy, xx = np.mgrid[0:h, 0:w]
    wobble = pg.fractal_noise(h, w, rng, ((10, 0.7), (4, 0.3))) - 0.5
    field = terrain.box_blur(terrain.cell_field(cells_of(',."TPH')), T // 3)
    top = field + wobble * 0.18 > 0.5
    image = np.zeros((h, w, 4), dtype=np.uint8)

    # 섬 아래: 윗면 아래 끝에서 흙이 매달리고, 열마다 깊이가 달라 뾰족하게 끝난다. 군데군데 수정.
    bottom = np.full(w, -1)
    for x in range(w):
        rows = np.nonzero(top[:, x])[0]
        if len(rows):
            bottom[x] = rows.max()
    hang = (pg.noise(1, w, 9, rng)[0] * 0.7 + pg.noise(1, w, 3, rng)[0] * 0.3)
    for x in range(w):
        if bottom[x] < 0:
            continue
        depth = int(DEPTH * (0.35 + hang[x] * 0.9))
        for k in range(1, depth):
            y = bottom[x] + k
            if y >= h:
                break
            level = 3 - int(3 * k / depth)
            image[y, x, :3] = EARTH[max(0, level)]
            image[y, x, 3] = 255
    for _ in range(14):
        x = int(rng.integers(0, w))
        if bottom[x] < 0:
            continue
        tip = bottom[x] + int(rng.integers(14, 34))
        width = int(rng.integers(3, 6))
        color = 0 if rng.random() < 0.5 else 3
        for y in range(bottom[x] + 2, min(h, tip)):
            half = width * (1 - (y - bottom[x]) / (tip - bottom[x]))
            span = slice(max(0, int(x - half)), min(w, int(x + half) + 1))
            image[y, span, :3] = CRYSTAL[color + (1 if y < (bottom[x] + tip) / 2 else 0)]
            image[y, span, 3] = 255

    # 윗면: 꽃밭. 바탕 풀은 옅게 빛나고, 꽃이 빽빽하다. 판도라 자리 둘레는 빛이 고인다.
    base = pg.quantize(np.clip(0.45 + pg.fractal_noise(h, w, rng, ((20, 0.6), (6, 0.4))) * 0.45, 0, 0.999), MEADOW, dither=0.15)
    image[top, :3] = base[top]
    image[top, 3] = 255
    # 웹게임의 키 큰 풀 두 무더기는 꽃이 빽빽한 무더기로 그린다.
    tall = terrain.region(cells_of('"'), wobble, 0.8) & top
    ys, xs = np.nonzero(top)
    dense = tall[ys, xs]
    picks = np.concatenate([rng.choice(len(xs), len(xs) // 40, replace=False), np.nonzero(dense)[0][::9]])
    for i in picks:
        x, y = xs[i], ys[i]
        petal = BLOOM[int(rng.integers(0, len(BLOOM)))]
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            if top[y + dy, x + dx]:
                image[y + dy, x + dx, :3] = petal
        image[y, x, :3] = BLOOM[2]
    px, py = 10.5 * T, 6.6 * T
    glow = np.hypot(xx - px, (yy - py) * 1.6)
    lit = top & (glow < 2.6 * T)
    mix = np.clip(1 - glow / (2.6 * T), 0, 1)[lit][:, None] * 0.55
    image[lit, :3] = (image[lit, :3] * (1 - mix) + np.array([255, 246, 220]) * mix).astype(np.uint8)
    edge = top & pg.neighbours(~top)
    image[edge, :3] = MEADOW[1]
    return image


def blossom_tree(rng):
    """흰 꽃나무: 분홍 꽃잎이 핀 둥근 수관(trees.leafy_tree에 꽃잎 빛깔)."""
    layout = [(0.5, 0.42, 0.3), (0.27, 0.55, 0.21), (0.73, 0.55, 0.21), (0.38, 0.24, 0.2), (0.64, 0.25, 0.19), (0.5, 0.66, 0.2)]
    return trees.leafy_tree(rng, 128, 160, layout, 0.78, BLOSSOM, 0.6, (0.04, 0.065), 128 * 0.065, roughness=0.3, airy=0.15)


def build():
    m = MapLayout("dream", "꿈", WIDTH, HEIGHT, theme="dream", music="void", weather="motes", background="void", seed=401)
    image = paint(np.random.default_rng(402))
    m.paint_ground = lambda: image
    m.block_where(~cells_of(',."PH'))
    m.stand("blossom_tree", blossom_tree(np.random.default_rng(403)), 6.5, 5, block=(0.8, 0.4))
    m.light(10.5 * LOGICAL, 6.5 * LOGICAL, 60, "#fff0d0")
    m.point("hero", (10, 9))
    m.point("pandora", (10, 6))
    return m


def main(project):
    m = build()
    if not m.reachable(m.points["hero"][0], m.points["pandora"]):
        print("주인공 자리에서 판도라 자리에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
