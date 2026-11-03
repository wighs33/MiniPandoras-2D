"""생물 도감 · 숲과 거대고목(4·5·18·19레벨).

  4 이끼갓     (귀여움) 마을 끝 숲길. 이끼가 덮인 갈색 갓의 버섯 생물, 갓 위에 고사리 순, 갓 아래 대에 작은 얼굴
  5 옹이뿔     (사나움) 숲길. 나무 옹이가 맺힌 굽은 뿔의 장수풍뎅이, 반들반들한 검갈색 등딱지, 호박빛 작은 눈과 큰 턱
 18 잎망토     (귀여움) 거대고목 가지. 큰 잎을 망토처럼 펴고 가지 사이를 활공하는 날다람쥐꼴, 도토리 깍정이 투구
 19 뿌리문어   (사나움) 고목의 뿌리. 나무껍질 살갗의 문어꼴, 머리에 보랏빛 버섯 갓, 뿌리 촉수로 검은 웅덩이를 긴다
"""
import numpy as np

import pixelgen as pg
from creature_kit import small_face
from monster_kit import HOP, ball, blob, canvas, chain, light_of, limb, shade, speckle, spark, tilted

# ── 4 이끼갓 ───────────────────────────────────────────

CAP = pg.hexes("#3a2414", "#58381e", "#7a5030", "#9a6c44", "#b88a5c")
MOSS = pg.hexes("#22401c", "#335a26", "#4a7632", "#66943e")
STEM = pg.hexes("#7a6a54", "#a09078", "#c2b498", "#dcd0b6")
FERN = pg.hexes("#2a5a26", "#428036", "#62a04a")
GILL = pg.hexes("#5a4632", "#7a644a")


def moss_cap(view, frame):
    c = canvas(22, 24, fine=True)
    sx, sy, lift = HOP[frame]
    cx = 22 + (1 if view == "right" else 0)
    ground = 46
    for side in (-1, 1):
        ball(c, cx + side * (5 if view != "right" else 3.5), ground - 1.5 - lift, 3.0, 1.9, STEM[:3], bias=-0.1)
    bottom = ground - 2 - lift
    top = bottom - 17 * sy
    stem = limb(c, (cx, top + 4), (cx, bottom - 4), 7 * sx, 7.6 * sx, STEM)
    rx, ry = 13.5 * sx, 9.5 * sy
    cap_y = top + 1
    rim = cap_y + ry * 0.38
    c.paint(c.ellipse(cx, rim, rx * 0.8, 1.8) & (c.yy + 0.5 >= rim), GILL[0])
    cap = ball(c, cx, cap_y, rx, ry, CAP, clip=c.yy + 0.5 < rim + 0.5)
    # 갓을 덮은 이끼 덩이(윗면 위주)와 고사리 순
    moss = np.zeros(c.xx.shape, dtype=bool)
    for k, (dx, dy, r) in enumerate(((-6, -5, 3.6), (0, -7.5, 4.2), (6, -4.5, 3.4), (-9, -1, 2.4), (9.5, -0.5, 2.2))):
        moss |= blob(c, cx + dx * sx, cap_y + dy * sy, r, MOSS, wobble=0.5, lobes=6, phase=k) & cap
    speckle(c, moss, MOSS[3], 0.15, 41, (0, -lift))
    sway = (0, 1, -1)[frame]
    curl = [(cx + 2, cap_y - ry + 1), (cx + 3 + sway, cap_y - ry - 4), (cx + 1 + sway, cap_y - ry - 7), (cx - 1 + sway, cap_y - ry - 6)]
    chain(c, curl, 0.8, 0.6, FERN)
    for k in range(3):
        leaf, u, v = tilted(c, cx + 3.5 + sway * 0.6 + k * 0.4, cap_y - ry - 2 - k * 1.6, 1.8, 0.8, -0.3)
        shade(c, leaf, 0.7, FERN)
    small_face(c, cx + (1 if view == "right" else 0), rim + 4.5, view, gap=3.2, eye=(0.9, 1.4), mouth="w")
    return c


# ── 5 옹이뿔 ───────────────────────────────────────────

