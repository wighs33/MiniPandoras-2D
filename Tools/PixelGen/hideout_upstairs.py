"""뉴웨이브 아지트 2층과 방을 짠다(길드 2층과 방을 맵으로. 2층에서 오로라가 인사하고, 방의 가로 침대에서 디엘이 잔다).

hideout.py처럼 두 모습을 짠다. abandoned는 지금의 낡은 아지트(5장), lively는 6장 회상 속 전성기다.
1층 계단(stairs)과 2층 계단(down)이 이어져 있어 언제든 오르내린다.

  hideoutUpper   2층 복도. 북쪽 벽에 방문 셋, 남쪽은 강당이 내려다보이는 난간과 내려가는 계단이다.
                 1층 계단은 북쪽으로 오르므로, 여기서는 남쪽 계단 위에 도착하고 남쪽으로 내려간다.
  hideoutRoom    2층 방 하나. 남쪽 문으로 들어온다. 북쪽 벽에 머리를 왼쪽에 둔 가로 침대가 있다.
                 침대는 바닥 장식이고 이불은 따로 세운 소품이라, 침대에 누운 인물 앞에 이불이 그려진다.

사용: python Tools/PixelGen/hideout_upstairs.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/hideoutUpper/…, hideoutRoom/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/hideoutUpper_preview.png, hideoutRoom_preview.png   미리보기
"""
import sys
from pathlib import Path

import numpy as np

import guild_furniture as gf
import interior
import pixelgen as pg
import yard
from houses import BRASS, CLOTH, PAPER, WOOD, Canvas, outline
from layout import LOGICAL, MapLayout
from room_interior import FACE, FLOORBOARD, RoomInterior, face_decal, face_top, floor_decal

T = pg.TILE
LAMP = "#ffc070"
SKY = "#a8c8f0"

# ── 2층 ───────────────────────────────────────────────
UPPER_WIDTH, UPPER_HEIGHT = 26, 12
CORRIDOR = (1, 2, 25, 7)
STAIRWELL = (17, 7, 20, 11)     # 내려가는 계단 칸(복도 남쪽으로 튀어나온다)
OVERLOOK = (1, 7, 16, 11)       # 난간 너머로 내려다보이는 강당
DOORS = (5, 12, 20)             # 복도 북쪽 벽의 방문 열. 가운데 문이 hideoutRoom으로 간다
WINDOWS = (2.5, 8.5, 16.0, 23.5)

# ── 방 ────────────────────────────────────────────────
ROOM_WIDTH, ROOM_HEIGHT = 14, 11
ROOM = (1, 2, 13, 10)
ROOM_DOOR = (6, 10)
BED_CELL = (9, 3)               # 누운 인물의 몸 가운데 칸
ROOM_WINDOWS = (3.5,)

# 침대 그림(점). 매트리스 가운데가 BED_CELL 가운데에 온다.
BED_W, BED_H = 124, 86
MATTRESS = (14, 24, 110, 66)    # 왼쪽, 위, 오른쪽, 아래
BLANKET_FROM = 50               # 이불이 덮는 왼쪽 끝(점). 그 왼쪽이 베개와 어깨 자리다
LINEN = pg.hexes("#b8ae9a", "#ddd4c2", "#f6f0e2")
STAINED = pg.hexes("#6e6250", "#8a7c66", "#a08f76")
TEAL = CLOTH["teal"]


# ── 소품 ──────────────────────────────────────────────

