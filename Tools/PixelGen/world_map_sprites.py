"""세계지도(world_map.py)에 세우는 작은 그림들. 모두 테두리가 있는 RGBA 점 그림이고, 발(맨 아래 가운데)을 기준으로 찍는다."""
import numpy as np

import pixelgen as pg
from houses import Canvas

INK_LINE = pg.hexes("#0e1820")[0]
SMOKE = pg.hexes("#5d5f68", "#858791", "#aeb0b7", "#d4d5d9")
GOLD = pg.hexes("#5a4418", "#8a6c2c", "#b8954e", "#e2c26e", "#f4e3a8")
EMBER = pg.hexes("#5e1a12", "#a02f14", "#de5a18", "#f7a52a", "#ffe08a")
SNOW_CAP = pg.hexes("#c6d4e0", "#f2f7fa")
PINE = pg.hexes("#1d342a", "#335a42", "#dfe9f0")
GLASS = pg.hexes("#16233a", "#223a58", "#33597c", "#5a93b0", "#b4eef4")
STEEL = pg.hexes("#2a2f3a", "#444b58", "#6a7382", "#98a2b0", "#d0d8e0")
BEACON = pg.hexes("#ff4a3c")[0]
ARMY = pg.hexes("#262b1e", "#3c4430", "#566043", "#737e58", "#98a378")
JADE = pg.hexes("#1f7a62", "#2f9e80", "#55c0a0")
TILE = pg.hexes("#1c2630", "#2f4250", "#4c6878", "#7c98a6")
TIMBER = pg.hexes("#5a1f18", "#8e2f24", "#bf4a34")
PLASTER = pg.hexes("#b8ac94", "#e8dfc8")
ROCK = pg.hexes("#3f4a52", "#5f6e74", "#8a9a9a", "#b6c4be")
MIST = pg.hexes("#c4d2d4", "#eef3f0", "#fbfdfb")


def sprite(canvas):
    return pg.outline(canvas.image, INK_LINE)


def mountain(width, height, ramp, rng, snow=False):
    """왼쪽 면이 밝고 오른쪽 면이 어두운 뾰족한 산. 능선은 꼭대기에서 지그재그로 내려온다."""
    c = Canvas(width + 2, height + 2)
    apex = width // 2 + int(rng.integers(-1, 2))
    ridge = apex
    for y in range(height):
        t = (y + 1) / height
        left = int(round(apex - apex * t))
        right = int(round(apex + (width - 1 - apex) * t))
        ridge = int(np.clip(ridge + rng.integers(-1, 2), left, right))
        row = y + 1
        c.image[row, left + 1:ridge + 2, :3] = ramp[-1]
        c.image[row, ridge + 2:right + 2, :3] = ramp[1]
        c.image[row, left + 1:right + 2, 3] = 255
        if snow and t < 0.42 + 0.12 * np.sin(y * 2.1):
            c.image[row, left + 1:ridge + 2, :3] = SNOW_CAP[1]
            c.image[row, ridge + 2:right + 2, :3] = SNOW_CAP[0]
    return sprite(c)


def round_tree(ramp):
    c = Canvas(7, 8)
    c.paint(c.rect(3, 5, 4, 7), pg.BARK[2])
    c.paint(c.ellipse(3.5, 3, 3, 2.8), ramp[1])
    c.paint(c.ellipse(3.0, 2.4, 1.9, 1.5), ramp[3])
    c.paint(c.rect(2, 1, 3, 2), ramp[-1])
    return sprite(c)


def snowy_pine():
    c = Canvas(7, 10)
    c.paint(c.rect(3, 7, 4, 9), pg.BARK[2])
    c.paint(c.polygon([(3.5, 0), (6.4, 8), (0.6, 8)]), PINE[1])
    c.paint(c.polygon([(3.5, 0), (5, 3.8), (2, 3.8)]), PINE[2])
    c.paint(c.rect(4, 5, 6, 8) & c.polygon([(3.5, 0), (6.4, 8), (0.6, 8)]), PINE[0])
    return sprite(c)


