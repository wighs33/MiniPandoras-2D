"""디엘의 어린 시절 집(4장 회상, jamieHouse) 안의 살림살이(회상 속 집과 집 안 그래픽 개선).

엄마(제이미)와 어린 디엘 둘이 사는 숲가 오두막: 저녁이 차려진 식탁, 접시가 놓인 찬장, 장작 바구니, 아궁이에 건 솥,
말린 약초 다발, 아이가 그린 그림, 나무 칼 장난감, 땋은 깔개. 가구는 (RGBA 그림, 발밑 기준점)을, 장식은 RGBA만 돌려준다.
"""
import numpy as np

import pixelgen as pg
from houses import BRASS, CLOTH, FLOWERS, IRON, LEAF, PAPER, WARM, WOOD, Canvas, outline

STEW = pg.hexes("#6a3a1a", "#8a5226", "#a86a34")
BREAD = pg.hexes("#8a5a24", "#b47a36", "#d8a050")
CERAMIC = pg.hexes("#6a7a8a", "#9aaabb", "#cfd8e0", "#eef2f4")
HERB = pg.hexes("#3a4a22", "#566a2e", "#7a8a3e", "#a8a058")
QUILT = pg.hexes("#5a2a26", "#7e3a32", "#a8584a", "#d8a070")


def supper_table(rng):
    """저녁이 차려진 나무 식탁(앞에서 조금 위로 본 모습): 스튜 두 그릇, 빵, 물주전자, 들꽃 꽂은 컵, 촛불."""
    w, h = 76, 46
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for x0 in (6, w - 11):
        c.paint(c.rect(x0, ground - 18, x0 + 5, ground), WOOD[1])
    top = c.rect(2, ground - 28, w - 2, ground - 16)
    c.paint(top, WOOD[3])
    c.paint(c.rect(2, ground - 28, w - 2, ground - 26), WOOD[4])
    c.paint(c.rect(2, ground - 18, w - 2, ground - 16), WOOD[2])
    c.paint(c.rect(10, ground - 27, w - 10, ground - 17), PAPER[1])
    c.paint(c.rect(10, ground - 27, w - 10, ground - 17) & ((c.xx + c.yy) % 8 < 1), CLOTH["red"][2])
    for bx in (20, w - 20):
        bowl = c.ellipse(bx, ground - 23, 7, 3.5)
        c.paint(bowl, CERAMIC[2])
        c.paint(c.ellipse(bx, ground - 24, 5.5, 2), STEW[2])
        c.paint(c.ellipse(bx - 1, ground - 24.5, 2, 0.8), STEW[1])
        c.line(bx + 6, ground - 26, bx + 9, ground - 30, IRON[3], 1)
    loaf = c.ellipse(cx, ground - 24, 8, 4)
    c.paint(loaf, BREAD[1])
    c.paint(loaf & (c.yy < ground - 25), BREAD[2])
    for k in range(3):
        c.paint(c.rect(cx - 5 + k * 4, ground - 26, cx - 4 + k * 4, ground - 23), BREAD[0])
    c.paint(c.rect(cx + 12, ground - 33, cx + 18, ground - 24), CERAMIC[1])
    c.paint(c.rect(cx + 12, ground - 33, cx + 18, ground - 31), CERAMIC[2])
    c.paint(c.rect(cx + 18, ground - 31, cx + 20, ground - 27), CERAMIC[1])
    c.paint(c.rect(cx - 17, ground - 30, cx - 13, ground - 24), CERAMIC[2])
    for k, color in enumerate((FLOWERS[0], FLOWERS[1], FLOWERS[3])):
        c.paint(c.ellipse(cx - 16 + k * 1.5 - 1.5, ground - 33 + (k % 2), 1.6, 1.6), color)
    c.paint(c.rect(cx - 27, ground - 30, cx - 24, ground - 24), PAPER[2])
    c.paint(c.ellipse(cx - 25.5, ground - 32, 1.4, 2), WARM[3])
    return outline(c.image), (cx, ground)


