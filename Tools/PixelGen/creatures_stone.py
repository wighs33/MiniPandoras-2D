"""생물 도감 · 뱀성·동굴·산길(8·10·11·12레벨).

  8 돌날개   (사나움) 뱀성 성가퀴. 돌 살갗에 이끼가 낀 웅크린 꼬마 악마, 굽은 뿔과 접은 돌 날개, 가래 꼬리, 가늘게 뜬 호박빛 눈과 이빨
 10 수정등   (사나움) 호빗 동굴. 눈이 없는 별코 두더지꼴, 코끝에 분홍 촉수가 별처럼 돋고, 등에 빛을 머금은 수정 기둥이 솟는다
 11 종유귀   (사나움) 동굴 천장. 종유석처럼 길게 늘어진 돌 귀로 매달리는 박쥐꼴, 귀 끝에서 물방울이 떨어진다
 12 구름뿔   (귀여움) 7장 산길. 복슬복슬한 흰 털의 산짐승, 말린 뿔에 구름이 휘감기고 꼬리도 구름 한 덩이
"""
import numpy as np

import pixelgen as pg
from creature_kit import quadruped, small_face
from monster_kit import GLINT, ball, blob, canvas, chain, feather, light_of, limb, shade, spark, speckle, tilted

AMBER = pg.hexes("#5a2c06", "#c87a1c", "#ffd878")

# ── 8 돌날개 ───────────────────────────────────────────

GRANITE = pg.hexes("#24242a", "#3a3a42", "#55555e", "#72727a", "#92929a", "#b2b2b8")
LICHEN = pg.hexes("#4a5a34", "#6a7a46")
CRACK = np.array([26, 24, 30], dtype=np.uint8)


def _stone_wing(c, root, side, spread, frame):
    """접은 돌 날개: 손가락뼈 셋 사이에 막, 끝이 갈고리처럼 굽는다."""
    tips = [(root[0] + side * 3 * spread, root[1] - 11)] + [(root[0] + side * (7 + k * 2.5) * spread, root[1] - 9 + k * 4) for k in range(3)]
    # 막은 이웃한 뼈 끝 사이에만 칠한다(뿌리에서 겹치는 두 점으로는 면이 생기지 않는다).
    for a, b in zip(tips, tips[1:]):
        c.paint(c.polygon([root, a, b]), GRANITE[2])
    for tip in tips:
        limb(c, root, tip, 0.8, 0.5, GRANITE[:3])
    limb(c, root, (root[0] + side * 3 * spread, root[1] - 11), 1.0, 0.6, GRANITE[:3])


