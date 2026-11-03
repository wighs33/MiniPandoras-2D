"""몬스터 6~10레벨: 땅과 바다에 스민 십이장의 더스트, 옛 싸움의 흔적에 깃든 것들(monsters.py의 도감 차례).

  6 덤불지기    대자연(청룡)의 더스트 찌꺼기로 숲길의 가시덤불이 일어선 것. 엮인 넝쿨 몸, 잎 두건, 빨간 열매 눈, 가시
  7 모래단지    모래(라)의 더스트가 사막에 묻힌 옛 토기에 깃든 것. 들썩이는 뚜껑 틈의 눈, 문양 띠, 모래로 된 팔
  8 돌비늘뱀    뱀성 돌뱀 조형물에서 떨어진 비늘 조각들이 이어진 작은 돌뱀. 금빛 눈, 돌 볏
  9 빈 갑주     전장에 버려진 에라드 병사 갑주에 패잔병의 원념이 깃든 것. 투구 틈 붉은 빛, 녹, 찢긴 남색 망토, 녹슨 창
 10 물거품령    바다(아쿠아)의 더스트가 파도 거품에 깃든 것. 거품 갓 속 진주 빛, 흔들리는 물줄기
"""
import numpy as np

import pixelgen as pg
from monster_kit import GLINT, ball, blob, canvas, chain, glow_eye, limb, shade, spark, speckle, stripes

RED_GLOW = pg.hexes("#4a0a0c", "#a01a1c", "#e8483a", "#ffc0a0")
VOID = pg.hexes("#0a080c", "#16121a")

# ── 6 덤불지기 ─────────────────────────────────────────

VINE = pg.hexes("#1c2812", "#2c401e", "#42602a", "#5a7c36", "#7a9a46", "#9eb660")
LEAF = pg.hexes("#183018", "#24482a", "#356236", "#4c7e40", "#6a9a4c", "#94bc62")
ROOT = pg.hexes("#20150e", "#3a2618", "#573a24", "#755034", "#936a44")
THORN = np.array([222, 212, 176], dtype=np.uint8)


def _thorns(c, body, cx, cy, every):
    """몸 가장자리 몇 곳에서 바깥으로 두 점짜리 가시가 돋는다."""
    ring = pg.neighbours(body) & ~body
    ys, xs = np.nonzero(ring)
    for x, y in zip(xs, ys):
        if (x * 7 + y * 13) % every:
            continue
        dx, dy = x + 0.5 - cx, y + 0.5 - cy
        n = max(np.hypot(dx, dy), 1e-6)
        c.image[y, x, :3], c.image[y, x, 3] = THORN, 255
        tx, ty = int(round(x + dx / n)), int(round(y + dy / n))
        if 0 <= tx < c.image.shape[1] and 0 <= ty < c.image.shape[0]:
            c.image[ty, tx, :3], c.image[ty, tx, 3] = THORN, 255


