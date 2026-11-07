"""뱀탑 안 세 층(뱀성은 여러 층이고 층마다 개성 있는 내부 구조).

  1층 비늘 홀(castleHall)    : 남쪽 문으로 들어오면 깃발대와 화로 사이로 붉은 양탄자가 바닥 가운데 돌뱀 모자이크까지
                               이어진다. 모자이크 둘레 촛대, 기둥 여섯, 북쪽 벽 큰 걸개 아래 제단, 벽을 따라 돌 의자와 큰 화분,
                               북동쪽 벽의 아치 계단(위층), 서쪽에 보물 상자.
  2층 병영(castleBarracks)   : 서쪽 벽에 이층 침대와 발치 사물함, 북쪽 벽에 방패·근무표 게시판·갑옷 걸이·무기 걸이,
                               가운데 양탄자 위 긴 식탁, 남쪽에 화로와 훈련장(허수아비, 활쏘기 과녁), 무기 통·나무통·상자.
                               동쪽 벽을 뚫고 들어온 돌뱀 몸통이 방을 가로지른다.
  3층 보물고(castleVault)    : 가운데 빛기둥 아래 빈 보석대(루나가 훔쳐 간 자리)를 벨벳 줄과 촛대가 둘러싼다.
                               벽에 금테 그림과 책장, 양옆에 진열장(금관·금잔)·열린 보물 상자·금괴·금화 더미·항아리,
                               남쪽에 보물 관리인의 책상과 보물 상자. 북쪽 아치 계단은 꼭대기(뱀 머리 위)로 간다.

층마다 아래층에서 올라온 자리(down, 1층은 entrance)와 위층으로 가는 포탈(up)이 있다. 순찰병 자리는 patrol 지점이고,
순찰 길은 이야기 코드(Chapter2)에 있다. 여기 적은 같은 길로 막힌 칸을 지나지 않는지 확인한다.

사용: python Tools/PixelGen/castle_floors.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np

import castle
import interior
import pixelgen as pg
from interior import RoundRoom
from layout import MapLayout, LOGICAL

T = pg.TILE
SIZE = 26
CENTER = (13, 14)
RADIUS = 10.5
# Chapter2.cs의 순찰 길과 같아야 한다(층마다 patrol 지점 순서대로)
ROUTES = {
    "castleHall": ["R10 L10", "L10 R10", "R12 L12"],
    "castleBarracks": ["R6 L6", "U6 D6"],
    "castleVault": ["R5 L5", "L5 R5"],
}


def new_floor(map_id, name, seed):
    m = MapLayout(map_id, name, SIZE, SIZE, theme="castle", music="castle", seed=seed)
    room = RoundRoom(SIZE, SIZE, CENTER, RADIUS, seed=seed)
    room.paint_floor()
    room.paint_walls()
    m.paint_ground = room.ground
    return m, room


def finish(m, room, rng):
    m.block_where(room.blocked_cells())
    for x in (5, 21):
        room.torch(m, x)


def hall():
    m, room = new_floor("castleHall", "뱀성 1층 · 비늘 홀", 81)
    rng = np.random.default_rng(81)
    room.carpet(12, 17, 14, 25)
    room.mosaic(13, 14, 3.2)
    for x in (8, 18):
        room.slit_window(x)
    room.banner(10)
    room.banner(15)
    room.tapestry(13)
    up = room.doorway(20)
    entrance = room.entrance(13)
    finish(m, room, rng)
    piece = interior.pillar(rng)
    for angle in (0, 60, 120, 180, 240, 300):
        a = np.radians(angle)
        x, y = int(CENTER[0] + 7 * np.cos(a)), int(CENTER[1] + 7 * np.sin(a))
        m.stand("pillar", piece, x + 0.5, y + 1, block=(1, 1))
    statue = castle.serpent_statue(rng)
    m.stand("statue", statue, 17.5, 8, block=(1, 0.5))
    m.stand("statue", statue, 21.5, 10, block=(1, 0.5))
    for x, y in ((10, 22), (16, 22)):
        image, pivot = castle.brazier(rng)
        m.upright("brazier", image, x * T + T // 2, (y + 1) * T - 1, pivot, (y + 1) * LOGICAL)
        m.block(x, y, x + 1, y + 1)
        m.fire(x, y - 1)
        m.light(x * 16 + 8, y * 16 - 4, 40, "#ff9a3a", flicker=True)
    # 순찰병이 가로로 다니는 줄(9·14·19)과 입구에서 계단까지의 길은 비워 둔다
    m.stand("altar", interior.altar(rng), 13, 6, block=(2, 0.6))
    bench = interior.stone_bench(rng)
    for x, ground in ((5, 12), (5, 17), (21, 12), (21, 17)):
        m.stand("stone_bench", bench, x, ground, block=(2, 0.5))
    plant = interior.planter(rng)
    for x, y in ((6, 7), (7, 21), (18, 21)):
        m.stand("planter", plant, x + 0.5, y + 1, block=(1, 0.5))
    flag = interior.banner_stand(rng)
    for x in (11, 15):
        m.stand("banner_stand", flag, x + 0.5, 22, block=(0.6, 0.4))
    candles = interior.candelabra(rng)
    for x, y in ((9, 11), (16, 11), (9, 17), (16, 17)):
        m.stand("candelabra", candles, x + 0.5, y + 1, block=(0.6, 0.4))
        m.light(x * 16 + 8, y * 16 - 10, 30, "#ffd27a")
    m.point("entrance", *entrance)
    m.keep_open(12, 24, 14, 25)
    m.point("up", *up)
    m.point("patrol", (7, 9), (18, 14), (7, 19))
    m.point("chest", (4, 14))
    solid = m.collision()
    m.point("howTo", *[(x, y) for x in range(3, 7) for y in range(11, 18) if not solid[y * 4:(y + 1) * 4, x * 4:(x + 1) * 4].any()])
    m.warp("up", "castleBarracks", "down", "up")
    return m


def barracks():
    m, room = new_floor("castleBarracks", "뱀성 2층 · 병영", 82)
    rng = np.random.default_rng(82)
    # 동쪽 벽을 뚫고 들어온 돌뱀 몸통: 북동쪽 벽면에서 나와 동쪽 바닥을 지나 남동쪽 벽으로 들어간다
    path = np.array([(19.6, 5.6), (21.6, 9.0), (22.2, 13.5), (21.4, 18.5), (19.2, 23.2)])
    t = np.linspace(0, 1, 260)
    knots = np.linspace(0, 1, len(path))
    xs, ys = np.interp(t, knots, path[:, 0]), np.interp(t, knots, path[:, 1])
    xs = np.convolve(np.pad(xs, 20, mode="edge"), np.ones(41) / 41, mode="valid")
    ys = np.convolve(np.pad(ys, 20, mode="edge"), np.ones(41) / 41, mode="valid")
    body = [(x * T, y * T, 38.0) for x, y in zip(xs, ys)]
    room.serpent_breach(body, 38)
    room.shield(4)
    room.carpet(9, 13, 15, 17)
    up = room.doorway(11)
    down = room.stairwell(5, 19)
    finish(m, room, rng)
    for x, y in zip(xs[::6], ys[::6]):
        m.block(x - 1.1, y - 1.1, x + 1.1, y + 1.1)
    locker = interior.footlocker(rng)
    for y in (10, 13, 16):
        m.stand("bunk_bed", interior.bunk_bed(rng), 5, y, block=(2, 1))
        m.stand("footlocker", locker, 6.5, y, block=(1, 0.5))

    def against_wall(x):
        """북쪽 벽면 바로 앞(벽에 기대 세우는 줄)."""
        return int(np.ceil(room.face_y(x - 0.5) / T)) + 1

    m.stand("notice_board", interior.notice_board(rng), 6.5, against_wall(6.5), block=(1.5, 0.6))
    armor = interior.armor_stand(rng)
    for x in (8.5, 13):
        m.stand("armor_stand", armor, x, against_wall(x), block=(1, 0.5))
    rack = interior.weapon_rack(rng)
    for x in (15, 18):
        m.stand("weapon_rack", rack, x, against_wall(x), block=(2, 1))
    m.stand("table", interior.long_table(rng), 12, 15, block=(4, 1))
    weapons = interior.weapon_barrel(rng)
    for x, y in ((13, 8), (17, 7)):
        m.stand("weapon_barrel", weapons, x + 0.5, y + 1, block=(1, 0.6))
    dummy = interior.dummy(rng)
    m.stand("dummy", dummy, 15.5, 19, block=(1, 0.5))
    m.stand("dummy", dummy, 17.5, 21, block=(1, 0.5))
    m.stand("archery_target", interior.archery_target(rng), 12.5, 21, block=(1, 0.5))
    image, pivot = castle.brazier(rng)
    m.upright("brazier", image, 10 * T + T // 2, 19 * T - 1, pivot, 19 * LOGICAL)
    m.block(10, 18, 11, 19)
    m.fire(10, 17)
    m.light(10 * 16 + 8, 18 * 16 - 4, 44, "#ff9a3a", flicker=True)
    for kind, x, y in (("barrel", 8, 21), ("crate", 9, 22), ("crate", 4, 18), ("barrel", 11, 22), ("crate", 12, 23)):
        m.thing(kind, x, y)
    m.block(5, 19, 7, 21)
    m.point("down", (8, 19))
    m.point("up", *up)
    m.point("patrol", (8, 11), (16, 16))
    m.warp("up", "castleVault", "down", "up")
    return m


def vault():
    m, room = new_floor("castleVault", "뱀성 3층 · 보물고", 83)
    rng = np.random.default_rng(83)
    room.carpet(10, 12, 16, 18)
    room.glow(13, 14.6, 2.6, (255, 236, 190))
    room.banner(9)
    room.banner(17)
    for x in (4, 22):
        room.painting(x, rng)
    up = room.doorway(13)
    down = room.stairwell(18, 19)
    finish(m, room, rng)

    def against_wall(x):
        return int(np.ceil(room.face_y(x - 0.5) / T)) + 1

    # 가운데: 빛기둥 아래 빈 보석대를 벨벳 줄과 촛대가 둘러싼다
    m.stand("jewel_pedestal", interior.jewel_pedestal(rng), 13, 15.5, block=(1.6, 0.8))
    rope = interior.rope_barrier(rng)
    for ground in (12.5, 18):
        m.stand("rope_barrier", rope, 13, ground, block=(6, 0.4))
    candles = interior.candelabra(rng)
    for x, y in ((9, 13), (16, 13), (9, 17), (16, 17)):
        m.stand("candelabra", candles, x + 0.5, y + 1, block=(0.6, 0.4))
        m.light(x * 16 + 8, y * 16 - 10, 30, "#ffd27a")
    # 북쪽 벽: 책장 둘, 계단 양옆 뱀 석상
    for x in (7, 19):
        m.stand(f"bookshelf_{x}", interior.bookshelf(np.random.default_rng(x)), x, against_wall(x), block=(2, 1))
    statue = castle.serpent_statue(rng)
    m.stand("statue", statue, 10.5, 7, block=(1, 0.5))
    m.stand("statue", statue, 15.5, 7, block=(1, 0.5))
    # 서쪽·동쪽: 진열장, 열린 보물 상자, 금괴, 금화 더미, 항아리
    case = interior.display_case(rng)
    opened = interior.open_chest(rng)
    bars = interior.gold_bars(rng)
    gold = interior.gold_pile(rng)
    jar = interior.urn(rng)
    chest = interior.treasure_chest(rng)
    for name, piece, x, ground, block in (
            ("display_case", case, 5.5, 11, (1.4, 0.6)), ("display_case", case, 21.5, 12, (1.4, 0.6)),
            ("open_chest", opened, 4.5, 14, (1.5, 0.6)), ("open_chest", opened, 21, 18, (1.5, 0.6)),
            ("gold_bars", bars, 7, 16, (1.5, 0.5)), ("gold_bars", bars, 18.5, 15, (1.5, 0.5)),
            ("gold_pile", gold, 4.5, 17, (1.5, 0.6)), ("gold_pile", gold, 22.5, 15, (1.5, 0.6)),
            ("gold_pile", gold, 13.5, 23, (1.5, 0.6)),
            ("urn", jar, 3.5, 13, (0.8, 0.5)), ("urn", jar, 20.5, 21, (0.8, 0.5)),
            ("treasure_chest", chest, 9.5, 22, (1.3, 0.6)), ("treasure_chest", chest, 12, 22, (1.3, 0.6)),
            ("treasure_chest", chest, 15.5, 22, (1.3, 0.6))):
        m.stand(name, piece, x, ground, block=block)
    m.stand("counting_desk", interior.counting_desk(rng), 7, 21, block=(2, 0.6))
    m.block(18, 19, 20, 21)
    m.point("down", (17, 19))
    m.point("up", *up)
    m.point("patrol", (7, 9), (19, 9))
    m.warp("up", "castleRoof", "arrive", "up")
    return m


def main(project):
    for build in (hall, barracks, vault):
        m = build()
        for start, route in zip(m.points["patrol"], ROUTES[m.id]):
            if not m.route_is_clear(start, route):
                print(f"{m.id}: 순찰 길 {route}({start})이 막힌 칸을 지난다")
        start = m.points["entrance" if "entrance" in m.points else "down"][0]
        if not m.reachable(start, m.points["up"]):
            print(f"{m.id}: 올라온 자리에서 위층 계단까지 걸어갈 수 없다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
