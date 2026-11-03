"""강을 남북으로 건너는 나무다리(사용자 요청 2026-11-03: 뱀성 앞 다리 품질).

  deck(columns, rows, rng)   다리 바닥(바닥에 까는 그림): 가로로 놓인 두꺼운 판자(판자마다 결과 밝기가 다르고 틈은 어둡다),
                             양옆 받침 들보와 못, 가운데 밟혀 닳은 자국, 양 끝 물가에 얹은 돌 문턱, 물에 드리운 그림자와 물결
  rail(length, rng)          난간 한 칸(세우는 것): 아래 기둥 하나와 그 기둥에서 북쪽 기둥까지 이어진 들보
  lantern_post(rng)          다리 네 귀퉁이의 굵은 기둥: 쇠 고리에 매단 등불

위에서 비스듬히 본 시점이라 남북으로 놓인 난간 들보는 화면에서 세로 띠로 보인다. 난간은 기둥마다 한 칸씩 나눠 세워
기둥 앞뒤로 인물이 제대로 가려진다. 점 격자(한 칸 32점)에 그린다.
"""
import numpy as np

import pixelgen as pg
from houses import IRON, WARM, WOOD, Canvas, outline

T = pg.TILE
SIDE = 4                # 양옆 받침 들보 너비(점)
PLANK = 7               # 판자 한 장 폭(틈 포함)
SILL = 9                # 양 끝 돌 문턱 깊이(점)
MARGIN = 6              # 바닥 그림 양옆 물 위 여백(그림자와 물결)
POST_HEIGHT = 22        # 난간 기둥이 바닥에서 솟은 높이(점)
WATER_SHADOW = np.array([8, 18, 32], dtype=np.uint8)
FOAM = pg.WATER[6]


def deck(columns, rows, rng):
    """
    다리 바닥(왼쪽 위 기준, 왼쪽에 MARGIN점 물 여백). 판자는 걷는 방향(남북)을 가로질러 놓인다.
    돌려주는 값은 RGBA 그림이다. 그림 너비는 columns칸 + 양옆 여백, 높이는 rows칸이다.
    """
    w, h = columns * T, int(round(rows * T))
    image = np.zeros((h, w + MARGIN * 2, 4), dtype=np.uint8)
    deck_rgb = np.zeros((h, w, 3), dtype=np.uint8)
    grain = pg.fractal_noise(h, w, rng, ((6, 0.6), (2, 0.4)))
    for y0 in range(0, h, PLANK):
        level = int(rng.choice([2, 3, 3, 4]))
        y1 = min(h, y0 + PLANK)
        deck_rgb[y0:y1] = WOOD[level]
        deck_rgb[y0] = WOOD[min(5, level + 1)]
        deck_rgb[y1 - 2:y1 - 1] = WOOD[max(1, level - 1)]
        deck_rgb[y1 - 1:y1] = WOOD[0]
        # 판자 결: 가로로 짧게 끊긴 어두운 줄
        for _ in range(int(w / 10)):
            gx, gy = int(rng.integers(0, w - 6)), int(rng.integers(y0 + 1, max(y0 + 2, y1 - 2)))
            deck_rgb[gy, gx:gx + int(rng.integers(3, 8))] = WOOD[max(1, level - 1)]
        # 판자 끝 못(받침 들보 위)
        for nx in (SIDE + 2, w - SIDE - 3):
            deck_rgb[y0 + 2:y0 + 3, nx:nx + 1] = IRON[1]
            deck_rgb[y0 + 4:y0 + 5, nx:nx + 1] = IRON[1]
    # 가운데 밟혀 닳은 자국(밝게, 성기게)
    wear = (np.abs(np.arange(w) - w / 2) < w * 0.22)[None, :] & (pg.bayer(h, w) < 0.4) & (grain > 0.45)
    deck_rgb[wear] = np.minimum(deck_rgb[wear].astype(int) + 18, 255)
    # 양옆 받침 들보와 볼트
    deck_rgb[:, :SIDE] = WOOD[1]
    deck_rgb[:, w - SIDE:] = WOOD[1]
    deck_rgb[:, SIDE - 1] = WOOD[0]
    deck_rgb[:, w - SIDE] = WOOD[0]
    for by in range(5, h, 16):
        deck_rgb[by:by + 2, 1:3] = IRON[3]
        deck_rgb[by:by + 2, w - 3:w - 1] = IRON[3]
    # 양 끝 물가에 얹은 돌 문턱
    for y0, y1 in ((0, SILL), (h - SILL, h)):
        x = 0
        while x < w:
            stone_w = int(rng.integers(10, 18))
            level = int(rng.choice([2, 3, 4]))
            deck_rgb[y0:y1, x:x + stone_w] = pg.STONE[level]
            deck_rgb[y0, x:x + stone_w] = pg.STONE[min(5, level + 1)]
            deck_rgb[y0:y1, x:x + 1] = pg.STONE[1]
            x += stone_w
        deck_rgb[y1 - 1 if y0 == 0 else y0] = pg.STONE[1]
    image[:, MARGIN:MARGIN + w, :3] = deck_rgb
    image[:, MARGIN:MARGIN + w, 3] = 255
    # 물 위: 오른쪽(동쪽)으로 다리 그림자, 양옆 들보가 물에 닿는 곳의 물결
    water_rows = slice(SILL, h - SILL)
    image[water_rows, MARGIN + w:MARGIN + w + MARGIN - 1, :3] = WATER_SHADOW
    image[water_rows, MARGIN + w:MARGIN + w + MARGIN - 1, 3] = 110
    for fy in range(SILL + 3, h - SILL - 2, 6):
        for fx in (MARGIN - 4, MARGIN + w + 1):
            length = int(rng.integers(2, 4))
            image[fy, fx:fx + length, :3] = FOAM
            image[fy, fx:fx + length, 3] = 200
    return image


