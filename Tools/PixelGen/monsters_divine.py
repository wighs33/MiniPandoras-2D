"""몬스터 16~20레벨: 십이장 번개·시간의 더스트와 태초의 신(차원·거인·재앙)의 잔재(monsters.py의 도감 차례).

 16 뇌명연      번개(래니)의 더스트가 찢긴 종이연에 깃든 것. 먹으로 그린 눈, 붉은 낙관, 매듭 꼬리를 타고 튀는 번개
 17 태엽파수꾼  시간(톰)의 더스트가 옛 시계탑 장치에 깃든 것. 놋쇠 몸, 가슴의 시계판(바늘이 돈다), 톱니 어깨, 진자, 등의 태엽 열쇠
 18 틈새눈      차원(디바인)이 남긴 틈에서 나온 것. 접힌 공간 조각(속에 별)이 보랏빛 눈 하나를 감싸고 돈다
 19 거신의 손   거신 헤토스의 봉인에서 새어 나온 더스트가 무너진 석상의 손에 깃든 것. 손가락으로 걷는다, 금빛 봉인 균열, 끊긴 족쇄
 20 재앙의 왕관 재앙신 칼립스의 원념. 부서진 금관 아래 검은 더스트가 몸을 이루고, 붉은 눈과 금빛 발톱의 긴 팔, 흘러내리는 검은 알갱이
"""
import numpy as np

import pixelgen as pg
from monster_kit import GLINT, ball, blob, canvas, chain, feather, glow_eye, limb, shade, spark, speckle, stripes

VOID = pg.hexes("#0a080c", "#16121a")

# ── 16 뇌명연 ──────────────────────────────────────────

PAPER = pg.hexes("#86785a", "#b4a47e", "#d8cca8", "#f2eacf")
INK = np.array([26, 24, 32], dtype=np.uint8)
RED_INK = pg.hexes("#7a1018", "#b02028", "#e04a48")
BAMBOO = pg.hexes("#3a2a14", "#6a5228", "#9a7c40")
BOLT = pg.hexes("#4a4ab0", "#8a9aff", "#e0e8ff")
BOLT_CORE = np.array([255, 250, 200], dtype=np.uint8)


def _bolt(c, start, length, angle, seed):
    """갈지자로 꺾이는 번개 한 줄기: 푸른 테 안에 밝은 속."""
    rng = np.random.default_rng(seed)
    x, y = start
    points = [(x, y)]
    for k in range(5):
        a = angle + rng.uniform(-0.9, 0.9)
        x, y = x + np.cos(a) * length / 5, y + np.sin(a) * length / 5
        points.append((x, y))
    chain(c, points, 1.1, 0.8, BOLT[:2])
    for a, b in zip(points, points[1:]):
        c.line(int(a[0]), int(a[1]), int(b[0]), int(b[1]), BOLT_CORE)


