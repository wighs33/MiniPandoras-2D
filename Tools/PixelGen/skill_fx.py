"""악마·불꽃·차원 판도라 스킬 이펙트(기획서 스케치대로 스킬 동작·이펙트를 바꾼다).

맵 그림과 같은 점 크기(한 칸 32점, 점 하나 = 2px)다. 판도라마다 모양 언어를 나눴다.
악마는 날카로운 직선(가시, 갈고리 손)과 검은 불길, 불꽃은 위로 솟는 둥근 불길, 차원은 깨진 사각 조각이다.

  Assets/MiniPandoras/Art/PixelGen/Effects/Skills/
    dark_hand_<0~2>.png   뻗는 손(오른쪽을 본다). 마지막 장은 움켜쥔 손
    dark_link_<0~2>.png   손과 몸을 잇는 검은 불길 마디
    spike_<0~6>.png       땅에서 솟는 가시(아래 가운데가 기준점)
    wings_<0~3>.png       검은 날개의 날갯짓(가운데가 등)
    darkfire_<0~5>.png    검은 불길 한 줄기(되풀이, 아래 가운데가 기준점)
    fire_<0~5>.png        불길 한 줄기(되풀이, 아래 가운데가 기준점)
    ember_ball_<0~3>.png  불씨탄(오른쪽으로 난다)
    ember_hit_<0~5>.png   불씨탄이 터지는 자리
    dive_hit_<0~6>.png    화염 낙하가 내리꽂히는 자리
    gate_<0~8>.png        차원 게이트가 열렸다 닫힌다(아래 가운데가 기준점)
    space_cut_<0~5>.png   공간이 사선으로 갈라졌다 닫힌다
    shard_<0~4>.png       깨진 사각 조각이 흩어진다
  Docs/ArtUpgrade/PixelGen/skill_fx_preview.png

사용: python Tools/PixelGen/skill_fx.py <프로젝트 루트>
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import pixelgen as pg

FIRE = pg.hexes("#a8230f", "#e8561a", "#ffa52e", "#ffe27a", "#fffbe0")
DARK_FIRE = pg.hexes("#150a20", "#2c1240", "#531a60", "#8e2474", "#d0508e")
THORN = pg.hexes("#150b1f", "#2e1540", "#5a2070", "#b8306f", "#ff9ac0")
SMOKE = pg.hexes("#3a2a2a", "#5a4438", "#7a6250")
PHASE = pg.hexes("#0e0a28", "#241a66", "#3a2aa0", "#7d6cff", "#ddd6ff", "#ffffff")
CRACK = pg.hexes("#0f0a14")[0]
TAU = math.pi * 2


def blank(width, height):
    return np.zeros((height, width, 4), np.uint8)


def over(canvas, mask, colour):
    canvas[mask, :3] = colour[:3]
    canvas[mask, 3] = 255


def grid(width, height):
    y, x = np.mgrid[0:height, 0:width]
    return x.astype(float), y.astype(float)


def outline(canvas, colour):
    """모양 둘레에 어두운 점을 한 겹 두른다."""
    solid = canvas[..., 3] > 0
    grown = solid.copy()
    grown[1:, :] |= solid[:-1, :]
    grown[:-1, :] |= solid[1:, :]
    grown[:, 1:] |= solid[:, :-1]
    grown[:, :-1] |= solid[:, 1:]
    over(canvas, grown & ~solid, colour)


def polygon_mask(width, height, points):
    image = Image.new("L", (width, height), 0)
    ImageDraw.Draw(image).polygon([(float(px), float(py)) for px, py in points], fill=255)
    return np.array(image) > 0


# ── 불길 ──────────────────────────────────────────────────

def tongue(x, y, width, height, cx, base, t, phase, tall):
    """불길 한 가닥의 세기(0~1). 위로 갈수록 가늘어지고 끝이 흔들린다."""
    up = np.clip((base - y) / (tall * height), 0.0, 1.5)
    u = (x - cx) / (width * 0.5)
    sway = 0.34 * up * np.sin(TAU * t + up * 5.0 + phase) + 0.14 * up * np.sin(TAU * 2 * t + up * 9.0 + phase * 2)
    taper = np.clip(1.0 - up, 0.0, 1.0) ** 0.62
    edge = np.abs(u - sway) / np.maximum(taper, 0.02)
    top = 0.9 + 0.1 * np.sin(TAU * t + phase * 3)
    inside = (edge < 1.0) & (up < top) & (y <= base)
    return np.where(inside, np.clip(1.0 - edge, 0.0, 1.0) ** 0.8 * np.clip(1.0 - up / top, 0.0, 1.0) ** 0.35, 0.0)


def paint_heat(canvas, heat, palette):
    for level, limit in enumerate((0.04, 0.24, 0.44, 0.62, 0.8)):
        over(canvas, heat > limit, palette[level])


def flame_frame(index, frames, palette):
    width, height = 40, 56
    canvas = blank(width, height)
    x, y = grid(width, height)
    t = index / frames
    heat = tongue(x, y, width * 0.9, height, width / 2, height - 2, t, 0.0, 0.98)
    heat = np.maximum(heat, tongue(x, y, width * 0.55, height, width * 0.28, height - 2, t, 2.1, 0.62))
    heat = np.maximum(heat, tongue(x, y, width * 0.55, height, width * 0.72, height - 2, t, 4.0, 0.72))
    # 밑동이 칼로 벤 듯 끊기지 않게 어둡게 식힌다.
    heat = heat * np.clip((height - 1 - y) / 6.0, 0.0, 1.0)
    paint_heat(canvas, heat, palette)
    # 불똥 하나가 올라간다.
    spark_y = int((1.0 - t) * (height - 10)) + 2
    spark_x = int(width / 2 + 11 * math.sin(TAU * t + 1.0))
    canvas[spark_y:spark_y + 2, spark_x:spark_x + 2] = (*palette[3][:3], 255)
    return canvas


def ball_frame(index, frames):
    width, height = 48, 32
    canvas = blank(width, height)
    x, y = grid(width, height)
    t = index / frames
    head_x, head_y = 34.0, 16.0
    behind = np.clip((head_x - x) / 32.0, 0.0, 1.0)
    wobble = 2.4 * behind * np.sin(TAU * t * 2 + behind * 7.0)
    half = 10.5 * (1.0 - behind) ** 0.8
    across = np.abs(y - head_y - wobble) / np.maximum(half, 0.3)
    front = np.hypot((x - head_x) / 10.5, (y - head_y) / 10.5)
    heat = np.where(x >= head_x, 1.0 - front, (1.0 - across) * (1.0 - behind * 0.92))
    heat = np.clip(heat, 0.0, 1.0) ** 0.6
    paint_heat(canvas, heat, FIRE)
    return canvas


def burst_heat(x, y, cx, cy, radius, squash, phase):
    """울퉁불퉁한 원 안쪽일수록 뜨겁다."""
    dx, dy = x - cx, (y - cy) / squash
    angle = np.arctan2(dy, dx)
    bumpy = radius * (1.0 + 0.2 * np.sin(5 * angle + phase) + 0.12 * np.sin(9 * angle + phase * 2))
    return np.clip(1.0 - np.hypot(dx, dy) / np.maximum(bumpy, 0.5), 0.0, 1.0)


def scatter_dots(canvas, seed, count, spread, colour, cx, cy, squash=1.0):
    rng = np.random.default_rng(seed)
    height, width = canvas.shape[:2]
    for _ in range(count):
        angle = rng.uniform(0, TAU)
        reach = rng.uniform(0.55, 1.0) * spread
        px = int(cx + math.cos(angle) * reach)
        py = int(cy + math.sin(angle) * reach * squash)
        if 0 <= px < width - 1 and 0 <= py < height - 1:
            canvas[py:py + 2, px:px + 2] = (*colour[:3], 255)


def ember_hit_frame(index):
    size = 72
    canvas = blank(size, size)
    x, y = grid(size, size)
    c = size / 2
    radii = (12, 22, 30, 32, 30, 26)
    heat = burst_heat(x, y, c, c, radii[index], 1.0, index * 1.3)
    if index >= 2:
        # 안쪽부터 식어 고리가 된다.
        heat = np.clip(heat * 1.8 - (index - 1) * 0.24, 0.0, 1.0)
    if index >= 3:
        speckle = (np.sin(x * 2.3 + index) * np.cos(y * 1.9 - index)) > (0.9 - index * 0.28)
        heat = np.where(speckle, heat, 0.0)
    if index == 0:
        heat = np.where(heat > 0, 1.0, 0.0)
    paint_heat(canvas, heat, FIRE)
    if index >= 1:
        scatter_dots(canvas, 11, 12, 16 + index * 5, FIRE[2 if index < 4 else 1], c, c)
    return canvas


def dive_hit_frame(index):
    width, height = 144, 96
    canvas = blank(width, height)
    x, y = grid(width, height)
    cx, ground = width / 2, height * 0.74
    radii = (20, 44, 60, 66, 66, 62, 56)
    # 바닥에 깔리는 불 고리.
    heat = burst_heat(x, y, cx, ground, radii[index], 0.5, index * 1.1)
    if index >= 1:
        heat = np.clip(heat * 2.1 - index * 0.26, 0.0, 1.0)
    if index == 0:
        heat = np.where(heat > 0, 1.0, 0.0)
    # 가운데에서 치솟는 기둥.
    tall = (0.5, 1.0, 1.0, 0.86, 0.64, 0.4, 0.2)[index]
    pillar = tongue(x, y, 58, height, cx, ground + 2, index / 7, 0.4, tall * 0.72)
    pillar = np.maximum(pillar, tongue(x, y, 38, height, cx - 30, ground + 2, index / 7, 2.0, tall * 0.46))
    pillar = np.maximum(pillar, tongue(x, y, 38, height, cx + 30, ground + 2, index / 7, 4.0, tall * 0.4))
    heat = np.maximum(heat, pillar)
    if index >= 4:
        speckle = (np.sin(x * 1.7 + index * 2) * np.cos(y * 2.1)) > (1.1 - index * 0.2)
        heat = np.where(speckle, heat, 0.0)
    paint_heat(canvas, heat, FIRE)
    # 바닥이 갈라진다.
    if index >= 1:
        reach = min(1.0, index / 3)
        for angle in (0.25, 1.3, 2.7, 3.5, 4.5, 5.6):
            end_x = cx + math.cos(angle) * 58 * reach
            end_y = ground + math.sin(angle) * 22 * reach
            mid_x = (cx + end_x) / 2 + 5 * math.sin(angle * 5)
            mid_y = (ground + end_y) / 2 + 2
            crack = Image.new("L", (width, height), 0)
            ImageDraw.Draw(crack).line([(cx, ground), (mid_x, mid_y), (end_x, end_y)], fill=255, width=2)
            mask = np.array(crack) > 0
            over(canvas, mask & (canvas[..., 3] == 0), CRACK)
    if index >= 2:
        scatter_dots(canvas, 23, 16, 34 + index * 6, FIRE[1 if index > 4 else 2], cx, ground - 8, 0.7)
    return canvas


# ── 가시 ──────────────────────────────────────────────────

# 가시마다 (밑동 x, 높이, 기울기, 밑동 반폭, 뒤에서 앞으로 그리는 차례대로).
THORNS = ((14, 44, -9, 5), (50, 50, 8, 5), (24, 68, -5, 6.5), (42, 60, 4, 6), (32, 80, 1, 7.5))
SPIKE_SCALE = (0.22, 0.72, 1.08, 1.0, 0.96, 0.72, 0.34)


def spike_frame(index):
    width, height = 64, 88
    canvas = blank(width, height)
    ground = height - 8
    scale = SPIKE_SCALE[index]
    for base_x, tall, lean, half in THORNS:
        top = tall * scale
        tip = (base_x + lean * scale, ground - top)
        whole = polygon_mask(width, height, [(base_x - half, ground), tip, (base_x + half, ground)])
        lit = polygon_mask(width, height, [(base_x - half, ground), tip, (base_x - half * 0.2, ground)])
        over(canvas, whole, THORN[1])
        over(canvas, lit, THORN[2])
        # 끝으로 갈수록 붉게 달아오른다.
        y = np.mgrid[0:height, 0:width][0]
        hot = whole & (y < tip[1] + top * 0.26)
        over(canvas, hot, THORN[3])
        over(canvas, whole & (y < tip[1] + top * 0.1), THORN[4])
    outline(canvas, THORN[0])
    # 솟는 순간 땅이 깨지고, 끝날 때는 조각이 흩어진다.
    x, y = grid(width, height)
    pit = np.hypot((x - width / 2) / (26 * min(1.0, 0.5 + index * 0.25)), (y - ground) / 6.0) < 1.0
    over(canvas, pit & (canvas[..., 3] == 0), CRACK)
    if index in (2, 3, 4):
        scatter_dots(canvas, 40 + index, 10, 22 + index * 4, THORN[3], width / 2, ground - 16, 1.2)
    if index >= 5:
        speckle = (np.sin(x * 2.1) * np.cos(y * 2.7 + index)) > (0.55 - (index - 5) * 0.7)
        canvas[speckle, 3] = 0
    return canvas


# ── 손 ───────────────────────────────────────────────────

def finger(base, angle, length, half):
    """갈고리처럼 끝이 안쪽으로 굽은 손가락의 윤곽."""
    a = math.radians(angle)
    bend = a * 0.45
    direction = (math.cos(a), math.sin(a))
    normal = (-direction[1], direction[0])
    knuckle = (base[0] + direction[0] * length * 0.62, base[1] + direction[1] * length * 0.62)
    tip = (knuckle[0] + math.cos(a - bend) * length * 0.5, knuckle[1] + math.sin(a - bend) * length * 0.5)
    return [(base[0] + normal[0] * half, base[1] + normal[1] * half), (knuckle[0] + normal[0] * half * 0.7, knuckle[1] + normal[1] * half * 0.7),
            tip, (knuckle[0] - normal[0] * half * 0.7, knuckle[1] - normal[1] * half * 0.7),
            (base[0] - normal[0] * half, base[1] - normal[1] * half)]


def hand_frame(index):
    width, height = 64, 48
    canvas = blank(width, height)
    x, y = grid(width, height)
    palm = (18.0, 24.0)
    spread, length = ((1.0, 40), (1.18, 42), (0.34, 24))[index]
    body = np.hypot((x - palm[0]) / 10.0, (y - palm[1]) / 8.0) < 1.0
    for angle, grow in ((-34, 0.86), (-11, 1.0), (11, 1.0), (34, 0.86)):
        body |= polygon_mask(width, height, finger((palm[0] + 4, palm[1] + angle * 0.14), angle * spread, length * grow, 3.2))
    body |= polygon_mask(width, height, finger((palm[0], palm[1] + 5), 78 * spread, 18, 3.0))
    over(canvas, body, THORN[1])
    # 위쪽 면이 밝고 손끝이 달아오른다.
    upper = body & ~np.roll(body, -2, axis=0)
    over(canvas, upper, THORN[2])
    tips = body & (np.hypot(x - palm[0], y - palm[1]) > (31 if index < 2 else 20))
    over(canvas, tips, THORN[3])
    over(canvas, body & (np.hypot(x - palm[0], y - palm[1]) > (38 if index < 2 else 25)), THORN[4])
    # 손목 쪽은 검은 불길로 흩어진다.
    t = index / 3
    wrist = (x < 11) & (np.abs(y - palm[1] - 3 * np.sin(x * 0.9 + TAU * t)) < (x * 0.7))
    over(canvas, wrist, DARK_FIRE[1])
    outline(canvas, THORN[0])
    return canvas


def link_frame(index):
    width, height = 24, 16
    canvas = blank(width, height)
    x, y = grid(width, height)
    t = index / 3
    swell = 5.4 + 1.2 * np.sin(x * 0.7 + TAU * t)
    body = np.abs(y - 8 - 1.6 * np.sin(x * 0.5 + TAU * t)) < swell
    over(canvas, body, DARK_FIRE[0])
    over(canvas, np.abs(y - 8 - 1.6 * np.sin(x * 0.5 + TAU * t)) < swell * 0.55, DARK_FIRE[1])
    over(canvas, np.abs(y - 8 - 1.6 * np.sin(x * 0.5 + TAU * t)) < 1.3, DARK_FIRE[3])
    return canvas


# ── 날개 ──────────────────────────────────────────────────

def wing_points(lift):
    """오른쪽 날개의 뼈 끝(어깨 기준). lift가 작을수록 접힌다."""
    elbow = (20, -22 * lift)
    tips = ((56, -38 * lift), (58, -8 * lift), (46, 14 * lift), (27, 25 * lift))
    return elbow, tips


def wings_frame(index):
    width, height = 128, 88
    half = blank(width // 2, height)
    lift = (1.0, 0.86, 0.68, 0.86)[index]
    shoulder = (2.0, 48.0)
    elbow, tips = wing_points(lift)
    points = lambda p: (shoulder[0] + p[0], shoulder[1] + p[1])
    outline_points = [shoulder, points(elbow), points(tips[0])]
    for left, right in zip(tips, tips[1:]):
        # 뼈 사이의 막은 안쪽으로 파인다.
        outline_points.append(points(((left[0] + right[0]) / 2 - 7, (left[1] + right[1]) / 2 - 3)))
        outline_points.append(points(right))

    w, h = half.shape[1], half.shape[0]
    membrane = polygon_mask(w, h, outline_points)
    x, y = grid(w, h)
    over(half, membrane, THORN[1])
    over(half, membrane & ((x - y * 0.4) > 22), THORN[2])
    for tip in tips:
        bone = Image.new("L", (w, h), 0)
        ImageDraw.Draw(bone).line([points(elbow), points(tip)], fill=255, width=2)
        over(half, np.array(bone) > 0, THORN[0])

    arm = Image.new("L", (w, h), 0)
    ImageDraw.Draw(arm).line([shoulder, points(elbow), points(tips[0])], fill=255, width=3)
    over(half, np.array(arm) > 0, THORN[3])
    outline(half, THORN[0])
    canvas = blank(width, height)
    canvas[:, width // 2:] = half
    canvas[:, :width // 2] = half[:, ::-1]
    return canvas


# ── 차원 ──────────────────────────────────────────────────

# 게이트가 벌어진 정도: 열리고, 서 있다가, 닫힌다.
GATE_SCALE = (0.16, 0.5, 0.86, 1.06, 1.0, 1.0, 1.0, 0.66, 0.24)
CUT_LENGTH = (30, 44, 46, 46, 44, 40)
CUT_WIDTH = (1.2, 3.2, 6.5, 5.0, 2.4, 0.8)


def square_chips(canvas, seed, count, spread, cx, cy, sizes, colours):
    """둘레에 사각 조각을 흩뿌린다. 같은 seed면 조각마다 같은 쪽으로 난다."""
    rng = np.random.default_rng(seed)
    for _ in range(count):
        angle = rng.uniform(0, TAU)
        reach = rng.uniform(0.35, 1.0) * spread
        side = int(rng.integers(sizes[0], sizes[1] + 1))
        colour = colours[int(rng.integers(0, len(colours)))]
        left = int(cx + math.cos(angle) * reach)
        top = int(cy + math.sin(angle) * reach)
        canvas[max(top, 0):max(top + side, 0), max(left, 0):max(left + side, 0), :3] = colour
        canvas[max(top, 0):max(top + side, 0), max(left, 0):max(left + side, 0), 3] = 255


def gate_frame(index):
    width, height = 72, 104
    canvas = blank(width, height)
    ground = height - 10
    scale = GATE_SCALE[index]
    cx, cy = width / 2, ground - 42
    x, y = grid(width, height)
    pool = np.hypot((x - cx) / (28 * scale), (y - ground) / (7 * scale))
    over(canvas, pool < 1.0, PHASE[3])
    over(canvas, pool < 0.8, PHASE[1])

    # 세로로 갈라진 틈. 세 점씩 묶어 가장자리가 사각 조각처럼 깨진다.
    rows, columns = (y // 3).astype(int), (x // 3).astype(int)
    chip = np.random.default_rng(70).uniform(-3.5, 3.5, (height // 3 + 1, width // 3 + 1))[rows, columns]
    tall = 44 * min(1.0, scale + 0.3)
    along = np.clip(1 - ((rows * 3 + 1 - cy) / tall) ** 2, 0, 1)
    depth = 23 * scale * along ** 0.6 + chip * scale - np.abs(columns * 3 + 1 - cx)
    inside = (depth > 0) & (along > 0)
    over(canvas, inside, PHASE[4])
    over(canvas, inside & (depth > 3), PHASE[3])
    over(canvas, inside & (depth > 6), PHASE[1])
    over(canvas, inside & (depth > 10), PHASE[0])
    stars = (np.sin(x * 1.7 + index) * np.cos(y * 2.3 - index * 0.7)) > 0.93
    over(canvas, inside & (depth > 7) & stars, PHASE[5])
    outline(canvas, PHASE[0])

    square_chips(canvas, 31 + index // 3, 12, 8 + 34 * scale, cx, cy - index, (2, 4), PHASE[3:5])
    if index == len(GATE_SCALE) - 1:
        canvas[(np.sin(x * 2.1) * np.cos(y * 2.7)) > 0.2, 3] = 0
    return canvas


def space_cut_frame(index):
    size = 96
    canvas = blank(size, size)
    x, y = grid(size, size)
    centre = (size - 1) / 2
    # 왼쪽 아래에서 오른쪽 위로 갈라진다.
    along = ((x - centre) - (y - centre)) / math.sqrt(2)
    across = ((x - centre) + (y - centre)) / math.sqrt(2)
    lens = CUT_WIDTH[index] * np.clip(1 - (along / CUT_LENGTH[index]) ** 2, 0, 1)
    slit = np.abs(across) < lens
    over(canvas, np.abs(across) < lens + 1.5 * (lens > 0), PHASE[3])
    over(canvas, slit, PHASE[4])
    if index in (2, 3):
        # 가장 벌어진 동안은 틈 안의 어둠이 보인다.
        over(canvas, np.abs(across) < lens - 1.6, PHASE[0])
    if index >= 1:
        square_chips(canvas, 52, 12, 10 + index * 7, centre, centre, (2, 4), PHASE[3:5])
    outline(canvas, PHASE[1])
    if index >= 4:
        canvas[(np.sin(x * 2.1) * np.cos(y * 2.7 + index)) > (0.5 - (index - 4) * 0.5), 3] = 0
    return canvas


def shard_frame(index):
    size = 40
    canvas = blank(size, size)
    square_chips(canvas, 11, 8, 5 + index * 3.5, size / 2, size / 2, (max(1, 4 - index), max(2, 5 - index)), PHASE[3:5])
    outline(canvas, PHASE[1])
    return canvas


# ── 내보내기 ──────────────────────────────────────────────

def sheets():
    return {
        "dark_hand": [hand_frame(i) for i in range(3)],
        "dark_link": [link_frame(i) for i in range(3)],
        "spike": [spike_frame(i) for i in range(len(SPIKE_SCALE))],
        "wings": [wings_frame(i) for i in range(4)],
        "darkfire": [flame_frame(i, 6, DARK_FIRE) for i in range(6)],
        "fire": [flame_frame(i, 6, FIRE) for i in range(6)],
        "ember_ball": [ball_frame(i, 4) for i in range(4)],
        "ember_hit": [ember_hit_frame(i) for i in range(6)],
        "dive_hit": [dive_hit_frame(i) for i in range(7)],
        "gate": [gate_frame(i) for i in range(len(GATE_SCALE))],
        "space_cut": [space_cut_frame(i) for i in range(len(CUT_WIDTH))],
        "shard": [shard_frame(i) for i in range(5)],
    }


def preview(all_sheets, project):
    rows = []
    for frames in all_sheets.values():
        height = max(frame.shape[0] for frame in frames)
        width = sum(frame.shape[1] + 6 for frame in frames) + 6
        row = Image.new("RGBA", (width, height + 8), (46, 74, 44, 255))
        left = 6
        for frame in frames:
            row.alpha_composite(Image.fromarray(frame, "RGBA"), (left, 4 + height - frame.shape[0]))
            left += frame.shape[1] + 6
        rows.append(row)

    sheet = Image.new("RGBA", (max(r.width for r in rows), sum(r.height for r in rows)), (46, 74, 44, 255))
    top = 0
    for row in rows:
        sheet.alpha_composite(row, (0, top))
        top += row.height
    sheet = sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST)
    sheet.save(project / "Docs/ArtUpgrade/PixelGen/skill_fx_preview.png")


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Effects/Skills"
    out.mkdir(parents=True, exist_ok=True)
    all_sheets = sheets()
    for name, frames in all_sheets.items():
        for i, frame in enumerate(frames):
            Image.fromarray(pg.upscale(frame), "RGBA").save(out / f"{name}_{i}.png")
    preview(all_sheets, project)
    print(out, sum(len(frames) for frames in all_sheets.values()))


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