def rail(length, rng, post=True):
    """
    난간 한 칸: 아래(남쪽) 기둥과, 그 기둥 꼭대기에서 length점 북쪽 기둥 꼭대기까지 이어진 들보(화면에서 세로 띠).
    post가 거짓이면 들보만 그린다(그 자리에 등불 기둥이 선다). 기준점은 기둥 아래 가운데다.
    """
    w, h = 8, length + POST_HEIGHT + 2
    c = Canvas(w, h)
    ground = h - 1
    beam_top, beam_bottom = 1, length + 2
    c.paint(c.rect(2, beam_top, 6, beam_bottom), WOOD[2])
    c.paint(c.rect(2, beam_top, 3, beam_bottom), WOOD[4])
    c.paint(c.rect(5, beam_top, 6, beam_bottom), WOOD[1])
    if post:
        post_top = ground - POST_HEIGHT
        c.paint(c.rect(1, post_top, 7, ground), WOOD[3])
        c.paint(c.rect(1, post_top, 3, ground), WOOD[4])
        c.paint(c.rect(6, post_top, 7, ground), WOOD[1])
        c.paint(c.rect(1, post_top, 7, post_top + 2), WOOD[5])
        c.paint(c.rect(1, post_top + 7, 7, post_top + 9), IRON[2])
    return outline(c.image), (w // 2, ground)


def lantern_post(rng, side=1):
    """
    다리 귀퉁이의 굵은 기둥: 뾰족한 머리, 쇠 띠, 바깥쪽 쇠 팔에 매단 불 켜진 등불. side는 등불이 달린 쪽(1 동쪽, -1 서쪽).
    기준점은 기둥 아래 가운데다.
    """
    w, h = 22, 44
    c = Canvas(w, h)
    ground = h - 1
    cx = 8
    top = ground - 32
    c.paint(c.rect(cx - 4, top, cx + 4, ground), WOOD[3])
    c.paint(c.rect(cx - 4, top, cx - 2, ground), WOOD[4])
    c.paint(c.rect(cx + 3, top, cx + 4, ground), WOOD[1])
    c.paint(c.polygon([(cx - 5, top + 1), (cx + 5, top + 1), (cx, top - 5)]), WOOD[2])
    for band in (top + 8, ground - 8):
        c.paint(c.rect(cx - 5, band, cx + 5, band + 2), IRON[2])
    c.paint(c.rect(cx + 4, top + 3, cx + 9, top + 4), IRON[2])
    lx, ly = cx + 9, top + 4
    c.paint(c.rect(lx - 3, ly + 2, lx + 4, ly + 3), IRON[2])
    c.paint(c.rect(lx - 2, ly + 3, lx + 3, ly + 11), WARM[3])
    c.paint(c.rect(lx - 1, ly + 4, lx + 2, ly + 9), WARM[4])
    c.paint(c.rect(lx - 3, ly + 11, lx + 4, ly + 12), IRON[2])
    image = outline(c.image)
    if side < 0:
        return image[:, ::-1].copy(), (w - 1 - cx, ground)
    return image, (cx, ground)
