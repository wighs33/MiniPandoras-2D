"""생물 도감 · 서부 사막과 오아시스(13·14레벨).

 13 모래돛     (귀여움) 서부 사막. 돛처럼 큰 귀(살이 비치는 얇은 막)로 바람을 받으며 모래 속을 헤엄치는 사막여우꼴
 14 가시꽃도마 (사나움) 오아시스. 등 가시마다 분홍 선인장 꽃이 핀 땅딸막한 도마뱀, 뿔 왕관과 가늘게 뜬 호박빛 눈
"""
import numpy as np

import pixelgen as pg
from creature_kit import quadruped, small_face
from monster_kit import ball, canvas, chain, feather, limb, shade, speckle, tilted

AMBER = pg.hexes("#5a2c06", "#c87a1c", "#ffd878")

# ── 13 모래돛 ──────────────────────────────────────────

SAND_FUR = pg.hexes("#6a4a26", "#8e6a3a", "#b08a4e", "#cca86a", "#e0c48a")
SAIL = pg.hexes("#7a4a2a", "#a0663a", "#c48a52", "#e0ae76")
PALE = pg.hexes("#c8b494", "#e2d2b4")
TIP = pg.hexes("#3a2616", "#5a3e24")


def _sail_ear(c, base, tip, width, frame):
    """돛 귀: 얇은 막에 살 셋이 부채처럼 뻗는다."""
    ear = feather(c, base, tip, width, SAIL, bias=0.05)
    for k in (-0.25, 0.0, 0.25):
        end = (tip[0] + (base[1] - tip[1]) * k * 0.4, tip[1] + (tip[0] - base[0]) * k * 0.4)
        c.paint(limb(c, base, ((base[0] + end[0]) / 2, (base[1] + end[1]) / 2), 0.35, 0.3, SAIL[:1]) & ear, SAIL[0])
    return ear


def sand_sail(view, frame):
    c = canvas(28, 28, fine=True)
    cx = 28 + (-2 if view == "right" else 0)

    def tail(c, x, y):
        tip = (x - 8, y - 3 + (frame - 1)) if view == "right" else (x + 5, y + 6)
        chain(c, [(x, y), ((x + tip[0]) / 2, (y + tip[1]) / 2 - 1.5), tip], 2.2, 2.6, SAND_FUR, bias=0.05)
        ball(c, tip[0], tip[1], 2.2, 2.0, TIP, bias=0.1)

    def head(c, x, y):
        hx, hy = (x + 2, y - 1) if view == "right" else (x, y - 1)
        flutter = (0, 1.5, -1)[frame]
        for side in ((-1, 1) if view != "right" else (-0.4, 1)):
            base = (hx + side * 3.2, hy - 3)
            tip = (hx + side * 9 - (2 if view == "right" else 0), hy - 15 + flutter * (1 if side > 0 else -1) * 0.5)
            _sail_ear(c, base, tip, 6.4, frame)
        if view == "up":
            ball(c, hx, hy, 5, 4.4, SAND_FUR, bias=-0.05)
            return
        ball(c, hx, hy, 5.2, 4.6, SAND_FUR, bias=0.05)
        mx = hx + (2.5 if view == "right" else 0)
        ball(c, mx, hy + 2.2, 3.0, 1.9, PALE, bias=0.1)
        c.paint(c.ellipse(mx + (1.2 if view == "right" else 0), hy + 1.3, 0.8, 0.6), TIP[0])
        small_face(c, hx + (1 if view == "right" else 0), hy - 0.6, view, gap=2.4, eye=(0.75, 1.1), mouth="w")

    quadruped(c, view, frame, cx, 52, (8.5 if view == "right" else 7, 5.5, 11), (1.6, 0.55), SAND_FUR, head, tail)
    return c


# ── 14 가시꽃도마 ───────────────────────────────────────

SCALE = pg.hexes("#3a3218", "#5a4c26", "#7a6a36", "#9a8a4a", "#b8a868")
CACTUS = pg.hexes("#2a4a2a", "#3e663a", "#5a844c")
BLOOM = pg.hexes("#8a2a52", "#c44a7a", "#ec84aa")


def _spikes(c, x, y, rx, ry, rows):
    """등의 선인장 가시: 원뿔마다 초록 바탕에 끝이 누렇고, 큰 가시 몇에는 분홍 꽃이 핀다."""
    for row in range(rows):
        t = row / max(rows - 1, 1)
        py = y - ry * (0.2 + t * 0.6)
        count = 4 - row // 2
        for k in range(count):
            px = x - rx * 0.7 + (k + 0.5 + (row % 2) * 0.5) * (rx * 1.4 / count)
            h = 4.5 - t * 1.2
            cone = c.polygon([(px - 1.6, py + 1), (px + 1.6, py + 1), (px, py - h)])
            c.paint(cone, CACTUS[1])
            c.paint(cone & (c.xx + 0.5 < px), CACTUS[2])
            c.paint(c.ellipse(px, py - h + 0.6, 0.6, 0.6), SCALE[4])
            if (row + k) % 3 == 0:
                for j in range(5):
                    a = j * 2 * np.pi / 5
                    c.paint(c.ellipse(px + np.cos(a) * 1.1, py - h - 0.6 + np.sin(a) * 0.9, 0.8, 0.7), BLOOM[1])
                c.paint(c.ellipse(px, py - h - 0.6, 0.5, 0.5), BLOOM[2])


def thorn_bloom(view, frame):
    c = canvas(30, 24, fine=True)
    cx = 30 + (-2 if view == "right" else 0)

    def armor(c, x, y):
        _spikes(c, x, y + 1, 12 if view == "right" else 11, 6.5, 3)
        speckle(c, c.ellipse(x, y, 12, 6.5) & ~c.ellipse(x, y - 3, 12, 5), SCALE[1], 0.2, 141, (0, 0))

    def tail(c, x, y):
        tip = (x - 9, y + 5) if view == "right" else (x + 3, y + 9)
        chain(c, [(x, y + 2), ((x + tip[0]) / 2, (y + tip[1]) / 2 + 1), tip], 2.6, 0.8, SCALE)

    def head(c, x, y):
        if view == "up":
            ball(c, x, y + 3, 4.6, 3.6, SCALE, bias=-0.05)
            return
        hx, hy = (x + 3, y + 5) if view == "right" else (x, y + 5)
        ball(c, hx, hy, 5.4 if view != "right" else 5, 4.2, SCALE, bias=0.05)
        # 뿔 왕관
        for k in range(5):
            a = np.pi * (1.15 + k * 0.175)
            base = (hx + np.cos(a) * 4.2, hy + np.sin(a) * 3.2)
            c.paint(c.polygon([(base[0] - 1, base[1]), (base[0] + 1, base[1]), (base[0] + np.cos(a) * 3, base[1] + np.sin(a) * 3)]), SCALE[3])
        small_face(c, hx + (1.5 if view == "right" else 0), hy, view, gap=2.6, eye=(1.1, 0.8), mouth="line", glow=AMBER, squint=0.4)

    quadruped(c, view, frame, cx, 46, (12 if view == "right" else 11, 6.5, 8), (2.0, 0.65), SCALE, head, tail, armor=armor)
    return c