def stone_imp(view, frame):
    c = canvas(26, 26, fine=True)
    cx = 26 + (1 if view == "right" else 0)
    ground = 50
    crouch = (0, 1.5, -1.0)[frame]
    side_view = view == "right"
    spread = 0.55 if side_view else 1.0
    if view != "up":
        for side in ((1,) if side_view else (-1, 1)):
            _stone_wing(c, (cx + side * 4 - (4 if side_view else 0), 30 + crouch), side if not side_view else -1, spread, frame)
    # 가래 꼬리
    tail_side = -1 if side_view else 1
    chain(c, [(cx - (4 if side_view else 0), 42 + crouch), (cx + tail_side * 9, 44), (cx + tail_side * 13, 39 + (frame - 1))], 1.4, 0.8, GRANITE)
    tip = (cx + tail_side * 13, 39 + (frame - 1))
    c.paint(c.polygon([(tip[0], tip[1] - 2.5), (tip[0] + tail_side * 2.5, tip[1]), (tip[0], tip[1] + 2), (tip[0] - tail_side * 1, tip[1])]), GRANITE[3])
    # 웅크린 다리(뒤로 꺾인 발목)
    for side in (-1, 1):
        up = 1.5 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
        hip = (cx + side * (3 if side_view else 5) + (2 if side_view else 0), 40 + crouch)
        knee = (hip[0] + (4 if side_view else side * 2), hip[1] + 4)
        foot = (hip[0] + (1 if side_view else side * 1), ground - 1 - up)
        limb(c, hip, knee, 2.4, 2.0, GRANITE)
        limb(c, knee, foot, 1.8, 1.5, GRANITE)
        for toe in (-1.2, 0, 1.2):
            limb(c, foot, (foot[0] + toe + (1.5 if side_view else 0), foot[1] + 0.8), 0.6, 0.4, GRANITE[:3])
    body = ball(c, cx + (1 if side_view else 0), 36 + crouch, 7.5 if not side_view else 6.5, 8, GRANITE)
    speckle(c, body, LICHEN[0], 0.08, 81, (0, 0))
    if view == "up":
        for side in (-1, 1):
            _stone_wing(c, (cx + side * 3, 31 + crouch), side, 1.0, frame)
    # 팔: 무릎에 얹은 굵은 앞발
    for side in ((1,) if side_view else (-1, 1)):
        limb(c, (cx + side * 6 + (2 if side_view else 0), 32 + crouch), (cx + side * 7 + (4 if side_view else 0), 41 + crouch), 1.8, 1.6, GRANITE)
    hx, hy = cx + (3 if side_view else 0), 25 + crouch
    head = ball(c, hx, hy, 6.5 if not side_view else 6, 5.8, GRANITE, bias=0.05)
    c.paint(head & (np.abs(c.xx + 0.5 - hx - 2 - (c.yy + 0.5 - hy) * 0.4) < 0.4) & (c.yy + 0.5 < hy - 1), CRACK)
    speckle(c, head & (c.yy + 0.5 < hy - 2), LICHEN[1], 0.1, 82, (0, 0))
    for side in ((-1, 1) if not side_view else (-0.4, 1)):
        chain(c, [(hx + side * 3.5, hy - 4.5), (hx + side * 6.5, hy - 8), (hx + side * 6, hy - 11)], 1.4, 0.6, GRANITE[:4])
        c.paint(c.polygon([(hx + side * 5.5, hy - 1.5), (hx + side * 9, hy - 3.5), (hx + side * 6, hy - 0.2)]), GRANITE[3])
    small_face(c, hx + (1 if side_view else 0), hy - 0.5, view, gap=2.8, eye=(1.2, 0.9), mouth="teeth", glow=AMBER, squint=0.35)
    return c


# ── 10 수정등 ──────────────────────────────────────────

VELVET = pg.hexes("#16121c", "#241e2c", "#342c3e", "#463c52", "#5a4e66")
NOSE = pg.hexes("#8a3a52", "#c45a74", "#e88ca0")
CLAW = pg.hexes("#8a8070", "#c0b6a2")
CRYSTAL = pg.hexes("#3a4a8a", "#5a7ac0", "#8ab0e4", "#c4e0fa", "#f0faff")


def _crystal(c, base, angle, length, width):
    """빛을 머금은 수정 기둥: 육각 기둥 끝이 뾰족하고, 왼쪽 면이 밝다."""
    ux, uy = np.cos(angle), np.sin(angle)
    px, py = -uy, ux
    bx, by = base
    tip = (bx + ux * length, by + uy * length)
    shoulder = (bx + ux * length * 0.72, by + uy * length * 0.72)
    left = c.polygon([(bx + px * width, by + py * width), (shoulder[0] + px * width, shoulder[1] + py * width), tip, (shoulder[0], shoulder[1]), (bx, by)])
    right = c.polygon([(bx, by), (shoulder[0], shoulder[1]), tip, (shoulder[0] - px * width, shoulder[1] - py * width), (bx - px * width, by - py * width)])
    lit, dim = (left, right) if px < 0 else (right, left)
    c.paint(dim, CRYSTAL[1])
    c.paint(lit, CRYSTAL[3])
    c.paint(lit & (np.abs((c.xx + 0.5 - bx) * px + (c.yy + 0.5 - by) * py) < 0.5), CRYSTAL[4])


