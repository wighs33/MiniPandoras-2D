"""에라드의 탑(eradTower, 11장)을 새로 짠다(사용자 요청 2026-11-03: 11장 맵을 생성기로).

페르니 앞에 간부들이 모이는 탑 꼭대기 왕좌의 방. 뱀탑 층과 같은 둥근 탑 방(interior.RoundRoom)을 에라드 빛깔로 그린다:
푸른 기가 도는 흰 대리석 바닥과 벽, 남색 융단과 금테, 벽면의 흰 깃발(금빛 날개와 해, 에라드 군함 돛과 같은 문장)과 좁은 창,
북쪽에 세 단 대리석 단과 날개 등받이 왕좌, 단 양옆 화로, 둘레 기둥. 남쪽 출입문에서 단까지 융단이 이어진다.
웹게임 맵(칸 21×16)과 지점 자리가 같다(perni, raco, luwan, door. 간부 셋은 페르니 자리에서 잰 칸에 선다).

사용: python Tools/PixelGen/erad_tower.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/eradTower/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/eradTower_preview.png
"""
import sys
from contextlib import contextmanager
from pathlib import Path

import numpy as np

import castle
import interior
import pixelgen as pg
from houses import Canvas, outline
from interior import RoundRoom
from layout import LOGICAL, MapLayout

T = pg.TILE
WIDTH, HEIGHT = 21, 16
CENTER, RADIUS = (10.5, 9.0), 6.8
FACE = 2.0                       # 북쪽 벽면 높이(칸): 맵 위 끝 안에 들어오게 뱀탑보다 낮다
STONE = pg.hexes("#262a38", "#3a4054", "#555d76", "#757e98", "#959eb6", "#b8c0d2", "#d9dee8", "#f0f2f7")
FLOOR = pg.hexes("#242836", "#363c4e", "#4e5568", "#666e84", "#80889e", "#9ca3b7")
CARPET = pg.hexes("#0c1636", "#142558", "#1e3a7a", "#2c529c", "#e2c26e")
GOLD = pg.hexes("#6a4a12", "#a8782a", "#dcae4a", "#f6dc8a", "#fff6cc")
CLOTH = pg.hexes("#b8bccb", "#dcdfe8", "#f6f7fb")


@contextmanager
def erad_palette():
    """같은 둥근 탑 방 그리기를 에라드 빛깔로 쓴다: 방 그리기가 읽는 바닥·돌·융단 빛깔을 잠시 바꿔 둔다."""
    saved = interior.FLOOR, interior.CARPET, castle.STONE
    interior.FLOOR, interior.CARPET, castle.STONE = FLOOR, CARPET, STONE
    try:
        yield
    finally:
        interior.FLOOR, interior.CARPET, castle.STONE = saved


def wing_sun(c, cx, cy, scale):
    """에라드 문장: 금빛 해 둘레로 펼친 두 날개(깃 세 겹). scale은 날개 끝까지의 길이(점)."""
    for side in (-1, 1):
        for layer, (reach, lift, thick) in enumerate(((1.0, 0.4, 0.17), (0.8, 0.23, 0.13), (0.57, 0.1, 0.13))):
            t = np.clip(side * (c.xx - cx) / (reach * scale), 0, 1)
            centre = cy - lift * scale * np.sin(t * np.pi * 0.85) + layer * scale * 0.13 - scale * 0.06
            half = thick * scale * (1 - t * 0.6)
            wing = (side * (c.xx - cx) >= scale * 0.16) & (side * (c.xx - cx) <= reach * scale) & (np.abs(c.yy - centre) <= half)
            c.paint(wing, GOLD[3 if layer == 0 else 2])
    sun = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
    c.paint(sun <= scale * 0.2, GOLD[2])
    c.paint(sun <= scale * 0.14, GOLD[4])


