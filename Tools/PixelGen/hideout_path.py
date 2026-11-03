"""아지트 가는 숲길(hideoutPath)을 새로 짠다(사용자 결정 2026-11-02: 디엘이 있는 뉴웨이브 길드 집은 숲속 나무에 둘러싸여 있다).

빽빽한 숲 가운데 공터에 뉴웨이브 길드 집이 서 있다. 서쪽 마을에서 오는 흙길이 공터로 굽어 들어오고, 집 앞에서 남쪽
바닷가로 가는 길이 갈라진다. 숲은 지나갈 수 없고 길과 공터만 걷는다.
집과 마당은 모습 두 벌이다(layout.py 모습). 기본은 지금의 버려진 모습(houses.CATALOG의 newwave_guild_old, 흩어진 통·상자)이고,
6장 디엘의 회상 속 전성기의 뉴웨이브 길드는 활기찬 모습(newwave_guild, 꽃 상자·길드 깃발·가로등·의자·장작더미·길드원 모집 게시판)이다.
웹게임도 5장은 낡은 아지트에서 깨어나 그날 뱀열차를 타고, 6장 회상에서 「그땐 텅 비어 있었는데」 하며 붐비는 아지트를 본다
(사용자 확인 2026-11-02).

지점 이름은 웹게임 숲길과 같다(door, doorArrive, west, westArrive, south, southArrive). 표지판 자리는 sign 지점이다.

사용: python Tools/PixelGen/hideout_path.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/hideoutPath/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/hideoutPath_preview.png    미리보기(버려진 모습)
  Docs/ArtUpgrade/PixelGen/hideoutPath_lively_preview.png    미리보기(회상 속 활기찬 모습)
"""
import sys
from pathlib import Path

import numpy as np

import forest as woodland  # 이 파일은 맵 변수 이름이 forest다
import houses
import interior
import yard
from layout import MapLayout

WIDTH, HEIGHT = 34, 30
HOUSE_X, HOUSE_GROUND = 17, 12
WEST_PATH = [(-1, 18), (5, 18), (9, 16.6), (13, 15.2), (17, 14.6)]
SOUTH_PATH = [(17, 14), (17.6, 20), (16.2, 25), (17, 31)]


def lively_yard(forest, guild):
    """
    전성기 뉴웨이브 길드의 마당: 벽 앞 꽃 상자, 집 모퉁이의 길드 깃발, 문 앞 길을 밝히는 가로등 둘,
    왼쪽 마당의 의자, 오른쪽 벽 곁 장작더미와 통, 남쪽 길가의 길드원 모집 게시판. 문 앞(16~17열)과 길은 비운다.
    """
    rng = np.random.default_rng(65)
    forest.house(guild, HOUSE_X, HOUSE_GROUND)
    forest.stand("flower_box_left", yard.flower_box(rng), 13.5, 13, block=(2, 0.6))
    forest.stand("flower_box_right", yard.flower_box(rng), 20.5, 13, block=(2, 0.6))
    forest.stand("guild_flag", yard.guild_flag(rng), 11.5, 13, block=(0.5, 0.4))
    forest.stand("guild_flag", yard.guild_flag(rng), 23.4, 13, block=(0.5, 0.4))
    forest.lamp(15, 13)
    forest.lamp(18, 13)
    forest.stand("bench", yard.bench(rng), 9.5, 14, block=(2, 0.6))
    forest.stand("woodpile", yard.woodpile(rng), 25.6, 13, block=(2, 0.8))
    forest.thing("barrel", 24, 14)
    forest.stand("recruit_board", interior.notice_board(rng), 20, 17, block=(1.5, 0.5))


def build():
    forest = MapLayout("hideoutPath", "아지트 가는 숲길", WIDTH, HEIGHT, music="hope", seed=63)
    designs = {design.name: design for design in houses.CATALOG}

    # ── 바닥: 숲 바닥(덤불), 공터, 길 ──
    clearing = ((forest.cx - HOUSE_X) / 10.5) ** 2 + ((forest.cy - 11.5) / 7.2) ** 2 <= 1
    paths = forest.near_line(WEST_PATH, 1.3) | forest.near_line(SOUTH_PATH, 1.3)
    woods = ~clearing & ~paths
    forest.kinds[woods] = "tallgrass"
    forest.line("path", WEST_PATH, 2.2)
    forest.line("path", SOUTH_PATH, 2.2)
    forest.stamp("tallgrass", (8, 13), (9, 13), (25, 14), (26, 14), (26, 13), (11, 8), (23, 7), (24, 8))
    forest.stamp("flowers", (10, 14), (11, 14), (23, 15), (24, 15), (13, 17), (21, 17), (20, 16), (8, 11))
    forest.stamp("bush", (7, 12), (27, 12), (12, 18), (22, 18))
    forest.stamp("rock", (9, 9), (26, 10), (14, 21))
    forest.stamp("sign", (4, 17))

    # ── 뉴웨이브 길드 집과 마당 물건: 지금은 버려진 모습, 6장 회상 속에서는 활기찬 모습 ──
    with forest.look("abandoned"):
        forest.house(designs["newwave_guild_old"], HOUSE_X, HOUSE_GROUND)
        for kind, x, y in (("barrel", 23, 12), ("crate", 24, 12), ("crate", 24, 13), ("barrel", 10, 12)):
            forest.thing(kind, x, y)
    with forest.look("lively"):
        lively_yard(forest, designs["newwave_guild"])
    forest.thing("well", 26, 15)

    # ── 숲: 여러 종류를 무리 지어 섞어 심는다(woodland.plant). 수관이 길과 공터를 덮을 자리에는 나무 대신 덤불을 둔다 ──
    rng = np.random.default_rng(7)
    shades_open = woodland.canopy_over(~woods)
    woodland.plant(forest, woods & ~shades_open, rng)
    for y, x in zip(*np.nonzero(woods & shades_open)):
        if rng.random() < 0.22:
            forest.stamp("bush", (int(x), int(y)))
    forest.block_where(woods)
    forest.border()

    # ── 이야기 지점과 포탈 ──
    forest.point("door", (16, HOUSE_GROUND - 1), (17, HOUSE_GROUND - 1))
    forest.point("doorArrive", (16, HOUSE_GROUND))
    forest.point("west", (0, 17), (0, 18))
    forest.point("westArrive", (2, 18))
    forest.point("south", (16, HEIGHT - 1), (17, HEIGHT - 1))
    forest.point("southArrive", (17, HEIGHT - 2))
    forest.point("sign", (4, 17), blocked=True)
    forest.warp("door", "hideout", "enter", "up")
    forest.warp("south", "port", "northArrive", "down")
    forest.warp("west", "town", "forestArrive", "down")
    return forest


def main(project):
    print(build().publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