SHELL = pg.hexes("#160e0a", "#281a12", "#40291c", "#5c3c28", "#7c5638", "#a07a56")
HORN = pg.hexes("#2e2014", "#4a3420", "#6a4c30", "#8a6a46", "#a8885e")
LEG = pg.hexes("#140e0a", "#2a1e16", "#42301e")
AMBER = pg.hexes("#5a3008", "#c07a18", "#ffd060")


def _knot(c, x, y, r):
    """뿔에 맺힌 나무 옹이: 어두운 테와 가운데 홈."""
    c.paint(c.ellipse(x, y, r, r * 0.8) & ~c.ellipse(x, y, r * 0.6, r * 0.45), HORN[0])
    c.paint(c.ellipse(x, y, r * 0.35, r * 0.28), HORN[1])


def _legs(c, hips, frame, ground, far):
    """곤충 다리 셋: 무릎이 높이 굽은 두 마디. 세 다리씩 번갈아 내딛는다."""
    for k, (hx, hy, side) in enumerate(hips):
        swing = (0, 2.2, -2.2)[frame] * (1 if k % 2 == 0 else -1)
        knee = (hx + side * 4, hy - 2.5)
        foot = (hx + side * 6.5 + swing, ground - 1)
        palette = LEG[:2] if far else LEG
        limb(c, (hx, hy), knee, 1.1, 0.9, palette)
        limb(c, knee, foot, 0.9, 0.6, palette)


def knot_horn(view, frame):
    c = canvas(28, 26, fine=True)
    bob = 0.5 if frame else 0
    if view == "right":
        cx, ground = 26, 50
        _legs(c, [(cx - 8, 38, 1), (cx - 1, 39, 1), (cx + 6, 38, 1)], frame, ground, True)
        shell = ball(c, cx - 3, 34 - bob, 14, 8.5, SHELL)
        c.paint(shell & (np.abs(c.yy + 0.5 - (34 - bob) + (c.xx - cx) * 0.05) < 0.5) & (c.xx + 0.5 < cx + 6), SHELL[0])
        c.paint(c.ellipse(cx - 7, 29 - bob, 4, 1.6) & shell, SHELL[5])
        thorax = ball(c, cx + 9, 32 - bob, 6.5, 6, SHELL, bias=0.05)
        ball(c, cx + 15, 35 - bob, 3.8, 3.4, SHELL, bias=0.05)
        horn = [(cx + 10, 27 - bob), (cx + 14, 19 - bob), (cx + 19, 13 - bob), (cx + 23, 12 - bob)]
        chain(c, horn, 3.0, 1.2, HORN)
        _knot(c, cx + 13, 21 - bob, 1.6)
        limb(c, (cx + 22, 12.5 - bob), (cx + 24, 9 - bob), 0.9, 0.5, HORN)
        for jaw in (-1, 1):
            limb(c, (cx + 17, 36 - bob), (cx + 20, 37.5 - bob + jaw), 0.8, 0.5, LEG)
        _legs(c, [(cx - 6, 39, 1), (cx + 1, 40, 1), (cx + 8, 39, 1)], frame, ground, False)
        c.paint(c.ellipse(cx + 16.2, 33.8 - bob, 0.9, 0.8), AMBER[2])
        return c
    cx, ground = 28, 50
    down = view == "down"
    for side in (-1, 1):
        _legs(c, [(cx + side * 6, 36 - bob, side), (cx + side * 7, 39 - bob, side), (cx + side * 6, 42 - bob, side)], frame, ground, False)
    shell_y = 30 - bob if down else 36 - bob
    shell = ball(c, cx, shell_y, 12, 9.5, SHELL)
    c.paint(shell & (np.abs(c.xx + 0.5 - cx) < 0.5), SHELL[0])
    c.paint(c.ellipse(cx - 5, shell_y - 4, 3, 1.8) & shell, SHELL[5])
    if down:
        ball(c, cx, 38 - bob, 7.5, 5.5, SHELL, bias=0.05)
        head = ball(c, cx, 42.5 - bob, 4.5, 3.5, SHELL, bias=0.05)
        for jaw in (-1, 1):
            chain(c, [(cx + jaw * 2.5, 45 - bob), (cx + jaw * 3.5, 47 - bob), (cx + jaw * 1.8, 48 - bob)], 0.8, 0.5, LEG)
        chain(c, [(cx, 36 - bob), (cx + 1, 26 - bob), (cx, 16 - bob), (cx - 2, 12 - bob)], 3.2, 1.3, HORN)
        _knot(c, cx + 0.8, 27 - bob, 1.6)
        limb(c, (cx - 1.5, 12.5 - bob), (cx - 4, 10 - bob), 0.9, 0.5, HORN)
        for side in (-1, 1):
            c.paint(c.ellipse(cx + side * 3, 41.5 - bob, 0.9, 0.8), AMBER[2])
    else:
        ball(c, cx, 26 - bob, 6.5, 4.5, SHELL, bias=0.05)
        chain(c, [(cx, 24 - bob), (cx - 1, 16 - bob), (cx - 3, 11 - bob)], 2.6, 1.1, HORN)
    return c


