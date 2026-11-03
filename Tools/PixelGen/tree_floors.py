"""거대고목 안 여러 층(사용자 요청 2026-11-03: 「뱀성처럼 여러 층으로. 작은 섬만 한 나무라 뱀성보다 층이 더 많고,
요정들이 사는 다양한 공간이 있어야 한다. 쉼터는 1층이 아니라 여러 방 가운데 하나로」).

고목은 아래에서부터 이렇게 오른다(5층 가지 회랑과 꼭대기 잎 덮개는 giant_tree.py):
  1층 뿌리 홀(treeHall)      페어리가든 문으로 들어오는 큰 홀. 뿌리가 꼬여 오른 기둥, 접수대, 게시판, 벤치. 서쪽 바닥의 뿌리 계단은
                              아래 뿌리 동굴로 내려간다(저주의 보랏빛이 샌다).
  2층 요정 장터(treeMarket)  노점(꿀·꽃·버섯·물약)이 둥글게 늘어서고 가운데 샘. 통과 상자.
  3층 요정 마을(treeRooms)    벽을 따라 요정 집 문이 늘어선 객실 층. 가운데 문이 고목의 쉼터(treeInn)다. 가운데 양탄자와 안락의자 쉼터.
  4층 서고와 공방(treeLibrary) 벽을 두른 책장, 읽는 탁자, 실을 잣는 공방 탁자, 씨앗 진열장.
층마다 둥근 나무 방(TreeRoom): 고목 나이테를 그대로 깎은 바닥, 살아 있는 나무결 벽면(이끼·선반 버섯·둥근 창·요정 등불),
위층 계단은 벽면 아치(up), 아래층 계단은 바닥 계단 구멍(down). 계단 자리는 층마다 달라 오르는 길이 나무를 감아 돈다.

사용: python Tools/PixelGen/tree_floors.py <프로젝트 루트>
"""
import sys
from contextlib import contextmanager
from pathlib import Path

import numpy as np

import castle
import fairy_props as fp
import guild_furniture as gf
import home_furniture as hf
import interior
import objects
import pixelgen as pg
import west_sands
from houses import BRASS, Canvas, WARM, outline
from interior import RoundRoom
from layout import LOGICAL, MapLayout

T = pg.TILE
SIZE = 26
CENTER = (13, 14)
RADIUS = 10.5
WOOD8 = pg.hexes("#1e140e", "#2e2016", "#433022", "#5a422e", "#71553c", "#8a6a4c", "#a6845e", "#c0a074")
RINGS = pg.hexes("#3e2818", "#523622", "#64442c", "#765236", "#886040", "#9c704c")
LEAF_RUNNER = pg.hexes("#16301c", "#22462a", "#2e5e38", "#3e7646", "#d6b45e")
SKY = pg.hexes("#7aaedc", "#b4d8f0", "#e8f4fb")
CURSE = pg.hexes("#120a1e", "#2a1240", "#5a2a8a")
GLOW_LIGHT = "#7ff0dc"
WARM_LIGHT = "#ffc070"


@contextmanager
def wood_palette():
    """둥근 방 그리기가 돌로 그리는 것(계단 아치, 계단 구멍, 출입문, 융단)을 나무 빛깔로 그리게 잠시 바꿔 둔다."""
    saved = castle.STONE, interior.CARPET
    castle.STONE, interior.CARPET = WOOD8, LEAF_RUNNER
    try:
        yield
    finally:
        castle.STONE, interior.CARPET = saved


