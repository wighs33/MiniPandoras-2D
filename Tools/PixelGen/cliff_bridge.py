"""들판과 마을 사이의 벼랑 다리(cliffBridge)를 짠다(들판 아랫길이 곧바로 마을 왼쪽 길로 이어져 방향이 어색했다).

들판에서 내려온 길이 벼랑 위를 지나 나무다리(bridges.py)로 골짜기를 건너고, 맞은편에서 동쪽으로 굽어 마을 서쪽 어귀에 닿는다.
벼랑 바위 벽이 강까지 떨어지는 골짜기가 맵을 동서로 가로지르므로, 건너는 곳은 다리뿐이다.
모습이 둘이다. 처음(broken)은 강 위 구간이 끊어졌다. 판자 없는 칸도 걸어 들어갈 수 있고, 지점 gap(끊어진 구간
첫 줄)을 밟으면 낭떠러지로 떨어져 죽는다(연출은 이야기가 건다). 뱀성에서 달아난 뒤의 모습(repaired)은
그 구간이 밝은 새 판자와 새 난간으로 바뀌었고, 옛 판자와 만나는 곳을 밧줄로 동여맸으며, 건너편에 쓰고 남은 장작과 상자가 있다.

사용: python Tools/PixelGen/cliff_bridge.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/cliffBridge/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/cliffBridge_preview.png      미리보기(끊어진 모습)
  Docs/ArtUpgrade/PixelGen/cliffBridge_repaired_preview.png    미리보기(고친 모습)
"""
import sys
from pathlib import Path

import numpy as np

import bridges
import forest
import yard
from layout import MapLayout

WIDTH, HEIGHT = 44, 34
BRIDGE_X = (12, 15)             # 다리가 놓이는 칸(가운데 13.5)
ROAD_X = 14                     # 흙길은 이 줄을 가운데 두고 두 칸 너비다
NORTH_ROAD = [(ROAD_X, -1), (ROAD_X, 12)]
SOUTH_ROAD = [(ROAD_X, 21), (ROAD_X, 25), (18, 28.5), (30, 27.8), (WIDTH + 1, 28.5)]
ROAD_WIDTH = 2.2
EAST_ROW = 28
SIGN = (16, 25)
NOTICE = (16, 10)               # 다리 앞 팻말
GAP = (16, 20)                  # 끊어진 구간(행). 강 위다


def build():
    m = MapLayout("cliffBridge", "벼랑 다리", WIDTH, HEIGHT, music="field", seed=361)
    rng = np.random.default_rng(362)

    # ── 골짜기: 북쪽 벼랑의 바위 벽이 강까지 떨어진다 ──
    m.line("cliff", [(-1, 13.6), (10, 14.2), (24, 13.4), (WIDTH + 1, 14.2)], 4.4)
    m.line("water", [(-1, 18.6), (12, 19), (26, 18.2), (WIDTH + 1, 18.8)], 6)
    gorge = np.nonzero(np.isin(m.kinds[:, list(BRIDGE_X)], ("cliff", "water")).any(axis=1))[0]

    # ── 흙길과 들꽃 ──
    m.line("path", NORTH_ROAD, ROAD_WIDTH)
    m.line("path", SOUTH_ROAD, ROAD_WIDTH)
    m.stamp("flowers", (20, 4), (21, 5), (31, 7), (32, 6), (8, 8), (7, 24), (21, 24), (22, 25), (35, 24), (36, 31))
    m.stamp("tallgrass", (24, 8), (25, 8), (26, 9), (34, 4), (35, 4), (8, 26), (9, 27), (27, 23), (28, 23), (39, 24))
    m.stamp("rock", (19, 9), (29, 10), (37, 9), (6, 23), (24, 24), (38, 31))
    m.stamp("bush", (9, 10), (38, 5), (10, 30))
    m.stamp("sign", SIGN, NOTICE)

    # ── 벼랑 위와 건너편 가장자리는 숲이다. 길 곁에는 수관이 길을 덮지 않게 어린 나무만 심는다 ──
    walk = m.near_line(NORTH_ROAD, 2.6) | m.near_line(SOUTH_ROAD, 2.6)
    edge = (m.cy < 3) | (m.cy > HEIGHT - 3.5) | (m.cx < 4) | (m.cx > WIDTH - 3)
    woods = edge & ~walk & (m.kinds == "grass")
    forest.plant(m, woods, np.random.default_rng(363), low=woods & forest.canopy_over(m.kinds == "path"))
    m.block_where(woods)
    m.border()

    with m.look("broken"):
        bridges.span(m, *BRIDGE_X, gorge.min(), gorge.max(), rng, broken=GAP)
    with m.look("repaired"):
        bridges.span(m, *BRIDGE_X, gorge.min(), gorge.max(), np.random.default_rng(364), mended=GAP)
        m.stand("spare_timber", yard.woodpile(np.random.default_rng(365)), 19.5, 24, block=(2.6, 0.8))
        m.thing("crate", 10, 23)
        m.thing("barrel", 11, 24)

    # ── 이야기 지점과 포탈 ──
    m.point("north", (ROAD_X - 1, 0), (ROAD_X, 0))
    m.point("northArrive", (ROAD_X, 2))
    m.point("east", (WIDTH - 1, EAST_ROW - 1), (WIDTH - 1, EAST_ROW), (WIDTH - 1, EAST_ROW + 1))
    m.point("eastArrive", (WIDTH - 3, EAST_ROW))
    m.point("sign", SIGN, blocked=True)
    m.point("notice", NOTICE, blocked=True)
    # 양 끝 칸은 난간에 조금 걸치므로 막힌 칸으로 친다.
    m.point("gap", *[(x, GAP[0]) for x in range(*BRIDGE_X)], blocked=True)
    m.warp("north", "hillia", "southArrive", "up")
    m.warp("east", "town", "arrive", "right")
    return m


def main(project):
    m = build()
    north, east = m.points["northArrive"][0], m.points["eastArrive"][0]
    if not m.reachable(north, [east], look="repaired"):
        print("고친 다리를 건너 동쪽 출구에 닿지 못한다")
    # 끊어진 모습에서도 충돌상 건너편까지 이어지지만, gap을 밟는 순간 떨어지므로 건너지는 못한다.
    if not m.reachable(north, m.points["gap"], look="broken"):
        print("끊어진 구간(gap)까지 걸어가지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