def shrub(ramp):
    c = Canvas(5, 4)
    c.paint(c.ellipse(2.5, 2.2, 2.2, 1.6), ramp[1])
    c.paint(c.rect(1, 1, 3, 2), ramp[3])
    return sprite(c)


# ── 팬데모니엄 ───────────────────────────────────────────

def gear(radius):
    size = radius * 2 + 7
    centre = size / 2
    c = Canvas(size, size)
    for k in range(8):
        angle = k * np.pi / 4
        x, y = centre + np.cos(angle) * (radius + 1.5), centre + np.sin(angle) * (radius + 1.5)
        c.paint(c.ellipse(x, y, 1.6, 1.6), pg.STONE[2])
    c.paint(c.ellipse(centre, centre, radius + 0.5, radius + 0.5), pg.STONE[3])
    c.paint(c.ellipse(centre - 1, centre - 1, radius - 1.5, radius - 1.5), pg.STONE[4])
    c.paint(c.ellipse(centre, centre, radius * 0.45, radius * 0.45), pg.STONE[0])
    return sprite(c)


def chimney():
    c = Canvas(14, 28)
    c.paint(c.rect(2, 3, 12, 27), pg.STONE[2])
    c.paint(c.rect(2, 3, 5, 27), pg.STONE[3])
    c.paint(c.rect(9, 3, 12, 27), pg.STONE[1])
    for y in range(7, 26, 4):
        c.line(5, y + 3, 9, y, pg.STONE[0])
    c.paint(c.ellipse(7, 3, 5, 2.4), pg.STONE[1])
    c.paint(c.ellipse(7, 3, 3.4, 1.4), pg.STONE[0])
    return sprite(c)


def sawtooth_hall():
    c = Canvas(18, 15)
    c.paint(c.rect(1, 7, 17, 14), pg.STONE[2])
    for x in range(1, 16, 5):
        c.paint(c.polygon([(x, 8), (x + 5, 8), (x + 5, 2)]), pg.STONE[3])
    for x in range(3, 16, 4):
        c.paint(c.rect(x, 9, x + 2, 12), EMBER[3])
    return sprite(c)


def smoke_plume():
    """굴뚝에서 오른쪽 위로 번지는 연기. 스케치처럼 위로 갈수록 넓게 퍼진다."""
    c = Canvas(40, 22)
    puffs = [(5, 18, 3.2), (10, 15, 4.2), (16, 12, 4.8), (22, 11, 5.2), (28, 8, 4.6), (34, 7, 3.8)]
    for x, y, r in puffs:
        c.paint(c.ellipse(x, y, r + 0.6, r * 0.8), SMOKE[0])
    for x, y, r in puffs:
        c.paint(c.ellipse(x - 0.8, y - 0.8, r - 0.6, r * 0.62), SMOKE[1])
    for x, y, r in puffs:
        c.paint(c.ellipse(x - 1.4, y - 1.4, r - 1.8, r * 0.34), SMOKE[3])
    return c.image.copy()


# ── 원더랜드 ───────────────────────────────────────────

def temple():
    """원더랜드의 황금 신전: 삼각 지붕, 기둥 넷, 계단."""
    c = Canvas(34, 30)
    c.paint(c.polygon([(17, 1), (32, 11), (2, 11)]), GOLD[3])
    c.paint(c.polygon([(17, 4), (27, 10.5), (7, 10.5)]), GOLD[1])
    c.paint(c.polygon([(17, 1), (32, 11), (17, 11)]) & c.rect(24, 0, 34, 12), GOLD[2])
    c.paint(c.rect(3, 11, 31, 13), GOLD[4])
    c.paint(c.rect(5, 13, 29, 25), GOLD[0])
    for x in (6, 12, 19, 25):
        c.paint(c.rect(x, 13, x + 3, 25), GOLD[3])
        c.paint(c.rect(x + 2, 13, x + 3, 25), GOLD[2])
    c.paint(c.rect(4, 25, 30, 27), GOLD[3])
    c.paint(c.rect(2, 27, 32, 29), GOLD[2])
    return sprite(c)


# ── 아이로스: 최첨단 도시와 군사 지역 ─────────────────────

