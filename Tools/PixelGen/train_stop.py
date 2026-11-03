"""뱀열차 정류장(trainStop, 5장)을 새로 짠다(사용자 요청 2026-11-02: 승강장이 승강장 같지 않다).

북쪽 숲과 남쪽 들 사이를 뱀길(모래 길)이 동서로 지나고, 그 남쪽에 돌로 쌓아 올린 승강장이 길게 놓인다.
뱀길에는 뱀열차가 늘 같은 물결로 기어 다녀 다져진 S자 홈이 나 있다(SnakeTrain이 기는 물결과 같은 자리).
승강장은 선로 쪽 가장자리에 밝은 갓돌과 놋쇠 띠, 판석 윗면, 앞 옹벽과 돌계단 둘. 동쪽 절반에 가로등·의자·역명판·여행 가방,
서쪽 절반(5장 주인공이 열차를 기다리는 자리)은 비워 둔다. 남쪽 들에는 서쪽 출구에서 이어지는 흙길과 매표소.
뱀길에는 내려갈 수 없다.

5장이 쓰는 자리는 웹게임과 같다: 서쪽 출구(west·enter), 뱀열차가 땅에 닿는 줄 track(칸 y 7), 승강장 줄(칸 y 8~10).
역명판은 지점 sign이다(웹게임 팻말 자리 대신).

사용: python Tools/PixelGen/train_stop.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/trainStop/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/trainStop_preview.png    미리보기
"""
import sys
from pathlib import Path

import numpy as np

import forest
import pixelgen as pg
import station
import yard
from layout import LOGICAL, MapLayout
from snake_train import GROOVE_DEPTH, GROOVE_LENGTH

WIDTH, HEIGHT = 40, 17
TRACK_Y = 7                     # 뱀열차가 땅에 닿는 줄은 칸 7.5(5장 SpawnSnakeTrain의 trackY × 16 + 8)
ROAD = (3, 8)                   # 뱀길(칸 y)
PLATFORM = (3, 37)              # 승강장(칸 x)
PLATFORM_TOP = 8                # 승강장 윗면은 칸 y 8~10, 그 아래 옹벽
STAIRS = [(4, 7), (15, 18)]     # 옹벽을 끊고 내려가는 돌계단(칸 x)
PATH_Y = 12


