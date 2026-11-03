"""코드로 그리는 지형: 풀밭, 키 큰 풀, 꽃, 흙길, 모래, 돌바닥, 못·바다, 다리, 절벽, 절벽을 오르는 돌계단.

맵 바닥은 칸 격자(웹게임 rows를 칸마다 타일 이름으로 푼 것, Tools/WebGame/export_tiles.mjs)에서 그린다.
칸 사이 경계는 칸 가운데끼리 부드럽게 이은 값(cell_field)이 0.5인 곳이라 칸 모양을 따르되 모서리가 둥글다.
경계 근처만 잡음이 흔들어 자로 그은 듯한 선이 되지 않게 한다.
"""
from collections import Counter

import numpy as np

import pixelgen as pg
import props

T = pg.TILE

# 따로 그린 그림으로 맵에 세우는 칸(objects.py). 바닥은 그 아래에 이웃 바닥을 깐다.
OBJECT_KINDS = {"houseRoof", "houseWall", "window", "door", "crate", "barrel", "well", "basin", "fence"}
GROUND_KINDS = ("grass", "path", "sand", "cobble")


# ── 칸 격자 → 점 격자의 모양 ─────────────────────────────

def cell_field(mask):
    """칸마다 0/1인 격자를 점 격자로 부드럽게 늘린다. 칸 가운데에서 원래 값, 칸 경계에서 이웃과 반반이다."""
    rows, cols = mask.shape
    padded = np.pad(mask.astype(float), 1, mode="edge")

    def axis(n):
        u = (np.arange(n * T) + 0.5) / T - 0.5 + 1
        i0 = np.floor(u).astype(int)
        return i0, pg.smooth(u - i0)

    y0, ty = axis(rows)
    x0, tx = axis(cols)
    top = padded[y0][:, x0] * (1 - tx) + padded[y0][:, x0 + 1] * tx
    bottom = padded[y0 + 1][:, x0] * (1 - tx) + padded[y0 + 1][:, x0 + 1] * tx
    return top * (1 - ty[:, None]) + bottom * ty[:, None]


def region(mask, wobble, amount):
    """칸 격자 mask의 점 격자 모양. 경계 근처에서만 wobble(−0.5~0.5)이 모양을 흔든다."""
    field = cell_field(mask)
    return field + wobble * amount * 4 * field * (1 - field) > 0.5


def box_blur(field, radius):
    """가로·세로 상자 흐림을 세 번(가우스 흐림에 가깝다). 칸 격자 모양을 둥글게 다듬을 때 쓴다."""
    out = field.astype(float)
    for _ in range(3):
        for axis in (0, 1):
            padded = np.pad(out, [(radius + 1, radius) if a == axis else (0, 0) for a in (0, 1)], mode="edge")
            summed = np.cumsum(padded, axis=axis)
            upper = np.take(summed, np.arange(2 * radius + 1, summed.shape[axis]), axis=axis)
            lower = np.take(summed, np.arange(0, summed.shape[axis] - 2 * radius - 1), axis=axis)
            out = (upper - lower) / (2 * radius + 1)
    return out


def ground_under(kinds):
    """
    세운 것이 설 칸의 바닥: 이웃한 바닥 가운데 가장 많은 것. 집처럼 여러 칸이 붙어 있으면 바깥 칸부터 안쪽으로 채운다.
    수반 안에 고인 물 칸(둘레가 모두 수반)도 수반으로 본다(물은 수반 그림이 그린다).
    """
    base = kinds.copy()
    rows, cols = base.shape
    near = ((-1, 0), (1, 0), (0, -1), (0, 1))
    for r, c in zip(*np.nonzero(base == "water")):
        if all(0 <= r + dr < rows and 0 <= c + dc < cols and base[r + dr, c + dc] == "basin" for dr, dc in near):
            base[r, c] = "basin"
    unknown = np.isin(base, list(OBJECT_KINDS))
    while unknown.any():
        filled = []
        for r, c in zip(*np.nonzero(unknown)):
            votes = Counter(base[r + dr, c + dc] for dr, dc in near
                            if 0 <= r + dr < rows and 0 <= c + dc < cols and not unknown[r + dr, c + dc] and base[r + dr, c + dc] in GROUND_KINDS)
            if votes:
                filled.append((r, c, votes.most_common(1)[0][0]))
        if not filled:
            base[unknown] = "grass"
            break
        for r, c, kind in filled:
            base[r, c] = kind
            unknown[r, c] = False
    return base