def crystal_mole(view, frame):
    c = canvas(28, 26, fine=True)
    cx = 28 + (-2 if view == "right" else 0)

    def armor(c, x, y):
        for k, (dx, a, length) in enumerate(((-6, -2.0, 8), (-1, -1.65, 11), (4, -1.3, 9), (8, -1.0, 6))):
            if view == "up" or view == "down" or k < 4:
                _crystal(c, (x + dx * (0.9 if view == "right" else 0.8), y - 4), a if view != "up" else a, length, 2.0)
        glow = (0.55, 0.75, 0.6)[frame]
        for k in range(2):
            spark(c, x - 4 + k * 9, y - 14 - k * 3 + frame, 0.8, CRYSTAL[3], CRYSTAL[4])

    def head(c, x, y):
        if view == "up":
            ball(c, x, y + 2, 5, 4, VELVET, bias=-0.1)
            return
        hx, hy = (x + 3, y + 5) if view == "right" else (x, y + 6)
        ball(c, hx, hy, 5.5, 4.8, VELVET, bias=0.05)
        nose = (hx + 5.5, hy + 1) if view == "right" else (hx, hy + 3.5)
        # 별코: 코끝에 분홍 촉수가 둥글게 돋는다(눈은 없다).
        for k in range(11):
            a = k * 2 * np.pi / 11 + frame * 0.1
            squash = 0.5 if view == "right" else 1.0
            limb(c, nose, (nose[0] + np.cos(a) * 2.8 * squash, nose[1] + np.sin(a) * 2.8), 0.6, 0.45, NOSE)
        ball(c, nose[0], nose[1], 1.2, 1.2, NOSE, bias=0.1)
        # 땅을 파는 넓적한 앞발톱
        for side in ((1,) if view == "right" else (-1, 1)):
            fx = hx + (4 if view == "right" else side * 6)
            for k in range(3):
                c.paint(c.polygon([(fx - 1.5 + k * 1.3, 43), (fx - 0.6 + k * 1.3, 43), (fx - 1.0 + k * 1.3 + side * 0.6, 46)]), CLAW[1])

    quadruped(c, view, frame, cx, 47, (11 if view == "right" else 10, 7, 9), (2.0, 0.6), VELVET, head, armor=armor)
    return c


# ── 11 종유귀 ──────────────────────────────────────────

SLATE = pg.hexes("#1a1c22", "#2a2e36", "#3e434e", "#565c68", "#727884")
DRIPSTONE = pg.hexes("#4a443a", "#6a6254", "#8a8272", "#aaa290", "#c8c0ae")
MEMBRANE = pg.hexes("#2a242c", "#3e3640", "#544a56")
WATER = pg.hexes("#4a78a8", "#9ac4ea")
PALE = pg.hexes("#3a4a5a", "#9ab8d0", "#e8f4ff")


def drip_ear(view, frame):
    c = canvas(28, 30, fine=True)
    cx = 28 + (1 if view == "right" else 0)
    bob = (0, -1, 1)[frame]
    flap = (0, -0.5, 0.5)[frame]
    side_view = view == "right"
    by = 38 + bob
    # 날개: 손가락뼈 사이의 막이 장마다 들렸다 내려간다.
    for side in ((-1,) if side_view else (-1, 1)):
        root = (cx + side * 3, by - 2)
        tips = [(root[0] + side * 4, root[1] - 7 + flap * 4), (root[0] + side * 13, root[1] - 6 + flap * 6), (root[0] + side * 14, root[1] + 1 + flap * 4), (root[0] + side * 10, root[1] + 6 + flap * 2)]
        for a, b in zip(tips, tips[1:]):
            c.paint(c.polygon([root, a, b]), MEMBRANE[1])
        for tip in tips:
            limb(c, root, tip, 0.7, 0.4, SLATE[:3])
    body = ball(c, cx, by, 5.5, 6.5, SLATE)
    for side in (-1, 1):
        limb(c, (cx + side * 2, by + 5), (cx + side * 2.5, by + 9), 0.7, 0.5, SLATE[:3])
    hx, hy = cx + (1.5 if side_view else 0), by - 7.5
    # 종유석 귀: 길게 솟아 가늘어지는 돌 귀, 결이 가로로 지고 끝에서 물방울이 떨어진다.
    for side in ((-0.5, 1) if side_view else (-1, 1)):
        base = (hx + side * 3.2, hy - 2)
        tip = (hx + side * 5.5, hy - 20)
        ear = feather(c, base, tip, 5.6, DRIPSTONE, bias=0.0)
        c.paint(ear & (np.mod(np.floor(c.yy * 0.8 + np.sin(c.xx) * 0.6), 3) == 0), DRIPSTONE[1])
        drop_y = tip[1] - 1 + (frame * 2) % 5
        c.paint(c.ellipse(tip[0], drop_y, 0.8, 1.2), WATER[0])
    ball(c, hx, hy, 5, 4.4, SLATE, bias=0.05)
    small_face(c, hx + (1 if side_view else 0), hy + 0.2, view, gap=2.4, eye=(1.0, 0.8), mouth="fang", glow=PALE, squint=0.2)
    return c


