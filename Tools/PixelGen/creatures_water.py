"""생물 도감 · 강가·웅덩이·바닷가(6·7·9·15레벨).

  6 물이끼방울 (귀여움) 뱀성 앞 강가. 물이끼 가닥과 조약돌 하나가 비치는 말랑한 물방울(슬라임 결), 꼭대기가 살짝 뾰족하다
  7 조약갑     (사나움) 강가. 젖은 강돌 판을 겹겹이 진 굴파기 짐승, 뾰족한 주둥이와 깊이 박힌 작은 눈, 돌 고리 꼬리
  9 웅덩발     (귀여움) 비 오는 마을 외곽. 개구리 뒷다리로 웅덩이를 건너뛰는 물고기, 등에 우산처럼 펴는 지느러미
 15 산호뿔     (귀여움) 서부 바닷가. 산호 뿔을 단 해달꼴, 두 앞발로 진주 조개를 배에 안고 뒤뚱뒤뚱 걷는다
"""
import numpy as np

import pixelgen as pg
from creature_kit import quadruped, small_face
from monster_kit import GLINT, HOP, ball, canvas, chain, feather, light_of, limb, shade, spark, speckle, tilted

# ── 6 물이끼방울 ───────────────────────────────────────

JELLY_HOP = ((1.0, 1.0, 0.0), (1.2, 0.8, 0.0), (0.88, 1.15, 4.0))
DROP = pg.hexes("#1a3c38", "#28584e", "#3c7666", "#5a9482", "#84b4a2", "#b4d4c6")
WEED = pg.hexes("#1e3a1c", "#2e5226")
PEBBLE = pg.hexes("#4a4a50", "#6a6a70", "#8a8a90")


def moss_drop(view, frame):
    c = canvas(20, 22, fine=True)
    sx, sy, lift = JELLY_HOP[frame]
    cx = 20 + (1 if view == "right" else 0)
    base = 41 - lift
    w, h = 14 * sx, 12 * sy
    cy = base - h
    nx = (c.xx + 0.5 - cx) / w
    ny = (c.yy + 0.5 - cy) / h
    squeeze = 1 + 0.45 * np.clip(ny, -1, 0)
    r2 = (nx / np.maximum(squeeze, 0.05)) ** 2 + ny ** 2
    body = (r2 <= 1) & (c.yy + 0.5 <= base)
    nz = np.sqrt(np.clip(1 - r2, 0, 1))
    shade(c, body, light_of(nx, ny, nz) + np.clip(ny - 0.2, 0, 1) * 0.2, DROP, dither=0.14)
    # 속에 비치는 물이끼 가닥과 바닥의 조약돌
    # 물이끼는 얼굴을 가리지 않게 바닥 양옆에서만 하늘거린다.
    for k, x0 in enumerate((cx - 9 * sx, cx - 6 * sx, cx + 8 * sx)):
        points = [(x0 + np.sin(j * 1.2 + k + frame * 0.6) * 0.9, base - 1.5 - j * 2.2 * sy) for j in range(3)]
        c.paint(chain(c, points, 0.5, 0.4, WEED) & body, WEED[1])
    c.paint(c.ellipse(cx + 4, base - 2.5, 2.6, 1.8) & body, PEBBLE[1])
    c.paint(c.ellipse(cx + 3.4, base - 3.2, 1.2, 0.6) & body, PEBBLE[2])
    c.paint(body & (c.yy + 0.5 > base - 1.4), DROP[1])
    c.paint(c.ellipse(cx - w * 0.45, cy - h * 0.35, w * 0.16, h * 0.11) & body, DROP[5])
    small_face(c, cx + (1 if view == "right" else 0), cy + h * 0.15, view, gap=3.4, eye=(0.9, 1.35), mouth="w", ink=np.array([16, 34, 30], dtype=np.uint8))
    return c


# ── 7 조약갑 ───────────────────────────────────────────

HIDE = pg.hexes("#2a2420", "#433a32", "#5e5246", "#7a6c5c")
STONES = [pg.hexes("#3a3e44", "#565c64", "#767c84", "#989ea4", "#bcc0c4"),
          pg.hexes("#3e3a34", "#5c564c", "#7c7466", "#9c9484", "#bcb4a4"),
          pg.hexes("#343c3e", "#4e585a", "#6c7678", "#8e9898", "#b2baba")]