def thunder_kite(view, frame):
    c = canvas(30, 38)
    cx = 30
    bob = (0, -1, 1)[frame]
    side_view = view == "right"
    half = 8 if side_view else 22
    top, mid, bottom = 4 + bob, 26 + bob, 50 + bob
    # 매듭 꼬리: 아래 꼭짓점에서 물결치며 늘어지고, 번개가 타고 튄다.
    tail = [(cx + 4 * np.sin(k * 0.9 + frame * 1.2) - (k * 1.6 if side_view else 0), bottom + k * 4.2) for k in range(7)]
    chain(c, tail, 0.9, 0.7, PAPER[1:])
    for k in (2, 4, 6):
        x, y = tail[k]
        c.paint(c.polygon([(x - 3, y - 2), (x, y), (x - 3, y + 2)]) | c.polygon([(x + 3, y - 2), (x, y), (x + 3, y + 2)]), RED_INK[1])
    _bolt(c, (tail[3][0], tail[3][1]), 12, 0.3 + frame, 160 + frame)
    _bolt(c, (cx + half * 0.8, mid - 4), 10, -0.6 - frame * 0.4, 170 + frame)
    # 연 바탕: 마름모 종이, 가장자리 몇 군데가 찢겨 있다.
    kite = c.polygon([(cx, top), (cx + half, mid), (cx, bottom), (cx - half, mid)])
    tears = c.polygon([(cx + half * 0.55, mid - 9), (cx + half * 0.8, mid - 6), (cx + half * 0.5, mid - 4)]) | \
        c.polygon([(cx - half * 0.5, mid + 9), (cx - half * 0.3, mid + 12), (cx - half * 0.6, mid + 12)])
    kite &= ~tears
    light = 0.78 - (c.xx - cx) / (half * 6) - (c.yy - top) / 140
    shade(c, kite, light, PAPER)
    edge = pg.neighbours(~kite) & kite
    c.paint(edge, PAPER[1])
    if view == "up":
        # 뒷면: 대나무 살 두 줄
        c.paint(kite & (np.abs(c.xx + 0.5 - cx) < 1.1), BAMBOO[2])
        c.paint(kite & (np.abs(c.yy + 0.5 - (mid - 2) - np.abs(c.xx + 0.5 - cx) * 0.12) < 1.0), BAMBOO[1])
        return c
    # 앞면: 먹으로 그린 눈과 구름 무늬, 붉은 낙관
    ex = cx + (2 if side_view else 0)
    rx = 3.5 if side_view else 9
    eye_outer = c.ellipse(ex, mid - 2, rx + 1.2, 5.6) & kite
    c.paint(eye_outer, INK)
    c.paint(c.ellipse(ex, mid - 2, rx, 4.4) & kite, PAPER[3])
    c.paint(c.ellipse(ex + (1 if side_view else 0), mid - 2, min(rx, 4.2), 4.2), RED_INK[1])
    c.paint(c.ellipse(ex + (1 if side_view else 0), mid - 2, 1.8, 3.2), INK)
    c.paint(c.ellipse(ex - 1, mid - 3.5, 0.8, 0.8), GLINT)
    if not side_view:
        for sx in (-1, 1):
            for k in range(3):
                c.paint(c.ellipse(cx + sx * (11 + k * 2), mid + 6 - k * 3, 2.2 - k * 0.4, 1.2) & kite, INK)
        c.paint(c.rect(cx - 2, bottom - 11, cx + 2, bottom - 7), RED_INK[1])
        c.paint(c.rect(cx - 1, bottom - 10, cx + 1, bottom - 8), RED_INK[2])
        # 양 꼭짓점의 수술
        for sx in (-1, 1):
            limb(c, (cx + sx * half, mid), (cx + sx * (half + 2), mid + 7 + frame), 0.6, 0.6, RED_INK)
    return c


# ── 17 태엽파수꾼 ───────────────────────────────────────

BRASS = pg.hexes("#3a2610", "#6a4a1c", "#9a7030", "#c69a48", "#e4c070", "#fbe6a8")
PATINA = pg.hexes("#2a5a50", "#3e7c6c", "#62a08c")
DIAL = pg.hexes("#a8a090", "#d8d0bc", "#f4f0e2")
LENS = pg.hexes("#0a3a4a", "#1a8aa8", "#6ae0f0", "#e0ffff")
GUNMETAL = pg.hexes("#1e1c20", "#34303a", "#4e4856")


def _gear(c, x, y, r, teeth, spin):
    for k in range(teeth):
        a = spin + k * 2 * np.pi / teeth
        c.paint(c.ellipse(x + np.cos(a) * r, y + np.sin(a) * r, 1.3, 1.3), BRASS[2])
    ball(c, x, y, r, r, BRASS, bias=0.05)
    c.paint(c.ellipse(x, y, r * 0.35, r * 0.35), GUNMETAL[0])


