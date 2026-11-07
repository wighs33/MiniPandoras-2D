"""힐리아 마을 모험가 길드 안(adventurersGuild)을 짠다(큰 건물로 들어가 안내원 힐라를 만난다).

방 하나짜리 큰 홀이다. 남쪽 문으로 들어오면 붉은 깔개가 곧장 북쪽 접수대로 이어진다.
  - 북쪽: 접수대와 그 뒤의 안내원 자리. 뒷벽에 길드 문장, 양옆은 책장으로 막혀 있다.
  - 서쪽: 의뢰 게시판과 무기 걸이, 쉬어 가는 탁자 둘.
  - 동쪽: 벽난로와 안락의자, 술잔이 놓인 탁자, 갑옷 진열대.
바닥과 벽은 room_interior.RoomInterior가 그린다.

지점: enter(문 안쪽), exit(문), hila(안내원 자리), counter(접수대 가운데 칸. 여기를 보고 말을 건다).

사용: python Tools/PixelGen/adventurers_guild.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/adventurersGuild/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/adventurersGuild_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import guild_furniture as gf
import interior
import pixelgen as pg
from houses import IRON, WOOD, Canvas, outline
from layout import LOGICAL, MapLayout
from room_interior import RoomInterior, face_decal, face_top, floor_decal

T = pg.TILE
WIDTH, HEIGHT = 27, 14
ROOM = (1, 2, 26, 13)            # 바닥 칸 (x0, y0, x1, y1)
DOOR = (13, 13)                  # 남쪽 문지방 칸
HEARTH = 22.5
WINDOWS = (4.5, 8.5, 18.5)
COUNTER_LEFT, COUNTER_RIGHT = 9, 18    # 접수대가 차지하는 열(끝 열은 들지 않는다)
COUNTER_ROW = 5
MIDDLE = (COUNTER_LEFT + COUNTER_RIGHT) / 2     # 문, 깔개, 접수대, 문장이 이 열에 맞춰 선다
LAMP = "#ffc070"
CLOTH = pg.hexes("#4a1420", "#7a2030", "#a83040", "#d05050")
CREST = pg.hexes("#2a3450", "#3e5078", "#5a74a0")
METAL = pg.hexes("#6e7482", "#a8aeba", "#dfe3ea")
GILT = pg.hexes("#7a5a20", "#c8963a", "#ecc876")


def counter(columns, rng):
    """안내 접수대: 앞면은 판자에 금빛 띠, 윗면에 서류철과 작은 종과 잉크병."""
    w, h = columns * T, 60
    c = Canvas(w, h)
    ground = h - 2
    front = c.rect(0, 20, w, ground)
    c.paint(front, WOOD[2])
    c.paint(front & (np.mod(c.xx, 28) < 2), WOOD[1])
    c.paint(front & (np.mod(c.xx + 7, 28) < 1), WOOD[3])
    c.paint(c.rect(0, 30, w, 33), GILT[1])
    c.paint(c.rect(0, 30, w, 31), GILT[2])
    c.paint(c.rect(0, ground - 5, w, ground), WOOD[1])
    top = c.rect(0, 6, w, 20)
    c.paint(top, WOOD[4])
    c.paint(top & (c.yy < 8), WOOD[5])
    c.paint(top & (c.yy >= 18), WOOD[3])
    papers = pg.hexes("#efe6cc", "#c8b88a")
    for k, x in enumerate(range(26, w - 40, 60)):
        c.paint(c.rect(x, 9, x + 22, 17), papers[0])
        c.paint(c.rect(x + 10, 9, x + 12, 17), papers[1])
        for y in range(11, 16, 2):
            c.paint(c.rect(x + 3, y, x + 8, y + 1), WOOD[1])
            c.paint(c.rect(x + 14, y, x + 19, y + 1), WOOD[1])
    bell = w // 2
    c.paint(c.ellipse(bell, 7, 6, 6) & (c.yy < 12), GILT[1])
    c.paint(c.ellipse(bell - 1, 5, 2, 2), GILT[2])
    c.paint(c.rect(bell - 7, 11, bell + 7, 13), WOOD[1])
    c.paint(c.ellipse(w - 22, 11, 4, 4), IRON[1])
    c.line(w - 21, 10, w - 14, 0, pg.hexes("#f4f0e8")[0], 1)
    return outline(c.image), (w // 2, ground)


def wall_crest():
    """뒷벽의 길드 문장: 붉은 천에 푸른 방패와 엇갈린 두 칼."""
    w, h = 66, 44
    c = Canvas(w, h)
    cx = w // 2
    cloth = c.polygon([(6, 0), (w - 6, 0), (w - 6, h - 12), (cx, h - 1), (6, h - 12)])
    c.paint(cloth, CLOTH[1])
    c.paint(cloth & (c.xx < 12), CLOTH[2])
    c.paint(cloth & (c.xx >= w - 12), CLOTH[0])
    c.paint(c.rect(4, 0, w - 4, 3), GILT[1])
    shield = c.polygon([(cx - 13, 7), (cx + 13, 7), (cx + 13, 20), (cx, 33), (cx - 13, 20)])
    c.paint(shield, CREST[1])
    c.paint(shield & (c.xx < cx), CREST[2])
    c.line(cx - 12, 29, cx + 12, 6, METAL[2], 2)
    c.line(cx + 12, 29, cx - 12, 6, METAL[1], 2)
    for x, y in ((cx - 9, 25), (cx + 9, 25)):
        c.paint(c.rect(x - 3, y - 1, x + 3, y + 1), GILT[2])
    return outline(c.image)


def carpet(columns, rows):
    """문에서 접수대까지 이어지는 붉은 깔개와 벽난로 앞 깔개."""
    w, h = int(columns * T), int(rows * T)
    c = Canvas(w, h)
    all_of_it = c.rect(0, 0, w, h)
    c.paint(all_of_it, CLOTH[1])
    c.paint(all_of_it & (np.mod(c.yy, 24) < 2), CLOTH[0])
    c.paint(c.rect(0, 0, 4, h) | c.rect(w - 4, 0, w, h), GILT[1])
    c.paint(c.rect(1, 0, 2, h) | c.rect(w - 2, 0, w - 1, h), GILT[2])
    return c.image


def wall_trim(m, room, rng):
    row = ROOM[1]
    crest = wall_crest()
    m.decal("crest", crest, MIDDLE * T - crest.shape[1] / 2, face_top(row) + 1)
    for i, x in enumerate((2.6, 6.6, 20.6, 25)):
        face_decal(m, f"sconce_{i}", gf.sconce(rng), x, row)
        m.light(x * LOGICAL, (face_top(row) + 28) / 2, 38, LAMP)
    for i, x in enumerate((MIDDLE - 3, MIDDLE + 3)):
        face_decal(m, f"shelf_{i}", gf.shelf_books(rng), x, row)


def west_side(m, rng):
    m.stand("quest_board", interior.notice_board(rng), 2.7, 3.4, block=(1.4, 0.5))
    m.stand("weapon_rack", interior.weapon_rack(rng), 6, 3.4, block=(2, 0.5))
    for i, (x, ground) in enumerate(((5, 8.4), (5, 11.6))):
        m.stand(f"table_west_{i}", gf.round_table(rng), x, ground, block=(1.4, 0.6))
        m.stand(f"stool_west_{i}a", gf.stool(rng), x - 1.2, ground + 0.2, block=(0.5, 0.3))
        m.stand(f"stool_west_{i}b", gf.stool(rng), x + 1.2, ground + 0.2, block=(0.5, 0.3))
    m.stand("planter_west", interior.planter(rng), 1.8, 12.5, block=(1, 0.5))


def east_side(m, rng):
    floor_decal(m, "rug_hearth", carpet(5, 3), 20, 3.4)
    m.stand("armchair_left", gf.armchair(rng), 20.2, 6.2, block=(1.2, 0.6))
    m.stand("armchair_right", gf.armchair(rng), 24.8, 6.2, block=(1.2, 0.6))
    for i, (x, ground) in enumerate(((22, 9), (22, 12))):
        m.stand(f"table_east_{i}", gf.round_table(rng), x, ground, block=(1.4, 0.6))
        m.stand(f"stool_east_{i}a", gf.stool(rng), x - 1.2, ground + 0.2, block=(0.5, 0.3))
        m.stand(f"stool_east_{i}b", gf.stool(rng), x + 1.2, ground + 0.2, block=(0.5, 0.3))
    m.stand("weapon_barrel", interior.weapon_barrel(rng), 25.2, 9.6, block=(0.9, 0.5))
    m.stand("planter_east", interior.planter(rng), 25.2, 12.5, block=(1, 0.5))


def reception(m, rng):
    columns = COUNTER_RIGHT - COUNTER_LEFT
    m.stand("counter", counter(columns, rng), MIDDLE, COUNTER_ROW + 1, block=(columns, 1))
    # 접수대 양 끝은 책장으로 막아 안내원 자리에 들어가지 못하게 한다.
    m.stand("shelf_counter_left", interior.bookshelf(rng), COUNTER_LEFT - 1, COUNTER_ROW + 1, block=(2, 1))
    m.stand("shelf_counter_right", interior.bookshelf(rng), COUNTER_RIGHT + 1, COUNTER_ROW + 1, block=(2, 1))
    m.block(COUNTER_LEFT - 2, ROOM[1], COUNTER_LEFT, COUNTER_ROW + 1)
    m.block(COUNTER_RIGHT, ROOM[1], COUNTER_RIGHT + 2, COUNTER_ROW + 1)
    floor_decal(m, "carpet", carpet(4, ROOM[3] - COUNTER_ROW - 1), MIDDLE - 2, COUNTER_ROW + 1)


def build():
    m = MapLayout("adventurersGuild", "모험가 길드", WIDTH, HEIGHT, theme="interior", music="town",
                  ambient={"color": "#201018", "alpha": 0.25}, seed=451)
    room = RoomInterior(WIDTH, HEIGHT, [ROOM], seed=451)
    row = ROOM[1]
    room.paint_base()
    for x in WINDOWS:
        room.window(x, row)
    room.fireplace(HEARTH, row)
    room.entrance(*DOOR)
    m.paint_ground = room.ground
    m.block_where(~room.floor)
    rng = np.random.default_rng(452)

    m.fire(22, 1, "fire")
    m.light(HEARTH * LOGICAL, 1.7 * LOGICAL, 64, "#ff9a40", flicker=True)
    wall_trim(m, room, rng)
    reception(m, rng)
    west_side(m, rng)
    east_side(m, rng)
    for x, ground in ((8.6, 12.4), (18.4, 12.4)):
        m.stand(f"doormat_lamp_{x}", gf.lantern(rng), x, ground, block=(0.4, 0.3))
        m.light(x * LOGICAL, ground * LOGICAL - 16, 30, LAMP, flicker=True)

    m.point("enter", (DOOR[0], DOOR[1] - 1))
    m.point("exit", DOOR)
    m.point("hila", (13, COUNTER_ROW - 1))
    m.point("counter", (13, COUNTER_ROW), blocked=True)
    m.warp("exit", "town", "guildArrive", "down")
    return m


def main(project):
    m = build()
    start = m.points["enter"][0]
    if not m.reachable(start, [(13, COUNTER_ROW + 1)]):
        print("문에서 접수대 앞에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
