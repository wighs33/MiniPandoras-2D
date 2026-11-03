"""생물 도감 · 페어리가든(16·17레벨).

 16 꽃잎꼬리 (귀여움) 페어리가든 꽃밭. 다섯 장 꽃잎이 활짝 핀 꼬리의 여우꼴, 귀 끝도 꽃잎처럼 물들고 꽃가루가 날린다
 17 주머니풀 (사나움) 페어리가든 냇물가. 뿌리 다리로 걷는 벌레잡이 주머니(네펜데스 결), 뚜껑 밑에 붉은 눈점 줄,
                    주머니 입 둘레에 안쪽으로 굽은 이빨, 흘러내리는 꿀물, 덩굴손이 감긴 잎 팔
"""
import numpy as np

import pixelgen as pg
from creature_kit import quadruped, small_face
from monster_kit import GLINT, ball, canvas, chain, feather, light_of, limb, shade, spark, tilted

# ── 16 꽃잎꼬리 ────────────────────────────────────────

FOX = pg.hexes("#6a3e2e", "#8e5844", "#b07a60", "#cc9c80", "#e2bea4")
PETAL = pg.hexes("#7a3a5a", "#a85a7e", "#cc84a2", "#e6b0c4", "#f6d8e2")
POLLEN = pg.hexes("#c8a030", "#f0d870")
PALE = pg.hexes("#c8b0a0", "#e6d4c6")


def _flower_tail(c, x, y, frame, back):
    """꽃잎 다섯 장이 부채처럼 핀 꼬리, 가운데 꽃술."""
    for k in range(5):
        a = (-np.pi * 0.85 + k * np.pi * 0.42) if not back else (-np.pi / 2 + (k - 2) * 0.62)
        a += (frame - 1) * 0.06
        feather(c, (x, y), (x + np.cos(a) * 11, y + np.sin(a) * 10), 7.5, PETAL, bias=0.05)
    ball(c, x, y, 2.2, 2.2, POLLEN, bias=0.1)


def petal_tail(view, frame):
    c = canvas(30, 28, fine=True)
    cx = 30 + (-2 if view == "right" else 0)

    def tail(c, x, y):
        tx, ty = (x - 6, y - 4) if view == "right" else (x, y - 6)
        if view == "up":
            tx, ty = x, y + 4
        chain(c, [(x, y), (tx, ty)], 1.8, 1.4, FOX)
        _flower_tail(c, tx, ty, frame, view == "up")
        for k in range(2):
            spark(c, tx + (k * 2 - 1) * 8, ty - 9 - frame * 1.5 + k * 3, 0.7, POLLEN[0], POLLEN[1])

    def head(c, x, y):
        hx, hy = (x + 2, y - 1) if view == "right" else (x, y - 1)
        for side in ((-1, 1) if view != "right" else (-0.4, 1)):
            base = (hx + side * 3, hy - 3)
            tip = (hx + side * 5.5 - (1 if view == "right" else 0), hy - 10)
            ear = feather(c, base, tip, 4.6, FOX, bias=0.05)
            c.paint(ear & (c.yy + 0.5 < tip[1] + 3.5), PETAL[2])
        if view == "up":
            ball(c, hx, hy, 5, 4.4, FOX, bias=-0.05)
            return
        ball(c, hx, hy, 5.4, 4.8, FOX, bias=0.05)
        mx = hx + (2.6 if view == "right" else 0)
        ball(c, mx, hy + 2.2, 3.0, 1.9, PALE, bias=0.1)
        c.paint(c.ellipse(mx + (1.2 if view == "right" else 0), hy + 1.2, 0.8, 0.6), FOX[0])
        small_face(c, hx + (1 if view == "right" else 0), hy - 0.6, view, gap=2.4, eye=(0.75, 1.1), mouth="w")

    if view == "up":
        quadruped(c, view, frame, cx, 52, (7, 5.5, 11), (1.6, 0.55), FOX, head, tail)
    else:
        quadruped(c, view, frame, cx, 52, (9 if view == "right" else 7, 5.5, 11), (1.6, 0.55), FOX, head, tail)
    return c


# ── 17 주머니풀 ────────────────────────────────────────

PITCHER = pg.hexes("#1e3a1c", "#2e5428", "#467038", "#628c48", "#80a85a")
FLUSH = pg.hexes("#5a1420", "#8a2430", "#b04044")
RIM = pg.hexes("#5a1018", "#8a2028", "#b8404a", "#dc6a6e")
THROAT = np.array([22, 10, 12], dtype=np.uint8)
FANG = pg.hexes("#c8c0a8", "#ece6d2")
NECTAR = pg.hexes("#8a9a2a", "#c8d45a")
EYESPOT = pg.hexes("#5a0a0a", "#d02a2a", "#ff9a7a")
ROOT = pg.hexes("#2a1e14", "#44321e", "#5e4a2e")


