"""힐리아 외곽(outskirts)에 남북 길을 낸다(맵의 방향이 맞지 않았다).

웹게임 들판에는 서쪽 마을에서 동쪽 정류장으로 가는 흙길 하나뿐이고, 부두는 아지트 가는 숲길 남쪽에 붙어 있었다.
마을 동쪽이 이 들판이고 숲길은 그 위쪽이므로 숲길 아래로 내려가면 들판이어야 맞다. 그래서 들판을 가운데 두고 잇는다.

        숲길(hideoutPath)
             │
    마을 ─ 외곽 ─ 정류장
             │
      서쪽 부두(port)

들판의 풀·절벽·바위는 웹게임 배치 그대로이고, 숲을 끊는 남북 길과 갈림길 팻말 둘만 더했다.
마을로 돌아가는 서쪽 출구도 이때 생겼다(웹게임에는 없었다). 서쪽 부두는 west_port.py가 짜고, 동쪽 선착장(eastPort)은 이 들판과 이어지지 않는다.

사용: python Tools/PixelGen/outskirts.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/outskirts/…    맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/outskirts_preview.png       미리보기
"""
import sys
from pathlib import Path

import numpy as np

import forest
from layout import SUB, MapLayout
from web_map import read

MAP_ID = "outskirts"
ROAD_Y = 15
NORTH_ROAD = [(17, -1), (17, ROAD_Y + 0.5)]
SOUTH_ROAD = [(33, ROAD_Y + 0.5), (33, 21)]
ROAD_WIDTH = 2.2
NORTH_SIGN = (19, 14)
SOUTH_SIGN = (31, 16)
GROVE_MIXES = (forest.CONIFERS, forest.BIRCHES, forest.BROADLEAF)
LOW_TREE_BUSH_SHARE = 0.6


def web_map(project, map_id):
    return project / f"Assets/MiniPandoras/Art/WebGame/Maps/{map_id}"


def plant_woods(m, solid):
    """웹게임에서 통째로 막힌 풀밭 칸(숲)을 다시 심는다. 흙길 곁에는 수관이 길과 갈림길 팻말을 덮지 않게 어린 나무만 심는다."""
    per_tile = solid.reshape(m.height, SUB, m.width, SUB)
    woods = per_tile.all(axis=(1, 3)) & (m.kinds == "grass")
    low = woods & forest.canopy_over(m.kinds == "path")
    rng = np.random.default_rng(m.seed)
    trunks = {(x, y) for x, y, _ in forest.plant(m, woods, rng, GROVE_MIXES, low=low)}
    for y, x in zip(*np.nonzero(low)):
        if (x, y) not in trunks and rng.random() < LOW_TREE_BUSH_SHARE:
            m.kinds[y, x] = "bush"


def build(project):
    source = web_map(project, MAP_ID)
    data = read(source / "map.json")
    width, height = data["width"], data["height"]
    m = MapLayout(MAP_ID, data["name"], width, height, music=data["music"], ambient=data["ambient"], seed=sum(map(ord, MAP_ID)))

    # ── 바닥: 웹게임 들판에 남북 길을 더한다 ──
    m.kinds[:] = np.array(read(source / "tiles.json")["tiles"], dtype=object)
    roads = m.near_line(NORTH_ROAD, ROAD_WIDTH / 2) | m.near_line(SOUTH_ROAD, ROAD_WIDTH / 2)
    m.kinds[roads] = "path"
    m.stamp("sign", NORTH_SIGN, SOUTH_SIGN)

    # ── 충돌: 웹게임 그대로 두고 길만 뚫는다 ──
    m.solid[:] = np.array([[cell == "1" for cell in row] for row in data["collision"]], dtype=bool)
    m.solid &= ~np.kron(roads, np.ones((SUB, SUB), dtype=bool))
    plant_woods(m, m.solid)
    m.block(0, 0, 1, height)
    m.block(width - 1, 0, width, height)
    m.keep_open(0, ROAD_Y - 1, 1, ROAD_Y + 2)
    m.keep_open(width - 1, ROAD_Y, width, ROAD_Y + 1)

    # ── 이야기 지점과 포탈 ──
    m.point("wolf", (20, 4), (30, 16))
    m.point("west", (0, ROAD_Y - 1), (0, ROAD_Y), (0, ROAD_Y + 1))
    m.point("enter", (2, ROAD_Y))
    m.point("east", (width - 1, ROAD_Y))
    m.point("eastArrive", (width - 2, ROAD_Y))
    m.point("north", (16, 0), (17, 0))
    m.point("northArrive", (17, 1))
    m.point("south", (32, height - 1), (33, height - 1))
    m.point("southArrive", (33, height - 2))
    m.point("signNorth", NORTH_SIGN, blocked=True)
    m.point("signSouth", SOUTH_SIGN, blocked=True)
    m.warp("west", "town", "eastArrive", "left")
    m.warp("east", "trainStop", "enter", "right")
    m.warp("north", "hideoutPath", "southArrive", "up")
    m.warp("south", "port", "northArrive", "down")
    return m


def main(project):
    print(build(project).publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