def _stone_plates(c, x, y, rx, ry, rows, frame):
    """등을 덮은 강돌 판: 줄마다 둥근 돌이 엇갈려 겹치고, 위쪽 돌일수록 젖은 빛이 돈다."""
    for row in range(rows):
        t = row / max(rows - 1, 1)
        py = y + ry * (0.55 - t * 1.05)
        width = rx * np.sqrt(np.clip(1 - (0.55 - t * 1.05) ** 2, 0.2, 1))
        count = 3 + row
        for k in range(count):
            px = x - width + (k + 0.5 + (row % 2) * 0.3) * (2 * width / count)
            palette = STONES[(row * 3 + k) % 3]
            ball(c, px, py, width / count * 1.25, ry * 0.42, palette, bias=0.05 - t * 0.05)
            c.paint(c.ellipse(px - 1, py - ry * 0.18, 0.9, 0.5), palette[4])


def pebble_shell(view, frame):
    c = canvas(30, 24, fine=True)
    cx = 30 + (-2 if view == "right" else 0)

    def armor(c, x, y):
        _stone_plates(c, x, y - 1, 12 if view == "right" else 11, 7.5, 4, frame)

    def head(c, x, y):
        if view == "up":
            ball(c, x, y + 3, 4.5, 3.5, HIDE, bias=-0.1)
            return
        if view == "right":
            hx, hy = x + 1, y + 6
            chain(c, [(hx - 2, hy), (hx + 4, hy + 1), (hx + 9, hy + 3)], 4.2, 1.2, HIDE)
            ball(c, hx - 1, hy - 3.5, 1.4, 1.7, HIDE, bias=0.05)
            c.paint(c.ellipse(hx + 9.4, hy + 3, 0.9, 0.8), HIDE[0])
            small_face(c, hx + 2, hy - 0.8, view, gap=2.0, eye=(0.6, 0.6), mouth="none")
            return
        hx, hy = x, y + 4
        for side in (-1, 1):
            ball(c, hx + side * 4.8, hy - 4, 1.6, 2.0, HIDE, bias=0.1)
        ball(c, hx, hy, 5.6, 4.8, HIDE, bias=0.15)
        # 앞으로 내민 뾰족한 주둥이와 코끝
        limb(c, (hx, hy + 1.5), (hx, hy + 6.5), 2.6, 1.2, HIDE, bias=0.15)
        c.paint(c.ellipse(hx, hy + 6.8, 1.0, 0.8), HIDE[0])
        small_face(c, hx, hy - 1, view, gap=2.4, eye=(0.65, 0.65), mouth="none")

    def tail(c, x, y):
        tip = (x - 9, y + 6) if view == "right" else (x + 2, y + 8)
        for k in range(4):
            t = k / 3
            ball(c, x + (tip[0] - x) * t, y + 3 + (tip[1] - y - 3) * t + (frame - 1) * t, 2.6 - k * 0.5, 2.2 - k * 0.4, STONES[k % 3], bias=0.05)

    quadruped(c, view, frame, cx, 46, (12 if view == "right" else 11, 7.5, 9), (1.8, 0.6), HIDE, head, tail, armor=armor)
    return c


# ── 9 웅덩발 ───────────────────────────────────────────

FISH = pg.hexes("#22323e", "#344a5a", "#4a6476", "#668094", "#8aa2b2", "#b2c6d2")
FROG = pg.hexes("#2a3a24", "#40563a", "#5a7452", "#7a926c")
FIN = pg.hexes("#3e4e6a", "#5a6c8a", "#7a8eaa", "#a2b4cc")
DROPLET = pg.hexes("#5a88b8", "#a8ccec")


