"""생물 도감 · 차원공간(20레벨).

 20 별고래 (사나움, 웅장) 차원공간의 가장자리. 짙은 남빛 몸에 별자리가 박힌 하늘 고래, 배에 옅은 주름, 작게 빛나는 눈.
          차원 틈을 헤엄치듯 떠다니고, 별이 장마다 깜빡인다.
"""
import numpy as np

import pixelgen as pg
from creature_kit import small_face
from monster_kit import ball, canvas, chain, light_of, limb, shade, spark, tilted

WHALE = pg.hexes("#0a0e22", "#121a38", "#1c2852", "#283a6c", "#36508a", "#4a68a6")
BELLY = pg.hexes("#5a6a8e", "#7a8aaa", "#9aa8c4")
STAR = pg.hexes("#8ab4ff", "#d8e8ff", "#ffffff")
LINE = np.array([90, 120, 190], dtype=np.uint8)
EYE = pg.hexes("#1a3a7a", "#7ab0ff", "#e8f4ff")
# 별자리: 몸 위에 찍을 별(몸 기준 비율 좌표)과 이을 선
STARS = ((-0.55, -0.35), (-0.3, -0.55), (0.0, -0.45), (0.25, -0.6), (0.45, -0.3), (-0.1, -0.15), (0.3, -0.05))
LINKS = ((0, 1), (1, 2), (2, 3), (3, 4), (2, 5), (5, 6))


def _constellation(c, mask, cx, cy, rx, ry, frame, mirror=1):
    points = [(cx + x * rx * mirror, cy + y * ry) for x, y in STARS]
    for a, b in LINKS:
        c.paint(limb(c, points[a], points[b], 0.3, 0.3, WHALE[3:4]) & mask, LINE)
    for k, (x, y) in enumerate(points):
        size = 1.4 if (k + frame) % 3 == 0 else 0.9
        spark(c, x, y, size, STAR[0], STAR[2])


def star_whale(view, frame):
    c = canvas(42, 30, fine=True)
    bob = (0, -1, 1)[frame]
    swish = (0, 1.5, -1.5)[frame]
    if view == "right":
        cx, cy = 44, 26 + bob
        # 꼬리 자루와 꼬리지느러미(왼쪽)
        chain(c, [(cx - 18, cy + 1), (cx - 27, cy - 1 + swish * 0.5), (cx - 33, cy - 2 + swish)], 6, 2.4, WHALE)
        for side in (-1, 1):
            fluke, u, v = tilted(c, cx - 36, cy - 2 + swish + side * 4, 5.5, 2.2, np.pi / 2 * side + 0.3 * side)
            shade(c, fluke, 0.55 - v * 0.2, WHALE)
        body = ball(c, cx, cy, 20, 10, WHALE)
        belly = body & (c.yy + 0.5 > cy + 3 - (c.xx + 0.5 - cx) * 0.08)
        shade(c, belly, 0.6, BELLY)
        for k in range(5):
            c.paint(belly & (np.abs(c.yy + 0.5 - (cy + 4.5 + k * 1.4)) < 0.3), BELLY[0])
        fin, u, v = tilted(c, cx + 2, cy + 7, 6, 2.2, 0.9)
        shade(c, fin, 0.5, WHALE)
        _constellation(c, body, cx - 2, cy, 18, 10, frame)
        c.paint(c.rect(cx + 10, cy + 3, cx + 19, cy + 3.6), WHALE[0])
        small_face(c, cx + 11, cy, view, gap=1.0, eye=(1.0, 0.9), mouth="none", glow=EYE)
        return c
    cx, cy = 42, 30 + bob
    if view == "up":
        # 뒤에서 보면 꼬리지느러미가 가장 앞에, 몸은 멀어지며 좁아진다.
        body = ball(c, cx, cy - 4, 15, 11, WHALE)
        _constellation(c, body, cx, cy - 4, 13, 10, frame)
        for side in (-1, 1):
            fin, u, v = tilted(c, cx + side * 16, cy - 1, 6, 2.2, side * 0.4)
            shade(c, fin, 0.5, WHALE)
        chain(c, [(cx, cy + 4), (cx, cy + 12)], 5, 3, WHALE)
        for side in (-1, 1):
            fluke, u, v = tilted(c, cx + side * 6 + swish * 0.3, cy + 14, 7, 2.4, side * 0.25)
            shade(c, fluke, 0.6 - v * 0.2, WHALE)
        return c
    for side in (-1, 1):
        fin, u, v = tilted(c, cx + side * 17, cy + 5, 7, 2.4, side * 0.5 + (frame - 1) * 0.1 * side)
        shade(c, fin, 0.5, WHALE)
    body = ball(c, cx, cy, 17, 13, WHALE)
    belly = body & (c.yy + 0.5 > cy + 4)
    shade(c, belly, 0.6, BELLY)
    for k in range(4):
        c.paint(belly & (np.abs(c.yy + 0.5 - (cy + 6 + k * 1.5)) < 0.3), BELLY[0])
    _constellation(c, body, cx, cy - 1, 15, 12, frame)
    c.paint(c.rect(cx - 9, cy + 3.5, cx + 9, cy + 4.1), WHALE[0])
    small_face(c, cx, cy + 0.5, view, gap=11, eye=(1.0, 0.9), mouth="none", glow=EYE)
    return c