def bramble_warden(view, frame):
    c = canvas(26, 36)
    cx = 26
    bob = 1 if frame else 0
    side_view = view == "right"
    # 뿌리 다리: 뒤틀린 뿌리가 땅을 짚고, 발끝은 잔뿌리로 갈라진다.
    for side in (-1, 1):
        up = 2 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
        hip = (cx + side * (2 if side_view else 5), 46 - bob)
        reach = (0, 3, -3)[frame] * side if side_view else 0
        foot = (cx + side * (4 if side_view else 8) + reach, 68 - up)
        knee = ((hip[0] + foot[0]) / 2 + side * 2.5, (hip[1] + foot[1]) / 2 - 1)
        chain(c, [hip, (hip[0] - side, hip[1] + 5), knee, (knee[0] - side * 1.5, knee[1] + 5), foot], 2.6, 1.8, ROOT)
        ball(c, knee[0], knee[1], 2.4, 2.2, ROOT, bias=0.05)
        for toe in (-4, -1, 2, 4):
            limb(c, foot, (foot[0] + toe, foot[1] + 2 + abs(toe) * 0.3), 1.1, 0.5, ROOT, bias=-0.1)
    # 몸통: 위아래로 감아 오른 넝쿨 여러 줄이 엮여 있다.
    rx, top, bottom = (8 if side_view else 12), 14 - bob, 48 - bob
    body = np.zeros(c.xx.shape, dtype=bool)
    for k in range(7):
        base = (k - 3) / 3
        points = []
        for y in np.linspace(bottom, top + 4, 7):
            narrow = np.sqrt(np.clip(1 - ((y - (top + bottom) / 2) / ((bottom - top) / 2 + 2)) ** 2, 0.15, 1))
            points.append((cx + base * rx * narrow + 1.6 * np.sin(y * 0.35 + k * 1.7), y))
        # 넝쿨마다 한 겹 굵은 어두운 줄을 먼저 그어, 엮인 넝쿨 사이에 틈이 보이게 한다.
        chain(c, points, 3.3, 2.7, VINE[:1])
        body |= chain(c, points, 2.5, 1.9, VINE, bias=0.04 * (k % 3) - 0.04)
    for y0, y1 in ((24, 33), (38, 31)):
        limb(c, (cx - rx + 1, y0 - bob), (cx + rx - 1, y1 - bob), 1.9, 1.9, VINE[:1])
        body |= limb(c, (cx - rx + 1, y0 - bob), (cx + rx - 1, y1 - bob), 1.3, 1.3, VINE, bias=0.1)
    # 넝쿨에 맺힌 빨간 열매
    for bx, by in ((-6, 30), (5, 37), (-3, 42), (8, 26)):
        if not side_view or bx > 0:
            ball(c, cx + bx * (0.7 if side_view else 1), by - bob, 1.4, 1.4, RED_GLOW, bias=0.15)
    # 팔: 길게 늘어진 넝쿨 끝에 가시 갈퀴
    swing = (0, 3, -3)[frame]
    arms = (1,) if side_view else (-1, 1)
    for side in arms:
        sx = cx + side * (rx - 2)
        hand = (sx + side * 6 + (swing if side_view else 0), 42 - bob + (swing * side * 0.5 if not side_view else 0))
        body |= chain(c, [(sx, 22 - bob), (sx + side * 5, 30 - bob), hand], 2.4, 1.6, VINE)
        for claw in (-2, 0, 2):
            limb(c, hand, (hand[0] + claw + side, hand[1] + 4), 1.0, 0.5, ROOT, bias=0.2)
    # 잎 두건과 얼굴
    hood = blob(c, cx + (2 if side_view else 0), top + 2, 9, LEAF, wobble=1.4, lobes=8, phase=1.2, squash=0.85)
    speckle(c, hood, LEAF[4], 0.18, 61, (0, -bob))
    body |= hood
    _thorns(c, body, cx, (top + bottom) / 2, 5)
    if view == "up":
        return c
    if side_view:
        c.paint(c.ellipse(cx + 6, top + 6, 3.5, 3.4), VOID[0])
        glow_eye(c, cx + 7, top + 6, 1.8, 1.8, RED_GLOW[1:])
    else:
        c.paint(c.ellipse(cx, top + 6, 6.5, 4.2), VOID[0])
        for ex in (cx - 3, cx + 3):
            glow_eye(c, ex, top + 6, 1.9, 1.9, RED_GLOW[1:])
    return c


# ── 7 모래단지 ─────────────────────────────────────────

CLAY = pg.hexes("#44200f", "#6e3419", "#964e25", "#bc6a34", "#d88c4c", "#eeb274")
SANDY = pg.hexes("#6a4a22", "#8e6630", "#b28640", "#d0a656", "#e6c474", "#f4dc9c")
AMBER = pg.hexes("#7a3a08", "#d88a18", "#ffd050", "#fff4c0")
CREAM = np.array([232, 212, 164], dtype=np.uint8)
DARK_PAINT = np.array([52, 26, 14], dtype=np.uint8)


