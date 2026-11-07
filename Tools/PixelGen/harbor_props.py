"""항구 소품(서쪽 부두와 동쪽 선착장을 새로 짠다). 서쪽 부두(west_port.py)와 동쪽 선착장(east_port.py)이 함께 쓴다.

  바닥에 까는 것(RGBA 그림): pier(나무 잔교), quay_wall(돌 안벽), gangway(배다리), mooring_line(밧줄)
  세우는 것((RGBA 그림, 기준점)): moored_ship, skiff, quay_crane, lighthouse, market_stall, crate_stack, barrel_stack, bollard,
                                pier_rail, pennant, bunting, notice_board, sunken_boat, sea_gate

배는 ship.py의 뉴웨이브 호와 같은 시점이다: 위에서 비스듬히 본 갑판, 남쪽 바깥 뱃전, 돛대. 모두 점 격자(한 칸 32점)에 그린다.
"""
import numpy as np

import objects
import pixelgen as pg
import ship
from houses import BRASS, CLOTH, IRON, PAPER, STONE, WARM, WOOD, Canvas, outline

T = pg.TILE
HULLS = {
    "teal": pg.hexes("#123a3a", "#1d5a58", "#2c7f7a", "#d0c4a0"),
    "red": pg.hexes("#4a1a18", "#7a2a24", "#a83e32", "#e4d6b0"),
    "navy": pg.hexes("#141e38", "#22345a", "#35507e", "#d4d8dc"),
    "ochre": pg.hexes("#5a3a14", "#8a5e20", "#b88a30", "#efe2bc"),
    "black": pg.hexes("#14141a", "#24242c", "#3a3a44", "#c8b888"),
}
DECK = pg.hexes("#5c3e24", "#7a5634", "#8e6840", "#a27a4c", "#b88e5c")
PLANK = pg.hexes("#2a1b10", "#4a3320", "#6a4a2e", "#84603c", "#9c7648")
OLD_PLANK = pg.hexes("#1e1a16", "#38312a", "#524a3e", "#6a6152", "#807766")
ROPE = pg.hexes("#4a3820", "#6e5630", "#94784a", "#b89a64")
CANVAS_WHITE = pg.hexes("#a89e88", "#d0c8b0", "#efe8d4")
FOAM = pg.WATER[6]
WALL = pg.hexes("#25262e", "#34353f", "#474955", "#5e606c", "#7c7e8a", "#a0a2ac")
MOSS = pg.hexes("#1f3a26", "#34563a")
SEA_GLOW = pg.hexes("#0e3a5a", "#14608a", "#1e8ab4", "#3ec0d8", "#8ae8ec", "#e0fbf8")
GATE_STONE = pg.hexes("#20303a", "#31464f", "#46626a", "#628488", "#8aaeaa")
FISH = pg.hexes("#7e96a4", "#b8c8d0", "#eef4f6")
CRAB = pg.hexes("#7a2a1c", "#c8503a")
FRUIT = pg.hexes("#c86a24", "#f0a040", "#a8c04a", "#d84a3a")


def over(dst, src, left, top):
    """RGBA 그림 dst에 src를 얹는다(src가 칠해진 점만)."""
    h, w = src.shape[:2]
    y0, x0 = max(0, -top), max(0, -left)
    y1, x1 = min(h, dst.shape[0] - top), min(w, dst.shape[1] - left)
    part = src[y0:y1, x0:x1]
    mask = part[..., 3] > 0
    dst[top + y0:top + y1, left + x0:left + x1][mask] = part[mask]


# ── 바닥에 까는 것 ──────────────────────────────────────

def plank_floor(w, h, rng, across=True, worn=False):
    """판자 바닥(RGB). across면 판자가 동서로 놓여 남북으로 걷는 잔교가 되고, 아니면 판자가 남북으로 놓인다."""
    palette = OLD_PLANK if worn else PLANK
    long_side, short_side = (h, w) if across else (w, h)
    level = np.zeros((long_side, short_side), dtype=int)
    for k in range(0, long_side, 7):
        tone = int(rng.choice([2, 3, 3, 4]))
        level[k:k + 7] = tone
        level[k + 6:k + 7] = 0
        level[k] = min(4, tone + 1)
    grain = pg.fractal_noise(long_side, short_side, rng, ((6, 0.6), (2, 0.4)))
    level = np.where((grain > (0.62 if worn else 0.78)) & (level > 1), level - 1, level)
    if worn:
        # 썩어 빠진 자리: 판자 끝이 들쭉날쭉하다
        for k in range(0, long_side, 7):
            if rng.random() < 0.14:
                level[k:k + 6, int(rng.integers(0, short_side - 6)):] = np.where(rng.random() < 0.5, 1, level[k:k + 6, 0].max())
    rgb = palette[level]
    return rgb if across else rgb.transpose(1, 0, 2)