def lit_windows(c, x0, y0, x1, y1, rng, lit=0.6):
    """층마다 불 켜진 유리창을 점으로 찍는다."""
    for y in range(y0, y1, 2):
        for x in range(x0, x1, 2):
            if rng.random() < lit:
                c.image[y, x, :3] = GLASS[4]


def glass_tower(width, height, rng, spire=0):
    """유리 고층 건물. 오른쪽 면이 어둡고 꼭대기에 빨간 표시등이 있다."""
    c = Canvas(width + 2, height + spire + 2)
    left, top, right, bottom = 1, spire + 1, width + 1, height + spire + 1
    c.paint(c.rect(left, top, right, bottom), GLASS[2])
    c.paint(c.rect(left + width * 2 // 3, top, right, bottom), GLASS[1])
    c.paint(c.rect(left, top, left + 1, bottom), GLASS[3])
    lit_windows(c, left + 1, top + 2, right - 1, bottom - 1, rng)
    c.paint(c.rect(left, top, right, top + 1), STEEL[4])
    if spire:
        middle = left + width // 2
        c.paint(c.rect(middle, 1, middle + 1, top), STEEL[3])
        c.paint(c.rect(middle, 0, middle + 1, 1), BEACON)
    return sprite(c)


def erad_tower(rng):
    """스케치의 격자 고층 건물 자리에 서는 에라드의 탑: 세 단으로 좁아지고 꼭대기에 첨탑이 있다."""
    width, height = 22, 50
    c = Canvas(width, height)
    for x0, x1, y0, y1 in ((2, 20, 26, 49), (4, 18, 14, 26), (6, 16, 7, 14)):
        c.paint(c.rect(x0, y0, x1, y1), GLASS[2])
        c.paint(c.rect((x0 + x1) // 2 + 2, y0, x1, y1), GLASS[1])
        c.paint(c.rect(x0, y0, x0 + 1, y1), GLASS[3])
        lit_windows(c, x0 + 1, y0 + 2, x1 - 1, y1 - 1, rng, lit=0.75)
        c.paint(c.rect(x0, y0, x1, y0 + 1), STEEL[4])
    c.paint(c.rect(10, 2, 12, 7), STEEL[3])
    c.paint(c.rect(10, 0, 11, 2), BEACON)
    c.paint(c.rect(8, 44, 14, 49), GLASS[0])
    c.paint(c.rect(9, 45, 13, 49), GLASS[3])
    return sprite(c)


def twin_towers(rng):
    """스케치의 나란한 건물 두 동: 공중 다리로 이은 쌍둥이 고층 건물."""
    c = Canvas(20, 38)
    for x0, x1, top in ((1, 8, 3), (11, 18, 0)):
        c.paint(c.rect(x0, top, x1, 37), GLASS[2])
        c.paint(c.rect(x1 - 2, top, x1, 37), GLASS[1])
        c.paint(c.rect(x0, top, x0 + 1, 37), GLASS[3])
        lit_windows(c, x0 + 1, top + 2, x1 - 1, 36, rng)
        c.paint(c.rect(x0, top, x1, top + 1), STEEL[4])
    c.paint(c.rect(8, 15, 11, 17), STEEL[4])
    c.paint(c.rect(8, 16, 11, 17), STEEL[2])
    return sprite(c)


def civic_block(rng):
    """스케치의 낮고 긴 건물과 둥근 건물 자리: 유리 복합 건물과 돔."""
    c = Canvas(34, 24)
    c.paint(c.polygon([(1, 10), (20, 6), (20, 23), (1, 23)]), STEEL[3])
    for y in range(11, 22, 3):
        c.paint(c.rect(2, y, 20, y + 1), GLASS[2])
    lit_windows(c, 3, 11, 19, 22, rng, lit=0.35)
    c.paint(c.rect(20, 11, 32, 23), GLASS[2])
    c.paint(c.rect(20, 11, 23, 23), GLASS[3])
    c.paint(c.rect(28, 11, 32, 23), GLASS[1])
    c.paint(c.ellipse(26, 11, 6, 6) & c.rect(19, 4, 33, 11), GLASS[3])
    c.paint(c.ellipse(24.5, 8.5, 2.5, 2) & c.rect(19, 4, 33, 11), GLASS[4])
    c.paint(c.rect(20, 11, 32, 12), STEEL[4])
    return sprite(c)


def hangar():
    c = Canvas(18, 11)
    c.paint(c.ellipse(9, 10, 8.4, 9) & c.rect(0, 0, 18, 10), ARMY[2])
    c.paint(c.ellipse(7, 9, 5, 7) & c.rect(0, 0, 18, 10), ARMY[3])
    c.paint(c.rect(6, 5, 12, 10), ARMY[0])
    for x in range(7, 12, 2):
        c.paint(c.rect(x, 5, x + 1, 10), ARMY[1])
    return sprite(c)


def barracks():
    c = Canvas(11, 6)
    c.paint(c.rect(0, 2, 11, 6), ARMY[1])
    c.paint(c.polygon([(0, 2), (11, 2), (9, 0), (2, 0)]), ARMY[3])
    for x in (2, 5, 8):
        c.paint(c.rect(x, 3, x + 1, 5), GLASS[3])
    return sprite(c)


def radar():
    c = Canvas(11, 13)
    c.paint(c.rect(5, 6, 6, 12), STEEL[2])
    c.paint(c.rect(3, 11, 8, 13), STEEL[1])
    c.paint(c.ellipse(5.5, 3.5, 5, 3.4), STEEL[4])
    c.paint(c.ellipse(5.5, 3.0, 3.4, 2), STEEL[2])
    c.paint(c.rect(5, 2, 6, 3), BEACON)
    return sprite(c)


def tank():
    c = Canvas(9, 5)
    c.paint(c.rect(0, 3, 9, 5), ARMY[0])
    c.paint(c.rect(1, 2, 8, 4), ARMY[2])
    c.paint(c.rect(3, 0, 6, 2), ARMY[3])
    c.paint(c.rect(6, 1, 9, 2), ARMY[1])
    return sprite(c)


def watchtower():
    c = Canvas(6, 11)
    c.paint(c.rect(1, 3, 2, 11) | c.rect(4, 3, 5, 11), STEEL[2])
    c.paint(c.rect(0, 1, 6, 4), ARMY[2])
    c.paint(c.rect(1, 2, 5, 3), GLASS[4])
    return sprite(c)


# ── 힐리아: 농경지, 뱀성, 뱀열차, 항구 ─────────────────────

def farmhouse():
    c = Canvas(11, 9)
    c.paint(c.rect(2, 4, 9, 9), PLASTER[1])
    c.paint(c.rect(7, 4, 9, 9), PLASTER[0])
    c.paint(c.polygon([(0.5, 4.5), (5.5, 0), (10.5, 4.5)]), TIMBER[2])
    c.paint(c.polygon([(5.5, 0), (10.5, 4.5), (5.5, 4.5)]), TIMBER[1])
    c.paint(c.rect(5, 6, 7, 9), pg.BARK[2])
    return sprite(c)


def windmill():
    c = Canvas(13, 16)
    c.paint(c.polygon([(5, 5), (8, 5), (9.5, 15), (3.5, 15)]), PLASTER[1])
    c.paint(c.polygon([(6.5, 5), (8, 5), (9.5, 15), (6.5, 15)]), PLASTER[0])
    c.paint(c.ellipse(6.5, 5, 2.2, 1.8), TIMBER[1])
    for dx, dy in ((-5, -4), (5, -4), (-4, 4), (4, 4)):
        c.line(6, 5, 6 + dx, 5 + dy, pg.WOOD[3])
    return sprite(c)


def snake_castle():
    """뱀성: 층층 돌 기단 위 성벽과 양쪽 망루, 가운데 초록 지붕 탑을 돌로 된 거대한 뱀이 감고 있다."""
    c = Canvas(36, 46)
    moss = pg.hexes("#5f6a4c", "#7f8a68", "#a3a98a")
    roof = pg.hexes("#1f6b58", "#2f8f76", "#58b898")

    c.paint(c.polygon([(1, 45), (35, 45), (32, 38), (4, 38)]), pg.STONE[2])
    c.paint(c.rect(0, 39, 36, 40), pg.GRASS[4])
    c.paint(c.rect(16, 38, 20, 45), pg.STONE[4])
    c.paint(c.rect(6, 30, 30, 38), pg.STONE[3])
    for x in range(6, 30, 3):
        c.paint(c.rect(x, 29, x + 2, 30), pg.STONE[3])
    c.paint(c.ellipse(18, 34, 2.4, 2.6) | c.rect(16, 34, 20, 38), pg.STONE[0])
    for x0, x1 in ((4, 10), (26, 32)):
        c.paint(c.rect(x0, 23, x1, 38), pg.STONE[3])
        c.paint(c.rect(x1 - 2, 23, x1, 38), pg.STONE[2])
        c.paint(c.polygon([((x0 + x1) / 2, 15), (x1 + 1, 23), (x0 - 1, 23)]), roof[1])
        c.paint(c.polygon([((x0 + x1) / 2, 15), (x1 + 1, 23), ((x0 + x1) / 2, 23)]), roof[0])

    c.paint(c.rect(13, 14, 23, 34), pg.STONE[3])
    c.paint(c.rect(20, 14, 23, 34), pg.STONE[2])
    c.paint(c.rect(13, 14, 14, 34), pg.STONE[4])
    c.paint(c.polygon([(25, 8), (27, 15), (24, 18), (22, 10)]), moss[0])
    c.paint(c.polygon([(18, 6), (25, 14.5), (11, 14.5)]), roof[2])
    c.paint(c.polygon([(18, 6), (25, 14.5), (18, 14.5)]), roof[1])
    for top, left, right in ((18, 8, 28), (24, 9, 27), (29, 10, 26)):
        coil = c.polygon([(left, top + 3), (right, top), (right, top + 3), (left, top + 6)])
        coil |= c.ellipse(left + 0.5, top + 4.5, 2, 1.8) | c.ellipse(right - 0.5, top + 1.5, 2, 1.8)
        c.paint(coil, moss[1])
        c.paint(c.polygon([(left, top + 3), (right, top), (right, top + 1), (left, top + 4)]), moss[2])
    c.paint(c.ellipse(18, 4.2, 5.4, 3.8), moss[1])
    c.paint(c.polygon([(13, 5), (23, 5), (19.5, 9), (16.5, 9)]), moss[1])
    c.paint(c.ellipse(17, 3.2, 3.4, 1.8), moss[2])
    c.paint(c.rect(15, 4, 16, 5) | c.rect(20, 4, 21, 5), BEACON)
    return sprite(c)


def harbor():
    """왼쪽으로 뻗은 나무 잔교, 끝에 매어 둔 돛배, 뭍의 창고와 기중기. 오른쪽으로 뻗게 하려면 좌우를 뒤집는다."""
    c = Canvas(36, 19)
    c.paint(c.polygon([(1, 13), (11, 13), (9, 16), (3, 16)]), pg.WOOD[2])
    c.paint(c.rect(1, 13, 11, 14), pg.WOOD[4])
    c.paint(c.rect(6, 2, 7, 13), pg.WOOD[1])
    c.paint(c.polygon([(7, 2), (12, 11), (7, 11)]), PLASTER[1])
    c.paint(c.polygon([(6, 3), (2, 11), (6, 11)]), PLASTER[0])
    c.paint(c.rect(11, 14, 28, 16), pg.WOOD[3])
    for x in range(13, 28, 4):
        c.paint(c.rect(x, 16, x + 1, 18), pg.WOOD[1])
    c.paint(c.rect(25, 8, 35, 16), PLASTER[0])
    c.paint(c.rect(25, 13, 28, 16), pg.WOOD[1])
    c.paint(c.polygon([(23.5, 8.5), (30, 3), (35.5, 8.5)]), TIMBER[2])
    c.paint(c.polygon([(30, 3), (35.5, 8.5), (30, 8.5)]), TIMBER[1])
    c.line(21, 14, 21, 3, pg.WOOD[2])
    c.line(21, 3, 15, 5, pg.WOOD[2])
    c.line(15, 5, 15, 10, STEEL[1])
    return sprite(c)


def old_pier():
    """배편이 끊긴 서쪽 부두: 왼쪽으로 길게 뻗은 낡은 잔교, 끝의 푸른 문, 뭍의 작은 창고. 돛배가 없다."""
    c = Canvas(34, 16)
    c.paint(c.rect(1, 3, 2, 12) | c.rect(7, 3, 8, 12), pg.STONE[2])
    c.paint(c.rect(1, 3, 8, 4), pg.STONE[3])
    c.paint(c.rect(2, 4, 7, 12), pg.hexes("#3ec0d8")[0])
    c.paint(c.rect(3, 5, 6, 11), pg.hexes("#8ae8ec")[0])
    c.paint(c.rect(0, 12, 22, 14), pg.WOOD[1])
    c.paint(c.rect(0, 12, 22, 13), pg.WOOD[2])
    for x in range(2, 22, 4):
        c.paint(c.rect(x, 14, x + 1, 16), pg.WOOD[0])
    c.paint(c.rect(23, 7, 33, 14), PLASTER[0])
    c.paint(c.rect(26, 10, 29, 14), pg.WOOD[1])
    c.paint(c.polygon([(21.5, 7.5), (28, 2), (33.5, 7.5)]), pg.hexes("#5a5a60")[0])
    c.paint(c.polygon([(28, 2), (33.5, 7.5), (28, 7.5)]), pg.hexes("#44444a")[0])
    return sprite(c)


# ── 마르: 동아시아 ─────────────────────────────────────

def tiled_roof(c, cx, y, half_width, height):
    """처마 끝이 살짝 들린 기와지붕. y는 처마 줄이다."""
    c.paint(c.polygon([(cx - half_width, y), (cx + half_width, y), (cx + half_width - 2, y - height), (cx - half_width + 2, y - height)]), TILE[1])
    c.paint(c.polygon([(cx - half_width + 2, y - height), (cx + half_width - 2, y - height), (cx + half_width - 3, y - height + 1), (cx - half_width + 3, y - height + 1)]), TILE[3])
    c.paint(c.rect(cx - half_width, y - 1, cx + half_width, y), TILE[2])
    c.paint(c.rect(cx - half_width - 1, y - 2, cx - half_width, y - 1), TILE[3])
    c.paint(c.rect(cx + half_width, y - 2, cx + half_width + 1, y - 1), TILE[3])


def pagoda():
    """다섯 층 목탑. 위로 갈수록 좁아지고 꼭대기에 금빛 상륜이 있다."""
    c = Canvas(24, 42)
    cx = 12
    c.paint(c.rect(4, 39, 20, 41), pg.STONE[3])
    y = 39
    for half_width, wall in ((10, 7), (9, 6), (8, 5), (7, 4), (6, 3)):
        c.paint(c.rect(cx - wall, y - 3, cx + wall, y), TIMBER[1])
        c.paint(c.rect(cx - wall + 1, y - 3, cx, y), TIMBER[2])
        c.paint(c.rect(cx - 1, y - 2, cx + 1, y), GOLD[3])
        tiled_roof(c, cx, y - 3, half_width, 3)
        y -= 6
    c.paint(c.rect(cx - 1, y - 6, cx + 1, y + 3), GOLD[3])
    c.paint(c.rect(cx - 2, y - 2, cx + 2, y - 1), GOLD[2])
    return sprite(c)


def palace_gate():
    """돌 축대에 무지개문을 내고 그 위에 두 겹 기와지붕을 올린 궁궐 정문."""
    c = Canvas(42, 28)
    c.paint(c.rect(4, 15, 38, 27), pg.STONE[3])
    c.paint(c.rect(4, 15, 38, 16), pg.STONE[4])
    for y in (19, 23):
        c.paint(c.rect(4, y, 38, y + 1), pg.STONE[2])
    c.paint(c.ellipse(21, 21, 3.4, 3.4) | c.rect(17.6, 21, 24.4, 27), pg.STONE[0])
    c.paint(c.rect(10, 11, 32, 15), TIMBER[1])
    for x in range(11, 32, 4):
        c.paint(c.rect(x, 11, x + 1, 15), TIMBER[2])
    tiled_roof(c, 21, 11, 17, 3)
    c.paint(c.rect(13, 5, 29, 8), TIMBER[1])
    tiled_roof(c, 21, 5, 12, 3)
    return sprite(c)


def tiled_house():
    c = Canvas(15, 10)
    c.paint(c.rect(3, 4, 12, 10), PLASTER[1])
    for x in (3, 7, 11):
        c.paint(c.rect(x, 4, x + 1, 10), TIMBER[0])
    c.paint(c.rect(8, 6, 10, 10), pg.WOOD[2])
    tiled_roof(c, 7.5, 5, 6, 3)
    return sprite(c)


def bamboo():
    c = Canvas(7, 11)
    for x, top in ((1, 2), (3, 0), (5, 3)):
        c.paint(c.rect(x, top, x + 1, 11), pg.hexes("#5f9a55")[0])
        c.paint(c.rect(x - 1, top, x + 2, top + 2), pg.hexes("#3f7a48")[0])
        c.paint(c.rect(x, top + 5, x + 1, top + 6), pg.hexes("#2f5f3c")[0])
    return sprite(c)


def umbrella_pine():
    c = Canvas(11, 9)
    c.paint(c.rect(5, 4, 6, 9), pg.BARK[2])
    c.line(5, 6, 8, 4, pg.BARK[2])
    c.paint(c.ellipse(5.5, 2.4, 5, 2), NEEDLE_DARK)
    c.paint(c.ellipse(4.5, 1.8, 3, 1.1), NEEDLE_LIGHT)
    return sprite(c)


NEEDLE_DARK, NEEDLE_LIGHT = pg.hexes("#22463a", "#3f7a58")


# ── 무릉도원 ───────────────────────────────────────────

def karst(width, height, rng):
    """산수화의 바위산: 위로 갈수록 좁고 둥글며 가로 결이 있다. 꼭대기에 풀이 덮였다."""
    c = Canvas(width + 2, height + 2)
    drift = 0.0
    for y in range(height):
        t = (y + 1) / height
        drift = float(np.clip(drift + rng.uniform(-0.5, 0.5), -1.5, 1.5))
        half = width / 2 * min(1.0, 0.3 + 0.85 * t ** 0.75)
        left = int(round(width / 2 - half + drift)) + 1
        right = int(round(width / 2 + half + drift)) + 1
        middle = left + (right - left) * 3 // 5
        band = 1 if y % 5 == 0 else 0
        c.image[y + 1, left:middle, :3] = ROCK[2 - band]
        c.image[y + 1, middle:right, :3] = ROCK[0]
        c.image[y + 1, left, :3] = ROCK[3]
        c.image[y + 1, left:right, 3] = 255
        if y < 5:
            c.image[y + 1, left:right, :3] = pg.GRASS[5 if y < 3 else 3]
    return sprite(c)


def pavilion():
    """물가의 작은 정자."""
    c = Canvas(13, 11)
    for x in (2, 6, 10):
        c.paint(c.rect(x, 5, x + 1, 10), TIMBER[1])
    c.paint(c.rect(1, 9, 12, 11), pg.STONE[4])
    tiled_roof(c, 6.5, 6, 5, 4)
    c.paint(c.rect(6, 0, 7, 3), GOLD[3])
    return sprite(c)


def cloud(width):
    """뭉게뭉게 떠 있는 안개구름."""
    height = max(8, width * 2 // 5)
    c = Canvas(width, height)
    puffs = ((0.2, 0.68, 0.17), (0.44, 0.48, 0.24), (0.7, 0.6, 0.2), (0.87, 0.74, 0.11))
    for colour, grow_by, lift in ((MIST[0], 0.0, 0.0), (MIST[1], -0.03, 0.08), (MIST[2], -0.1, 0.16)):
        for x, y, radius in puffs:
            r = max(1.0, (radius + grow_by) * width)
            c.paint(c.ellipse(x * width - lift * 4, (y - lift) * height, r, r * 0.72) & c.rect(0, 0, width, height - 1), colour)
    return c.image.copy()