def clock_warden(view, frame):
    c = canvas(30, 44)
    cx = 30
    bob = 1 if frame else 0
    side_view = view == "right"
    spin = frame * 0.5
    # 다리: 톱니 무릎, 납작한 발판
    for side in (-1, 1):
        up = 3 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
        reach = (0, 3, -3)[frame] * side if side_view else 0
        hx = cx + side * (2 if side_view else 6)
        knee = (hx + reach * 0.5 + side, 66 - up * 0.5)
        foot = (hx + reach, 82 - up)
        limb(c, (hx, 54 - bob), knee, 2.2, 1.8, BRASS)
        _gear(c, knee[0], knee[1], 2.6, 8, spin * side)
        limb(c, knee, foot, 1.8, 1.6, BRASS)
        ball(c, foot[0] + (2 if side_view else 0), foot[1] + 1, 4.5, 1.8, GUNMETAL, bias=0.1)
    # 몸통 아래 흔들리는 진자
    swing = (0, -4, 4)[frame]
    limb(c, (cx, 52 - bob), (cx + swing, 64 - bob), 0.7, 0.7, BRASS[1:])
    ball(c, cx + swing, 66 - bob, 3.4, 3.4, BRASS, bias=0.15)
    # 몸통
    rx = 10 if side_view else 13
    torso = ball(c, cx, 40 - bob, rx, 13, BRASS)
    speckle(c, torso & (c.yy > 46 - bob), PATINA[1], 0.15, 171, (0, -bob))
    for k in range(-3, 4):
        c.paint(c.ellipse(cx + k * 3.2, 52.5 - bob, 0.7, 0.7) & torso, BRASS[5])
    # 등의 태엽 열쇠: 옆에서는 몸 뒤로 튀어나오고, 뒤에서는 등 한가운데에 꽂혀 있다.
    ky = 38 - bob
    if side_view:
        kx = cx - (rx + 2)
        limb(c, (kx + 3, ky), (kx - 2, ky), 1.2, 1.2, BRASS)
        for side in (-1, 1):
            ball(c, kx - 3, ky + side * 4, 3, 2.6, BRASS, bias=0.15)
    elif view == "up":
        c.paint(c.ellipse(cx, ky, 2.2, 2.2), GUNMETAL[0])
        limb(c, (cx, ky), (cx, ky - 5), 1.2, 1.2, BRASS, bias=0.1)
        for side in (-1, 1):
            ball(c, cx + side * 4, ky - 6, 3.4, 2.8, BRASS, bias=0.2)
    # 톱니 어깨와 반달 날 손
    arm = (0, 2, -2)[frame]
    for side in ((1,) if side_view else (-1, 1)):
        sx = cx + side * (4 if side_view else 14)
        _gear(c, sx, 30 - bob, 4.6, 10, -spin * side)
        hand = (sx + side * 4, 48 - bob + arm * side)
        limb(c, (sx, 32 - bob), hand, 1.4, 1.2, BRASS)
        blade = c.ellipse(hand[0] + side * 1.5, hand[1] + 3, 4.5, 4.5) & ~c.ellipse(hand[0] + side * 3.5, hand[1] + 1.5, 4, 4)
        shade(c, blade, 0.8 - (c.yy - hand[1]) * 0.05, DIAL)
    # 가슴의 시계판: 바늘이 장마다 돈다.
    if view != "up":
        dx = cx + (5 if side_view else 0)
        drx = 4.5 if side_view else 9
        c.paint(c.ellipse(dx, 40 - bob, drx + 1.4, 10.4), BRASS[4])
        dial = c.ellipse(dx, 40 - bob, drx, 9)
        shade(c, dial, 0.8 - (c.yy - 31 + bob) * 0.02, DIAL)
        for k in range(12):
            a = k * np.pi / 6
            c.paint(c.ellipse(dx + np.cos(a) * drx * 0.78, 40 - bob + np.sin(a) * 7, 0.55, 0.55), GUNMETAL[1])
        for length, angle in ((6.5, -np.pi / 2 + frame * 2.1), (4.2, 0.6 + frame * 0.2)):
            end = (dx + np.cos(angle) * length * drx / 9, 40 - bob + np.sin(angle) * length)
            c.line(int(dx), int(40 - bob), int(end[0]), int(end[1]), GUNMETAL[0])
        c.paint(c.ellipse(dx, 40 - bob, 1, 1), BRASS[2])
    # 머리: 둥근 덮개, 렌즈 눈 하나, 꼭대기 작은 종
    hx = cx + (2 if side_view else 0)
    head = ball(c, hx, 20 - bob, 8, 7, BRASS, bias=0.05)
    speckle(c, head & (c.yy > 22 - bob), PATINA[2], 0.12, 172, (0, -bob))
    limb(c, (hx, 13 - bob), (hx, 9 - bob), 0.7, 0.7, BRASS[1:])
    ball(c, hx, 8 - bob, 2, 1.8, BRASS, bias=0.2)
    if view == "up":
        return c
    lx = hx + (4 if side_view else 0)
    c.paint(c.ellipse(lx, 20 - bob, 4.4, 4.4), GUNMETAL[0])
    glow_eye(c, lx, 20 - bob, 3.2, 3.2, LENS[1:])
    c.paint(c.ellipse(lx - 1.2, 18.8 - bob, 0.8, 0.8), GLINT)
    return c


