"""코드로 그리는 나무 여러 종(나무가 조잡하다, 여러 종류의 나무로 숲 같은 구역을).

  broadleaf(rng, w, h)  둥근 활엽수(맵의 tree)
  oak(rng, w, h)        넓게 퍼진 참나무: 굵은 줄기와 드러난 뿌리, 짙은 잎
  maple(rng, w, h)      밝은 연두 잎의 둥근 나무
  birch(rng, w, h)      흰 껍질에 검은 무늬, 가늘고 성긴 수관의 자작나무
  pine(rng, w, h)       층층이 처진 가지의 소나무(맵의 pine)
  spruce(rng, w, h)     좁고 높은 짙은 가문비나무
  sapling(rng, w, h)    어린 나무

모두 점 격자(한 칸 32점)의 RGBA와 기준점(줄기 밑동 가운데, 점)을 돌려준다. 빛은 왼쪽 위에서 온다.
잎 수관은 큰 덩어리마다 공처럼 빛을 받고, 그 위에 잎 다발(작은 공)의 결, 테두리의 울퉁불퉁한 잎 끝, 밝은 쪽의 반짝이는 잎,
아래쪽 그늘을 얹는다. 침엽수는 아래로 처진 가지 다발을 층층이 겹쳐 쌓는다.
"""
import numpy as np

import pixelgen as pg

OUTLINE = np.array([16, 24, 18], dtype=np.uint8)
OAK = pg.hexes("#14251a", "#1d3622", "#29492a", "#376032", "#48783a", "#5f9046", "#82ab58")
MAPLE = pg.hexes("#25351a", "#364d20", "#4d6a26", "#69882e", "#89a43a", "#acbf4e", "#d2d876")
BIRCH_LEAF = pg.hexes("#22391f", "#32502a", "#466b33", "#5d853c", "#77a047", "#96ba58", "#bcd47a")
SPRUCE = pg.hexes("#0d1a19", "#142623", "#1c342f", "#26443c", "#33564b", "#466c5d")
BIRCH_BARK = pg.hexes("#2e2b28", "#6f6a62", "#b9b4a8", "#e6e2d8")


def lumpy_blobs(width, height, blobs, rng, roughness):
    """원 여러 개를 합친 덩어리. 테두리를 잡음으로 흔들어 울퉁불퉁하게 한다. 돌려주는 값: 안쪽 마스크, 덩어리 빛(공처럼)."""
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    edge = (pg.fractal_noise(height, width, rng, ((7, 0.6), (3, 0.4))) - 0.5) * roughness
    body = np.full((height, width), -1.0)
    form = np.zeros((height, width))
    for cx, cy, r in blobs:
        d = np.hypot(xx - cx, yy - cy) / r
        cap = np.where(d <= 1 + edge, np.sqrt(np.clip(1 - np.minimum(d, 1) ** 2, 0, 1)), -1)
        higher = cap > body
        lit = np.clip((xx - cx) / r * pg.LIGHT[0] + (yy - cy) / r * pg.LIGHT[1] + np.clip(cap, 0, 1) * pg.LIGHT[2], 0, 1)
        form = np.where(higher, lit, form)
        body = np.maximum(body, cap)
    return body >= 0, form


def leaf_clusters(inside, cluster, rng):
    """수관 안에 고르게 흩은 잎 다발(작은 공). 아래 다발이 위 다발을 덮는다. 돌려주는 값: 다발 빛, 다발 경계(짙은 틈)."""
    height, width = inside.shape
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    ys, xs = np.nonzero(inside)
    count = max(1, int(len(xs) / (cluster * cluster) * 1.6))
    pick = rng.choice(len(xs), min(count, len(xs)), replace=False)
    leaf = np.full((height, width), 0.15)
    owner = np.full((height, width), -1.0)
    rim = np.zeros((height, width), dtype=bool)
    for px, py in sorted(zip(xs[pick], ys[pick]), key=lambda p: p[1]):
        r = cluster * rng.uniform(0.75, 1.2)
        nx, ny = (xx - px) / r, (yy - py) / r
        d2 = nx * nx + ny * ny
        newer = (d2 <= 1) & (py >= owner)
        nz = np.sqrt(np.clip(1 - d2, 0, 1))
        shade = np.clip(nx * pg.LIGHT[0] + ny * pg.LIGHT[1] + nz * pg.LIGHT[2], 0, 1)
        leaf = np.where(newer, shade, leaf)
        rim = np.where(newer, (d2 > 0.72) & (ny > -0.1), rim)
        owner = np.where(newer, py, owner)
    return leaf, rim


