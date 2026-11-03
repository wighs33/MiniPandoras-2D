"""뱀성(castle)을 새로 짠다(사용자 결정 2026-11-02: 뱀성은 다리를 건너 산 위에 있다, 성 건물은 꼭대기가 뱀 머리인 탑이다).

56×72칸. 아래(남쪽) 풀밭에서 강 위 나무다리(bridges.py: 판자 바닥, 난간, 등불 기둥)를 건너, 양 끝이 휘어 올라간 절벽 세 단을 돌계단으로 지그재그로 올라
산꼭대기 성문에 닿는다. 성문을 지나면 안뜰(뱀 석상, 화로, 양옆 정원)이고, 맨 위에 크림색 얼룩 뱀이 감아 오른 탑이 있다.
탑이 맵 안에 다 들어가도록 맨 위에 빈 줄을 둔다. 성 건물은 castle.py 소품이다.

지점 이름은 웹게임 뱀성과 같다(arrive, south, gate, keep, guard 둘, npc 셋, luna, side 둘).
루나가 나타나는 줄은 courtyard 지점의 y다(웹게임은 이야기 코드에 y ≤ 16을 적어 두었다).

사용: python Tools/PixelGen/snake_castle.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/castle/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/castle_preview.png    미리보기
"""
import sys
from pathlib import Path

import numpy as np

import bridges
import castle
import forest as woods
import pixelgen as pg
from layout import MapLayout, LOGICAL

T = pg.TILE
WIDTH, HEIGHT = 56, 72
CX = 28                 # 성의 가운데 줄(칸 경계)
KEEP_GROUND = 22        # 뱀 탑 아래
NORTH_WALL = 18         # 북쪽 성벽 아래(안뜰 윗줄)
WALL_GROUND = 38        # 남쪽 성벽 아래
WEST, EAST = CX - 15, CX + 15   # 옆 성벽 칸
BRIDGE_X = (27, 30)     # 강을 건너는 다리가 놓이는 칸(가운데 28.5)


def stand(layout, name, piece, x, ground):
    """소품(그림, 기준점)을 칸 (x, ground)에 세운다. x는 가운데, ground는 발밑이 닿는 칸 경계."""
    image, pivot = piece
    layout.upright(name, image, x * T, ground * T, pivot, ground * LOGICAL)