def chair(rng, facing="right"):
    """등받이 있는 나무 의자(옆모습). facing은 앉은 사람이 보는 쪽."""
    w, h = 22, 36
    c = Canvas(w, h)
    ground = h - 2
    back_x = 3 if facing == "right" else w - 7
    c.paint(c.rect(back_x, 4, back_x + 4, ground), WOOD[2])
    c.paint(c.rect(back_x, 4, back_x + 4, 6), WOOD[4])
    for y in (10, 16):
        c.paint(c.rect(back_x, y, back_x + 4, y + 2), WOOD[1])
    seat = c.rect(3, ground - 16, w - 3, ground - 12)
    c.paint(seat, WOOD[3])
    c.paint(c.rect(3, ground - 16, w - 3, ground - 15), WOOD[4])
    leg_x = w - 7 if facing == "right" else 3
    c.paint(c.rect(leg_x, ground - 12, leg_x + 3, ground), WOOD[1])
    return outline(c.image), (w // 2, ground)


def dresser(rng):
    """찬장: 아래는 문 달린 장, 위는 선반에 세운 접시와 컵, 꿀단지."""
    w, h = 54, 84
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(3, 4, w - 3, ground), WOOD[2])
    c.paint(c.rect(1, 2, w - 1, 6), WOOD[3])
    c.paint(c.rect(1, 2, w - 1, 3), WOOD[4])
    c.paint(c.rect(1, ground - 36, w - 1, ground - 32), WOOD[4])
    for x0 in (6, cx + 1):
        c.paint(c.rect(x0, ground - 30, x0 + 20, ground - 4), WOOD[3])
        c.paint(c.rect(x0 + 3, ground - 27, x0 + 17, ground - 7), WOOD[2])
    c.paint(c.ellipse(cx - 3, ground - 17, 1.5, 1.5) | c.ellipse(cx + 4, ground - 17, 1.5, 1.5), BRASS[3])
    c.paint(c.rect(5, 8, w - 5, ground - 36), WOOD[1])
    for y0 in (26, 44):
        c.paint(c.rect(4, y0, w - 4, y0 + 2), WOOD[3])
    for k, x in enumerate(range(9, w - 8, 9)):
        plate = c.ellipse(x + 3, 18, 4.5, 7)
        c.paint(plate, CERAMIC[2])
        c.paint(c.ellipse(x + 3, 18, 2.5, 4.5), CERAMIC[1] if k % 2 else CLOTH["blue"][2])
    for x in range(9, w - 10, 10):
        c.paint(c.rect(x, 36, x + 6, 44), CERAMIC[2 if x % 20 else 1])
        c.paint(c.rect(x, 36, x + 6, 37), CERAMIC[3])
    c.paint(c.ellipse(w - 12, 40, 4, 4), WARM[2])
    c.paint(c.rect(w - 15, 34, w - 9, 37), PAPER[1])
    return outline(c.image), (cx, ground)


def firewood_basket(rng):
    """엮은 바구니에 담긴 장작."""
    w, h = 34, 30
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for k in range(5):
        x = 7 + k * 5
        c.paint(c.rect(x, 4 + (k % 2) * 2, x + 4, ground - 8), WOOD[2 + k % 2])
        c.paint(c.ellipse(x + 2, 5 + (k % 2) * 2, 2, 1.5), pg.hexes("#c8a070")[0])
    basket = c.rect(4, ground - 14, w - 4, ground)
    c.paint(basket, pg.hexes("#8a6a3a")[0])
    c.paint(basket & ((c.xx + c.yy) % 4 < 2), pg.hexes("#a8844a")[0])
    c.paint(c.rect(3, ground - 15, w - 3, ground - 12), pg.hexes("#6a4e28")[0])
    return outline(c.image), (cx, ground)