def erad_banner(room, x):
    """벽면에 건 흰 깃발: 금 막대에 걸린 흰 천, 금테와 날개·해 문장, 아래는 갈라진 제비 꼬리."""
    c = room.c
    cx = int(x * T + 16)
    top = int(room.face_y(x) - room.face + 8)
    bottom = int(room.face_y(x) - 10)
    w = 26
    c.paint(c.rect(cx - w // 2 - 4, top - 3, cx + w // 2 + 4, top), GOLD[1])
    cloth = c.rect(cx - w // 2, top, cx + w // 2, bottom) & ~(c.polygon([(cx - 6, bottom + 1), (cx + 6, bottom + 1), (cx, bottom - 8)]))
    c.paint(cloth, CLOTH[1])
    c.paint(cloth & (c.xx < cx - w // 2 + 5), CLOTH[2])
    c.paint(cloth & (c.xx > cx + w // 2 - 4), CLOTH[0])
    c.paint(cloth & ~c.rect(cx - w // 2 + 2, top + 2, cx + w // 2 - 2, bottom - 2), GOLD[2])
    wing_sun(c, cx, (top + bottom) / 2 - 4, 12)


def dais(room, x0, x1, rows):
    """북쪽 벽 앞의 세 단 대리석 단(칸 x0~x1, 단마다 한 줄): 단마다 윗면은 밝고 앞면 띠는 어둡고, 맨 아래 단에 금테."""
    c = room.c
    for k, (y0, y1) in enumerate(rows):
        inset = k * 0.6
        step = c.rect((x0 + inset) * T, y0 * T, (x1 - inset) * T, y1 * T) & room.floor
        c.paint(step, STONE[5 - k % 2])
        c.paint(step & (c.yy >= y1 * T - 6), STONE[3])
        c.paint(step & (c.yy >= y1 * T - 2), STONE[2])
        if k == 0:
            c.paint(step & (c.yy >= y1 * T - 8) & (c.yy < y1 * T - 6), GOLD[2])


def throne(rng):
    """날개 등받이 왕좌: 높은 흰 대리석 등받이 위로 금빛 날개 장식이 펼쳐지고, 남색 방석과 금 팔걸이."""
    w, h = 76, 96
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    back = c.arch(cx - 16, ground - 78, cx + 16, ground - 22)
    c.paint(back, STONE[5])
    c.paint(back & (c.xx < cx - 8), STONE[6])
    c.paint(back & (c.xx > cx + 9), STONE[3])
    c.paint(back & ~c.arch(cx - 13, ground - 75, cx + 13, ground - 22), GOLD[2])
    wing_sun(c, cx, ground - 74, 34)
    seat = c.rect(cx - 20, ground - 30, cx + 20, ground - 12)
    c.paint(seat, STONE[4])
    c.paint(c.rect(cx - 16, ground - 30, cx + 16, ground - 24), CARPET[3])
    c.paint(c.rect(cx - 16, ground - 26, cx + 16, ground - 24), CARPET[2])
    for side in (-1, 1):
        arm = c.rect(cx + side * 20 - (4 if side > 0 else 0), ground - 36, cx + side * 20 + (0 if side > 0 else 4), ground - 12)
        c.paint(arm, GOLD[2])
        c.paint(arm & (c.yy < ground - 33), GOLD[4])
    c.paint(c.rect(cx - 22, ground - 12, cx + 22, ground), STONE[3])
    c.paint(c.rect(cx - 22, ground - 12, cx + 22, ground - 10), STONE[5])
    return outline(c.image), (cx, ground)


def build():
    m = MapLayout("eradTower", "에라드의 탑", WIDTH, HEIGHT, theme="castle", music="boss",
                  ambient={"color": "#0a0610", "alpha": 0.5}, seed=361)
    rng = np.random.default_rng(362)
    with erad_palette():
        room = RoundRoom(WIDTH, HEIGHT, CENTER, RADIUS, face=FACE, seed=361)
        room.paint_floor()
        room.paint_walls()
        room.carpet(9, 5, 12, 16)
        dais(room, 6, 15, ((4, 5), (3, 4), (2, 3)))
        room.carpet(9, 2, 12, 5)
        for x in (7, 14):
            room.slit_window(x)
        for x in (9, 12):
            erad_banner(room, x)
        entrance = room.entrance(11)
        ground = room.ground()
        m.paint_ground = lambda: ground
        m.block_where(room.blocked_cells())
        for x in (5, 16):
            room.torch(m, x)
        pillar = interior.pillar(rng)
    m.stand("throne", throne(np.random.default_rng(363)), 10.5, 3.0, block=(2.2, 0.6))
    for x, y in ((5, 9), (16, 9), (6, 13), (15, 13)):
        m.stand("pillar", pillar, x + 0.5, y + 1, block=(1, 1))
    for x, y in ((6, 4), (15, 4)):
        image, pivot = castle.brazier(rng)
        m.upright("brazier", image, x * T + T // 2, (y + 1) * T - 1, pivot, (y + 1) * LOGICAL)
        m.block(x, y, x + 1, y + 1)
        m.fire(x, y - 1)
        m.light(x * 16 + 8, y * 16 - 4, 44, "#ffb45a", flicker=True)
    m.light(10.5 * LOGICAL, 3.5 * LOGICAL, 56, "#f6e8c0")
    m.point("perni", (10, 3))
    m.point("raco", (9, 6))
    m.point("luwan", (13, 6))
    m.point("door", entrance[0])
    m.keep_open(9, 15, 12, 16)
    return m


def main(project):
    m = build()
    start = m.points["door"][0]
    for name, cells in m.points.items():
        for cell in cells:
            if not m.reachable(start, [cell]):
                print(f"문에서 {name} {cell}에 닿지 못한다")
    for cell in ((6, 6), (14, 6), (10, 8)):
        if not m.reachable(start, [cell]):
            print(f"간부 자리 {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