# ── 18 틈새눈 ──────────────────────────────────────────

SCLERA = pg.hexes("#8a8098", "#c4bcd2", "#ece6f6", "#ffffff")
IRIS = pg.hexes("#2a0a4a", "#5a1a9a", "#9a4ae0", "#d8a8ff")
SPACE = pg.hexes("#0e0a1c", "#1e1636", "#342a5a", "#5a4a8e")
RIFT_EDGE = np.array([200, 130, 255], dtype=np.uint8)
STAR = np.array([236, 230, 255], dtype=np.uint8)


def _shard(c, cx, cy, angle, near, far, width, seed):
    """접힌 공간 조각 하나: 바깥을 향한 뾰족한 유리 조각, 속은 별이 박힌 어둠, 빛 받는 쪽 가장자리는 보랏빛."""
    ca, sa = np.cos(angle), np.sin(angle)
    px, py = -sa, ca
    base = (cx + ca * near, cy + sa * near)
    tip = (cx + ca * far, cy + sa * far)
    poly = c.polygon([(base[0] + px * width, base[1] + py * width), tip, (base[0] - px * width, base[1] - py * width)])
    shade(c, poly, 0.35 + (c.yy - cy) * -0.01, SPACE)
    rng = np.random.default_rng(seed)
    stars = poly & (rng.random(c.xx.shape) < 0.05)
    c.paint(stars, STAR)
    edge = pg.neighbours(~poly) & poly
    c.paint(edge & ((c.xx + 0.5 - cx) * pg.LIGHT[0] + (c.yy + 0.5 - cy) * pg.LIGHT[1] > 0), RIFT_EDGE)
    return poly


def rift_eye(view, frame):
    c = canvas(32, 32)
    cx, cy = 32, 28 + (0, -1, 1)[frame]
    turn = frame * 0.12
    shards = [(a + turn, near, far, w) for a, near, far, w in
              ((-2.6, 12, 26, 4.5), (-1.7, 11, 24, 4), (-0.7, 12, 27, 4.5), (0.3, 13, 25, 4), (1.2, 12, 23, 3.5),
               (2.1, 12, 26, 4.5), (2.9, 12, 22, 3.5))]
    behind = [s for s in shards if np.sin(s[0]) < 0]
    front = [s for s in shards if np.sin(s[0]) >= 0]
    for k, (a, near, far, w) in enumerate(behind):
        _shard(c, cx, cy, a, near, far, w, 180 + k)
    for k in range(5):
        spark(c, cx + np.cos(k * 1.3 + turn * 3) * 20, cy + np.sin(k * 1.3 + turn * 3) * 18, 0.7, IRIS[2], IRIS[3])
    if view == "up":
        # 뒤에서 보면 눈 대신 빨려 드는 소용돌이
        swirl = c.ellipse(cx, cy, 11, 10)
        angle = np.arctan2(c.yy + 0.5 - cy, c.xx + 0.5 - cx)
        r = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
        shade(c, swirl, 0.3 + 0.25 * np.sin(angle * 2 + r * 0.6 - frame), SPACE)
        c.paint(swirl & (np.abs(np.sin(angle * 2 + r * 0.6 - frame)) > 0.92), IRIS[2])
    else:
        rx = 8 if view == "right" else 12
        # 공간이 눈꺼풀처럼 접혀 눈을 위아래로 덮는다.
        c.paint(c.ellipse(cx, cy, rx + 3, 12), SPACE[1])
        white = ball(c, cx, cy, rx, 9.5, SCLERA)
        for k in range(4):
            a = k * 1.6 + 0.4
            limb(c, (cx + np.cos(a) * rx * 0.95, cy + np.sin(a) * 9), (cx + np.cos(a) * rx * 0.6, cy + np.sin(a) * 6), 0.5, 0.4, IRIS[1:3])
        ix = cx + (3 if view == "right" else 0) + (frame - 1)
        iris = c.ellipse(ix, cy, 6 if view == "down" else 4.5, 6.2) & white
        shade(c, iris, 0.85 - np.hypot(c.xx + 0.5 - ix, c.yy + 0.5 - cy) / 8, IRIS)
        c.paint(c.ellipse(ix, cy, 1.4, 5) & white, VOID[0])
        c.paint(c.ellipse(ix - 2.2, cy - 2.6, 1.2, 1.2), GLINT)
        lids = (c.ellipse(cx, cy - 10.5, rx + 3, 4.2) | c.ellipse(cx, cy + 10.5, rx + 3, 4.2)) & c.ellipse(cx, cy, rx + 3, 12)
        shade(c, lids, 0.4, SPACE)
        c.paint(pg.neighbours(lids) & ~lids & white, RIFT_EDGE)
    for k, (a, near, far, w) in enumerate(front):
        _shard(c, cx, cy, a, near, far, w, 190 + k)
    return c