def puddle_hopper(view, frame):
    c = canvas(24, 22, fine=True)
    sx, sy, lift = HOP[frame]
    cx = 24 + (-1 if view == "right" else 0)
    ground = 42
    body_y = ground - 4 - lift - 7 * sy
    spread = (0.0, 2.5, -1.5)[frame]
    if view == "right":
        # 개구리 뒷다리(접혔다 뻗는다)
        knee = (cx - 6, body_y + 3 - spread)
        limb(c, (cx - 3, body_y + 4), knee, 2.6, 2.2, FROG)
        limb(c, knee, (cx - 2 + spread, ground - 1), 1.8, 1.4, FROG)
        ball(c, cx - 1 + spread, ground - 1, 3, 1.3, FROG, bias=0.05)
        # 꼬리지느러미
        c.paint(c.polygon([(cx - 9, body_y), (cx - 16, body_y - 6), (cx - 15, body_y + 6)]), FIN[1])
        c.paint(c.polygon([(cx - 9, body_y), (cx - 15, body_y - 5), (cx - 13, body_y)]), FIN[2])
        body = ball(c, cx, body_y, 10 * sx, 7 * sy, FISH)
        c.paint(body & (c.yy + 0.5 > body_y + 3), FISH[4])
        speckle(c, body & (c.yy + 0.5 < body_y + 1), FISH[1], 0.08, 91, (0, 0))
        fin_x, fin_y = cx - 1, body_y - 6 * sy
        hx = cx + 3.5
    else:
        for side in (-1, 1):
            knee = (cx + side * 9, body_y + 2 - spread)
            limb(c, (cx + side * 5, body_y + 4), knee, 2.4, 2.0, FROG)
            limb(c, knee, (cx + side * 8, ground - 1), 1.7, 1.3, FROG)
            ball(c, cx + side * 8.5, ground - 1, 2.8, 1.3, FROG, bias=0.05)
        if view == "up":
            c.paint(c.polygon([(cx, body_y + 4), (cx - 6, body_y + 12), (cx + 6, body_y + 12)]), FIN[1])
        body = ball(c, cx, body_y, 9 * sx, 7.5 * sy, FISH)
        if view == "down":
            ball(c, cx, body_y + 3, 5.5, 3.5, FISH[3:], bias=0.1)
        speckle(c, body & (c.yy + 0.5 < body_y), FISH[1], 0.08, 92, (0, 0))
        fin_x, fin_y = cx, body_y - 7 * sy
        hx = cx
    # 등 위의 우산 지느러미: 살 다섯이 부채처럼 펴지고 그 사이에 막
    for k in range(5):
        a = np.pi * (1.15 + k * 0.175)
        tip = (fin_x + np.cos(a) * 8, fin_y + np.sin(a) * 6)
        c.paint(c.polygon([(fin_x, fin_y + 1.5), tip, (fin_x + np.cos(a + 0.18) * 8, fin_y + np.sin(a + 0.18) * 6)]), FIN[2] if k % 2 else FIN[3])
        limb(c, (fin_x, fin_y + 1.5), tip, 0.4, 0.35, FIN[:1])
    if frame == 2:
        for k in range(3):
            c.paint(c.ellipse(cx + (k - 1) * 6, ground - 1 + k % 2, 1.0, 1.4), DROPLET[0])
    small_face(c, hx + (1 if view == "right" else 0), body_y - 1.5, view, gap=3.4, eye=(0.9, 1.3), mouth="w")
    return c


# ── 15 산호뿔 ──────────────────────────────────────────

OTTER = pg.hexes("#2a1c14", "#422c1e", "#5e402a", "#7c583a", "#9a7450")
CREAM = pg.hexes("#9a8a72", "#bcac90", "#d8caac")
CORAL = pg.hexes("#7a2a34", "#ac4448", "#d06a5e", "#ea9480")
POLYP = np.array([248, 222, 206], dtype=np.uint8)
CLAM = pg.hexes("#6e6258", "#988c7c", "#c0b4a0", "#dcd2c0")
PEARL = pg.hexes("#b8b0b8", "#e8e4ec")
WHISKER = np.array([206, 192, 168], dtype=np.uint8)


