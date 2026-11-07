"""던전 보스. 레비아탄은 힐리아 동부와 서부 사이의 바다에 사는 괴물이다.

바다뱀의 몸이 물을 끌고 다닌다. 뒤로 똬리 두 덩이가 솟고, 긴 목 끝의 머리에는 지느러미 뿔과 작은 눈이 있다.
"""
import pixelgen as pg
from monster_kit import ball, canvas, limb, shade

SKIN = pg.hexes("#0a1a2a", "#103048", "#184a64", "#256a82", "#3f96a2")
BELLY = pg.hexes("#3e6a62", "#5f9080", "#8ab8a0", "#b6d8bc")
FIN = pg.hexes("#3a1c3e", "#6a2a58", "#9c3c6c")
WATER = pg.hexes("#123a52", "#1e5a74", "#8fd0dc", "#e4f6f4")
FANG = pg.hexes("#d8d4bc")[0]
EYE = pg.hexes("#ffd060")[0]
INK = pg.hexes("#061018")[0]


def wake(c, cx, ground, frame):
    c.paint(c.ellipse(cx, ground - 7, 88, 12), WATER[0])
    c.paint(c.ellipse(cx, ground - 8, 80, 9), WATER[1])
    for k in range(6):
        x = cx - 72 + k * 29 + (frame * 7) % 12
        c.paint(c.rect(x, ground - 3 - (k % 2) * 13, x + 12, ground - 2 - (k % 2) * 13), WATER[2])


def hump(c, x, y, rx, ry):
    body = ball(c, x, y, rx, ry, SKIN)
    # 물에 잠긴 아래쪽은 어둡다.
    shade(c, body & (c.yy > y + ry * 0.45), 0.25, SKIN)
    for k in range(-1, 2):
        c.paint(c.polygon([(x + k * rx * 0.55 - 6, y - ry * 0.86), (x + k * rx * 0.62, y - ry - 12), (x + k * rx * 0.55 + 6, y - ry * 0.8)]), FIN[1])


def neck(c, points, r0, r1):
    last = len(points) - 2
    for k in range(last + 1):
        t0, t1 = k / (last + 1), (k + 1) / (last + 1)
        limb(c, points[k], points[k + 1], r0 + (r1 - r0) * t0, r0 + (r1 - r0) * t1, SKIN)


def belly(c, points, r0, r1):
    last = len(points) - 2
    for k in range(last + 1):
        t0, t1 = k / (last + 1), (k + 1) / (last + 1)
        limb(c, points[k], points[k + 1], (r0 + (r1 - r0) * t0) * 0.55, (r0 + (r1 - r0) * t1) * 0.55, BELLY)


def crest(c, points, side):
    """목을 따라 등지느러미가 돋는다."""
    for k in range(len(points) - 1):
        x0, y0 = points[k]
        x1, y1 = points[k + 1]
        tip = ((x0 + x1) / 2 + side * 20, (y0 + y1) / 2 - 8)
        c.paint(c.polygon([(x0 + side * 9, y0), tip, (x1 + side * 8, y1)]), FIN[1 + k % 2])


def fins(c, x, y, spread):
    for side in (-1, 1):
        c.paint(c.polygon([(x + side * spread, y), (x + side * (spread + 26), y - 22), (x + side * (spread + 16), y + 2)]), FIN[1])
        c.paint(c.polygon([(x + side * spread, y + 6), (x + side * (spread + 22), y + 6), (x + side * (spread + 8), y + 14)]), FIN[2])


def leviathan(view, frame):
    c = canvas(96, 84)
    cx, ground = 96, 164
    sway = (0, 3, -3)[frame]
    wake(c, cx, ground, frame)
    if view == "right":
        hump(c, cx - 58, ground - 26 + sway, 26, 24)
        hump(c, cx - 18, ground - 18 - sway, 22, 18)
        path = [(cx + 18, ground - 8), (cx + 30, ground - 42), (cx + 12 + sway, ground - 78), (cx + 22 + sway, ground - 112), (cx + 34 + sway, ground - 128)]
        crest(c, path[1:], -1)
        neck(c, path, 22, 12)
        belly(c, [(x + 7, y) for x, y in path], 22, 12)
        hx, hy = cx + 44 + sway, ground - 132
        c.paint(c.polygon([(hx - 20, hy - 6), (hx - 42, hy - 26), (hx - 34, hy + 2)]), FIN[1])
        ball(c, hx, hy, 26, 15, SKIN)
        ball(c, hx + 20, hy + 5, 17, 8, SKIN)
        jaw = ball(c, hx + 10, hy + 14, 22, 6, BELLY)
        shade(c, jaw, 0.4, BELLY)
        c.paint(c.rect(hx + 2, hy + 8, hx + 36, hy + 10), INK)
        for k in range(5):
            x = hx + 8 + k * 6
            c.paint(c.polygon([(x, hy + 9), (x + 4, hy + 9), (x + 2, hy + 14)]), FANG)
        c.paint(c.ellipse(hx + 6, hy - 4, 2.4, 1.8), EYE)
        return c

    hump(c, cx - 56, ground - 26 - sway, 26, 24)
    hump(c, cx + 56, ground - 24 + sway, 24, 22)
    path = [(cx, ground - 6), (cx + 16, ground - 42), (cx - 10 + sway, ground - 78), (cx + 6 + sway, ground - 112), (cx + sway, ground - 124)]
    if view == "up":
        neck(c, path, 24, 13)
        crest(c, path, 0)
        hx, hy = cx + sway, ground - 130
        fins(c, hx, hy - 4, 16)
        ball(c, hx, hy, 22, 18, SKIN)
        return c

    neck(c, path, 24, 13)
    belly(c, path, 24, 13)
    hx, hy = cx + sway, ground - 130
    fins(c, hx, hy - 4, 16)
    ball(c, hx, hy, 23, 17, SKIN)
    ball(c, hx, hy + 11, 16, 9, SKIN)
    jaw = ball(c, hx, hy + 18, 15, 6, BELLY)
    shade(c, jaw, 0.4, BELLY)
    c.paint(c.rect(hx - 13, hy + 13, hx + 13, hy + 15), INK)
    for x in range(-11, 12, 5):
        c.paint(c.polygon([(hx + x - 2, hy + 14), (hx + x + 2, hy + 14), (hx + x, hy + 19)]), FANG)
    for side in (-1, 1):
        c.paint(c.ellipse(hx + side * 11, hy - 3, 2.4, 1.8), EYE)
        c.paint(c.ellipse(hx + side * 4, hy + 8, 1.2, 1.0), INK)
    return c