def pier(columns, rows, rng, worn=False, across=True):
    """
    나무 잔교(왼쪽 위 기준): 판자, 양옆 받침 들보와 말뚝 머리, 끝의 앞면과 말뚝, 물에 드리운 그림자와 물결.
    양옆 MARGIN점과 아래 FRONT점이 칸 밖으로 나온다. 왼쪽 위 자리는 칸 왼쪽 위에서 (−MARGIN, 0)이다.
    """
    margin, front = PIER_MARGIN, PIER_FRONT
    w, h = int(columns * T), int(rows * T)
    image = np.zeros((h + front, w + margin * 2, 4), dtype=np.uint8)
    deck = plank_floor(w, h, rng, across, worn)
    beam = OLD_PLANK if worn else PLANK
    for side in (0, w - 4):
        deck[:, side:side + 4] = beam[1]
        deck[:, side + (3 if side == 0 else 0)] = beam[0]
    for y in range(6, h, T):
        for x in (1, w - 4):
            deck[y:y + 3, x:x + 3] = beam[4]
            deck[y + 2, x:x + 3] = beam[2]
    image[:h, margin:margin + w, :3] = deck
    image[:h, margin:margin + w, 3] = 255
    # 끝 앞면과 말뚝
    image[h:h + 8, margin:margin + w, :3] = beam[1]
    image[h:h + 8, margin:margin + w, 3] = 255
    image[h, margin:margin + w, :3] = beam[3]
    for x in range(margin + 3, margin + w - 3, T // 2):
        image[h + 8:h + front - 2, x:x + 5, :3] = beam[1]
        image[h + 8:h + front - 2, x:x + 5, 3] = 255
        image[h + 8:h + front - 2, x:x + 2, :3] = beam[2]
        image[h + front - 3, x - 1:x + 7, :3] = FOAM
        image[h + front - 3, x - 1:x + 7, 3] = 190
    # 물 위 그림자와 물결
    image[:h, margin + w:margin * 2 + w - 1, :3] = (8, 18, 32)
    image[:h, margin + w:margin * 2 + w - 1, 3] = 110
    for y in range(4, h - 3, 8):
        for x in (margin - 4, margin + w + 1):
            length = int(rng.integers(2, 4))
            image[y, x:x + length, :3] = FOAM
            image[y, x:x + length, 3] = 190
    return image


PIER_MARGIN, PIER_FRONT = 6, 26


def quay_wall(columns, rng, gaps=()):
    """
    돌 안벽(바다와 맞닿은 부두 벽, 왼쪽 위 기준). 맨 위는 밝은 갓돌, 아래로 쌓은 돌, 이끼 낀 물가, 쇠고리, 물거품.
    gaps는 잔교가 지나 벽을 그리지 않는 칸 범위(왼쪽, 오른쪽)들이다.
    """
    w, h = int(columns * T), 30
    image = np.zeros((h, w, 4), dtype=np.uint8)
    rgb = np.zeros((h, w, 3), dtype=np.uint8)
    rgb[:6] = WALL[4]
    rgb[0] = WALL[5]
    rgb[5] = WALL[1]
    for x in range(int(rng.integers(0, 20)), w, 24):
        rgb[:5, x:x + 1] = WALL[2]
    for course in range(3):
        y0 = 6 + course * 7
        x = -int(rng.integers(0, 16)) - course * 7
        while x < w:
            block = int(rng.integers(16, 28))
            x0, x1 = max(0, x), min(w, x + block)
            rgb[y0:y0 + 7, x0:x1] = WALL[int(rng.choice([2, 2, 3])) - course // 2]
            rgb[y0, x0:x1] = WALL[3]
            rgb[y0:y0 + 7, x0:x0 + 1] = WALL[0]
            x += block
        rgb[y0 + 6] = WALL[0]
    moss = pg.noise(h, w, 5, rng) > 0.52
    rgb[20:26][moss[20:26]] = MOSS[1]
    rgb[24:27] = WALL[0]
    rgb[24:27][moss[24:27]] = MOSS[0]
    for x in range(40, w, 96):
        ring = np.zeros((h, w), dtype=bool)
        yy, xx = np.mgrid[0:h, 0:w]
        ring |= (np.hypot(xx - x, yy - 15) < 4) & (np.hypot(xx - x, yy - 15) > 2)
        rgb[ring] = IRON[3]
    image[:27, :, :3] = rgb[:27]
    image[:27, :, 3] = 255
    image[27:29, :, :3] = FOAM
    image[27:29, :, 3] = 150
    image[27:29][pg.noise(h, w, 4, rng)[27:29] < 0.45] = 0
    for left, right in gaps:
        image[:, int(left * T):int(right * T)] = 0
    return image


def gangway(w, h):
    """배와 부두를 잇는 배다리(바닥에 까는 그림): 가로대를 댄 긴 널판."""
    c = Canvas(w, h)
    board = c.rect(0, 0, w, h)
    c.paint(board, WOOD[3])
    c.paint(board & (c.yy % 6 == 0), WOOD[1])
    c.paint(board & ((c.xx < 2) | (c.xx >= w - 2)), WOOD[2])
    return outline(c.image)


def mooring_line(dx, dy):
    """부두에서 배로 늘어진 밧줄(바닥에 까는 그림). 돌려주는 값은 (그림, 왼쪽 위에서 시작점까지 (x, y))이다."""
    w, h = abs(dx) + 4, abs(dy) + 8
    c = Canvas(w, h)
    x0, x1 = (1, w - 3) if dx >= 0 else (w - 3, 1)
    y0, y1 = (1, h - 7) if dy >= 0 else (h - 7, 1)
    steps = max(abs(dx), abs(dy)) + 1
    for k in range(steps):
        t = k / max(1, steps - 1)
        x = int(round(x0 + (x1 - x0) * t))
        y = int(round(y0 + (y1 - y0) * t + 5 * np.sin(t * np.pi)))
        c.image[y:y + 2, x:x + 1, :3] = ROPE[1 + k % 2]
        c.image[y:y + 2, x:x + 1, 3] = 255
    return c.image, (x0, y0)


# ── 배 ────────────────────────────────────────────────

def half_beam(x, length, beam):
    """고물(왼쪽) 0에서 이물(오른쪽) length까지, x 점에서 갑판의 반폭(점). 고물은 둥글고 이물은 뾰족하다."""
    x = np.asarray(x, dtype=float)
    half = np.full(x.shape, beam / 2)
    corner = beam * 0.32
    near = x < corner
    half[near] -= corner - np.sqrt(np.clip(corner ** 2 - (corner - x[near]) ** 2, 0, None))
    bow = x > length * 0.62
    t = np.clip((x[bow] - length * 0.62) / (length * 0.38), 0, 1)
    half[bow] *= np.clip(1 - t ** 1.8, 0, 1) ** 0.8
    half[(x < 0) | (x > length)] = -1
    return half


def moored_ship(seed, length, beam, masts=2, paint="teal", cabin=True, flip=False, cargo=0, pennant_color="red"):
    """
    항구에 맨 배(남쪽에서 비스듬히 본 모습). length·beam은 칸이다. 이물이 오른쪽이고 flip이면 왼쪽이다.
    갑판 판자와 뱃전, 남쪽 바깥 뱃전(띠 장식, 현창, 물막이 띠), 고물 선실, 돛대(접은 돛), 이물대, 갑판의 상자.
    기준점은 물에 닿은 뱃바닥 가운데다.
    """
    rng = np.random.default_rng(seed)
    hull = HULLS[paint]
    span, width = int(length * T), int(beam * T)
    side = int(12 + beam * 9)
    tallest = int(length * 13) if masts else 0
    bow_room = int(1.6 * T)
    w, h = span + bow_room + 14, tallest + width + side + 40
    c = Canvas(w, h)
    left = 6
    water_y = h - 6
    center = water_y - side - width / 2
    xs = np.arange(w) - left + 0.5
    half = half_beam(xs, span, width)
    inside = half >= 0
    north, south = center - half, center + half
    deck = inside[None, :] & (c.yy >= north[None, :]) & (c.yy < south[None, :])

    # 남쪽 바깥 뱃전
    v = c.yy - south[None, :]
    wall = inside[None, :] & (v >= 0) & (v < side)
    c.paint(wall, hull[1])
    c.paint(wall & (((v.astype(int)) // 5) % 2 == 1), hull[0])
    c.paint(wall & (v >= 0) & (v < 3), hull[3])
    c.paint(wall & (v >= side * 0.45) & (v < side * 0.45 + 2), hull[2])
    c.paint(wall & (v >= side - 5), pg.hexes("#1a1412")[0])
    c.paint(wall & (v >= side - 2), pg.hexes("#3a2a1e")[0])
    for px in range(left + int(span * 0.14), left + int(span * 0.8), int(max(26, span * 0.13))):
        py = int(south[px] + side * 0.3)
        c.paint(c.ellipse(px, py, 3.2, 2.8), BRASS[3])
        c.paint(c.ellipse(px, py, 2, 1.8), pg.hexes("#24384a")[0])

    # 갑판과 뱃전 윗면
    ring = ship.dilate(deck, 3) & ~deck
    c.paint(ring, DECK[3])
    c.paint(ring & ~ship.dilate(deck, 2), hull[0])
    level = (c.yy // 6) % 3 + 1
    c.image[deck, :3] = DECK[level[deck]]
    c.image[deck, 3] = 255
    c.paint(deck & (c.yy % 6 == 0), DECK[0])
    c.paint(deck & (c.yy < center - half.max() * 0.7), DECK[1])

    # 이물대
    tip_y = center
    tip = left + span
    c.line(tip - 8, tip_y - 2, tip + bow_room - 4, tip_y - 12, WOOD[3], 3)
    c.line(tip - 8, tip_y - 3, tip + bow_room - 4, tip_y - 13, WOOD[5], 1)
    c.paint(c.ellipse(tip - 2, tip_y, 3.4, 3), hull[3])

    # 갑판 짐
    for k in range(cargo):
        cx, cy = left + int(span * (0.4 + k * 0.1)), int(center + (k % 2) * 3 - 2)
        c.paint(c.rect(cx - 8, cy - 9, cx + 8, cy + 7), WOOD[0])
        c.paint(c.rect(cx - 7, cy - 8, cx + 7, cy + 6), WOOD[2])
        c.paint(c.rect(cx - 7, cy - 8, cx + 7, cy - 3), WOOD[5])
        c.paint(c.rect(cx - 7, cy - 3, cx + 7, cy - 2), WOOD[1])

    # 고물 선실: 위는 지붕, 남쪽에 창이 난 벽이 보인다
    if cabin:
        box_w, box_h = int(max(2.2 * T, span * 0.2)), int(width * 0.62)
        x0 = left + int(width * 0.3)
        roof_top, wall_bottom = int(center - box_h / 2 - 2), int(center + box_h / 2 + 6)
        wall_top = wall_bottom - 18
        c.paint(c.rect(x0, wall_top, x0 + box_w, wall_bottom), WOOD[3])
        c.paint(c.rect(x0, wall_bottom - 3, x0 + box_w, wall_bottom), WOOD[1])
        for wx in range(x0 + 6, x0 + box_w - 8, 14):
            c.paint(c.rect(wx, wall_bottom - 14, wx + 8, wall_bottom - 6), WARM[2])
            c.paint(c.rect(wx, wall_bottom - 14, wx + 8, wall_bottom - 12), WARM[3])
        roof = c.rect(x0 - 3, roof_top, x0 + box_w + 3, wall_top + 2)
        c.paint(roof, hull[1])
        c.paint(roof & ((c.yy - roof_top) % 5 == 0), hull[0])
        c.paint(c.rect(x0 - 3, roof_top, x0 + box_w + 3, roof_top + 3), hull[2])
        c.paint(c.rect(x0 - 3, wall_top, x0 + box_w + 3, wall_top + 2), hull[3])
    image = outline(c.image)

    # 돛대: 뉴웨이브 호의 돛대 그림을 크기만 바꿔 쓴다.
    if masts:
        spots = {1: (0.5,), 2: (0.42, 0.68), 3: (0.3, 0.52, 0.72)}[masts]
        for k, spot in enumerate(spots):
            height = int(tallest * (1.0 if k == len(spots) // 2 else 0.84))
            boom = int(span * 0.2)
            stay = (int(tip + bow_room - 4 - (left + span * spot)), int(side + width * 0.1)) if k == len(spots) - 1 else None
            (mast_image, (base_x, base_y)), _ = ship.mast(rng, height, boom, nest=height > 150, flag=False, lantern_at=66, stay=stay)
            over(image, mast_image, left + int(span * spot) - base_x, int(center) + 2 - base_y)
            pole_x, top_y = left + int(span * spot), int(center) + 2 - height
            flag = Canvas(w, h)
            flag.paint(flag.rect(pole_x - 1, top_y - 14, pole_x + 1, top_y + 2), WOOD[2])
            cloth = CLOTH[pennant_color]
            flag.paint(flag.polygon([(pole_x + 1, top_y - 14), (pole_x + 26, top_y - 9), (pole_x + 1, top_y - 3)]), cloth[2])
            flag.paint(flag.polygon([(pole_x + 1, top_y - 14), (pole_x + 26, top_y - 9), (pole_x + 1, top_y - 10)]), hull[3])
            over(image, outline(flag.image), 0, 0)

    # 물에 닿는 물거품(뱃바닥을 따라 두 줄)
    for x in range(w):
        if inside[x]:
            y = int(south[x] + side)
            if y + 2 < h and rng.random() < 0.7:
                image[y, x, :3] = FOAM
                image[y, x, 3] = 220
            if y + 3 < h and rng.random() < 0.3:
                image[y + 2, x, :3] = pg.WATER[5]
                image[y + 2, x, 3] = 200
    pivot_x = left + span // 2
    if flip:
        return image[:, ::-1].copy(), (w - 1 - pivot_x, water_y)
    return image, (pivot_x, water_y)


def skiff(seed, paint="teal", upright=False):
    """작은 쪽배: 뾰족한 양 끝, 가로 널, 노 한 쌍. upright면 남북으로 놓였다. 기준점은 뱃바닥 가운데다."""
    rng = np.random.default_rng(seed)
    hull = HULLS[paint]
    length, beam = 76, 26
    w, h = (length + 8, beam + 18) if not upright else (beam + 10, length + 12)
    c = Canvas(w, h)
    side = 6
    if upright:
        cx, cy = w / 2, (h - side) / 2
        along, across, ll, bb = c.yy - cy, c.xx - cx, length, beam
    else:
        cx, cy = w / 2, (h - side) / 2
        along, across, ll, bb = c.xx - cx, c.yy - cy, length, beam
    shape = np.abs(across) < (bb / 2) * np.clip(1 - (np.abs(along) / (ll / 2)) ** 2.2, 0, 1) ** 0.7
    wall = np.zeros_like(shape)
    for k in range(1, side + 1):
        wall[k:, :] |= shape[:-k, :]
    c.paint(wall & ~shape, hull[1])
    c.paint(pg.neighbours(wall & ~shape) & (wall & ~shape) & (c.yy % 2 == 0), hull[0])
    c.paint(shape, hull[2])
    inner = np.abs(across) < (bb / 2 - 3) * np.clip(1 - (np.abs(along) / (ll / 2 - 4)) ** 2.2, 0, 1) ** 0.7
    c.paint(inner, WOOD[2])
    c.paint(inner & ((c.yy if not upright else c.xx) % 5 == 0), WOOD[1])
    for k in (-16, 8):
        thwart = (np.abs(along - k) < 2.5) & inner
        c.paint(thwart, WOOD[5])
    oar_y = cy - 2
    if upright:
        c.line(cx - 7, cy - 22, cx + 7, cy + 10, WOOD[4], 2)
    else:
        c.line(cx - 22, oar_y - 3, cx + 12, oar_y + 5, WOOD[4], 2)
    image = outline(c.image)
    rows = np.nonzero(image[..., 3].any(axis=1))[0]
    return image, (w // 2, int(rows.max()))


def sunken_boat(seed):
    """가라앉다 만 쪽배: 고물만 물 위로 나왔고 둘레에 찌꺼기가 뜬다(서쪽 부두의 쓸쓸한 소품)."""
    rng = np.random.default_rng(seed)
    w, h = 64, 34
    c = Canvas(w, h)
    hull = c.polygon([(4, 20), (28, 8), (54, 12), (60, 22), (34, 30), (10, 28)])
    c.paint(hull, OLD_PLANK[2])
    c.paint(hull & (c.yy % 5 == 0), OLD_PLANK[1])
    c.paint(hull & (c.xx > 44), OLD_PLANK[1])
    inner = c.polygon([(12, 20), (28, 13), (46, 16), (48, 21), (32, 25), (14, 25)])
    c.paint(inner, pg.WATER[1])
    c.paint(inner & (c.yy > 21), pg.WATER[2])
    for x in (22, 36):
        c.paint(c.rect(x, 12, x + 3, 26) & hull, OLD_PLANK[3])
    image = outline(c.image)
    image[29:33, 4:60, :3] = FOAM
    image[29:33, 4:60, 3] = (rng.random((4, 56)) < 0.25) * 170
    return image, (w // 2, h - 3)


def shore_rock(seed, wet=False):
    """
    모래밭이나 물에 놓는 바위(세우는 것). terrain의 rock 칸은 풀밭용이라 둘레에 풀이 깔린다.
    wet이면 물에 잠겨 밑동에 물거품이 친다.
    """
    rng = np.random.default_rng(seed)
    w, h = int(rng.integers(40, 56)), int(rng.integers(30, 40))
    c = Canvas(w, h)
    ground = h - 5
    points = [(2, ground), (6, ground - h * 0.4), (w * 0.3, 4), (w * 0.62, 2), (w - 6, ground - h * 0.42), (w - 2, ground)]
    body = c.polygon(points)
    c.paint(body, WALL[3])
    c.paint(body & (c.yy < ground - h * 0.42), WALL[4])
    c.paint(body & (c.xx > w * 0.62), WALL[2])
    c.paint(body & (c.yy >= ground - 5), WALL[1])
    c.paint(body & (pg.noise(h, w, 6, rng) > 0.68), WALL[2])
    c.paint(body & (c.yy < 8) & (c.xx < w * 0.55), WALL[5])
    image = outline(c.image)
    if wet:
        foam = (np.abs(c.yy - ground) < 2) & (c.xx >= 0) & (rng.random((h, w)) < 0.55)
        image[foam, :3] = FOAM
        image[foam, 3] = 220
    return image, (w // 2, ground)


# ── 부두의 물건 ─────────────────────────────────────────

def crate_stack(seed, columns=2, layers=2):
    """쌓아 올린 나무 상자."""
    rng = np.random.default_rng(seed)
    first, _ = objects.crate(rng)
    box_h, box_w = first.shape[:2]
    step_x, step_y = box_w - 3, int(box_h * 0.62)
    w, h = step_x * columns + 6, box_h + step_y * (layers - 1) + 2
    image = np.zeros((h, w, 4), dtype=np.uint8)
    for layer in range(layers):
        count = columns - (layer if layer < columns else columns - 1)
        for k in range(count):
            piece, _ = objects.crate(rng)
            over(image, piece, 1 + k * step_x + layer * step_x // 2, h - box_h - 1 - layer * step_y)
    return image, (w // 2, h - 2)


def barrel_stack(seed, count=3):
    """나란히 세운 통 몇 개와 그 위에 얹은 통 하나."""
    rng = np.random.default_rng(seed)
    first, _ = objects.barrel(rng)
    box_h, box_w = first.shape[:2]
    step = box_w - 3
    w, h = step * count + 6, box_h + int(box_h * 0.6) + 2
    image = np.zeros((h, w, 4), dtype=np.uint8)
    for k in range(count):
        over(image, objects.barrel(rng)[0], 1 + k * step, h - box_h - 1)
    over(image, objects.barrel(rng)[0], 1 + step // 2, h - box_h - 1 - int(box_h * 0.6))
    return image, (w // 2, h - 2)


def bollard(seed, rope=True):
    """부두의 계선주: 짧고 굵은 쇠기둥과 감아 둔 밧줄."""
    c = Canvas(20, 24)
    ground = 22
    c.paint(c.rect(5, 6, 15, ground), IRON[2])
    c.paint(c.rect(5, 6, 8, ground), IRON[3])
    c.paint(c.rect(12, 6, 15, ground), IRON[1])
    c.paint(c.ellipse(10, 6, 7, 3.4), IRON[3])
    c.paint(c.ellipse(9, 5, 3.4, 1.6), IRON[4])
    if rope:
        for y in (12, 15, 18):
            c.paint(c.rect(4, y, 16, y + 2), ROPE[2])
            c.paint(c.rect(4, y + 1, 16, y + 2), ROPE[1])
    return outline(c.image), (10, ground)


def pier_rail(length, rng, worn=False, broken=False):
    """
    잔교 난간 한 칸(세우는 것, 남북으로 뻗는다): 아래 말뚝과 북쪽으로 이어진 밧줄 또는 들보. worn이면 낡은 나무이고,
    broken이면 들보가 끊겨 말뚝만 남았다. 기준점은 말뚝 아래 가운데다.
    """
    palette = OLD_PLANK if worn else PLANK
    height = 22
    w, h = 10, length + height + 2
    c = Canvas(w, h)
    ground = h - 1
    if not broken:
        c.paint(c.rect(3, 1, 7, length + 3), palette[2])
        c.paint(c.rect(3, 1, 4, length + 3), palette[4])
        c.paint(c.rect(6, 1, 7, length + 3), palette[1])
    post_top = ground - height + (6 if broken else 0)
    c.paint(c.rect(2, post_top, 8, ground), palette[3])
    c.paint(c.rect(2, post_top, 4, ground), palette[4])
    c.paint(c.rect(7, post_top, 8, ground), palette[1])
    c.paint(c.rect(2, post_top, 8, post_top + 2), palette[4])
    return outline(c.image), (w // 2, ground)


def pennant(seed, color, height=112):
    """높은 깃대와 긴 삼각 깃발."""
    w, h = 62, height + 8
    c = Canvas(w, h)
    ground = h - 2
    c.paint(c.rect(6, 6, 9, ground), WOOD[2])
    c.paint(c.rect(6, 6, 7, ground), WOOD[4])
    c.paint(c.rect(2, ground - 4, 13, ground), STONE[3])
    c.paint(c.ellipse(7.5, 5, 2.6, 2.6), BRASS[3])
    cloth = CLOTH[color]
    shape = c.polygon([(9, 8), (58, 16), (9, 32)])
    c.paint(shape, cloth[2])
    c.paint(shape & (c.yy < 14), cloth[1])
    c.paint(shape & (c.xx % 13 == 3), cloth[0])
    c.paint(c.ellipse(20, 19, 4, 4), PAPER[2])
    return outline(c.image), (7, ground)


def bunting(columns, seed):
    """두 기둥 사이에 건 줄 깃발. 사람 머리 위로 높이 걸려 있다."""
    rng = np.random.default_rng(seed)
    w, h = int(columns * T), 96
    c = Canvas(w, h)
    ground = h - 2
    for x in (2, w - 6):
        c.paint(c.rect(x, 4, x + 4, ground), WOOD[2])
        c.paint(c.rect(x, 4, x + 1, ground), WOOD[4])
        c.paint(c.ellipse(x + 2, 4, 3, 3), BRASS[3])
    colors = [CLOTH["red"][2], PAPER[2], CLOTH["blue"][2], BRASS[3], CLOTH["teal"][2], CLOTH["green"][2]]
    sag = lambda x: 8 + 20 * np.sin((x - 4) / (w - 8) * np.pi)      # noqa: E731
    for x in range(4, w - 4):
        c.paint(c.rect(x, int(sag(x)), x + 1, int(sag(x)) + 1), ROPE[1])
    for k, x in enumerate(range(12, w - 14, 15)):
        y = sag(x + 4)
        c.paint(c.polygon([(x, y), (x + 10, y), (x + 5, y + 13)]), colors[int(rng.integers(0, len(colors)))])
    return outline(c.image), (w // 2, ground)


def notice_board(seed, torn=True):
    """부두 팻말판: 비바람에 바랜 판자에 찢어진 공고문(배편 끊김)과 X자로 친 줄."""
    rng = np.random.default_rng(seed)
    w, h = 48, 56
    c = Canvas(w, h)
    ground = h - 2
    for x in (8, w - 12):
        c.paint(c.rect(x, 26, x + 4, ground), OLD_PLANK[2])
    c.paint(c.rect(2, 6, w - 2, 34), OLD_PLANK[2])
    c.paint(c.rect(2, 6, w - 2, 8), OLD_PLANK[4])
    c.paint(c.rect(2, 32, w - 2, 34), OLD_PLANK[0])
    paper = c.rect(9, 10, w - 9, 31)
    c.paint(paper, PAPER[0])
    for y in range(13, 29, 4):
        c.paint(c.rect(12, y, w - 12 - int(rng.integers(0, 8)), y + 1), OLD_PLANK[1])
    if torn:
        c.line(10, 11, w - 11, 30, CLOTH["red"][2], 2)
        c.line(w - 11, 11, 10, 30, CLOTH["red"][2], 2)
    return outline(c.image), (w // 2, ground)


# ── 큰 설비 ────────────────────────────────────────────

def quay_crane(seed):
    """
    부두 기중기: 돌 받침 위의 나무 탑, 동쪽 바다 쪽으로 뻗은 긴 팔, 도르래에서 늘어진 밧줄 끝의 그물 짐.
    기준점은 받침 아래 가운데다.
    """
    w, h = 200, 196
    c = Canvas(w, h)
    ground = h - 2
    base_x = 44
    # 받침
    c.paint(c.rect(base_x - 34, ground - 26, base_x + 34, ground), WALL[2])
    c.paint(c.rect(base_x - 34, ground - 26, base_x + 34, ground - 22), WALL[4])
    for y in (ground - 18, ground - 10):
        c.paint(c.rect(base_x - 34, y, base_x + 34, y + 1), WALL[0])
    # 탑: 두 기둥과 엇댄 가새
    top_y = 36
    for x in (base_x - 22, base_x + 16):
        c.paint(c.rect(x, top_y, x + 7, ground - 24), WOOD[3])
        c.paint(c.rect(x, top_y, x + 2, ground - 24), WOOD[5])
        c.paint(c.rect(x + 6, top_y, x + 7, ground - 24), WOOD[1])
    for k in range(3):
        y0, y1 = top_y + 12 + k * 40, top_y + 52 + k * 40
        c.line(base_x - 18, y0, base_x + 18, y1, WOOD[2], 3)
        c.line(base_x + 18, y0, base_x - 18, y1, WOOD[2], 3)
        c.paint(c.rect(base_x - 22, y0 - 3, base_x + 23, y0 + 1), WOOD[2])
    # 사람 키만 한 감는 북
    drum_y = ground - 54
    c.paint(c.ellipse(base_x, drum_y, 15, 15), WOOD[3])
    c.paint(c.ellipse(base_x, drum_y, 12, 12), WOOD[2])
    for k in range(8):
        a = k * np.pi / 4
        c.line(base_x, drum_y, base_x + np.cos(a) * 13, drum_y + np.sin(a) * 13, WOOD[4], 2)
    c.paint(c.ellipse(base_x, drum_y, 3, 3), IRON[3])
    # 팔과 버팀줄
    tip_x, tip_y = w - 18, 12
    arm = c.polygon([(base_x - 16, top_y + 8), (base_x - 10, top_y + 16), (tip_x, tip_y + 4), (tip_x - 2, tip_y - 2)])
    c.paint(arm, WOOD[3])
    c.line(base_x - 14, top_y + 8, tip_x, tip_y - 1, WOOD[5], 1)
    c.line(base_x - 2, top_y - 16, tip_x - 4, tip_y, ROPE[1], 1)
    c.line(base_x - 2, top_y - 16, base_x - 40, top_y + 30, ROPE[1], 1)
    c.paint(c.rect(base_x - 5, top_y - 18, base_x + 2, top_y + 6), WOOD[2])
    c.paint(c.rect(base_x - 5, top_y - 18, base_x - 3, top_y + 6), WOOD[4])
    # 도르래, 밧줄, 그물 짐
    c.paint(c.ellipse(tip_x - 2, tip_y + 6, 4, 4), IRON[2])
    load_top = 118
    c.line(tip_x - 2, tip_y + 8, tip_x - 2, load_top - 12, ROPE[2], 1)
    c.line(tip_x - 2, load_top - 12, tip_x - 16, load_top, ROPE[2], 1)
    c.line(tip_x - 2, load_top - 12, tip_x + 12, load_top, ROPE[2], 1)
    bag = c.ellipse(tip_x - 2, load_top + 12, 18, 15)
    c.paint(bag, CANVAS_WHITE[1])
    c.paint(bag & (c.xx > tip_x + 6), CANVAS_WHITE[0])
    c.paint(bag & (((c.xx + c.yy) % 6 == 0) | ((c.xx - c.yy) % 6 == 0)), ROPE[1])
    return outline(c.image), (base_x, ground)


def lighthouse(seed):
    """등대: 돌 밑동, 빨강·흰 띠 탑, 난간 두른 등롱, 붉은 고깔. 기준점은 밑동 아래 가운데다."""
    w, h = 96, 214
    c = Canvas(w, h)
    ground = h - 2
    cx = w // 2
    base_top = ground - 26
    c.paint(c.polygon([(cx - 40, ground), (cx + 40, ground), (cx + 32, base_top), (cx - 32, base_top)]), WALL[3])
    c.paint(c.rect(cx - 34, base_top, cx + 34, base_top + 3), WALL[5])
    for y in range(base_top + 8, ground, 7):
        c.paint(c.rect(cx - 40, y, cx + 40, y + 1) & (c.image[..., 3] > 0), WALL[1])
    tower_top = 58
    bands = 5
    for y in range(tower_top, base_top):
        t = (y - tower_top) / (base_top - tower_top)
        half = 15 + 11 * t
        stripe = int(t * bands) % 2
        row = c.rect(cx - half, y, cx + half, y + 1)
        c.paint(row, CLOTH["red"][2] if stripe else PAPER[2])
        c.paint(row & (c.xx < cx - half * 0.55), CLOTH["red"][2] * 0 + (pg.hexes("#c8605a")[0] if stripe else pg.hexes("#ffffff")[0]))
        c.paint(row & (c.xx > cx + half * 0.55), CLOTH["red"][1] if stripe else PAPER[0])
    door = c.arch(cx - 8, base_top - 28, cx + 8, base_top + 2)
    c.paint(door, WOOD[2])
    c.paint(door & (c.xx == cx), WOOD[0])
    # 난간과 등롱
    c.paint(c.rect(cx - 22, tower_top - 4, cx + 22, tower_top + 2), IRON[2])
    c.paint(c.rect(cx - 22, tower_top - 4, cx + 22, tower_top - 3), IRON[4])
    for x in range(cx - 22, cx + 23, 6):
        c.paint(c.rect(x, tower_top - 14, x + 1, tower_top - 4), IRON[3])
    c.paint(c.rect(cx - 23, tower_top - 15, cx + 23, tower_top - 13), IRON[3])
    glass = c.rect(cx - 13, tower_top - 32, cx + 13, tower_top - 4)
    c.paint(glass, WARM[3])
    c.paint(c.ellipse(cx, tower_top - 18, 8, 10), WARM[4])
    for x in (cx - 13, cx - 5, cx + 4, cx + 12):
        c.paint(c.rect(x, tower_top - 32, x + 1, tower_top - 4), IRON[1])
    roof = c.polygon([(cx - 18, tower_top - 31), (cx + 18, tower_top - 31), (cx, tower_top - 52)])
    c.paint(roof, CLOTH["red"][1])
    c.paint(roof & (c.xx < cx - 3), CLOTH["red"][2])
    c.paint(c.ellipse(cx, tower_top - 53, 3, 3), BRASS[3])
    return outline(c.image), (cx, ground)


def market_stall(seed, cloth, goods):
    """
    어시장 천막: 네 기둥 위 줄무늬 천막과 물결 친 처마, 앞 판매대 위의 물건.
    goods: fish(생선과 얼음), crab(게와 조개), fruit(과일 바구니), rope(밧줄과 그물). 기준점은 판매대 아래 가운데다.
    """
    rng = np.random.default_rng(seed)
    w, h = 112, 104
    c = Canvas(w, h)
    ground = h - 2
    for x in (10, w - 14):
        c.paint(c.rect(x, 40, x + 4, ground), WOOD[3])
        c.paint(c.rect(x, 40, x + 1, ground), WOOD[5])
    # 뒤쪽 그늘
    c.paint(c.rect(14, 46, w - 14, ground - 26), pg.hexes("#2a2422")[0])
    # 판매대
    top = ground - 30
    c.paint(c.rect(6, top, w - 6, top + 10), WOOD[4])
    c.paint(c.rect(6, top, w - 6, top + 2), WOOD[5])
    c.paint(c.rect(6, top + 10, w - 6, ground), WOOD[2])
    for x in range(10, w - 8, 12):
        c.paint(c.rect(x, top + 10, x + 1, ground), WOOD[1])
    c.paint(c.rect(6, ground - 3, w - 6, ground), WOOD[1])
    if goods == "fish":
        c.paint(c.rect(10, top - 4, w - 10, top + 6), pg.hexes("#d8ecf0")[0])
        for k in range(int((w - 24) / 11)):
            x, y = 15 + k * 11, top + (k % 2) * 3
            c.paint(c.ellipse(x, y, 6, 2.4), FISH[1])
            c.paint(c.ellipse(x - 1, y - 1, 3, 1), FISH[2])
            c.paint(c.polygon([(x + 5, y), (x + 9, y - 2), (x + 9, y + 2)]), FISH[0])
    elif goods == "crab":
        c.paint(c.rect(10, top - 3, w - 10, top + 6), PAPER[0])
        for k in range(7):
            x, y = 16 + k * 13, top + 1 + (k % 2) * 2
            c.paint(c.ellipse(x, y, 5, 3.4), CRAB[1])
            c.paint(c.rect(x - 7, y, x + 8, y + 1), CRAB[0])
            c.paint(c.rect(x - 2, y - 3, x, y - 2) | c.rect(x + 1, y - 3, x + 3, y - 2), PAPER[2])
    elif goods == "fruit":
        for k, x in enumerate(range(16, w - 18, 24)):
            basket = c.polygon([(x - 10, top - 3), (x + 10, top - 3), (x + 7, top + 7), (x - 7, top + 7)])
            c.paint(basket, WOOD[3])
            c.paint(basket & ((c.xx + c.yy) % 3 == 0), WOOD[2])
            for j in range(6):
                fx, fy = x - 7 + j * 3, top - 4 - (j % 2) * 2
                c.paint(c.ellipse(fx, fy, 2.6, 2.6), FRUIT[(k + j) % 4])
    else:
        for k, x in enumerate(range(18, w - 16, 26)):
            c.paint(c.ellipse(x, top + 1, 11, 6), ROPE[1 + k % 2])
            c.paint(c.ellipse(x, top + 1, 7, 3.6), ROPE[0])
    # 천막
    colors = CLOTH[cloth]
    roof = c.polygon([(14, 4), (w - 14, 4), (w - 2, 34), (2, 34)])
    stripe = ((c.xx - w / 2) / (1 + (c.yy - 4) / 30 * 0.28) // 12).astype(int) % 2 == 0
    c.paint(roof, colors[1])
    c.paint(roof & stripe, PAPER[2])
    c.paint(roof & (c.yy < 8), colors[2])
    for x in range(2, w - 2):
        drop = 3 if (x // 9) % 2 else 6
        tint = colors[1] if int((x - w / 2) // 13) % 2 else PAPER[1]
        c.paint(c.rect(x, 34, x + 1, 36 + drop), tint)
        c.paint(c.rect(x, 36 + drop, x + 1, 37 + drop), colors[0])
    return outline(c.image), (w // 2, ground)


def sea_gate(seed):
    """
    바다로 들어가는 공간의 문: 이끼 낀 푸른 돌기둥 둘과 윗돌, 파도 문양. 문 안은 푸른 바다빛이 소용돌이친다(반쯤 비쳐 뒤의 인물이 보인다).
    기준점은 기둥 아래 가운데다.
    """
    rng = np.random.default_rng(seed)
    w, h = 124, 128
    c = Canvas(w, h)
    ground = h - 2
    cx = w // 2
    inner_left, inner_right, inner_top = cx - 40, cx + 40, 26
    # 기둥 둘
    for left in (cx - 58, cx + 40):
        pillar = c.rect(left, 18, left + 18, ground)
        c.paint(pillar, GATE_STONE[2])
        c.paint(c.rect(left, 18, left + 5, ground), GATE_STONE[3])
        c.paint(c.rect(left + 14, 18, left + 18, ground), GATE_STONE[1])
        for y in range(30, ground, 18):
            c.paint(c.rect(left, y, left + 18, y + 2), GATE_STONE[0])
        c.paint(c.rect(left - 3, ground - 10, left + 21, ground), GATE_STONE[1])
        c.paint(c.rect(left - 3, ground - 10, left + 21, ground - 7), GATE_STONE[3])
    # 윗돌: 가운데가 솟은 아치
    top = c.arch(cx - 60, 2, cx + 60, 60) & ~c.arch(cx - 40, 20, cx + 40, 90)
    c.paint(top, GATE_STONE[2])
    c.paint(top & (c.yy < 12), GATE_STONE[4])
    c.paint(top & (((c.xx - cx) % 20) == 0), GATE_STONE[0])
    # 쐐기돌의 파도 문양
    key = c.polygon([(cx - 10, 2), (cx + 10, 2), (cx + 7, 26), (cx - 7, 26)])
    c.paint(key, GATE_STONE[4])
    c.paint(key & (np.abs(c.yy - 12 - 3 * np.sin((c.xx - cx) / 3.0)) < 1.4), SEA_GLOW[3])
    moss = (pg.noise(h, w, 5, rng) > 0.66) & (c.image[..., 3] > 0)
    c.paint(moss, MOSS[1])
    image = outline(c.image)

    # 문 안: 소용돌이치는 바다빛. 반쯤 비친다.
    yy, xx = c.yy, c.xx
    portal = c.arch(inner_left, inner_top, inner_right, ground - 2)
    dx, dy = (xx + 0.5 - cx) / 40, (yy + 0.5 - (ground + inner_top) / 2) / 50
    away = np.hypot(dx, dy)
    swirl = np.sin(np.arctan2(dy, dx) * 3 + away * 9)
    tone = np.clip((1.05 - away) * 2.4 + swirl * 0.9 + (pg.noise(h, w, 6, rng) - 0.5) * 1.2, 0, 5).astype(int)
    image[portal, :3] = SEA_GLOW[tone[portal]]
    image[portal, 3] = 150 + tone[portal] * 14
    rim = portal & ~c.arch(inner_left + 3, inner_top + 3, inner_right - 3, ground)
    image[rim, :3] = SEA_GLOW[4]
    image[rim, 3] = 235
    return image, (cx, ground)
