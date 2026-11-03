"""디엘의 어린 시절 집 안(jamieHouse, 4장 회상)을 새로 그린다(사용자 요청 2026-11-02: 회상 속 집 안 그래픽 개선).

엄마와 어린 디엘이 사는 숲가 오두막의 한 칸 방이다. 북쪽 벽에 솥을 건 아궁이, 창, 찬장, 접시 선반과 말린 약초, 아이 그림.
가운데에 저녁이 차려진 식탁(4장: 「식탁에는 엄마가 차려 둔 저녁이 식은 채 그대로」), 서쪽에 아이 침대, 아궁이 곁 장작 바구니,
남쪽 문 앞 발깔개. 바닥과 벽은 room_interior.RoomInterior가 그린다.
웹게임 집과 방 크기가 같고, 벽면이 두 칸이 되도록 한 줄 아래로 내렸다(칸 y + 1). 4장이 쓰는 자리는 이름 있는 지점이다.

사용: python Tools/PixelGen/jamie_house.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/jamieHouse/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/jamieHouse_preview.png     미리보기
"""
import sys
from pathlib import Path

import numpy as np

import home_furniture as hf
import pixelgen as pg
from layout import LOGICAL, MapLayout
from room_interior import RoomInterior, face_decal, floor_decal

T = pg.TILE
WIDTH, HEIGHT = 12, 10
ROOM = (1, 2, 11, 9)            # 바닥 칸 (x0, y0, x1, y1)
DOOR = (5, 9)                   # 남쪽 문지방 칸
HEARTH = 5.5                    # 아궁이 가운데(칸)
WINDOW = 8.6
LAMP = "#ffc070"


def build():
    m = MapLayout("jamieHouse", "디엘의 어린 시절 집", WIDTH, HEIGHT, theme="interior", music="sorrow", seed=81)
    room = RoomInterior(WIDTH, HEIGHT, [ROOM], seed=81)
    row = ROOM[1]
    room.paint_base()
    room.window(WINDOW, row)
    room.fireplace(HEARTH, row)
    room.entrance(*DOOR)
    m.paint_ground = room.ground
    m.block_where(~room.floor)
    rng = np.random.default_rng(82)

    # ── 벽: 솥 건 아궁이, 아이 그림, 접시 선반, 말린 약초 ──
    m.fire(5, 1, "fire")
    m.light(HEARTH * LOGICAL, 1.6 * LOGICAL, 56, "#ff9a40", flicker=True)
    m.decal("pot", hf.cooking_pot(rng), HEARTH * T - 14, row * T - 34)
    face_decal(m, "drawing", hf.child_drawing(rng), 1.7, row)
    face_decal(m, "dish_shelf", hf.dish_shelf(rng), 3.3, row)
    face_decal(m, "herbs", hf.herbs(rng), 7.2, row)
    m.decal("toy_sword", hf.toy_sword(rng), 1.1 * T, row * T - 30)

    # ── 바닥: 땋은 깔개 위 식탁과 의자 둘, 아이 침대, 찬장, 장작 바구니, 문 앞 발깔개 ──
    floor_decal(m, "rug", hf.braided_rug(4.6, 2.8, rng), 3.2, 4.4)
    m.decal("doormat", hf.doormat(rng), DOOR[0] * T - 4, 8.4 * T)
    m.stand("supper_table", hf.supper_table(rng), 5.5, 6, block=(2.2, 0.7))
    m.stand("chair_left", hf.chair(rng, "right"), 3.9, 6, block=(0.6, 0.4))
    m.stand("chair_right", hf.chair(rng, "left"), 7.1, 6, block=(0.6, 0.4))
    m.light(5.5 * LOGICAL, 5.2 * LOGICAL, 30, LAMP, flicker=True)
    m.stand("child_bed", hf.child_bed(rng), 2, 5, block=(0.9, 1.6))
    m.stand("dresser", hf.dresser(rng), 10.15, 3, block=(1.6, 0.6))
    m.stand("firewood", hf.firewood_basket(rng), 3.8, 3, block=(0.9, 0.4))

    # 4장(웹게임 집 칸 y + 1): 식탁 곁의 어린 디엘, 엄마가 선 자리, 아궁이 앞, 문 앞
    m.point("childStart", (4, 6))
    m.point("jamie", (6, 3))
    m.point("hearth", (5, 2))
    m.point("exit", (5, 8))
    return m


def main(project):
    m = build()
    for goal in ("jamie", "hearth", "exit"):
        if not m.reachable(m.points["childStart"][0], m.points[goal]):
            print(f"식탁 곁에서 {goal}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