def sand_urn(view, frame):
    c = canvas(26, 30)
    cx = 26
    hop = (0, 2, 1)[frame]
    lid_lift = (3, 5, 2)[frame]
    # 모래 팔(뒤쪽 팔 먼저)
    arm = (0, 2, -2)[frame]
    arms = (1,) if view == "right" else (-1, 1)
    for side in arms:
        root = (cx + side * 13, 31 - hop)
        fist = (cx + side * 21, 45 - hop + arm * side)
        arm_mask = chain(c, [root, (cx + side * 19, 29 - hop), (cx + side * 23, 36 - hop), fist], 2.4, 3.0, SANDY, bias=-0.15)
        speckle(c, arm_mask, SANDY[1], 0.3, 72 + side, (0, -hop))
        speckle(c, arm_mask, SANDY[5], 0.12, 74 + side, (0, -hop))
        f = blob(c, fist[0], fist[1], 3.8, SANDY, wobble=0.8, lobes=6, phase=frame)
        speckle(c, f, SANDY[1], 0.25, 70 + side, (0, 0))
        for k in range(3):
            spark(c, fist[0] + side * (k - 1), fist[1] + 5 + k * 2 + frame, 0.5, SANDY[3], SANDY[4])
    ball(c, cx, 56 - hop, 9, 2.6, CLAY, bias=-0.15)
    belly = ball(c, cx, 40 - hop, 17, 14, CLAY)
    speckle(c, belly, CLAY[1], 0.08, 77, (0, -hop))
    # 문양 띠: 크림색 띠에 진한 지그재그
    band = belly & (c.yy >= 33 - hop) & (c.yy < 38 - hop)
    c.paint(band, CREAM)
    c.paint(band & (np.abs(np.mod(c.xx, 8) - 4) == c.yy - (33 - hop)), DARK_PAINT)
    c.paint(belly & ((c.yy == 32 - hop) | (c.yy == 38 - hop)), DARK_PAINT)
    # 금 사이로 새는 빛
    if view != "up":
        for (x0, y0), (x1, y1) in (((cx - 9, 42), (cx - 6, 46)), ((cx - 6, 46), (cx - 7, 50))):
            limb(c, (x0, y0 - hop), (x1, y1 - hop), 0.6, 0.5, AMBER[1:])
    neck = limb(c, (cx, 22 - hop), (cx, 28 - hop), 7, 8, CLAY, bias=0.05)
    # 모래 팔이 흘러나오는 손잡이 고리
    for side in arms:
        c.paint(c.ellipse(cx + side * 12, 31 - hop, 3, 3.4) & ~c.ellipse(cx + side * 12, 31 - hop, 1.6, 2.0), CLAY[2])
    rim = ball(c, cx, 21 - hop, 10, 3, CLAY, bias=0.2)
    # 뚜껑이 들썩이고 그 틈 어둠에 눈 둘
    gap_top = 21 - hop - lid_lift
    if view == "up":
        lid_lift = 1
        gap_top = 21 - hop - 1
    # 단지 아가리 안의 어둠. 뚜껑과 아가리 사이 틈으로 눈이 보인다.
    c.paint(c.ellipse(cx, 20.5 - hop, 8.5, 2.6) | c.rect(cx - 8, gap_top - 1, cx + 9, 20 - hop), VOID[0])
    if view == "down":
        for ex in (cx - 3.5, cx + 3.5):
            glow_eye(c, ex, 19.5 - hop, 1.8, 1.4, AMBER[1:])
    elif view == "right":
        glow_eye(c, cx + 5, 19.5 - hop, 1.6, 1.4, AMBER[1:])
    tilt = 2 if view == "right" else 0
    lid = ball(c, cx - tilt * 0.5, gap_top - 2.5, 10.5, 3.0, CLAY, bias=0.25)
    ball(c, cx - tilt, gap_top - 5.5, 2.6, 2.4, CLAY, bias=0.2)
    # 뚜껑 틈에서 흘러내리는 모래
    for k in range(2):
        spark(c, cx + 9 - k * 2, 24 - hop + k * 3 + frame, 0.5, SANDY[3], SANDY[4])
    return c


# ── 8 돌비늘뱀 ─────────────────────────────────────────

SERPENT = pg.hexes("#242a36", "#38414f", "#525d6c", "#717d8b", "#95a0ab", "#bfc8ce")
GOLD = pg.hexes("#5a3a08", "#b07a18", "#f0c040", "#fff0a0")


def _segment(c, x, y, r, bias=0.0):
    seg = ball(c, x, y, r, r * 0.9, SERPENT, bias=bias)
    # 비늘 자국: 초승달 모양 밝은 줄
    arc = c.ellipse(x - r * 0.15, y - r * 0.2, r * 0.75, r * 0.6) & ~c.ellipse(x - r * 0.15, y + r * 0.05, r * 0.75, r * 0.6)
    c.paint(seg & arc, SERPENT[4])
    return seg


