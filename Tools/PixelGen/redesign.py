"""그림 갈아엎기 시범: 입체감 있는 집(모임지붕·처마·굴뚝)과 자연스러운 바위 언덕.

웹게임 칸 모양을 그대로 따라 그리면 지붕이 납작한 직사각형, 절벽이 담장처럼 보인다(사용자 지적 2026-11-02).
여기서는 칸 자리(집이 서는 칸, 바위가 막는 칸)만 지키고 그림은 칸 밖으로도 자연스럽게 나가게 그린다.
"""
import numpy as np

import pixelgen as pg
import terrain

T = pg.TILE
ROOF = pg.hexes("#2e1310", "#4d1d16", "#6e281c", "#8e3523", "#ab452b", "#c65b36", "#de7a4c", "#ef9d6c")
PLASTER = pg.hexes("#9c8a6c", "#b9a785", "#d2c19c", "#e4d6b3", "#f1e7ca")
CHIMNEY = pg.hexes("#3a3236", "#55494c", "#716166", "#8f7e80")
ROCK = pg.hexes("#1e1c24", "#2f2c37", "#433f4c", "#5a5563", "#736d7c", "#8f8898", "#aca5b3", "#c8c2cd")


def canvas(width, height):
    return np.zeros((height, width, 4), dtype=np.uint8)


def put(image, mask, color):
    image[mask, :3] = color
    image[mask, 3] = 255


# ── 집 ────────────────────────────────────────────────