# ── 18 잎망토 ──────────────────────────────────────────

GLIDER = pg.hexes("#3e3630", "#5c5248", "#7c7064", "#9c9082", "#b8ae9e")
BELLY = pg.hexes("#a89c88", "#c8bea8")
CAPE = pg.hexes("#1e4420", "#2e622c", "#44803a", "#5e9c4a", "#7cb85c")
ACORN = pg.hexes("#3a2a18", "#5a4228", "#7a5c38", "#9a7a50")


def _canopy(c, x, y, span, droop, frame, tilt=0.0):
    """머리 위로 펼쳐 든 큰 잎: 양끝이 뾰족한 잎이 우산처럼 아래로 휘고, 가운데 잎맥과 갈래 잎맥, 가장자리가 바람에 흔들린다."""
    flutter = (0.0, 0.8, -0.8)[frame]
    dx = c.xx + 0.5 - x
    t = np.clip(np.abs(dx) / span, 0, 1)
    top = y - droop * (1 - t ** 2) + dx * tilt
    thickness = 3.4 * np.sqrt(np.clip(1 - t ** 1.6, 0, 1)) + 0.4
    leaf = (np.abs(dx) <= span) & (c.yy + 0.5 >= top - thickness * 0.4) & (c.yy + 0.5 <= top + thickness + flutter * t)
    shade(c, leaf, 0.82 - (c.yy + 0.5 - top) / 8 - t * 0.15, CAPE)
    c.paint(leaf & (np.abs(dx) < 0.5), CAPE[0])
    for k in (-3, -2, -1, 1, 2, 3):
        vx = x + k * span * 0.25
        c.paint(leaf & (np.abs(c.xx + 0.5 - vx - (c.yy + 0.5 - top) * np.sign(k) * 0.8) < 0.4), CAPE[1])
    return leaf


def leaf_glider(view, frame):
    c = canvas(32, 28, fine=True)
    bob = (0, -1, 1)[frame]
    cx = 32 + (1 if view == "right" else 0)
    side_view = view == "right"
    # 머리 위로 든 큰 잎(옆에서는 앞으로 기울어진다)
    stem_top = (cx + (3 if side_view else 0), 12 + bob)
    _canopy(c, stem_top[0], stem_top[1], 13 if side_view else 22, 4 if side_view else 6, frame, tilt=0.12 if side_view else 0.0)
    hx, hy = cx + (2 if side_view else 0), 30 + bob
    # 잎자루를 두 앞발로 쥐고 매달려 있다.
    limb(c, stem_top, (hx, hy - 6), 0.6, 0.6, CAPE[:2])
    # 위로 말린 복슬복슬한 꼬리(앞에서는 몸 뒤, 뒤에서는 가장 앞)
    def tail():
        tx = hx - (8 if side_view else 6)
        chain(c, [(hx - 2, hy + 6), (tx, hy + 4), (tx - 2, hy - 2), (tx + 1, hy - 6)], 3.0, 3.6, GLIDER, bias=0.05)
    if view != "up":
        tail()
    for side in (-1, 1):
        limb(c, (hx + side * 2.5, hy + 4), (hx + side * 3, hy + 9 + (frame - 1) * side * 0.5), 1.2, 1.0, GLIDER)
    ball(c, hx, hy + 3, 4.8, 5.5, GLIDER)
    if view == "down":
        ball(c, hx, hy + 4, 3.0, 3.8, BELLY, bias=0.05)
    for side in (-1, 1):
        limb(c, (hx + side * 3, hy), (hx + side * 1, hy - 6), 1.0, 0.8, GLIDER)
    if view == "up":
        tail()
    head = ball(c, hx, hy - 3, 5.2, 4.8, GLIDER, bias=0.05)
    for side in ((-1, 1) if not side_view else (-0.5, 1)):
        ball(c, hx + side * 4.2, hy - 6.5, 1.5, 1.9, GLIDER, bias=0.05)
    cup = c.ellipse(hx, hy - 6, 5.6, 3.4) & (c.yy + 0.5 < hy - 4.6)
    shade(c, cup, 0.7 - (c.xx - hx) / 30, ACORN)
    c.paint(cup & ((np.mod(np.floor(c.xx + c.yy), 3) == 0) | (np.mod(np.floor(c.xx - c.yy), 3) == 0)), ACORN[0])
    small_face(c, hx + (1 if side_view else 0), hy - 2.2, view, gap=2.5, eye=(0.8, 1.2), mouth="w")
    return c


