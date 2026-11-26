"""뱀성 꼭대기(castleRoof): 돌뱀 머리 위(뱀 머리 위에서 경비대장과 싸운 뒤 글라이더로 떨어진다).

30×30칸. 맵 거의 전부가 거대한 돌뱀 머리다. 위(북쪽) 목에 3층 보물고에서 올라오는 계단 구멍이 있고,
양옆에 보석 눈, 아래(남쪽) 주둥이 끝에 글라이더가 묶여 있다. 머리는 조각 모형(snake_head_model)이고,
머리 밖은 까마득히 아래에 까는 땅(산, 강, 숲)과 그 사이를 흐르는 구름이다(layout.below).

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
import snake_head_model
from layout import MapLayout, LOGICAL

T = pg.TILE
WIDTH, HEIGHT = 30, 30
HEAD_W, HEAD_H = 22, 25          # 머리 그림 크기(칸), 맵 위쪽 끝에 붙는다
SEA = pg.hexes("#1c3a5a", "#24507a", "#2e6894")
LAND = pg.hexes("#2c4a26", "#3e6030", "#55753a", "#7a7a48", "#8a6e4a", "#9a8c7a", "#c8c4bc", "#eef0f2")
HAZE = np.array([168, 190, 214])
CLOUD = pg.hexes("#9fb4cc", "#bccddf", "#d6e2ee", "#eaf1f7", "#fbfdfe")
FAR = 12                         # 먼 땅·구름 그림은 한 칸에 12점(멀고 흐려 보인다)
# 먼 땅이 깔리는 칸 범위(왼쪽, 위, 너비, 높이)와 깊이(칸). 원근 카메라에 화면 위 끝까지 보이는 만큼이다.
LAND_AREA, LAND_DEPTH = (-45, -58, 120, 96), 17
# 구름: (그림, 왼쪽, 위, 깊이, 흐르는 빠르기(칸/초))
CLOUDS = [("cloud_a", -20, -22, 11, 0.5), ("cloud_b", 18, -34, 12, 0.42), ("cloud_c", -34, -6, 8, 0.7), ("cloud_a", 36, 4, 9, 0.62),
          ("cloud_b", -8, 14, 7, 0.8), ("cloud_c", 22, 16, 10, 0.55), ("cloud_a", -38, 20, 12, 0.4), ("cloud_c", 2, -44, 11, 0.45),
          ("cloud_b", 46, -14, 9, 0.6), ("cloud_c", 40, -46, 12, 0.4), ("cloud_b", -42, -38, 10, 0.5), ("cloud_a", -12, 2, 11, 0.46),
          ("cloud_b", 30, -4, 12, 0.44), ("cloud_a", 52, 24, 8, 0.7), ("cloud_c", -30, -26, 12, 0.42)]
SHADOW_OPACITY = 0.4             # 땅에 진 구름 그림자가 덮는 정도
CLOUD_SPAN = 110                 # 구름이 이만큼(칸) 흐르면 처음 자리로 돌아온다


def far_below(height, width, rng):
    """
    아득히 아래로 보이는 땅: 높이 잡음으로 바다·숲·들·산·눈 덮인 봉우리를 칠하고 비탈 그늘을 준 뒤,
    거리만큼 푸른 안개를 섞는다.
    """
    elevation = pg.fractal_noise(height, width, rng, ((90, 0.46), (30, 0.3), (9, 0.16), (4, 0.08)))
    gy, gx = np.gradient(elevation)
    shade = np.clip(0.5 - (gx + gy) * 9, 0, 1)
    level = np.digitize(elevation, [0.36, 0.42, 0.5, 0.58, 0.64, 0.7, 0.76])
    land = np.where((level == 0)[..., None], SEA[np.clip((elevation * 6).astype(int), 0, 2)], LAND[np.clip(level - 1, 0, 7)])
    land = (land * (0.75 + 0.5 * shade[..., None])).clip(0, 255)
    # 강: 잡음의 능선을 따라 굽이치는 물줄기
    winding = np.abs(pg.fractal_noise(height, width, rng, ((150, 0.6), (40, 0.3), (12, 0.1))) - 0.5)
    river = (winding < 0.006) & (level > 0) & (level < 6)
    land[river] = SEA[2]
    return (land * 0.8 + HAZE * 0.2).astype(np.uint8)


def shadow_of(image):
    """구름이 땅에 드리운 그림자(짙은 남색 실루엣)."""
    out = image.copy()
    out[..., :3] = (22, 34, 58)
    return out


def cloud(width, height, seed, palette=CLOUD):
    """위에서 본 구름 한 덩이: 둥근 송이를 겹친 실루엣에, 해가 오는 오른쪽 위가 밝고 아래 가장자리가 그늘진다."""
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:height, 0:width]
    bulk = np.zeros((height, width))
    for _ in range(26):
        t = rng.uniform(-1, 1)
        cx = width / 2 + t * width * 0.34
        cy = height / 2 + rng.normal(0, 0.12) * height
        r = height * rng.uniform(0.14, 0.3) * (1 - 0.5 * abs(t))
        bulk = np.maximum(bulk, np.sqrt(np.clip(1 - ((xx - cx) / (r * 1.5)) ** 2 - ((yy - cy) / r) ** 2, 0, 1)) * r)
    painted = bulk > 1.2
    gy, gx = np.gradient(bulk)
    light = np.clip(0.62 + (gy - gx) * 0.22 + bulk / height * 0.6, 0, 1)
    rgb = pg.quantize(np.clip(light, 0, 0.999), palette, dither=0.06)
    return np.dstack([rgb, np.where(painted, 255, 0)]).astype(np.uint8)


def build():
    m = MapLayout("castleRoof", "뱀성 꼭대기", WIDTH, HEIGHT, theme="castle", music="castle", seed=91)
    rng = np.random.default_rng(91)
    head = serpent_sculpture.head_top(HEAD_W * T, HEAD_H * T, rng)
    left = (WIDTH - HEAD_W) * T // 2
    hatch = (14, 3)

    def paint():
        # 바닥 모형과 먼 땅이 화면을 그린다. 이 그림은 잔디 자리 지도와 미리보기에 쓴다.
        ground = np.zeros((HEIGHT * T, WIDTH * T, 4), dtype=np.uint8)
        yy, xx = np.mgrid[0:HEAD_H * T, 0:HEAD_W * T]
        half = serpent_sculpture.head_half((yy + 0.5) / (HEAD_H * T) * 2 - 1)
        inside = np.abs((xx + 0.5) / (HEAD_W * T) * 2 - 1) < half
        ground[0:HEAD_H * T, left:left + HEAD_W * T][inside] = head.image[inside]
        return ground

    m.paint_ground = paint
    m.ground_model = snake_head_model.build(head, left, (hatch[0] * T, hatch[1] * T, (hatch[0] + 2) * T, (hatch[1] + 2) * T))
    x, y, w, h = LAND_AREA
    m.below("land", far_below(h * FAR, w * FAR, rng), LAND_DEPTH, x, y, w, h)
    shapes = {"cloud_a": cloud(18 * FAR, 8 * FAR, 1), "cloud_b": cloud(12 * FAR, 6 * FAR, 2), "cloud_c": cloud(22 * FAR, 9 * FAR, 3)}
    for name, x, y, depth, drift in CLOUDS:
        h, w = shapes[name].shape[0] / FAR, shapes[name].shape[1] / FAR
        m.below(name, shapes[name], depth, x, y, w, h, drift, CLOUD_SPAN)
        # 해가 오른쪽에서 비추므로 그림자는 땅 위 왼쪽 아래에 진다.
        fall = LAND_DEPTH - depth
        m.below("shadow_" + name, shadow_of(shapes[name]), LAND_DEPTH - 0.2, x - fall, y + fall * 0.4, w, h, drift, CLOUD_SPAN, SHADOW_OPACITY)

    # 걸을 수 있는 곳: 머리 윗면(가장자리와 솟은 눈에서 반 칸 안쪽)만
    cells = np.zeros((HEIGHT, WIDTH), dtype=bool)
    for y in range(HEIGHT):
        for x in range(WIDTH):
            px, py = x * T + T // 2 - left, y * T + T // 2
            if 0 <= px < HEAD_W * T and py < HEAD_H * T:
                y0, y1 = max(0, py - 10), min(HEAD_H * T, py + 10)
                x0, x1 = max(0, px - 10), min(HEAD_W * T, px + 10)
                cells[y, x] = head.walk[y0:y1, x0:x1].all()
    m.block_where(~cells)
    m.block(hatch[0], hatch[1], hatch[0] + 2, hatch[1] + 2)
    image, pivot = castle.glider(rng)
    m.upright("glider", image, 11.5 * T, 22 * T, pivot, 22 * LOGICAL, castle.glider_model(pivot, rng))
    m.block(10, 21.4, 13, 22)
    for ex, ey, er in head.eyes:
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
