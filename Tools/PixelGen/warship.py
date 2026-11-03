"""9장 에라드 군함: 케이트가 연 길로 뉴웨이브 호 동쪽 바다에 다가와 붙는 배(웹게임 warship 자리, 사용자 요청 2026-11-03: 그림은 생성기로).

웹게임 그림처럼 옆에서 조금 내려다본 범선이고, 뱃머리가 왼쪽(뉴웨이브 호 쪽)을 본다. 우리 뉴웨이브 호의 나무 갑판과 달리
남색 칠을 한 선체에 금빛 띠와 포문 줄, 높은 고물 망루와 불 켜진 창, 날개 펼친 금빛 뱃머리 장식을 단 에라드 군함이다.
돛대 셋(앞·큰·뒷돛대)에 흰 네모돛을 층층이 달고, 큰 돛에는 에라드 문장(금빛 날개와 해)을 그린다. 이물대에서 앞돛대로
삼각돛 둘, 뒷돛대 뒤로 세로돛 하나. 돛대 꼭대기와 고물에 바람에 날리는 흰 깃발, 물에 닿는 곳에는 물거품과 뱃머리 물결.

  Assets/MiniPandoras/Art/PixelGen/Stage/warship.png   기준점은 아래 가운데(물 위에 뜬 줄). StageImage가 그 자리에 세운다
  Docs/ArtUpgrade/PixelGen/warship_preview.png         바다 위에 띄운 모습

점 격자(논리 px 하나 = 2점)에 그려 두 배로 키운다.

사용: python Tools/PixelGen/warship.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

W, H = 384, 232
WL = H - 14                         # 물에 뜬 줄(점). 아래는 물거품과 물에 잠긴 선체

NAVY = pg.hexes("#0c1220", "#151e36", "#1f2c4c", "#2a3c64", "#384f80", "#4c6698")
GOLD = pg.hexes("#5a3c10", "#8c621e", "#c2902c", "#e6c058", "#fff0a6")
SAIL = pg.hexes("#7e7a6a", "#a8a28c", "#cbc5ac", "#e4dfca", "#f6f3e6")
MAST = pg.hexes("#24160e", "#43291a", "#63412a", "#835b3a")
DECK = pg.hexes("#40291a", "#5e4028", "#7c5a3a", "#9a7550")
GLASS = pg.hexes("#7a3e16", "#e0a040", "#ffe08a")
FOAM = pg.hexes("#7fb2c6", "#c4e2ea", "#f2fafc")
SEA = pg.hexes("#0c2238", "#112e48", "#163a58", "#1d4868", "#255a7c")
ROPE = np.array([46, 32, 22], dtype=np.uint8)
OUTLINE = np.array([8, 10, 20], dtype=np.uint8)

YY, XX = np.mgrid[0:H, 0:W].astype(float)


# ── 선체 모양 ─────────────────────────────────────────────

# 뱃전 윗줄(점): 뱃머리에서 솟고, 허리에서 낮고, 고물 망루에서 높다.
SHEER_X = [20, 44, 90, 150, 232, 282, 298, 302, 350, 362]
SHEER_Y = [WL - 86, WL - 80, WL - 66, WL - 58, WL - 58, WL - 66, WL - 72, WL - 112, WL - 118, WL - 114]
CASTLE_FROM = 300                   # 고물 망루가 시작하는 x


def sheer(x):
    return np.interp(x, SHEER_X, SHEER_Y)


def stem(y):
    """뱃머리 앞 끝의 x: 위에서 앞으로 내밀고 물 쪽으로 갈수록 뒤로 굽는다."""
    t = np.clip((y - sheer(20)) / (WL + 6 - sheer(20)), 0, 1)
    return 20 + 54 * t ** 1.7


def stern(y):
    """고물 끝의 x: 물에 닿는 곳에서 안쪽으로 들어갔다가 위로 갈수록 바깥으로 나온다."""
    return 334 + 28 * np.clip((WL + 6 - y) / 44, 0, 1)


def hull_mask():
    return (XX >= stem(YY)) & (XX <= stern(YY)) & (YY >= sheer(XX)) & (YY <= WL + 8)


def paint_hull(rgba):
    mask = hull_mask()
    top = sheer(XX)
    depth = YY - top                                   # 뱃전에서 내려온 깊이(점)
    # 빛은 왼쪽 위: 윗부분이 밝고 물 쪽으로 둥글게 말려 들어가며 어두워진다. 뱃머리 쪽이 조금 더 밝다.
    # 아래쪽은 선체가 안으로 말려 들어가 그늘이 짙다.
    light = 0.8 - depth / 150 - np.clip((YY - (WL - 22)) / 22, 0, 1) * 0.22 + (1 - XX / W) * 0.12
    plank = (np.floor(depth / 6) % 2 == 0)
    light = light + np.where(plank, 0.03, -0.03)
    rgb = pg.quantize(np.clip(light, 0, 0.999), NAVY[1:], dither=0.08)
    seam = (np.mod(depth, 6) < 1) & (depth > 2)
    rgb[seam] = NAVY[0]
    # 판자 끝 이음매: 줄마다 어긋나게.
    row = np.floor(depth / 6)
    butt = (np.mod(XX + row * 23, 52) < 1) & ~seam
    rgb[butt] = NAVY[1]
    rgba[mask, :3] = rgb[mask]
    rgba[mask, 3] = 255

    # 금빛 띠 둘(뱃전 바로 아래와 포문 아래), 물에 닿는 흰 띠.
    for offset, thick in ((3, 3), (30, 4)):
        band = mask & (depth >= offset) & (depth < offset + thick)
        rgba[band, :3] = GOLD[2]
        rgba[band & (depth < offset + 1), :3] = GOLD[4]
        rgba[band & (depth >= offset + thick - 1), :3] = GOLD[1]
    boot = mask & (YY >= WL - 5) & (YY < WL - 1)
    rgba[boot, :3] = SAIL[3]
    rgba[mask & (YY >= WL - 1) & (YY < WL), :3] = SAIL[1]
    # 뱃전 윗줄 손잡이 난간(금빛 테).
    rail = mask & (depth < 2)
    rgba[rail, :3] = GOLD[3]

    # 포문 두 줄: 위 줄은 두 금빛 띠 사이, 아래 줄은 그 밑(어긋나게). 금테 안의 어둠과 대포 아가리.
    for row, (start, below) in enumerate(((92, 13), (103, 40))):
        for x in range(start, 292 - row * 10, 22):
            y = int(round(sheer(x + 4))) + below
            if y + 9 >= WL - 6:
                continue
            rgba[y - 1:y + 9, x - 1:x + 10, :3] = GOLD[1]
            rgba[y - 1, x - 1:x + 10, :3] = GOLD[3]
            rgba[y:y + 8, x:x + 9, :3] = NAVY[0]
            rgba[y + 3:y + 6, x + 2:x + 7, :3] = (34, 36, 42)
            rgba[y + 3, x + 3:x + 6, :3] = (70, 74, 84)

    # 고물 망루: 불 켜진 창 셋과 금빛 조각 테, 꼭대기 등불.
    castle_top = int(sheer(320))
    for i, x in enumerate((310, 324, 338)):
        y = castle_top + 12
        rgba[y - 1:y + 12, x - 1:x + 9, :3] = GOLD[2]
        rgba[y:y + 11, x:x + 8, :3] = GLASS[1]
        rgba[y:y + 4, x:x + 8, :3] = GLASS[2]
        rgba[y + 5, x:x + 8, :3] = GOLD[1]
        rgba[y:y + 11, x + 3:x + 5, :3] = GOLD[1]
    # 고물 옆 둥근 창 회랑: 망루 옆에 불룩하게 내민 방, 금테와 창 둘, 아래로 오므라든 조각 끝.
    gx, gy = 328, castle_top + 46
    gallery = (np.abs(XX - gx) <= 16 - np.clip(YY - gy, 0, 20) * 0.6) & (YY >= gy - 14) & (YY <= gy + 18)
    rgba[gallery, :3] = NAVY[3]
    rgba[gallery & (YY < gy - 10), :3] = GOLD[3]
    rgba[gallery & (YY > gy + 6) & (YY <= gy + 8), :3] = GOLD[2]
    rgba[gallery & (YY > gy + 12), :3] = GOLD[1]
    for x in (318, 332):
        rgba[gy - 7:gy + 4, x:x + 7, :3] = GOLD[2]
        rgba[gy - 6:gy + 3, x + 1:x + 6, :3] = GLASS[1]
        rgba[gy - 6:gy - 3, x + 1:x + 6, :3] = GLASS[2]
    rgba[gallery, 3] = 255
    trim = (XX >= CASTLE_FROM) & (XX <= stern(YY)) & (YY >= sheer(XX) + 28) & (YY < sheer(XX) + 31)
    rgba[trim & mask, :3] = GOLD[3]
    # 망루 앞 벽(허리 갑판에서 솟은 면).
    front = (XX >= CASTLE_FROM - 3) & (XX < CASTLE_FROM + 1) & (YY >= sheer(XX)) & (YY < sheer(CASTLE_FROM - 6) + 4)
    rgba[front, :3] = GOLD[2]
    rgba[front, 3] = 255
    lamp_x, lamp_y = 352, castle_top - 14
    rgba[lamp_y + 6:castle_top, lamp_x:lamp_x + 2, :3] = MAST[1]
    rgba[lamp_y + 6:castle_top, lamp_x:lamp_x + 2, 3] = 255
    rgba[lamp_y:lamp_y + 7, lamp_x - 3:lamp_x + 5, :3] = GOLD[2]
    rgba[lamp_y + 1:lamp_y + 6, lamp_x - 2:lamp_x + 4, :3] = GLASS[2]
    rgba[lamp_y:lamp_y + 7, lamp_x - 3:lamp_x + 5, 3] = 255


def paint_deck(rgba):
    """뱃전 너머로 조금 보이는 갑판과 맞은편 뱃전(위에서 비스듬히 본 띠)."""
    top = sheer(XX)
    for x0, x1, depth in ((44, CASTLE_FROM - 2, 12),):
        strip = (XX >= x0) & (XX < x1) & (YY >= top - depth) & (YY < top) & (XX >= stem(YY) + 6)
        board = np.floor((XX - x0) / 9) % 2
        rgb = np.where(board[..., None] == 0, DECK[2], DECK[1])
        rgba[strip, :3] = rgb[strip]
        rgba[strip & (YY < top - depth + 3), :3] = NAVY[2]        # 맞은편 뱃전 안쪽 면
        rgba[strip & (YY < top - depth + 1), :3] = GOLD[3]        # 맞은편 뱃전 윗줄
        rgba[strip, 3] = 255
    # 고물 망루 지붕.
    castle = (XX >= CASTLE_FROM) & (XX <= stern(YY) - 2) & (YY >= top - 10) & (YY < top)
    rgba[castle, :3] = DECK[2]
    rgba[castle & (YY < top - 8), :3] = GOLD[3]
    rgba[castle, 3] = 255


# ── 돛대와 돛 ─────────────────────────────────────────────

MASTS = [
    # x, 꼭대기 y, 굵기, 네모돛(위 y, 아래 y, 너비) 목록
    (128, 30, 5, [(40, 62, 52), (68, 96, 70), (102, 140, 84)]),
    (206, 8, 6, [(22, 52, 62), (58, 92, 82), (98, 142, 98)]),
    (280, 52, 5, [(64, 92, 54)]),
]


def mast_base(x):
    return int(sheer(x)) - 6


def paint_masts(rgba):
    for x, top, thick, sails in MASTS:
        base = mast_base(x)
        left = x - thick // 2
        rgba[top:base, left:left + thick, :3] = MAST[2]
        rgba[top:base, left:left + 1, :3] = MAST[3]
        rgba[top:base, left + thick - 1:left + thick, :3] = MAST[1]
        rgba[top:base, left:left + thick, 3] = 255
        for y in range(top + 14, base, 34):
            rgba[y:y + 2, left - 1:left + thick + 1, :3] = GOLD[2]
            rgba[y:y + 2, left - 1:left + thick + 1, 3] = 255
        # 큰 돛대 망대.
        if thick == 6:
            rgba[94:98, x - 12:x + 12, :3] = MAST[1]
            rgba[94:95, x - 12:x + 12, :3] = GOLD[3]
            rgba[94:98, x - 12:x + 12, 3] = 255


def sail_light(u, v):
    """네모돛 한 장의 빛: 바람을 받아 부푼 천이라 가운데가 앞으로 나오고, 왼쪽이 밝다."""
    belly = 1 - (2 * u - 1) ** 2
    return 0.55 + 0.25 * belly - 0.22 * u + 0.12 * (1 - v) + 0.1 * np.sin(v * np.pi) * belly


def paint_square_sail(rgba, x, top, bottom, width, emblem=False):
    left, right = x - width / 2, x + width / 2
    u = (XX - left) / width
    v = (YY - top) / (bottom - top)
    # 아래 끝은 바람에 부풀어 둥글게 처지고, 양 옆은 아래로 가며 조금 벌어진다.
    sag = 5 * np.sin(np.clip(u, 0, 1) * np.pi)
    spread = 3 * np.clip(v, 0, 1)
    inside = (XX >= left - spread) & (XX <= right + spread) & (YY >= top) & (YY <= bottom + sag)
    light = sail_light(np.clip(u, 0, 1), np.clip(v, 0, 1))
    rgb = pg.quantize(np.clip(light, 0, 0.999), SAIL, dither=0.1)
    # 천 이음 줄(세로)과 부푼 주름(가로 그늘).
    seam = (np.mod(XX - left, 12) < 1)
    rgb[seam] = SAIL[1]
    rgba[inside, :3] = rgb[inside]
    rgba[inside, 3] = 255
    edge = inside & ~(
        (XX >= left - spread + 1) & (XX <= right + spread - 1) & (YY >= top + 1) & (YY <= bottom + sag - 1))
    rgba[edge, :3] = SAIL[1]
    # 활대(돛 위의 가로 나무).
    y = int(top)
    rgba[y - 2:y + 1, int(left) - 4:int(right) + 5, :3] = MAST[2]
    rgba[y - 2, int(left) - 4:int(right) + 5, :3] = MAST[3]
    rgba[y - 2:y + 1, int(left) - 4:int(right) + 5, 3] = 255
    if emblem:
        paint_emblem(rgba, x, (top + bottom) / 2 + 2)


def paint_emblem(rgba, cx, cy):
    """에라드 문장: 금빛 해 둘레로 펼친 두 날개(깃 세 겹)."""
    for side in (-1, 1):
        for layer, (reach, lift, thick) in enumerate(((30, 12, 5), (24, 7, 4), (17, 3, 4))):
            t = np.clip(side * (XX - cx) / reach, 0, 1)
            centre = cy - lift * np.sin(t * np.pi * 0.85) + layer * 4 - 2
            wing = (side * (XX - cx) >= 5) & (side * (XX - cx) <= reach) & (np.abs(YY - centre) <= thick * (1 - t * 0.6))
            rgba[wing, :3] = GOLD[3 if layer == 0 else 2]
            rgba[wing & (YY < centre - thick * (1 - t * 0.6) + 1.5), :3] = GOLD[4]
    r = np.hypot(XX - cx, YY - cy)
    rgba[r <= 6.5, :3] = GOLD[2]
    rgba[r <= 5, :3] = GOLD[3]
    rgba[r <= 2.5, :3] = GOLD[4]
    for k in range(8):
        a = k * np.pi / 4
        for d in range(8, 11):
            px, py = int(round(cx + np.cos(a) * d)), int(round(cy + np.sin(a) * d * 0.9))
            rgba[py, px, :3] = GOLD[3]


def triangle(p0, p1, p2):
    def side(a, b):
        return (b[0] - a[0]) * (YY - a[1]) - (b[1] - a[1]) * (XX - a[0])
    s0, s1, s2 = side(p0, p1), side(p1, p2), side(p2, p0)
    return ((s0 >= 0) & (s1 >= 0) & (s2 >= 0)) | ((s0 <= 0) & (s1 <= 0) & (s2 <= 0))


def paint_fore_and_aft(rgba):
    """이물대에서 앞돛대로 삼각돛 둘, 뒷돛대 뒤 세로돛(바람에 조금 부푼 가로 결)."""
    bowsprit_tip = (2, 108)
    sails = [
        triangle((124, 38), bowsprit_tip, (98, 132)),
        triangle((125, 70), (34, 124), (106, 140)),
    ]
    spanker = (XX >= 284) & (XX <= 336) & (YY >= 58 + (XX - 284) * 0.55) & (YY <= 132 - (XX - 284) * 0.1)
    for shape, light_at in ((sails[0], 0), (sails[1], 1), (spanker, 2)):
        light = 0.75 - 0.18 * light_at - (YY / H) * 0.25 + 0.06 * np.sin(XX * 0.21)
        rgb = pg.quantize(np.clip(light, 0, 0.999), SAIL, dither=0.1)
        rgba[shape, :3] = rgb[shape]
        rgba[shape, 3] = 255
        ring = pg.neighbours(~shape) & shape
        rgba[ring, :3] = SAIL[1]
    # 세로돛 위아래 활대.
    rgba[56:59, 280:338, :3] = MAST[2]
    rgba[56:59, 280:338, 3] = 255
    rgba[130:133, 280:340, :3] = MAST[1]
    rgba[130:133, 280:340, 3] = 255


def line(rgba, p0, p1, color, width=1):
    n = int(max(abs(p1[0] - p0[0]), abs(p1[1] - p0[1]))) + 1
    for t in np.linspace(0, 1, n):
        x = int(round(p0[0] + (p1[0] - p0[0]) * t))
        y = int(round(p0[1] + (p1[1] - p0[1]) * t))
        if 0 <= x < W and 0 <= y < H:
            rgba[y:y + width, x:x + width, :3] = color
            rgba[y:y + width, x:x + width, 3] = 255


def paint_rigging(rgba):
    """돛대를 붙잡는 밧줄: 꼭대기에서 앞뒤 뱃전으로 내린 줄과 돛대 사이 버팀줄, 이물대."""
    line(rgba, (40, WL - 84), (2, 108), MAST[2], 3)
    line(rgba, (2, 107), (2, 109), GOLD[3], 2)
    for x, top, _, _ in MASTS:
        for dx in (-30, -18, 22, 34):
            foot = (x + dx, int(sheer(x + dx)) + 2)
            line(rgba, (x, top + 6), foot, ROPE)
    line(rgba, (206, 10), (128, 32), ROPE)
    line(rgba, (206, 10), (280, 54), ROPE)
    line(rgba, (128, 32), (4, 108), ROPE)


def paint_flags(rgba):
    """돛대 꼭대기 긴 깃발(바람에 오른쪽으로 날림)과 고물의 큰 깃발."""
    for x, top, _, _ in MASTS:
        length = 34 if top < 20 else 26
        for i in range(length):
            wave = int(round(np.sin(i * 0.35) * 2))
            half = max(1, int(round(3 * (1 - i / length))))
            y0 = top - 4 + wave
            rgba[y0 - half:y0 + half + 1, x + 2 + i, :3] = SAIL[4] if i < length - 6 else GOLD[3]
            rgba[y0 - half, x + 2 + i, :3] = SAIL[2]
            rgba[y0 - half:y0 + half + 1, x + 2 + i, 3] = 255
        rgba[top - 6:top, x - 1:x + 2, :3] = GOLD[3]
        rgba[top - 6:top, x - 1:x + 2, 3] = 255
    # 고물 깃발: 깃대와 금테 흰 깃발, 가운데 해.
    pole_x, pole_top = 360, int(sheer(356)) - 40
    rgba[pole_top:int(sheer(356)), pole_x:pole_x + 2, :3] = MAST[2]
    rgba[pole_top:int(sheer(356)), pole_x:pole_x + 2, 3] = 255
    for i in range(22):
        wave = int(round(np.sin(i * 0.4 + 1) * 1.5))
        y0, y1 = pole_top + 2 + wave, pole_top + 18 + wave
        x = pole_x + 2 + i
        if x >= W:
            break
        rgba[y0:y1, x, :3] = SAIL[4]
        rgba[y0, x, :3] = GOLD[3]
        rgba[y1 - 1, x, :3] = GOLD[3]
        rgba[y0:y1, x, 3] = 255
    r = np.hypot(XX - (pole_x + 12), YY - (pole_top + 10))
    rgba[(r <= 3.5) & (rgba[..., 3] > 0) & (XX > pole_x + 2), :3] = GOLD[3]


def paint_figurehead(rgba):
    """이물 아래 날개 펼친 금빛 뱃머리 장식."""
    cx, cy = 30, WL - 72
    body = ((XX - cx) / 5) ** 2 + ((YY - cy) / 9) ** 2 <= 1
    wing = triangle((cx + 2, cy - 4), (cx + 26, cy - 16), (cx + 20, cy + 4))
    wing |= triangle((cx + 2, cy), (cx + 22, cy + 2), (cx + 12, cy + 10))
    shape = body | wing
    rgba[shape, :3] = GOLD[2]
    rgba[shape & (YY < cy - 2), :3] = GOLD[3]
    rgba[body & (XX < cx - 1), :3] = GOLD[4]
    rgba[shape, 3] = 255


# ── 물 ───────────────────────────────────────────────

def paint_foam(rgba, rng):
    """물에 닿는 줄의 물거품과 뱃머리 물결(배가 왼쪽으로 나아간다), 고물 뒤 물보라."""
    n = pg.noise(H, W, 5, rng)
    span = (XX >= stem(np.full_like(YY, WL)) - 14) & (XX <= stern(np.full_like(YY, WL)) + 10)
    band = span & (YY >= WL - 3 + n * 3) & (YY <= WL + 6 + n * 5)
    bow = (np.hypot((XX - 64) / 26, (YY - WL - 2) / 7) <= 1 + n * 0.5) & (XX < 80)
    wake = (XX > 320) & (XX < 380) & (np.abs(YY - WL - 3) <= 2 + (XX - 320) / 18 * n)
    foam = band | bow | wake
    level = np.clip(n * 1.4 - (YY - WL) / 14, 0, 0.999)
    rgba[foam, :3] = pg.quantize(level, FOAM, dither=0.2)[foam]
    rgba[foam, 3] = 255
    # 물에 잠긴 선체 아래는 지운다.
    rgba[YY > WL + 10] = 0


def build():
    rng = np.random.default_rng(9)
    rgba = np.zeros((H, W, 4), dtype=np.uint8)
    paint_rigging(rgba)
    paint_masts(rgba)
    for x, _, _, sails in MASTS:
        for i, (top, bottom, width) in enumerate(sails):
            paint_square_sail(rgba, x, top, bottom, width, emblem=(x == 206 and i == 2))
    paint_fore_and_aft(rgba)
    paint_flags(rgba)
    paint_deck(rgba)
    paint_hull(rgba)
    paint_figurehead(rgba)
    out = pg.outline(rgba, OUTLINE)
    paint_foam(out, rng)
    return out


def preview(sprite):
    rng = np.random.default_rng(3)
    sea_h, sea_w = H + 60, W + 80
    yy, xx = np.mgrid[0:sea_h, 0:sea_w]
    n = pg.noise(sea_h, sea_w, 9, rng)
    stripe = np.sin(yy * 0.9 + n * 6) * 0.5 + 0.5
    canvas = pg.quantize(np.clip(0.35 + stripe * 0.2 + n * 0.25, 0, 0.999), SEA, dither=0.2)
    pg.stamp(canvas, sprite, sea_w / 2, sea_h - 30, (W / 2, H))
    return canvas


def main(project):
    sprite = build()
    out = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(sprite), "RGBA").save(out / "warship.png")
    Image.fromarray(pg.upscale(preview(sprite)), "RGB").save(project / "Docs/ArtUpgrade/PixelGen/warship_preview.png")
    print(out / "warship.png")


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