class TreeRoom(RoundRoom):
    """고목 속 둥근 방 한 층: 나이테 바닥과 살아 있는 나무결 벽면."""

    def paint_floor(self):
        c = self.c
        wobble = pg.noise(c.size[1], c.size[0], 22, self.rng) * 7
        d = self.distance + wobble
        band = np.floor(d / 8)
        level = 2 + ((band * 7) % 3).astype(int) - (np.mod(d, 8) < 1)
        c.paint(self.floor, RINGS[np.clip(level, 0, 5)])
        crack = self.floor & (np.abs(np.mod(self.angle * 6 / np.pi + 0.2, 1) - 0.5) < 0.01) & (self.distance > 60)
        c.paint(crack, RINGS[0])
        near = self.floor & (self.distance > self.r - 22)
        c.image[near, :3] = (c.image[near, :3] * 0.82).astype(np.uint8)

    def paint_walls(self):
        c = self.c
        band = (self.distance > self.r) & (self.distance <= self.r + 18)
        c.paint(band, WOOD8[3])
        c.paint(band & ((np.degrees(self.angle) * 3).astype(int) % 7 == 0), WOOD8[1])
        c.paint(band & (self.distance <= self.r + 3), WOOD8[6])
        face = np.zeros(self.distance.shape, dtype=bool)
        for x in range(int(self.cx - self.r), int(self.cx + self.r) + 1):
            top = self.floor_top(x + 0.5)
            if top > self.cy:
                continue
            face[max(0, int(top - self.face)):max(0, int(top)), x] = True
        fp.bark_texture(c, face, self.rng, self.cx - self.r, self.cx + self.r)
        fp.moss_patches(c, face, self.rng, amount=0.68)
        for x in range(int(self.cx - self.r), int(self.cx + self.r) + 1):
            top = self.floor_top(x + 0.5)
            if top > self.cy:
                continue
            y0, y1 = int(top - self.face), int(top)
            ys = np.arange(max(0, y0), max(0, y1))
            fade = np.clip((ys - y0) / self.face, 0, 1)
            c.image[ys, x, :3] = (c.image[ys, x, :3] * (0.6 + 0.4 * fade[:, None])).astype(np.uint8)
            c.image[max(0, y1 - 3):max(0, y1), x, :3] = WOOD8[1]
        under = self.floor & (c.yy < self.cy) & (self.distance > self.r - 14)
        c.image[under, :3] = (c.image[under, :3] * 0.75).astype(np.uint8)

    def round_window(self, x):
        """벽면의 둥근 창: 나무 테 안으로 하늘이 보인다."""
        c = self.c
        cx, cy = x * T + 16, int(self.face_y(x) - self.face * 0.55)
        c.paint(c.ellipse(cx, cy, 13, 13), WOOD8[2])
        c.paint(c.ellipse(cx, cy, 11, 11), WOOD8[5])
        glass = c.ellipse(cx, cy, 9, 9)
        c.paint(glass, SKY[0])
        c.paint(glass & (c.yy < cy - 2), SKY[1])
        c.paint(glass & (c.xx < cx - 3) & (c.yy < cy - 4), SKY[2])
        c.paint(glass & ((np.abs(c.xx - cx) < 1) | (np.abs(c.yy - cy) < 1)), WOOD8[3])

    def fungus(self, x, height):
        c = self.c
        fp.shelf_fungus(c, x * T + 16, int(self.face_y(x) - self.face * height), 16)

    def fairy_door(self, x, sign):
        """
        벽면의 요정 집 둥근 문: 나무 테 아치, 칠한 문짝, 놋쇠 손잡이, 문 위 간판(sign: inn 달·별, leaf 잎, flower 꽃, cup 찻잔).
        돌려주는 값: 문 바로 아래 바닥 칸(문 앞에 서면 들어간다).
        """
        c = self.c
        cx = x * T + 16
        bottom = int(self.face_y(x)) + 1
        top = bottom - 56
        c.paint(c.arch(cx - 15, top - 3, cx + 15, bottom), WOOD8[6])
        door = c.arch(cx - 12, top, cx + 12, bottom)
        paint = {"inn": fp.DOOR, "leaf": fp.CAPS["blue"], "flower": fp.CAPS["red"], "cup": fp.CAPS["violet"]}[sign]
        c.paint(door, paint[2])
        c.paint(door & (c.xx < cx - 4), paint[3])
        c.paint(door & (np.mod(c.xx - cx + 12, 6) == 0), paint[0])
        c.paint(c.ellipse(cx + 6, bottom - 22, 1.8, 1.8), BRASS[4])
        board = c.rect(cx - 13, top - 18, cx + 13, top - 5)
        c.paint(board, WOOD8[5])
        c.paint(board & ~c.rect(cx - 12, top - 17, cx + 12, top - 6), WOOD8[2])
        sx, sy = cx, top - 11
        if sign == "inn":
            c.paint(c.ellipse(sx - 4, sy, 4, 4) & ~c.ellipse(sx - 2, sy - 1, 3.5, 3.5), BRASS[4])
            c.paint(c.rect(sx + 3, sy - 2, sx + 5, sy), BRASS[4])
            c.paint(c.rect(sx + 6, sy + 1, sx + 7, sy + 2), BRASS[4])
        elif sign == "leaf":
            c.paint(c.ellipse(sx, sy, 6, 3), fp.MOSS[3])
        elif sign == "flower":
            for dx, dy in ((-3, 0), (3, 0), (0, -2), (0, 2)):
                c.paint(c.ellipse(sx + dx, sy + dy, 2.2, 2.2), pg.FLOWERS[3])
            c.paint(c.ellipse(sx, sy, 1.4, 1.4), pg.FLOWER_CORE)
        else:
            c.paint(c.rect(sx - 4, sy - 2, sx + 3, sy + 3), pg.hexes("#efe6d2")[0])
            c.paint(c.rect(sx + 3, sy - 1, sx + 5, sy + 2), pg.hexes("#efe6d2")[0])
        if sign == "inn":
            c.paint(c.rect(cx + 15, top + 4, cx + 17, top + 20), WOOD8[2])
            c.paint(c.ellipse(cx + 16, top + 24, 3, 4), WARM[3])
        row = int(bottom // T)
        return [(x, row)]

    def curse_glow(self, x, y):
        """아래 뿌리 동굴에서 저주의 보랏빛이 새어 오른다(계단 구멍 둘레 바닥이 보랏빛을 띤다)."""
        c = self.c
        d = np.hypot(c.xx + 0.5 - (x + 1) * T, (c.yy + 0.5 - (y + 1) * T) * 1.3) / (2.6 * T)
        lit = (d < 1) & self.floor
        mix = np.clip(1 - d, 0, 1)[lit][:, None] * 0.45
        c.image[lit, :3] = (c.image[lit, :3] * (1 - mix) + np.array(CURSE[2]) * mix).astype(np.uint8)


def new_floor(map_id, name, seed, music="field"):
    m = MapLayout(map_id, name, SIZE, SIZE, theme="castle", music=music, ambient={"color": "#1a1208", "alpha": 0.22}, seed=seed)
    room = TreeRoom(SIZE, SIZE, CENTER, RADIUS, seed=seed)
    room.paint_floor()
    room.paint_walls()
    return m, room


def against_wall(room, x):
    """북쪽 벽면 바로 앞(벽에 기대 세우는 줄)."""
    return int(np.ceil(room.face_y(x - 0.5) / T)) + 1


def finish(m, room, rng):
    """둘레를 막고 벽 둘레에 요정 등불과 빛나는 버섯을 둔다."""
    m.paint_ground = room.ground
    m.block_where(room.blocked_cells())
    for i, a in enumerate((200, 250, 290, 340)):
        r = np.radians(a)
        x, y = CENTER[0] + 9.2 * np.cos(r), CENTER[1] + 9.2 * np.sin(r)
        if y < CENTER[1]:
            continue
        m.stand(f"lantern_{i}", fp.fairy_lantern(np.random.default_rng(int(rng.integers(1 << 30)))), x, y + 1)
        m.light((x + 0.3) * LOGICAL, (y + 1) * LOGICAL - 26, 34, GLOW_LIGHT)
    for i, (x, y) in enumerate(((4.6, 15.5), (21.4, 15.5), (8.2, 22.8), (18.0, 22.8))):
        m.stand(f"wall_shrooms_{i}", fp.glow_shrooms(np.random.default_rng(int(rng.integers(1 << 30)))), x, y)
        m.light(x * LOGICAL, y * LOGICAL - 4, 16, GLOW_LIGHT)
    m.light(CENTER[0] * LOGICAL, CENTER[1] * LOGICAL, 120, "#ffe8b0")


def stairs(m, room, up_x, down_cell, below, above, up_target="down", down_target="up"):
    """위층 아치 계단과 아래층 계단 구멍, 그 포탈. below·above가 None이면 그쪽 계단을 만들지 않는다."""
    if above is not None:
        with wood_palette():
            up = room.doorway(up_x)
        m.point("up", *up)
        m.warp("up", above, up_target, "up")
    if below is not None:
        x, y = down_cell
        with wood_palette():
            room.stairwell(x, y)
        m.block(x, y, x + 2, y + 2)
        # 계단 구멍 옆 칸 가운데 방 가운데에 가까운 쪽에 내려선다(둥근 벽에 막히지 않게).
        side = min(((x + 2, y + 1), (x - 1, y + 1), (x, y - 1)), key=lambda cell: np.hypot(cell[0] + 0.5 - CENTER[0], cell[1] + 0.5 - CENTER[1]))
        m.point("down", side)
        m.warp("down", below, down_target, "down")


def residents(m, *cells):
    """이 층에 사는 요정들이 서는 자리(이야기가 요정을 세운다)."""
    m.point("fairy", *cells)


def hall():
    m, room = new_floor("treeHall", "거대고목 1층 · 뿌리 홀", 501)
    rng = np.random.default_rng(501)
    for x in (6, 20):
        room.round_window(x)
    room.fungus(9.5, 0.75)
    room.fungus(16.5, 0.35)
    with wood_palette():
        room.carpet(12, 14, 14, 25)
        entrance = room.entrance(13)
    room.curse_glow(4, 18)
    stairs(m, room, 13, (4, 18), None, "treeMarket")
    with wood_palette():
        room.stairwell(4, 18)
    m.block(4, 18, 6, 20)
    finish(m, room, rng)
    # 뿌리가 꼬여 오른 기둥 여섯.
    pillar = fp.root_pillar(rng)
    for angle in (30, 90, 150, 210, 270, 330):
        a = np.radians(angle)
        x, y = int(CENTER[0] + 6.5 * np.cos(a)), int(CENTER[1] + 6.5 * np.sin(a))
        if (x, y) in ((13, 20), (13, 7)):
            continue
        m.stand("root_pillar", pillar, x + 0.5, y + 1, block=(1, 1))
    m.stand("reception", interior.counting_desk(rng), 19.5, 18, block=(2.4, 0.7))
    m.stand("notice_board", interior.notice_board(rng), 9.5, against_wall(room, 9.5), block=(1.5, 0.6))
    for x, y in ((17.5, 22), (8.5, 22)):
        m.stand("bench", gf.armchair(rng), x, y, block=(1.2, 0.5))
    for x, y in ((7, 10), (19, 10)):
        m.stand("planter", interior.planter(rng), x + 0.5, y + 1, block=(1, 0.5))
    m.light(5 * LOGICAL, 19 * LOGICAL, 40, "#8a4ad0")
    # 문 포탈 바로 윗칸에 내려서면 몸 충돌이 문 칸에 걸쳐, 동료가 밀기만 해도 밖으로 튕겨 나간다. 한 칸 띄운다.
    m.point("enter", (13, 22))
    m.point("door", *entrance)
    m.warp("door", "fairyGarden", "treeArrive", "down")
    m.point("roots", (6, 19))
    m.point("rootsArrive", (7, 19))
    m.warp("roots", "treeRoots", "enter", "down")
    m.point("receptionist", (19, 16))
    residents(m, (10, 16), (16, 12), (8, 13))
    m.point("slime", (17, 15), (9, 19), (15, 21))
    return m


def market():
    m, room = new_floor("treeMarket", "거대고목 2층 · 요정 장터", 502)
    rng = np.random.default_rng(502)
    for x in (5, 21):
        room.round_window(x)
    room.fungus(13, 0.7)
    stairs(m, room, 8, (18, 20), "treeHall", "treeRooms")
    finish(m, room, rng)
    size = 3
    m.stand("sap_fountain", objects.fountain(size, rng), 13.5, 15.6, block=(2.6, 1.6))
    stall = west_sands.market_stall
    for i, (x, ground) in enumerate(((7.5, 12), (19.5, 12), (6.5, 17), (20.5, 17), (10, 21.5))):
        m.stand(f"stall_{i}", fp.fairy_stall(np.random.default_rng(510 + i), i) if i % 2 else stall(np.random.default_rng(510 + i)),
                x, ground, block=(2.2, 0.6))
    for kind, x, y in (("barrel", 4, 13), ("crate", 22, 13), ("crate", 21, 19), ("barrel", 5, 20)):
        m.thing(kind, x, y)
    residents(m, (9, 14), (17, 14), (11, 18), (16, 19))
    m.point("merchant", (7, 12), (19, 12), (6, 17), (20, 17))
    return m


def rooms():
    m, room = new_floor("treeRooms", "거대고목 3층 · 요정 마을", 503)
    rng = np.random.default_rng(503)
    doors = {}
    for x, sign in ((6, "leaf"), (9, "flower"), (13, "inn"), (17, "cup")):
        doors[sign] = room.fairy_door(x, sign)
    room.round_window(20)
    room.fungus(11, 0.5)
    stairs(m, room, 21, (4, 18), "treeMarket", "treeLibrary")
    finish(m, room, rng)
    m.decal("lounge_rug", gf.rug(5, 3, rng), 10.5 * T, 15 * T)
    for x, y, facing in ((10.2, 16.8, "right"), (16.0, 16.8, "left")):
        m.stand("armchair", gf.armchair(rng), x, y, block=(1, 0.5))
    m.stand("round_table", gf.round_table(rng), 13, 17, block=(1.6, 0.6))
    for x, y in ((8, 21), (18, 21), (21, 13)):
        m.stand("planter", interior.planter(rng), x + 0.5, y + 1, block=(1, 0.5))
    m.light(13 * LOGICAL, 16 * LOGICAL, 40, WARM_LIGHT, flicker=True)
    m.point("inn", *doors["inn"])
    inn_x, inn_y = doors["inn"][0]
    m.point("innArrive", (inn_x, inn_y + 1))
    m.warp("inn", "treeInn", "enter", "up")
    m.point("home", *[(x, y + 1) for x, y in (doors["leaf"][0], doors["flower"][0], doors["cup"][0])])
    residents(m, (8, 12), (18, 12), (11, 20), (16, 21))
    return m


def library():
    m, room = new_floor("treeLibrary", "거대고목 4층 · 서고와 공방", 504)
    rng = np.random.default_rng(504)
    room.round_window(13)
    stairs(m, room, 6, (19, 19), "treeRooms", "giantTree", up_target="down")
    finish(m, room, rng)
    shelf = interior.bookshelf(rng)
    for x in (9, 11, 16, 18, 20.5):
        m.stand("bookshelf", shelf, x, against_wall(room, x), block=(1.6, 0.6))
    for x, y in ((9, 14), (16, 14)):
        m.stand("reading_table", interior.long_table(rng), x + 0.5, y + 1, block=(3, 1))
        for dx in (-1.6, 1.6):
            m.stand("stool", gf.stool(rng), x + 0.5 + dx, y + 1, block=(0.5, 0.3))
        m.light((x + 0.5) * LOGICAL, y * LOGICAL, 30, WARM_LIGHT, flicker=True)
    m.stand("workbench", gf.workbench(rng), 7.5, 20, block=(2.4, 0.6))
    m.stand("seed_case", interior.display_case(rng), 13.5, 21, block=(2, 0.6))
    m.decal("reading_rug", gf.rug(4, 3, rng), 11.5 * T, 17 * T)
    residents(m, (12, 12), (8, 17), (17, 18))
    return m


def main(project):
    for build in (hall, market, rooms, library):
        m = build()
        start = m.points["enter" if "enter" in m.points else "down"][0]
        for name, cells in m.points.items():
            for cell in cells:
                if name in ("home",):
                    continue
                if not m.reachable(start, [cell]):
                    print(f"{m.id}: {name} {cell}에 닿지 못한다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