def gallery_rail(columns):
    """강당을 내려다보는 2층 난간: 굵은 윗 난간과 동자 기둥, 네 칸마다 큰 기둥."""
    w, h = columns * T, 34
    c = Canvas(w + 2, h)
    ground = h - 1
    for x in range(2, w, 7):
        c.paint(c.rect(x, 8, x + 3, ground), WOOD[3])
        c.paint(c.rect(x, 8, x + 1, ground), WOOD[4])
    c.paint(c.rect(0, 5, w + 2, 10), WOOD[3])
    c.paint(c.rect(0, 5, w + 2, 6), WOOD[5])
    c.paint(c.rect(0, 9, w + 2, 10), WOOD[1])
    c.paint(c.rect(0, ground - 3, w + 2, ground), WOOD[1])
    for x in list(range(0, w, 4 * T)) + [w - 5]:
        c.paint(c.rect(x, 0, x + 7, ground), WOOD[3])
        c.paint(c.rect(x, 0, x + 2, ground), WOOD[5])
        c.paint(c.rect(x + 6, 0, x + 7, ground), WOOD[1])
        c.paint(c.rect(x - 1, 0, x + 8, 3), BRASS[3])
    return outline(c.image), ((w + 2) // 2, ground)


def wide_bed(rng, bare=False):
    """
    머리를 왼쪽에 둔 가로 나무 침대(바닥 장식): 왼쪽 높은 머리판과 오른쪽 낮은 발판, 앞 옆판과 짧은 다리, 맨 시트와 베개.
    이불은 quilt()로 따로 세운다. bare면 얼룩진 맨 매트리스뿐이다(낡은 아지트).
    """
    c = Canvas(BED_W, BED_H)
    left, top, right, bottom = MATTRESS
    # 앞 옆판과 다리
    for x in (left + 4, right - 12):
        c.paint(c.rect(x, bottom + 12, x + 8, bottom + 18), WOOD[1])
    rail = c.rect(left - 2, bottom, right + 2, bottom + 13)
    c.paint(rail, WOOD[3])
    c.paint(c.rect(left - 2, bottom, right + 2, bottom + 2), WOOD[5])
    c.paint(c.rect(left - 2, bottom + 11, right + 2, bottom + 13), WOOD[1])
    for x in range(left + 8, right, 19):
        c.paint(c.rect(x, bottom + 3, x + 1, bottom + 11), WOOD[2])
    # 뒤 판과 매트리스
    c.paint(c.rect(left - 2, top - 4, right + 2, top), WOOD[2])
    sheet = c.rect(left, top, right, bottom)
    if bare:
        c.paint(sheet, STAINED[1])
        c.paint(sheet & (c.yy < top + 3), STAINED[2])
        c.paint(sheet & (c.yy >= bottom - 4), STAINED[0])
        c.paint(sheet & (c.xx % 16 == 0), STAINED[0])
        c.paint(c.ellipse(left + 58, top + 24, 16, 9), STAINED[0])
    else:
        c.paint(sheet, LINEN[1])
        c.paint(sheet & (c.yy < top + 3), LINEN[2])
        c.paint(sheet & (c.yy >= bottom - 4), LINEN[0])
        c.paint(sheet & ((c.xx * 2 + c.yy) % 23 == 0), LINEN[0])
        # 베개
        pillow = c.ellipse(left + 15, (top + bottom) / 2, 12, 16)
        c.paint(pillow, LINEN[2])
        c.paint(pillow & (c.xx > left + 21), LINEN[1])
        c.paint(c.ellipse(left + 14, (top + bottom) / 2, 4, 8), PAPER[2])
    # 왼쪽 머리판(높다), 오른쪽 발판(낮다)
    head = c.rect(2, 6, left, bottom + 16) | c.ellipse(8, 7, 6, 4)
    c.paint(head, WOOD[3])
    c.paint(c.rect(2, 6, 5, bottom + 16), WOOD[5])
    c.paint(c.rect(left - 2, 6, left, bottom + 16), WOOD[1])
    foot = c.rect(right, top - 8, right + 10, bottom + 16)
    c.paint(foot, WOOD[2])
    c.paint(c.rect(right, top - 8, right + 3, bottom + 16), WOOD[4])
    c.paint(c.rect(right + 8, top - 8, right + 10, bottom + 16), WOOD[1])
    for x, y, r in ((8, 8, 3), (right + 5, top - 8, 3)):
        c.paint(c.ellipse(x, y, r, r), BRASS[3])
    return outline(c.image)


def quilt(rng):
    """
    침대 위 이불(세우는 소품): 매트리스 오른쪽을 덮고 앞 옆판 위로 조금 늘어진다. 왼쪽 끝은 접어 둔 밝은 깃이다.
    기준점은 그림 아래 가운데다. 침대에 누운 인물보다 앞에 그려지도록 맵에서 발밑을 침대 칸보다 아래에 둔다.
    """
    left, top, right, bottom = MATTRESS
    w, h = right - BLANKET_FROM + 4, bottom - top + 12
    c = Canvas(w, h)
    body = c.rect(2, 2, w - 2, h - 2) | c.ellipse(3, h / 2, 3, h / 2 - 2)
    c.paint(body, TEAL[1])
    c.paint(body & (c.yy < 8), TEAL[2])
    c.paint(body & (c.yy >= bottom - top), TEAL[0])
    # 누빈 줄
    c.paint(body & ((c.xx - 12) % 14 == 0) & (c.yy < bottom - top), TEAL[0])
    c.paint(body & ((c.yy - 3) % 13 == 0) & (c.yy < bottom - top), TEAL[0])
    # 접은 깃
    cuff = body & (c.xx < 13)
    c.paint(cuff, LINEN[2])
    c.paint(cuff & (c.xx >= 11), LINEN[0])
    c.paint(cuff & (c.yy >= bottom - top), LINEN[1])
    return outline(c.image), (w // 2, h - 1)


def nightstand(rng):
    """침대 곁 작은 탁자와 그 위 등잔."""
    w, h = 30, 46
    c = Canvas(w, h)
    ground = h - 2
    top = 20
    c.paint(c.rect(4, top, 26, ground), WOOD[3])
    c.paint(c.rect(4, top, 26, top + 3), WOOD[5])
    c.paint(c.rect(6, top + 6, 24, ground - 4), WOOD[2])
    c.paint(c.ellipse(15, top + 12, 1.6, 1.6), BRASS[3])
    c.paint(c.rect(11, top - 3, 19, top), BRASS[2])
    c.paint(c.rect(13, top - 12, 17, top - 3), BRASS[3])
    c.paint(c.ellipse(15, top - 14, 6, 6), pg.hexes("#ffd27a")[0])
    c.paint(c.ellipse(14, top - 15, 3, 3), pg.hexes("#fff1c4")[0])
    return outline(c.image), (15, ground)


def study_desk(rng):
    """벽에 붙인 작은 책상: 펼친 책, 잉크병, 쌓은 책."""
    w, h = 66, 54
    c = Canvas(w, h)
    ground = h - 2
    top = 22
    for x in (5, w - 10):
        c.paint(c.rect(x, top + 10, x + 5, ground), WOOD[2])
    c.paint(c.rect(2, top, w - 2, top + 11), WOOD[3])
    c.paint(c.rect(2, top, w - 2, top + 3), WOOD[5])
    c.paint(c.rect(2, top + 10, w - 2, top + 11), WOOD[1])
    c.paint(c.rect(10, top - 6, 30, top + 1), PAPER[2])
    c.paint(c.rect(19, top - 6, 21, top + 1), PAPER[0])
    c.paint(c.rect(38, top - 5, 44, top), pg.hexes("#22324a")[0])
    for k, color in enumerate((CLOTH["red"][1], CLOTH["blue"][2], TEAL[2])):
        c.paint(c.rect(50 - k, top - 4 - k * 4, 62 - k, top - k * 4), color)
    return outline(c.image), (w // 2, ground)


# ── 2층 바닥 ──────────────────────────────────────────

def overlook(c, rng):
    """난간 너머로 보이는 아래층 강당: 어둡게 가라앉은 판자 바닥. 양탄자 끝자락은 전성기에만 있다(overlook_rug)."""
    x0, y0, x1, y1 = (v * T for v in OVERLOOK)
    area = c.rect(x0, y0, x1, y1)
    planks = np.zeros(c.image.shape[:2], dtype=int)
    for row in range(y0 // 8, y1 // 8 + 1):
        offset = int(rng.integers(0, 90))
        planks[row * 8:row * 8 + 8] = 1 + (((c.xx[row * 8:row * 8 + 8] + offset) // 90 + row) % 3 == 0)
        planks[row * 8] = 0
    deep = pg.hexes("#120c0a", "#1c130d", "#241912")
    c.paint(area, deep[np.clip(planks, 0, 2)])
    # 위층 바닥의 두께
    c.paint(c.rect(x0, y0, x1, y0 + 6), WOOD[2])
    c.paint(c.rect(x0, y0 + 4, x1, y0 + 6), WOOD[0])


def overlook_rug():
    """난간 너머 강당의 어둡게 가라앉은 양탄자 끝자락."""
    w, h = 7 * T, 2 * T
    c = Canvas(w, h)
    rug = c.rect(0, 0, w, h)
    c.paint(rug, pg.hexes("#0e2a2a")[0])
    c.paint(rug & ~c.rect(5, 5, w - 5, h), pg.hexes("#5e4a1e")[0])
    return c.image


def down_stairs(c):
    """남쪽으로 내려가는 나무 계단: 내려갈수록 어두워지고, 양옆에 난간 벽이 있다."""
    x0, y0, x1, y1 = (v * T for v in STAIRWELL)
    steps = (y1 - y0) // 8
    for k in range(steps):
        level = max(0, 5 - k * 6 // steps)
        top = y0 + k * 8
        step = c.rect(x0 + 5, top, x1 - 5, top + 8)
        c.paint(step, FLOORBOARD[level])
        c.paint(step & (c.yy == top), FLOORBOARD[min(5, level + 1)])
        c.paint(step & (c.yy >= top + 6), FLOORBOARD[max(0, level - 2)])
    for left in (x0, x1 - 5):
        wall = c.rect(left, y0, left + 5, y1)
        c.paint(wall, WOOD[2])
        c.paint(c.rect(left, y0, left + 1, y1), WOOD[4])
        c.paint(c.rect(left + 4, y0, left + 5, y1), WOOD[1])
    c.paint(c.rect(x0, y1 - 10, x1, y1), pg.hexes("#100a08")[0])


def upper_abandoned(m, room):
    """지금의 낡은 2층(5장): 판자로 막은 창, 거미줄, 찢어진 깃발, 먼지. 양탄자와 화분은 치워지고 없다."""
    rng = np.random.default_rng(79)
    with m.look("abandoned"):
        for i, x in enumerate(WINDOWS):
            left, top, w, h = room.window_rect(x, CORRIDOR[1])
            m.decal(f"boards_{i}", gf.boards(w, h, rng), left, top)
            m.light(x * LOGICAL, 2.2 * LOGICAL, 20, SKY)
        for i, (x, corner) in enumerate(((1, "left"), (25, "right"))):
            m.decal(f"cobweb_{i}", gf.cobweb(26, corner), x * T - (26 if corner == "right" else 0), face_top(CORRIDOR[1]))
        face_decal(m, "banner_torn", gf.guild_banner(rng, torn=True), 14.1, CORRIDOR[1])
        for i, (x, y, w, h) in enumerate(((2, 3, 5, 3), (8, 4, 4, 2), (14, 3, 5, 3), (20, 4, 4, 3))):
            floor_decal(m, f"dust_{i}", gf.dust(w, h, rng), x, y)
        floor_decal(m, "debris_west", gf.debris(rng), 3, 5.4)
        floor_decal(m, "debris_east", gf.debris(rng), 21.5, 5.2)
        floor_decal(m, "papers", gf.papers(rng), 15.5, 5.0)
        # 벤치 자리에 쓰러진 걸상, 책장에는 천을 덮었다. 방문 앞과 계단 위는 비운다.
        m.stand("shelf_sheet", gf.sheet_lump(rng, 62, 92), 24.0, 3.0, block=(1.8, 0.6))
        m.stand("stool_down", gf.stool(rng, toppled=True), 8.4, 3.2, block=(0.6, 0.4))
        for kind, x, y in (("crate", 1, 2), ("crate", 23, 6), ("barrel", 24, 6)):
            m.thing(kind, x, y)


def upper_lively(m, rng):
    """6장 회상 속 전성기의 2층: 복도 양탄자, 길드 깃발, 벽등. 서쪽에 벤치와 화분, 동쪽에 책장과 궤짝을 둔다."""
    with m.look("lively"):
        m.decal("overlook_rug", overlook_rug(), (OVERLOOK[0] + 5) * T, (OVERLOOK[3] - 2) * T)
        floor_decal(m, "corridor_runner", gf.runner(22, 1.6, rng), 2, 3.7)
        face_decal(m, "banner_upper", gf.guild_banner(rng), 14.1, CORRIDOR[1])
        face_decal(m, "wall_map_upper", gf.wall_map(rng), 22.0, CORRIDOR[1])
        for i, x in enumerate((7.0, 10.6, 18.5)):
            face_decal(m, f"sconce_{i}", gf.sconce(rng), x, CORRIDOR[1])
            m.light(x * LOGICAL, (face_top(CORRIDOR[1]) + FACE / 2) / 2, 42, LAMP)
        for x in WINDOWS:
            m.light(x * LOGICAL, 2.2 * LOGICAL, 34, SKY)
        # 오로라와 방문 앞은 비운다.
        m.stand("planter_upper", interior.planter(rng), 1.8, 3.6, block=(1, 0.5))
        m.stand("bench_upper", yard.bench(rng), 8.5, 3.0, block=(1.8, 0.5))
        m.stand("bookshelf_upper", interior.bookshelf(rng), 24.0, 3.0, block=(1.8, 0.6))
        m.stand("planter_east", interior.planter(rng), 24.2, 7.0, block=(1, 0.5))
        m.stand("footlocker_upper", interior.footlocker(rng), 22.0, 7.0, block=(0.9, 0.5))


def build_upper():
    m = MapLayout("hideoutUpper", "뉴웨이브 아지트 2층", UPPER_WIDTH, UPPER_HEIGHT, theme="interior", music="hope", seed=75)
    room = RoomInterior(UPPER_WIDTH, UPPER_HEIGHT, [CORRIDOR, STAIRWELL], seed=75)
    room.paint_base()
    rng = np.random.default_rng(76)
    for x in WINDOWS:
        room.window(x, CORRIDOR[1])
    for x in DOORS:
        room.wall_door(x, CORRIDOR[1])
    overlook(room.c, rng)
    down_stairs(room.c)
    m.paint_ground = room.ground
    m.block_where(~room.floor)

    m.stand("gallery_rail", gallery_rail(OVERLOOK[2] - OVERLOOK[0]), (OVERLOOK[0] + OVERLOOK[2]) / 2, OVERLOOK[1] + 0.15)
    upper_abandoned(m, room)
    upper_lively(m, rng)

    m.point("arrive", (18, 6))
    m.point("down", (17, 10), (18, 10), (19, 10))
    m.point("aurora", (15, 4))
    m.point("roomDoor", (DOORS[1], 1))
    m.point("roomArrive", (DOORS[1], 2))
    m.warp("down", "hideout", "stairsArrive", "down")
    m.warp("roomDoor", "hideoutRoom", "arrive", "up")
    return m


def room_abandoned(m, room, bed_left, bed_top):
    """지금의 낡은 방(5장): 맨 매트리스 침대, 막힌 창, 비어 있는 옷장, 천 덮은 책상."""
    rng = np.random.default_rng(80)
    with m.look("abandoned"):
        m.decal("wide_bed_bare", wide_bed(rng, bare=True), bed_left, bed_top)
        for i, x in enumerate(ROOM_WINDOWS):
            left, top, w, h = room.window_rect(x, ROOM[1])
            m.decal(f"boards_{i}", gf.boards(w, h, rng), left, top)
            m.light(x * LOGICAL, 2.2 * LOGICAL, 20, SKY)
        for i, (x, corner) in enumerate(((1, "left"), (13, "right"))):
            m.decal(f"cobweb_{i}", gf.cobweb(26, corner), x * T - (26 if corner == "right" else 0), face_top(ROOM[1]))
        face_decal(m, "banner_torn", gf.guild_banner(rng, torn=True), 6.2, ROOM[1])
        for i, (x, y, w, h) in enumerate(((2, 5, 4, 3), (7, 7, 3, 2), (10, 5, 3, 2))):
            floor_decal(m, f"dust_{i}", gf.dust(w, h, rng), x, y)
        floor_decal(m, "debris_room", gf.debris(rng), 2, 8.2)
        m.stand("wardrobe_empty", gf.wardrobe(rng, empty=True), 2.2, 4.0, block=(1.4, 0.7))
        m.stand("desk_sheet", gf.sheet_lump(rng, 66, 46), 11.4, 8.4, block=(2, 0.6))
        m.stand("stool_down", gf.stool(rng, toppled=True), 10.4, 9.6, block=(0.6, 0.4))
        m.thing("crate", 12, 5)


def room_lively(m, rng, bed_left, bed_top):
    """6장 회상 속 전성기의 방: 이불 덮은 침대, 등불 켠 협탁, 옷장과 책상."""
    bed_x = BED_CELL[0]
    with m.look("lively"):
        m.decal("wide_bed", wide_bed(rng), bed_left, bed_top)
        # 이불의 발밑은 침대 칸보다 아래라 그 칸에 누운 인물보다 앞에 그려진다.
        quilt_image, quilt_pivot = quilt(rng)
        quilt_x = (bed_left + BLANKET_FROM + quilt_image.shape[1] / 2) / T
        quilt_ground = (bed_top + MATTRESS[1] + quilt_image.shape[0] - 3) / T
        m.stand("bed_quilt", (quilt_image, quilt_pivot), quilt_x, quilt_ground)

        m.stand("nightstand", nightstand(rng), bed_x - 2.6, 3.8, block=(0.9, 0.5))
        m.light((bed_x - 2.6) * LOGICAL, 3.2 * LOGICAL, 36, LAMP)
        m.stand("wardrobe_room", gf.wardrobe(rng), 2.2, 4.0, block=(1.4, 0.7))
        m.stand("desk_room", study_desk(rng), 11.4, 8.4, block=(2, 0.6))
        m.stand("stool_room", gf.stool(rng), 10.6, 9.6, block=(0.5, 0.3))
        m.stand("footlocker_room", interior.footlocker(rng), 12.2, 5.6, block=(0.9, 0.5))
        m.stand("planter_room", interior.planter(rng), 1.8, 9.4, block=(1, 0.5))
        floor_decal(m, "rug_room", gf.rug(5, 3, rng), 3.4, 5.4)
        face_decal(m, "banner_room", gf.guild_banner(rng), 6.2, ROOM[1])
        face_decal(m, "sconce_room", gf.sconce(rng), 11.6, ROOM[1])
        m.light(11.6 * LOGICAL, (face_top(ROOM[1]) + FACE / 2) / 2, 38, LAMP)
        for x in ROOM_WINDOWS:
            m.light(x * LOGICAL, 2.2 * LOGICAL, 34, SKY)


def build_room():
    m = MapLayout("hideoutRoom", "뉴웨이브 아지트 방", ROOM_WIDTH, ROOM_HEIGHT, theme="interior", music="hope", seed=77)
    room = RoomInterior(ROOM_WIDTH, ROOM_HEIGHT, [ROOM], seed=77)
    room.paint_base()
    rng = np.random.default_rng(78)
    for x in ROOM_WINDOWS:
        room.window(x, ROOM[1])
    room.entrance(*ROOM_DOOR)
    m.paint_ground = room.ground
    m.block_where(~room.floor)

    bed_x, bed_y = BED_CELL
    left = (bed_x + 0.5) * T - (MATTRESS[0] + MATTRESS[2]) / 2
    top = (bed_y + 0.5) * T - (MATTRESS[1] + MATTRESS[3]) / 2
    m.block(left / T + 0.1, top / T + 0.3, (left + BED_W) / T - 0.1, (top + MATTRESS[3] + 18) / T)
    room_abandoned(m, room, left, top)
    room_lively(m, rng, left, top)

    m.point("arrive", (ROOM_DOOR[0], ROOM_DOOR[1] - 1))
    m.point("door", ROOM_DOOR)
    m.point("bed", BED_CELL, blocked=True)
    m.warp("door", "hideoutUpper", "roomArrive", "down")
    return m


def main(project):
    upper = build_upper()
    start = upper.points["arrive"][0]
    for look in upper.look_solid:
        for name in ("down", "aurora", "roomDoor", "roomArrive"):
            for cell in upper.points[name]:
                if not upper.reachable(start, [cell], look):
                    print(f"hideoutUpper[{look}]: arrive에서 {name} {cell}에 닿지 못한다")
    print(upper.publish(project))
    room = build_room()
    for look in room.look_solid:
        if not room.reachable(room.points["arrive"][0], room.points["door"], look):
            print(f"hideoutRoom[{look}]: arrive에서 문에 닿지 못한다")
        below_bed = (BED_CELL[0], BED_CELL[1] + 2)
        if not room.reachable(room.points["arrive"][0], [below_bed], look):
            print(f"hideoutRoom[{look}]: arrive에서 침대 앞에 닿지 못한다")
    print(room.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
