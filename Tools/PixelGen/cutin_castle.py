"""2장 뱀성을 처음 마주하는 컷인 그림(웹게임 패널 대신 생성기로).

두 장면을 그린다. 성 그림은 맵과 같은 castle.py 소품이다(원경은 절반 크기).
  wide.png        원경(가상 384×360, 세로로 길다): 강가에서 올려다본 뱀성. 강과 나무다리, 양 끝이 휘어 오른 절벽 세 단을
                  지그재그로 오르는 돌계단과 솔숲, 산꼭대기 성벽·성문·망루, 그 위로 돌뱀이 감아 오른 탑과 꼭대기의 돌뱀 머리.
                  오후 햇살, 구름, 먼 산, 앞쪽 수풀. 카메라는 아래(강가)에서 시작해 탑 꼭대기까지 올라간다.
  party.png       원경 앞에 서는 주인공과 랑의 뒷모습(웹게임 인물 그림을 도트 그대로 네 배)
  head.png        근경(가상 384×216): 탑 꼭대기의 돌뱀 머리와 초록 원뿔 지붕
  head_glow.png   돌뱀 눈에서 번지는 붉은 빛(근경 위에 겹쳐 진하기를 바꾼다)
그림 안 자리(가상 px)는 이야기 코드(Chapter2)가 쓴다: 원경의 주인공 발밑 PARTY_FOOT, 돌뱀 머리 HEAD_WIDE, 근경의 머리 HEAD_CLOSE.

사용: python Tools/PixelGen/cutin_castle.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import bridges
import castle
import glider_backdrop
import pixelgen as pg
import props
from houses import WOOD

W = 768                     # 점(가상 화면 384의 두 배). 그림은 두 배로 키워 쓴다.
WIDE_H = 720                # 원경 높이(가상 360)
CLOSE_H = 432               # 근경 높이(가상 216)
CX = W // 2
PLATEAU = 420               # 산꼭대기(성벽이 서는 줄)
RIVER = (636, 668)          # 강(위, 아래)
TIERS = [(420, 478, 492), (492, 548, 562), (562, 618, 636)]   # 절벽 단: (윗면, 벽 아래, 다음 단)
HILL_TOP = 236              # 산꼭대기 반폭(점). 아래로 내려갈수록 넓어진다
STAIRS_AT = [470, 300, 384]  # 단마다 돌계단 가운데(맵처럼 지그재그로 오른다)
STAIR_HALF = 13
SKY_STOPS = [(0.0, "#36599e"), (0.45, "#6f9fd4"), (0.78, "#b6cde4"), (1.0, "#efe0bc")]
CLOUD_DAY = pg.hexes("#8ea4c2", "#b2c4da", "#d6e1ec", "#eef3f8", "#ffffff")
FAR_HILLS = pg.hexes("#7f95b5", "#6d84a6")
NEAR_HILLS = pg.hexes("#5a7a74", "#4c6b66")
CLIFF = pg.hexes("#3a3b44", "#4f515c", "#676a75", "#80838e", "#9a9da7", "#b6b9c1")
LEDGE = pg.GRASS
EYE_GLOW = np.array([255, 96, 40])
PARCHMENT = pg.hexes("#7a5a34", "#b08d57", "#d9bd85", "#efdcae")
RED_MARK = pg.hexes("#b0302a")[0]
MAP_CENTER = (384, 216)     # 지도 근경의 가운데(점)
MAP_HALF = (216, 136)       # 지도의 가로·세로 절반(점)
# 그림 안 자리(가상 px): 원경 주인공 발밑, 원경 돌뱀 머리, 근경 돌뱀 머리
PARTY_FOOT = (118, 352)
PARTY_HEIGHT = 64
HEAD_WIDE = (CX / 2, 0)     # main()이 탑을 놓은 뒤 채운다
HEAD_CLOSE = (CX / 2, 0)


def sky(height, horizon, rng):
    """오후 하늘: 위는 짙은 파랑, 지평선은 옅은 금빛. 띠 경계만 디더로 섞는다. 왼쪽 위에 해의 빛무리."""
    yy, xx = np.mgrid[0:height, 0:W]
    t = np.clip((yy + 0.5) / horizon, 0, 1)
    stepped = np.floor(t * 16 + (pg.bayer(height, W) - 0.5) * 0.9) / 16
    image = pg.mix_stops(SKY_STOPS, stepped)
    glow = np.exp(-np.hypot(xx - 140, (yy - 120) * 1.2) / 160)
    glow = np.floor(glow * 6 + (pg.bayer(height, W) - 0.5) * 0.9) / 6
    image = image * (1 - glow[..., None] * 0.45) + np.array([255, 246, 220]) * glow[..., None] * 0.45
    return image


def day_cloud(rng, width, height):
    """낮 구름: 글라이더 구름 모양을 흰빛·옅은 회청색으로 다시 칠한다."""
    image = glider_backdrop.cloud(rng, width, height)
    for level, color in enumerate(glider_backdrop.CLOUD):
        match = (image[..., :3] == color).all(axis=-1) & (image[..., 3] > 0)
        image[match, :3] = CLOUD_DAY[level]
    return image


def paste(canvas, image, left, top, haze=0.0, haze_color=None):
    """RGB 캔버스에 RGBA 그림을 붙인다. haze만큼 하늘빛을 섞어 멀리 있는 것처럼 흐리게 한다."""
    h, w = image.shape[:2]
    y0, x0 = max(0, top), max(0, left)
    y1, x1 = min(canvas.shape[0], top + h), min(canvas.shape[1], left + w)
    if y0 >= y1 or x0 >= x1:
        return
    part = image[y0 - top:y1 - top, x0 - left:x1 - left].astype(float)
    alpha = part[..., 3:4] / 255
    rgb = part[..., :3]
    if haze:
        rgb = rgb * (1 - haze) + haze_color * haze
    canvas[y0:y1, x0:x1] = canvas[y0:y1, x0:x1] * (1 - alpha) + rgb * alpha


def half(image):
    """맵 그림(점)을 절반 크기로: 2×2 평균 뒤 반투명 가장자리를 끊는다."""
    small = Image.fromarray(image, "RGBA").resize((image.shape[1] // 2, image.shape[0] // 2), Image.BOX)
    out = np.array(small)
    out[..., 3] = np.where(out[..., 3] > 120, 255, 0)
    return out


def hills(canvas, rng, top, amplitude, colors, seed_shift):
    """먼 산 띠: 사인 여럿을 겹친 능선 아래를 칠하고, 능선 바로 아래는 한 단 밝게."""
    xs = np.arange(W)
    ridge = top + sum(np.sin(xs / period + phase) * amp for period, phase, amp in
                      ((90, seed_shift, amplitude), (37, seed_shift * 2, amplitude * 0.4), (13, seed_shift * 3, amplitude * 0.12)))
    yy = np.arange(canvas.shape[0])[:, None]
    below = yy >= ridge[None, :]
    canvas[below] = colors[1]
    canvas[below & (yy < ridge[None, :] + 4)] = colors[0]


def hill_half(y):
    """그 줄에서 성이 선 언덕의 반폭(점): 꼭대기에서 강으로 내려갈수록 넓어진다."""
    return HILL_TOP + np.maximum(0, y - PLATEAU) * 0.62


def tier_line(y, x):
    """절벽 단의 윗선: 가운데는 평평하고 언덕 가장자리로 가며 조금 휘어 올라간다(맵의 절벽과 같은 모양)."""
    return y - 14 * np.clip(np.abs(x - CX) / hill_half(y) - 0.6, 0, None) / 0.4


def mountain(canvas, rng):
    """
    성이 선 언덕: 꼭대기에서 강으로 갈수록 넓어지는 둥근 언덕(풀 비탈)에, 단마다 이끼 낀 윗선·세로 바위판 벽·아래 풀 턱.
    벽은 언덕 안에만 있고 양옆은 솔숲 비탈이다. 돌려주는 값: 단마다 (윗선, 아랫선, 다음 단 윗선) 배열(점).
    """
    xs = np.arange(W)
    yy = np.arange(canvas.shape[0])[:, None]
    grain = pg.fractal_noise(canvas.shape[0], W, rng, ((6, 0.6), (2, 0.4)))
    moss = pg.fractal_noise(canvas.shape[0], W, rng, ((8, 0.6), (3, 0.4)))
    # 언덕 몸(풀 비탈): 윤곽은 꼭대기 반폭에서 바깥으로 비스듬히 내려간다
    outline = PLATEAU - 4 + np.maximum(0, np.abs(xs - CX) - HILL_TOP) / 0.62
    body = yy >= outline[None, :]
    canvas[body] = LEDGE[2]
    canvas[body & (moss > 0.6)] = LEDGE[3]
    canvas[body & (yy < outline[None, :] + 2)] = LEDGE[4]
    # 세로 바위판: 열마다 판 안 가로 자리(0~1)
    slab_u = np.zeros(W)
    x = 0
    while x < W:
        w = int(rng.integers(5, 14))
        slab_u[x:x + w] = (np.arange(min(w, W - x)) + 0.5) / w
        x += w
    lines = []
    for top, bottom, ledge_bottom in TIERS:
        top_line, bottom_line, next_line = tier_line(top, xs), tier_line(bottom, xs), tier_line(ledge_bottom, xs)
        lines.append((top_line, bottom_line, next_line))
        inside = (np.abs(xs - CX) < hill_half(bottom) - 6)[None, :]
        face = inside & (yy >= top_line[None, :] + 5) & (yy < bottom_line[None, :])
        lip = inside & (yy >= top_line[None, :]) & (yy < top_line[None, :] + 5)
        ledge = inside & (yy >= bottom_line[None, :]) & (yy < next_line[None, :])
        depth = (yy - top_line[None, :]) / np.maximum(bottom_line - top_line, 1)[None, :]
        u = slab_u[None, :]
        level = np.where(u > 0.86, 0, np.where(u < 0.3, 4, 3)) - (depth > 0.7) - (grain < 0.3)
        canvas[face] = CLIFF[np.clip(level, 0, 5)][face]
        canvas[lip] = np.where((moss > 0.5)[..., None], LEDGE[4], CLIFF[5])[lip]
        canvas[ledge] = LEDGE[3]
        canvas[ledge & (moss > 0.62)] = LEDGE[4]
        canvas[ledge & (yy < bottom_line[None, :] + 2)] = LEDGE[1]
        # 벽 양 끝은 비탈 풀이 덮어 둥글게 사라진다
        edge = (np.abs(np.abs(xs - CX) - (hill_half(bottom) - 6)) < 3)[None, :] & (yy >= top_line[None, :]) & (yy < bottom_line[None, :])
        canvas[edge] = LEDGE[2]
    return lines


def stairs(canvas, lines):
    """단마다 돌계단(디딤판은 밝고 챌판은 어둡다, 양옆 낮은 돌난간)과, 계단 사이를 잇는 단 위 흙길."""
    for (top_line, bottom_line, next_line), cx in zip(lines, STAIRS_AT):
        y0, y1 = int(top_line[cx]) - 2, int(next_line[cx])
        x0, x1 = cx - STAIR_HALF, cx + STAIR_HALF
        for y in range(y0, y1):
            step = (y - y0) % 6
            canvas[y, x0:x1] = CLIFF[5] if step < 2 else CLIFF[3] if step < 5 else CLIFF[1]
        canvas[y0:y1, x0 - 3:x0] = CLIFF[2]
        canvas[y0:y1, x1:x1 + 3] = CLIFF[2]
        canvas[y0:y1, x0 - 4] = CLIFF[0]
        canvas[y0:y1, x1 + 3] = CLIFF[0]
    # 단 위 흙길: 위 계단 아래에서 다음 계단 위까지
    for (top_line, bottom_line, next_line), here, there in zip(lines, STAIRS_AT, STAIRS_AT[1:]):
        y = int((bottom_line[here] + next_line[here]) / 2)
        a, b = sorted((here, there))
        canvas[y - 3:y + 3, a - STAIR_HALF:b + STAIR_HALF] = pg.DIRT[3]
        canvas[y - 3, a - STAIR_HALF:b + STAIR_HALF] = pg.DIRT[1]


def forest(canvas, rng, lines):
    """솔숲: 양옆 비탈과 단 위에 소나무. 멀수록(위로 갈수록) 작고 흐리다. 계단과 흙길 자리는 비운다."""
    spots = []
    for _ in range(110):
        x = rng.uniform(0, W)
        y = rng.uniform(PLATEAU + 10, RIVER[0] - 4)
        if y < PLATEAU - 4 + max(0, abs(x - CX) - HILL_TOP) / 0.62 + 8:
            continue
        if np.abs(x - CX) < hill_half(y) - 6:
            # 산 몸 위: 단의 풀 턱 자리에만
            for top_line, bottom_line, next_line in lines:
                if bottom_line[int(x)] + 4 <= y <= next_line[int(x)] - 2:
                    break
            else:
                continue
            if any(abs(x - s) < STAIR_HALF + 14 for s in STAIRS_AT):
                continue
        spots.append((x, y))
    sky_color = pg.hexes("#b6cde4")[0].astype(float)
    for x, y in sorted(spots, key=lambda s: s[1]):
        scale = 0.55 + (y - PLATEAU) / (RIVER[0] - PLATEAU) * 0.6
        tree, pivot = props.pine(np.random.default_rng(int(x * 7 + y)), width=int(36 * scale) // 2 * 2, height=int(62 * scale))
        haze = 0.3 * (1 - (y - PLATEAU) / (RIVER[0] - PLATEAU))
        paste(canvas, tree, int(x - pivot[0]), int(y - pivot[1]), haze, sky_color)


def river_and_bridge(canvas, rng):
    """강: 하늘빛이 비친 가로 물결, 물가의 짙은 둑. 강을 건너 맨 아래 계단으로 이어지는 나무다리(멀어질수록 좁아진다)."""
    top, bottom = RIVER
    yy, xx = np.mgrid[top:bottom, 0:W]
    shade = pg.fractal_noise(bottom - top, W, rng, ((24, 0.6), (6, 0.4)))
    level = 3 + pg.dither_levels(shade, (0.45, 0.62), 0.1)
    canvas[top:bottom] = pg.WATER[level]
    ripples = (rng.random((bottom - top, W)) < 0.03) & ((xx % 7) < 4)
    canvas[top:bottom][ripples] = pg.WATER[6]
    canvas[top:top + 3] = pg.DIRT[1]
    canvas[bottom - 2:bottom] = pg.DIRT[2]
    # 다리: 아래(가까운 쪽)는 넓고 위는 좁은 사다리꼴 판자 바닥, 양옆 들보와 기둥, 귀퉁이 등불
    for y in range(top - 6, bottom + 4):
        t = (y - (top - 6)) / (bottom + 4 - (top - 6))
        half_width = 22 + t * 10
        x0, x1 = int(CX - half_width), int(CX + half_width)
        plank = (y - top) % 4
        canvas[y, x0:x1] = WOOD[2 if plank == 3 else 3 if (y // 4) % 3 else 4]
        canvas[y, x0:x0 + 3] = WOOD[1]
        canvas[y, x1 - 3:x1] = WOOD[1]
    post, pivot = bridges.lantern_post(rng)
    post = half(post)
    for x, y, side in ((CX - 24, top - 2, -1), (CX + 24, top - 2, 1), (CX - 33, bottom + 6, -1), (CX + 33, bottom + 6, 1)):
        image = post if side > 0 else post[:, ::-1]
        paste(canvas, image, int(x - image.shape[1] / 2), int(y - image.shape[0]))


def foreground(canvas, rng):
    """앞쪽 강가: 풀밭과 풀잎, 들꽃. 양 아래 귀퉁이에 짙은 수풀이 화면을 감싼다."""
    top = RIVER[1]
    height = canvas.shape[0] - top
    canvas[top:] = pg.GRASS[3]
    shade = pg.fractal_noise(height, W, rng, ((20, 0.6), (6, 0.4)))
    canvas[top:][shade > 0.6] = pg.GRASS[4]
    for _ in range(900):
        x, y = int(rng.integers(0, W)), int(rng.integers(top + 2, canvas.shape[0]))
        for k in range(int(rng.integers(2, 6))):
            if top <= y - k < canvas.shape[0]:
                canvas[y - k, x] = pg.GRASS[5 if k > 2 else 4]
    for _ in range(40):
        x, y = int(rng.integers(0, W)), int(rng.integers(top + 4, canvas.shape[0] - 2))
        canvas[y:y + 2, x:x + 2] = pg.FLOWERS[int(rng.integers(0, len(pg.FLOWERS)))]
    # 짙은 수풀(가까워서 어둡게)
    yy, xx = np.mgrid[0:canvas.shape[0], 0:W]
    for cx, cy, r in ((20, 700, 90), (90, 735, 70), (W - 30, 690, 100), (W - 110, 740, 70)):
        bush = np.hypot(xx - cx, (yy - cy) * 1.2) < r + pg.fractal_noise(canvas.shape[0], W, rng, ((10, 1.0),)) * 14
        canvas[bush] = pg.LEAF[1]
        canvas[bush & (yy < cy - r * 0.45)] = pg.LEAF[2]


def fortress(canvas, rng):
    """산꼭대기 성(절반 크기): 뒤에 돌뱀 탑, 앞에 성벽 두 토막과 가운데 성문, 양 끝 원뿔 망루. 돌려주는 값: 돌뱀 머리 가운데(점)."""
    haze_color = pg.hexes("#b6cde4")[0].astype(float)
    tower = half(castle.serpent_tower(np.random.default_rng(9))[0])
    tower_left, tower_top = CX - tower.shape[1] // 2, 34
    paste(canvas, tower, tower_left, tower_top, 0.08, haze_color)
    eyes = eye_centers(tower)
    head = (tower_left + np.mean([e[0] for e in eyes]), tower_top + np.mean([e[1] for e in eyes]))
    wall = half(castle.curtain_wall(9, np.random.default_rng(3))[0])
    for left in (CX - 48 - wall.shape[1], CX + 48):
        paste(canvas, wall, left, PLATEAU - wall.shape[0] + 2, 0.08, haze_color)
    cone = half(castle.tower(np.random.default_rng(4), width=76, body_h=150, roof="cone")[0])
    for x in (CX - 48 - wall.shape[1], CX + 48 + wall.shape[1]):
        paste(canvas, cone, x - cone.shape[1] // 2, PLATEAU + 6 - cone.shape[0], 0.08, haze_color)
    gate = half(castle.gatehouse(np.random.default_rng(2), closed=False)[0])
    paste(canvas, gate, CX - gate.shape[1] // 2, PLATEAU + 4 - gate.shape[0], 0.06, haze_color)
    return head


def eye_centers(image):
    """돌뱀 머리의 두 보석 눈 가운데(그림 점 자리): 위쪽 4할 안의 붉은 점들을 왼쪽·오른쪽으로 나눠 평균한다."""
    rgb = image[..., :3].astype(int)
    red = (image[..., 3] > 0) & (rgb[..., 0] > 190) & (rgb[..., 1] < 140) & (rgb[..., 2] < 90)
    red[int(image.shape[0] * 0.4):] = False
    ys, xs = np.nonzero(red)
    middle = image.shape[1] / 2
    return [(xs[side].mean(), ys[side].mean()) for side in (xs < middle, xs >= middle)]


def birds(canvas, rng):
    """하늘의 새 몇 마리(작은 V)."""
    for _ in range(6):
        x, y = int(rng.integers(470, 720)), int(rng.integers(70, 200))
        for k in range(3):
            canvas[y + k // 2, x + k] = (40, 44, 60)
            canvas[y + k // 2, x - k] = (40, 44, 60)


def wide(rng):
    canvas = sky(WIDE_H, 520, rng)
    for x, y, w, h in ((40, 150, 170, 52), (520, 90, 210, 64), (300, 250, 140, 44), (610, 300, 120, 40)):
        paste(canvas, day_cloud(rng, w, h), x, y)
    birds(canvas, rng)
    hills(canvas, rng, 430, 22, FAR_HILLS, 1.3)
    hills(canvas, rng, 500, 16, NEAR_HILLS, 2.1)
    head = fortress(canvas, rng)
    lines = mountain(canvas, rng)
    stairs(canvas, lines)
    forest(canvas, rng, lines)
    river_and_bridge(canvas, rng)
    foreground(canvas, rng)
    return canvas.astype(np.uint8), head


def close(rng):
    """근경: 탑 꼭대기(원래 크기)의 돌뱀 머리를 가운데 위에 둔다. 돌려주는 값: 그림, 눈 빛 그림, 머리 가운데(점)."""
    canvas = sky(CLOSE_H, 900, rng)
    for x, y, w, h in ((30, 260, 220, 70), (540, 200, 200, 64), (420, 40, 160, 50)):
        paste(canvas, day_cloud(rng, w, h), x, y)
    tower = castle.serpent_tower(np.random.default_rng(9))[0]
    left, top = CX - tower.shape[1] // 2, -30
    paste(canvas, tower, left, top)
    eyes = [(left + x, top + y) for x, y in eye_centers(tower)]
    glow = np.zeros((CLOSE_H, W, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:CLOSE_H, 0:W]
    strength = np.zeros((CLOSE_H, W))
    for x, y in eyes:
        strength = np.maximum(strength, np.exp(-np.hypot(xx - x, (yy - y) * 1.3) / 24))
    stepped = np.floor(strength * 5 + (pg.bayer(CLOSE_H, W) - 0.5) * 0.8) / 5
    glow[..., :3] = np.where((stepped > 0.6)[..., None], np.array([255, 210, 140]), EYE_GLOW)
    glow[..., 3] = (np.clip(stepped, 0, 1) * 230).astype(np.uint8)
    head = (np.mean([e[0] for e in eyes]), np.mean([e[1] for e in eyes]))
    return canvas.astype(np.uint8), glow, head


def party(project):
    """주인공과 랑의 뒷모습(웹게임 인물 그림, 위를 보는 첫 장)을 나란히 놓아 도트 그대로 네 배로 키운다. 주인공 발밑이 아래 끝이다."""
    root = project / "Assets/MiniPandoras/Art/WebGame/Characters"
    hero = Image.open(root / "hero/up_0.png").convert("RGBA")
    rang = Image.open(root / "rang/up_0.png").convert("RGBA")
    hero = hero.crop(hero.getbbox())
    rang = rang.crop(rang.getbbox())
    width = hero.width + rang.width - 6
    image = Image.new("RGBA", (width, hero.height))
    image.alpha_composite(rang, (hero.width - 6, hero.height - rang.height - 2))
    image.alpha_composite(hero, (0, 0))
    return image.resize((image.width * 4, image.height * 4), Image.NEAREST)


def fallen_map(rng):
    """지도 근경: 풀밭에 비스듬히 떨어진 낡은 지도. 돌려주는 값: 그림, 지도 둘레에 차오르는 빛."""
    yy, xx = np.mgrid[0:CLOSE_H, 0:W]
    dither = pg.bayer(CLOSE_H, W) - 0.5
    grass = pg.fractal_noise(CLOSE_H, W, rng)
    tone = np.clip((grass * 5.5 + dither * 1.1 + 0.6).astype(int), 0, len(pg.GRASS) - 1)
    canvas = pg.GRASS[tone].astype(float)
    blades = rng.random((CLOSE_H, W)) > 0.986
    for lift in range(5):
        canvas[np.roll(blades, -lift, axis=0)] = pg.GRASS[min(5, 3 + lift // 2)]

    # 지도의 자리: 가운데를 돌린 좌표 (u, v)로 그린다.
    turn = np.radians(-6)
    du, dv = xx - MAP_CENTER[0], yy - MAP_CENTER[1]
    u = du * np.cos(turn) - dv * np.sin(turn)
    v = du * np.sin(turn) + dv * np.cos(turn)
    edge = pg.fractal_noise(CLOSE_H, W, rng) * 7
    sheet = (np.abs(u) < MAP_HALF[0] + edge - 3) & (np.abs(v) < MAP_HALF[1] + edge - 3)
    shadow = np.roll(np.roll(sheet, 10, axis=0), 8, axis=1) & ~sheet
    canvas[shadow] = canvas[shadow] * 0.45

    panel = np.clip(((u + MAP_HALF[0]) / (MAP_HALF[0] * 2) * 3).astype(int), 0, 2)
    paper = pg.fractal_noise(CLOSE_H, W, rng)
    shade = np.clip((np.array([1.4, 2.3, 1.7])[panel] + paper * 1.6 + dither * 0.8).astype(int), 0, len(PARCHMENT) - 1)
    canvas[sheet] = PARCHMENT[shade][sheet]
    folds = (np.abs(np.abs(u) - MAP_HALF[0] / 3) < 1.6) & sheet
    canvas[folds] = PARCHMENT[0]

    # 지도 안: 뭍의 윤곽, 점선 길, 붉은 표시.
    land = (pg.fractal_noise(CLOSE_H, W, rng) > 0.5) & (np.abs(u) < MAP_HALF[0] - 26) & (np.abs(v) < MAP_HALF[1] - 26)
    canvas[land] = canvas[land] * 0.88
    outline = land & ~(np.roll(land, 3, axis=0) & np.roll(land, -3, axis=0) & np.roll(land, 3, axis=1) & np.roll(land, -3, axis=1))
    canvas[outline] = PARCHMENT[0]
    route = [(-130 + 250 * t, 60 - 110 * t + 26 * np.sin(t * 6)) for t in np.linspace(0, 1, 15)]
    for x, y in route[:-1]:
        canvas[np.hypot(u - x, v - y) < 4.5] = RED_MARK
    x, y = route[-1]
    cross = ((np.abs((u - x) - (v - y)) < 5) | (np.abs((u - x) + (v - y)) < 5)) & (np.abs(u - x) < 20) & (np.abs(v - y) < 20)
    canvas[cross] = RED_MARK

    glow = np.zeros((CLOSE_H, W, 4), dtype=np.uint8)
    reach = np.maximum(np.abs(u) - MAP_HALF[0], np.abs(v) - MAP_HALF[1])
    strength = np.exp(-np.clip(reach, 0, None) / 36) * (reach > -6)
    stepped = np.floor(strength * 5 + dither * 0.8) / 5
    glow[..., :3] = np.array([255, 226, 150])
    glow[..., 3] = (np.clip(stepped, 0, 1) * 170).astype(np.uint8)
    return canvas.astype(np.uint8), glow


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/castle"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(41)
    wide_image, wide_head = wide(rng)
    Image.fromarray(pg.upscale(wide_image), "RGB").save(out / "wide.png")
    close_image, glow, close_head = close(rng)
    Image.fromarray(pg.upscale(close_image), "RGB").save(out / "head.png")
    Image.fromarray(pg.upscale(glow), "RGBA").save(out / "head_glow.png")
    party(project).save(out / "party.png")
    map_image, map_glow = fallen_map(np.random.default_rng(43))
    Image.fromarray(pg.upscale(map_image), "RGB").save(out / "fallen_map.png")
    Image.fromarray(pg.upscale(map_glow), "RGBA").save(out / "fallen_map_glow.png")
    print("원경 돌뱀 머리(가상 px):", tuple(round(v / 2, 1) for v in wide_head))
    print("근경 돌뱀 머리(가상 px):", tuple(round(v / 2, 1) for v in close_head))
    preview = Image.fromarray(pg.upscale(wide_image), "RGB").convert("RGBA")
    figure = party(project)
    height = PARTY_HEIGHT * 4
    figure = figure.resize((int(figure.width * height / figure.height), height), Image.NEAREST)
    preview.alpha_composite(figure, (PARTY_FOOT[0] * 4 - figure.width // 2, PARTY_FOOT[1] * 4 - figure.height))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/cutin_castle_preview.png")
    Image.fromarray(pg.upscale(close_image), "RGB").save(project / "Docs/ArtUpgrade/PixelGen/cutin_castle_head_preview.png")
    Image.fromarray(pg.upscale(map_image), "RGB").save(project / "Docs/ArtUpgrade/PixelGen/cutin_fallen_map_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
