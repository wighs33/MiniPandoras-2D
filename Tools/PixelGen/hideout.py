"""뉴웨이브 아지트 안(hideout)을 새로 그린다(사용자 요청 2026-11-02: 디엘이 있는 길드 건물 안부터 그래픽 개선).

방 배치는 웹게임 아지트와 같다: 서쪽 큰 강당(남쪽 문, 북쪽 벽난로), 북쪽 가운데 단장실, 북동쪽 뒷문·위층 계단 방,
남동쪽 숙소. 바닥과 벽은 room_interior.RoomInterior가 그린다(판자 바닥, 북쪽 벽 안쪽 면, 방 사이 벽 윗면).
1층이라 2층은 보이지 않는다(계단만 있다). 벽면이 두 칸 높이가 되도록 웹게임보다 한 줄 아래로 내렸다(칸 y + 1).
3장에서 2층 난간에 걸터앉은 디엘은 맵이 아니라 따로 그린 컷인 화면으로 보여 준다(cutin_railing.py, 사용자 요청 2026-11-02).

모습 두 벌(layout.py 모습):
  abandoned  기본. 3·5장의 낡은 아지트: 먼지·거미줄·판자로 막은 창·식은 벽난로, 천을 덮거나 부서진 가구,
             강당 동쪽에 디엘이 지내는 자리(침낭과 바닥 등불).
  lively     6장 디엘의 회상 속 전성기의 뉴웨이브: 불 피운 벽난로, 길드 양탄자와 깃발, 안락의자·둥근 탁자,
             단장 책상과 톱니 작업대, 정돈된 침대.

지점은 웹게임 아지트와 같은 이름이고(칸 y + 1), 3장 연출이 쓰는 자리를 이름 있는 지점으로 더했다.

사용: python Tools/PixelGen/hideout.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/hideout/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/hideout_preview.png, hideout_lively_preview.png   미리보기
"""
import sys
from pathlib import Path

import numpy as np

import guild_furniture as gf
import interior
import pixelgen as pg
import yard
from layout import LOGICAL, MapLayout
from room_interior import FACE, RoomInterior, face_decal, face_top, floor_decal

T = pg.TILE
WIDTH, HEIGHT = 30, 19
ROOMS = {                       # 바닥 칸 (x0, y0, x1, y1), 끝 칸은 들지 않는다
    "hall": (1, 2, 13, 18),
    "office": (14, 2, 21, 7),
    "back": (22, 2, 29, 7),
    "dorm": (14, 9, 29, 17),
}
PASSAGES = [(13, 4, 14, 6), (21, 4, 22, 6), (13, 12, 14, 14)]
ENTRANCE = (6, 18)              # 강당 남쪽 문(entryExit)
BACK_DOOR = 24                  # 뒷문이 난 열(out)
STAIRS = (27, 2)                # 위층 계단(2×2칸)의 왼쪽 위 칸
FIREPLACE = 6.5                 # 강당 벽난로 가운데(칸)
WINDOWS = [(2.5, 2), (10.0, 2), (19.5, 2), (16.0, 9), (20.0, 9), (25.0, 9)]   # 벽면 창 (가운데 칸, 그 방 바닥 첫 줄)
LAMP = "#ffc070"


def paint_rooms():
    room = RoomInterior(WIDTH, HEIGHT, list(ROOMS.values()) + PASSAGES, seed=71)
    room.paint_base()
    for x, row in WINDOWS:
        room.window(x, row)
    room.fireplace(FIREPLACE, ROOMS["hall"][1])
    room.wall_door(BACK_DOOR, ROOMS["back"][1])
    room.stairs(*STAIRS)
    room.entrance(*ENTRANCE)
    return room


