"""생물 도감(creatures_*.py)의 공통 재료: 작고 덤덤한 얼굴, 네발 짐승 뼈대.

사용자 지적(2026-11-03): 「초롱초롱한 눈은 절대 금지, 프로젝트와 안 맞는다. 주황버섯·슬라임처럼 눈이 작고 입이 약간 ω 모양이라 귀엽다.
입이 없는 게 낫다면 눈만」. 그래서 눈은 빛점 없는 작은 타원이고, 입은 가는 ω나 없음이다. 볼 홍조도 넣지 않는다.
사나운 몬스터는 같은 자리에 그 색으로 빛나는 작은 눈(가늘게 뜰 수도 있다)과 이빨을 둔다.
그림은 FineCanvas(점 하나 = 2×2 px)에 그려 웹게임 인물처럼 곱다. 좌표는 점 단위다.
"""
import numpy as np

import pixelgen as pg
from monster_kit import ball, limb

INK = np.array([34, 26, 24], dtype=np.uint8)
TEETH = np.array([236, 230, 214], dtype=np.uint8)


def small_face(c, x, y, view, gap=3.0, eye=(0.9, 1.4), mouth="w", ink=INK, glow=None, squint=0.0):
    """
    작은 눈 둘과 작은 입. (x, y)는 두 눈 사이 가운데. 옆(오른쪽)에서는 3/4으로 돌아 먼 눈이 좁고 가장자리 쪽이다. 뒷모습에는 그리지 않는다.
    glow: 빛나는 눈 색 셋(어두운 테 → 밝은 속, 사나운 몬스터). squint: 0~1, 눈 위를 가로로 깎아 가늘게 뜬 눈.
    mouth: w(가는 ω), line(가는 한 일), fang(한 일 아래 송곳니 하나), teeth(이빨 줄), none.
    """
    if view == "up":
        return
    rx, ry = eye
    if view == "right":
        eyes = ((x + gap * 0.25, 1.0), (x + gap * 1.45, 0.65))
        mx = x + gap * 0.95
    else:
        eyes = ((x - gap, 1.0), (x + gap, 1.0))
        mx = x
    for ex, width in eyes:
        shape = c.ellipse(ex, y, rx * width, ry)
        if squint:
            shape &= c.yy + 0.5 >= y - ry * (1 - squint * 1.6)
        if glow is None:
            c.paint(shape, ink)
        else:
            c.paint(shape, glow[0])
            c.paint(shape & c.ellipse(ex, y + ry * 0.1, rx * width * 0.6, ry * 0.6), glow[1])
            c.paint(shape & c.ellipse(ex, y + ry * 0.15, max(0.5, rx * width * 0.3), ry * 0.3), glow[2])
    my = y + ry + 1.3
    if mouth == "w":
        for dx in (-0.75, 0.75):
            arc = c.ellipse(mx + dx, my, 0.8, 0.65) & ~c.ellipse(mx + dx, my - 0.5, 0.8, 0.65) & (c.yy + 0.5 >= my)
            c.paint(arc, ink)
    elif mouth in ("line", "fang"):
        c.paint(c.rect(mx - 1.4, my + 0.2, mx + 1.4, my + 0.7), ink)
        if mouth == "fang":
            c.paint(c.polygon([(mx + 0.2, my + 0.6), (mx + 1.2, my + 0.6), (mx + 0.7, my + 2.0)]), TEETH)
    elif mouth == "teeth":
        c.paint(c.rect(mx - 2.6, my, mx + 2.6, my + 1.6), ink)
        for k in range(5):
            tx = mx - 2.2 + k * 1.1
            c.paint(c.polygon([(tx - 0.45, my), (tx + 0.45, my), (tx, my + 1.3)]), TEETH)


def quadruped(c, view, frame, cx, ground, body, legs, palette, head, tail=None, bob=1.0, armor=None):
    """
    네발 짐승 뼈대: 몸통 타원과 다리 넷을 방향에 맞는 앞뒤 차례로 그리고, 머리·꼬리는 넘겨받은 함수가 그린다.
    body = (가로 반지름, 세로 반지름, 몸통 가운데 높이: 땅에서 위로), legs = (다리 굵기, 앞뒤 간격 비율).
    head(c, x, y) / tail(c, x, y)는 그 자리(머리·꼬리 뿌리)에 그린다. 옆에서는 머리가 오른쪽 앞, 위(뒷모습)에서는 머리가 몸 뒤라 먼저 그린다.
    armor(c, x, y)가 있으면 몸통을 칠한 바로 뒤 몸통 가운데에 덧그린다(등딱지·갈기).
    걸음: 옆에서는 대각선 두 다리씩 앞뒤로 엇갈리고, 앞·뒤에서는 좌우 다리를 번갈아 든다. 돌려주는 값: 몸통 가운데.
    """
    rx, ry, height = body
    leg_r, spread = legs
    lift = bob if frame else 0.0
    by = ground - height - lift
    far = palette[:max(2, len(palette) - 1)]
    if view == "right":
        step = (0.0, 3.0, -3.0)[frame]
        fronts, backs = cx + rx * spread, cx - rx * spread
        if tail:
            tail(c, cx - rx * 0.9, by - ry * 0.3)
        for x, s in ((fronts - 1.5, -step), (backs - 1.5, step)):
            limb(c, (x, by + ry * 0.3), (x + s * 0.5, ground - 1), leg_r, leg_r * 0.9, far, bias=-0.15)
        ball(c, cx, by, rx, ry, palette)
        if armor:
            armor(c, cx, by)
        for x, s in ((fronts + 1.0, step), (backs + 1.0, -step)):
            limb(c, (x, by + ry * 0.35), (x + s * 0.5, ground - 1), leg_r, leg_r * 0.9, palette)
            ball(c, x + s * 0.5 + leg_r * 0.3, ground - 1, leg_r * 1.15, leg_r * 0.7, palette, bias=-0.05)
        head(c, cx + rx * 0.9, by - ry * 0.75)
        return cx, by
    if view == "up":
        head(c, cx, by - ry * 0.85)
    elif tail:
        tail(c, cx, by - ry * 0.9)
    for side in (-1, 1):
        up = 1.5 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0.0
        x = cx + side * rx * 0.55
        limb(c, (x, by), (x + side * 0.5, ground - 2.5 - up), leg_r * 0.9, leg_r * 0.8, far, bias=-0.2)
    ball(c, cx, by, rx, ry, palette)
    if armor:
        armor(c, cx, by)
    for side in (-1, 1):
        up = 1.5 if (frame == 1 and side > 0) or (frame == 2 and side < 0) else 0.0
        x = cx + side * rx * 0.42
        limb(c, (x, by + ry * 0.4), (x, ground - 1 - up), leg_r, leg_r * 0.9, palette)
        ball(c, x, ground - 1 - up, leg_r * 1.1, leg_r * 0.7, palette, bias=-0.05)
    if view == "up":
        if tail:
            tail(c, cx, by + ry * 0.2)
    else:
        head(c, cx, by - ry * 0.55)
    return cx, by


def outline_of(color, darken=0.45):
    """몸 색에서 테두리 색을 만든다(검정이 아니라 그 색의 어두운 쪽)."""
    return (np.asarray(color, dtype=float) * darken).astype(np.uint8)


def palette(*codes):
    return pg.hexes(*codes)
