"""뱀성 소품(뱀성은 다리를 건너 산 위에 선다, 성 건물은 집처럼 따로 디자인한다).

성벽(앞면과 위 통로, 총안), 성벽 위에서 본 옆 성벽 한 칸, 둥근 망루, 성문(쇠창살을 올린·내린 두 벌), 뱀 탑(
성이 아니라 탑이고 꼭대기는 뱀 머리. 뱀은 살아 있는 게 아니라 돌 조형물이다), 뱀 석상, 화로를 그린다. 모두 위에서 비스듬히 본 시점이고 빛은 왼쪽 위에서 온다.
그림은 점 격자 RGBA와 발밑 기준점(아래 가운데, 점)을 돌려준다. 한 칸 = 32점.

사용: python Tools/PixelGen/castle.py <프로젝트 루트>
  Docs/ArtUpgrade/PixelGen/castle_catalog.png     견본 모음(주인공과 함께)
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
import serpent_sculpture
from houses import Canvas, IRON, WARM, GLASS, WOOD, cover, door, outline, window, Design

T = pg.TILE
FACE = 88             # 성벽 앞면 높이(점)
WALK = 22             # 성벽 위 통로 깊이
MERLON = 12           # 총안 사이 돌(머론) 높이
STONE = pg.hexes("#2c2b31", "#423f48", "#5b5862", "#77737e", "#938f99", "#b1adb5", "#cdc9cf", "#e2dfe3")
JADE = pg.hexes("#0f2a22", "#164032", "#1f5a44", "#2a7556", "#3a916a", "#5aad82", "#8acda2")
# 돌뱀 조각: 비바람에 바랜 돌(머리 조각과 같은 돌)
SERPENT = serpent_sculpture.STONE
GOLD = pg.hexes("#5a3c10", "#9a6c1e", "#d6a03a", "#f4d27a")
EMBER = pg.hexes("#5a1a0c", "#a8380e", "#e8701a", "#ffc44a")
BANNER = pg.hexes("#0e2a20", "#174434", "#22624a", "#2f7e60")
PAPER_WHITE = pg.hexes("#f2efe4")[0]


# ── 돌 ─────────────────────────────────────────────────

def masonry(height, width, rng, course=(11, 15), block=(16, 28)):
    """큰 돌을 줄마다 어긋나게 쌓은 면: 돌마다 밝기가 조금 다르고 윗가장자리는 밝고 아랫가장자리는 어둡다."""
    image = np.zeros((height, width, 3), dtype=np.uint8)
    y = 0
    while y < height:
        h = int(rng.integers(*course))
        x = -int(rng.integers(0, block[1]))
        while x < width:
            w = int(rng.integers(*block))
            level = int(rng.choice([3, 4, 4, 4, 5]))
            x0, x1, y1 = max(0, x), min(width, x + w), min(height, y + h)
            image[y:y1, x0:x1] = STONE[level]
            if y + 1 < y1:
                image[y + 1, x0 + 1:x1] = STONE[level + 1]
            image[y1 - 1:y1, x0:x1] = STONE[level - 2]
            image[y:y1, x0:x0 + 1] = STONE[1]
            x += w
        image[y:y + 1, :] = STONE[1]
        y += h
    return image


def moss_at_base(c, x0, x1, bottom, rng, reach=16):
    """벽 밑동의 이끼와 풀: 아래가 짙고 위로 성기다."""
    for _ in range((x1 - x0) * reach // 6):
        x = int(rng.integers(x0, x1))
        y = int(bottom - rng.random() ** 2 * reach)
        if c.image[y, x, 3]:
            c.paint(c.rect(x, y, x + 2, y + 1), pg.GRASS[int(rng.integers(2, 5))])


# ── 성벽 ───────────────────────────────────────────────

def battlements(c, x0, x1, front_y, rng, merlon_w=10, gap=8):
    """
    성벽 위: 앞 가장자리(front_y)를 따라 머론이 서고, 그 뒤로 돌 통로가 WALK점 깊이로 보인다. 통로 뒤 끝은 낮은 난간.
    """
    walk = c.rect(x0, front_y - WALK, x1, front_y)
    c.paint(walk, STONE[5])
    c.paint(walk & ((c.xx - x0) % 14 == 0), STONE[4])
    c.paint(walk & ((c.yy - front_y) % 8 == 0), STONE[4])
    c.paint(c.rect(x0, front_y - WALK, x1, front_y - WALK + 4), STONE[3])
    c.paint(c.rect(x0, front_y - WALK, x1, front_y - WALK + 1), STONE[6])
    for x in range(x0, x1, merlon_w + gap):
        right = min(x + merlon_w, x1)
        c.paint(c.rect(x, front_y - MERLON, right, front_y), STONE[4])
        c.paint(c.rect(x, front_y - MERLON - 3, right, front_y - MERLON), STONE[6])
        c.paint(c.rect(x, front_y - MERLON, x + 1, front_y), STONE[5])
        c.paint(c.rect(right - 1, front_y - MERLON, right, front_y), STONE[2])
        c.paint(c.rect(x, front_y - 1, right, front_y), STONE[2])
    c.paint(c.rect(x0, front_y, x1, front_y + 3), STONE[6])
    c.paint(c.rect(x0, front_y + 3, x1, front_y + 4), STONE[2])


def arrow_slit(c, cx, top):
    c.paint(c.rect(cx - 3, top - 2, cx + 4, top + 18), STONE[5])
    c.paint(c.rect(cx - 1, top, cx + 2, top + 16), STONE[0])
    c.paint(c.rect(cx - 3, top + 6, cx + 4, top + 8), STONE[0])


def curtain_wall(columns, rng):
    """앞을 보는 성벽 한 토막(columns칸): 큰 돌 앞면, 총안, 위 통로와 머론, 밑동 받침과 이끼."""
    width = columns * T
    height = MERLON + 3 + WALK + FACE
    c = Canvas(width, height + 2)
    front_y = MERLON + 3 + WALK
    ground = front_y + FACE
    c.paint(c.rect(0, front_y, width, ground), masonry(c.size[1], width, rng))
    c.paint(c.rect(0, ground - 8, width, ground), STONE[2])
    c.paint(c.rect(0, ground - 8, width, ground - 7), STONE[4])
    for cx in range(T + T // 2, width - T // 2, 3 * T):
        arrow_slit(c, cx, front_y + 26)
    battlements(c, 0, width, front_y, rng)
    moss_at_base(c, 0, width, ground - 1, rng)
    return outline(c.image), (width // 2, ground)


def side_wall(rng):
    """
    옆으로(남북으로) 달리는 성벽 한 칸을 위에서 본 모습: 가운데 통로, 양쪽 가장자리에 머론.
    맵에 칸마다 세워 앞뒤가 칸 단위로 갈린다.
    """
    width, depth = 40, T
    c = Canvas(width, depth + MERLON + 3)
    top = MERLON + 3
    c.paint(c.rect(0, top, width, top + depth), STONE[5])
    c.paint(c.rect(0, top, width, top + depth) & ((c.yy - top) % 10 == 0), STONE[4])
    for x0 in (0, width - 9):
        c.paint(c.rect(x0, top, x0 + 9, top + depth), STONE[3])
        c.paint(c.rect(x0, top, x0 + 1, top + depth), STONE[5] if x0 == 0 else STONE[2])
        for y in (top + 4,):
            c.paint(c.rect(x0, y - MERLON, x0 + 9, y + 6), STONE[4])
            c.paint(c.rect(x0, y - MERLON - 3, x0 + 9, y - MERLON), STONE[6])
            c.paint(c.rect(x0, y + 5, x0 + 9, y + 6), STONE[2])
    return outline(c.image), (width // 2, top + depth)


# ── 망루 ───────────────────────────────────────────────

def cylinder(c, x0, x1, top, bottom, rng):
    """둥근 돌탑 몸통: 돌 무늬에 왼쪽은 밝고 오른쪽은 어둡게, 가장자리로 갈수록 둥글게 어두워진다."""
    body = c.rect(x0, top, x1, bottom)
    c.paint(body, masonry(c.size[1], c.size[0], rng, course=(9, 12), block=(10, 18)))
    u = (c.xx + 0.5 - (x0 + x1) / 2) / ((x1 - x0) / 2)
    shade = np.clip(1.08 - 0.42 * np.abs(u) ** 1.6 - 0.18 * (u > 0.25), 0.45, 1.15)
    c.image[body, :3] = np.clip(c.image[body, :3] * shade[body][:, None], 0, 255).astype(np.uint8)


def cone_roof(c, cx, base_y, half, height, material_palette, rng):
    """원뿔 지붕: 비늘 기와, 왼쪽 반은 밝고 오른쪽 반은 어둡다. 처마는 몸통보다 조금 넓다."""
    from houses import shingles
    left = c.polygon([(cx - half, base_y), (cx, base_y), (cx, base_y - height)])
    right = c.polygon([(cx, base_y), (cx + half, base_y), (cx, base_y - height)])
    shingles(c, left, material_palette, base_y - height, base_y, rng, side_light=1)
    shingles(c, right, material_palette, base_y - height, base_y, rng, side_light=-1)
    c.paint(c.rect(cx - half, base_y - 2, cx + half, base_y + 2), material_palette[1])


def pennant(c, x, top, length=14):
    """뾰족 지붕 위 깃대와 뱀 깃발."""
    c.paint(c.rect(x - 1, top - 22, x + 1, top + 2), IRON[3])
    c.paint(c.ellipse(x, top - 23, 2, 2), GOLD[3])
    flag = c.polygon([(x + 1, top - 21), (x + 1 + length, top - 16), (x + 1, top - 11)])
    c.paint(flag, BANNER[3])
    c.paint(flag & (c.yy > top - 16), BANNER[2])


def tower(rng, width=72, body_h=150, roof="cone"):
    """둥근 망루: 돌 몸통, 창 둘, 위에 원뿔 비취 지붕(roof="cone") 또는 머론 띠(roof="crown")."""
    roof_h = int(width * 1.15) if roof == "cone" else 0
    height = roof_h + body_h + 40
    c = Canvas(width + 16, height)
    x0, x1 = 8, 8 + width
    ground = height - 2
    top = ground - body_h
    cylinder(c, x0, x1, top, ground, rng)
    c.paint(c.rect(x0, ground - 8, x1, ground), STONE[2])
    c.paint(c.rect(x0, ground - 8, x1, ground - 7), STONE[4])
    lit = Design("tower", 1, lit=True)
    cx = (x0 + x1) // 2
    window(c, cx, top + 30, lit, wide=12, tall=20, arched=True, flowers=False)
    arrow_slit(c, cx, top + 86)
    if roof == "cone":
        c.paint(c.rect(x0 - 2, top - 4, x1 + 2, top + 4), STONE[5])
        c.paint(c.rect(x0 - 2, top + 3, x1 + 2, top + 4), STONE[2])
        cone_roof(c, cx, top - 2, width // 2 + 7, roof_h, JADE, rng)
        pennant(c, cx, top - 2 - roof_h)
    else:
        battlements(c, x0 - 4, x1 + 4, top, rng, merlon_w=9, gap=7)
    moss_at_base(c, x0, x1, ground - 1, rng)
    return outline(c.image), (8 + width // 2, ground)


# ── 성문 ───────────────────────────────────────────────

GATE_W, GATE_H = 54, 66


def gatehouse(rng, closed):
    """
    성문: 성벽보다 높은 문루 가운데 둥근 아치. 아치 안은 비워 맵 바닥(성문 통로)이 보인다.
    열린 문은 쇠창살을 아치 꼭대기로 끌어 올려 이빨만 보이고, 닫힌 문은 창살이 아치를 막는다(창살 사이로 안쪽이 보인다).
    아치 위에 뱀 문장 돋을새김, 양옆에 비취 깃발.
    """
    width = 6 * T
    face = FACE + 34
    height = MERLON + 3 + WALK + face + 2
    c = Canvas(width, height)
    front_y = MERLON + 3 + WALK
    ground = front_y + face
    c.paint(c.rect(0, front_y, width, ground), masonry(height, width, rng))
    for x0 in (0, width - 24):
        c.paint(c.rect(x0, front_y, x0 + 24, ground), masonry(height, width, rng, course=(9, 12), block=(10, 16)))
        c.paint(c.rect(x0, front_y, x0 + 1, ground), STONE[5])
        c.paint(c.rect(x0 + 23, front_y, x0 + 24, ground), STONE[1])
    c.paint(c.rect(0, ground - 8, width, ground), STONE[2])
    c.paint(c.rect(0, ground - 8, width, ground - 7), STONE[4])
    battlements(c, 0, width, front_y, rng)
    cx = width // 2
    gx0, gx1 = cx - GATE_W // 2, cx + GATE_W // 2
    gate_top = ground - GATE_H
    # 아치 돌테와 쐐기돌
    rim = c.arch(gx0 - 8, gate_top - 8, gx1 + 8, ground)
    c.paint(rim, STONE[5])
    voussoir = rim & (np.round(np.degrees(np.arctan2(c.yy + 0.5 - (gate_top + GATE_W / 2), c.xx + 0.5 - cx))) % 18 < 2)
    c.paint(voussoir, STONE[3])
    c.paint(c.rect(cx - 5, gate_top - 10, cx + 5, gate_top + 2), STONE[6])
    opening = c.arch(gx0, gate_top, gx1, ground)
    # 통로 천장 그늘: 아치 안쪽 위가 어둡다
    c.paint(opening, STONE[0])
    c.image[opening & (c.yy > gate_top + 26), 3] = 0
    shade_band = opening & (c.yy > gate_top + 18) & (c.yy <= gate_top + 26)
    c.paint(shade_band, STONE[1])
    bars_y0 = gate_top + (0 if closed else 0)
    bars_y1 = ground - 2 if closed else gate_top + 22
    grid = opening & (c.yy >= bars_y0) & (c.yy < bars_y1) & (((c.xx - gx0) % 9 < 2) | ((c.yy - gate_top) % 12 < 2))
    c.paint(grid, IRON[1])
    c.paint(grid & ((c.xx - gx0) % 9 == 0), IRON[3])
    teeth = opening & (c.yy >= bars_y1) & (c.yy < bars_y1 + 5) & ((c.xx - gx0) % 9 < 2)
    c.paint(teeth, IRON[2])
    if closed:
        # 닫힌 창살 사이는 비워 둔다(안쪽 통로가 보인다)
        holes = opening & ~grid & ~teeth & (c.yy >= gate_top + 26)
        c.image[holes, 3] = 0
    # 아치 위 뱀 문장과 양옆 깃발
    serpent_emblem(c, cx, front_y + 14, 11)
    for bx in (gx0 - 30, gx1 + 16):
        flag_banner(c, bx, front_y + 6)
    moss_at_base(c, 0, gx0 - 8, ground - 1, rng)
    moss_at_base(c, gx1 + 8, width, ground - 1, rng)
    return outline(c.image), (width // 2, ground)


def serpent_emblem(c, cx, cy, r):
    """뱀 문장 돋을새김: 둥근 판에 S자로 똬리 튼 금빛 뱀."""
    c.paint(c.ellipse(cx, cy, r + 2, r + 2), STONE[2])
    c.paint(c.ellipse(cx, cy, r, r), BANNER[2])
    t = np.linspace(-1, 1, 60)
    for k, tk in enumerate(t):
        x = cx + np.sin(tk * np.pi * 1.2) * r * 0.45
        y = cy + tk * r * 0.75
        c.paint(c.ellipse(x, y, 2.2, 2.2), GOLD[2] if k % 6 else GOLD[3])
    c.paint(c.ellipse(cx + np.sin(-1.2 * np.pi) * r * 0.45, cy - r * 0.78, 3, 2.5), GOLD[3])


def flag_banner(c, x, y, length=48):
    """비취 깃발: 쇠 막대, 긴 천, 금빛 뱀 문양, 제비 꼬리."""
    c.paint(c.rect(x - 3, y, x + 17, y + 3), IRON[2])
    body = c.rect(x, y + 3, x + 14, y + length)
    notch = c.polygon([(x - 1, y + length), (x + 15, y + length), (x + 7, y + length - 7)])
    cloth = body & ~notch
    c.paint(cloth, BANNER[2])
    c.paint(cloth & (c.xx < x + 2), BANNER[3])
    c.paint(cloth & (c.xx >= x + 12), BANNER[1])
    c.paint(cloth & ((c.yy == y + 7) | (c.yy == y + length - 12)), GOLD[2])
    for k in range(18):
        sx = x + 7 + np.sin(k / 17 * np.pi * 2) * 3
        c.paint(c.rect(int(sx) - 1, y + 13 + k, int(sx) + 1, y + 14 + k), GOLD[3 if k < 3 else 2])


# ── 본관과 돌뱀 ─────────────────────────────────────────

def serpent_tube(c, points, radius, front_only=None, palette=SERPENT):
    """
    뱀 몸통: 점 줄을 따라 원판을 찍되, 칠한 점마다 몸통 길이 자리(along)와 가로 자리(across)를 기억한다.
    across는 진행 방향에 수직인 거리(−1 ~ +1)이고, 빛이 오는 왼쪽 위가 −다. 그래서 가로로 누운 마디도
    세로로 선 목도 빛 쪽은 밝고 반대쪽은 어둡다. 둘을 함께 써서 마름모 비늘을 칠한다.
    front_only가 주어지면 그 조건(점 번호 → 참/거짓)을 만족하는 마디만 칠한다(탑 앞으로 도는 마디).
    """
    height, width = c.size[1], c.size[0]
    along = np.full((height, width), np.nan)
    across = np.zeros((height, width))
    length, previous = 0.0, None
    for k, (x, y, r) in enumerate(points):
        if previous is not None:
            length += np.hypot(x - previous[0], y - previous[1])
        previous = (x, y)
        if front_only is not None and not front_only(k):
            continue
        ahead, behind = points[min(k + 1, len(points) - 1)], points[max(k - 1, 0)]
        tx, ty = ahead[0] - behind[0], ahead[1] - behind[1]
        norm = np.hypot(tx, ty) or 1.0
        nx, ny = -ty / norm, tx / norm
        if nx + ny < 0:
            nx, ny = -nx, -ny
        x0, x1 = max(0, int(x - r - 1)), min(width, int(x + r + 2))
        y0, y1 = max(0, int(y - r - 1)), min(height, int(y + r + 2))
        yy, xx = c.yy[y0:y1, x0:x1] + 0.5, c.xx[y0:y1, x0:x1] + 0.5
        disc = (xx - x) ** 2 + (yy - y) ** 2 <= r * r
        along[y0:y1, x0:x1][disc] = length
        across[y0:y1, x0:x1][disc] = (((xx - x) * nx + (yy - y) * ny) / r)[disc]
    body = ~np.isnan(along)
    v = across[body]
    level = np.select([v < -0.6, v < -0.2, v < 0.3, v < 0.7], [6, 5, 4, 3], 2)
    # 홈을 판 마름모 비늘: 두 방향 홈 줄은 어둡고, 홈 바로 뒤 비늘 끝은 빛을 받는다
    u = along[body] / 6.0
    w = (v + 1) * radius / 7.0
    g1, g2 = (u + w) % 1, (u - w) % 1
    groove = (g1 < 0.13) | (g2 < 0.13)
    edge = ((g1 >= 0.13) & (g1 < 0.24)) | ((g2 >= 0.13) & (g2 < 0.24))
    level = np.where(groove, level - 2, np.where(edge & (v < 0.3), level + 1, level))
    c.image[body, :3] = palette[np.clip(level, 1, len(palette) - 1)]
    c.image[body, 3] = 255
    weather(c, body, across, np.random.default_rng(len(points)))
    c.paint(body & pg.neighbours(~body), palette[0])


def weather(c, body, across, rng):
    """돌 조각의 풍화: 빛 받는 윗면에 이끼 덩어리, 아랫면에 흘러내린 물때."""
    height, width = c.size[1], c.size[0]
    field = pg.noise(height, width, 9, rng)
    moss = body & (across < -0.2) & (field > 0.74)
    c.paint(moss, serpent_sculpture.MOSS[1])
    c.paint(moss & (field > 0.8), serpent_sculpture.MOSS[2])
    stain = body & (across > 0.35) & (pg.noise(height, width, 4, rng) > 0.7)
    c.image[stain, :3] = (c.image[stain, :3] * 0.8).astype(np.uint8)


def coiled_serpent(c, cx, top, bottom, radius_x, thickness, tower_draw, turns=2.0, start=0.0):
    """
    탑을 감은 돌뱀. 탑 뒤로 도는 마디를 먼저 그리고, 탑을 그린 뒤, 앞으로 도는 마디를 그린다.
    위쪽 끝(start 각도, 0이면 탑 앞, π이면 탑 뒤)에서 아래로 내려가며 꼬리 쪽이 가늘어진다. 돌려주는 값: 위쪽 끝 자리.
    """
    steps = 1200
    points, front = [], []
    for k in range(steps):
        s = k / (steps - 1)
        angle = start + s * turns * 2 * np.pi
        x = cx + np.sin(angle) * radius_x
        y = top + s * (bottom - top) + np.cos(angle) * 6
        r = thickness * (1.0 - 0.55 * s ** 2)
        points.append((x, y, r))
        # 탑 옆에서 앞뒤가 바뀌는 곳은 탑 밖이라 앞 마디를 조금 더 그려 이음매가 잘려 보이지 않게 한다
        front.append(np.cos(angle) > -0.3)
    serpent_tube(c, points, thickness, lambda k: not front[k])
    tower_draw()
    serpent_tube(c, points, thickness, lambda k: front[k])
    return points[0]


def paste(c, image, left, top):
    """다른 그림을 캔버스에 얹는다(불투명한 점만)."""
    h, w = image.shape[:2]
    region = c.image[top:top + h, left:left + w]
    opaque = image[..., 3] > 0
    region[opaque] = image[opaque]


def serpent_tower(rng):
    """
    뱀성의 탑(성이 아니라 탑, 꼭대기는 뱀 머리): 넓은 받침 위 둥근 돌탑에 층 띠 둘과 나선으로 오르는 창,
    꼭대기에 비취 원뿔 지붕. 크림색 얼룩 뱀이 꼬리부터 두 바퀴 반을 감아 오르다 지붕 뒤에서 목을 세우고,
    지붕 위로 거대한 머리를 내밀어 아래를 내려다본다. 머리는 따로 그린 정면 조각(serpent_sculpture.head_front)이다.
    아래에 큰 쌍여닫이 문(탑 입구)과 뱀 문장.
    """
    tower_w, body_h = 196, 330
    roof_h, roof_half = 150, tower_w // 2 + 14
    head, head_pivot = serpent_sculpture.head_front(184, rng)
    coil_reach = tower_w // 2 + 18 + 30
    width = 2 * coil_reach + 40
    height = body_h + roof_h + head.shape[0] + 10
    c = Canvas(width, height)
    cx = width // 2
    ground = height - 4
    top = ground - body_h
    tx0, tx1 = cx - tower_w // 2, cx + tower_w // 2
    lit = Design("tower", 1, lit=True, shutters="", flowers=False)
    # 머리 아래 끝(엄니)이 지붕 꼭대기 앞에 걸린다
    head_left, head_top = cx - head_pivot[0], top - roof_h - head.shape[0] + 46
    neck_end = (cx, head_top + head.shape[0] * 0.45)

    def draw_tower():
        # 지붕 뒤로 솟는 목: 탑과 지붕보다 먼저 그려 아랫부분이 가려진다
        neck = []
        for u in np.linspace(0, 1, 240):
            nx = cx + 50 * np.sin(u * np.pi)
            ny = top + 40 + (neck_end[1] - top - 40) * u
            neck.append((nx, ny, 34 - 4 * u))
        serpent_tube(c, neck, 32)
        cylinder(c, tx0, tx1, top, ground, rng)
        for inset, h in ((-14, 30), (-7, 44)):
            base = c.rect(tx0 + inset, ground - h, tx1 - inset, ground - h + 14)
            c.paint(base, STONE[4])
            c.paint(base & (c.yy == ground - h), STONE[6])
            c.paint(base & (c.yy == ground - h + 13), STONE[1])
        c.paint(c.rect(tx0 - 14, ground - 16, tx1 + 14, ground), STONE[3])
        c.paint(c.rect(tx0 - 14, ground - 16, tx1 + 14, ground - 15), STONE[5])
        for y in (top + 110, top + 220):
            band = c.rect(tx0 - 5, y, tx1 + 5, y + 7)
            c.paint(band, STONE[5])
            c.paint(band & (c.yy == y), STONE[7])
            c.paint(band & (c.yy == y + 6), STONE[2])
        for k in range(7):
            wy = ground - 92 - k * 40
            wx = cx + np.sin(k * 1.05 + 0.6) * tower_w * 0.3
            if wy > top + 30:
                window(c, int(wx), int(wy), lit, wide=10, tall=18, arched=True, flowers=False)
        # 처마 띠와 원뿔 지붕, 꼭대기 쇠 장식
        c.paint(c.rect(tx0 - 8, top - 6, tx1 + 8, top + 6), STONE[5])
        c.paint(c.rect(tx0 - 8, top + 5, tx1 + 8, top + 6), STONE[2])
        cone_roof(c, cx, top - 4, roof_half, roof_h, JADE, rng)
        c.paint(c.rect(cx - 2, top - 4 - roof_h - 20, cx + 2, top - 4 - roof_h + 4), IRON[3])
        c.paint(c.ellipse(cx, top - 4 - roof_h - 22, 4, 4), GOLD[3])

    coiled_serpent(c, cx, top + 40, ground - 70, tower_w // 2 + 18, 30, draw_tower, turns=2.5, start=np.pi)
    paste(c, head, head_left, head_top)
    door(c, cx, ground, "double")
    serpent_emblem(c, cx, ground - 96, 11)
    moss_at_base(c, tx0 - 14, tx1 + 14, ground - 1, rng)
    return outline(c.image), (cx, ground)


# ── 작은 것 ────────────────────────────────────────────

def serpent_statue(rng):
    """뱀 석상: 네모 받침 위에 똬리를 틀고 머리를 든 돌뱀(탑 꼭대기 머리와 같은 정면 조각을 작게)."""
    w, h = 48, 84
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(cx - 18, ground - 18, cx + 18, ground), STONE[4])
    c.paint(c.rect(cx - 18, ground - 18, cx + 18, ground - 15), STONE[6])
    c.paint(c.rect(cx - 18, ground - 3, cx + 18, ground), STONE[2])
    c.paint(c.rect(cx + 14, ground - 15, cx + 18, ground - 3), STONE[3])
    coil = []
    for k in range(240):
        s = k / 239
        a = s * 2.2 * 2 * np.pi
        coil.append((cx + np.sin(a) * (14 - s * 8), ground - 24 - s * 22 + np.cos(a) * 3, 6 - s * 1.5))
    serpent_tube(c, coil[::-1], 6)
    neck = [(cx + np.sin(s * 2) * 3, ground - 46 - s * 16, 4.5) for s in np.linspace(0, 1, 30)]
    serpent_tube(c, neck, 4)
    head, pivot = serpent_sculpture.head_front(30, rng)
    paste(c, head, cx - pivot[0], ground - 62 - head.shape[0] // 2)
    return outline(c.image), (cx, ground)


def glider(rng):
    """
    뱀 머리 위에 묶어 둔 글라이더(2장 탈출): 나무 뼈대에 기운 천 날개(위에서 비스듬히 본 넓은 세모), 손잡이 막대, 묶은 밧줄.
    """
    w, h = 104, 58
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    cloth = pg.hexes("#5a4a30", "#8a7450", "#b49c70", "#d8c49a")
    wing = c.polygon([(cx, 4), (w - 3, ground - 14), (cx, ground - 22), (3, ground - 14)])
    c.paint(wing, cloth[2])
    c.paint(wing & (c.xx < cx), cloth[3])
    c.paint(wing & (c.yy > ground - 24), cloth[1])
    for px, py in ((cx - 26, 22), (cx + 18, 26)):
        c.paint(c.rect(px, py, px + 12, py + 9) & wing, cloth[1])
        c.paint(c.rect(px, py, px + 12, py + 1) & wing, cloth[0])
    c.line(cx, 4, cx, ground - 20, WOOD[1], 2)
    c.line(3, ground - 14, w - 3, ground - 14, WOOD[2], 2)
    c.line(cx - 14, ground - 18, cx - 10, ground - 4, WOOD[2], 2)
    c.line(cx + 14, ground - 18, cx + 10, ground - 4, WOOD[2], 2)
    c.line(cx - 12, ground - 4, cx + 12, ground - 4, WOOD[3], 2)
    c.line(cx + 12, ground - 4, cx + 30, ground, pg.hexes("#c8b88a")[0], 1)
    return outline(c.image), (cx, ground)


def brazier(rng):
    """화로: 쇠 다리 셋 위의 넓은 그릇에 숯불. 불꽃은 맵의 움직이는 불(fire)이 그 위에 얹힌다."""
    w, h = 30, 42
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for dx in (-9, 0, 9):
        c.line(cx + dx * 0.4, ground - 22, cx + dx, ground - 1, IRON[1], 2)
    bowl = c.ellipse(cx, ground - 26, 13, 6) | c.polygon([(cx - 13, ground - 26), (cx + 13, ground - 26), (cx + 8, ground - 19), (cx - 8, ground - 19)])
    c.paint(bowl, IRON[2])
    c.paint(bowl & (c.xx < cx - 4), IRON[3])
    c.paint(c.ellipse(cx, ground - 27, 11, 4), EMBER[1])
    c.paint(c.ellipse(cx, ground - 27, 7, 2.5), EMBER[2])
    return outline(c.image), (cx, ground)


# ── 견본 ───────────────────────────────────────────────

def catalog(project):
    import terrain
    rng = np.random.default_rng(3)
    pieces = [
        ("wall", *curtain_wall(5, np.random.default_rng(1))),
        ("gate_open", *gatehouse(np.random.default_rng(2), closed=False)),
        ("gate_closed", *gatehouse(np.random.default_rng(2), closed=True)),
        ("tower", *tower(np.random.default_rng(4))),
        ("crown", *tower(np.random.default_rng(5), width=64, body_h=130, roof="crown")),
        ("statue", *serpent_statue(np.random.default_rng(6))),
        ("brazier", *brazier(np.random.default_rng(7))),
        ("side", *side_wall(np.random.default_rng(8))),
    ]
    keep_image, keep_pivot = serpent_tower(np.random.default_rng(9))
    gap = 20
    row1 = pieces
    width = max(sum(img.shape[1] for _, img, _ in row1) + gap * (len(row1) + 1), keep_image.shape[1] + 40)
    h1 = max(img.shape[0] for _, img, _ in row1) + 30
    h2 = keep_image.shape[0] + 30
    ground = terrain.cobble(h1 + h2, width, rng)
    scene = Image.fromarray(np.dstack([ground, np.full(ground.shape[:2], 255, np.uint8)]), "RGBA")
    x = gap
    for _, img, pivot in row1:
        scene.alpha_composite(Image.fromarray(img, "RGBA"), (x, h1 - 10 - pivot[1]))
        x += img.shape[1] + gap
    scene.alpha_composite(Image.fromarray(keep_image, "RGBA"), (20, h1 + h2 - 10 - keep_pivot[1]))
    scene = scene.resize((scene.width * 2, scene.height * 2), Image.NEAREST)
    hero = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/hero/down_0.png").convert("RGBA")
    door_x = (20 + keep_pivot[0]) * 2
    scene.alpha_composite(hero, (door_x - hero.width // 2 + 120, (h1 + h2 - 10) * 2 - hero.height + 10))
    path = project / "Docs/ArtUpgrade/PixelGen/castle_catalog.png"
    scene.save(path)
    print(path)


if __name__ == "__main__":
    catalog(Path(sys.argv[1]).resolve())