def abandoned(m, room, rng):
    """지금의 낡은 아지트(3·5장): 먼지·거미줄·막힌 창·식은 벽난로, 천 덮인 가구와 부서진 가구, 디엘이 지내는 자리."""
    with m.look("abandoned"):
        for i, (x, row) in enumerate(WINDOWS):
            left, top, w, h = room.window_rect(x, row)
            m.decal(f"boards_{i}", gf.boards(w, h, rng), left, top)
        corners = ((1, "left", 2), (13, "right", 2), (14, "left", 2), (21, "right", 2), (22, "left", 2), (29, "right", 2),
                   (14, "left", 9), (29, "right", 9))
        for i, (x, corner, row) in enumerate(corners):
            m.decal(f"cobweb_{i}", gf.cobweb(26, corner), x * T - (26 if corner == "right" else 0), face_top(row))
        face_decal(m, "banner_torn", gf.guild_banner(rng, torn=True), 4.6, 2)
        m.decal("ashes", gf.ashes(), FIREPLACE * T - 13, ROOMS["hall"][1] * T - 12)
        for i, (x, y, w, h) in enumerate(((2, 5, 4, 3), (9, 3, 3, 3), (2, 12, 3, 3), (15, 3, 4, 3), (23, 3, 4, 3),
                                          (15, 12, 5, 3), (22, 10, 4, 4), (25, 14, 3, 2))):
            floor_decal(m, f"dust_{i}", gf.dust(w, h, rng), x, y)
        floor_decal(m, "debris_hall", gf.debris(rng), 1, 15.6)
        floor_decal(m, "debris_back", gf.debris(rng), 25, 4.6)
        floor_decal(m, "papers", gf.papers(rng), 14.3, 5.2)

        # 강당: 천 덮인 안락의자와 책장, 넘어진 탁자와 걸상, 쌓인 상자. 남쪽 가운데는 3장 연출 자리라 비운다.
        m.stand("armchair_sheet", gf.armchair(rng, sheet=True), 3.5, 4, block=(1.2, 0.6))
        m.stand("shelf_sheet", gf.sheet_lump(rng, 62, 92), 11.5, 3, block=(2, 0.6))
        for kind, x, y in (("crate", 12, 14), ("crate", 12, 15), ("barrel", 11, 15)):
            m.thing(kind, x, y)
        face_decal(m, "shelf_empty", gf.shelf_books(rng, empty=True), 15.6, 2)
        m.stand("table_broken", gf.round_table(rng, broken=True), 10.5, 7, block=(1.5, 0.6))
        m.stand("stool_down", gf.stool(rng, toppled=True), 11.8, 8.2, block=(0.6, 0.4))
        for kind, x, y in (("crate", 1, 9), ("crate", 1, 10), ("barrel", 2, 10)):
            m.thing(kind, x, y)
        # 디엘이 지내는 자리: 강당 동쪽 바닥에 편 침낭과 등불(3장 「낡은 등불 하나가 희미하게 흔들린다」).
        floor_decal(m, "bedroll", gf.bedroll(rng), 10, 10.1)
        m.stand("lantern", gf.lantern(rng), 10.5, 12.5, block=(0.4, 0.3))
        m.light(10 * LOGICAL + 8, 12 * LOGICAL + 2, 26, LAMP, flicker=True)

        # 단장실: 천을 덮은 책상, 쓰러진 걸상. 뒷문 방: 부서진 통과 상자.
        m.stand("desk_sheet", gf.guild_desk(rng, sheet=True), 17.5, 4, block=(3, 0.7))
        m.stand("stool_office", gf.stool(rng, toppled=True), 19.8, 6.4, block=(0.6, 0.4))
        for kind, x, y in (("crate", 22, 6), ("barrel", 23, 6), ("crate", 28, 6)):
            m.thing(kind, x, y)

        # 숙소: 맨 매트리스 침대 셋과 주저앉은 침대 하나, 빈 옷장, 부서진 탁자.
        for i, x in enumerate((15, 17, 19, 21)):
            m.stand(f"bed_bare_{i}", gf.bed(rng, made=False, broken=(i == 3)), x + 0.5, 11, block=(0.9, 1.8))
        m.stand("wardrobe_empty", gf.wardrobe(rng, empty=True), 27.5, 11, block=(1.4, 0.7))
        m.stand("table_dorm_broken", gf.round_table(rng, broken=True), 26, 14, block=(1.5, 0.6))
        m.stand("stool_dorm", gf.stool(rng, toppled=True), 24.4, 15, block=(0.6, 0.4))