# ── 19 거신의 손 ───────────────────────────────────────

GRANITE = pg.hexes("#2a2622", "#433c35", "#5f564c", "#7e7366", "#a09486", "#c4b8a8")
SEAL = pg.hexes("#6a4a08", "#c89018", "#ffd040", "#fff4b0")
IRON = pg.hexes("#1e1e24", "#3a3a44", "#5e5e6a", "#8a8a96")
LICHEN = pg.hexes("#4a5a30", "#6a7a40", "#8a9a52")


def _finger(c, knuckle, tip, bend, r):
    mid = ((knuckle[0] + tip[0]) / 2 + bend, (knuckle[1] + tip[1]) / 2)
    limb(c, knuckle, mid, r, r * 0.9, GRANITE)
    ball(c, mid[0], mid[1], r * 0.95, r * 0.9, GRANITE, bias=0.05)
    limb(c, mid, tip, r * 0.9, r * 0.75, GRANITE, bias=-0.03)
    c.paint(c.ellipse(tip[0], tip[1] - 1, r * 0.55, r * 0.4), GRANITE[4])


def _seal(c, mask, x, y, r, frame):
    """손등의 금빛 봉인 문양: 고리, 가운데 점, 사방으로 갈라진 금."""
    ring = c.ellipse(x, y, r, r * 0.7) & ~c.ellipse(x, y, r - 1.2, r * 0.7 - 1.0)
    c.paint(ring & mask, SEAL[2])
    c.paint(c.ellipse(x, y, 1.8, 1.4) & mask, SEAL[3])
    for k in range(5):
        a = k * 1.26 + 0.3
        end = (x + np.cos(a) * (r + 6), y + np.sin(a) * (r * 0.7 + 4))
        start = (x + np.cos(a) * r, y + np.sin(a) * r * 0.7)
        crack = limb(c, start, end, 0.6, 0.5, SEAL[1:3]) & mask
        c.paint(crack, SEAL[1 + (k + frame) % 2])