# ── 12 구름뿔 ──────────────────────────────────────────

FLEECE = pg.hexes("#7a766c", "#a29e92", "#c4c0b4", "#dedad0", "#efede6")
HOOF = pg.hexes("#3a3632", "#5a5650")
HORN = pg.hexes("#5a4a3a", "#7e6a54", "#a08a70", "#c0aa8e")
CLOUD = pg.hexes("#a8b4c4", "#c8d2de", "#e2e8f0", "#f4f7fa")
GOAT_FACE = pg.hexes("#6a5e52", "#8a7e70", "#a89c8c")


def _curl_horn(c, x, y, side, frame):
    """말린 뿔에 구름이 휘감긴다."""
    points = [(x + side * (2 + np.cos(t) * 3.2 * (1 + t * 0.15)), y - 1 + np.sin(t) * 3.4 * (1 + t * 0.15) - t * 0.4) for t in np.linspace(-1.6, 3.2, 7)]
    chain(c, points, 1.6, 0.8, HORN)
    for k, (px, py) in enumerate(points[2::3]):
        blob(c, px + side * 1.8, py - 1.8, 2.0 - k * 0.3, CLOUD, wobble=0.3, lobes=5, phase=k + frame, bias=0.1)


def cloud_horn(view, frame):
    c = canvas(28, 28, fine=True)
    cx = 28 + (-2 if view == "right" else 0)

    def armor(c, x, y):
        for k in range(9):
            a = np.pi * (1.05 + k * 0.11)
            blob(c, x + np.cos(a) * 8, y + np.sin(a) * 5.5, 3.0, FLEECE, wobble=0.4, lobes=5, phase=k, bias=0.05)

    def tail(c, x, y):
        blob(c, x - (2 if view == "right" else 0), y - 1 + (frame - 1) * 0.5, 3.2, CLOUD, wobble=0.4, lobes=6, phase=frame, bias=0.1)

    def head(c, x, y):
        hx, hy = (x + 2, y - 1) if view == "right" else (x, y)
        if view == "up":
            for side in (-1, 1):
                _curl_horn(c, hx + side * 2, hy - 3, side, frame)
            blob(c, hx, hy, 5, FLEECE, wobble=0.5, lobes=7, phase=1)
            return
        blob(c, hx, hy - 1.5, 5.6, FLEECE, wobble=0.5, lobes=7, phase=2, bias=0.05)
        face = ball(c, hx + (1.5 if view == "right" else 0), hy + 1.5, 3.8 if view != "right" else 3.2, 4.2, GOAT_FACE, bias=0.05)
        for side in ((-1, 1) if view != "right" else (-0.4, 1)):
            _curl_horn(c, hx + side * 3, hy - 3.5, 1 if side > 0 else -1, frame)
            tilted_ear, u, v = tilted(c, hx + side * 5.2, hy, 2.0, 0.9, 0.2 * side)
            shade(c, tilted_ear, 0.5, GOAT_FACE)
        blob(c, hx + (1.5 if view == "right" else 0), hy + 6, 1.4, FLEECE, wobble=0.2, lobes=4, phase=0)
        small_face(c, hx + (1.5 if view == "right" else 0), hy + 0.8, view, gap=2.0, eye=(0.7, 1.0), mouth="w")

    quadruped(c, view, frame, cx, 52, (10 if view == "right" else 8.5, 7, 12), (1.8, 0.55), FLEECE, head, tail, armor=armor)
    return c