def distance_inside(mask, limit):
    """mask 안에서 바깥까지의 거리(점, limit에서 멈춤)."""
    distance = np.zeros(mask.shape, dtype=int)
    current = mask.copy()
    for step in range(1, limit + 1):
        current = current & ~pg.neighbours(~current)
        distance[current] = step
    return distance


# ── 바탕 무늬 ──────────────────────────────────────────

def grass(height, width, rng):
    """풀밭: 큰 얼룩 세 단계 위에 풀잎 다발과 드문 밝은 잎 끝."""
    shade = pg.fractal_noise(height, width, rng, ((96, 0.55), (32, 0.3), (10, 0.15)))
    image = pg.GRASS[2 + pg.dither_levels(shade, (0.4, 0.6))].copy()
    sprinkle_tufts(image, shade, rng, int(height * width / 34))
    for _ in range(int(height * width / 160)):
        x, y = rng.integers(0, width), rng.integers(0, height)
        image[y, x] = pg.GRASS[6]
    return image


def sprinkle_tufts(image, shade, rng, count, wrap=False):
    """풀잎 다발: 아래는 그늘, 위로 밝은 잎 끝. wrap이면 가장자리를 넘을 때 반대편으로 돈다(끝없이 잇는 무늬)."""
    height, width = image.shape[:2]
    for _ in range(count):
        x, y = rng.integers(0, width), rng.integers(0, height)
        light = shade[y, x] > 0.5
        for b in range(rng.integers(2, 4)):
            bx = x + b * rng.choice([-1, 1]) * rng.integers(1, 3)
            blade = rng.integers(2, 4)
            for h in range(-1, blade):
                py = y - h
                px, py = (bx % width, py % height) if wrap else (bx, py)
                if 0 <= px < width and 0 <= py < height:
                    tip = h == blade - 1
                    image[py, px] = pg.GRASS[2] if h < 0 else pg.GRASS[6 if tip and light else 5 if tip else 4]


def dirt(height, width, rng):
    """흙길: 한 톤 바탕에 옅은 얼룩, 잔돌과 자갈."""
    shade = pg.fractal_noise(height, width, rng, ((40, 0.7), (12, 0.3)))
    image = pg.DIRT[2 + pg.dither_levels(shade, (0.36, 0.62), 0.08)].copy()
    for _ in range(int(height * width / 30)):
        x, y = rng.integers(0, width), rng.integers(0, height)
        image[y, x] = pg.DIRT[2] if rng.random() < 0.6 else pg.DIRT[4]
    for _ in range(int(height * width / 220)):
        x, y = rng.integers(1, width - 1), rng.integers(1, height - 1)
        image[y, x] = pg.STONE[3]
        image[y - 1, x] = pg.STONE[4]
        image[y + 1, x] = pg.DIRT[1]
        if rng.random() < 0.4:
            image[y, x + 1] = pg.STONE[2]
            image[y + 1, x + 1] = pg.DIRT[1]
    return image


def sand(height, width, rng):
    """모래: 옅은 물결 얼룩에 어두운·밝은 모래알, 드문 조개껍데기와 자갈."""
    shade = pg.fractal_noise(height, width, rng, ((48, 0.6), (12, 0.4)))
    image = pg.SAND[3 - pg.dither_levels(shade, (0.7,), 0.08)].copy()
    for _ in range(int(height * width / 26)):
        x, y = rng.integers(0, width), rng.integers(0, height)
        image[y, x] = pg.SAND[1] if rng.random() < 0.6 else pg.SAND[4]
    for _ in range(int(height * width / 900)):
        x, y = rng.integers(1, width - 1), rng.integers(1, height - 1)
        image[y, x] = pg.STONE[4]
        image[y, x + 1] = pg.SAND[4]
        image[y + 1, x] = pg.SAND[0]
    return image