def titan_hand(view, frame):
    c = canvas(42, 38)
    cx = 42
    bob = 1 if frame else 0
    side_view = view == "right"
    lift = [(0, 0, 0, 0), (3, 0, 3, 0), (0, 3, 0, 3)][frame]
    if side_view:
        # 옆모습: 손목은 뒤(왼쪽)에, 손가락이 앞으로 굽어 땅을 짚는다.
        wrist = limb(c, (cx - 22, 30 - bob), (cx - 8, 34 - bob), 9, 11, GRANITE)
        chain(c, [(cx - 26, 30 - bob), (cx - 34, 36), (cx - 38, 44 + frame)], 1.4, 1.2, IRON)
        back = ball(c, cx + 2, 34 - bob, 16, 12, GRANITE)
        for k, (kx, tip_x) in enumerate(((cx + 12, cx + 22), (cx + 8, cx + 16), (cx + 4, cx + 10))):
            _finger(c, (kx, 40 - bob), (tip_x, 70 - lift[k]), 4, 4.4)
        _finger(c, (cx - 6, 42 - bob), (cx - 10, 68 - lift[3]), -3, 4.4)
        band = c.rect(cx - 30, 26 - bob, cx - 24, 40 - bob)
        shade(c, band, 0.5 - (c.yy - 26) * 0.01, IRON)
        speckle(c, back | wrist, LICHEN[1], 0.08, 191, (0, -bob))
        _seal(c, back, cx + 2, 30 - bob, 6, frame)
        return c
    # 앞·뒤: 손등이 위, 손가락 넷이 앞으로 굽어 땅을 짚는다. 엄지는 옆.
    wrist_y = 10 - bob
    wrist = limb(c, (cx, wrist_y), (cx, 26 - bob), 11, 13, GRANITE, bias=0.05)
    # 부서진 손목 끝과 족쇄
    for k in range(-4, 5):
        c.paint(c.polygon([(cx + k * 2.6 - 1.3, wrist_y), (cx + k * 2.6 + 1.3, wrist_y), (cx + k * 2.6, wrist_y - 2 - (k % 3))]), GRANITE[3])
    cuff = c.rect(cx - 12, 14 - bob, cx + 12, 20 - bob) & c.ellipse(cx, 17 - bob, 13, 8)
    shade(c, cuff, 0.6 - (c.xx - cx) / 60, IRON)
    for k in range(3):
        ball(c, cx + 13 + k * 3, 19 + k * 3 + frame * (k % 2), 1.6, 1.2, IRON, bias=0.1)
    if view == "up":
        # 뒤에서 보면 손목과 족쇄가 앞에 오고 손가락은 손등 너머로 조금만 보인다.
        for k, kx in enumerate((-15, -5, 5, 15)):
            _finger(c, (cx + kx, 40 - bob), (cx + kx * 1.1, 58 - lift[k]), 0, 3.6)
        back = ball(c, cx, 34 - bob, 20, 11, GRANITE, bias=-0.05)
        speckle(c, back, LICHEN[1], 0.1, 192, (0, -bob))
        wrist = limb(c, (cx, 30 - bob), (cx, 44 - bob), 11, 12, GRANITE, bias=0.1)
        cuff = c.rect(cx - 12, 36 - bob, cx + 12, 42 - bob) & c.ellipse(cx, 39 - bob, 13, 8)
        shade(c, cuff, 0.6 - (c.xx - cx) / 60, IRON)
        return c
    back = ball(c, cx, 34 - bob, 21, 13, GRANITE)
    speckle(c, back, LICHEN[1], 0.1, 193, (0, -bob))
    speckle(c, back & (c.yy < 30 - bob), LICHEN[2], 0.06, 194, (0, -bob))
    _seal(c, back, cx, 32 - bob, 7, frame)
    _finger(c, (cx - 20, 38 - bob), (cx - 28, 62 - lift[0]), -3, 4.2)
    for k, kx in enumerate((-12, -4, 4, 12)):
        ball(c, cx + kx, 43 - bob, 4.6, 3.6, GRANITE, bias=0.1)
        _finger(c, (cx + kx, 44 - bob), (cx + kx * 1.2, 72 - lift[k]), kx * 0.08, 4.4)
    return c


# ── 20 재앙의 왕관 ──────────────────────────────────────

CROWN = pg.hexes("#3a2608", "#6a4a10", "#a87a20", "#dcae3c", "#f6d870", "#fff4c0")
DUST = pg.hexes("#050408", "#0d0b13", "#17131f", "#221c30", "#332a48")
CALAMITY = pg.hexes("#3a0408", "#8a0c14", "#e0262a", "#ff8a6a", "#ffe0c0")
GEM = pg.hexes("#5a0612", "#b01828", "#ff6a70")