def _coral_crown(c, x, y, frame):
    """머리에 자란 산호 덤불: 굵은 가지 셋 끝이 혹처럼 둥글고, 혹마다 하얀 폴립 점."""
    for k, (dx, h, lean) in enumerate(((-3.5, 6, -0.5), (0, 8.5, 0.1), (3.5, 6.5, 0.5))):
        top = (x + dx + lean * 3, y - h)
        chain(c, [(x + dx * 0.4, y), (x + dx + lean, y - h * 0.5), top], 1.6, 1.1, CORAL)
        ball(c, top[0], top[1], 1.7, 1.7, CORAL, bias=0.1)
        side = (x + dx + lean * 2 + (1.8 if dx >= 0 else -1.8), y - h * 0.55)
        limb(c, (x + dx + lean, y - h * 0.45), side, 0.9, 0.8, CORAL)
        ball(c, side[0], side[1], 1.2, 1.2, CORAL, bias=0.1)
        for px, py in (top, side):
            c.paint(c.ellipse(px - 0.3, py - 0.3, 0.45, 0.45), POLYP)


def coral_otter(view, frame):
    c = canvas(26, 30, fine=True)
    cx = 26 + (1 if view == "right" else 0)
    ground = 58
    lean = (0, -1.2, 1.2)[frame]
    side_view = view == "right"
    # 길고 넓적한 꼬리가 옆으로 휘어 땅에 닿는다.
    if view != "up":
        tail_root = (cx - (4 if side_view else 3), ground - 6)
        chain(c, [tail_root, (tail_root[0] - 6, ground - 3), (tail_root[0] - 11, ground - 2 + (frame - 1))], 2.6, 1.6, OTTER, bias=-0.05)
    for side in (-1, 1):
        up = 1.5 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
        ball(c, cx + side * (3.5 if not side_view else 2.2) + (2 if side_view else 0), ground - 1.5 - up, 3.0, 1.5, OTTER, bias=-0.05)
    body_x = cx + lean * 0.5
    ball(c, body_x, ground - 13, 6.5 if not side_view else 6, 11.5, OTTER)
    if view == "up":
        chain(c, [(cx, ground - 6), (cx + 5, ground - 3), (cx + 10, ground - 2 + (frame - 1))], 2.6, 1.6, OTTER)
    else:
        ball(c, body_x + (1.5 if side_view else 0), ground - 12.5, 4.2 if not side_view else 3.4, 8.5, CREAM, bias=0.05)
        # 두 앞발로 배에 안은 가리비 조개와 진주
        clam_x, clam_y = body_x + (2 if side_view else 0), ground - 12.5
        shell = c.ellipse(clam_x, clam_y, 4.4, 3.2) & (c.yy + 0.5 > clam_y - 1.6)
        shade(c, shell, 0.75 - (c.yy - clam_y) * 0.06, CLAM)
        for k in range(-2, 3):
            c.paint(shell & (np.abs(c.xx + 0.5 - clam_x - k * 1.6 - (c.yy + 0.5 - clam_y) * k * 0.25) < 0.35), CLAM[0])
        ball(c, clam_x, clam_y - 2.2, 1.3, 1.2, PEARL, bias=0.2)
        for side in (-1, 1):
            ball(c, clam_x + side * 4, clam_y - 0.5, 1.8, 1.5, OTTER, bias=0.05)
    hx, hy = cx + lean + (2.5 if side_view else 0), ground - 27
    _coral_crown(c, hx - (1 if side_view else 0), hy - 3, frame)
    ball(c, hx, hy, 6.4 if not side_view else 5.6, 4.6, OTTER, bias=0.05)
    for side in ((-1, 1) if not side_view else (-0.5, 1)):
        ball(c, hx + side * 5.4, hy - 2.6, 1.2, 1.1, OTTER, bias=0.1)
    if view != "up":
        mx = hx + (2 if side_view else 0)
        for side in (-1, 1):
            ball(c, mx + side * 1.6, hy + 2, 2.0, 1.6, CREAM, bias=0.1)
            for k in range(2):
                limb(c, (mx + side * 2.8, hy + 1.6 + k * 0.9), (mx + side * 6, hy + 1 + k * 1.6), 0.3, 0.3, WHISKER[None, :])
        c.paint(c.ellipse(mx, hy + 0.9, 1.0, 0.7), OTTER[0])
    small_face(c, hx + (1 if side_view else 0), hy - 1.2, view, gap=3.0, eye=(0.8, 1.1), mouth="w")
    return c