def cells(height, width, cell, rng):
    """
    돌판·자갈 무늬: 어긋난 격자 점마다 가장 가까운 칸(보로노이). 칸 번호와 이음매(경계)를 돌려준다.
    점마다 둘레 3×3 격자 점만 견줘 큰 맵에서도 빠르다.
    """
    rows, cols = height // cell + 3, width // cell + 3
    jitter = rng.uniform(0.2, 0.8, (rows, cols, 2))
    yy, xx = np.mgrid[0:height, 0:width]
    gy, gx = yy // cell + 1, xx // cell + 1
    best = np.full((height, width), np.inf)
    owner = np.zeros((height, width), dtype=int)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cy, cx = gy + dy, gx + dx
            centre_y = (cy - 1 + jitter[cy, cx, 0]) * cell
            centre_x = (cx - 1 + jitter[cy, cx, 1]) * cell
            distance = (yy - centre_y) ** 2 + ((xx - centre_x) * 0.8) ** 2
            closer = distance < best
            best[closer] = distance[closer]
            owner[closer] = (cy * cols + cx)[closer]
    seams = np.zeros((height, width), dtype=bool)
    seams[:, 1:] |= owner[:, 1:] != owner[:, :-1]
    seams[1:, :] |= owner[1:, :] != owner[:-1, :]
    return owner, seams


def cobble(height, width, rng):
    """돌바닥: 둥근 자갈돌마다 밝기가 다르고, 틈은 어둡고, 돌 윗가장자리는 빛을 받는다."""
    owner, seams = cells(height, width, 9, rng)
    shade = rng.integers(0, 3, owner.max() + 1)
    image = pg.STONE[2 + shade[owner]].copy()
    image[seams] = pg.STONE[1]
    below = ~seams & np.pad(seams, ((1, 0), (0, 0)))[:-1]
    image[below] = pg.STONE[5]
    return image


def edge_border(image, area, rng, inner, outer):
    """바닥 경계: 안쪽(area) 가장자리 한 줄은 inner 색, 바깥쪽 풀 가장자리는 짙게, 풀잎이 조금 넘어온다."""
    outside = ~area
    edge = area & pg.neighbours(outside)
    image[edge] = inner
    image[outside & pg.neighbours(area)] = outer
    ys, xs = np.nonzero(edge)
    for y, x, roll in zip(ys, xs, rng.random(len(ys))):
        if roll < 0.18:
            image[y, x] = pg.GRASS[4]


