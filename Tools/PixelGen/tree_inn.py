"""고목의 쉼터(treeInn, 11장)를 새로 짠다(사용자 요청 2026-11-03: 11장 맵을 생성기로).

거대고목 3층 요정 마을의 여러 방 가운데 하나, 요정들이 꾸리는 쉼터(사용자 요청 2026-11-03: 쉼터는 1층이 아니라 여러 방 가운데 하나).
방 하나에 이불을 양쪽에 세 채씩 깔아 두었다:
  - 북쪽 벽면은 살아 있는 나무결(이끼, 빛나는 버섯)에 둥근 창 둘과 돌 벽난로. 바닥은 고목 나이테를 그대로 깎은 둥근 결이다.
  - 요정 이불 여섯 채(누빈 이불, 베개), 남동쪽 작은 탁자와 의자, 구석의 덩굴 등불과 빛나는 버섯, 남쪽 문.
웹게임 맵(칸 15×11)보다 한 줄 아래로 내렸다(북쪽 벽면이 맵 위 끝에서 잘리지 않게). 지점 이름은 같다.

사용: python Tools/PixelGen/tree_inn.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/treeInn/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/treeInn_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import fairy_props as fp
import home_furniture as hf
import pixelgen as pg
from houses import Canvas, outline
from layout import LOGICAL, MapLayout
from room_interior import FACE, RoomInterior

T = pg.TILE
WIDTH, HEIGHT = 15, 12
ROOM = (1, 2, 14, 11)            # 바닥 칸 (x0, y0, x1, y1)
DOOR = (7, 11)                   # 남쪽 문지방 칸
HEARTH = 7.5
WINDOWS = (3.5, 11.5)
RINGS = pg.hexes("#4a3020", "#5e3e28", "#704c32", "#82593a", "#946844", "#a8784e")
QUILTS = [pg.hexes(*c) for c in (("#a85a72", "#d8849a", "#f2b6c6"), ("#4a8a72", "#74b296", "#a6d8bc"), ("#6e5aa0", "#9682c8", "#c4b4e8"),
                                  ("#4a76a8", "#72a0d0", "#a8cceb"), ("#b08a3a", "#d8b45e", "#f2d896"), ("#b0684a", "#d89270", "#f2bea0"))]
LINEN = pg.hexes("#b8ae9a", "#ddd4c2", "#f6f0e2")
# 이불 자리(칸): 서쪽 셋, 동쪽 셋. 웹게임 bed·bedDiel·bedLargo, bedRang·bedIsabel·bedAurora 칸 y + 1.
BEDS = {"bed": (3, 3), "bedDiel": (3, 5), "bedLargo": (3, 7), "bedRang": (10, 3), "bedIsabel": (10, 5), "bedAurora": (10, 7)}


def living_wood(room, rng):
    """북쪽 벽면을 살아 있는 나무결로 덮는다: 세로 결, 이끼, 빛나는 옹이(창·벽난로는 그 뒤에 그린다)."""
    c = room.c
    x0, row, x1 = ROOM[0], ROOM[1], ROOM[2]
    face = c.rect(x0 * T, row * T - FACE, x1 * T, row * T)
    fp.bark_texture(c, face, rng, x0 * T, x1 * T, grain=11)
    fp.moss_patches(c, face, rng, amount=0.7, spacing=10)
    c.paint(face & (c.yy >= row * T - 4), fp.BARK[1])
    for x, y in ((1.6, row - 1.2), (13.2, row - 1.0)):
        fp.shelf_fungus(c, x * T, y * T, 14)


def ring_floor(room):
    """고목 나이테를 그대로 깎은 바닥: 방 가운데를 중심으로 둥근 결이 퍼지고, 결마다 밝기가 다르며 가는 갈라짐이 지나간다."""
    c = room.c
    x0, y0, x1, y1 = ROOM
    floor = c.rect(x0 * T, y0 * T, x1 * T, y1 * T)
    cx, cy = (x0 + x1) / 2 * T, (y0 + y1) / 2 * T
    d = np.hypot((c.xx - cx) / 1.25, c.yy - cy)
    wobble = pg.noise(c.size[1], c.size[0], 18, room.rng) * 6
    band = np.floor((d + wobble) / 7)
    level = 2 + ((band * 7) % 3).astype(int) - (np.mod(d + wobble, 7) < 1)
    c.paint(floor, RINGS[np.clip(level, 0, 5)])
    angle = np.arctan2(c.yy - cy, c.xx - cx)
    crack = floor & (np.abs(np.mod(angle * 5 / np.pi + 0.3, 1) - 0.5) < 0.012) & (d > 40)
    c.paint(crack, RINGS[0])
    c.paint(floor & (c.yy < y0 * T + 6), RINGS[1])


def futon(rng, quilt):
    """요정 이불 한 채(바닥 그림): 크림색 요 위에 누빈 이불, 왼쪽 끝에 흰 베개."""
    w, h = int(2.8 * T), int(1.2 * T)
    c = Canvas(w, h)
    mattress = c.rect(1, 3, w - 1, h - 1)
    c.paint(mattress, LINEN[1])
    c.paint(mattress & (c.yy >= h - 4), LINEN[0])
    cover = c.rect(22, 2, w - 2, h - 3)
    c.paint(cover, quilt[1])
    c.paint(cover & ((np.mod(c.xx - 22, 12) < 1) | (np.mod(c.yy - 2, 10) < 1)), quilt[0])
    c.paint(cover & (np.mod(c.xx + c.yy, 12) == 6) & (np.mod(c.xx - 22, 12) > 2), quilt[2])
    c.paint(cover & (c.yy < 5), quilt[2])
    pillow = c.ellipse(12, h / 2, 10, 8)
    c.paint(pillow, LINEN[2])
    c.paint(pillow & (c.yy > h / 2 + 3), LINEN[1])
    return outline(c.image)


def build():
    m = MapLayout("treeInn", "고목의 쉼터", WIDTH, HEIGHT, theme="interior", music="hope",
                  ambient={"color": "#0e1430", "alpha": 0.4}, seed=421)
    room = RoomInterior(WIDTH, HEIGHT, [ROOM], seed=421)
    row = ROOM[1]
    room.paint_base()
    living_wood(room, np.random.default_rng(422))
    ring_floor(room)
    for x in WINDOWS:
        room.window(x, row)
    room.fireplace(HEARTH, row)
    room.entrance(*DOOR)
    m.paint_ground = room.ground
    m.block_where(~room.floor)
    rng = np.random.default_rng(423)

    m.fire(7, 1, "fire")
    m.light(HEARTH * LOGICAL, 1.7 * LOGICAL, 60, "#ff9a40", flicker=True)
    for x in WINDOWS:
        m.light(x * LOGICAL, 2.2 * LOGICAL, 34, "#9ab4ff")
    for i, (name, (x, y)) in enumerate(BEDS.items()):
        m.decal(f"futon_{name}", futon(rng, QUILTS[i]), (x - 1.0) * T, (y - 0.1) * T)
    m.stand("tea_table", hf.supper_table(rng), 12.3, 10, block=(2.0, 0.6))
    m.stand("tea_chair", hf.chair(rng, "right"), 10.8, 10, block=(0.6, 0.4))
    m.light(12.3 * LOGICAL, 9.2 * LOGICAL, 28, "#ffc070", flicker=True)
    for i, (x, ground) in enumerate(((1.5, 10.6), (13.5, 3.2), (1.5, 3.2))):
        m.stand(f"lantern_{i}", fp.fairy_lantern(np.random.default_rng(430 + i)), x, ground)
        m.light((x + 0.3) * LOGICAL, ground * LOGICAL - 26, 30, "#7ff0dc")
    for i, (x, y) in enumerate(((6.2, 3.4), (8.8, 3.4), (13.3, 8.6), (1.6, 6.8))):
        m.stand(f"shrooms_{i}", fp.glow_shrooms(np.random.default_rng(440 + i)), x, y)

    for name, cell in BEDS.items():
        m.point(name, cell)
    m.point("window", (11, row))
    m.point("keeper", (5, 9))
    m.point("enter", (DOOR[0], DOOR[1] - 1))
    m.point("exit", DOOR)
    m.warp("exit", "treeRooms", "innArrive", "down")
    return m


def main(project):
    m = build()
    start = m.points["enter"][0]
    for name, cells in m.points.items():
        for cell in cells:
            if not m.reachable(start, [cell]):
                print(f"문에서 {name} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
