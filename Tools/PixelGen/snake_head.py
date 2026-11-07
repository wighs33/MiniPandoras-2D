"""뱀성 꼭대기(castleRoof): 돌뱀 머리 위(뱀 머리 위에서 경비대장과 싸운 뒤 글라이더로 떨어진다).

30×26칸. 맵 거의 전부가 위에서 내려다본 거대한 돌뱀 머리다. 위(북쪽) 목에 3층 보물고에서 올라오는 계단 구멍이 있고,
양옆에 보석 눈, 아래(남쪽) 주둥이 끝에 글라이더가 묶여 있다. 머리 밖은 아득히 아래로 보이는 땅(산, 강, 숲)과 그 사이 구름이다.

지점: arrive(계단 구멍 앞), deadEnd(주인공이 몰리는 주둥이 쪽), captain(경비대장·병사가 올라오는 자리 둘),
captainAfter(싸움 뒤 두 사람 자리), glider(글라이더 옆), jump(주둥이 끝).

사용: python Tools/PixelGen/snake_head.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np

import castle
import pixelgen as pg
import serpent_sculpture
from interior import STAIR_DARK
from layout import MapLayout, LOGICAL

T = pg.TILE
WIDTH, HEIGHT = 30, 26
HEAD_W, HEAD_H = 22, 25          # 머리 그림 크기(칸), 맵 위쪽 끝에 붙는다
SEA = pg.hexes("#1c3a5a", "#24507a", "#2e6894")
LAND = pg.hexes("#2c4a26", "#3e6030", "#55753a", "#7a7a48", "#8a6e4a", "#9a8c7a", "#c8c4bc", "#eef0f2")
HAZE = np.array([168, 190, 214])
CLOUD = pg.hexes("#c8d6e4", "#e2eaf2", "#f6f8fb")


def far_below(height, width, rng):
    """
    아득히 아래로 보이는 땅: 높이 잡음으로 바다·숲·들·산·눈 덮인 봉우리를 칠하고 비탈 그늘을 준 뒤,
    거리만큼 푸른 안개를 섞는다. 그 위로 구름이 떠 있다.
    """
    scale = 2
    h, w = height // scale + 1, width // scale + 1
    elevation = pg.fractal_noise(h, w, rng, ((70, 0.5), (22, 0.3), (7, 0.2)))
    gy, gx = np.gradient(elevation)
    shade = np.clip(0.5 - (gx + gy) * 6, 0, 1)
    level = np.digitize(elevation, [0.36, 0.42, 0.5, 0.58, 0.64, 0.7, 0.76])
    land = np.where((level == 0)[..., None], SEA[np.clip((elevation * 6).astype(int), 0, 2)], LAND[np.clip(level - 1, 0, 7)])
    land = (land * (0.75 + 0.5 * shade[..., None])).clip(0, 255)
    land = land * 0.55 + HAZE * 0.45
    image = np.repeat(np.repeat(land, scale, axis=0), scale, axis=1)[:height, :width].astype(np.uint8)
    cloud = pg.fractal_noise(height, width, rng, ((60, 0.7), (20, 0.3)))
    image[cloud > 0.66] = CLOUD[0]
    image[cloud > 0.71] = CLOUD[1]
    image[cloud > 0.76] = CLOUD[2]
    return image


def build():
    m = MapLayout("castleRoof", "뱀성 꼭대기", WIDTH, HEIGHT, theme="castle", music="castle", seed=91)
    rng = np.random.default_rng(91)
    head, walk, eyes = serpent_sculpture.head_top(HEAD_W * T, HEAD_H * T, rng)
    left = (WIDTH - HEAD_W) * T // 2
    hatch = (14, 3)

    def paint():
        ground = far_below(HEIGHT * T, WIDTH * T, rng)
        region = ground[0:HEAD_H * T, left:left + HEAD_W * T]
        opaque = head[..., 3] > 0
        region[opaque] = head[opaque, :3]
        # 목에 낸 계단 구멍(3층 보물고에서 올라온다)
        x0, y0 = hatch[0] * T, hatch[1] * T
        ground[y0 - 4:y0 + 2 * T + 4, x0 - 4:x0 + 2 * T + 4] = castle.STONE[4]
        ground[y0 - 4:y0 - 1, x0 - 4:x0 + 2 * T + 4] = castle.STONE[6]
        for k in range(0, 2 * T, 6):
            ground[y0 + k:y0 + k + 6, x0:x0 + 2 * T] = STAIR_DARK[max(0, 4 - k // 12)]
        return ground

    m.paint_ground = paint
    # 걸을 수 있는 곳: 머리 윗면(가장자리에서 반 칸 안쪽)만
    cells = np.zeros((HEIGHT, WIDTH), dtype=bool)
    for y in range(HEIGHT):
        for x in range(WIDTH):
            px, py = x * T + T // 2 - left, y * T + T // 2
            if 0 <= px < HEAD_W * T and py < HEAD_H * T:
                y0, y1 = max(0, py - 10), min(HEAD_H * T, py + 10)
                x0, x1 = max(0, px - 10), min(HEAD_W * T, px + 10)
                cells[y, x] = walk[y0:y1, x0:x1].all()
    m.block_where(~cells)
    for ex, ey, er in eyes:
        x, y = (ex + left) / T, ey / T
        m.block(x - er / T, y - er / T, x + er / T, y + er / T)
    m.block(hatch[0], hatch[1], hatch[0] + 2, hatch[1] + 2)
    image, pivot = castle.glider(rng)
    m.upright("glider", image, 11.5 * T, 22 * T, pivot, 22 * LOGICAL)
    m.block(10, 21.4, 13, 22)
    for ex, ey, er in eyes:
        m.light((ex + left) / 2, ey / 2, 34, "#ff6a2a", flicker=True)

    m.point("arrive", (15, 6))
    m.point("deadEnd", (15, 18))
    m.point("captain", (14, 6), (16, 6))
    m.point("captainAfter", (14, 10), (16, 9))
    m.point("glider", (13, 21))
    m.point("jump", (15, 23))
    return m


def main(project):
    print(build().publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