def brazier(layout, x, y):
    """화로를 칸 (x, y)에 세우고 그릇 위에 불꽃과 흔들리는 빛을 둔다."""
    image, pivot = castle.brazier(np.random.default_rng(x * 7 + y))
    layout.upright("brazier", image, x * T + T // 2, (y + 1) * T - 1, pivot, (y + 1) * LOGICAL)
    layout.block(x, y, x + 1, y + 1)
    layout.fire(x, y - 1)
    layout.light(x * LOGICAL + 8, y * LOGICAL - 4, 40, "#ff9a3a", flicker=True)


def stairs_through(layout, x0, x1, near_y):
    """x0~x1 열에서 near_y 근처 절벽 한 단을 돌계단으로 바꾸고, 위아래 한 칸씩 더 이어 오르내리는 자리를 만든다."""
    rows = np.nonzero((layout.kinds[:, x0:x1] == "cliff").any(axis=1))[0]
    rows = rows[np.abs(rows - near_y) <= 4]
    layout.rect("stairs", x0, rows.min() - 1, x1, rows.max() + 2)


def mountain(m):
    """산과 강: 양 끝이 휘어 올라가는 절벽 세 단, 단마다 돌계단, 그 사이 산길, 강과 나무다리, 남쪽 길."""
    m.line("cliff", [(-1, 37), (5, 40), (11, 43.6), (22, 44.6), (34, 44.8), (45, 43.6), (51, 40), (57, 37)], 3)
    m.line("cliff", [(-1, 46), (6, 50), (14, 53), (28, 53.6), (42, 53), (50, 50), (57, 46)], 3)
    m.line("cliff", [(-1, 55), (8, 58.6), (18, 60.4), (38, 60.8), (48, 58.6), (57, 55)], 3)
    stairs_through(m, 36, 39, 44)
    stairs_through(m, 18, 21, 53)
    stairs_through(m, 27, 30, 61)
    # 강은 맨 아래 절벽 발치까지 차올라 골짜기가 된다
    m.line("water", [(-1, 65), (14, 64.6), (30, 65.4), (57, 64.8)], 5.5)
    m.rect("cobble", CX - 6, WALL_GROUND, CX + 7, 42)
    m.line("path", [(34, 41), (37.5, 42)], 2.4)
    m.line("path", [(37.5, 47), (37, 49), (28, 50.5), (19.5, 50.5)], 2.4)
    m.line("path", [(19.5, 56), (21, 57.5), (28.5, 57.5)], 2.4)
    m.line("path", [(28.5, 67), (28, 69), (28, 73)], 2.4)
    m.stamp("rock", (9, 48), (13, 47), (45, 48), (46, 50), (32, 47), (9, 56), (35, 56), (43, 55), (47, 57),
            (14, 67), (40, 68), (45, 69), (19, 70), (3, 42), (52, 43), (2, 52), (53, 51))
    m.stamp("flowers", (11, 67), (12, 68), (36, 69), (37, 68), (22, 49), (41, 49), (13, 56), (32, 56), (45, 40), (9, 40))
    m.stamp("tallgrass", (7, 47), (8, 47), (48, 49), (49, 49), (12, 57), (44, 56), (8, 69), (9, 69), (47, 67), (48, 67))
    m.stamp("bush", (16, 47), (42, 47), (11, 55), (41, 56), (23, 68), (34, 68))


def bridge(m, rng):
    """
    강을 건너는 나무다리: 판자 바닥(바닥 그림, 양쪽 물가에 반 칸씩 얹힌다), 양옆 난간(기둥마다 한 칸씩 세워 인물 앞뒤가 맞다),
    네 귀퉁이 등불 기둥. 물 위는 난간 안쪽만 걸어서 지나간다.
    """
    x0, x1 = BRIDGE_X
    # 강 폭은 다리 바로 옆 열에서 잰다(다리 칸은 남쪽 길이 덮어 칠했을 수 있다). 다리 밑은 물로 되돌린다.
    rows = np.nonzero((m.kinds[:, [x0 - 1, x1]] == "water").any(axis=1))[0]
    m.kinds[rows.min():rows.max() + 1, x0:x1] = "water"
    top, bottom = rows.min() - 0.5, rows.max() + 1.5
    m.decal("bridge_deck", bridges.deck(x1 - x0, bottom - top, rng), x0 * T - bridges.MARGIN, top * T)
    m.keep_open(x0 + 0.25, rows.min(), x1 - 0.25, rows.max() + 1)
    posts = np.linspace(top + 0.35, bottom - 0.1, int(round((bottom - top) / 1.25)) + 1)
    for side, x in ((-1, x0 + 0.12), (1, x1 - 0.12)):
        corner = bridges.lantern_post(rng, side)
        stand(m, "bridge_lantern_post", corner, x, posts[0])
        for k in range(1, len(posts)):
            length = int(round((posts[k] - posts[k - 1]) * T))
            stand(m, f"bridge_rail_{length}" + ("" if k < len(posts) - 1 else "_end"),
                  bridges.rail(length, rng, post=k < len(posts) - 1), x, posts[k])
        stand(m, "bridge_lantern_post", corner, x, posts[-1])
        m.block(x - 0.12, top, x + 0.12, bottom)
        for y in (posts[0], posts[-1]):
            m.light((x + side * 0.4) * LOGICAL, y * LOGICAL - 18, 24)


def fortress(m, rng):
    """산꼭대기 성: 안뜰, 뱀 탑, 북쪽·옆 성벽, 망루 넷, 남쪽 성벽과 성문, 뱀 석상, 화로."""
    m.rect("cobble", WEST + 1, NORTH_WALL, EAST, WALL_GROUND)
    m.rect("grass", WEST + 2, 24, WEST + 9, 31)
    m.rect("grass", EAST - 7, 24, EAST, 31)
    m.stamp("flowers", (15, 25), (16, 25), (19, 29), (20, 29), (37, 29), (38, 29), (41, 25), (42, 25))
    m.tree(WEST + 4, 27)
    m.tree(EAST - 4, 27)

    # 탑 뒤 북쪽 성벽이 두 망루 사이를 막는다(탑보다 위 줄이라 탑 뒤에 그려진다)
    stand(m, "wall_north", castle.curtain_wall(EAST - WEST - 1, rng), (WEST + EAST + 1) / 2, NORTH_WALL)
    m.block(WEST + 1, NORTH_WALL - 4, EAST, NORTH_WALL)
    stand(m, "serpent_tower", castle.serpent_tower(rng), CX, KEEP_GROUND)
    m.block(CX - 4, KEEP_GROUND - 6, CX + 4, KEEP_GROUND)
    for kind, x, y in (("crate", 15, 19), ("barrel", 16, 19), ("crate", 15, 20), ("barrel", 40, 19), ("crate", 41, 19),
                       ("barrel", 41, 20)):
        m.thing(kind, x, y)
    side = castle.side_wall(rng)
    for y in range(NORTH_WALL, WALL_GROUND - 1):
        stand(m, "side_wall", side, WEST + 0.5, y + 1)
        stand(m, "side_wall", side, EAST + 0.5, y + 1)
    m.block(WEST, NORTH_WALL - 1, WEST + 1.5, WALL_GROUND)
    m.block(EAST - 0.5, NORTH_WALL - 1, EAST + 1, WALL_GROUND)
    crown = castle.tower(rng, width=64, body_h=120, roof="crown")
    stand(m, "tower_crown", crown, WEST + 0.5, NORTH_WALL + 1)
    stand(m, "tower_crown", crown, EAST + 0.5, NORTH_WALL + 1)
    m.block(WEST - 1, NORTH_WALL - 2, WEST + 2.5, NORTH_WALL + 1)
    m.block(EAST - 1.5, NORTH_WALL - 2, EAST + 2, NORTH_WALL + 1)

    stand(m, "wall_west", castle.curtain_wall(13, rng), CX - 9.5, WALL_GROUND)
    stand(m, "wall_east", castle.curtain_wall(13, rng), CX + 9.5, WALL_GROUND)
    m.block(WEST, WALL_GROUND - 4, CX - 3, WALL_GROUND)
    m.block(CX + 3, WALL_GROUND - 4, EAST + 1, WALL_GROUND)
    opened, pivot = castle.gatehouse(np.random.default_rng(2), closed=False)
    closed, _ = castle.gatehouse(np.random.default_rng(2), closed=True)
    m.gate("gate", opened, closed, CX * T, WALL_GROUND * T, pivot, WALL_GROUND * LOGICAL)
    m.block(CX - 3, WALL_GROUND - 5, CX - 1, WALL_GROUND)
    m.block(CX + 1, WALL_GROUND - 5, CX + 3, WALL_GROUND)
    cone = castle.tower(rng, width=76, body_h=150, roof="cone")
    stand(m, "tower_cone", cone, WEST - 1.5, WALL_GROUND + 1)
    stand(m, "tower_cone", cone, EAST + 1.5, WALL_GROUND + 1)
    m.block(WEST - 3, WALL_GROUND - 3, WEST + 0.5, WALL_GROUND + 1)
    m.block(EAST + 0.5, WALL_GROUND - 3, EAST + 4, WALL_GROUND + 1)

    statue = castle.serpent_statue(rng)
    for x, y in ((CX - 4, 26), (CX + 3, 26), (CX - 4, 31), (CX + 3, 31)):
        stand(m, "statue", statue, x + 0.5, y + 1)
        m.block(x, y + 0.5, x + 1, y + 1)
    for x, y in ((CX - 3, 23), (CX + 2, 23), (CX - 3, 33), (CX + 2, 33), (CX - 4, 40), (CX + 3, 40)):
        brazier(m, x, y)


def forest(m):
    """
    나무: 산꼭대기 뒤는 침엽수가 주인 숲 구역을 심고(탑 꼭대기와 성벽 뒤는 비운다), 산비탈과 강 남쪽은 자리는 그대로 두고
    종류만 섞는다(산은 침엽수가 주이고 군데군데 활엽수·자작나무 무리, 강 남쪽은 활엽수·자작나무, forest.py의 무리 비율).
    """
    rng = np.random.default_rng(78)
    # 산의 무리 비율: 잡음 덩어리 대부분(셋째 무리)이 침엽수, 군데군데 활엽수·자작나무 무리
    hillside = (woods.BROADLEAF, woods.BIRCHES, woods.CONIFERS)
    behind = np.zeros((HEIGHT, WIDTH), dtype=bool)
    behind[:NORTH_WALL - 2] = True
    behind[:9, 17:40] = False           # 탑 꼭대기 돌뱀 머리 둘레는 비운다
    behind[9:, 11:46] = False           # 북쪽 성벽과 망루 뒤
    woods.plant(m, behind, rng, mixes=hillside)
    slopes = [(2, 16), (6, 14), (50, 14), (53, 17), (3, 23), (7, 28), (49, 22), (52, 29), (4, 33), (51, 35),
              (16, 40), (40, 41), (4, 44), (52, 45), (7, 51), (13, 51), (25, 48), (43, 51), (49, 52),
              (5, 54), (15, 58), (37, 57), (46, 58), (23, 55), (2, 60), (53, 59)]
    woods.mixed(m, slopes, rng, mixes=hillside)
    south = [(2, 68), (7, 71), (14, 69), (20, 71), (35, 71), (40, 70), (45, 68), (50, 70), (53, 66), (4, 66)]
    woods.mixed(m, south, rng, mixes=(woods.BIRCHES, woods.BROADLEAF, woods.BROADLEAF))


def build():
    m = MapLayout("castle", "뱀성", WIDTH, HEIGHT, theme="castle", music="town", seed=77)
    mountain(m)
    bridge(m, np.random.default_rng(6))
    fortress(m, np.random.default_rng(5))
    forest(m)
    m.block(0, 0, WIDTH, NORTH_WALL - 2)
    m.border()

    m.point("arrive", (CX, 68))
    m.point("south", (CX - 1, HEIGHT - 1), (CX, HEIGHT - 1))
    m.point("gate", (CX - 1, WALL_GROUND - 1), (CX, WALL_GROUND - 1))
    m.keep_open(CX - 1, WALL_GROUND - 5, CX + 1, WALL_GROUND)
    m.point("keep", (CX - 1, KEEP_GROUND - 1), (CX, KEEP_GROUND - 1))
    m.point("guard", (CX - 2, KEEP_GROUND), (CX + 1, KEEP_GROUND))
    m.point("npc", (WEST + 4, 32), (EAST - 5, 23), (EAST - 7, 33))
    m.point("luna", (EAST - 3, 32))
    m.point("side", (WEST + 2, 31), (EAST - 2, 31))
    m.point("courtyard", (CX, 32))
    # 안뜰에 처음 들어설 때 카메라가 올라가 보여 주는 탑 꼭대기(카메라는 맵 안에서만 움직여 이 줄이면 머리까지 다 보인다)
    m.point("towerTop", (CX, 7), blocked=True)
    m.warp("keep", "castleHall", "entrance", "up")
    m.warp("south", "hillia", "north", "up")
    return m


def main(project):
    print(build().publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
