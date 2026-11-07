"""훈련장(trainingGround)과 허수아비를 그린다(봉인서의 「체험」으로 판도라 스킬을 시험하는 곳).

숲속 빈터를 울타리로 두른 흙 마당이다. 가운데는 스킬을 써 보도록 비우고, 허수아비 넷을 간격을 두어 세운다.
가까이 하나, 양옆으로 둘, 멀리 하나라서 범위 스킬과 파고드는 스킬을 함께 시험할 수 있다. 위쪽 울타리 곁에 과녁과 무기 걸이가 있다.
이야기 맵이 아니라서 드나드는 길이 없고, 체험이 끝나면 돌아간다.

사용: python Tools/PixelGen/training_ground.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/trainingGround/…        맵 데이터와 그림(layout.py)
  Assets/MiniPandoras/Art/PixelGen/Monsters/trainingDummy/…     허수아비 시트
  Docs/ArtUpgrade/PixelGen/trainingGround_preview.png           미리보기
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import forest
import pixelgen as pg
from houses import Canvas, outline
from layout import MapLayout

WIDTH, HEIGHT = 28, 18
LEFT, TOP, RIGHT, BOTTOM = 3, 2, 24, 15     # 울타리가 서는 칸
START = (7, 9)
DUMMIES = ((13, 9), (17, 7), (17, 11), (21, 9))

TIMBER = pg.hexes("#2b1d16", "#4a3324", "#6b4a33", "#8c6646", "#ad855c")
STRAW = pg.hexes("#6e5220", "#a2803a", "#c9a950", "#e4c878")
BURLAP = pg.hexes("#4e3c28", "#7a6244", "#9c8660", "#bda880")
ROPE = pg.hexes("#3e3020", "#8a7044")
STEEL = pg.hexes("#3a3e4a", "#6e7482", "#a8aeba", "#dfe3ea")
MOUND = pg.hexes("#3d2c23", "#58402f")
INK = pg.hexes("#2a1e16")[0]
TARGET = pg.hexes("#e8dcc0", "#b83a2a", "#3a4a7a")

DOTS_WIDE, DOTS_TALL = 64, 88


# ── 허수아비 ─────────────────────────────────────────────

def straw_fan(c, x, y, spread, length, down=False):
    """짚단: 한 점에서 부챗살로 퍼진다."""
    for i, turn in enumerate(np.linspace(-spread, spread, 7)):
        reach = length - abs(turn) * 0.25
        c.line(x, y, x + turn, y + reach if down else y - reach, STRAW[1 + i % 3], 2)


def arm_tuft(c, x, y, side):
    for i, slope in enumerate(np.linspace(-0.7, 0.7, 5)):
        c.line(x, y, x + side * 9, y + slope * 9, STRAW[1 + i % 3], 2)


def stitch_x(c, x, y):
    c.line(x - 2, y - 2, x + 2, y + 2, INK, 1)
    c.line(x - 2, y + 2, x + 2, y - 2, INK, 1)


def dummy(view):
    c = Canvas(DOTS_WIDE, DOTS_TALL)
    cx, ground = DOTS_WIDE // 2, DOTS_TALL - 2
    # 뒤에서 앞으로: 흙무더기, 기둥, 팔대, 치마, 몸통, 머리.
    c.paint(c.ellipse(cx, ground - 1, 13, 3.4), MOUND[1])
    c.paint(c.ellipse(cx, ground - 2, 8, 2), MOUND[0])
    c.paint(c.rect(cx - 3, 22, cx + 3, ground - 1), TIMBER[2])
    c.paint(c.rect(cx - 3, 22, cx - 1, ground - 1), TIMBER[3])
    if view == "right":
        c.paint(c.ellipse(cx, 37, 5, 3), TIMBER[2])
    else:
        c.paint(c.rect(cx - 25, 34, cx + 25, 39), TIMBER[2])
        c.paint(c.rect(cx - 25, 34, cx + 25, 35), TIMBER[4])
        arm_tuft(c, cx - 24, 36, -1)
        arm_tuft(c, cx + 24, 36, 1)
    straw_fan(c, cx, 54, 13, 20, down=True)
    width = 9 if view == "right" else 12
    body = c.ellipse(cx, 44, width, 15)
    c.paint(body, BURLAP[2])
    c.paint(body & (c.xx > cx + width * 0.35), BURLAP[1])
    c.paint(body & (c.xx < cx - width * 0.55), BURLAP[3])
    for seam in (37, 42, 47):
        c.paint(body & (c.yy == seam) & ((c.xx + seam) % 4 == 0), BURLAP[1])
    c.paint(c.rect(cx - width, 54, cx + width, 57), ROPE[1])
    c.paint(c.rect(cx - width, 56, cx + width, 57), ROPE[0])
    c.paint(c.rect(cx - 6, 27, cx + 6, 30), ROPE[1])
    head = c.ellipse(cx, 17, 11, 11.5)
    c.paint(head, BURLAP[2])
    c.paint(head & (c.xx > cx + 6), BURLAP[1])
    c.paint(head & (c.xx < cx - 7), BURLAP[3])
    straw_fan(c, cx, 7, 7, 6)
    if view == "down":
        stitch_x(c, cx - 4, 16)
        stitch_x(c, cx + 4, 16)
        for x in range(cx - 4, cx + 5, 2):
            c.paint(c.rect(x, 22, x + 1, 23), INK)
    elif view == "right":
        stitch_x(c, cx + 5, 16)
    else:
        for y in range(9, 26, 3):
            c.paint(c.rect(cx, y, cx + 1, y + 2), BURLAP[1])
    return outline(c.image)


def write_dummy(project):
    folder = project / "Assets/MiniPandoras/Art/PixelGen/Monsters/trainingDummy"
    folder.mkdir(parents=True, exist_ok=True)
    for view in ("down", "right", "up"):
        image = Image.fromarray(pg.upscale(dummy(view)), "RGBA")
        for frame in range(3):
            image.save(folder / f"{view}_{frame}.png")
    sheet = {"id": "trainingDummy", "width": DOTS_WIDE // 2, "height": DOTS_TALL // 2, "anchorX": DOTS_WIDE // 4, "anchorY": DOTS_TALL // 2 - 1,
             "framesDown": 3, "framesRight": 3, "framesUp": 3, "hasPortrait": False}
    (folder / "sheet.json").write_text(json.dumps(sheet), encoding="utf-8")


# ── 소품 ─────────────────────────────────────────────────

def target_board():
    """나무 다리에 세운 과녁."""
    w, h = 48, 66
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.line(cx - 12, ground, cx - 4, 30, TIMBER[2], 3)
    c.line(cx + 12, ground, cx + 4, 30, TIMBER[2], 3)
    c.line(cx, ground - 6, cx, 30, TIMBER[1], 3)
    c.paint(c.ellipse(cx, 24, 20, 21), STRAW[1])
    c.paint(c.ellipse(cx, 23, 19, 20), STRAW[2])
    c.paint(c.ellipse(cx, 23, 15, 16), TARGET[0])
    c.paint(c.ellipse(cx, 23, 11, 12), TARGET[2])
    c.paint(c.ellipse(cx, 23, 7, 8), TARGET[0])
    c.paint(c.ellipse(cx, 23, 3.5, 4), TARGET[1])
    return outline(c.image), (cx, ground)


def weapon_rack():
    """창과 검을 세워 둔 나무 걸이."""
    w, h = 84, 64
    c = Canvas(w, h)
    ground = h - 2
    for x in (8, w - 12):
        c.paint(c.rect(x, 18, x + 4, ground), TIMBER[2])
        c.paint(c.rect(x, 18, x + 1, ground), TIMBER[4])
    for x in range(20, w - 18, 12):
        spear = x % 24 == 8
        top = 6 if spear else 14
        c.paint(c.rect(x, top + 6, x + 2, ground - 3), TIMBER[3] if spear else STEEL[1])
        if spear:
            c.paint(c.polygon([(x - 2, top + 7), (x + 1, top), (x + 4, top + 7)]), STEEL[2])
        else:
            c.paint(c.rect(x - 3, ground - 16, x + 5, ground - 14), TIMBER[3])
            c.paint(c.rect(x, top + 6, x + 1, ground - 16), STEEL[3])
    c.paint(c.rect(4, 22, w - 4, 26), TIMBER[3])
    c.paint(c.rect(4, 22, w - 4, 23), TIMBER[4])
    c.paint(c.rect(4, ground - 8, w - 4, ground - 5), TIMBER[2])
    return outline(c.image), (w // 2, ground)


def hay_bale():
    w, h = 44, 30
    c = Canvas(w, h)
    ground = h - 2
    bale = c.rect(3, 6, w - 3, ground)
    c.paint(bale, STRAW[2])
    c.paint(bale & ((c.xx + c.yy * 3) % 7 < 2), STRAW[1])
    c.paint(c.rect(3, 6, w - 3, 8), STRAW[3])
    for x in (13, w - 15):
        c.paint(c.rect(x, 6, x + 2, ground), ROPE[0])
    return outline(c.image), (w // 2, ground)


# ── 맵 ───────────────────────────────────────────────────

def fence_cells():
    top = [(x, TOP) for x in range(LEFT, RIGHT + 1)]
    bottom = [(x, BOTTOM) for x in range(LEFT, RIGHT + 1)]
    sides = [(x, y) for y in range(TOP + 1, BOTTOM) for x in (LEFT, RIGHT)]
    return top + bottom + sides


def build():
    m = MapLayout("trainingGround", "훈련장", WIDTH, HEIGHT, seed=211)
    yard = (m.cx > LEFT) & (m.cx < RIGHT + 1) & (m.cy > TOP) & (m.cy < BOTTOM + 1)
    m.kinds[yard] = "path"
    m.fence(fence_cells())

    # 위쪽 울타리 곁: 과녁 둘과 무기 걸이, 구석의 짚단과 통.
    m.stand("target_a", target_board(), 8.5, TOP + 3, block=(1.4, 0.5))
    m.stand("target_b", target_board(), 12.5, TOP + 3, block=(1.4, 0.5))
    m.stand("weapon_rack", weapon_rack(), 18.5, TOP + 3, block=(2.6, 0.5))
    m.stand("hay_a", hay_bale(), LEFT + 2.5, BOTTOM - 0.2, block=(1.4, 0.7))
    m.stand("hay_b", hay_bale(), RIGHT - 1.4, BOTTOM - 0.2, block=(1.4, 0.7))
    m.thing("barrel", LEFT + 1, TOP + 2)
    m.thing("crate", RIGHT - 1, TOP + 2)
    m.stamp("flowers", (1, 3), (25, 5), (1, 13), (26, 11))

    # 둘레는 숲이다. 수관이 마당을 덮지 않게 그 자리 나무는 뺀다.
    woods = ~yard
    shades_yard = forest.canopy_over(yard, side=1, rows=3)
    forest.plant(m, woods & ~shades_yard, np.random.default_rng(212))
    m.block_where(woods)
    m.border()

    m.point("start", START)
    m.point("dummies", *DUMMIES)
    return m


def main(project):
    write_dummy(project)
    m = build()
    for cell in DUMMIES:
        if not m.reachable(START, [cell]):
            print(f"시작 칸에서 허수아비 {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