def bridge(image, area, rng):
    """
    나무 다리·부두(물 위): 걷는 방향을 가로지르는 판자, 판자 사이 틈, 양옆 난간 들보와 기둥.
    세로로 긴 묶음이면 위아래로 걷는 부두다.
    """
    ys, xs = np.nonzero(area)
    if len(ys) == 0:
        return
    vertical = (ys.max() - ys.min()) >= (xs.max() - xs.min())
    along, across = (ys, xs) if vertical else (xs, ys)
    plank = 6
    for y, x in zip(ys, xs):
        a = y if vertical else x
        b = x if vertical else y
        level = 3 if (a // plank + b // 17) % 3 else 2
        if a % plank == 0:
            level = 1
        image[y, x] = pg.WOOD[level]
    # 양옆 들보와 기둥
    side = pg.neighbours(~area) & area
    image[side] = pg.WOOD[1]
    for y, x in zip(*np.nonzero(side)):
        a = y if vertical else x
        if a % 32 < 3:
            image[y, x] = pg.WOOD[0]


# ── 지형 ───────────────────────────────────────────────

def path_border(image, is_dirt, rng):
    """풀밭과 흙의 경계: 풀은 한 단 높아 보이게 테두리를 어둡게, 흙은 풀 밑 그늘 두 줄, 풀잎이 흙 위로 늘어진다."""
    grass_side = ~is_dirt
    edge = is_dirt & pg.neighbours(grass_side)
    below_edge = is_dirt & ~edge & np.pad(edge, ((1, 0), (0, 0)))[:-1]
    image[edge] = pg.DIRT[1]
    image[below_edge] = pg.DIRT[2]
    image[grass_side & pg.neighbours(is_dirt)] = pg.GRASS[1]
    ys, xs = np.nonzero(edge)
    hang = rng.random(len(ys))
    for y, x, roll in zip(ys, xs, hang):
        if roll < 0.28:
            image[y, x] = pg.GRASS[4]
            if y + 1 < is_dirt.shape[0] and is_dirt[y + 1, x] and roll < 0.14:
                image[y + 1, x] = pg.GRASS[3]


def woods_floor(image, area, rng):
    """
    숲 바닥(나무 그늘): 짙은 풀과 이끼 얼룩, 떨어진 잎(갈색·누런 점), 잔가지. 숲 가장자리는 풀밭과 성기게 섞인다.
    """
    height, width = area.shape
    shade = pg.fractal_noise(height, width, rng, ((40, 0.6), (12, 0.4)))
    floor = pg.GRASS[1 + pg.dither_levels(shade, (0.45, 0.7), 0.1)].copy()
    moss = pg.fractal_noise(height, width, rng, ((10, 0.7), (4, 0.3))) > 0.66
    floor[moss] = pg.GRASS[3]
    leaves = rng.random((height, width))
    floor[leaves < 0.035] = pg.BARK[3]
    floor[(leaves >= 0.035) & (leaves < 0.05)] = pg.DIRT[4]
    floor[(leaves >= 0.05) & (leaves < 0.058)] = pg.LEAF[5]
    for _ in range(int(height * width / 900)):
        x, y = int(rng.integers(2, width - 6)), int(rng.integers(1, height - 1))
        length = int(rng.integers(3, 7))
        floor[y, x:x + length] = pg.BARK[2]
    edge = distance_inside(area, 6)
    blend = area & ((edge > 5) | (pg.bayer(height, width) * 6 < edge))
    image[blend] = floor[blend]


def tall_grass(image, area, rng):
    """키 큰 풀숲: 어두운 바탕에 길게 선 풀잎이 빽빽하다. 풀잎 끝은 숲 위쪽 경계를 조금 넘는다."""
    image[area] = pg.GRASS[1]
    ys, xs = np.nonzero(area)
    height, width = area.shape
    for i in rng.choice(len(xs), int(len(xs) / 5), replace=False):
        x, y = xs[i], ys[i]
        blade = rng.integers(4, 8)
        lean = rng.choice([-1, 0, 0, 1])
        for h in range(blade):
            px, py = x + (lean * h) // 3, y - h
            if 0 <= px < width and 0 <= py < height:
                image[py, px] = pg.GRASS[2 if h < blade * 0.35 else 4 if h < blade - 1 else 5]
    # 숲 아래 끝에 그늘 한 줄(풀숲이 땅보다 솟아 보이게)
    below = ~area & np.pad(area, ((1, 0), (0, 0)))[:-1]
    image[below] = pg.GRASS[1]


def flowers(image, cells, rng):
    """꽃밭 칸마다 꽃 몇 송이(꽃잎 넷, 노란 가운데)."""
    for row, column in zip(*np.nonzero(cells)):
        for _ in range(rng.integers(3, 6)):
            x = column * T + rng.integers(3, T - 3)
            y = row * T + rng.integers(3, T - 3)
            petal = pg.FLOWERS[rng.integers(0, len(pg.FLOWERS))]
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                image[y + dy, x + dx] = petal
            image[y, x] = pg.FLOWER_CORE
            image[y + 2, x] = pg.GRASS[2]


def water(image, area, rng):
    """
    못: 가장자리는 얕아 밝고 안쪽으로 갈수록 깊다. 북쪽 물가는 흙 둑이 물 쪽으로 보인다(위에서 비스듬히 본 시점).
    물결은 짧은 가로줄이다. 반짝임은 맵의 반짝임 효과(TileAnimator)가 따로 얹는다.
    """
    # 물가에서 멀어질수록 한 단씩 깊어진다. 단 사이 경계만 잡음과 디더로 살짝 흔들어 넓은 바다가 얼룩지지 않게 한다.
    depth = distance_inside(area, 12) + (pg.fractal_noise(*area.shape, rng, ((24, 0.7), (8, 0.3))) - 0.5) * 3
    level = 5 - pg.dither_levels(depth, (1.5, 3.5, 6.5, 10.5), 0.8)
    image[area] = pg.WATER[np.clip(level, 1, 5)][area]
    # 물결(드문 짧은 가로줄)
    ys, xs = np.nonzero(area & (depth > 2))
    for i in rng.choice(len(xs), int(len(xs) / 400), replace=False):
        y, x = ys[i], xs[i]
        length = rng.integers(2, 5)
        for dx in range(length):
            if x + dx < area.shape[1] and area[y, x + dx]:
                image[y, x + dx] = pg.WATER[5] if dx < length - 1 else pg.WATER[4]
    # 북쪽 물가: 바로 위가 땅인 물 점에서 아래로 세 점은 흙 둑
    land_above = np.zeros_like(area)
    for k in range(1, 4):
        land_above |= area & ~np.pad(area, ((k, 0), (0, 0)), constant_values=True)[:-k]
    image[land_above] = pg.DIRT[1]
    bank_top = area & ~np.pad(area, ((1, 0), (0, 0)), constant_values=True)[:-1]
    image[bank_top] = pg.DIRT[3]
    # 물가 둘레 땅에 젖은 흙 한 줄, 그 바깥 풀은 짙게
    shore = ~area & pg.neighbours(area)
    image[shore] = pg.DIRT[2]
    image[~area & ~shore & pg.neighbours(shore)] = pg.GRASS[1]


def cliff(image, area, rng):
    """
    바위 절벽(위에서 비스듬히 본 시점): 위쪽 40%는 이끼 낀 윗면, 아래는 세로 바위판이 늘어선 벽면.
    벽면 아래 풀밭에 그림자가 드리운다.
    """
    height, width = area.shape
    moss = pg.fractal_noise(height, width, rng, ((10, 0.6), (4, 0.4)))
    grain = pg.fractal_noise(height, width, rng, ((6, 0.6), (2, 0.4)))
    # 세로 바위판: 열마다 판 번호와 판 안의 가로 위치(0~1). 판마다 너비가 다르고, 한두 군데 가로로 갈라진다.
    slab_of = np.zeros(width, dtype=int)
    slab_u = np.zeros(width)
    x = 0
    slabs = 0
    while x < width:
        w = int(rng.integers(4, 13))
        slab_of[x:x + w] = slabs
        slab_u[x:x + w] = (np.arange(min(w, width - x)) + 0.5) / w
        x += w
        slabs += 1
    # 판마다 벽면 높이의 몇 % 자리에서 갈라지는지(없으면 2 → 갈라지지 않음)
    cracks = [sorted(rng.uniform(0.25, 0.8, rng.integers(0, 3))) for _ in range(slabs)]
    for column in range(width):
        rows = np.nonzero(area[:, column])[0]
        if len(rows) == 0:
            continue
        # 같은 열의 이어진 구간마다(절벽 하나) 윗면과 벽면을 나눈다.
        for run in np.split(rows, np.nonzero(np.diff(rows) > 1)[0] + 1):
            top, bottom = run[0], run[-1]
            split = top + int((bottom - top + 1) * 0.4)
            crack_rows = {split + 1 + int(c * (bottom - split - 1)) for c in cracks[slab_of[column]]}
            u = slab_u[column]
            for y in run:
                if y < split:
                    # 윗면: 거친 돌 바탕에 이끼 얼룩
                    if moss[y, column] > 0.56:
                        image[y, column] = pg.GRASS[4 if moss[y, column] > 0.68 else 3]
                    else:
                        image[y, column] = pg.STONE[4 if y == top + 1 or grain[y, column] > 0.62 else 3]
                elif y == split:
                    image[y, column] = pg.STONE[5]
                elif y in crack_rows:
                    image[y, column] = pg.STONE[0]
                elif y - 1 in crack_rows or y == split + 1:
                    image[y, column] = pg.STONE[4] if u < 0.85 else pg.STONE[2]
                else:
                    # 판 왼쪽은 밝고 오른쪽 끝은 틈. 아래로 갈수록 어둡다.
                    depth = (y - split) / max(1, bottom - split)
                    level = 0 if u > 0.86 else 3 if u < 0.28 else 2
                    if level and (depth > 0.75 or grain[y, column] < 0.3):
                        level -= 1
                    image[y, column] = pg.STONE[level]
    # 테두리와 아래 풀밭의 그림자
    ring = ~area & pg.neighbours(area)
    image[ring] = pg.OUTLINE
    shadow = np.zeros_like(area)
    for k in range(1, 5):
        shadow |= np.pad(area, ((k, 0), (0, 0)))[:-k]
    shadow &= ~area & ~ring
    image[shadow] = (image[shadow] * 0.6).astype(np.uint8)


def stairs(image, area, rng):
    """
    절벽을 오르는 돌계단: 한 단 8점, 단마다 윗가장자리는 밝고 아래 챌판은 어둡다. 양옆에 낮은 돌 난간.
    """
    ys, xs = np.nonzero(area)
    if len(ys) == 0:
        return
    wear = rng.random(area.shape)
    step = (ys % 8)
    level = np.select([step < 1, step < 2, step < 5, step < 7], [6, 5, 4, 3], 2)
    level = np.where((wear[ys, xs] < 0.12) & (step >= 2) & (step < 5), 3, level)
    image[ys, xs] = pg.STONE[np.clip(level, 0, 5)]
    cheek = area & (pg.neighbours(~area) | np.pad(~area, ((0, 0), (2, 0)), constant_values=True)[:, :-2]
                    | np.pad(~area, ((0, 0), (0, 2)), constant_values=True)[:, 2:])
    image[cheek] = pg.STONE[2]
    image[cheek & (np.arange(area.shape[0])[:, None] % 8 == 0)] = pg.STONE[4]
    image[~area & pg.neighbours(area)] = pg.OUTLINE


# ── 맵 한 장 ───────────────────────────────────────────

def draw_outdoor(tiles, seed):
    """바깥 맵(웹게임 테마 outdoor)의 바닥을 그린다. 돌려주는 값은 점 격자 RGB."""
    rng = np.random.default_rng(seed)
    kinds = np.array(tiles, dtype=object)
    rows, cols = kinds.shape
    height, width = rows * T, cols * T
    wobble = pg.fractal_noise(height, width, rng, ((12, 0.7), (5, 0.3))) - 0.5

    kinds = ground_under(kinds)

    image = grass(height, width, rng)
    if (kinds == "woods").any():
        woods_floor(image, region(kinds == "woods", wobble, 0.8), rng)
    tall_grass(image, region(kinds == "tallgrass", wobble, 0.8), rng)
    flowers(image, kinds == "flowers", rng)
    if (kinds == "sand").any():
        is_sand = region(kinds == "sand", wobble, 0.5)
        image[is_sand] = sand(height, width, rng)[is_sand]
        edge_border(image, is_sand, rng, pg.SAND[1], pg.GRASS[1])
    is_path = region(kinds == "path", wobble, 0.35)
    image[is_path] = dirt(height, width, rng)[is_path]
    path_border(image, is_path, rng)
    if (kinds == "cobble").any():
        is_cobble = region(kinds == "cobble", wobble, 0.2)
        image[is_cobble] = cobble(height, width, rng)[is_cobble]
        edge_border(image, is_cobble, rng, pg.STONE[1], pg.GRASS[1])
    # 다리는 물 위에 놓이므로 다리 칸도 물로 칠한 뒤 판자를 얹는다.
    water(image, region(np.isin(kinds, ("water", "bridge")), wobble, 0.5), rng)
    bridge(image, cell_field(kinds == "bridge") > 0.5, rng)
    cliff(image, region(kinds == "cliff", wobble, 0.25), rng)
    stairs(image, cell_field(kinds == "stairs") > 0.5, rng)

    # 칸에 놓는 작은 것(웹게임 stamp): 덤불, 바위, 표지판. 위쪽 칸부터 그려 아래 것이 앞에 온다.
    makers = {"bush": props.bush, "rock": props.rock, "sign": props.sign}
    for row, column in zip(*np.nonzero(np.isin(kinds, list(makers)))):
        sprite, pivot = makers[kinds[row, column]](np.random.default_rng(seed + row * 131 + column))
        foot_x, foot_y = column * T + T / 2, row * T + T - 2
        under = props.shadow(int(sprite.shape[1] * 0.85), 7)
        pg.stamp(image, under, foot_x + 2, foot_y + 1, (under.shape[1] / 2, under.shape[0] / 2))
        pg.stamp(image, sprite, foot_x, foot_y, pivot)
    return image


# ── 차원공간: 허공에 뜬 돌섬 ─────────────────────────────

# 돌섬 윗면(보랏빛 돌판)과 마법진 빛. 웹게임 platform 타일 색을 따랐다.
ARCANE = pg.hexes("#1c1838", "#2a2450", "#3a3360", "#453d72", "#554c88", "#6a5acd", "#8f7cff", "#d6ccff")
ISLAND_ROCK = pg.hexes("#0b0918", "#161230", "#221b45", "#30285c", "#40367a")
CRYSTAL = pg.hexes("#6a5acd", "#8f7cff", "#c9bfff", "#ffffff")


def magic_circle(height, width, cx, cy, radius, flatten):
    """돌판에 새긴 마법진: 두 겹 원, 여덟 갈래 별, 동서남북의 작은 원, 원 사이의 룬 점. 새긴 선만 True."""
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    dx, dy = (xx - cx) / radius, (yy - cy) / (radius * flatten)
    r = np.hypot(dx, dy)
    angle = np.arctan2(dy, dx)
    line = 1.1 / radius
    carved = (np.abs(r - 1) < line) | (np.abs(r - 0.84) < line)
    # 여덟 갈래 별: 45도 어긋난 두 정사각형의 테두리(꼭짓점 반지름 0.62), 가운데 작은 원
    for turn in (0, np.pi / 4):
        rx = dx * np.cos(turn) + dy * np.sin(turn)
        ry = -dx * np.sin(turn) + dy * np.cos(turn)
        square = np.maximum(np.abs(rx + ry), np.abs(rx - ry)) / np.sqrt(2)
        carved |= np.abs(square - 0.62 / np.sqrt(2)) < line * 0.9
    carved |= np.abs(r - 0.18) < line
    for a in (0, np.pi / 2, np.pi, 3 * np.pi / 2):
        nx, ny = np.cos(a) * 0.92, np.sin(a) * 0.92
        carved |= np.abs(np.hypot(dx - nx, dy - ny) - 0.075) < line
    rune = (np.round((angle + np.pi) / (2 * np.pi) * 24) % 2 == 0) & (np.abs(r - 0.92) < line * 0.8)
    carved |= rune & (np.abs(np.hypot(dx, dy) - 0.92) < line)
    return carved


def draw_void(tiles, seed):
    """
    차원공간(웹게임 테마 void): 허공에 뜬 보랏빛 돌섬. 윗면은 돌판과 은은히 빛나는 마법진, 앞쪽은 돌섬의 두께,
    그 아래로 뾰족하게 매달린 바위와 박힌 수정. 돌섬 밖은 투명해 맵 배경(MapBackdrop)이 비친다. 돌려주는 값은 점 격자 RGBA.
    """
    rng = np.random.default_rng(seed)
    kinds = np.array(tiles)
    rows, cols = kinds.shape
    height, width = rows * T, cols * T
    wobble = pg.fractal_noise(height, width, rng, ((10, 0.7), (4, 0.3))) - 0.5
    # 칸으로 적은 돌섬은 계단처럼 각지므로, 한 칸쯤 흐려 둥근 섬으로 만든 뒤 가장자리만 살짝 흔든다.
    field = box_blur(cell_field(kinds == "platform"), T // 3)
    top = field + wobble * 0.18 > 0.5
    image = np.zeros((height, width, 4), dtype=np.uint8)

    # 윗면: 돌판마다 밝기가 조금 다르고, 이음매는 어둡고, 잔금이 있다.
    owner, seams = cells(height, width, 24, rng)
    shade = rng.integers(0, 2, owner.max() + 1)
    grain = pg.fractal_noise(height, width, rng, ((8, 0.6), (3, 0.4)))
    surface = ARCANE[np.clip(3 + shade[owner] - (grain < 0.28).astype(int), 2, 4)]
    surface[seams] = ARCANE[1]
    # 돌판 이음매 바로 아래 한 점은 밝게(돌판 모서리가 빛을 받는다)
    below_seam = ~seams & np.pad(seams, ((1, 0), (0, 0)))[:-1]
    surface[below_seam] = ARCANE[5]
    # 마법진: 새긴 홈은 어둡게, 그 가운데 빛나는 선
    ys, xs = np.nonzero(top)
    cx, cy = xs.mean(), ys.mean()
    radius = (xs.max() - xs.min()) * 0.33
    carved = magic_circle(height, width, cx, cy, radius, flatten=0.62)
    groove = pg.neighbours(carved) & ~carved
    surface[groove] = ARCANE[1]
    surface[carved] = ARCANE[6]
    image[top, :3] = surface[top]
    image[top, 3] = 255

    # 가장자리: 위쪽 테두리는 밝게(빛을 받는 모서리), 좌우는 보랏빛 선
    outside = ~top
    up_edge = top & np.pad(outside, ((1, 0), (0, 0)), constant_values=True)[:-1]
    image[up_edge, :3] = ARCANE[7]
    image[top & np.pad(up_edge, ((1, 0), (0, 0)))[:-1] & ~up_edge, :3] = ARCANE[6]

    # 돌섬의 두께와 아래로 매달린 바위: 열마다 윗면 아래 끝에서 아래로 그린다.
    xs_cols = np.nonzero(top.any(axis=0))[0]
    left, right = xs_cols.min(), xs_cols.max()
    half = (right - left) / 2
    centre = (left + right) / 2
    jag = pg.noise(1, width, 7, rng)[0]
    for x in xs_cols:
        bottom = np.nonzero(top[:, x])[0].max()
        u = (x - centre) / half
        thickness = 9
        hang = int(thickness + 70 * np.sqrt(max(0.0, 1 - u * u)) ** 1.6 * (0.7 + 0.5 * jag[x]))
        for k in range(1, hang):
            y = bottom + k
            if y >= height:
                break
            if k <= thickness:
                level = 3 if k <= 2 else 2
            else:
                lit = u < -0.15
                level = 3 if lit and k < hang * 0.5 else 2 if lit or k < hang * 0.35 else 1
                if (x * 3 + k) % 11 == 0:
                    level = max(0, level - 1)
            image[y, x, :3] = ISLAND_ROCK[level]
            image[y, x, 3] = 255
        if k > thickness:
            image[min(height - 1, bottom + thickness), x, :3] = ISLAND_ROCK[1]

    # 바위에 박힌 수정 몇 개
    for _ in range(9):
        x = int(rng.uniform(left + half * 0.3, right - half * 0.3))
        rows_below = np.nonzero(image[:, x, 3] & ~top[:, x])[0]
        if len(rows_below) < 12:
            continue
        y = int(rng.uniform(rows_below.min() + 10, rows_below.max() - 2))
        size = int(rng.integers(2, 5))
        for k in range(size * 2):
            span = size - abs(k - size)
            for dx in range(-span // 2, span // 2 + 1):
                if 0 <= y - k < height and image[y - k, x + dx, 3]:
                    image[y - k, x + dx, :3] = CRYSTAL[min(3, 1 + (dx < 0) + (k > size))]

    # 둘레의 작은 바위 조각
    for _ in range(6):
        fx, fy = int(rng.uniform(0.08, 0.92) * width), int(rng.uniform(0.15, 0.9) * height)
        size = int(rng.integers(5, 10))
        if image[max(0, fy - size * 2):fy + size * 2, max(0, fx - size * 2):fx + size * 2, 3].any():
            continue
        for x in range(-size, size + 1):
            u = x / size
            depth = int(size * np.sqrt(max(0, 1 - u * u)))
            for y in range(-2, depth):
                image[fy + y, fx + x, :3] = ARCANE[4] if y < 0 else ISLAND_ROCK[3 if u < 0 else 2]
                image[fy + y, fx + x, 3] = 255

    return pg.outline(image, ISLAND_ROCK[0])


# ── 끝없이 잇는 무늬와 모서리 타일(시범 장면) ───────────────

def grass_texture(rng, size=T * pg.PERIOD):
    """끝없이 잇는 풀밭 무늬(가장자리가 이어진다)."""
    k = size // T
    shade = pg.tileable_noise(size, k, rng) * 0.55 + pg.tileable_noise(size, 3 * k, rng) * 0.3 + pg.tileable_noise(size, 8 * k, rng) * 0.15
    image = pg.GRASS[2 + pg.dither_levels(shade, (0.4, 0.6))].copy()
    sprinkle_tufts(image, shade, rng, int(size * size / 34), wrap=True)
    return image


def dirt_texture(rng, size=T * pg.PERIOD):
    """끝없이 잇는 흙 무늬(가장자리가 이어진다)."""
    k = size // T
    shade = pg.tileable_noise(size, 2 * k, rng) * 0.7 + pg.tileable_noise(size, 6 * k, rng) * 0.3
    image = pg.DIRT[2 + pg.dither_levels(shade, (0.36, 0.62), 0.08)].copy()
    for _ in range(int(size * size / 30)):
        x, y = rng.integers(0, size, 2)
        image[y, x] = pg.DIRT[2] if rng.random() < 0.6 else pg.DIRT[4]
    return image


def corner_masks(rng):
    """
    풀밭↔흙길 모서리 타일 16장의 흙 모양. 번호의 비트가 네 모서리(왼위 1, 오위 2, 왼아래 4, 오아래 8)의 흙 여부다.
    맞닿는 변은 같은 두 모서리와 같은(가장자리가 이어지는) 잡음을 보므로 어느 타일끼리 붙여도 경계가 이어진다.
    """
    wobble = pg.tileable_noise(T, 4, rng) * 0.7 + pg.tileable_noise(T, 8, rng) * 0.3 - 0.5
    u = (np.arange(T) + 0.5) / T
    masks = []
    for index in range(16):
        tl, tr, bl, br = [(index >> bit) & 1 for bit in range(4)]
        field = (tl * (1 - u[None, :]) + tr * u[None, :]) * (1 - u[:, None]) + (bl * (1 - u[None, :]) + br * u[None, :]) * u[:, None]
        masks.append(field + wobble * 0.5 * (4 * field * (1 - field)) > 0.5)
    return masks