def leafy_crown(width, height, blobs, cluster, rng, palette, roughness=0.35, airy=0.0):
    """
    잎 수관 한 덩어리(RGBA): 덩어리 빛과 다발 결을 섞고, 아래쪽을 그늘지게, 다발 경계를 짙게, 밝은 쪽에 반짝이는 잎,
    테두리에 삐죽 나온 잎 끝을 둔다. airy만큼 수관 가장자리 안쪽에 구멍을 낸다(성긴 나무).
    """
    inside, form = lumpy_blobs(width, height, blobs, rng, roughness)
    leaf, rim = leaf_clusters(inside, cluster, rng)
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    ys = np.nonzero(inside)[0]
    top, bottom = ys.min(), ys.max()
    under = np.clip((yy - (top + (bottom - top) * 0.55)) / ((bottom - top) * 0.45 + 1), 0, 1)
    light = leaf * 0.5 + form * 0.6 - under * 0.32 - 0.04
    light = np.where(rim, light - 0.18, light)
    if airy:
        holes = (pg.fractal_noise(height, width, rng, ((5, 0.7), (2, 0.3))) < airy) & (form < 0.75)
        inside &= ~holes
    image = np.zeros((height, width, 4), dtype=np.uint8)
    levels = pg.quantize(np.clip(light, 0, 1), palette, dither=0.14)
    image[inside, :3] = levels[inside]
    image[inside, 3] = 255
    # 밝은 쪽의 반짝이는 잎, 어두운 쪽의 짙은 잎
    sparkle = inside & (light > 0.66) & (rng.random((height, width)) < 0.06)
    image[sparkle, :3] = palette[-1]
    dark = inside & (light < 0.3) & (rng.random((height, width)) < 0.05)
    image[dark, :3] = palette[0]
    # 테두리 바깥으로 삐죽 나온 잎 끝(한두 점)
    border = pg.neighbours(inside) & ~inside
    ys, xs = np.nonzero(border)
    for i in rng.choice(len(ys), len(ys) // 7, replace=False):
        y, x = ys[i], xs[i]
        lit_side = (x < width / 2) and (y < height * 0.6)
        image[y, x, :3] = palette[4 if lit_side else 2]
        image[y, x, 3] = 255
    return image, inside


def trunk(image, cx, top, bottom, half_top, half_bottom, bark=pg.BARK, roots=True, lean=0.0):
    """
    아래로 조금 벌어지는 줄기: 왼쪽이 밝고 오른쪽이 어두운 세로 나무결과 옹이. roots면 밑동 양옆으로 짧은 뿌리 갈래가 뻗는다.
    lean만큼 위로 갈수록 옆으로 기운다.
    """
    height, width = image.shape[:2]
    for y in range(int(top), int(bottom)):
        t = (y - top) / max(1, bottom - top)
        half = half_top + (half_bottom - half_top) * t ** 2
        center = cx + lean * (1 - t)
        left, right = int(round(center - half)), int(round(center + half))
        for x in range(max(0, left), min(width, right + 1)):
            u = (x - left) / max(1, right - left)
            level = 3 if u < 0.28 else 2 if u < 0.68 else 1
            if (x * 3 + (y // 4) * 5) % 7 == 0:
                level = max(0, level - 1)
            image[y, x, :3] = bark[min(len(bark) - 1, level + 1)]
            image[y, x, 3] = 255
    if roots:
        # 밑동에서 양옆으로 뻗어 땅에 묻히는 뿌리 갈래(바깥으로 갈수록 가늘고 낮다)
        for side in (-1, 1):
            for k, length in enumerate((half_bottom * 1.5, half_bottom * 0.9)):
                for step in range(int(length)):
                    x = int(round(cx + side * (half_bottom * 0.5 + step)))
                    lift = int(round((1 - step / length) * (3 - k)))
                    for y in range(int(bottom) - 1 - lift, int(bottom)):
                        if 0 <= x < width:
                            image[y, x, :3] = bark[2 if side < 0 else 1]
                            image[y, x, 3] = 255


def branches(image, cx, from_y, tips, bark=pg.BARK, thickness=2):
    """줄기 꼭대기에서 수관 속으로 갈라져 올라가는 가지(수관 틈으로 보인다)."""
    height, width = image.shape[:2]
    for tx, ty in tips:
        steps = int(max(abs(tx - cx), abs(ty - from_y))) + 1
        for k in range(steps):
            t = k / max(1, steps - 1)
            x = cx + (tx - cx) * t
            y = from_y + (ty - from_y) * t
            w = max(1, int(round(thickness * (1 - t * 0.6))))
            for dx in range(w):
                px, py = int(round(x)) + dx, int(round(y))
                if 0 <= px < width and 0 <= py < height:
                    image[py, px, :3] = bark[1 if dx else 2]
                    image[py, px, 3] = 255


def under_shadow(image, inside):
    """수관 바로 아래 줄기에 드리운 짙은 그늘(두세 줄)."""
    shadow = np.zeros_like(inside)
    for k in range(1, 4):
        shadow |= np.pad(inside, ((k, 0), (0, 0)))[:-k]
    shadow &= ~inside & (image[..., 3] > 0)
    image[shadow, :3] = (image[shadow, :3] * 0.55).astype(np.uint8)


def compose(width, height, crown, crown_inside, trunk_image):
    """줄기 그림 위에 수관을 덮고 수관 아래 그늘을 드리운 뒤 테두리를 두른다."""
    image = trunk_image
    under_shadow(image, crown_inside)
    image[crown_inside] = crown[crown_inside]
    return pg.outline(image, OUTLINE)


def round_crown_blobs(width, crown_bottom, rng, layout):
    blobs = []
    for u, v, r in layout:
        radius = r * width * rng.uniform(0.9, 1.06)
        x = u * width + rng.uniform(-0.035, 0.035) * width
        y = v * crown_bottom + rng.uniform(-0.03, 0.03) * crown_bottom
        radius = min(radius, x - 3, width - 4 - x, y - 3)
        blobs.append((x, y, radius))
    return blobs


def leafy_tree(rng, width, height, layout, crown_share, palette, trunk_top, trunk_half, cluster, roughness=0.35,
               airy=0.0, bark=pg.BARK, lean=0.0, fork=True):
    """활엽수 한 그루: 줄기·가지를 먼저 그리고 수관을 덮는다."""
    image = np.zeros((height, width, 4), dtype=np.uint8)
    cx = width / 2
    crown_bottom = height * crown_share
    trunk(image, cx, int(height * trunk_top), height - 1, trunk_half[0] * width, trunk_half[1] * width, bark, lean=lean)
    if fork:
        branches(image, cx + lean * 0.3, int(height * trunk_top) + 4,
                 [(cx - width * 0.22, crown_bottom * 0.45), (cx + width * 0.2, crown_bottom * 0.4), (cx + width * 0.02, crown_bottom * 0.3)],
                 bark, thickness=max(2, int(width * 0.03)))
    blobs = round_crown_blobs(width, crown_bottom, rng, layout)
    crown, inside = leafy_crown(width, height, blobs, cluster, rng, palette, roughness, airy)
    return compose(width, height, crown, inside, image), (width // 2, height - 1)


def broadleaf(rng, width=140, height=176):
    """둥근 활엽수: 덩어리 여섯으로 된 둥근 수관과 아래로 벌어지는 줄기(맵의 tree)."""
    layout = [(0.5, 0.42, 0.32), (0.26, 0.56, 0.22), (0.74, 0.56, 0.22), (0.34, 0.25, 0.21), (0.67, 0.26, 0.2), (0.5, 0.7, 0.2)]
    return leafy_tree(rng, width, height, layout, 0.8, pg.LEAF, 0.6, (0.045, 0.075), width * 0.07)


def oak(rng, width=168, height=192):
    """참나무: 옆으로 넓게 퍼진 짙은 수관, 굵고 짧은 줄기와 드러난 뿌리."""
    layout = [(0.5, 0.45, 0.28), (0.22, 0.56, 0.2), (0.78, 0.56, 0.2), (0.33, 0.3, 0.2), (0.66, 0.3, 0.2), (0.12, 0.66, 0.12),
              (0.88, 0.66, 0.12), (0.5, 0.68, 0.22), (0.5, 0.2, 0.17)]
    return leafy_tree(rng, width, height, layout, 0.78, OAK, 0.62, (0.055, 0.085), width * 0.06, roughness=0.4)


def maple(rng, width=128, height=168):
    """밝은 연두 잎의 둥근 나무: 조금 위로 솟은 수관, 가는 줄기."""
    layout = [(0.5, 0.4, 0.3), (0.28, 0.52, 0.22), (0.72, 0.52, 0.22), (0.4, 0.22, 0.2), (0.62, 0.24, 0.19), (0.5, 0.66, 0.2)]
    return leafy_tree(rng, width, height, layout, 0.78, MAPLE, 0.6, (0.04, 0.065), width * 0.065, roughness=0.32)


def birch(rng, width=96, height=184):
    """자작나무: 흰 껍질에 검은 가로 무늬가 있는 가는 줄기, 위로 길고 성긴 연두 수관(틈으로 가지가 보인다)."""
    image = np.zeros((height, width, 4), dtype=np.uint8)
    cx = width / 2
    lean = rng.uniform(-3, 3)
    trunk(image, cx, int(height * 0.2), height - 1, width * 0.045, width * 0.065, BIRCH_BARK, roots=False, lean=lean)
    # 검은 껍질 무늬: 줄기를 가로지르는 짧은 띠
    for _ in range(22):
        y = int(rng.uniform(height * 0.25, height - 4))
        row = np.nonzero(image[y, :, 3])[0]
        if len(row) > 2:
            x0 = int(rng.integers(row.min(), row.max()))
            image[y, x0:x0 + int(rng.integers(2, 5)), :3] = BIRCH_BARK[0]
    branches(image, cx + lean * 0.5, int(height * 0.45), [(cx - width * 0.25, height * 0.18), (cx + width * 0.26, height * 0.22),
                                                            (cx - width * 0.1, height * 0.08)], BIRCH_BARK, thickness=2)
    layout = [(0.5, 0.36, 0.3), (0.32, 0.5, 0.24), (0.68, 0.48, 0.24), (0.45, 0.17, 0.22), (0.6, 0.62, 0.2), (0.38, 0.66, 0.18)]
    blobs = round_crown_blobs(width, height * 0.66, rng, layout)
    crown, inside = leafy_crown(width, height, blobs, width * 0.08, rng, BIRCH_LEAF, roughness=0.6)
    return compose(width, height, crown, inside, image), (width // 2, height - 1)


def sapling(rng, width=60, height=88):
    """어린 나무: 가는 줄기와 작은 둥근 수관."""
    layout = [(0.5, 0.42, 0.34), (0.3, 0.6, 0.2), (0.7, 0.58, 0.2)]
    palette = MAPLE if rng.random() < 0.5 else pg.LEAF
    return leafy_tree(rng, width, height, layout, 0.62, palette, 0.5, (0.04, 0.07), width * 0.1, roughness=0.3, fork=False)


def conifer(rng, width, height, palette, layers, crown_top, crown_bottom, trunk_rows, droop, ragged):
    """
    침엽수: 아래 층부터 위로, 층마다 줄기에서 바깥·아래로 처진 가지 다발을 펼친다. 위 층이 아래 층의 윗부분을 덮는다.
    층마다 너비와 좌우가 조금씩 달라 반듯하지 않고, 아래 끝은 뾰족한 가지 끝이 톱니처럼 늘어진다. 바늘잎은 짧은 점 결이다.
    왼쪽 윗면이 밝고 오른쪽과 층 아래쪽이 어둡다.
    """
    image = np.zeros((height, width, 4), dtype=np.uint8)
    cx = width / 2
    trunk(image, cx, height - trunk_rows, height - 1, width * 0.03, width * 0.05, roots=False)
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    needles = rng.random((height, width))
    covered = np.zeros((height, width), dtype=bool)
    for i in reversed(range(layers)):
        t = (i + 1) / layers
        base = crown_top + (crown_bottom - crown_top) * t + rng.uniform(-2, 2)
        reach = (width / 2 - 3) * (0.16 + 0.84 * t)
        left_half, right_half = reach * rng.uniform(0.82, 1.0), reach * rng.uniform(0.82, 1.0)
        half = np.where(xx < cx, left_half, right_half)
        rise = (crown_bottom - crown_top) / layers * 1.8
        u = (xx - cx) / np.maximum(half, 1)
        # 아래 끝: 바깥으로 갈수록 처지고, 가지 끝마다 뾰족한 톱니(삼각파)
        period = rng.uniform(5, 7)
        saw = np.abs(((xx + i * 3) % period) / period - 0.5) * 2
        lower = base + droop * np.abs(u) ** 1.5 + saw * ragged * (0.4 + np.abs(u))
        upper = base - rise * (1 - np.abs(u) ** 0.8)
        shape = (np.abs(u) <= 1) & (yy >= upper) & (yy <= lower)
        # 바깥 끝은 가지 끝이 갈라져 들쭉날쭉하다
        shape &= ~((np.abs(u) > 0.88) & (needles < 0.35))
        v = np.clip((yy - upper) / np.maximum(lower - upper, 1), 0, 1)
        light = 0.72 - u * 0.32 - v * 0.42
        light = np.where(needles < 0.16, light - 0.16, np.where(needles > 0.9, light + 0.1, light))
        image[shape, :3] = pg.quantize(np.clip(light, 0, 1), palette, dither=0.1)[shape]
        image[shape, 3] = 255
        # 위 층이 바로 아래 층에 드리운 그늘
        shade = covered & ~shape & (yy > lower) & (yy < lower + 3)
        image[shade, :3] = palette[0]
        covered |= shape
    # 꼭대기 순
    tip = (np.abs(xx - cx) <= 1.5) & (yy >= crown_top - 6) & (yy < crown_top + 4)
    image[tip, :3] = palette[3]
    image[tip, 3] = 255
    return pg.outline(image, OUTLINE), (width // 2, height - 1)


def pine(rng, width=112, height=192):
    """소나무: 처진 가지 다발이 여섯 층, 조금 둥근 원뿔(맵의 pine)."""
    return conifer(rng, width, height, pg.NEEDLE, 6, 8, height - 26, 26, droop=10, ragged=5)


def spruce(rng, width=96, height=224):
    """가문비나무: 좁고 높고 짙은 원뿔, 가지가 많이 처진 아홉 층."""
    return conifer(rng, width, height, SPRUCE, 9, 8, height - 20, 20, droop=12, ragged=5)


# ── 사막(8장 힐리아 서부) ──────────────────────────────────

PALM_LEAF = pg.hexes("#16301c", "#21462a", "#2f6034", "#3f7a3e", "#58964a", "#7cb25a")
PALM_BARK = pg.hexes("#3a2616", "#5a3c22", "#7a5632", "#9a7046", "#b88c5c")
CACTUS = pg.hexes("#123222", "#1c4a30", "#28623c", "#387c4a", "#4e985c", "#76b874")


def palm(rng, width=120, height=176):
    """야자수: 마디진 줄기가 한쪽으로 휘어 오르고, 꼭대기에서 잎줄기 여덟이 아래로 늘어지며, 잎줄기 밑에 열매가 달린다."""
    image = np.zeros((height, width, 4), dtype=np.uint8)
    bend = rng.uniform(-0.18, 0.18) * width
    top_y = int(height * 0.3)
    crown_x = width / 2 + bend
    # 줄기: 아래에서 위로 휘는 곡선, 마디마다 밝고 어두운 띠
    for y in range(top_y, height):
        t = (height - 1 - y) / (height - 1 - top_y)
        x = width / 2 + bend * t ** 1.6
        half = 4.5 - 1.5 * t
        x0, x1 = int(round(x - half)), int(round(x + half))
        ring = (height - y) % 7
        for xx in range(x0, x1 + 1):
            u = (xx - x0) / max(1, x1 - x0)
            level = 3 if u < 0.35 else 2 if u < 0.75 else 1
            if ring == 0:
                level = max(0, level - 1)
            elif ring == 1:
                level = min(4, level + 1)
            image[y, xx, :3] = PALM_BARK[level]
            image[y, xx, 3] = 255
    # 잎줄기: 꼭대기에서 바깥으로 뻗었다가 처지는 곡선. 곡선을 따라 양쪽으로 아래로 처진 넓은 잎을 촘촘히 달아 잎이 부채처럼 찬다.
    # 뒤쪽 잎줄기(어둡게)를 먼저, 앞쪽(밝게)을 나중에 그린다.
    yy, xx = np.mgrid[0:height, 0:width]

    def put(px, py, level):
        px, py = int(round(px)), int(round(py))
        if 0 <= px < width and 0 <= py < height:
            image[py, px, :3] = PALM_LEAF[int(np.clip(level, 0, 5))]
            image[py, px, 3] = 255

    for layer, count, shade in ((0, 6, -1), (1, 7, 0)):
        for k in range(count):
            angle = -np.pi / 2 + (k - (count - 1) / 2) / count * np.pi * (1.5 + 0.2 * layer) + rng.uniform(-0.12, 0.12)
            reach = width * rng.uniform(0.34, 0.46)
            droop = height * rng.uniform(0.1, 0.2)
            for step in range(70):
                t = step / 69
                fx = crown_x + np.cos(angle) * reach * t
                fy = top_y + np.sin(angle) * reach * t * 0.5 + droop * t * t
                length = 11 * (1 - t) ** 0.7 + 3
                for side in (-1, 1):
                    # 잎은 잎줄기에서 바깥 아래로 처진다
                    dx, dy = side * np.sin(angle) * 0.55 + np.cos(angle) * 0.25, 0.85
                    for s_ in np.linspace(0, 1, int(length) + 1):
                        level = (4 if side < 0 else 3) - (1 if s_ > 0.6 else 0) + shade
                        put(fx + dx * length * s_, fy + dy * length * s_, level)
                put(fx, fy, 5 + shade)
                put(fx, fy - 1, 4 + shade)
    # 꼭대기 잎 뭉치
    tuft = ((xx - crown_x) ** 2 / 64 + (yy - top_y) ** 2 / 25) <= 1
    image[tuft, :3] = PALM_LEAF[3]
    image[tuft & (xx < crown_x) & (yy < top_y), :3] = PALM_LEAF[4]
    image[tuft, 3] = 255
    # 열매 셋
    for dx in (-5, 0, 5):
        nut = ((xx - (crown_x + dx)) ** 2 + (yy - (top_y + 6)) ** 2) <= 9
        image[nut, :3] = PALM_BARK[1]
        image[nut, 3] = 255
        image[nut & (xx < crown_x + dx) & (yy < top_y + 6), :3] = PALM_BARK[3]
    return pg.outline(image, OUTLINE), (width // 2, height - 1)


def cactus(rng, width=48, height=80):
    """기둥 선인장: 둥근 끝의 굵은 줄기에 팔 한두 개, 세로 골과 가시 점, 왼쪽이 밝다."""
    image = np.zeros((height, width, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:height, 0:width]
    cx = width / 2
    body = (np.abs(xx + 0.5 - cx) <= 7) & (yy >= 10) & (yy < height - 1)
    body |= ((xx + 0.5 - cx) ** 2 + (yy - 10) ** 2) <= 49
    arms = [(-1, rng.uniform(0.35, 0.55)), (1, rng.uniform(0.25, 0.45))] if rng.random() < 0.7 else [(1, rng.uniform(0.3, 0.5))]
    for side, at in arms:
        y = int(height * (1 - at))
        x_out = cx + side * 15
        body |= (np.abs(yy + 0.5 - y) <= 4) & ((xx - cx) * side >= 0) & ((xx - cx) * side <= 15)
        top = y - int(rng.uniform(12, 20))
        body |= (np.abs(xx + 0.5 - x_out) <= 4) & (yy >= top) & (yy <= y + 4)
        body |= ((xx + 0.5 - x_out) ** 2 + (yy - top) ** 2) <= 16
    lit = np.clip(0.75 - (xx - cx) / 18 + ((xx // 3) % 2) * -0.15, 0, 0.99)
    image[body, :3] = pg.quantize(lit, CACTUS[1:], 0.1)[body]
    image[body, 3] = 255
    spines = body & (rng.random((height, width)) < 0.015)
    image[spines, :3] = pg.hexes("#e8e2b0")[0]
    return pg.outline(image, OUTLINE), (int(cx), height - 1)
