"""코드로 그리는 소품: 활엽수, 침엽수(그림은 trees.py), 덤불, 바위, 표지판, 발밑 그림자.

모두 점 격자(한 칸 32점)의 RGBA 배열과 기준점(발밑, 점 단위)을 돌려준다. 왼쪽 위에서 빛을 받아 몇 단계 색으로 나눠
칠하고 진한 테두리를 두른다. 같은 seed면 같은 모양이다.
"""
import numpy as np

import pixelgen as pg
import trees


def cluster_canopy(width, height, blobs, cluster, rng):
    """
    원 여러 개를 합친 수관을 잎 다발로 채운다. 큰 덩어리는 원마다 공처럼 빛을 받고, 그 위에 다발마다 작은 공처럼
    왼쪽 위가 밝고 오른쪽 아래가 어두운 결을 얹는다. 돌려주는 값: 수관 안인지, 빛 세기(0~1).
    """
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    body = np.full((height, width), -1.0)
    form = np.zeros((height, width))
    for cx, cy, r in blobs:
        d2 = (xx - cx) ** 2 + (yy - cy) ** 2
        cap = np.where(d2 <= r * r, np.sqrt(np.clip(r * r - d2, 0, None)) / r, -1)
        higher = cap > body
        lit = np.clip((xx - cx) / r * pg.LIGHT[0] + (yy - cy) / r * pg.LIGHT[1] + np.clip(cap, 0, 1) * pg.LIGHT[2], 0, 1)
        form = np.where(higher, lit, form)
        body = np.maximum(body, cap)
    inside = body >= 0
    # 잎 다발: 수관 안에 고르게 흩은 점마다 작은 공. 아래쪽 다발이 위쪽 다발을 덮는다(앞에 있다).
    count = int(inside.sum() / (cluster * cluster) * 1.4)
    ys, xs = np.nonzero(inside)
    pick = rng.choice(len(xs), count, replace=False)
    leaf = np.zeros((height, width))
    owner = np.full((height, width), -1.0)
    for px, py in sorted(zip(xs[pick], ys[pick]), key=lambda p: p[1]):
        r = cluster * rng.uniform(0.8, 1.15)
        d2 = (xx - px) ** 2 + (yy - py) ** 2
        nx, ny = (xx - px) / r, (yy - py) / r
        nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0, 1))
        shade = np.clip(nx * pg.LIGHT[0] + ny * pg.LIGHT[1] + nz * pg.LIGHT[2], 0, 1)
        newer = (d2 <= r * r) & (py >= owner)
        leaf = np.where(newer, shade, leaf)
        owner = np.where(newer, py, owner)
    leaf = np.where(owner >= 0, leaf, 0.1)
    light = leaf * 0.45 + form * 0.6 - 0.05
    return inside, np.clip(light, 0, 1)


def tree(rng, width=140, height=176):
    """둥근 활엽수(맵의 tree). 그림은 trees.broadleaf가 그린다."""
    return trees.broadleaf(rng, width, height)


def pine(rng, width=112, height=192):
    """침엽수(맵의 pine). 그림은 trees.pine이 그린다."""
    return trees.pine(rng, width, height)


def bush(rng, width=36, height=28):
    image = np.zeros((height, width, 4), dtype=np.uint8)
    cx = width / 2
    blobs = [(cx, height * 0.6, height * 0.42), (cx - width * 0.24, height * 0.7, height * 0.3), (cx + width * 0.24, height * 0.7, height * 0.3), (cx, height * 0.38, height * 0.32)]
    inside, light = cluster_canopy(width, height, blobs, cluster=4, rng=rng)
    image[inside, :3] = pg.quantize(light, pg.LEAF)[inside]
    image[inside, 3] = 255
    return pg.outline(image), (width // 2, height - 2)


def rock(rng, width=30, height=24):
    """깎인 면이 있는 바위: 위쪽은 밝은 윗면, 옆은 각도마다 다른 면, 아래쪽은 그늘."""
    image = np.zeros((height, width, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    sides = 8
    angles = np.sort(rng.random(sides) * 2 * np.pi)
    radii = 0.8 + rng.random(sides) * 0.2
    dx = (xx - width / 2) / (width / 2 - 2)
    dy = (yy - height * 0.6) / (height * 0.55 - 2)
    angle = np.arctan2(dy, dx) % (2 * np.pi)
    extended = np.concatenate([angles - 2 * np.pi, angles, angles + 2 * np.pi])
    radius = np.interp(angle, extended, np.tile(radii, 3))
    inside = np.hypot(dx, dy) < radius
    top_face = ((xx - width * 0.47) / (width * 0.33)) ** 2 + ((yy - height * 0.38) / (height * 0.22)) ** 2 < 1
    facet = np.digitize(angle, angles) % sides
    facet_angle = (angles + np.roll(angles, -1) + np.where(np.arange(sides) == sides - 1, 2 * np.pi, 0)) / 2
    face = facet_angle[facet]
    side_light = 0.45 + 0.4 * (np.cos(face) * pg.LIGHT[0] + np.sin(face) * pg.LIGHT[1]) - 0.15 * dy
    light = np.where(top_face, 0.86 - 0.1 * dy, side_light)
    image[inside, :3] = pg.quantize(np.clip(light, 0, 1), pg.STONE, 0.05)[inside]
    image[inside, 3] = 255
    seams = inside & (np.abs(np.diff(np.pad(facet, ((0, 0), (1, 0)), mode="edge"), axis=1)) > 0) & ~top_face
    image[seams, :3] = pg.STONE[1]
    return pg.outline(image), (width // 2, height - 2)


def sign(rng, width=26, height=28):
    """나무 표지판: 기둥 하나에 판자 하나. 판자에는 글씨 대신 어두운 줄 두 개."""
    image = np.zeros((height, width, 4), dtype=np.uint8)
    cx = width // 2
    image[12:height - 1, cx - 1:cx + 2, :3] = pg.WOOD[2]
    image[12:height - 1, cx - 1, :3] = pg.WOOD[3]
    image[12:height - 1, cx - 1:cx + 2, 3] = 255
    board = (slice(3, 15), slice(2, width - 2))
    image[board + (slice(0, 3),)] = pg.WOOD[3]
    image[board + (3,)] = 255
    image[3, 2:width - 2, :3] = pg.WOOD[4]
    image[14, 2:width - 2, :3] = pg.WOOD[1]
    for row in (7, 10):
        image[row, 6:width - 6, :3] = pg.WOOD[1]
    return pg.outline(image), (cx, height - 2)


def shadow(width, height, alpha=96):
    """소품 발밑 그림자(땅 위에 반투명 타원)."""
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    d = ((xx - width / 2) / (width / 2)) ** 2 + ((yy - height / 2) / (height / 2)) ** 2
    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[d < 1] = [10, 20, 12, alpha]
    return image