def _crown(c, x, y, tilt, broken):
    band = c.polygon([(x - 11, y + tilt), (x + 11, y - tilt), (x + 10, y + 5 - tilt), (x - 10, y + 5 + tilt)])
    shade(c, band, 0.7 - (c.yy - y) * 0.06, CROWN)
    for k, h in enumerate((7, 10, 13, 10, 7)):
        px = x - 9 + k * 4.5
        py = y + tilt * (1 - k / 2) * 0.5
        height = 3 if k == broken else h
        spike = c.polygon([(px - 2, py + 1), (px + 2, py + 1), (px + (0.6 if k == broken else 0), py - height)])
        shade(c, spike, 0.8 - (c.xx - px) * 0.08, CROWN)
        if k != broken:
            c.paint(c.ellipse(px, py - height + 0.6, 1, 1), CROWN[5])
    for k in (-1, 1):
        ball(c, x + k * 5, y + 2.5 + tilt * -k * 0.4, 1.4, 1.3, GEM, bias=0.15)
    ball(c, x, y + 2.5, 1.8, 1.6, GEM, bias=0.2)
    c.paint(band & (np.abs(c.xx + 0.5 - (x + 3)) < 0.6), DUST[1])


def calamity_crown(view, frame):
    c = canvas(38, 50)
    cx = 38 + (2 if view == "right" else 0)
    bob = (0, -1, 1)[frame]
    side_view = view == "right"
    # 아래로 흘러내리며 흩어지는 검은 더스트 자락
    for k in range(-3, 4):
        feather(c, (cx + k * 3, 54 + bob), (cx + k * 4.5 - (6 if side_view else 0) + (frame - 1) * (k % 2) * 2, 90 - abs(k) * 4 + bob), 7, DUST, bias=-0.1)
    for k in range(7):
        spark(c, cx - 15 + k * 5 + (frame - 1), 74 + (k * 5 + frame * 4) % 20 + bob, 0.6, CALAMITY[1] if k % 2 else DUST[4], CALAMITY[2])
    # 긴 팔과 금빛 발톱(뒤쪽 팔 먼저)
    reach = (0, 3, -3)[frame]
    for side in ((1,) if side_view else (-1, 1)):
        shoulder = (cx + side * (6 if side_view else 13), 42 + bob)
        elbow = (cx + side * (18 if side_view else 24), 50 + bob + reach * side * 0.5)
        hand = (cx + side * (22 if side_view else 27), 64 + bob + reach * side)
        chain(c, [shoulder, elbow, hand], 4.2, 2.6, DUST, bias=0.05)
        for claw in (-3, -1, 1, 3):
            tip = (hand[0] + claw * 1.3 + side * 1.5, hand[1] + 8 - abs(claw) * 0.7)
            limb(c, hand, tip, 1.1, 0.5, DUST)
            c.paint(c.ellipse(tip[0], tip[1] - 0.5, 0.9, 1.4), CROWN[3])
    body = blob(c, cx, 46 + bob, 16, DUST, wobble=1.6, lobes=7, phase=frame * 0.7, squash=1.1)
    speckle(c, body, DUST[4], 0.06, 201, (0, bob))
    hood = blob(c, cx + (2 if side_view else 0), 30 + bob, 12, DUST, wobble=1.0, lobes=6, phase=0.5 + frame * 0.4)
    # 부서진 금관이 머리 위에 떠 있다.
    _crown(c, cx + (2 if side_view else 0), 13 + bob - (frame % 2), 1 if side_view else 0, 3)
    if view == "up":
        return c
    face = c.ellipse(cx + (4 if side_view else 0), 31 + bob, 7 if side_view else 9, 6)
    c.paint(face & hood, VOID[0])
    eyes = (cx + 6,) if side_view else (cx - 4, cx + 4)
    for k, ex in enumerate(eyes):
        tilt = -1 if (k == 0 and not side_view) else 1
        glow_eye(c, ex, 30 + bob, 2.6, 1.6, CALAMITY[1:])
        c.paint(c.polygon([(ex - 3, 28 + bob - tilt), (ex + 3, 28 + bob + tilt), (ex + 3, 27 + bob + tilt), (ex - 3, 27 + bob - tilt)]), VOID[0])
    return c