# ── 19 뿌리문어 ────────────────────────────────────────

BARKSKIN = pg.hexes("#160e12", "#2a1c20", "#3e2a2c", "#563c3a", "#70524a")
VIOLET_CAP = pg.hexes("#2a0e40", "#4a1e6a", "#7034a0", "#9a5ad0", "#c48cf0")
CURSE = pg.hexes("#3a1060", "#8a48d8", "#e0b8ff")
ROOTLET = pg.hexes("#1e1410", "#3a2a20")


def root_octo(view, frame):
    c = canvas(30, 26, fine=True)
    cx = 30 + (1 if view == "right" else 0)
    sway = frame * 0.8
    lean = 4 if view == "right" else 0
    # 뿌리 촉수 여섯: 바닥을 짚고 끝이 말린다.
    for k in range(6):
        a = np.pi * (0.05 + k * 0.18) if view != "right" else np.pi * (0.1 + k * 0.13)
        base = (cx + np.cos(a) * 6 - lean * 0.3, 30 + np.sin(a) * 2)
        points = [base]
        for j in range(1, 5):
            t = j / 4
            reach = 6 + j * 3.2
            curl = np.sin(t * np.pi) * 2.5 * (1 if np.cos(a) >= 0 else -1)
            points.append((cx + np.cos(a) * reach * (1.0 if view != "right" else 1.3) - lean * t + curl + np.sin(j + k + sway) * 0.8, 31 + t * 15 - np.sin(t * np.pi) * 2))
        chain(c, points, 2.6, 0.9, BARKSKIN, bias=-0.05)
        tip = points[-1]
        c.paint(c.ellipse(tip[0] + 1, tip[1] - 1, 1.4, 1.4) & ~c.ellipse(tip[0] + 1.6, tip[1] - 1.6, 1.0, 1.0), BARKSKIN[2])
        limb(c, points[2], (points[2][0] + 1.5, points[2][1] + 3), 0.4, 0.3, ROOTLET)
    mantle = ball(c, cx - lean * 0.3, 22, 10, 10.5, BARKSKIN)
    c.paint(mantle & (np.mod(np.floor(c.xx * 0.7 + np.sin(c.yy * 0.4) * 1.5), 3) == 0), BARKSKIN[1])
    for k, (x0, y0, x1, y1) in enumerate(((-6, 16, -3, 26), (4, 15, 6, 25), (-1, 13, 0, 22))):
        limb(c, (cx + x0, y0), (cx + x1, y1 + (frame - 1) * 0.3), 0.45, 0.35, CURSE[:2])
    # 보랏빛 버섯 갓(빛 점이 깜빡인다)
    cap = ball(c, cx - lean * 0.2, 12.5, 9.5, 5.5, VIOLET_CAP, clip=c.yy + 0.5 < 14.5)
    for k, (dx, dy) in enumerate(((-4, -2), (2, -3.5), (5, -0.5), (-1, 0))):
        if (k + frame) % 3 != 2:
            c.paint(c.ellipse(cx + dx, 12 + dy, 0.8, 0.8) & cap, CURSE[2])
    for k in range(2):
        spark(c, cx + (k * 2 - 1) * 13, 8 + k * 3 + frame, 0.8, CURSE[1], CURSE[2])
    small_face(c, cx + (1.5 if view == "right" else 0), 24, view, gap=3.8, eye=(1.6, 1.3), mouth="none", glow=CURSE, squint=0.25)
    return c
