"""뉴웨이브 호(ship): 6장 디엘의 기억과 9장 라르고의 기억 속 뉴웨이브 길드의 배를 새로 짠다(다음 장의 맵과 그래픽).

웹게임 배는 네모난 갑판에 방을 얹은 모양이라, 바다 위를 동쪽으로 나아가는 두 돛대 배로 다시 그렸다. 46×22칸.
  - 바다: 물결 줄무늬와 반짝임, 배 둘레 물거품, 이물에서 갈라지는 물결, 고물 뒤로 남는 물보라.
  - 배: 둥근 고물과 굽은 이물, 북쪽 뱃전은 안쪽 면, 남쪽은 바깥 뱃전(난간·띠 장식·현창·청록 물막이 띠, 고물 쪽에 「뉴웨이브 호」).
    이물 끝에 톱니 장식과 앞으로 뻗은 이물대. 갑판은 배 길이 방향으로 깐 판자다.
  - 고물 북쪽 선실 두 칸(room_interior: 위에서 비스듬히 본 판자 방): 서쪽 침실(디엘·에루스 침대, 둥근 현창),
    동쪽 단장실(단장 책상, 길드 양탄자와 깃발, 지도, 책장). 남쪽 벽에 문이 하나씩 있다.
  - 돛대 둘(큰 돛대, 앞돛대): 접어 묶은 돛을 얹은 붐, 망대, 꼭대기의 톱니 깃발, 등불. 앞돛대에서 이물대로 버팀줄.
  - 고물 키, 북쪽 구명보트, 화물 해치, 통·상자·그물 더미, 밧줄 사리, 이물 쪽 권양기.
동쪽 바다는 9장에서 에라드 군함이 다가와 붙는 자리다(warship, 막힌 칸).
모습 둘: 바다를 나아가는 배(sailing, 기본)와 9장에서 청룡이 휘몰아쳐 두 동강 난 배(broken: 앞돛대 앞 갑판을
가로지르는 갈라진 틈으로 바다가 보이고, 부러진 판자 끝·가시, 끊어진 뱃전, 흩어지고 물에 뜬 판자 조각. 틈은 막힌 칸이다).

지점은 웹게임 배와 같은 이름이다(자리는 새 배에 맞게 옮겼다).

사용: python Tools/PixelGen/ship.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np

import guild_furniture as gf
import interior
import objects
import pixelgen as pg
from houses import BRASS, IRON, PAPER, WOOD, Canvas, gear, outline
from layout import LOGICAL, MapLayout
from room_interior import CAP as CAP_TONES, FACE, RoomInterior, face_decal, floor_decal
from station import text_mask

T = pg.TILE
# 동쪽 바다는 9장에서 뉴웨이브 호만 한 에라드 군함이 들어오는 자리라 넓다.
WIDTH, HEIGHT = 82, 22
MID = 11                    # 배 가운데 줄(칸 경계)
HALF = 8                    # 갑판 반폭(칸): 북쪽 끝 MID - HALF, 남쪽 끝 MID + HALF
STERN, BOW_START, BOW_TIP = 1.6, 26.0, 39.0
CORNER = 1.3                # 고물 모서리 둥글기(칸)
FACE_H = 14                 # 북쪽 뱃전 안쪽 면 높이(점)
CAP = 5                     # 뱃전 윗면 두께(점)
SIDE = 50                   # 남쪽 바깥 뱃전 높이(점)
CABINS = {"bunk": (3, 5, 10, 10), "captain": (11, 5, 19, 10)}   # 선실 바닥 칸(끝 칸은 들지 않는다)
CABIN_BOX = (2, 3, 20, 11)  # 벽까지 넣은 선실 칸
DOORS = [(6, 10), (15, 10)]
PORTHOLES = [(4.5, 5), (8.0, 5), (12.0, 5)]
MAIN_MAST, FORE_MAST = (21, 13), (33, 11)

SEA = pg.hexes("#0c2238", "#112e48", "#163a58", "#1d4868", "#255a7c", "#3a7896", "#6aa2ba", "#b8dce6")
FOAM = pg.hexes("#9cc8d6", "#d4ecf0", "#f4fbfc")
DECK = pg.hexes("#3a2616", "#5c3e24", "#7a5634", "#8e6840", "#a27a4c", "#b88e5c", "#caa26e")
HULL = pg.hexes("#1a100a", "#2c1c12", "#40291a", "#563824", "#6c4930")
PAINT = pg.hexes("#0e3434", "#16504c", "#22706a", "#3a948a")
CREAM = pg.hexes("#a89a78", "#d0c4a0", "#ece2c2")
CANVAS = pg.hexes("#7a7058", "#a29778", "#c4b994", "#ddd3b0", "#efe8cc")
ROPE = pg.hexes("#4a3820", "#6e5630", "#94784a", "#b89a64")


# ── 배 모양 ─────────────────────────────────────────────

def half_height(x):
    """x(점)에서 갑판의 반폭(점). 이물은 끝으로 갈수록 뾰족하게 좁아지고 고물 모서리는 둥글다. 배 밖이면 음수."""
    x = np.asarray(x, dtype=float)
    half = np.full(x.shape, HALF * T, dtype=float)
    bow = x > BOW_START * T
    t = np.clip((x[bow] - BOW_START * T) / ((BOW_TIP - BOW_START) * T), 0, 1)
    half[bow] = HALF * T * np.clip(1 - t ** 1.7, 0, 1) ** 0.9
    radius = CORNER * T
    corner = x < STERN * T + radius
    inset = radius - np.sqrt(np.clip(radius ** 2 - (STERN * T + radius - x[corner]) ** 2, 0, None))
    half[corner] -= inset
    half[(x < STERN * T) | (x > BOW_TIP * T)] = -1
    return half


def deck_edges(width):
    """열마다 갑판의 북쪽·남쪽 끝 y(점)."""
    half = half_height(np.arange(width) + 0.5)
    return MID * T - half, MID * T + half, half >= 0


def walkable_cells():
    """걸을 수 있는 칸: 칸 전체가 갑판 위이고, 북쪽 뱃전 안쪽 면에 걸치지 않는 칸."""
    cells = np.zeros((HEIGHT, WIDTH), dtype=bool)
    for x in range(WIDTH):
        xs = np.arange(x * T, (x + 1) * T) + 0.5
        half = half_height(xs).min()
        if half <= 0:
            continue
        top, bottom = MID * T - half + FACE_H, MID * T + half
        for y in range(HEIGHT):
            cells[y, x] = y * T >= top and (y + 1) * T <= bottom
    return cells


# ── 바다 ───────────────────────────────────────────────

def dilate(mask, steps):
    for _ in range(steps):
        mask = mask | pg.neighbours(mask)
    return mask


def sea(rng, hull):
    """동쪽으로 나아가는 배 둘레의 바다(RGB 점): 물결 줄무늬·반짝임, 배 둘레 물거품, 이물 물결, 고물 뒤 물보라."""
    h, w = HEIGHT * T, WIDTH * T
    yy, xx = np.mgrid[0:h, 0:w]
    swell = pg.fractal_noise(h, w, rng, ((80, 0.6), (28, 0.3), (9, 0.1)))
    bend = pg.noise(h, w, 46, rng) * 9
    crest = np.sin((yy + bend) / 15 * 2 * np.pi + xx / 70)
    broken = pg.noise(h, w, 18, rng) > 0.45
    light = 0.4 + 0.3 * swell + 0.06 * crest
    light = np.where((crest > 0.93) & broken, light + 0.16, light)
    light = np.where((crest < -0.95) & broken, light - 0.1, light)
    image = pg.quantize(np.clip(light, 0, 1), SEA[1:7], 0.12).astype(np.uint8)
    sparkle = (rng.random((h, w)) < 0.0016) & (crest > 0.6)
    image[sparkle] = SEA[7]

    froth = pg.noise(h, w, 6, rng)
    ring = dilate(hull, 7) & ~hull
    image[ring & (froth > 0.45)] = FOAM[0]
    image[dilate(hull, 3) & ~hull & (froth > 0.35)] = FOAM[1]
    # 이물에서 양옆 뒤로 갈라지는 물결
    tip_x, mid_y = BOW_TIP * T, MID * T
    behind = tip_x - xx
    for side in (-1, 1):
        edge = mid_y + side * (behind * 0.42 + 4)
        wave = (behind > 0) & (behind < 15 * T) & (np.abs(yy - edge) < 2 + behind / 160) & ~hull
        image[wave & (froth > 0.4 + behind / (30 * T))] = FOAM[1]
    # 고물 뒤로 남는 물보라
    stern = STERN * T - xx
    streak = np.repeat(pg.noise(h, w // 8 + 2, 3, rng), 8, axis=1)[:, :w]
    wake = (stern > 0) & (np.abs(yy - mid_y) < 2.6 * T + stern * 0.3) & (streak > 0.55 + stern / (6 * T))
    image[wake & ~hull] = FOAM[0]
    image[wake & ~hull & (streak > 0.68 + stern / (6 * T))] = FOAM[2]
    return image


# ── 뱃전과 갑판 ────────────────────────────────────────

def hull_and_deck(c, rng, name="뉴웨이브 호"):
    """뱃전 윗면, 북쪽 안쪽 면, 남쪽 바깥 뱃전, 갑판 판자, 이물대와 이물 장식을 그린다."""
    w, h = c.size
    north, south, inside = deck_edges(w)
    xx, yy = c.xx, c.yy
    deck = inside[None, :] & (yy >= north[None, :]) & (yy < south[None, :])
    cap_ring = dilate(deck, CAP) & ~deck

    # 남쪽 바깥 뱃전: 남쪽 끝 아래 SIDE점. 위에서부터 크림 띠 → 짙은 뱃전 판자(현창) → 굵은 띠목 → 청록 물막이 띠.
    v = yy - south[None, :]
    side = inside[None, :] & (v >= CAP) & (v < CAP + SIDE)
    strake = (v - CAP) // 7
    c.paint(side, HULL[2])
    c.paint(side & (strake % 2 == 1), HULL[3])
    c.paint(side & ((v - CAP) % 7 == 0), HULL[0])
    c.paint(side & (v >= CAP) & (v < CAP + 5), CREAM[1])
    c.paint(side & (v == CAP + 5), HULL[0])
    c.paint(side & (v >= CAP + 22) & (v < CAP + 27), HULL[4])
    c.paint(side & (v == CAP + 22), CREAM[0])
    c.paint(side & (v >= CAP + SIDE - 9), PAINT[1])
    c.paint(side & (v >= CAP + SIDE - 9) & (v < CAP + SIDE - 7), PAINT[3])
    c.paint(side & (v >= CAP + SIDE - 2), PAINT[0])
    for k, px in enumerate(range(int(5 * T), int(36 * T), 3 * T)):
        py = south[px] + CAP + 13
        c.paint(c.ellipse(px, py, 5, 4.5), BRASS[3])
        c.paint(c.ellipse(px, py, 3.5, 3), pg.hexes("#24384a")[0])
        c.paint(c.ellipse(px - 1, py - 1, 1.2, 1), pg.hexes("#8ab4c8")[0])
    if name:
        letters = text_mask(name, 12, 80)
        nh, nw = letters.shape
        left, top = int(4.4 * T), int(south[int(4.4 * T)] + CAP + 30)
        region = c.image[top:top + nh, left:left + nw]
        region[letters, :3] = CREAM[2]
        region[letters, 3] = 255

    # 뱃전 윗면: 짙은 테 안의 밝은 나무, 일정한 간격으로 버팀 기둥 머리
    c.paint(cap_ring, WOOD[4])
    c.paint(cap_ring & ~dilate(deck, CAP - 1), WOOD[1])
    c.paint(cap_ring & (xx % 32 < 3), WOOD[5])

    # 갑판 판자: 배 길이 방향으로 8점 줄, 판자마다 길이·밝기가 조금씩 다르고 이음매에 못, 뱃밥을 채운 짙은 틈.
    level = np.zeros((h, w), dtype=int)
    for row in range(h // 8):
        y0 = row * 8
        x = int(rng.integers(-90, 0))
        while x < w:
            length = int(rng.integers(70, 150))
            level[y0:y0 + 8, max(0, x):x + length] = 4 + int(rng.choice([-1, 0, 0, 0, 0, 1]))
            level[y0 + 3:y0 + 5, max(0, x + length - 3):max(0, x + length - 1)] = 2
            level[y0:y0 + 8, max(0, x + length - 1):x + length] = 1
            x += length
        level[y0, :] = 1
        level[y0 + 1, :] = np.minimum(level[y0 + 1, :] + 1, 6)
    grain = ((xx * 3 + (yy // 8) * 17) % 23 == 0)
    level = np.where(grain, level - 1, level)
    stain = (pg.noise(h, w, 30, rng) > 0.8) & (pg.bayer(h, w) > 0.5)
    level = np.where(stain & (level > 2), level - 1, level)
    near_face = deck & (yy < north[None, :] + FACE_H + 6)
    near_south = deck & (yy > south[None, :] - 5)
    level = np.where(near_face | near_south, level - 1, level)
    c.paint(deck, DECK[np.clip(level, 0, 6)])

    # 북쪽 뱃전 안쪽 면: 세로 판자와 버팀 기둥, 위에 난간 띠, 아래 굽도리 그늘
    u = yy - north[None, :]
    face = deck & (u < FACE_H)
    c.paint(face, HULL[3])
    c.paint(face & (xx % 6 == 0), HULL[2])
    c.paint(face & (xx % 32 < 4), WOOD[3])
    c.paint(face & (xx % 32 == 0), WOOD[5])
    c.paint(face & (u < 3), WOOD[4])
    c.paint(deck & (u >= FACE_H) & (u < FACE_H + 3), DECK[1])

    # 이물대: 이물 끝에서 동쪽으로 뻗은 둥근 나무 기둥과 그 끝 밧줄 고리. 이물 끝엔 톱니 장식.
    tip = BOW_TIP * T
    spar = c.polygon([(tip - 10, MID * T - 5), (tip + 4.2 * T, MID * T - 0.9 * T - 3), (tip + 4.2 * T, MID * T - 0.9 * T + 2), (tip - 10, MID * T + 5)])
    c.paint(spar, WOOD[3])
    c.paint(spar & (yy < MID * T - 0.45 * (xx - tip + 10) / (4.2 * T + 10) * T - 1), WOOD[5])
    for k in range(1, 5):
        bx = tip + k * T * 0.95
        by = MID * T - 0.9 * T * (bx - tip + 10) / (4.2 * T + 10)
        c.paint(c.rect(bx, by - 4, bx + 2, by + 4) & spar, IRON[2])
    gear(c, tip - 4, MID * T, 11, 9, BRASS, open_spokes=False)
    c.paint(c.ellipse(tip - 4, MID * T, 4, 4), PAINT[2])
    return deck, side | cap_ring | deck | spar


def coil(rng):
    """갑판에 사려 둔 밧줄(위에서 본 둥근 사리)."""
    c = Canvas(22, 16)
    for r in range(9, 1, -2):
        ring = c.ellipse(11, 8, r, r * 0.68) & ~c.ellipse(11, 8, r - 1.2, (r - 1.2) * 0.68)
        c.paint(ring, ROPE[1 + (r // 2) % 3])
    c.paint(c.ellipse(11, 8, 2, 1.4), ROPE[0])
    c.line(18, 7, 21, 12, ROPE[2])
    return outline(c.image)


def hatch(columns, rows):
    """화물 해치: 굵은 나무 테 안의 쇠 격자 뚜껑, 틈으로 어두운 짐칸이 비친다."""
    w, h = int(columns * T), int(rows * T)
    c = Canvas(w, h)
    c.paint(c.rect(0, 0, w, h), WOOD[2])
    c.paint(c.rect(0, 0, w, 3), WOOD[4])
    c.paint(c.rect(0, h - 3, w, h), WOOD[1])
    inner = c.rect(5, 5, w - 5, h - 5)
    c.paint(inner, pg.hexes("#100a08")[0])
    bars = inner & (((c.xx - 5) % 7 < 3) | ((c.yy - 5) % 7 < 3))
    c.paint(bars, WOOD[3])
    c.paint(bars & (((c.xx - 5) % 7 == 0) | ((c.yy - 5) % 7 == 0)), WOOD[5])
    return outline(c.image)


def net_pile(rng):
    """그물 더미: 엉킨 그물코와 둥근 부표 몇 개."""
    c = Canvas(40, 18)
    pile = c.ellipse(20, 11, 19, 7) | c.ellipse(13, 8, 9, 5)
    c.paint(pile, ROPE[1])
    c.paint(pile & (((c.xx + c.yy) % 4 == 0) | ((c.xx - c.yy) % 4 == 0)), ROPE[3])
    c.paint(pile & (c.yy > 14), ROPE[0])
    for x, y in ((8, 9), (27, 7), (33, 12)):
        c.paint(c.ellipse(x, y, 3, 2.5), pg.hexes("#c8463a")[0])
        c.paint(c.ellipse(x - 1, y - 1, 1, 1), CREAM[2])
    return outline(c.image), (20, 17)


def anchor(rng):
    """갑판에 눕혀 둔 닻(위에서 본 모습): 쇠 자루와 갈고리 두 팔, 고리에 이어진 닻줄 사슬."""
    c = Canvas(84, 40)
    c.line(20, 20, 62, 20, IRON[2], 4)
    c.line(20, 19, 62, 19, IRON[3], 1)
    c.paint(c.rect(56, 13, 60, 28), IRON[1])
    arm = (c.ellipse(20, 20, 14, 16) & ~c.ellipse(24, 20, 13, 13)) & (c.xx < 22)
    c.paint(arm, IRON[2])
    c.paint(arm & (c.xx < 9), IRON[3])
    for y in (5, 35):
        c.paint(c.polygon([(9, y - 4), (16, y), (9, y + 4), (5, y)]), IRON[2])
    c.paint(c.ellipse(66, 20, 4, 4) & ~c.ellipse(66, 20, 2, 2), IRON[2])
    for k in range(5):
        x = 72 + k * 3
        c.paint(c.ellipse(x, 20 + (k % 2) * 2 - 1, 2, 1.4) & ~c.ellipse(x, 20 + (k % 2) * 2 - 1, 0.8, 0.5), IRON[1 + k % 2])
    return outline(c.image)


def cargo(rng):
    """천을 덮어 밧줄로 묶은 짐더미: 상자 몇 개 위에 씌운 범포, 그 위를 가로지르는 밧줄."""
    w, h = 78, 52
    c = Canvas(w, h)
    lump = c.rect(4, 18, w - 4, h - 3) | c.ellipse(26, 18, 22, 10) | c.ellipse(56, 22, 18, 9)
    c.paint(lump, CANVAS[1])
    c.paint(lump & (c.yy < 20), CANVAS[3])
    c.paint(lump & (c.yy > h - 10), CANVAS[0])
    c.paint(lump & ((c.xx * 2 + c.yy) % 17 == 0), CANVAS[0])
    for x in (20, 50):
        c.paint(lump & (np.abs(c.xx - x - (c.yy - 20) * 0.1) < 1.2), ROPE[1])
    c.paint(lump & (np.abs(c.yy - 34) < 1), ROPE[1])
    return outline(c.image), (w // 2, h - 2)


def fish_basket(rng):
    """생선 바구니: 엮은 대바구니에 담긴 은빛 생선."""
    c = Canvas(26, 20)
    body = c.polygon([(2, 7), (24, 7), (21, 19), (5, 19)])
    c.paint(body, WOOD[3])
    c.paint(body & ((c.xx + c.yy) % 3 == 0), WOOD[2])
    c.paint(c.rect(2, 6, 24, 8), WOOD[4])
    for x, y in ((7, 5), (12, 4), (17, 5), (10, 6), (19, 6)):
        c.paint(c.ellipse(x, y, 3.5, 1.4), pg.hexes("#b8c8d0")[0])
        c.paint(c.ellipse(x - 1, y - 0.5, 1.5, 0.6), pg.hexes("#eef4f6")[0])
    return outline(c.image), (13, 19)


def bucket(rng):
    c = Canvas(12, 14)
    body = c.polygon([(1, 3), (11, 3), (9.5, 13), (2.5, 13)])
    c.paint(body, WOOD[3])
    c.paint(body & (c.xx < 4), WOOD[4])
    c.paint(body & ((c.yy == 6) | (c.yy == 10)), IRON[2])
    c.paint(c.ellipse(6, 3, 5, 1.6), pg.hexes("#2a5470")[0])
    c.line(1, 3, 6, 0, IRON[2])
    c.line(6, 0, 11, 3, IRON[2])
    return outline(c.image), (6, 13)


# ── 세운 것 ─────────────────────────────────────────────

def mast(rng, height, boom, nest, flag, lantern_at, stay=None):
    """
    돛대(남쪽에서 본 모습): 위로 가늘어지는 나무 기둥과 쇠테, 아래 둘레의 밧줄 걸이(핀과 사리), 고물 쪽으로 뻗은 붐에 접어 묶은 돛,
    붐 끝에서 꼭대기로 가는 밧줄, nest면 망대, flag면 꼭대기에 청록 톱니 깃발, 등불 하나. stay=(dx, dy)면 꼭대기에서 그 자리(이물대 끝)로 버팀줄.
    돌려주는 값: (그림, 기준점=기둥 밑동 가운데), 등불 자리(밑동에서 위로 점).
    """
    right = (stay[0] + 6) if stay else 30
    left = boom + 20
    w, hgt = left + right, height + 40
    c = Canvas(w, hgt)
    base_x, base_y = left, hgt - 4
    top_y = base_y - height

    # 꼭대기에서 붐 끝으로 가는 밧줄, 버팀줄(가장 뒤에 그린다)
    c.line(base_x, top_y + 8, base_x - boom + 4, base_y - 44, ROPE[1])
    if stay:
        c.line(base_x + 2, top_y + 14, base_x + stay[0], base_y + stay[1], ROPE[1])
        for k in range(1, 4):
            t = k / 4
            sx, sy = base_x + 2 + stay[0] * t, top_y + 14 + (base_y + stay[1] - top_y - 14) * t
            c.paint(c.ellipse(sx, sy, 3, 4) & (c.yy > sy - 1), CANVAS[2])
    for side in (-1, 1):
        c.line(base_x + side * 3, top_y + 30, base_x + side * 26, base_y - 6, ROPE[0])

    # 기둥
    for y in range(top_y, base_y + 1):
        t = (y - top_y) / height
        half = 2.5 + 2.5 * t
        x0, x1 = base_x - half, base_x + half
        row = c.rect(x0, y, x1 + 1, y + 1)
        c.paint(row, WOOD[3])
        c.paint(row & (c.xx < x0 + 2), WOOD[5])
        c.paint(row & (c.xx > x1 - 1.5), WOOD[1])
    for y in range(top_y + 24, base_y - 10, 38):
        c.paint(c.rect(base_x - 5, y, base_x + 6, y + 2), IRON[2])

    # 붐과 접어 묶은 돛(붐 위에 둥글게 뭉친 천, 일정한 간격으로 밧줄)
    boom_y = base_y - 44
    c.paint(c.rect(base_x - boom, boom_y, base_x + 4, boom_y + 4), WOOD[3])
    c.paint(c.rect(base_x - boom, boom_y, base_x + 4, boom_y + 1), WOOD[5])
    lump = np.zeros(c.image.shape[:2], dtype=bool)
    for k in range(0, boom - 8, 6):
        r = 5 + 2 * np.sin(k * 0.21 + rng.random())
        lump |= c.ellipse(base_x - 6 - k, boom_y - r * 0.6, 5, r)
    c.paint(lump, CANVAS[2])
    c.paint(lump & (c.yy < boom_y - 6), CANVAS[3])
    c.paint(lump & ((c.xx - base_x) % 7 == 0), CANVAS[1])
    for k in range(10, boom - 8, 22):
        c.paint(lump & (np.abs(c.xx - (base_x - k)) < 1), ROPE[1])
    # 가프(돛 윗활대)는 내려서 묶은 돛 위에 얹혀 있다
    c.line(base_x - 2, boom_y - 13, base_x - boom + 30, boom_y - 10, WOOD[2], 2)

    if nest:
        ny = top_y + int(height * 0.3)
        basket = c.rect(base_x - 13, ny, base_x + 14, ny + 11)
        c.paint(basket, WOOD[2])
        c.paint(basket & ((c.xx - base_x) % 4 == 0), WOOD[1])
        c.paint(c.ellipse(base_x, ny, 14, 4), WOOD[4])
        c.paint(c.ellipse(base_x, ny, 11, 2.5), WOOD[1])
        c.paint(c.rect(base_x - 13, ny + 9, base_x + 14, ny + 11), IRON[2])
    if flag:
        pole = top_y - 4
        cloth = c.polygon([(base_x + 1, pole - 22), (base_x + 30, pole - 18), (base_x + 22, pole - 11), (base_x + 30, pole - 4), (base_x + 1, pole - 2)])
        c.paint(cloth, PAINT[2])
        c.paint(cloth & (c.yy < pole - 18), PAINT[3])
        c.paint(cloth & (c.yy > pole - 6), PAINT[1])
        gear(c, base_x + 11, pole - 12, 6, 7, BRASS, open_spokes=False)
        c.paint(c.rect(base_x - 1, pole - 24, base_x + 2, top_y + 2), WOOD[2])
        c.paint(c.ellipse(base_x, pole - 25, 2.5, 2.5), BRASS[3])

    # 밑동: 밧줄 걸이 테(핀이 꽂힌 짧은 난간)와 걸어 둔 밧줄 사리
    rail = c.rect(base_x - 15, base_y - 9, base_x + 16, base_y - 6)
    c.paint(rail, WOOD[4])
    for px in range(base_x - 13, base_x + 15, 5):
        c.paint(c.rect(px, base_y - 13, px + 2, base_y - 9), WOOD[5])
    for px in (base_x - 15, base_x + 14):
        c.paint(c.rect(px, base_y - 9, px + 2, base_y + 1), WOOD[2])
    for sx in (base_x - 10, base_x + 9):
        c.paint(c.ellipse(sx, base_y - 3, 4, 3.5) & ~c.ellipse(sx, base_y - 3, 2, 1.8), ROPE[2])
    lantern = (base_x + 7, base_y - 66)
    c.paint(c.rect(lantern[0] - 3, lantern[1] - 5, lantern[0] + 4, lantern[1] + 5), BRASS[2])
    c.paint(c.rect(lantern[0] - 2, lantern[1] - 3, lantern[0] + 3, lantern[1] + 4), pg.hexes("#ffd890")[0])
    c.line(base_x + 3, lantern[1] - 7, lantern[0], lantern[1] - 5, IRON[2])
    return (outline(c.image), (base_x, base_y)), base_y - lantern[1]


def helm(rng):
    """키(남쪽에서 본 모습): 나무 받침대 위의 살 여덟 개 둥근 키 바퀴와 손잡이, 놋쇠 굴대."""
    c = Canvas(40, 54)
    cx, cy = 20, 20
    c.paint(c.rect(15, 26, 25, 52), WOOD[2])
    c.paint(c.rect(15, 26, 17, 52), WOOD[4])
    c.paint(c.rect(11, 48, 29, 53), WOOD[1])
    for k in range(8):
        a = k * np.pi / 4
        ex, ey = cx + np.cos(a) * 18, cy + np.sin(a) * 18
        c.line(cx, cy, ex, ey, WOOD[3], 2)
        c.paint(c.ellipse(ex, ey, 2.2, 2.2), WOOD[4])
    ring = c.ellipse(cx, cy, 13, 13) & ~c.ellipse(cx, cy, 10, 10)
    c.paint(ring, WOOD[3])
    c.paint(ring & (c.yy < cy - 6), WOOD[5])
    c.paint(c.ellipse(cx, cy, 3.5, 3.5), BRASS[3])
    c.paint(c.ellipse(cx - 1, cy - 1, 1.2, 1.2), BRASS[4])
    return outline(c.image), (20, 53)


def lifeboat(rng):
    """구명보트: 받침 둘 위에 엎어 두지 않고 바로 놓은 작은 나룻배(뱃전·가로 널·노 두 자루)."""
    w, h = 128, 44
    c = Canvas(w, h)
    for x in (30, 98):
        c.paint(c.rect(x - 6, h - 12, x + 6, h - 2), WOOD[1])
    body = c.ellipse(w / 2, 20, 60, 15) & (c.yy < 30)
    keel = c.ellipse(w / 2, 26, 56, 12) & (c.yy >= 22)
    c.paint(body | keel, PAINT[2])
    c.paint(keel & (c.yy > 30), PAINT[1])
    c.paint((body | keel) & (np.abs(c.yy - 27) < 1), CREAM[1])
    inner = c.ellipse(w / 2, 18, 55, 11)
    c.paint(inner, WOOD[3])
    c.paint(inner & (c.xx % 9 == 0), WOOD[2])
    for x in (40, 64, 88):
        c.paint(c.rect(x - 3, 9, x + 3, 28) & inner, WOOD[5])
    c.line(26, 15, 100, 22, WOOD[4], 2)
    c.line(28, 21, 102, 14, WOOD[4], 2)
    return outline(c.image), (w // 2, h - 2)


def capstan(rng):
    """권양기: 굵은 나무 북에 감긴 닻줄, 위에 꽂은 막대 넷."""
    c = Canvas(34, 34)
    drum = c.rect(9, 12, 25, 31)
    c.paint(drum, WOOD[3])
    c.paint(drum & (c.xx < 12), WOOD[5])
    c.paint(drum & (c.xx > 22), WOOD[1])
    for y in range(15, 29, 3):
        c.paint(c.rect(9, y, 25, y + 2), ROPE[2])
    c.paint(c.ellipse(17, 12, 9, 3.5), WOOD[4])
    for k in range(4):
        a = k * np.pi / 2 + 0.4
        c.line(17, 11, 17 + np.cos(a) * 15, 11 + np.sin(a) * 6, WOOD[2], 2)
    c.paint(c.rect(7, 30, 27, 33), WOOD[1])
    return outline(c.image), (17, 33)


def porthole(c, x, row):
    """선실 벽면의 둥근 현창: 놋쇠 테, 바다빛 유리와 반사."""
    cx, cy = x * T, row * T - FACE + 24
    c.paint(c.ellipse(cx, cy, 9, 9), BRASS[2])
    c.paint(c.ellipse(cx, cy, 9, 9) & (c.yy < cy - 3), BRASS[3])
    c.paint(c.ellipse(cx, cy, 6, 6), pg.hexes("#24506a")[0])
    c.paint(c.ellipse(cx, cy, 6, 6) & (c.xx + c.yy < cx + cy - 3), pg.hexes("#4e86a2")[0])
    c.paint(c.ellipse(cx - 2, cy - 2, 1.6, 1.2), pg.hexes("#cfe8f0")[0])
    for k in range(6):
        a = k * np.pi / 3
        c.paint(c.ellipse(cx + np.cos(a) * 7.5, cy + np.sin(a) * 7.5, 0.9, 0.9), BRASS[4])


def deckhouse_walls(c):
    """
    선실 벽: 실내 그림의 짙은 벽 윗면을 배의 나무빛으로 바꾸고, 갑판에서 보이는 남쪽 바깥벽(크림 띠를 두른 판자벽,
    문 자리는 열린 문간)을 선실 아래 첫 줄 위쪽에 그린다.
    """
    box = c.rect(CABIN_BOX[0] * T, CABIN_BOX[1] * T, CABIN_BOX[2] * T, CABIN_BOX[3] * T)
    swaps = ((CAP_TONES[0], HULL[2]), (CAP_TONES[1], HULL[3]), (CAP_TONES[2], HULL[4]), (CAP_TONES[3], WOOD[4]))
    rgb = c.image[..., :3]
    for old, new in swaps:
        match = box & (rgb == old).all(axis=-1)
        c.paint(match, new)
    x0, x1 = CABIN_BOX[0] * T, CABIN_BOX[2] * T
    top = CABIN_BOX[3] * T
    wall = c.rect(x0, top, x1, top + 12)
    c.paint(wall, HULL[3])
    c.paint(wall & (c.xx % 6 == 0), HULL[2])
    c.paint(wall & (c.yy < top + 3), CREAM[1])
    c.paint(wall & (c.yy == top + 3), HULL[0])
    c.paint(wall & (c.yy >= top + 10), HULL[1])
    for x, _ in DOORS:
        doorway = c.rect(x * T + 4, top, (x + 1) * T - 4, top + 12)
        c.paint(doorway, pg.hexes("#140c08")[0])
        for px in (x * T + 2, (x + 1) * T - 4):
            c.paint(c.rect(px, top, px + 2, top + 12), WOOD[4])


# ── 맵 ─────────────────────────────────────────────────

# ── 9장: 두 동강 난 배 ─────────────────────────────────

BREAK_X = 35.6              # 갈라진 틈 가운데(칸): 앞돛대와 이물 사이. 이물 쪽이 떨어져 나간다
BREAK_BOX = (33.6, 5.4, 37.6, 17.8)   # 틈 그림이 덮는 칸(왼쪽, 위, 오른쪽, 아래)
RAW = pg.hexes("#6a4a2c", "#9a7046", "#c8a070", "#e4c48e")


def deck_break(rng):
    """
    청룡이 휘몰아쳐 갈라진 갑판(점, RGBA): 북쪽 뱃전부터 남쪽 바깥 뱃전까지 비스듬히 지그재그로 갈라지고, 틈으로 어두운 배 속과 바다가 보인다.
    판자 줄(8점)마다 부러진 끝이 들쭉날쭉하고, 가시처럼 튀어나온 나뭇조각과 밝게 드러난 생나무 단면이 있다.
    남쪽 바깥 뱃전은 물 쪽으로 갈수록 틈이 벌어진다. 둘레 갑판과 바다에 판자 조각이 흩어져 있다.
    """
    left, top = int(BREAK_BOX[0] * T), int(BREAK_BOX[1] * T)
    w, h = int((BREAK_BOX[2] - BREAK_BOX[0]) * T), int((BREAK_BOX[3] - BREAK_BOX[1]) * T)
    yy, xx = np.mgrid[0:h, 0:w]
    gx, gy = xx + left + 0.5, yy + top + 0.5
    north, south, inside = deck_edges(WIDTH * T)
    col = np.clip(gx.astype(int), 0, WIDTH * T - 1)
    hull_top, deck_bottom = north[col] - CAP, south[col]
    hull = inside[col] & (gy >= hull_top) & (gy < deck_bottom + CAP + SIDE)
    image = np.zeros((h, w, 4), dtype=np.uint8)

    # 틈의 가운데 줄: 위에서 아래로 비스듬히, 굽이치며 지그재그. 판자 줄마다 양쪽 끝이 따로 들쭉날쭉하다.
    wobble = pg.noise(h, 1, 22, rng)[:, 0]
    centre = BREAK_X * T - left + 10 * np.sin(yy[:, 0] / 37.0) + (wobble - 0.5) * 14 + (yy[:, 0] - h / 2) * 0.06
    width = 5 + pg.noise(h, 1, 30, rng)[:, 0] * 6
    row = (yy[:, 0] + top) // 8
    jag_left = np.array([rng.integers(-4, 5) for _ in range(row.max() + 1)])[row]
    jag_right = np.array([rng.integers(-4, 5) for _ in range(row.max() + 1)])[row]
    below = np.clip((gy[:, 0] - (south[int(BREAK_X * T)] + CAP)) / SIDE, 0, 1)
    open_left = centre - width - jag_left - below * 10
    open_right = centre + width + jag_right + below * 10
    gap = hull & (xx >= open_left[:, None]) & (xx <= open_right[:, None])

    # 틈 속: 가장자리 쪽은 어두운 배 속, 가운데로 바다가 비친다. 물거품이 조금 인다.
    depth = np.minimum(xx - open_left[:, None], open_right[:, None] - xx)
    froth = pg.noise(h, w, 4, rng)
    inner = np.where(depth[..., None] < 3, pg.hexes("#120a06")[0], np.where(froth[..., None] > 0.62, FOAM[0], SEA[2]))
    image[gap, :3] = inner[gap]
    image[gap & (depth >= 3) & (froth < 0.3), :3] = SEA[1]
    image[gap, 3] = 255

    # 부러진 판자 끝: 틈 가장자리 두 점은 밝은 생나무 단면, 그 바깥 한 점은 그늘.
    edge = hull & ~gap & (
        ((xx >= open_left[:, None] - 2) & (xx < open_left[:, None])) | ((xx > open_right[:, None]) & (xx <= open_right[:, None] + 2)))
    image[edge, :3] = RAW[2]
    image[edge & ((yy + top) % 8 == 0), :3] = RAW[1]
    image[edge, 3] = 255
    shade = hull & ~gap & ~edge & (
        ((xx >= open_left[:, None] - 4) & (xx < open_left[:, None] - 2)) | ((xx > open_right[:, None] + 2) & (xx <= open_right[:, None] + 3)))
    image[shade, :3] = RAW[0]
    image[shade, 3] = 255
    # 가시: 판자 줄 몇 군데에서 틈 쪽으로 튀어나온 나뭇조각.
    for r in range(0, h, 8):
        for side in (-1, 1):
            if rng.random() > 0.55:
                continue
            y = r + int(rng.integers(2, 6))
            if y >= h or not hull[y, int(np.clip(centre[y], 0, w - 1))]:
                continue
            start = open_left[y] if side < 0 else open_right[y]
            length = int(rng.integers(3, 8))
            for k in range(length):
                x = int(round(start - side * 1 + side * k))
                yk = y + (k // 3) * (1 if rng.random() < 0.5 else 0)
                if 0 <= x < w and 0 <= yk < h:
                    image[yk, x, :3] = RAW[3] if k < length - 1 else RAW[1]
                    image[yk, x, 3] = 255

    # 흩어진 판자 조각: 갑판 위(짙은 테를 두른 생나무)와 틈 둘레 바다에 뜬 것.
    for k in range(16):
        on_deck = k < 9
        for _ in range(30):
            cx, cy = rng.uniform(4, w - 4), rng.uniform(4, h - 4)
            here = hull[int(cy), int(cx)] and not gap[int(cy), int(cx)]
            if here == on_deck and (on_deck or not hull[max(0, int(cy) - 6):int(cy) + 6, max(0, int(cx) - 8):int(cx) + 8].any()):
                break
        else:
            continue
        angle = rng.uniform(0, np.pi)
        length, thick = rng.uniform(5, 12), rng.uniform(1.5, 2.5)
        u = (xx - cx) * np.cos(angle) + (yy - cy) * np.sin(angle)
        v = -(xx - cx) * np.sin(angle) + (yy - cy) * np.cos(angle)
        piece = (np.abs(u) <= length / 2) & (np.abs(v) <= thick / 2)
        ring = pg.neighbours(piece) & ~piece
        image[ring, :3] = RAW[0] if on_deck else SEA[0]
        image[ring, 3] = 255
        image[piece, :3] = RAW[2] if on_deck else RAW[1]
        image[piece & (v < 0), :3] = RAW[3] if on_deck else RAW[2]
        image[piece, 3] = 255
        if not on_deck:
            splash = (np.abs(u) <= length / 2 + 2) & (np.abs(v) <= thick / 2 + 1.5) & ~piece & ~ring & (froth > 0.45)
            image[splash, :3] = FOAM[1]
            image[splash, 3] = 255
    return image, left, top


def build(night=False):
    """뉴웨이브 호. night면 11장 밤바다 위의 같은 갑판(shipNight: 밤빛, 웹게임 SHIP_DECK를 함께 쓴 맵)이다."""
    if night:
        m = MapLayout("shipNight", "바다 위", WIDTH, HEIGHT, theme="ship", music="sorrow",
                      ambient={"color": "#0a0c22", "alpha": 0.6}, seed=61)
    else:
        m = MapLayout("ship", "뉴웨이브 호", WIDTH, HEIGHT, theme="ship", music="hope", seed=61)
    rng = np.random.default_rng(61)
    m.kinds[:] = "water"
    walk = walkable_cells()
    m.kinds[walk] = "deck"

    room = RoomInterior(WIDTH, HEIGHT, list(CABINS.values()), seed=62)
    room.paint_base()
    for x, row in PORTHOLES:
        porthole(room.c, x, row)
    for x, y in DOORS:
        room.entrance(x, y)
    deckhouse_walls(room.c)

    def paint():
        c = Canvas(WIDTH * T, HEIGHT * T)
        deck, hull = hull_and_deck(c, np.random.default_rng(63))
        ground = sea(np.random.default_rng(64), hull)
        opaque = c.image[..., 3] > 0
        ground[opaque] = c.image[opaque, :3]
        x0, y0, x1, y1 = (v * T for v in CABIN_BOX)
        ground[y0:y1 + 12, x0:x1] = room.ground()[y0:y1 + 12, x0:x1]
        # 선실 남쪽·동쪽 벽이 갑판에 드리운 그늘
        shade = np.zeros(ground.shape[:2], dtype=bool)
        shade[y1 + 12:y1 + 17, x0:x1] = True
        shade[y0 + FACE_H:y1, x1:x1 + 5] = True
        ground[shade & deck] = (ground[shade & deck] * 0.78).astype(np.uint8)
        return ground

    m.paint_ground = paint

    # ── 충돌: 갑판 밖, 선실 벽(문 빼고) ──
    m.block_where(~walk)
    box = np.zeros((HEIGHT, WIDTH), dtype=bool)
    bx0, by0, bx1, by1 = CABIN_BOX
    box[by0:by1, bx0:bx1] = True
    floors = np.zeros_like(box)
    for x0, y0, x1, y1 in CABINS.values():
        floors[y0:y1, x0:x1] = True
    for x, y in DOORS:
        floors[y, x] = True
    m.block_where(box & ~floors)

    # ── 침실: 디엘과 에루스의 침대(누워 있는 장면이 있어 바닥에 깐다), 발치 궤, 옷장, 벽 등 ──
    floor_decal(m, "bed_diel", gf.bed(rng)[0], 3.47, 5.1)
    floor_decal(m, "bed_erus", gf.bed(rng)[0], 6.47, 5.1)
    m.stand("footlocker", interior.footlocker(rng), 4.0, 8.4, block=(0.9, 0.5))
    m.stand("wardrobe", gf.wardrobe(rng), 9.0, 6.0, block=(1.4, 0.7))
    face_decal(m, "sconce_bunk", gf.sconce(rng), 6.2, 5)
    m.light(6.2 * LOGICAL, (5 * T - FACE / 2) / 2, 36, "#ffc070")

    # ── 단장실: 단장 책상(뒤에 에루스가 선다), 길드 양탄자와 깃발, 지도·책장·톱니 시계, 걸상 ──
    floor_decal(m, "rug", gf.rug(5, 3, rng), 12.5, 6.9)
    m.stand("desk", gf.guild_desk(rng), 14.5, 7.0, block=(3, 0.7))
    face_decal(m, "banner_left", gf.guild_banner(rng), 11.6, 5)
    face_decal(m, "wall_map", gf.wall_map(rng), 14.5, 5)
    face_decal(m, "clock", gf.gear_clock(rng), 16.6, 5)
    face_decal(m, "banner_right", gf.guild_banner(rng), 18.3, 5)
    m.stand("bookshelf", interior.bookshelf(rng), 18.0, 9.0, block=(1.6, 0.6))
    m.stand("stool_a", gf.stool(rng), 11.6, 9.2, block=(0.5, 0.3))
    m.light(14.5 * LOGICAL, 6 * LOGICAL, 44, "#ffc070")

    # ── 갑판 ──
    (image, pivot), lantern_up = mast(rng, 236, 170, nest=True, flag=True, lantern_at=66)
    m.stand("mast_main", (image, pivot), MAIN_MAST[0] + 0.5, MAIN_MAST[1] + 1, block=(1, 0.6))
    m.light((MAIN_MAST[0] + 0.5) * LOGICAL + 3, (MAIN_MAST[1] + 1) * LOGICAL - lantern_up / 2, 40, "#ffc070", flicker=True)
    stay = (int((BOW_TIP + 4.2 - FORE_MAST[0] - 0.5) * T), int((MID - 0.9 - FORE_MAST[1] - 1) * T))
    (image, pivot), lantern_up = mast(rng, 196, 120, nest=False, flag=True, lantern_at=66, stay=stay)
    m.stand("mast_fore", (image, pivot), FORE_MAST[0] + 0.5, FORE_MAST[1] + 1, block=(1, 0.6))
    m.light((FORE_MAST[0] + 0.5) * LOGICAL + 3, (FORE_MAST[1] + 1) * LOGICAL - lantern_up / 2, 36, "#ffc070", flicker=True)

    m.stand("helm", helm(rng), 3.5, 15.0, block=(0.9, 0.5))
    m.stand("lifeboat", lifeboat(rng), 26.0, 5.4, block=(4, 1.2))
    floor_decal(m, "hatch", hatch(3, 2), 11, 13)
    floor_decal(m, "hatch_fore", hatch(2, 2), 27, 12)
    for kind, x, y in (("barrel", 7, 17), ("barrel", 8, 17), ("barrel", 7, 16), ("crate", 28, 17), ("crate", 29, 17), ("crate", 28, 16),
                       ("barrel", 19, 4), ("crate", 31, 6)):
        m.thing(kind, x, y)
    m.stand("nets", net_pile(rng), 10.5, 18.0, block=(1.2, 0.5))
    m.stand("bucket", bucket(rng), 17.5, 16.0, block=(0.4, 0.3))
    m.stand("cargo", cargo(rng), 23.0, 7.0, block=(2.2, 0.9))
    m.stand("fish", fish_basket(rng), 11.7, 18.0, block=(0.6, 0.4))
    m.decal("anchor", anchor(rng), 32.4 * T, 15.6 * T)
    for i, (x, y) in enumerate(((13.2, 16.3), (23.6, 14.2), (31.2, 15.4), (5.6, 11.4))):
        m.decal(f"coil_{i}", coil(rng), x * T, y * T)

    # ── 모습: 바다를 나아가는 배(기본)와 9장에서 두 동강 난 배 ──
    # 이물 쪽 권양기와 밧줄 사리는 두 동강 날 때 바다에 빠진다.
    with m.look("sailing"):
        m.stand("capstan", capstan(rng), 35.5, 12.0, block=(0.8, 0.5))
        m.decal("coil_4", coil(rng), 36.0 * T, 9.6 * T)
    with m.look("broken"):
        image, left, top = deck_break(np.random.default_rng(91))
        m.decal("deck_break", image, left, top)
        m.block(int(BREAK_X), 0, int(BREAK_X) + 2, HEIGHT)

    # ── 지점: 웹게임 배와 같은 이름 ──
    m.point("bed", (4, 6))
    m.point("erusBed", (7, 6))
    m.point("meeting", (14, 5), (16, 6), (12, 6))
    m.point("lesson", (16, 7))
    m.point("lessonDiel", (16, 9))
    m.point("cabinDoor", (6, 10))
    m.point("deck", (6, 11))
    m.point("eavesdrop", (15, 11))
    m.point("buddy", (5, 13))
    m.point("hass", (22, 14))
    m.point("mast", (20, 13))
    m.point("largo", (9, 16))
    m.point("night", (13, 16))
    m.point("luwan", (26, 16))
    m.point("diel", (23, 9))
    m.point("hassStand", (25, 9))
    m.point("erus", (27, 9))
    # 에라드 쪽 셋(9장)은 앞돛대의 접은 돛(활대, 칸 y 10~11에 x 29.8~33.6)에 겹치지 않게 그 위아래에 선다.
    m.point("kyle", (30, 8))
    m.point("kate", (32, 8))
    m.point("neo", (30, 12))
    m.point("warship", (42, 10), blocked=True)
    return m


def main(project):
    print(build().publish(project))
    print(build(night=True).publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
