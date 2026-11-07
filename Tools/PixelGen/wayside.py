"""들판과 길가 소품(들판의 돌담 품질, 밋밋한 길 둘레를 자연스럽게).

  stone_wall(columns, rng)  막돌을 쌓은 돌담: 둥근 들돌(따뜻한 회갈색과 푸른 회색이 섞임), 이끼 낀 갓돌, 발치의 풀과 들꽃,
                            양 끝은 돌이 낮아지며 끝난다. 아래에 납작한 그림자
  dirt_path(...)            바닥 그림에 칠하는 굽이진 흙길(점마다 길까지의 거리로 칠해 가장자리가 매끄럽다)
길가에 흩뿌리는 작은 것(바닥에 까는 그림): pebbles(자갈), daisies(들국화), clover(토끼풀), flower_patch(들꽃 무리)
길가에 서는 작은 것(세우는 것, 막지 않는다): grass_clump(풀 무더기), wildflower(키 큰 들꽃), mushrooms(버섯), fern(고사리),
  boulder(이끼 낀 작은 바위), stump(그루터기)

세우는 것은 (RGBA 그림, 기준점)을 돌려준다(기준점은 바닥에 닿는 아래 가운데). 바닥에 까는 것은 RGBA만 돌려준다.
모두 점 격자(한 칸 32점)에 그린다.
"""
import numpy as np

import pixelgen as pg
import terrain
from houses import PAPER, Canvas, outline

T = pg.TILE
# 들돌: 따뜻한 회갈색과 푸른 회색(진한 쪽부터)
FIELDSTONE = pg.hexes("#2a2723", "#4a453e", "#6a645b", "#8a8378", "#a8a094", "#c6beb0")
BLUESTONE = pg.hexes("#24262c", "#3e4049", "#5a5d68", "#777a86", "#9598a3", "#b6b8c1")
GAP = np.array([24, 22, 20], dtype=np.uint8)
MOSS = pg.hexes("#2a4226", "#3a5a2e", "#4d7436", "#66903f")
WALL_FACE = 34          # 돌담 앞면 높이(점)
WALL_TOP = 14           # 위에서 보이는 갓돌 깊이(점)
SHADOW = 4              # 발치 그림자 줄 수


def rounded(c, x0, y0, x1, y1, power=2.6):
    """네모 안을 꽉 채우는 둥근 돌 모양(초타원)."""
    cx, cy, rx, ry = (x0 + x1) / 2, (y0 + y1) / 2, max(1, (x1 - x0) / 2), max(1, (y1 - y0) / 2)
    return (np.abs((c.xx + 0.5 - cx) / rx) ** power + np.abs((c.yy + 0.5 - cy) / ry) ** power) <= 1


