"""아지트 가는 숲길(hideoutPath)을 새로 짠다(산으로 둘러싸인 가로로 긴 맵).

산으로 둘러싸인 분지 가운데 공터에 뉴웨이브 길드 집이 서 있다. 북쪽과 양옆은 바위 절벽이고, 분지 가장자리는 숲이다.
집 앞에서 길이 두 갈래로 갈라져 남쪽으로 나간다. 왼쪽 갈래는 마을 북쪽 출구로, 오른쪽 갈래는 외곽 들판으로 내려간다.
두 출구 사이가 멀어야 하므로 맵을 가로로 길게 짰다. 산과 숲은 지나갈 수 없고 길과 공터만 걷는다.
집과 마당은 모습 두 벌이다(layout.py 모습). 기본은 지금의 버려진 모습(houses.CATALOG의 newwave_guild_old, 흩어진 통·상자)이고,
6장 디엘의 회상 속 전성기의 뉴웨이브 길드는 활기찬 모습(newwave_guild, 꽃 상자·길드 깃발·가로등·의자·장작더미·길드원 모집 게시판)이다.
웹게임도 5장은 낡은 아지트에서 깨어나 그날 뱀열차를 타고, 6장 회상에서 「그땐 텅 비어 있었는데」 하며 붐비는 아지트를 본다
.

지점 이름은 웹게임 숲길과 같다(door, doorArrive, west, westArrive, south, southArrive). west는 왼쪽 갈래(마을), south는 오른쪽 갈래(외곽)의 출구다. 표지판 자리는 sign 지점이다.

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
import pixelgen as pg
import yard
from layout import MapLayout

WIDTH, HEIGHT = 64, 34
HOUSE_X, HOUSE_GROUND = 32, 16
WEST_EXIT_X, EAST_EXIT_X = 12, 52
WEST_PATH = [(HOUSE_X, 18.6), (26, 20.6), (17, 23.4), (WEST_EXIT_X, 28), (WEST_EXIT_X, HEIGHT + 1)]
SOUTH_PATH = [(HOUSE_X, 18.6), (38, 20.6), (47, 23.4), (EAST_EXIT_X, 28), (EAST_EXIT_X, HEIGHT + 1)]
SIGN = (29, 22)


def lively_yard(forest, guild):
    """
    전성기 뉴웨이브 길드의 마당: 벽 앞 꽃 상자, 집 모퉁이의 길드 깃발, 문 앞 길을 밝히는 가로등 둘,
    왼쪽 마당의 의자, 오른쪽 벽 곁 장작더미와 통, 남쪽 길가의 길드원 모집 게시판. 문 앞(집 가운데 두 열)과 길은 비운다.
    """
    rng = np.random.default_rng(65)
    forest.house(guild, HOUSE_X, HOUSE_GROUND)
    forest.stand("flower_box_left", yard.flower_box(rng), HOUSE_X - 3.5, HOUSE_GROUND + 1, block=(2, 0.6))
    forest.stand("flower_box_right", yard.flower_box(rng), HOUSE_X + 3.5, HOUSE_GROUND + 1, block=(2, 0.6))
    forest.stand("guild_flag", yard.guild_flag(rng), HOUSE_X - 5.5, HOUSE_GROUND + 1, block=(0.5, 0.4))
    forest.stand("guild_flag", yard.guild_flag(rng), HOUSE_X + 6.4, HOUSE_GROUND + 1, block=(0.5, 0.4))
    forest.lamp(HOUSE_X - 2, HOUSE_GROUND + 1)
    forest.lamp(HOUSE_X + 1, HOUSE_GROUND + 1)
    forest.stand("bench", yard.bench(rng), HOUSE_X - 7.5, HOUSE_GROUND + 2, block=(2, 0.6))
    forest.stand("woodpile", yard.woodpile(rng), HOUSE_X + 8.6, HOUSE_GROUND + 1, block=(2, 0.8))
    forest.thing("barrel", HOUSE_X + 7, HOUSE_GROUND + 2)
    forest.stand("recruit_board", interior.notice_board(rng), HOUSE_X + 3, HOUSE_GROUND + 5, block=(1.5, 0.5))


def build():
    forest = MapLayout("hideoutPath", "아지트 가는 숲길", WIDTH, HEIGHT, music="hope", seed=63)
    designs = {design.name: design for design in houses.CATALOG}

    # ── 바닥: 산에 둘러싸인 분지, 가장자리 숲, 가운데 공터, 두 갈래 길 ──
    wobble = 0.08 * np.sin(forest.cx * 0.45) + 0.07 * np.sin(forest.cy * 0.8 + 1)
    basin = ((forest.cx - HOUSE_X) / 29.5) ** 2 + ((forest.cy - 17.5) / 11.5) ** 2 <= 1 + wobble
    clearing = ((forest.cx - HOUSE_X) / 22) ** 2 + ((forest.cy - 16) / 8.8) ** 2 <= 1
    paths = forest.near_line(WEST_PATH, 1.3) | forest.near_line(SOUTH_PATH, 1.3)
    passes = forest.near_line(WEST_PATH, 3) | forest.near_line(SOUTH_PATH, 3)
    mountain = ~(basin | passes)
    groves = pg.fractal_noise(HEIGHT, WIDTH, np.random.default_rng(66)) > 0.6
    woods = ~mountain & ~clearing & ~paths & groves
    forest.kinds[woods] = "tallgrass"
    forest.kinds[mountain] = "cliff"
    forest.line("path", WEST_PATH, 2.2)
    forest.line("path", SOUTH_PATH, 2.2)
    forest.stamp("tallgrass", (23, 17), (24, 17), (40, 18), (41, 18), (41, 17), (26, 12), (38, 11), (39, 12),
                 (15, 15), (16, 15), (48, 14), (49, 14), (17, 20), (46, 20))
    forest.stamp("flowers", (25, 18), (26, 18), (38, 19), (39, 19), (28, 21), (36, 21), (35, 20), (23, 15),
                 (14, 18), (15, 19), (50, 18), (49, 19), (20, 11), (44, 11))
    forest.stamp("bush", (22, 16), (42, 16), (27, 22), (37, 22), (18, 16), (46, 16))
    forest.stamp("rock", (24, 13), (41, 14), (29, 25), (17, 12), (47, 13))
    forest.stamp("sign", SIGN)

    # ── 뉴웨이브 길드 집과 마당 물건: 지금은 버려진 모습, 6장 회상 속에서는 활기찬 모습 ──
    with forest.look("abandoned"):
        forest.house(designs["newwave_guild_old"], HOUSE_X, HOUSE_GROUND)
        for kind, x, y in (("barrel", HOUSE_X + 6, HOUSE_GROUND), ("crate", HOUSE_X + 7, HOUSE_GROUND), ("crate", HOUSE_X + 7, HOUSE_GROUND + 1), ("barrel", HOUSE_X - 7, HOUSE_GROUND)):
            forest.thing(kind, x, y)
    with forest.look("lively"):
        lively_yard(forest, designs["newwave_guild"])
    forest.thing("well", HOUSE_X + 9, HOUSE_GROUND + 3)

    # ── 숲: 여러 종류를 무리 지어 섞어 심는다(woodland.plant). 수관이 길과 공터를 덮을 자리에는 나무 대신 덤불을 둔다 ──
    rng = np.random.default_rng(7)
    shades_open = woodland.canopy_over(forest.kinds == "path")
    woodland.plant(forest, woods & ~shades_open, rng)
    # 산 위의 침엽수: 절벽을 가리지 않는 자리에 무리 지어 선다.
    ridge = mountain & ~woodland.canopy_over(~mountain, side=3, rows=8) & groves
    woodland.plant(forest, ridge, rng, mixes=(woodland.CONIFERS,) * 3, floor=False)
    for y, x in zip(*np.nonzero(woods & shades_open)):
        if rng.random() < 0.22:
            forest.stamp("bush", (int(x), int(y)))
    forest.block_where(woods)
    forest.border()

    # ── 이야기 지점과 포탈 ──
    forest.point("door", (HOUSE_X - 1, HOUSE_GROUND - 1), (HOUSE_X, HOUSE_GROUND - 1))
    forest.point("doorArrive", (HOUSE_X - 1, HOUSE_GROUND))
    forest.point("west", (WEST_EXIT_X - 1, HEIGHT - 1), (WEST_EXIT_X, HEIGHT - 1))
    forest.point("westArrive", (WEST_EXIT_X, HEIGHT - 3))
    forest.point("south", (EAST_EXIT_X - 1, HEIGHT - 1), (EAST_EXIT_X, HEIGHT - 1))
    forest.point("southArrive", (EAST_EXIT_X, HEIGHT - 3))
    forest.point("sign", SIGN, blocked=True)
    forest.warp("door", "hideout", "enter", "up")
    forest.warp("south", "outskirts", "northArrive", "down")
    forest.warp("west", "town", "forestArrive", "down")
    return forest


def main(project):
    print(build().publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
