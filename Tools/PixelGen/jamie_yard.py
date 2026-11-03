"""디엘의 어린 시절 집 마당(jamieYard, 4장 회상)을 새로 짠다(사용자 요청 2026-11-02: 회상 속 집 그래픽 개선).

숲속 빈터에 엄마와 아이가 사는 초가 오두막(houses.py 설계)이 서 있다. 집 앞 마당은 4장에서 병사들이 집을 에워싸고,
엄마와 카일이 마주 서고, 아이가 장작더미 뒤에 숨는 곳이라 가운데를 비운다. 둘레에 서쪽 텃밭, 동쪽 빨랫줄,
장작 패는 그루터기와 장작더미, 창 아래 의자, 꽃. 남쪽으로 숲을 빠져나가는 오솔길이 있다. 숲은 지나갈 수 없다.
웹게임 마당과 빈터 모양·지점 자리가 같다(이야기 칸 좌표 그대로). 남쪽 숲 나무가 마당을 가리지 않게 맵을 아래로 세 줄 늘렸다.

사용: python Tools/PixelGen/jamie_yard.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/jamieYard/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/jamieYard_preview.png     미리보기
"""
import sys
from pathlib import Path

import numpy as np

import forest
import yard
from houses import Design
from layout import MapLayout

WIDTH, HEIGHT = 24, 19
HOUSE_X, HOUSE_GROUND = 11.5, 5
COTTAGE = Design("jamie_cottage", 7, roof_material="thatch", walls="timber", shutters="green", lit=True, chimney_at=0.22,
                 seed=37)
PATH = [(11.5, 5), (11.6, 9), (11, 12.5), (11.8, 16), (11.2, 20)]
# 4장 인물이 서는 칸: 엄마, 카일, 숨는 자리, 네오, 병사 넷
STAGE = [(11, 8), (14, 9), (17, 10), (3, 13), (5, 7), (16, 7), (7, 11), (15, 11)]


def build():
    m = MapLayout("jamieYard", "디엘의 어린 시절 집 마당", WIDTH, HEIGHT, music="sorrow", seed=83)

    # ── 바닥: 숲(덤불), 둥근 빈터, 집 둘레, 오솔길 ──
    clearing = ((m.cx - 9.6) / 9.8) ** 2 + ((m.cy - 8.7) / 6.5) ** 2 <= 1
    clearing |= (m.cx > 6) & (m.cx < 18.5) & (m.cy < 5.5)
    paths = m.near_line(PATH, 1.2)
    woods = ~clearing & ~paths
    m.kinds[woods] = "tallgrass"
    m.line("path", PATH, 1.8)
    m.stamp("flowers", (13, 5), (14, 5), (7, 6), (15, 6), (6, 9), (17, 12), (3, 7), (9, 13), (14, 13))
    m.stamp("tallgrass", (2, 6), (18, 11), (16, 13), (4, 12))
    m.stamp("bush", (1, 9), (18, 12))

    # ── 초가 오두막과 마당 살림 ──
    door_x, _ = m.house(COTTAGE, HOUSE_X, HOUSE_GROUND)
    m.keep_open(door_x, HOUSE_GROUND - 1, door_x + 1, HOUSE_GROUND)
    rng = np.random.default_rng(84)
    m.decal("vegetables", yard.vegetable_patch(3.6, 2.5, rng), 2.2 * 32, 8.5 * 32)
    m.stand("laundry", yard.laundry_line(rng), 16.6, 6.5)
    for pole_x in (14.7, 18.4):
        m.block(pole_x - 0.15, 6.2, pole_x + 0.15, 6.5)
    m.stand("woodpile", yard.woodpile(rng), 17.5, 10, block=(2, 0.8))
    m.stand("chopping_block", yard.chopping_block(rng), 6.6, 5, block=(0.9, 0.4))
    m.stand("bench", yard.bench(rng), 9.2, 6, block=(1.8, 0.5))
    m.thing("barrel", 16, 4)

    # ── 숲: 여러 종류를 무리 지어 섞어 심는다(forest.plant). 맵이 작아 빈터 둘레 나무의 수관이 마당 가장자리를 덮는다(뒤로 가면 반투명해진다).
    #    4장 인물이 서는 자리와 오솔길 위로는 수관이 덮지 않게 그 자리 나무는 덤불로 바꾼다.
    stage = paths.copy()
    for x, y in STAGE:
        stage |= np.hypot(m.cx - (x + 0.5), m.cy - (y + 0.5)) < 1.0
    shades_stage = forest.canopy_over(stage, side=1, rows=3)
    # 아이가 숨는 장작더미는 큰 활엽수 수관도 덮지 않게 더 넓게 비운다.
    woodpile = np.zeros_like(stage)
    woodpile[9:11, 16:19] = True
    shades_stage |= forest.canopy_over(woodpile)
    trunks = np.zeros_like(woods)
    for x, y, _ in forest.plant(m, woods & ~shades_stage, np.random.default_rng(9)):
        trunks[y, x] = True
    # 나무를 세우지 못한 숲 가장자리는 덤불을 촘촘히 둘러 막힌 곳임이 보이게 한다.
    edge = woods & ~trunks & (np.roll(~woods, 1, 0) | np.roll(~woods, -1, 0) | np.roll(~woods, 1, 1) | np.roll(~woods, -1, 1))
    for y, x in zip(*np.nonzero(edge)):
        m.stamp("bush", (x, y))
    m.block_where(woods)
    m.border()

    # ── 4장 지점: 문, 엄마, 카일, 아이가 숨는 장작더미 앞, 네오가 오는 숲 끝, 집을 에워싼 병사들, 마당 가운데 ──
    m.point("door", (door_x, HOUSE_GROUND - 1))
    m.point("jamie", (11, 8))
    m.point("kyle", (14, 9))
    m.point("hide", (17, 10))
    m.point("neo", (3, 13))
    m.point("troops", *STAGE[4:])
    m.point("view", (12, 8))
    return m


def main(project):
    m = build()
    for goal in ("jamie", "kyle", "hide", "neo", "troops"):
        for cell in m.points[goal]:
            if not m.reachable(m.points["door"][0], [cell]):
                print(f"문에서 {goal} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