def _crest(c, x, y, facing):
    for k in range(3):
        bx = x + (k - 1) * 3 * (1 if facing == 0 else 0.6) - (k * 2 if facing > 0 else 0)
        c.paint(c.polygon([(bx - 1.6, y + 1), (bx + 1.6, y + 1), (bx + (0.8 if facing > 0 else 0) - (0.8 if facing < 0 else 0), y - 3.5 - (k == 1) * 1.5)]), SERPENT[3])


def stonecoil(view, frame):
    c = canvas(34, 26)
    phase = frame * 0.9
    if view == "right":
        # 옆모습: 땅을 기는 S자 몸이 앞에서 고개를 쳐든다.
        points = []
        for k in range(14):
            t = k / 13
            x = 6 + t * 44
            y = 42 + 3.5 * np.sin(t * 6.0 + phase) * (1 - t * 0.6)
            points.append((x, y, 2.2 + t * 3.6))
        neck = [(50 + k * 1.6, 40 - k * 4.2, 6 - k * 0.3) for k in range(6)]
        for x, y, r in points + neck:
            _segment(c, x, y, r)
        hx, hy = 60, 15
        ball(c, hx - 1, hy, 7, 5.5, SERPENT, bias=0.05)
        ball(c, hx + 4.5, hy + 1.5, 4.5, 3.4, SERPENT, bias=0.1)
        c.paint(c.rect(hx, hy + 3, hx + 9, hy + 4), SERPENT[0])
        glow_eye(c, hx + 1, hy - 1.5, 1.8, 1.4, GOLD[1:])
        c.paint(c.ellipse(hx + 1.3, hy - 1.5, 0.5, 1.2), VOID[0])
        _crest(c, hx - 5, hy - 5, 1)
        return c
    # 앞·뒤: 몸을 둥글게 감고 가운데에서 고개를 든다.
    cx, cy = 34, 40
    ring = []
    for k in range(18):
        a = np.pi * (1.05 + k / 17 * 1.9) + phase * 0.15
        r = 6.5 - abs(k - 9) * 0.25
        ring.append((cx + 24 * np.cos(a), cy + 8 * np.sin(a), r))
    ring.sort(key=lambda s: s[1])
    for x, y, r in ring:
        _segment(c, x, y, r, bias=-0.05 if y < cy else 0.0)
    sway = (0, -1.5, 1.5)[frame]
    neck = [(cx + sway * k / 5, cy - k * 4.6, 6.4 - k * 0.25) for k in range(6)]
    for x, y, r in neck:
        _segment(c, x, y, r)
    hx, hy = cx + sway, 13
    head = ball(c, hx, hy, 8.5, 6.5, SERPENT, bias=0.05)
    _crest(c, hx, hy - 5, 0)
    if view == "down":
        ball(c, hx, hy + 3, 5.5, 3.2, SERPENT, bias=0.15)
        for ex in (hx - 5, hx + 5):
            glow_eye(c, ex, hy - 0.5, 1.9, 1.5, GOLD[1:])
            c.paint(c.ellipse(ex, hy - 0.5, 0.5, 1.3), VOID[0])
        for nx in (hx - 1.5, hx + 1.5):
            c.paint(c.ellipse(nx, hy + 3.5, 0.6, 0.6), SERPENT[0])
    return c


# ── 9 빈 갑주 ──────────────────────────────────────────

STEEL = pg.hexes("#22252d", "#393e49", "#575d6a", "#7a808c", "#a1a7b0", "#ccd0d5")
RUST = pg.hexes("#4a2410", "#7a3e1a", "#a85a28")
CAPE = pg.hexes("#11162e", "#1c2548", "#2a376a", "#3e5190")
TRIM = pg.hexes("#6a4a12", "#b08a30", "#e8c868")
POLE = pg.hexes("#2a1c12", "#4a3220", "#6c4a30")


def _spear(c, x, top, bottom):
    shaft = c.rect(x - 1, top + 6, x + 1, bottom)
    shade(c, shaft, 0.55 - (c.xx - x) * 0.15, POLE)
    blade = c.polygon([(x, top - 4), (x + 3, top + 3), (x, top + 8), (x - 3, top + 3)])
    shade(c, blade, 0.75 - (c.xx - x) * 0.08 - (c.yy - top) * 0.02, STEEL)
    speckle(c, blade, RUST[1], 0.3, 93, (0, 0))
    c.paint(c.rect(x - 2, top + 7, x + 2, top + 9), TRIM[1])