def pitcher_fang(view, frame):
    c = canvas(26, 34, fine=True)
    cx = 26 + (1 if view == "right" else 0)
    ground = 66
    sway = (0, -1, 1)[frame]
    # 뿌리 다리 넷: 번갈아 땅을 짚는다.
    for k, side in enumerate((-1.4, -0.5, 0.5, 1.4)):
        up = 2 if frame and (k % 2 == frame - 1) else 0
        hip = (cx + side * 4, ground - 12)
        foot = (cx + side * 9, ground - 1 - up)
        chain(c, [hip, ((hip[0] + foot[0]) / 2 + side, hip[1] + 5), foot], 1.8, 1.0, ROOT)
    # 주머니 몸: 아래가 불룩한 항아리꼴, 위로 갈수록 붉게 물들고 붉은 핏줄이 세로로 흐른다.
    by = ground - 26 + sway * 0.3
    y = c.yy + 0.5
    t = np.clip((y - (by - 16)) / 30, 0, 1)
    half = 7.5 + 3.5 * np.sin(t * np.pi * 0.85)
    nx = (c.xx + 0.5 - cx) / half
    body = (np.abs(nx) <= 1) & (y >= by - 14) & (y <= ground - 10)
    body |= c.ellipse(cx, ground - 12, 10, 4)
    light = light_of(nx, 0, np.sqrt(np.clip(1 - nx ** 2, 0, 1)))
    shade(c, body, light, PITCHER)
    c.paint(body & (y < by - 3) & (np.mod(np.floor(c.xx * 0.9), 2) == 0), FLUSH[1])
    for k in range(-2, 3):
        vx = cx + k * 3.2
        c.paint(body & (np.abs(c.xx + 0.5 - vx - np.sin(y * 0.4 + k) * 0.6) < 0.45), FLUSH[0])
    # 잎 팔과 덩굴손
    for side in ((1,) if view == "right" else (-1, 1)):
        leaf, u, v = tilted(c, cx + side * 12, by + 4 + sway * side, 6.5, 2.4, side * 0.7)
        shade(c, leaf, 0.7 - v * 0.2, PITCHER)
        c.paint(leaf & (np.abs(v) < 0.15), PITCHER[0])
        end = (cx + side * 17, by + 9 + sway * side)
        chain(c, [end, (end[0] + side * 2, end[1] + 2), (end[0] + side * 1, end[1] + 4), (end[0] - side * 0.5, end[1] + 3)], 0.5, 0.4, PITCHER[1:3])
    # 주머니 입: 이빨 돋은 붉은 테, 속은 어둡고 꿀물이 흘러내린다.
    mouth_y = by - 14
    if view != "up":
        c.paint(c.ellipse(cx + (1 if view == "right" else 0), mouth_y, 8.5, 3.4), THROAT)
        rim = c.ellipse(cx, mouth_y, 10, 4.2) & ~c.ellipse(cx + (1 if view == "right" else 0), mouth_y + 0.3, 8.5, 3.0)
        shade(c, rim, 0.75 - (c.yy - mouth_y) * 0.08, RIM)
        c.paint(rim & (np.mod(np.floor(np.arctan2(c.yy + 0.5 - mouth_y, c.xx + 0.5 - cx) * 6), 2) == 0), RIM[0])
        for k in range(7):
            a = np.pi * (0.1 + k * 0.13)
            fx, fy = cx + np.cos(a) * 8, mouth_y + np.sin(a) * 2.8
            c.paint(c.polygon([(fx - 0.8, fy), (fx + 0.8, fy), (fx - np.cos(a) * 1.8, fy - 1.8)]), FANG[1])
        drip = (0, 1.5, 0.5)[frame]
        for dx, length in ((-3, 4 + drip), (4, 2.5 + drip * 0.6)):
            chain(c, [(cx + dx, mouth_y + 3), (cx + dx, mouth_y + 3 + length)], 0.7, 0.6, NECTAR)
            ball(c, cx + dx, mouth_y + 3.5 + length, 0.9, 1.1, NECTAR, bias=0.1)
    # 뒤로 젖혀진 뚜껑: 그 밑에 붉은 눈점이 줄지어 빛난다.
    lid_y = mouth_y - 6.5 - (0, 1, 0.5)[frame]
    lid, u, v = tilted(c, cx - (2 if view == "right" else 0), lid_y, 9.5, 4.6, -0.12 if view != "up" else 0.0)
    shade(c, lid, 0.72 - v * 0.25, PITCHER)
    c.paint(lid & (np.abs(v) < 0.12), FLUSH[0])
    limb(c, (cx - (5 if view == "right" else 0), lid_y + 3), (cx - (6 if view == "right" else 0), mouth_y - 1), 1.0, 1.2, PITCHER)
    if view != "up":
        for k in range(-2, 3):
            ex = cx + k * 3 + (1.5 if view == "right" else 0)
            ey = lid_y + 3.2 + abs(k) * 0.3
            c.paint(c.ellipse(ex, ey, 0.9, 0.7), EYESPOT[1])
            c.paint(c.ellipse(ex, ey, 0.45, 0.35), EYESPOT[2])
    return c