def lively(m, room, rng):
    """6장 회상 속 전성기의 뉴웨이브: 불 피운 벽난로, 길드 양탄자와 깃발, 손때 묻은 가구."""
    with m.look("lively"):
        m.fire(6, 1, "fire")
        m.light(FIREPLACE * LOGICAL, 1.6 * LOGICAL, 64, "#ff9a40", flicker=True)
        face_decal(m, "banner_left", gf.guild_banner(rng), 4.6, 2)
        face_decal(m, "banner_right", gf.guild_banner(rng), 8.4, 2)
        # 벽난로 선반 위에 올린 톱니 시계
        m.decal("clock", gf.gear_clock(rng), FIREPLACE * T - 15, ROOMS["hall"][1] * T - 58)
        face_decal(m, "wall_map", gf.wall_map(rng), 17.5, 2)
        for i, (x, row) in enumerate(((1.5, 2), (20.6, 2), (26.0, 2), (18.0, 9), (23.0, 9))):
            face_decal(m, f"sconce_{i}", gf.sconce(rng), x, row)
            m.light(x * LOGICAL, (face_top(row) + FACE / 2) / 2, 40, LAMP)
        floor_decal(m, "rug_hall", gf.rug(7, 6, rng), 3, 3.4)
        floor_decal(m, "runner_dorm", gf.runner(8, 1.4, rng), 14.5, 11.6)
        floor_decal(m, "cushion", gf.cushion(rng), 2.4, 2.1)

        # 강당: 벽난로 앞 안락의자 둘, 둥근 탁자와 걸상, 의뢰 게시판, 책장, 화분, 옷걸이.
        m.stand("armchair_left", gf.armchair(rng), 3.5, 4, block=(1.2, 0.6))
        m.stand("armchair_right", gf.armchair(rng), 9.5, 4, block=(1.2, 0.6))
        m.stand("bookshelf_hall", interior.bookshelf(rng), 11.5, 3, block=(2, 0.6))
        m.stand("quest_board", interior.notice_board(rng), 1.6, 4, block=(1.2, 0.5))
        for i, (x, ground) in enumerate(((2.6, 12), (10.6, 12), (10.6, 16))):
            m.stand(f"table_{i}", gf.round_table(rng), x, ground, block=(1.4, 0.6))
            m.stand(f"stool_{i}a", gf.stool(rng), x - 1.1, ground + 0.2, block=(0.5, 0.3))
            m.stand(f"stool_{i}b", gf.stool(rng), x + 1.1, ground + 0.2, block=(0.5, 0.3))
        m.stand("planter_hall", interior.planter(rng), 1.7, 18, block=(1, 0.5))
        m.stand("coat_rack", gf.coat_rack(rng), 8.8, 18, block=(0.5, 0.4))

        # 단장실: 큰 책상, 책장, 톱니 작업대, 화분. 뒷문 방: 옷걸이, 신발 벗는 의자, 통.
        m.stand("desk", gf.guild_desk(rng), 17.5, 4, block=(3, 0.7))
        m.stand("bookshelf_office", interior.bookshelf(rng), 15, 3, block=(2, 0.6))
        m.stand("workbench", gf.workbench(rng), 19.4, 7, block=(2.1, 0.7))
        m.stand("planter_office", interior.planter(rng), 14.6, 7, block=(1, 0.5))
        m.stand("coat_rack_back", gf.coat_rack(rng), 22.6, 3, block=(0.5, 0.4))
        m.stand("bench_back", yard.bench(rng), 25.5, 7, block=(1.8, 0.5))
        for kind, x, y in (("barrel", 28, 6), ("crate", 28, 5)):
            m.thing(kind, x, y)

        # 숙소: 정돈된 침대 넷과 발치 상자, 옷장, 둥근 탁자.
        for i, x in enumerate((15, 17, 19, 21)):
            m.stand(f"bed_{i}", gf.bed(rng, made=True), x + 0.5, 11, block=(0.9, 1.8))
        for i, x in enumerate((15, 19)):
            m.stand(f"footlocker_{i}", interior.footlocker(rng), x + 0.5, 12, block=(0.9, 0.5))
        m.stand("wardrobe", gf.wardrobe(rng), 27.5, 11, block=(1.4, 0.7))
        m.stand("table_dorm", gf.round_table(rng), 26, 14, block=(1.4, 0.6))
        m.stand("stool_dorm_a", gf.stool(rng), 24.9, 14.2, block=(0.5, 0.3))
        m.stand("stool_dorm_b", gf.stool(rng), 27.1, 14.2, block=(0.5, 0.3))
        m.stand("planter_dorm", interior.planter(rng), 23.2, 11, block=(1, 0.5))


def build():
    m = MapLayout("hideout", "뉴웨이브 아지트", WIDTH, HEIGHT, theme="interior", music="hope", seed=71)
    room = paint_rooms()
    m.paint_ground = room.ground
    m.block_where(~room.floor)

    rng = np.random.default_rng(72)
    abandoned(m, room, rng)
    lively(m, room, rng)

    # 웹게임 아지트 지점(칸 y + 1)
    m.point("out", (BACK_DOOR, 1))
    m.point("outArrive", (BACK_DOOR, 2))
    m.point("isabel", (3, 2))
    m.point("erus", (17, 2))
    m.point("report", (17, 4))
    m.point("aurora", (26, 3))
    m.point("stairs", (27, 3))
    m.point("hall", (6, 5), (4, 8), (6, 8), (8, 8))
    m.point("bed", (16, 10))
    m.point("enter", (6, 17))
    m.point("entryExit", ENTRANCE)
    # 3장: 쓰러진 랑, 안쪽으로 들어서면 2층에서 목소리가 들리는 줄, 대련 자리(주인공·디엘),
    #      문을 부수고 들어온 병사들, 병사들 앞을 막아서는 디엘
    m.point("rangLie", (4, 15))
    m.point("strangerCalls", (6, 14))
    m.point("heroHide", (6, 16))
    m.point("spar", (6, 14))
    m.point("dielGuard", (6, 15))
    m.point("raid", (6, 17), (5, 17), (7, 17))
    # 5장: 쓰러졌던 주인공이 깨어나는 자리, 곁을 지킨 랑과 디엘, 디엘이 다가서는 자리
    m.point("wakeHero", (5, 14))
    m.point("wakeRang", (4, 14))
    m.point("wakeDiel", (7, 13))
    m.point("dielCloser", (6, 13))
    m.warp("entryExit", "hideoutPath", "doorArrive", "down")
    m.warp("out", "forest", "start", "up")
    return m


def main(project):
    m = build()
    # 이야기가 걷는 길: 남쪽 문에서 강당·단장실·뒷문·숙소까지 두 모습 모두 막히지 않아야 한다.
    goals = ["hall", "isabel", "erus", "report", "outArrive", "aurora", "bed", "spar", "rangLie"]
    for look in m.look_solid:
        for goal in goals:
            for cell in m.points[goal]:
                if not m.reachable(m.points["enter"][0], [cell], look):
                    print(f"[{look}] 문에서 {goal} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