def hollow_helm(view, frame):
    c = canvas(26, 40)
    cx = 26
    bob = 1 if frame else 0
    side_view = view == "right"
    # 찢긴 남색 망토(몸 뒤)
    y = c.yy + 0.5
    t = np.clip((y - 26) / 40, 0, 1)
    cape_half = (6 if side_view else 11) + t * 4
    cape_cx = cx - (4 if side_view else 0) - t * (5 if side_view else 0) + (frame - 1) * t
    hem = 64 + 4 * np.abs(np.sin((c.xx + 0.5) * 0.7 + frame)) - bob
    cape = (y >= 25 - bob) & (np.abs(c.xx + 0.5 - cape_cx) <= cape_half) & (y <= hem)
    shade(c, cape, 0.62 + 0.2 * np.sin((c.xx + 0.5) * 0.6) - t * 0.25, CAPE)
    # 다리: 정강이 갑주와 쇠신, 무릎 틈은 비어 있다.
    for side in (-1, 1):
        up = 3 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
        reach = (0, 3, -3)[frame] * side if side_view else 0
        hx = cx + side * (2 if side_view else 5)
        knee = (hx + reach * 0.5, 61 - up)
        foot = (hx + reach, 75 - up)
        limb(c, (hx, 50 - bob), (knee[0], knee[1] - 3), 3.6, 3.2, STEEL)
        c.paint(c.ellipse(knee[0], knee[1] - 1, 2.6, 1.6), VOID[0])
        limb(c, knee, foot, 3.2, 3.0, STEEL, bias=0.05)
        ball(c, foot[0] + (2 if side_view else 0), foot[1] + 1.5, 4.6, 2.4, STEEL, bias=-0.05)
    # 몸통: 가슴판(가운데 능선), 허리 비늘판
    chest = ball(c, cx, 37 - bob, 9 if side_view else 11, 12, STEEL, clip=c.yy < 47 - bob)
    if not side_view:
        c.paint(chest & (np.abs(c.xx + 0.5 - cx) < 0.6) & (c.yy < 44 - bob), STEEL[5])
        c.paint(c.polygon([(cx - 3, 33 - bob), (cx + 3, 33 - bob), (cx, 37 - bob)]), TRIM[1])
    for k in range(3):
        band = c.rect(cx - (8 if side_view else 10) + k, 46 - bob + k * 2.5, cx + (8 if side_view else 10) - k, 48.5 - bob + k * 2.5)
        shade(c, band, 0.6 - k * 0.12, STEEL)
    rust = (pg.noise(c.xx.shape[0], c.xx.shape[1], 5, np.random.default_rng(95)) > 0.62)
    rust = np.roll(rust, -bob, axis=0)
    speckle(c, (chest | c.rect(cx - 10, 46 - bob, cx + 10, 54 - bob)) & rust, RUST[1], 0.55, 91, (0, -bob))
    speckle(c, chest & rust, RUST[2], 0.25, 92, (0, -bob))
    # 팔과 창
    swing = (0, 2, -2)[frame]
    if side_view:
        _spear(c, cx + 9, 6 - bob, 74 - bob)
        ball(c, cx + 1, 27 - bob, 6, 5, STEEL, bias=0.1)
        limb(c, (cx + 2, 29 - bob), (cx + 8, 40 - bob), 2.8, 2.6, STEEL)
        ball(c, cx + 9, 41 - bob, 3, 3, STEEL, bias=0.05)
    else:
        limb(c, (cx - 11, 28 - bob), (cx - 13, 45 - bob + swing), 2.8, 2.6, STEEL)
        ball(c, cx - 13, 46 - bob + swing, 3, 3, STEEL, bias=0.05)
        _spear(c, cx + 15, 6 - bob, 74 - bob)
        limb(c, (cx + 11, 28 - bob), (cx + 14, 40 - bob), 2.8, 2.6, STEEL)
        ball(c, cx + 15, 41 - bob, 3, 3, STEEL, bias=0.05)
        for side in (-1, 1):
            ball(c, cx + side * 11, 27 - bob, 5.5, 4.5, STEEL, bias=0.1)
            c.paint(c.ellipse(cx + side * 11, 29.5 - bob, 5, 1) & c.ellipse(cx + side * 11, 27 - bob, 5.5, 4.5), TRIM[1])
    # 투구: 둥근 머리, 눈 틈 속 붉은 빛 둘, 숨구멍
    hx = cx + (2 if side_view else 0)
    helm = ball(c, hx, 15 - bob, 8, 8.5, STEEL, bias=0.05) | limb(c, (hx, 15 - bob), (hx, 22 - bob), 7.5, 7, STEEL)
    speckle(c, helm & rust, RUST[1], 0.4, 94, (0, -bob))
    c.paint(helm & (np.abs(c.xx + 0.5 - hx) < 0.6) & (c.yy < 15 - bob), STEEL[5])
    limb(c, (hx, 6 - bob), (hx - 3, 3 - bob + frame % 2), 1.6, 1.0, CAPE)
    if view == "up":
        return c
    if side_view:
        c.paint(c.rect(hx + 2, 15 - bob, hx + 8, 17 - bob), VOID[0])
        glow_eye(c, hx + 5.5, 16 - bob, 1.4, 1.0, RED_GLOW[1:])
    else:
        c.paint(c.rect(hx - 6, 15 - bob, hx + 6, 17 - bob), VOID[0])
        for ex in (hx - 3, hx + 3):
            glow_eye(c, ex, 16 - bob, 1.6, 1.0, RED_GLOW[1:])
        for k in range(3):
            c.paint(c.ellipse(hx - 2 + k * 2, 20 - bob, 0.5, 0.5), VOID[0])
    return c


