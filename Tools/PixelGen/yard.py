"""집 마당에 세우는 소품: 꽃 상자, 나무 의자, 길드 깃발, 장작더미(뉴웨이브 아지트), 텃밭, 빨랫줄, 장작 패는 그루터기
(디엘의 어린 시절 집). 사용자 요청 2026-11-02.

세우는 것은 (RGBA 그림, 기준점)을 돌려준다. 기준점은 바닥에 닿는 아래 가운데다(layout.MapLayout.stand로 세운다).
텃밭은 바닥에 까는 그림이라 RGBA만 돌려준다(layout.MapLayout.decal).
"""
import numpy as np

import pixelgen as pg
from houses import BRASS, CLOTH, FLOWERS, IRON, LEAF, PAPER, WOOD, Canvas, gear, outline


def flower_box(rng, columns=2):
    """길쭉한 나무 꽃 상자에 여러 색 꽃이 소복하다."""
    w, h = columns * 30, 34
    c = Canvas(w, h)
    ground = h - 2
    box = c.rect(2, ground - 12, w - 2, ground)
    c.paint(box, WOOD[3])
    c.paint(c.rect(2, ground - 12, w - 2, ground - 10), WOOD[5])
    c.paint(c.rect(2, ground - 2, w - 2, ground), WOOD[1])
    for x in range(8, w - 4, 10):
        c.paint(c.rect(x, ground - 10, x + 1, ground - 2), WOOD[2])
    c.paint(c.ellipse(w / 2, ground - 13, w / 2 - 3, 7) & (c.yy < ground - 11), LEAF[1])
    for _ in range(columns * 14):
        x, y = rng.uniform(5, w - 5), rng.uniform(ground - 22, ground - 13)
        c.paint(c.ellipse(x, y, 2.4, 2.2), LEAF[int(rng.integers(2, 4))])
    for _ in range(columns * 7):
        x, y = rng.uniform(6, w - 6), rng.uniform(ground - 22, ground - 14)
        color = FLOWERS[int(rng.integers(0, len(FLOWERS)))]
        c.paint(c.ellipse(x, y, 2.2, 2.2), color)
        c.paint(c.rect(int(x), int(y), int(x) + 1, int(y) + 1), FLOWERS[1])
    return outline(c.image), (w // 2, ground)


def bench(rng):
    """등받이가 있는 나무 의자(정면에서 조금 위로 본 모습)."""
    w, h = 60, 38
    c = Canvas(w, h)
    ground = h - 2
    for x in (6, w - 9):
        c.paint(c.rect(x, ground - 14, x + 3, ground), WOOD[1])
    c.paint(c.rect(3, ground - 16, w - 3, ground - 11), WOOD[4])
    c.paint(c.rect(3, ground - 16, w - 3, ground - 15), WOOD[5])
    c.paint(c.rect(3, ground - 12, w - 3, ground - 11), WOOD[2])
    for x in (6, w - 9):
        c.paint(c.rect(x, ground - 32, x + 3, ground - 16), WOOD[2])
    for y in (ground - 31, ground - 24):
        c.paint(c.rect(4, y, w - 4, y + 5), WOOD[3])
        c.paint(c.rect(4, y, w - 4, y + 1), WOOD[5])
    return outline(c.image), (w // 2, ground)


def guild_flag(rng):
    """쇠 깃대에 늘어뜨린 청록 깃발과 놋쇠 톱니 문장(뉴웨이브 길드의 간판과 같은 색)."""
    w, h = 34, 104
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.ellipse(cx, ground - 2, 7, 3), IRON[1])
    c.paint(c.rect(cx - 1, 6, cx + 2, ground - 2), IRON[3])
    c.paint(c.rect(cx - 1, 6, cx, ground - 2), IRON[4])
    c.paint(c.ellipse(cx + 0.5, 5, 3, 3), BRASS[3])
    c.paint(c.rect(cx - 12, 10, cx + 13, 12), IRON[2])
    cloth = CLOTH["teal"]
    banner = c.polygon([(cx - 11, 12), (cx + 12, 12), (cx + 12, 54), (cx + 0.5, 47), (cx - 11, 54)])
    c.paint(banner, cloth[1])
    c.paint(banner & (c.xx < cx - 7), cloth[2])
    c.paint(banner & (c.xx > cx + 8), cloth[0])
    c.paint(banner & (c.yy < 15), cloth[2])
    gear(c, cx + 0.5, 29, 8, 8, BRASS, open_spokes=False)
    return outline(c.image), (cx, ground)


def woodpile(rng):
    """통나무 장작더미: 둥근 마구리가 보이게 세 줄로 쌓았다."""
    w, h = 62, 40
    c = Canvas(w, h)
    ground = h - 2
    rows = ((ground - 6, 5), (ground - 16, 4), (ground - 26, 3))
    for cy, count in rows:
        start = w / 2 - (count - 1) * 5.5
        for k in range(count):
            cx = start + k * 11 + rng.uniform(-0.6, 0.6)
            log = c.ellipse(cx, cy, 5.5, 5.5)
            c.paint(log, WOOD[2])
            c.paint(c.ellipse(cx, cy, 4, 4), pg.hexes("#c8a070")[0])
            c.paint(c.ellipse(cx, cy, 2, 2), pg.hexes("#a87c4c")[0])
            c.paint(log & (c.xx > cx + 3), WOOD[1])
    return outline(c.image), (w // 2, ground)


def vegetable_patch(columns, rows, rng):
    """텃밭(바닥에 까는 그림): 갈아엎은 흙 고랑마다 양배추·당근 잎·콩 줄기가 줄지어 자란다."""
    w, h = int(columns * pg.TILE), int(rows * pg.TILE)
    c = Canvas(w, h)
    soil = pg.hexes("#3a2614", "#4e3420", "#62442a")
    c.paint(c.rect(0, 0, w, h), soil[1])
    for y0 in range(0, h, 16):
        c.paint(c.rect(0, y0, w, y0 + 3), soil[0])
        c.paint(c.rect(0, y0 + 3, w, y0 + 5), soil[2])
        kind = (y0 // 16) % 3
        for x in range(8, w - 4, 12):
            cx, cy = x + rng.uniform(-1, 1), y0 + 10
            if kind == 0:
                head = c.ellipse(cx, cy, 5, 4)
                c.paint(head, LEAF[2])
                c.paint(c.ellipse(cx, cy - 0.5, 2.5, 2), LEAF[3])
                c.paint(head & (c.yy > cy + 2), LEAF[1])
            elif kind == 1:
                for dx in (-2, 0, 2):
                    c.line(cx, cy + 3, cx + dx, cy - 4, LEAF[3], 1)
                c.paint(c.rect(int(cx) - 1, int(cy) + 2, int(cx) + 2, int(cy) + 4), pg.hexes("#d8783a")[0])
            else:
                c.paint(c.rect(int(cx), int(cy) - 8, int(cx) + 1, int(cy) + 4), WOOD[2])
                for dy in (-6, -2, 2):
                    c.paint(c.ellipse(cx + (1.5 if dy % 4 else -1.5), cy + dy, 2, 1.4), LEAF[2])
    return outline(c.image)


def laundry_line(rng, columns=4):
    """빨랫줄: 두 기둥 사이 줄에 널린 옷가지(셔츠·아이 옷·천)."""
    w, h = columns * pg.TILE, 64
    c = Canvas(w, h)
    ground = h - 2
    for x in (3, w - 6):
        c.paint(c.rect(x, 8, x + 3, ground), WOOD[2])
        c.paint(c.rect(x - 2, 8, x + 5, 10), WOOD[3])
    def sag(x):
        return 10 + 4 * np.sin(np.pi * (x - 4) / (w - 10))

    for x in range(5, w - 5):
        c.paint(c.rect(x, int(sag(x)), x + 1, int(sag(x)) + 1), PAPER[0])
    colors = (CLOTH["blue"][2], PAPER[2], CLOTH["red"][2], PAPER[1], CLOTH["green"][2])
    x = 12
    k = 0
    while x < w - 20:
        cw = int(rng.integers(12, 20))
        top = int(sag(x + cw / 2)) + 1
        cloth = c.rect(x, top, x + cw, top + int(rng.integers(14, 24)))
        if k % 2 == 0:
            cloth |= c.rect(x - 3, top, x, top + 7) | c.rect(x + cw, top, x + cw + 3, top + 7)
        color = colors[k % len(colors)]
        c.paint(cloth, color)
        c.paint(cloth & (c.xx < x + 2), (color * 0.75).astype(np.uint8))
        c.paint(c.rect(x + 2, top - 1, x + 4, top + 1) | c.rect(x + cw - 4, top - 1, x + cw - 2, top + 1), WOOD[4])
        x += cw + 7
        k += 1
    return outline(c.image), (w // 2, ground)


def chopping_block(rng):
    """장작 패는 그루터기에 박힌 도끼와 쪼갠 장작 몇 개."""
    w, h = 40, 34
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    stump = c.rect(cx - 9, ground - 14, cx + 9, ground)
    c.paint(stump, WOOD[2])
    c.paint(stump & ((c.xx - cx) % 5 == 0), WOOD[1])
    top = c.ellipse(cx, ground - 14, 9, 3.5)
    c.paint(top, pg.hexes("#c8a070")[0])
    c.paint(c.ellipse(cx, ground - 14, 5, 2), pg.hexes("#a87c4c")[0])
    c.line(cx + 2, ground - 15, cx + 13, ground - 28, WOOD[3], 2)
    c.paint(c.polygon([(cx - 3, ground - 18), (cx + 4, ground - 18), (cx + 3, ground - 12), (cx - 2, ground - 13)]), IRON[3])
    for x0 in (2, w - 10):
        c.paint(c.rect(x0, ground - 4, x0 + 8, ground), WOOD[3])
        c.paint(c.rect(x0, ground - 4, x0 + 2, ground), pg.hexes("#c8a070")[0])
    return outline(c.image), (cx, ground)