def stone(c, x0, y0, x1, y1, palette, level, rng):
    """들돌 한 개: 바탕, 왼쪽 위 빛 받는 테, 오른쪽 아래 그늘 테, 잔 얼룩."""
    shape = rounded(c, x0, y0, x1, y1)
    c.paint(shape, palette[level])
    inner = rounded(c, x0 + 1, y0 + 1, x1 - 1, y1 - 1)
    rim = shape & ~inner
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    c.paint(rim & ((c.xx < cx) | (c.yy < cy - (y1 - y0) * 0.15)), palette[min(len(palette) - 1, level + 1)])
    c.paint(rim & (c.xx >= cx) & (c.yy >= cy), palette[max(0, level - 1)])
    ys, xs = np.nonzero(inner)
    if len(ys):
        for i in rng.choice(len(ys), max(1, len(ys) // 14), replace=False):
            c.image[ys[i], xs[i], :3] = palette[max(0, level - 1)]


def tufts(c, xs, base, rng, height=(2, 5)):
    """풀잎 다발: base 줄에서 위로 선 짧은 풀잎(아래는 짙고 끝은 밝다)."""
    for x in xs:
        blade = int(rng.integers(*height))
        lean = int(rng.choice([-1, 0, 0, 1]))
        for k in range(blade):
            px, py = int(x + (lean * k) // 2), int(base - k)
            if 0 <= px < c.size[0] and 0 <= py < c.size[1]:
                c.image[py, px, :3] = pg.GRASS[3 if k < blade - 1 else 5]
                c.image[py, px, 3] = 255


def stone_wall(columns, rng):
    """
    막돌을 쌓은 돌담(위에서 비스듬히 본 모습): 앞면은 크기가 제각각인 둥근 들돌을 줄지어 쌓고, 돌 사이는 그늘진 틈이다.
    위는 납작한 갓돌이 덮고 이끼가 끼었으며 풀이 삐죽 솟는다. 양 끝은 돌이 한두 단 낮아지며 끝난다.
    """
    w = columns * T + 6
    ground = WALL_TOP + WALL_FACE
    h = ground + SHADOW
    c = Canvas(w, h)
    xs = np.arange(w)
    # 양 끝으로 갈수록 담이 낮아진다(끝 12점 안에서 두 단).
    end = np.minimum(xs, w - 1 - xs)
    drop = np.select([end < 5, end < 12], [12, 6], 0)

    # 앞면: 아래 줄부터 크기가 제각각인 돌을 쌓는다(줄 높이도 돌마다 위아래로 조금씩 어긋난다). 돌 사이 틈은 어둡다.
    # 가끔 두 줄 높이의 큰 돌이 박혀 줄눈이 이어지지 않게 한다.
    c.paint(c.rect(0, WALL_TOP, w, ground), GAP)
    y1 = ground
    while y1 > WALL_TOP + 2:
        course = int(rng.integers(6, 13))
        y0 = max(WALL_TOP, y1 - course)
        x0 = -int(rng.integers(0, 10))
        while x0 < w:
            x1 = x0 + int(rng.choice([7, 9, 11, 13, 16, 20, 24]))
            top = y0 + int(rng.integers(0, 3))
            if rng.random() < 0.12 and y0 - 8 > WALL_TOP:
                top = y0 - int(rng.integers(5, 9))
            palette = BLUESTONE if rng.random() < 0.3 else FIELDSTONE
            stone(c, x0 + 1, top + 1, x1, y1 - int(rng.integers(0, 2)), palette, int(rng.choice([2, 3, 3, 4])), rng)
            x0 = x1
        y1 = y0

    # 갓돌: 위에서 내려다본 납작한 돌, 이끼 얼룩
    c.paint(c.rect(0, 0, w, WALL_TOP + 1), GAP)
    for y0, y1 in ((0, 7), (6, WALL_TOP + 1)):
        x0 = -int(rng.integers(0, 10))
        while x0 < w:
            x1 = x0 + int(rng.integers(12, 22))
            stone(c, x0 + 1, y0, x1, y1, FIELDSTONE, int(rng.choice([3, 4, 4, 5])), rng)
            x0 = x1
    moss = pg.fractal_noise(h, w, rng, ((8, 0.6), (3, 0.4)))
    top_moss = (c.yy < WALL_TOP + 1) & (moss > 0.58) & (c.image[..., 3] > 0)
    c.paint(top_moss, MOSS[2])
    c.paint(top_moss & (moss > 0.7), MOSS[3])
    face_moss = (c.yy >= WALL_TOP) & (c.yy < WALL_TOP + 10) & (moss > 0.72) & (c.image[..., 3] > 0)
    c.paint(face_moss, MOSS[1])

    # 담 끝을 낮추고(그 위는 비운다), 갓돌 위와 발치에 풀, 발치에 들꽃 몇 송이
    c.image[c.yy < drop[None, :]] = 0
    tufts(c, rng.choice(xs, columns * 6), 2 + int(rng.integers(0, 2)), rng, (2, 5))
    for x in rng.choice(xs, columns * 9):
        tufts(c, [x], ground - 1, rng, (3, 8))
    for x in rng.choice(xs[4:-4], columns):
        y = ground - int(rng.integers(2, 5))
        color = pg.FLOWERS[int(rng.integers(0, len(pg.FLOWERS)))]
        c.paint(c.rect(int(x), y, int(x) + 2, y + 2), color)
    image = outline(c.image)
    # 발치 그림자(빛이 왼쪽 위에서 와 오른쪽 아래로 드리운다)
    shadow = np.zeros((h, w), dtype=bool)
    shadow[ground:ground + SHADOW, 3:] = True
    shadow &= image[..., 3] == 0
    image[shadow] = (14, 20, 12, 90)
    return image, (w // 2, ground)


# ── 길 ─────────────────────────────────────────────────

def dirt_path(image, paths, rng):
    """
    굽이진 흙길을 바닥 그림(점 격자 RGB)에 칠한다. paths는 [(꺾은선(칸), 반폭(칸)), …]이다.
    칸이 아니라 점마다 길에서 떨어진 거리로 칠해 가장자리가 매끄럽게 굽고, 잡음으로 조금씩 들쭉날쭉하다.
    흙 바탕에 밟혀 다져진 밝은 가운데, 풀밭 경계는 풀이 한 단 높아 보이게 어둡고 풀잎이 흙 위로 늘어진다(terrain.path_border).
    돌려주는 값: 점마다 길 반폭에 견준 거리(1보다 작으면 길 안).
    """
    height, width = image.shape[:2]
    yy, xx = np.mgrid[0:height, 0:width] + 0.5
    reach = np.full((height, width), np.inf)
    for points, half in paths:
        for (ax, ay), (bx, by) in zip(points, points[1:]):
            ax, ay, bx, by = ax * T, ay * T, bx * T, by * T
            dx, dy = bx - ax, by - ay
            t = np.clip(((xx - ax) * dx + (yy - ay) * dy) / max(1e-9, dx * dx + dy * dy), 0, 1)
            reach = np.minimum(reach, np.hypot(xx - (ax + t * dx), yy - (ay + t * dy)) / (half * T))
    wobble = pg.fractal_noise(height, width, rng, ((20, 0.6), (6, 0.4))) - 0.5
    inside = reach + wobble * 0.3 < 1
    image[inside] = terrain.dirt(height, width, rng)[inside]
    dither = pg.bayer(height, width)
    worn = inside & (reach + wobble * 0.5 < 0.4)
    image[worn & (dither < 0.65)] = pg.DIRT[3]
    image[worn & (reach < 0.16) & (dither < 0.35)] = pg.DIRT[4]
    terrain.path_border(image, inside, rng)
    return reach + wobble * 0.3


def pebbles(rng):
    """자갈 몇 알(바닥에 까는 그림)."""
    w, h = 14, 9
    c = Canvas(w, h)
    for _ in range(int(rng.integers(3, 6))):
        x, y = rng.uniform(2, w - 3), rng.uniform(2, h - 2)
        rx, ry = rng.uniform(1.2, 2.4), rng.uniform(1, 1.8)
        palette = BLUESTONE if rng.random() < 0.4 else FIELDSTONE
        shape = c.ellipse(x, y, rx, ry)
        c.paint(shape, palette[3])
        c.paint(shape & (c.yy < y - ry * 0.2), palette[4])
        c.paint(c.ellipse(x + 0.6, y + ry * 0.9, rx * 0.8, 0.8) & ~shape, pg.GRASS[1])
    return c.image


def daisies(rng):
    """들국화 무리: 흰 꽃잎에 노란 가운데, 사이사이 잎(바닥에 까는 그림)."""
    w, h = 16, 11
    c = Canvas(w, h)
    for _ in range(6):
        x, y = int(rng.integers(1, w - 2)), int(rng.integers(1, h - 2))
        c.paint(c.rect(x, y, x + 2, y + 1), pg.GRASS[4])
    for _ in range(int(rng.integers(3, 6))):
        x, y = int(rng.integers(2, w - 2)), int(rng.integers(2, h - 2))
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            c.paint(c.rect(x + dx, y + dy, x + dx + 1, y + dy + 1), pg.FLOWERS[0])
        c.paint(c.rect(x, y, x + 1, y + 1), pg.FLOWER_CORE)
    return c.image


def clover(rng):
    """토끼풀 무리: 세 잎 토끼풀과 드문 분홍 꽃(바닥에 까는 그림)."""
    w, h = 18, 12
    c = Canvas(w, h)
    for _ in range(int(rng.integers(5, 9))):
        x, y = int(rng.integers(2, w - 3)), int(rng.integers(2, h - 2))
        for dx, dy in ((0, -1), (-1, 1), (1, 1)):
            c.paint(c.ellipse(x + dx + 0.5, y + dy + 0.5, 1.1, 1.0), pg.GRASS[5])
        c.paint(c.rect(x, y, x + 1, y + 1), pg.GRASS[3])
    if rng.random() < 0.6:
        x, y = int(rng.integers(3, w - 3)), int(rng.integers(2, h - 3))
        c.paint(c.ellipse(x + 0.5, y + 0.5, 1.6, 1.6), pg.hexes("#e8a0c0")[0])
        c.paint(c.rect(x, y, x + 1, y + 1), pg.hexes("#f6d6e4")[0])
    return c.image


def grass_clump(rng):
    """풀 무더기: 길가에 수북이 솟은 풀잎(아래는 짙고 끝은 밝다)과 끝이 휜 이삭 몇 대. 테두리 없이 풀밭에 녹아든다."""
    w, h = 20, 17
    c = Canvas(w, h)
    ground = h - 1
    c.paint(c.ellipse(w / 2, ground, 7, 2), pg.GRASS[1])
    for _ in range(22):
        x = rng.uniform(4, w - 4)
        blade = int(rng.integers(5, 14))
        lean = float(rng.uniform(-0.5, 0.5))
        for k in range(blade):
            px, py = int(round(x + lean * k + lean * k * k * 0.02)), ground - k
            if 0 <= px < w:
                c.image[py, px, :3] = pg.GRASS[2 if k < 2 else 4 if k < blade * 0.5 else 5 if k < blade - 1 else 6]
                c.image[py, px, 3] = 255
    for _ in range(int(rng.integers(1, 4))):
        x, top = int(rng.integers(5, w - 5)), int(rng.integers(0, 4))
        bend = int(rng.choice([-1, 1]))
        c.paint(c.rect(x, top + 2, x + 1, ground - 5), pg.GRASS[4])
        c.paint(c.rect(x + bend, top, x + bend + 1, top + 3), pg.SAND[4])
        c.paint(c.rect(x + bend, top + 1, x + bend + 1, top + 2), pg.SAND[2])
    return c.image, (w // 2, ground)


def wildflower(rng):
    """키 큰 들꽃: 가는 줄기 서넛 끝에 작은 꽃송이(한 무리는 한 가지 색), 줄기 밑의 잎."""
    w, h = 14, 18
    c = Canvas(w, h)
    ground = h - 1
    petal = pg.hexes(str(rng.choice(["#a98ae0", "#7d9ce8", "#f0cc4a", "#f07a96", "#f4f0e4"])))[0]
    for x in rng.choice(np.arange(3, w - 3), int(rng.integers(3, 5)), replace=False):
        x = int(x)
        top = int(rng.integers(3, 9))
        lean = int(rng.choice([-1, 0, 1]))
        for y in range(top + 1, ground + 1):
            c.image[y, x + (lean if y < top + 4 else 0), :3] = pg.GRASS[3]
            c.image[y, x + (lean if y < top + 4 else 0), 3] = 255
        fx = x + lean
        for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1)):
            c.paint(c.rect(fx + dx, top + dy, fx + dx + 1, top + dy + 1), petal)
        c.paint(c.rect(fx, top, fx + 1, top + 1), pg.FLOWER_CORE)
        c.paint(c.rect(x - 2, ground - 3, x, ground - 2) | c.rect(x + 1, ground - 5, x + 3, ground - 4), pg.GRASS[4])
    return c.image, (w // 2, ground)


def mushrooms(rng):
    """버섯 두셋: 붉은 갓에 흰 점, 또는 밤색 갓. 굵은 흰 대."""
    w, h = 16, 12
    c = Canvas(w, h)
    ground = h - 1
    red = rng.random() < 0.5
    cap = pg.hexes("#a8322a", "#d4483a") if red else pg.hexes("#6a4426", "#93643a")
    for k, x in enumerate((4, 9, 12)[:int(rng.integers(2, 4))]):
        size = 3 if k == 0 else 2
        c.paint(c.rect(x - 1, ground - size - 2, x + 1, ground), PAPER[1])
        c.paint(c.ellipse(x, ground - size - 2, size + 1, size * 0.8), cap[0])
        c.paint(c.ellipse(x - 0.5, ground - size - 2.5, size, size * 0.55), cap[1])
        if red:
            c.paint(c.rect(x - 1, ground - size - 3, x, ground - size - 2), (240, 232, 220))
    return outline(c.image), (w // 2, ground)


def fern(rng):
    """고사리: 숲 가장자리에서 부채처럼 솟아 끝이 휘는 잎줄기 몇 대, 줄기 양옆으로 엇갈린 작은 잎."""
    w, h = 26, 20
    c = Canvas(w, h)
    ground = h - 1
    c.paint(c.ellipse(w / 2, ground, 6, 2), pg.GRASS[1])
    for angle in np.linspace(-0.95, 0.95, int(rng.integers(4, 6))) + rng.uniform(-0.1, 0.1):
        length = rng.uniform(11, 16)
        for k in range(int(length)):
            t = k / length
            # 위로 솟다가 끝이 바깥으로 처진다
            x = w / 2 + np.sin(angle) * k * (0.6 + t * 0.6)
            y = ground - k * np.cos(angle) * (1.0 - t * 0.45)
            px, py = int(round(x)), int(round(y))
            if not (0 <= px < w and 0 <= py < h):
                continue
            c.image[py, px, :3] = pg.GRASS[3]
            c.image[py, px, 3] = 255
            leaf = int(round(3 * (1 - t))) + 1
            if k % 2 == 0 and k > 1:
                for step in range(1, leaf + 1):
                    for side in (-1, 1):
                        lx, ly = px + side * step, py - (step // 2)
                        if 0 <= lx < w and 0 <= ly < h:
                            c.image[ly, lx, :3] = pg.GRASS[5 if side < 0 else 4]
                            c.image[ly, lx, 3] = 255
    return c.image, (w // 2, ground)


def flower_patch(rng):
    """들꽃 무리(바닥에 까는 그림): 낮은 잎 사이로 두 가지 색 작은 꽃이 소복하다."""
    w, h = 26, 14
    c = Canvas(w, h)
    base = c.ellipse(w / 2, h / 2, w / 2 - 1, h / 2 - 1)
    leaves = base & (pg.bayer(h, w) < 0.45)
    c.paint(leaves, pg.GRASS[4])
    colors = pg.FLOWERS[rng.choice(len(pg.FLOWERS), 2, replace=False)]
    for _ in range(int(rng.integers(8, 13))):
        x, y = int(rng.integers(2, w - 2)), int(rng.integers(2, h - 2))
        if base[y, x]:
            color = colors[int(rng.integers(0, 2))]
            c.paint(c.rect(x - 1, y, x + 2, y + 1) | c.rect(x, y - 1, x + 1, y + 2), color)
            c.paint(c.rect(x, y, x + 1, y + 1), pg.FLOWER_CORE)
    return c.image


def boulder(rng):
    """길가의 이끼 낀 작은 바위: 둥근 들돌 하나와 그 옆 작은 돌, 발치의 풀."""
    w, h = 22, 16
    c = Canvas(w, h)
    ground = h - 1
    palette = BLUESTONE if rng.random() < 0.4 else FIELDSTONE
    stone(c, 3, 3, 16, ground, palette, 3, rng)
    stone(c, 14, ground - 6, 20, ground, palette, 2, rng)
    moss = pg.fractal_noise(h, w, rng, ((5, 0.6), (2, 0.4)))
    c.paint((c.image[..., 3] > 0) & (c.yy < 8) & (moss > 0.55), MOSS[2])
    image = outline(c.image)
    canvas = Canvas(w, h)
    canvas.image = image
    tufts(canvas, rng.choice(np.arange(2, w - 2), 6), ground, rng, (2, 5))
    return canvas.image, (w // 2, ground)


def stump(rng):
    """그루터기: 나이테가 보이는 잘린 윗면, 갈라진 껍질, 이끼, 옆으로 뻗은 뿌리."""
    w, h = 22, 18
    c = Canvas(w, h)
    ground = h - 1
    bark = pg.BARK
    c.paint(c.rect(5, 6, 17, ground), bark[2])
    c.paint(c.rect(5, 6, 7, ground), bark[3])
    c.paint(c.rect(15, 6, 17, ground), bark[1])
    for x in (9, 12):
        c.paint(c.rect(x, 8, x + 1, ground - 1), bark[1])
    c.paint(c.polygon([(1, ground + 1), (6, ground - 4), (7, ground + 1)]) | c.polygon([(21, ground + 1), (16, ground - 4), (15, ground + 1)]), bark[2])
    top = c.ellipse(11, 6, 6.5, 3)
    c.paint(top, pg.WOOD[4])
    c.paint(c.ellipse(11, 6, 4.5, 2) & ~c.ellipse(11, 6, 3.5, 1.4), pg.WOOD[3])
    c.paint(c.ellipse(11, 6, 1.5, 0.8), pg.WOOD[2])
    c.paint(c.rect(5, ground - 3, 9, ground - 1), MOSS[2])
    return outline(c.image), (w // 2, ground)