# ── 10 물거품령 ────────────────────────────────────────

FOAM = pg.hexes("#24546a", "#3a7a90", "#5ea4b4", "#90c8d2", "#c4e8ea", "#f2ffff")
STREAM = pg.hexes("#1a5470", "#2a7692", "#469cb4", "#78c4d2")
PEARL = pg.hexes("#b898aa", "#e2c8d4", "#fff2f6")
INK = np.array([18, 40, 56], dtype=np.uint8)


def foam_wisp(view, frame):
    c = canvas(26, 32)
    cx = 26 + (1 if view == "right" else 0)
    bob = (0, -1, 1)[frame]
    # 갓 아래로 늘어진 물줄기 여섯(옆에서는 뒤로 흐른다)
    trail = -3 if view == "right" else 0
    for k in range(6):
        x0 = cx - 11 + k * 4.4
        points = [(x0 + 1.6 * np.sin(j * 0.9 + k + frame * 1.1) + trail * j / 5, 26 + bob + j * 4.6) for j in range(6)]
        chain(c, points, 1.8, 0.8, STREAM, bias=0.05)
        tx, ty = points[-1]
        ball(c, tx, ty + 1, 1.4, 1.4, FOAM, bias=0.2)
    dome = ball(c, cx, 21 + bob, 16, 12, FOAM, clip=c.yy < 27 + bob)
    # 아랫자락: 물결치는 거품 테
    rim = c.ellipse(cx, 26.5 + bob, 15.5, 2.8) & (np.mod(c.xx + frame, 4) < 3)
    c.paint(rim, FOAM[4])
    # 거품 방울: 밝은 테와 왼쪽 위 빛점
    rng = np.random.default_rng(101)
    for _ in range(14):
        bx, by, br = rng.uniform(cx - 13, cx + 13), rng.uniform(11, 25), rng.uniform(1.2, 2.4)
        bubble = c.ellipse(bx, by + bob, br, br) & ~c.ellipse(bx + 0.4, by + bob + 0.4, br - 0.8, br - 0.8) & dome
        c.paint(bubble, FOAM[4])
        c.paint(c.ellipse(bx - br * 0.4, by + bob - br * 0.4, 0.5, 0.5) & dome, FOAM[5])
    # 속이 비쳐 보이는 진주
    ball(c, cx, 18 + bob, 4.4, 4.0, PEARL, bias=0.2)
    c.paint(c.ellipse(cx - 1.5, 16.5 + bob, 1, 1), GLINT)
    if view == "up":
        return c
    eyes = (cx + 6,) if view == "right" else (cx - 6, cx + 6)
    for ex in eyes:
        c.paint(c.ellipse(ex, 22 + bob, 1.6, 2.1), INK)
        c.paint(c.ellipse(ex - 0.5, 21.2 + bob, 0.7, 0.7), GLINT)
    return c
