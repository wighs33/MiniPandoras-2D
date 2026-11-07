"""9장 에라드 군함: 케이트가 연 길로 달려와 뉴웨이브 호의 뱃머리를 들이받는 배.

뉴웨이브 호(ship.py)와 같은 시점·크기이고 선체 모양도 같은 코드로 그린 뒤 좌우를 뒤집어 뱃머리가 왼쪽을 본다.
판자는 남색 칠, 뱃전은 금빛, 물막이 띠는 붉은색이다. 돛대 셋과 양쪽 뱃전의 대포, 갑판 가운데 에라드 문장(금빛 날개와 해)이 있다.

  Assets/MiniPandoras/Art/PixelGen/Stage/warship.png         기준점은 아래 가운데. 그림 크기는 뉴웨이브 호가 든 칸(46×22)이다
  Assets/MiniPandoras/Art/PixelGen/Stage/warship_plank.png   두 배 사이에 걸치는 널다리(가로 9칸). 기준점은 아래 가운데
  Docs/ArtUpgrade/PixelGen/warship_preview.png               바다 위에 띄운 모습

사용: python Tools/PixelGen/warship.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
import ship
from houses import IRON, WOOD, Canvas

W, H = 384, 232

GOLD = pg.hexes("#5a3c10", "#8c621e", "#c2902c", "#e6c058", "#fff0a6")
DECK = pg.hexes("#40291a", "#5e4028", "#7c5a3a", "#9a7550")
SEA = pg.hexes("#0c2238", "#112e48", "#163a58", "#1d4868", "#255a7c")
OUTLINE = np.array([8, 10, 20], dtype=np.uint8)

YY, XX = np.mgrid[0:H, 0:W].astype(float)


def paint_emblem(rgba, cx, cy):
    """에라드 문장: 금빛 해 둘레로 펼친 두 날개(깃 세 겹)."""
    for side in (-1, 1):
        for layer, (reach, lift, thick) in enumerate(((30, 12, 5), (24, 7, 4), (17, 3, 4))):
            t = np.clip(side * (XX - cx) / reach, 0, 1)
            centre = cy - lift * np.sin(t * np.pi * 0.85) + layer * 4 - 2
            wing = (side * (XX - cx) >= 5) & (side * (XX - cx) <= reach) & (np.abs(YY - centre) <= thick * (1 - t * 0.6))
            rgba[wing, :3] = GOLD[3 if layer == 0 else 2]
            rgba[wing & (YY < centre - thick * (1 - t * 0.6) + 1.5), :3] = GOLD[4]
    r = np.hypot(XX - cx, YY - cy)
    rgba[r <= 6.5, :3] = GOLD[2]
    rgba[r <= 5, :3] = GOLD[3]
    rgba[r <= 2.5, :3] = GOLD[4]
    for k in range(8):
        a = k * np.pi / 4
        for d in range(8, 11):
            px, py = int(round(cx + np.cos(a) * d)), int(round(cy + np.sin(a) * d * 0.9))
            rgba[py, px, :3] = GOLD[3]


# ── 물 ───────────────────────────────────────────────


# 뉴웨이브 호의 나무·청록을 남색·금·붉은색으로 바꿔 그린다.
TOP_DECK = pg.hexes("#141a2c", "#1d2740", "#28344f", "#324362", "#3c5074", "#4c6488", "#5f7a9c")
TOP_HULL = pg.hexes("#0c1220", "#151e36", "#1f2c4c", "#2a3c64", "#384f80")
TOP_PAINT = pg.hexes("#3a0f12", "#5e181c", "#86262a", "#b03a3a")
TOP_CREAM = pg.hexes("#8c621e", "#c2902c", "#e6c058")
TOP_WOOD = pg.hexes("#3a2a10", "#5a3c10", "#8c621e", "#a97c24", "#c2902c", "#e6c058")


def overlay(sprite, piece, left, top):
    """RGBA 그림 위에 RGBA 조각을 겹친다. 조각이 칠한 점만 덮는다."""
    h, w = piece.shape[:2]
    y0, x0 = max(0, top), max(0, left)
    y1, x1 = min(sprite.shape[0], top + h), min(sprite.shape[1], left + w)
    part = piece[y0 - top:y1 - top, x0 - left:x1 - left]
    solid = part[..., 3] > 0
    sprite[y0:y1, x0:x1][solid] = part[solid]


def cannon(c, x, y, facing):
    """뱃전의 대포: 쇠 포신과 나무 받침."""
    c.paint(c.rect(x - 7, y - 5, x + 7, y + 5), WOOD[2])
    c.paint(c.rect(x - 7, y - 5, x + 7, y - 3), WOOD[3])
    c.paint(c.rect(x - 3, y + (2 if facing > 0 else -14), x + 3, y + (14 if facing > 0 else -2)), IRON[1])
    c.paint(c.rect(x - 2, y + (2 if facing > 0 else -14), x, y + (14 if facing > 0 else -2)), IRON[3])


def topdown():
    """뉴웨이브 호와 같은 선체를 군함 빛깔로 그린다. 돌려주는 값은 좌우를 뒤집은 RGBA(뱃머리가 왼쪽)."""
    saved = ship.DECK, ship.HULL, ship.PAINT, ship.CREAM, ship.WOOD
    ship.DECK, ship.HULL, ship.PAINT, ship.CREAM, ship.WOOD = TOP_DECK, TOP_HULL, TOP_PAINT, TOP_CREAM, TOP_WOOD
    rng = np.random.default_rng(71)
    t = pg.TILE
    c = Canvas(46 * t, 22 * t)
    deck, _ = ship.hull_and_deck(c, rng, name=None)
    north, south, inside = ship.deck_edges(c.size[0])
    # 양쪽 뱃전을 따라 늘어선 대포
    for x in range(5 * t, 25 * t, 4 * t):
        cannon(c, x, int(north[x]) + ship.FACE_H + 8, 1)
        cannon(c, x, int(south[x]) - 8, -1)
    # 갑판 가운데 붉은 양탄자와 금빛 띠
    top, bottom = int(9.6 * t), int(12.4 * t)
    run = c.rect(4 * t, top, 30 * t, bottom) & deck
    c.paint(run, TOP_PAINT[1])
    c.paint(c.rect(4 * t, top, 30 * t, top + 4), TOP_CREAM[1])
    c.paint(c.rect(4 * t, bottom - 4, 30 * t, bottom), TOP_CREAM[1])
    sprite = c.image[:, ::-1].copy()
    emblem = np.zeros((H, W, 4), dtype=np.uint8)
    paint_emblem(emblem, W // 2, H // 2)
    emblem[emblem[..., :3].any(axis=2), 3] = 255
    emblem = emblem.repeat(2, axis=0).repeat(2, axis=1)
    overlay(sprite, emblem, 30 * t - W, 11 * t - H)
    # 돛대 셋(남쪽에서 본 모습이라 뒤집은 그림 위에 얹는다)
    for x, ground, height, boom, nest in ((12, 13, 236, 150, False), (24, 13, 260, 170, True), (36, 13, 220, 150, False)):
        (image, pivot), _ = ship.mast(rng, height, boom, nest=nest, flag=True, lantern_at=66)
        overlay(sprite, image[:, ::-1], int(x * t - (image.shape[1] - 1 - pivot[0])), int(ground * t - pivot[1]))
    ship.DECK, ship.HULL, ship.PAINT, ship.CREAM, ship.WOOD = saved
    return sprite


def plank():
    """두 배 사이에 걸치는 널다리: 쇠갈고리를 단 긴 판자와 낮은 밧줄 난간."""
    t = pg.TILE
    c = Canvas(9 * t, int(1.6 * t))
    w, h = c.size
    board = c.rect(10, 8, w - 10, h - 6)
    c.paint(board, WOOD[3])
    c.paint(board & (c.xx % 26 < 2), WOOD[1])
    c.paint(board & (c.yy % 8 == 0), WOOD[2])
    c.paint(c.rect(10, h - 6, w - 10, h - 3), WOOD[1])
    for x in (4, w - 10):
        c.paint(c.rect(x, 4, x + 6, h - 2), IRON[2])
        c.paint(c.rect(x + 1, 5, x + 3, h - 3), IRON[3])
    c.paint(c.rect(10, 6, w - 10, 8), pg.hexes("#b89a64")[0])
    return pg.outline(c.image, OUTLINE)


def preview(sprite):
    rng = np.random.default_rng(3)
    sea_h, sea_w = sprite.shape[:2]
    yy, xx = np.mgrid[0:sea_h, 0:sea_w]
    n = pg.noise(sea_h, sea_w, 9, rng)
    stripe = np.sin(yy * 0.9 + n * 6) * 0.5 + 0.5
    canvas = pg.quantize(np.clip(0.35 + stripe * 0.2 + n * 0.25, 0, 0.999), SEA, dither=0.2)
    pg.stamp(canvas, sprite, sea_w / 2, sea_h, (sea_w / 2, sea_h))
    return canvas


def main(project):
    sprite = pg.outline(topdown(), OUTLINE)
    out = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(sprite), "RGBA").save(out / "warship.png")
    Image.fromarray(pg.upscale(plank()), "RGBA").save(out / "warship_plank.png")
    Image.fromarray(pg.upscale(preview(sprite)), "RGB").save(project / "Docs/ArtUpgrade/PixelGen/warship_preview.png")
    print(out / "warship.png")


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