def house(columns, roof_rows, wall_rows, windows, doors, rng):
    """
    모임지붕 집(위에서 비스듬히 본 시점). 벽은 칸 자리 그대로, 지붕은 처마가 벽보다 양옆·앞으로 나오고
    용마루가 처마보다 짧아 양옆에 세모난 지붕면이 보인다(왼쪽은 빛, 오른쪽은 그늘). 굴뚝이 지붕을 뚫고 솟는다.
    돌려주는 값: 그림, 그림 안에서 벽 왼쪽 아래(칸 자리의 왼쪽 아래)의 점 좌표.
    """
    eave = 7                                  # 처마가 벽 밖으로 나온 점 수
    wall_w, wall_h = columns * T, wall_rows * T
    roof_h = roof_rows * T + 10               # 지붕은 칸보다 조금 더 위로 솟는다
    width = wall_w + eave * 2
    height = roof_h + wall_h + 2
    image = canvas(width, height)
    wall_top = roof_h - 4                     # 처마가 벽 윗부분을 조금 덮는다
    x0 = eave

    # 벽: 회반죽, 나무 기둥·들보, 돌 받침
    ys, xs = np.mgrid[0:height, 0:width]
    wall = (xs >= x0) & (xs < x0 + wall_w) & (ys >= wall_top) & (ys < height - 2)
    grain = pg.noise(height, width, 5, rng)
    image[wall, :3] = PLASTER[2 + (grain > 0.65).astype(int) - (grain < 0.3).astype(int)][wall]
    image[wall, 3] = 255
    beam = pg.BARK[2]
    for x in list(range(x0, x0 + wall_w, T)) + [x0 + wall_w - 3]:
        put(image, wall & (xs >= x) & (xs < x + 3), beam)
        put(image, wall & (xs == x), pg.BARK[3])
    put(image, wall & (ys >= wall_top + wall_h // 2 - 1) & (ys < wall_top + wall_h // 2 + 1), beam)
    base = height - 2 - 7
    put(image, wall & (ys >= base), pg.STONE[2])
    put(image, wall & (ys == base), pg.STONE[4])
    put(image, wall & (ys > base) & (xs % 7 == 0), pg.STONE[1])
    for column in windows:
        window(image, x0 + column * T + T // 2, wall_top + 14)
    for column in doors:
        door(image, x0 + column * T + T // 2, height - 2)
    # 처마 그늘이 벽 윗부분에 드리운다
    shade = wall & (ys < wall_top + 9)
    image[shade, :3] = (image[shade, :3] * np.linspace(0.55, 0.9, 9)[ys[shade] - wall_top][:, None]).astype(np.uint8)

    roof(image, width, roof_h, eave, rng)
    chimney(image, int(width * 0.72), roof_h)
    # 땅에 닿는 곳 그림자
    put(image, (ys >= height - 2) & (xs >= x0 - 2) & (xs < x0 + wall_w + 2), np.array([24, 30, 22]))
    image[height - 2:, :, 3] = np.where(image[height - 2:, :, 3] > 0, 110, 0)
    return pg.outline(image), (x0, height - 2)


def roof(image, width, height, eave, rng):
    """
    모임지붕: 앞 지붕면(기와 줄)과 양옆 지붕면(왼쪽은 빛, 오른쪽은 그늘)이 모서리 선에서 만난다.
    모서리는 처마 양끝에서 용마루 양끝으로 비스듬히 올라가, 위로 갈수록 옆 지붕면이 넓어진다.
    용마루 너머로는 뒤 지붕면이 얇게 보이고, 처마 끝에는 판자가 있다.
    """
    ridge_y = 8
    inset = int(min(width * 0.25, (height - ridge_y) * 0.8))
    for y in range(height):
        if y < ridge_y:
            side = int(round((ridge_y - y) * 1.6))
            image[y, side:width - side, :3] = ROOF[2]
            image[y, side:width - side, 3] = 255
            continue
        t = (y - ridge_y) / (height - ridge_y)
        hip = int(round(inset * (1 - t)))
        course, within = (y - ridge_y) // 5, (y - ridge_y) % 5
        for x in range(width):
            if x < hip:
                level = 6 if within else 5
            elif x >= width - hip:
                level = 2 if within else 1
            else:
                level = 5 - int(t * 1.8)
                if within == 4:
                    level -= 2
                elif within == 0:
                    level += 1
                if (x + course * 4) % 9 == 0 and within < 4:
                    level -= 1
            image[y, x, :3] = ROOF[int(np.clip(level, 0, 7))]
            image[y, x, 3] = 255
        if 0 < hip < width:
            image[y, hip, :3] = ROOF[7]
            image[y, width - hip - 1, :3] = ROOF[3]
    image[ridge_y - 2:ridge_y + 1, inset:width - inset, :3] = ROOF[7]
    image[ridge_y + 1, inset:width - inset, :3] = ROOF[3]
    image[height - 3:height, :, :3] = pg.WOOD[1]
    image[height - 3, :, :3] = pg.WOOD[3]


def chimney(image, cx, roof_h):
    """돌 굴뚝: 지붕 앞면 위로 솟고, 위에 테두리 돌."""
    top, bottom = 0, int(roof_h * 0.45)
    image[top:bottom, cx - 5:cx + 5, :3] = CHIMNEY[2]
    image[top:bottom, cx - 5:cx - 3, :3] = CHIMNEY[3]
    image[top:bottom, cx + 3:cx + 5, :3] = CHIMNEY[1]
    image[top:bottom, cx - 5:cx + 5, 3] = 255
    for y in range(top + 4, bottom, 4):
        image[y, cx - 5:cx + 5, :3] = CHIMNEY[1]
    image[top:top + 3, cx - 6:cx + 6, :3] = CHIMNEY[3]
    image[top:top + 3, cx - 6:cx + 6, 3] = 255
    image[top + 1:top + 3, cx - 3:cx + 3, :3] = CHIMNEY[0]


def window(image, cx, cy):
    w, h = 14, 13
    x0, y0 = cx - w // 2, cy - h // 2
    image[y0:y0 + h, x0:x0 + w, :3] = pg.WOOD[1]
    image[y0 + 2:y0 + h - 2, x0 + 2:x0 + w - 2, :3] = pg.hexes("#28374f")[0]
    image[y0 + 2:y0 + 5, x0 + 2:x0 + 6, :3] = pg.hexes("#7c9cc0")[0]
    image[y0 + 2:y0 + h - 2, cx, :3] = pg.WOOD[2]
    image[cy, x0 + 2:x0 + w - 2, :3] = pg.WOOD[2]
    image[y0 + h:y0 + h + 2, x0 - 1:x0 + w + 1, :3] = pg.WOOD[3]
    image[y0 - 1:y0 + h + 2, x0 - 1:x0 + w + 1, 3] = 255


def door(image, cx, bottom):
    w, h = 16, 27
    x0, y0 = cx - w // 2, bottom - h - 2
    for y in range(y0, bottom - 2):
        for x in range(x0, x0 + w):
            u = (x - x0 + 0.5) / w * 2 - 1
            if y < y0 + 6 and (y - y0 - 6) ** 2 / 36 + u * u > 1:
                continue
            image[y, x, :3] = pg.WOOD[1] if (x - x0) % 4 == 0 else pg.WOOD[2 if u > 0.3 else 3]
            image[y, x, 3] = 255
    image[bottom - 14:bottom - 12, x0 + w - 4, :3] = pg.hexes("#62626e")[0]
    image[bottom - 2:bottom, x0 - 2:x0 + w + 2, :3] = pg.STONE[3]
    image[bottom - 2:bottom, x0 - 2:x0 + w + 2, 3] = 255


# ── 바위 언덕 ──────────────────────────────────────────

def boulder(rx, ry, rng):
    """
    바위 덩어리 하나: 둘레가 울퉁불퉁한 둥근 덩어리를 몇 갈래 면으로 깎았다. 면마다 왼쪽 위 빛을 다르게 받고,
    아래쪽은 그늘, 잔금 몇 줄. 돌려주는 값: RGBA(테두리 포함), 가운데 점.
    """
    w, h = int(rx * 2 + 4), int(ry * 2 + 4)
    cx, cy = w / 2, h / 2
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    angle = np.arctan2((yy - cy) / ry, (xx - cx) / rx)
    lumps = sum(rng.uniform(0.04, 0.1) * np.cos(k * angle + rng.uniform(0, 6.3)) for k in (2, 3, 5))
    d = np.hypot((xx - cx) / rx, (yy - cy) / ry) / (1 + lumps)
    inside = d < 1
    # 깎인 면: 면 방향을 여섯 갈래로 나누고, 면마다 가운데로 갈수록 위를 본다
    facet = np.round(angle / (np.pi / 3)) * (np.pi / 3)
    slope = np.clip(d, 0, 1) ** 1.4
    nx, ny = np.cos(facet) * slope, np.sin(facet) * slope * 1.2
    nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0.05, 1))
    light = np.clip(nx * pg.LIGHT[0] + ny * pg.LIGHT[1] + nz * pg.LIGHT[2], 0, 1)
    light = light * 0.8 + 0.25 - np.clip((yy - cy) / ry, 0, 1) * 0.3
    image = canvas(w, h)
    image[inside, :3] = pg.quantize(light, ROCK[1:8], 0.08)[inside]
    image[inside, 3] = 255
    # 면 사이 모서리: 빛 쪽은 밝은 선, 그늘 쪽은 어두운 선
    edge = inside & (np.abs(np.diff(np.pad(facet, ((0, 0), (1, 0)), mode="edge"), axis=1)) > 0)
    image[edge, :3] = ROCK[2]
    # 잔금
    for _ in range(rng.integers(2, 5)):
        x, y = rng.uniform(cx - rx * 0.6, cx + rx * 0.6), rng.uniform(cy - ry * 0.3, cy + ry * 0.6)
        for k in range(rng.integers(3, 7)):
            px, py = int(x + k * rng.choice([-1, 1]) * 0.7), int(y + k)
            if 0 <= px < w and 0 <= py < h and inside[py, px]:
                image[py, px, :3] = ROCK[1]
    # 윗면 이끼
    top_band = inside & ((yy - cy) / ry < -0.45) & (rng.random((h, w)) < 0.35)
    image[top_band, :3] = pg.GRASS[3]
    return pg.outline(image, ROCK[0]), (cx, cy)


def rocks(image, area, rng):
    """
    바위 언덕: 칸 자리(area)를 크고 작은 바위 덩어리로 덮는다. 뒤 줄은 작고 위에, 앞 줄은 크고 아래에 놓아
    앞 덩어리가 뒤 덩어리를 가린다. 덩어리는 칸 모양보다 위로 솟고 옆으로 조금 나간다. 아래 땅에는 그림자가 진다.
    """
    height, width = area.shape
    ys, xs = np.nonzero(area)
    if len(ys) == 0:
        return
    layer = np.zeros((height, width, 4), dtype=np.uint8)
    placed = []
    # 칸 자리를 가로로 훑으며 덩어리를 놓는다(뒤 줄 → 앞 줄)
    for row, (scale, lift) in enumerate(((1.15, 0.2), (1.45, 0.62))):
        x = xs.min() + rng.uniform(6, 12) * scale
        while x < xs.max() - 4:
            column = np.nonzero(area[:, int(x)])[0]
            if len(column):
                top, bottom = column.min(), column.max()
                rx = rng.uniform(15, 22) * scale
                ry = rx * rng.uniform(0.72, 0.85)
                cy = top + (bottom - top) * lift + rng.uniform(-3, 3)
                placed.append((row, cy, x, rx, ry))
            x += rx * rng.uniform(1.1, 1.4)
    for row, cy, cx, rx, ry in sorted(placed, key=lambda p: (p[0], p[1])):
        sprite, (sx, sy) = boulder(rx, ry, rng)
        left, top = int(cx - sx), int(cy - sy)
        h, w = sprite.shape[:2]
        y0, x0 = max(0, top), max(0, left)
        y1, x1 = min(height, top + h), min(width, left + w)
        part = sprite[y0 - top:y1 - top, x0 - left:x1 - left]
        solid = part[..., 3] > 0
        layer[y0:y1, x0:x1][solid] = part[solid]
    solid = layer[..., 3] > 0
    # 아래 땅에 드리운 그림자(아래로 6점, 점점 옅게)
    for k in range(1, 7):
        below = np.pad(solid, ((k, 0), (0, 0)))[:-k] & ~solid
        image[below] = (image[below] * (0.55 + k * 0.06)).astype(np.uint8)
    image[solid] = layer[solid, :3]