def child_bed(rng):
    """아이 침대: 낮은 나무 틀에 누비이불과 헝겊 인형."""
    w, h = 32, 66
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    head = c.rect(2, 2, w - 2, 16) | c.ellipse(cx, 4, 14, 4)
    c.paint(head, WOOD[3])
    c.paint(head & (c.yy < 7), WOOD[4])
    c.paint(c.rect(2, 16, w - 2, ground), WOOD[2])
    quilt = c.rect(4, 26, w - 4, ground - 4)
    c.paint(c.rect(4, 16, w - 4, ground - 4), PAPER[1])
    c.paint(quilt, QUILT[1])
    patch = quilt & (((c.xx - 4) // 6 + (c.yy - 26) // 6) % 2 == 0)
    c.paint(patch, QUILT[2])
    c.paint(quilt & (((c.xx - 4) % 6 == 0) | ((c.yy - 26) % 6 == 0)), QUILT[3])
    pillow = c.ellipse(cx, 21, 10, 4.5)
    c.paint(pillow, PAPER[2])
    c.paint(pillow & (c.yy > 22), PAPER[1])
    doll = c.ellipse(cx + 7, 30, 3, 3) | c.rect(cx + 5, 32, cx + 10, 38)
    c.paint(doll, pg.hexes("#c8a07a")[0])
    c.paint(c.rect(cx + 5, 33, cx + 10, 38), CLOTH["blue"][2])
    return outline(c.image), (cx, ground)


# ── 장식 ───────────────────────────────────────────────

def braided_rug(columns, rows, rng):
    """둥글게 땋은 헝겊 깔개: 동심 타원 띠가 색을 바꿔 가며 돈다."""
    w, h = int(columns * pg.TILE), int(rows * pg.TILE)
    c = Canvas(w, h)
    d = np.hypot((c.xx + 0.5 - w / 2) / (w / 2), (c.yy + 0.5 - h / 2) / (h / 2))
    rug = d <= 1
    ring = (d * 9).astype(int)
    colors = pg.hexes("#7e3a32", "#a8584a", "#c89a5a", "#566a2e", "#3c5a6a")
    c.image[rug, :3] = colors[ring[rug] % len(colors)]
    c.image[rug, 3] = 255
    braid = rug & (((np.arctan2(c.yy - h / 2, c.xx - w / 2) * 20).astype(int) + ring) % 2 == 0)
    c.image[braid, :3] = (c.image[braid, :3] * 0.85).astype(np.uint8)
    return c.image


def doormat(rng):
    c = Canvas(40, 16)
    mat = c.rect(1, 1, 39, 15)
    c.paint(mat, pg.hexes("#7a5a2a")[0])
    c.paint(mat & (c.xx % 3 == 0), pg.hexes("#5a4220")[0])
    return outline(c.image)


def cooking_pot(rng):
    """아궁이 불 위에 쇠고리로 건 무쇠 솥과 피어오르는 김."""
    c = Canvas(28, 30)
    c.line(14, 0, 14, 8, IRON[2], 1)
    c.paint(c.ellipse(14, 10, 8, 3) & ~c.ellipse(14, 11, 7, 2), IRON[3])
    pot = c.ellipse(14, 18, 10, 8) & (c.yy > 12)
    c.paint(pot, IRON[1])
    c.paint(pot & (c.xx < 10), IRON[2])
    c.paint(c.rect(4, 12, 24, 14), IRON[2])
    c.paint(c.ellipse(14, 12.5, 8, 1.5), STEW[1])
    return c.image


def herbs(rng):
    """들보에 거꾸로 매단 말린 약초 다발 셋."""
    c = Canvas(40, 26)
    c.paint(c.rect(0, 0, 40, 2), WOOD[2])
    for k, x in enumerate((7, 20, 33)):
        c.line(x, 2, x, 6, PAPER[0], 1)
        bunch = c.polygon([(x - 2, 6), (x + 2, 6), (x + 5, 22 - k * 2), (x - 5, 22 - k * 2)])
        c.paint(bunch, HERB[1 + k % 3])
        c.paint(bunch & ((c.xx + c.yy) % 3 == 0), HERB[0])
        c.paint(c.rect(x - 2, 6, x + 2, 8), CLOTH["red"][1])
    return outline(c.image)


def dish_shelf(rng):
    """벽 선반 하나에 단지와 컵, 국자."""
    w, h = 50, 26
    c = Canvas(w, h)
    c.paint(c.rect(0, h - 5, w, h - 2), WOOD[3])
    c.paint(c.rect(0, h - 5, w, h - 4), WOOD[4])
    for k, x in enumerate((4, 14, 24, 34)):
        jar = c.rect(x, h - 5 - (10 + k % 2 * 4), x + 8, h - 5)
        c.paint(jar, (CERAMIC[1], WARM[1], CERAMIC[2], pg.hexes("#7a5a3a")[0])[k])
        c.paint(c.rect(x, h - 5 - (10 + k % 2 * 4), x + 8, h - 4 - (10 + k % 2 * 4)), PAPER[1])
    c.line(44, 4, 46, h - 6, IRON[3], 1)
    c.paint(c.ellipse(46, h - 7, 2.5, 1.5), IRON[3])
    return outline(c.image)


def child_drawing(rng):
    """벽에 붙인 아이 그림: 엄마와 아이, 해, 집."""
    c = Canvas(26, 22)
    paper = c.rect(1, 1, 25, 21)
    c.paint(paper, PAPER[2])
    c.paint(c.ellipse(20, 5, 3, 3), FLOWERS[1])
    c.paint(c.rect(3, 15, 23, 16), LEAF[2])
    for x, tall, color in ((8, 9, FLOWERS[0]), (14, 6, CLOTH["blue"][2])):
        c.paint(c.ellipse(x, 15 - tall, 1.8, 1.8), pg.hexes("#c8a07a")[0])
        c.paint(c.rect(x - 1, 16 - tall, x + 2, 15), color)
    c.paint(c.rect(1, 1, 3, 3), WARM[2])
    return outline(c.image)


def toy_sword(rng):
    """벽에 기대 둔 나무 칼 장난감."""
    c = Canvas(12, 30)
    c.paint(c.rect(5, 2, 8, 22), WOOD[4])
    c.paint(c.rect(5, 2, 6, 22), WOOD[5])
    c.paint(c.rect(2, 21, 11, 23), WOOD[2])
    c.paint(c.rect(5, 23, 8, 29), CLOTH["red"][1])
    return outline(c.image)