def build():
    m = MapLayout("trainStop", "뱀열차 정류장", WIDTH, HEIGHT, music="hope", ambient={"color": "#0e1430", "alpha": 0.5}, seed=97)
    rng = np.random.default_rng(98)

    # ── 바닥: 북쪽 숲, 뱀길, 승강장 밑, 남쪽 들과 흙길 ──
    m.rect("tallgrass", 0, 0, WIDTH, ROAD[0])
    m.rect("sand", 0, ROAD[0], WIDTH, ROAD[1])
    m.rect("cobble", PLATFORM[0], PLATFORM_TOP, PLATFORM[1], PLATFORM_TOP + 3)
    # 서쪽 출구에서 두 계단과 매표소 앞까지 돌을 깐 앞마당, 그 동쪽은 흙길
    m.line("path", [(18, PATH_Y + 0.5), (32, PATH_Y + 0.5)], 1.6)
    m.line("cobble", [(-1, PATH_Y + 0.5), (19, PATH_Y + 0.5)], 1.8)
    m.rect("cobble", 7, PATH_Y + 1, 15, PATH_Y + 3)
    for left, right in STAIRS:
        m.rect("cobble", left, PLATFORM_TOP + 3, right, PATH_Y)
    m.stamp("flowers", (2, 14), (6, 15), (13, 14), (19, 14), (24, 15), (28, 13), (33, 14), (37, 13))
    m.stamp("tallgrass", (0, 9), (1, 10), (38, 9), (39, 10), (16, 15), (31, 15))
    m.stamp("bush", (1, 8), (38, 11), (21, 13), (35, 15))

    # ── 뱀길의 S자 홈: 맵 x(논리 px)에서 SnakeTrain과 같은 물결 ──
    def groove_center(x_dots):
        x = (x_dots + 0.5) / 2
        return ((TRACK_Y + 0.5) * LOGICAL + np.sin(x * 2 * np.pi / GROOVE_LENGTH) * GROOVE_DEPTH) * 2

    groove, groove_top = station.serpent_groove(WIDTH * pg.TILE, groove_center, rng)
    m.decal("serpent_groove", groove, 0, groove_top)

    # ── 승강장: 판석 윗면, 갓돌과 놋쇠 띠, 앞 옹벽, 돌계단 ──
    stairs = [(left - PLATFORM[0], right - PLATFORM[0]) for left, right in STAIRS]
    m.decal("platform", station.platform(PLATFORM[1] - PLATFORM[0], stairs, rng), PLATFORM[0] * pg.TILE, PLATFORM_TOP * pg.TILE)

    # 승강장 동쪽 절반: 기둥 시계, 의자, 역명판, 가로등, 여행 가방(서쪽 절반은 열차를 기다리는 자리라 비운다)
    front = PLATFORM_TOP + 3 - 0.1
    m.stand("station_clock", station.station_clock(rng), 21.5, front, block=(0.5, 0.3))
    m.lamp(33, PLATFORM_TOP + 2)
    m.stand("bench_west", yard.bench(rng), 24.5, front, block=(1.8, 0.4))
    m.stand("station_board", station.station_board(rng), 27.5, front, block=(2.8, 0.4))
    m.stand("bench_east", yard.bench(rng), 30.5, front, block=(1.8, 0.4))
    m.stand("luggage", station.luggage(rng), 35.5, front, block=(2, 0.6))
    m.thing("barrel", 36, PLATFORM_TOP)
    m.stand("planter", yard.flower_box(rng, 2), 4.2, PLATFORM_TOP + 1, block=(1.8, 0.5))

    # 뱀길 건너 숲가의 가로등(열차가 서면 그 뒤로 가려진다)
    for x in (8, 20, 32):
        m.lamp(x, ROAD[0] - 1)

    # ── 남쪽 들: 매표소와 시간표, 숲 ──
    m.stand("ticket_booth", station.ticket_booth(rng), 10.5, HEIGHT - 0.6, block=(2.0, 0.9))
    m.stand("timetable", station.timetable(rng), 13.8, PATH_Y + 3, block=(1.4, 0.3))
    m.light(10.5 * LOGICAL, (HEIGHT - 0.6) * LOGICAL - 26, 26, "#ffb45a")
    north = np.zeros((HEIGHT, WIDTH), dtype=bool)
    north[:ROAD[0]] = True
    for prop in m.props:  # 가로등과 그 양옆은 비워 수관이 가리지 않게
        north[prop["y"], max(0, prop["x"] - 1):prop["x"] + 2] = False
    forest.plant(m, north, np.random.default_rng(11))
    # 남쪽 들은 좁아서 큰 나무가 길과 계단을 가린다. 양 끝 모퉁이에만 나무를 두고 덤불로 가장자리를 두른다.
    for x in (0, 39):
        m.tree(x, HEIGHT - 1, "pine")
    m.stamp("bush", (3, 16), (7, 16), (14, 16), (18, 16), (22, 16), (27, 16), (31, 16), (35, 16))

    # ── 막힌 곳: 숲, 뱀길, 승강장 양 끝 옆, 계단 밖 옹벽 ──
    m.block(0, 0, WIDTH, ROAD[0])
    m.block(0, ROAD[0], WIDTH, ROAD[1])
    m.block(0, PLATFORM_TOP, PLATFORM[0], PLATFORM_TOP + 3.5)
    m.block(PLATFORM[1], PLATFORM_TOP, WIDTH, PLATFORM_TOP + 3.5)
    edges = [PLATFORM[0]] + [x for pair in STAIRS for x in pair] + [PLATFORM[1]]
    for left, right in zip(edges[::2], edges[1::2]):
        m.block(left, PLATFORM_TOP + 3, right, PLATFORM_TOP + 3.5)
    m.border()

    # ── 5장 지점: 서쪽 출구, 뱀열차가 땅에 닿는 줄, 역명판 ──
    m.point("west", (0, PATH_Y))
    m.point("enter", (1, PATH_Y))
    m.point("track", (24, TRACK_Y), blocked=True)
    m.point("sign", (27, PLATFORM_TOP + 2), blocked=True)
    m.warp("west", "outskirts", "eastArrive", "left")
    return m


def main(project):
    m = build()
    for goal in [(x, PLATFORM_TOP + 1) for x in (8, 12, 16, 20)] + [(30, PATH_Y)]:
        if not m.reachable(m.points["enter"][0], [goal]):
            print(f"출구에서 {goal}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
