"""타이틀 화면의 풍경 두 장. 부마다 그 부의 이야기와 지형으로 그린다(Unity의 UI.TitleScenery가 겹을 쌓고 움직인다).

1부(0~5장, 뱀열차에 오르기까지): 오후의 힐리아 동부. 왼쪽 언덕의 뱀성, 굽이치는 강과 절벽 다리, 오른쪽 언덕의 마을과 숲, 들판과 모래 길.
  앞쪽 정류장 승강장에 디엘·주인공·랑이 서고, 뱀열차가 들어와 멈췄다가 떠난다(5장 끝의 그 순간).
  Part1/  sky, clouds, far(먼 산과 뱀성), valley(강·절벽 다리·마을·숲), fields(들판과 모래 길, S자 홈), ground(승강장과 앞 풀밭)
2부(6~11장, 뱀열차에 탄 뒤): 밤의 힐리아 서부 사막. 큰 달, 왼쪽에 드래곤 고원의 뷰트, 오른쪽 지평선에 빛나는 거대고목, 겹겹의 달빛 사구.
  다시 모인 여섯(라르고·이자벨·오로라·디엘·주인공·랑)을 태운 나리온이 사구 위를 날아가고, 앞 사구에는 등성이로 오른 발자국이 남았다.
  2부에는 뱀열차가 나오지 않는다(6장 첫 객실뿐). 2부의 끝에 일행은 나리온을 타고 아이로스로 간다.
  Part2/  sky, stars_a, stars_b, far(고원과 거대고목), dunes(사구 겹 넷), ground(앞 사구)
          narion_0·1(여섯을 태운 나리온, 밤빛)은 Tools/WebGame/narion.py가 굽는다(웹게임 원화를 PixelOE로 바꾼 것). 이 스크립트는 미리보기에만 얹는다.

게임 안의 뱀열차(snake_train.py), 나리온(Tools/WebGame/narion.py)과 웹게임 인물 그림을 그대로 쓰므로 풍경만 그린다.
크기: 가상 화면 높이 216, 너비 600(21:9보다 긴 화면까지 덮는다). 점 하나 = 가상 px 반(컷인과 같은 점 크기)이고, 키우지 않고 1200×432로 저장해
Unity가 점 필터로 600×216에 늘인다(겹이 많아 두 배로 저장하면 메모리가 네 배 든다).
가로 자리는 화면 가운데에서 잰 가상 px, 높이는 화면 아래에서 잰 가상 px로 적는다(Unity와 같은 값).
뱀열차는 논리 px 하나 = 가상 px 하나로 서므로 홈의 물결도 게임(SnakeTrain.cs)과 같은 굽이 140, 깊이 7이다.

  Assets/MiniPandoras/Art/PixelGen/UI/Title/Part1/, Part2/
  Docs/ArtUpgrade/PixelGen/title_part1_preview.png, title_part2_preview.png   겹과 뱀열차·인물을 얹은 미리보기(16:9와 20:9)

사용: python Tools/PixelGen/title_scene.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from cutin_gods import color
from cutin_train import ellipse, grid, mask_paint, stepped

UNITS_W, UNITS_H = 600, 216
W, H = UNITS_W * 2, UNITS_H * 2          # 점 격자(점 하나 = 가상 px 반)

# Unity(TitleScenery·프리팹)와 같은 값: 홈의 물결, 부마다 뱀열차가 땅에 닿는 높이.
GROOVE_LENGTH, GROOVE_DEPTH = 140, 7
# 1부: 열차 길을 낮게 두어 그 위로 강·다리·마을이 보이게 한다. 승강장(앞쪽 오른쪽)에 셋이 서고, 열차는 불 켜진 문(머리에서 102)이 디엘과 주인공 사이로 보이게 멈춘다.
# 승강장 왼쪽 끝은 16:9 화면에서 메뉴 「장 선택」(가운데에서 76까지) 오른쪽에 둔다.
PART1_GROUND = 44
PLATFORM_LEFT, PLATFORM_TOP = 84, 30
PART1_PEOPLE = (("diel", 126), ("hero", 150), ("rang", 174))
PART1_STOP_X = 36
# 2부: 앞 사구 꼭대기, 달, 나리온이 나는 자리(미리보기의 가운데, Unity의 flyerStart와 같다).
RIDGE_TOP = 40
MOON = (172, 180)
NARION_AT = (-60, 88)


# ── 공통 도구 ───────────────────────────────────────────

def dot_x(units):
    """화면 가운데에서 잰 가상 px → 점 열."""
    return (units + UNITS_W / 2) * 2


def dot_y(units):
    """화면 아래에서 잰 가상 px → 점 행."""
    return (UNITS_H - units) * 2


YY, XX = grid(H, W)
UNITS_X = XX / 2 - UNITS_W / 2


def blank():
    return np.zeros((H, W, 4))


def sky_column(stops):
    """점 행마다의 하늘 색. stops는 (화면 아래에서 잰 높이, 색)이다."""
    units = UNITS_H - np.arange(H) / 2
    at = [u for u, _ in stops]
    colors = [color(c) for _, c in stops]
    return np.stack([np.interp(units, at, [c[k] for c in colors]) for k in range(3)], axis=-1)


def sky(stops):
    column = sky_column(stops)
    image = blank()
    t = stepped(YY / (H - 1), 40)
    image[..., :3] = column[(t * (H - 1)).astype(int)]
    image[..., 3] = 1
    return image, column


def glow(image, center, radius, rgb, alpha, stretch=1.6, levels=7):
    sx, sy = dot_x(center[0]), dot_y(center[1])
    halo = np.clip(1 - np.hypot((XX - sx) / stretch, YY - sy) / radius, 0, 1)
    mask_paint(image, stepped(halo, levels), rgb, alpha)


def stars(seed, count, keep, floor_units):
    """하늘 위쪽에 흩은 별. keep으로 두 벌에 나눠 담는다(반짝임은 두 벌이 엇갈려 밝아지는 것이다)."""
    rng = np.random.default_rng(seed)
    image = blank()
    floor = dot_y(floor_units)
    for i in range(count):
        x, y = int(rng.uniform(2, W - 3)), int(rng.uniform(2, floor))
        bright = rng.uniform(0.35, 1.0) * np.clip(1.25 - y / floor, 0, 1)
        if not keep(i):
            continue
        rgb = color("#fff6e4")
        image[y, x, :3] = rgb
        image[y, x, 3] = bright
        if bright > 0.75:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                image[y + dy, x + dx, :3] = rgb
                image[y + dy, x + dx, 3] = bright * 0.4
    return image


def ridge(rng, base, height, cycles, jag, sharp=True):
    """가로로 이어지는 등성이(점 행). sharp면 사인을 접어(abs) 뾰족한 봉우리를, 아니면 둥근 언덕을 만든다."""
    x = np.arange(W) / W * 2 * np.pi
    if sharp:
        peaks = sum(height / (k ** 1.2) * np.abs(np.sin(x * cycles * k / 2 + rng.uniform(0, 6.3))) for k in range(1, 4))
    else:
        peaks = sum(height / (k ** 1.4) * (np.sin(x * cycles * k + rng.uniform(0, 6.3)) + 1) / 2 for k in range(1, 4))
    rough = jag * pg.noise(1, W, 5, rng)[0]
    return dot_y(base) - 2 * (peaks + rough)


def below(line):
    return YY >= line[XX]


def paint(image, mask, rgb, alpha=1.0):
    mask_paint(image, np.asarray(mask, dtype=float), rgb, alpha)


def box(cx, base, x0, x1, y0, y1):
    """점 좌표 (cx, base)에서 가로 x0~x1, 위로 y0~y1(점)인 사각형."""
    return (XX >= cx + x0) & (XX < cx + x1) & (YY >= base - y1) & (YY < base - y0)


def cone(cx, base, x, bottom, height, half):
    return (YY >= base - bottom - height) & (YY < base - bottom) & (np.abs(XX - (cx + x)) <= (YY - (base - bottom - height)) * half / height)


def groove_line(ground):
    """뱀열차의 몸이 땅에 닿는 줄(점 행): 게임과 같은 물결이고, 값이 클수록 앞(아래)으로 휜다."""
    return dot_y(ground) + 2 * np.sin(UNITS_X * 2 * np.pi / GROOVE_LENGTH) * GROOVE_DEPTH


def track(image, rng, ground, dirt, light, dark, edge):
    """뱀열차가 다니는 길: 다져진 띠, 몸이 닿는 S자 홈, 위쪽 가장자리. 길 아래 끝(점 행)을 돌려준다."""
    top, bottom = dot_y(ground + 7), dot_y(ground - 9)
    road = (YY >= top) & (YY < bottom)
    noise = pg.noise(H, W, 4, rng)
    paint(image, road, dirt)
    paint(image, road & (noise > 0.62), light)
    paint(image, road & (noise < 0.3), dark)
    groove = groove_line(ground)
    paint(image, road & (np.abs(YY - groove) <= 1.5), dark * 0.8)
    paint(image, road & (np.abs(YY - groove + 2) <= 0.5), light, 0.7)
    paint(image, road & (YY < top + 2), edge)
    return bottom


def snake_castle(image, x_units, base_units, p):
    """
    뱀성: 계단식 성벽 세 단, 성벽과 양 옆 탑, 가운데 탑을 세 번 휘감은 돌뱀, 원뿔 지붕 뒤로 고개를 든 뱀 머리.
    p는 돌·지붕·돌뱀·창·햇빛(오른쪽 모서리) 색이다.
    """
    cx, base = dot_x(x_units), dot_y(base_units)
    paint(image, ellipse(YY, XX, cx, base + 8, 124, 30) & (YY < base + 12), p["hill"])
    level = 0
    for half, height in ((86, 16), (70, 15), (56, 13)):
        tier = box(cx, base, -half, half, level, level + height)
        paint(image, tier, p["stone"])
        paint(image, tier & (YY == base - level - height), p["stone_top"])
        paint(image, tier & (XX >= cx + half - 2), p["rim"], 0.75)
        for wx in range(-half + 10, half - 6, 18):
            paint(image, box(cx, base, wx, wx + 1, level + 3, level + 10), p["shade"])
        level += height
    wall_top = level + 13
    paint(image, box(cx, base, -48, 48, level, wall_top), p["stone"])
    paint(image, box(cx, base, -48, 48, level, wall_top + 3) & ((((XX - cx) // 4) % 2 == 0) | (YY >= base - wall_top)), p["stone"])
    paint(image, box(cx, base, 46, 48, level, wall_top + 3), p["rim"], 0.75)
    paint(image, box(cx, base, -6, 6, level, level + 9), p["shade"])
    for side in (-1, 1):
        tx = 48 * side
        paint(image, box(cx, base, tx - 8, tx + 8, level, wall_top + 20), p["stone"])
        paint(image, box(cx, base, tx + 6, tx + 8, level, wall_top + 20), p["rim"], 0.7)
        roof = cone(cx, base, tx, wall_top + 20, 18, 11)
        paint(image, roof, p["roof"])
        paint(image, roof & (XX > cx + tx), p["roof_rim"], 0.6)
        paint(image, box(cx, base, tx - 1, tx + 1, wall_top + 8, wall_top + 12), p["window"])
    tower_top = wall_top + 64
    tower = box(cx, base, -13, 13, wall_top, tower_top)
    paint(image, tower, p["stone"])
    paint(image, tower & (XX >= cx + 10), p["rim"], 0.65)
    roof = cone(cx, base, 0, tower_top, 24, 16)
    paint(image, roof, p["roof"])
    paint(image, roof & (XX > cx), p["roof_rim"], 0.6)
    for k, y in enumerate((wall_top + 10, wall_top + 28, wall_top + 46)):
        rise = (base - YY) - (y + (XX - cx) * 0.3)
        band = (np.abs(rise) <= 5) & (np.abs(XX - cx) <= 19 - k)
        paint(image, band, p["coil"])
        paint(image, band & (rise > 2), p["coil_light"])
        paint(image, band & (rise < -3), p["coil_dark"])
    neck_top = base - tower_top - 36
    neck = (np.abs(XX - (cx + 14 - (YY - neck_top) * 0.18)) <= 5) & (YY >= neck_top) & (YY < base - tower_top - 6)
    paint(image, neck, p["coil"])
    paint(image, neck & (XX > cx + 15 - (YY - neck_top) * 0.18), p["coil_light"])
    head = ellipse(YY, XX, cx + 7, neck_top - 4, 15, 9) | ellipse(YY, XX, cx - 10, neck_top - 1, 11, 6)
    paint(image, head, p["coil"])
    paint(image, head & (YY < neck_top - 8), p["coil_light"])
    paint(image, head & (YY > neck_top + 2), p["coil_dark"])
    paint(image, (np.abs(YY - (neck_top + 2)) < 1) & (XX > cx - 20) & (XX < cx + 4), p["coil_dark"])
    for ex in (1, 2, 3):
        image[int(neck_top - 6), int(cx + ex), :3] = p["eye"]
    for wx, wy in ((-3, wall_top + 16), (3, wall_top + 34), (-3, wall_top + 52), (-30, level + 5), (26, level + 5), (-64, 22), (54, 22), (-44, 6), (72, 6), (-16, 36), (12, 6)):
        paint(image, box(cx, base, wx, wx + 2, wy, wy + 3), p["window"])


def tree(image, x, y, r, dark, light, rim=None):
    """둥근 나무 덩이 하나(점 좌표, 밑동이 y). 위쪽 왼편이 밝고 rim이 있으면 오른쪽 가장자리에 햇빛이 든다."""
    crown = ellipse(YY, XX, x, y - r, r, r * 0.85)
    paint(image, crown, dark)
    paint(image, crown & (YY < y - r * 1.3) & (XX < x + r * 0.4), light, 0.75)
    if rim is not None:
        paint(image, crown & (XX > x + r * 0.6), rim, 0.5)


def save_layers(folder, layers):
    folder.mkdir(parents=True, exist_ok=True)
    for name, image in layers.items():
        rgba = np.clip(image, 0, 255).copy()
        rgba[..., 3] = np.clip(image[..., 3], 0, 1) * 255
        Image.fromarray(rgba.astype(np.uint8), "RGBA").save(folder / f"{name}.png")


# ── 1부: 오후의 힐리아 동부 ─────────────────────────────

PART1_SKY = ((0, "#f4dcae"), (100, "#f4dcae"), (116, "#f1d2a8"), (132, "#cfd6cf"), (156, "#9cc0d8"), (186, "#6f9fcb"), (216, "#4a78ae"))
SUNNY = color("#fff0d0")


def part1_sky():
    image, _ = sky(PART1_SKY)
    glow(image, (190, 150), 220, color("#fff3d6"), 0.45, stretch=2.2)
    return image


def part1_clouds(rng):
    """뭉게구름: 둥근 덩이 넷을 붙인 구름. 아랫면은 하늘빛 그늘. 끝과 처음이 이어져 흐른다."""
    image = blank()
    puff_shape = ((0, 0, 1.0), (1.1, -0.35, 0.8), (-1.1, 0.1, 0.72), (2.0, 0.2, 0.6), (-1.9, 0.3, 0.5))
    for row_units, count, size in ((190, 4, 9), (166, 5, 12), (146, 4, 8)):
        spacing = W / count
        for i in range(count):
            cx = i * spacing + rng.uniform(-0.3, 0.3) * spacing
            cy = dot_y(row_units) + rng.uniform(-5, 5)
            r = size * rng.uniform(0.85, 1.2)
            for wrap in (-W, 0, W):
                body = np.zeros((H, W), dtype=bool)
                for dx, dy, k in puff_shape:
                    body |= ellipse(YY, XX, cx + wrap + dx * r, cy + dy * r, k * r, k * r)
                shade = body & (YY > cy + r * 0.25)
                paint(image, body, color("#fbf6ec"))
                paint(image, shade, color("#d8dfe8"))
                paint(image, body & (YY > cy + r * 0.6), color("#bcc8d8"))
    return image


def part1_far(rng):
    image = blank()
    back = ridge(rng, 132, 12, 3, 1.5)
    paint(image, below(back), color("#9fb4c6"))
    front = ridge(rng, 120, 9, 4, 1.2)
    paint(image, below(front), color("#7f9cb0"))
    paint(image, below(front) & (YY < front[XX] + 2), SUNNY, 0.45)
    day = {
        "hill": color("#6f9a5a"), "stone": color("#9a98a4"), "stone_top": color("#c4c2cc"), "rim": color("#f3e8d4"), "shade": color("#5e5c6c"),
        "roof": color("#3f8a6a"), "roof_rim": color("#7cc09a"), "coil": color("#8a8590"), "coil_light": color("#bcb6bc"), "coil_dark": color("#5e5966"),
        "window": color("#3a3848"), "eye": color("#e05040"),
    }
    snake_castle(image, -150, 112, day)
    return image


def river_path():
    """강의 가운데 줄(점 행, 열마다)과 반폭(점): 뱀성 아래에서 나와 굽이치며 오른쪽 앞으로 흘러오며 넓어진다."""
    x = np.arange(W)
    units = x / 2 - UNITS_W / 2
    return dot_y(106 - (units + 300) * 0.022 + 3 * np.sin(units / 34.0)), 2 + (units + 300) * 0.008


def part1_valley(rng):
    """뱀열차 위로 보이는 골짜기: 굽이치는 강과 절벽 다리, 오른쪽 언덕의 마을, 양쪽 숲."""
    image = blank()
    back = ridge(rng, 114, 4, 2, 0.8, sharp=False)
    paint(image, below(back), color("#8ab062"))
    paint(image, below(back) & (YY < back[XX] + 2), SUNNY, 0.5)
    mid = ridge(rng, 100, 4, 3, 0.8, sharp=False)
    paint(image, below(mid), color("#76a052"))
    paint(image, below(mid) & (YY < mid[XX] + 2), color("#a8c878"))
    shade = pg.noise(H, W, 6, rng)
    paint(image, below(back) & (shade > 0.65), color("#6a9248"), 0.6)
    # 강: 뱀성 쪽에서 굽이쳐 흐르고, 물가에 모래톱과 반짝임
    center, half = river_path()
    water = (np.abs(YY - center[XX]) <= half[XX]) & below(back)
    bank = (np.abs(YY - center[XX]) <= half[XX] + 2) & below(back) & ~water
    paint(image, bank, color("#c9b88a"))
    paint(image, water, color("#5b9cc4"))
    paint(image, water & (YY < center[XX] - half[XX] * 0.3), color("#7ab6d8"))
    sparkle = water & (pg.noise(H, W, 2, rng) > 0.8)
    paint(image, sparkle, color("#d8f0f8"))
    # 절벽 다리: 강이 바위 협곡 사이를 지나는 자리. 양쪽 바위 비탈 위에 둥근 아치 돌다리가 걸려 있다
    gx = dot_x(-70)
    gy = center[int(gx)]
    for side in (-1, 1):
        edge = gx + side * 12
        slope = (side * (XX - edge) >= 0) & (side * (XX - edge) <= 26) & (YY >= gy - 16 + np.abs(XX - edge) * 0.0) & (YY < gy + 8)
        cliff = slope & (YY >= gy - 16 + np.clip(8 - side * (XX - edge), 0, 8))
        paint(image, cliff, color("#8a7e6c"))
        paint(image, cliff & (((YY - gy) // 4) % 2 == 0), color("#9a8e7a"))
        paint(image, cliff & (side * (XX - edge) < 3), color("#6a604f"))
        paint(image, cliff & (YY < gy - 13), color("#7aa052"))
    paint(image, (np.abs(XX - gx) <= 11) & (YY >= gy - 8) & (YY < gy + 8), color("#4a7aa0"))
    body = (np.abs(XX - gx) <= 34) & (YY >= gy - 20) & (YY < gy - 9) & ~ellipse(YY, XX, gx, gy - 6, 20, 11)
    paint(image, body, color("#b8ac94"))
    paint(image, body & (((XX - gx) // 5) % 2 == 0) & (YY > gy - 17), color("#a89c84"))
    paint(image, (np.abs(XX - gx) <= 35) & (YY >= gy - 21) & (YY < gy - 19), color("#e0d4b8"))
    paint(image, (np.abs(XX - gx) <= 34) & (YY >= gy - 24) & (YY < gy - 21) & (((XX - gx) % 6) == 0), color("#c8bca0"))
    paint(image, ellipse(YY, XX, gx, gy - 6, 20, 11) & ~ellipse(YY, XX, gx, gy - 5, 18, 10) & (YY < gy - 6), color("#8a7e66"))
    # 숲: 왼쪽 강가와 오른쪽 마을 뒤
    for _ in range(30):
        tx = dot_x(rng.uniform(-300, -110)) if rng.uniform() < 0.45 else dot_x(rng.uniform(150, 300))
        ty = mid[int(np.clip(tx, 0, W - 1))] + rng.uniform(2, 20)
        tree(image, tx, ty, rng.uniform(6, 11), color("#3f6e3a"), color("#6a9a4a"), SUNNY)
    # 마을: 오른쪽 언덕 위 하얀 벽, 주황 지붕, 굴뚝 연기
    for i, hx_units in enumerate((40, 58, 76, 96, 112, 130, 70, 92)):
        hx = dot_x(hx_units)
        hy = mid[int(hx)] + (12 if i < 6 else 0) + (i % 3) * 3
        w = rng.uniform(9, 13)
        walls = (np.abs(XX - hx) <= w) & (YY >= hy - 12) & (YY < hy)
        paint(image, walls, color("#efe4cc"))
        paint(image, walls & (XX > hx + w - 3), color("#cfc0a4"))
        roof = (YY >= hy - 21) & (YY < hy - 12) & (np.abs(XX - hx) <= (YY - (hy - 21)) * 1.45 + 2)
        paint(image, roof, color(["#c8603c", "#b85440", "#d47a44"][i % 3]))
        paint(image, roof & (XX > hx), color("#9c4430"), 0.5)
        paint(image, (np.abs(XX - (hx - 3)) <= 1) & (YY >= hy - 8) & (YY < hy - 4), color("#5a6a7a"))
        if i % 3 == 0:
            paint(image, (np.abs(XX - (hx + 5)) <= 1) & (YY >= hy - 25) & (YY < hy - 18), color("#8a6a58"))
            for k in range(3):
                paint(image, ellipse(YY, XX, hx + 6 + k * 3, hy - 28 - k * 4, 2 + k, 2 + k * 0.6), color("#f4f0ea"), 0.6 - k * 0.15)
    return image


def part1_fields(rng):
    """앞 들판의 밭 몇 조각과, 동쪽 정류장으로 이어지는 모래 길(뱀열차가 지나는 S자 홈)."""
    image = blank()
    top = ridge(rng, 90, 2, 2, 0.5, sharp=False)
    paint(image, below(top), color("#6e9a4c"))
    paint(image, below(top) & (YY < top[XX] + 2), color("#a6c674"))
    lean = XX + (XX - W / 2) * 0.15
    plots = ((lean // 70) % 4)
    crop = [color("#9cae5a"), color("#c4b46a"), color("#7aa04e"), color("#b0a466")]
    field = below(top) & (YY < dot_y(PART1_GROUND + 7)) & (YY > top[XX] + 3)
    for k in range(4):
        paint(image, field & (plots == k) & ((XX // 140) % 2 == 0), crop[k])
    paint(image, field & ((lean.astype(int) % 3) == 0) & ((XX // 140) % 2 == 0), color("#7a8a46"), 0.4)
    bottom = track(image, rng, PART1_GROUND, color("#d2bf8e"), color("#e4d4a8"), color("#b4a072"), color("#7a9a52"))
    meadow = YY >= bottom
    paint(image, meadow, color("#5e8c40"))
    paint(image, meadow & (pg.noise(H, W, 3, rng) > 0.6), color("#6e9c48"))
    for gx in rng.uniform(0, W, 220):
        gy = rng.uniform(bottom + 3, dot_y(16))
        paint(image, (np.abs(XX - gx) < 1) & (YY >= gy - rng.integers(2, 5)) & (YY < gy), color("#86b056"))
    return image


def part1_ground(rng):
    """앞쪽: 왼쪽 풀 언덕과 덤불, 오른쪽 정류장 승강장(나무 판, 기둥, 등, 역명판). 메뉴가 놓이는 왼쪽 아래는 그늘로 어둡다."""
    image = blank()
    x = np.arange(W)
    line = dot_y(20) - 4 * np.sin(x / 80.0) - 3 * np.sin(x / 31.0 + 1.0)
    paint(image, below(line), color("#3c6a34"))
    depth = (YY - line[XX]).astype(float)
    paint(image, stepped(np.clip(1 - depth / 60, 0, 1), 4) * (depth >= 0), color("#4e7c3c"), 0.9)
    paint(image, (depth >= 0) & (depth < 2), color("#9cc464"))
    paint(image, (depth > 8) & (pg.noise(H, W, 3, rng) > 0.66), color("#356030"))
    shadow = np.clip(1 - (UNITS_X + 300) / 420, 0, 1) * np.clip(depth / 30, 0, 1)
    paint(image, stepped(shadow, 4) * (depth >= 0), color("#1e3a24"), 0.7)
    for gx in rng.uniform(0, W, 200):
        gy = line[int(gx)] + rng.uniform(-1, 26)
        tuft = (np.abs(XX - gx) < 1) & (YY >= gy - rng.integers(3, 7)) & (YY < gy)
        paint(image, tuft, color("#5e9040") if rng.uniform() < 0.6 else color("#2e5a2c"))
    for bx_units in (-280, -236, -170):
        bx = dot_x(bx_units)
        tree(image, bx, line[int(bx)] + 10, rng.uniform(9, 13), color("#2f5a2e"), color("#4e8040"))
    for fx in rng.uniform(0, W * 0.6, 26):
        fy = int(line[int(fx)] + rng.uniform(3, 30))
        image[fy, int(fx), :3] = [color("#f0b0b8"), color("#f8e080"), color("#ffffff")][rng.integers(0, 3)]
        image[fy, int(fx), 3] = 1
    # 승강장: 나무 판을 깐 단, 앞면 판자와 기둥
    left, top = dot_x(PLATFORM_LEFT), dot_y(PLATFORM_TOP)
    deck = (XX >= left) & (YY >= top) & (YY < top + 6)
    paint(image, deck, color("#a8845a"))
    paint(image, deck & (YY < top + 2), color("#c8a474"))
    paint(image, deck & ((XX - left) % 14 == 0), color("#7a5a3c"))
    face = (XX >= left) & (YY >= top + 6) & (YY < top + 22)
    paint(image, face, color("#6e5036"))
    paint(image, face & (((YY - top) % 5) == 0), color("#5a4028"))
    for px in range(int(left) + 6, W, 40):
        paint(image, (np.abs(XX - px) <= 2) & (YY >= top + 6) & (YY < top + 30), color("#4a3422"))
    paint(image, (XX >= left) & (XX < left + 3) & (YY >= top) & (YY < top + 22), color("#4a3422"))
    # 등: 승강장 왼쪽 끝 기둥 위의 등불
    lx = left + 4
    paint(image, (np.abs(XX - lx) <= 1) & (YY >= top - 44) & (YY < top), color("#3a2a20"))
    paint(image, box(lx, top - 44, -4, 5, 0, 8), color("#3a2a20"))
    paint(image, box(lx, top - 45, -3, 4, 1, 7), color("#ffe0a0"))
    glow(image, (PLATFORM_LEFT + 2, PLATFORM_TOP + 25), 18, color("#ffe8b0"), 0.25, stretch=1.0, levels=3)
    # 역명판: 두 기둥에 건 나무 판(글자는 점 몇 줄로 줄였다). 등과 디엘 사이
    sx = dot_x(PLATFORM_LEFT + 18)
    for post in (-14, 14):
        paint(image, (np.abs(XX - (sx + post)) <= 1) & (YY >= top - 34) & (YY < top), color("#4a3422"))
    sign = box(sx, top - 24, -18, 18, 0, 14)
    paint(image, sign, color("#e8dcc0"))
    paint(image, sign & ((YY == top - 24 - 13) | (YY == top - 24 - 1)), color("#6e5036"))
    paint(image, box(sx, top - 24, -11, 11, 6, 8) & ((XX // 3) % 2 == 0), color("#3a3040"))
    return image


# ── 2부: 밤의 힐리아 서부 사막 ───────────────────────────

PART2_SKY = ((0, "#3a3060"), (96, "#3a3060"), (112, "#4e3a66"), (128, "#3a3462"), (160, "#232a56"), (190, "#161d44"), (216, "#0b1230"))
MOONLIT = color("#c8c4e8")


def part2_sky():
    image, _ = sky(PART2_SKY)
    glow(image, MOON, 120, color("#a8a8d8"), 0.3, stretch=1.0, levels=6)
    mx, my = dot_x(MOON[0]), dot_y(MOON[1])
    moon = ellipse(YY, XX, mx, my, 26, 26)
    paint(image, moon, color("#f4f0dc"))
    paint(image, moon & ellipse(YY, XX, mx + 8, my + 6, 26, 26) & ~ellipse(YY, XX, mx - 2, my - 2, 26, 26), color("#d8d4c4"))
    for cx, cy, r in ((-8, -6, 5), (9, 4, 4), (-3, 10, 3), (12, -10, 3)):
        paint(image, ellipse(YY, XX, mx + cx, my + cy, r, r), color("#dcd6c4"))
    return image


def part2_far(rng):
    """왼쪽 드래곤 고원의 뷰트 셋(층층이 쌓인 사암 절벽과 그 아래 퍼진 돌 비탈), 오른쪽 지평선의 거대고목(가지마다 반딧불 같은 빛).
    달이 오른쪽 위에 있으므로 오른쪽 면과 윗가장자리가 밝다."""
    image = blank()
    hazy = ridge(rng, 104, 4, 3, 0.8, sharp=False)
    paint(image, below(hazy), color("#4a3e6e"))
    rough = pg.noise(H, W, 3, rng)
    for x_units, half_top, half_base, height in ((-230, 40, 70, 46), (-158, 26, 48, 34), (-282, 22, 40, 28)):
        cx, base = dot_x(x_units), dot_y(100)
        top = base - height * 2
        rows = np.clip((YY - top) / max(base - top, 1), 0, 1)
        # 위 45%는 거의 곧은 절벽, 그 아래는 무너진 돌이 쌓여 넓게 퍼지는 비탈(애추)이다.
        cliff = rows < 0.45
        flare = np.where(cliff, rows / 0.45 * 0.1, 0.1 + (np.clip(rows - 0.45, 0, None) / 0.55) ** 1.5 * 0.9)
        half = (half_top + (half_base - half_top) * flare) * 2 + (rough - 0.5) * 4
        mesa = (YY >= top) & (YY < base + 4) & (np.abs(XX - cx) <= half)
        side = (XX - cx) / np.maximum(half, 1)
        paint(image, mesa, color("#4a3a62"))
        strata = (((YY - top) + np.sin(XX / 17.0) * 1.5).astype(int) // 6) % 2 == 0
        paint(image, mesa & cliff & strata, color("#56446e"))
        paint(image, mesa & ~cliff & (rough > 0.62), color("#56466e"))
        paint(image, mesa & (side > 0.45), color("#6e5c8c"), 0.6)
        paint(image, mesa & (side < -0.55), color("#36294c"), 0.8)
        paint(image, mesa & (YY < top + 3), MOONLIT, 0.6)
    # 거대고목: 아래로 갈수록 뿌리가 넓게 퍼지는 굵은 줄기, 양옆으로 뻗은 큰 가지, 겹겹이 퍼진 넓은 가지 덩이.
    # 달빛이 오른쪽 위 가장자리에 맺히고, 덩이 사이사이에 요정의 빛이 드문드문 켜져 있다.
    tx, base = dot_x(110), dot_y(98)
    rise = (base - YY).astype(float)
    flare = 10 + np.clip(14 - rise, 0, 14) ** 1.3
    trunk = (np.abs(XX - tx - rise * 0.05) <= flare) & (YY >= base - 70) & (YY < base + 4)
    paint(image, trunk, color("#2a2440"))
    paint(image, trunk & (XX > tx + 2 + rise * 0.05), color("#3a3456"))
    for side in (-1, 1):
        branch = (np.abs((base - 60 - YY) - side * (XX - tx) * 0.45) <= 3) & (side * (XX - tx) > 4) & (side * (XX - tx) < 44)
        paint(image, branch, color("#2a2440"))
    lobes = ((0, -100, 64, 24), (-60, -82, 40, 17), (62, -80, 42, 17), (-34, -114, 42, 18), (36, -112, 44, 18), (0, -126, 42, 16), (-88, -70, 22, 11), (90, -68, 24, 11))
    canopy = np.zeros((H, W), dtype=bool)
    lit = np.zeros((H, W), dtype=bool)
    for dx, dy, rx, ry in lobes:
        lobe = ellipse(YY, XX, tx + dx, base + dy, rx, ry)
        canopy |= lobe
        lit |= lobe & ~ellipse(YY, XX, tx + dx - rx * 0.18, base + dy + ry * 0.3, rx, ry)
    paint(image, canopy, color("#1c3540"))
    paint(image, canopy & (pg.noise(H, W, 6, rng) > 0.58), color("#22404a"))
    paint(image, lit, color("#4a7a80"), 0.75)
    lights = canopy & (pg.noise(H, W, 2, rng) > 0.93)
    paint(image, lights, color("#a8f8d0"))
    glow(image, (110, 150), 80, color("#7ad8b0"), 0.14, stretch=1.4, levels=4)
    return image


def dune_layer(image, rng, base, height, count, lit, shade, edge):
    """사구 한 겹. 사구마다 왼쪽은 가파른 미끄럼면, 오른쪽은 달을 향해 길게 내려가는 비탈이다.
    빛과 그늘은 등성이 꼭대기에서 왼쪽 아래로 휘어 내려가는 줄에서 갈린다(초승달 사구를 옆에서 본 모습).
    등성이 바로 아래가 가장 밝고, 밝은 비탈에는 바람이 그린 모래 결이 겉면을 따라 흐른다."""
    units = np.arange(W) / 2 - UNITS_W / 2
    step = (UNITS_W + 120) / count
    crests = [-UNITS_W / 2 - 40 + step * (k + rng.uniform(0.2, 0.8)) for k in range(count)]
    shapes = []
    for c in crests:
        h = height * rng.uniform(0.65, 1.0)
        steep, gentle = rng.uniform(16, 26), rng.uniform(70, 120)
        rise = np.clip((units - (c - steep)) / steep, 0, 1)
        fall = np.clip((units - c) / gentle, 0, 1)
        profile = np.where(units <= c, h * rise ** 0.9, h * np.cos(fall * np.pi / 2) ** 1.4)
        profile[(units < c - steep) | (units > c + gentle)] = 0
        shapes.append((c, h, steep, profile))
    profiles = np.stack([p for _, _, _, p in shapes])
    top = profiles.max(axis=0)
    owner = profiles.argmax(axis=0)
    line = dot_y(base + top + 0.6 * np.sin(units / 23.0))
    region = below(line)
    depth = (YY - line[XX]).astype(float)

    # 그늘: 기둥마다 그 기둥을 차지한 사구의 등성이에서 내려오는 갈림줄 왼쪽.
    crest_x = np.array([dot_x(c) for c, _, _, _ in shapes])[owner]
    crest_y = np.array([dot_y(base + h) for _, h, _, _ in shapes])[owner]
    reach = np.array([steep * 2 * 0.75 for _, _, steep, _ in shapes])[owner]
    tall = np.array([h * 2 for _, h, _, _ in shapes])[owner]
    fall_rows = np.clip((YY - crest_y[XX]) / tall[XX], 0, 1)
    split = crest_x[XX] - reach[XX] * fall_rows ** 0.8
    has_dune = (top > 0.5)[XX]
    shadow = region & has_dune & (XX < split) & (YY < dot_y(base) + 2)
    sunny = region & ~shadow

    paint(image, region, color(lit))
    paint(image, sunny & (depth < 5), edge, 0.55)
    paint(image, sunny & (depth >= 5) & (depth < 12), edge, 0.25)
    ripple = sunny & (depth > 4) & ((((YY - line[XX]) + np.sin(XX / 11.0) * 1.5).astype(int) % 8) == 0)
    paint(image, ripple, color(shade), 0.3)
    paint(image, shadow, color(shade))
    paint(image, shadow & (depth < 3), color(lit), 0.35)
    paint(image, sunny & (depth < 1.5), MOONLIT, 0.85)


def part2_dunes(rng):
    """달빛 사구 겹 넷. 먼 겹일수록 하늘빛에 묻혀 흐리고, 가까운 겹일수록 빛과 그늘이 또렷하다."""
    image = blank()
    dune_layer(image, rng, 94, 12, 6, "#5e5888", "#443e6c", color("#7e78aa"))
    dune_layer(image, rng, 82, 15, 5, "#6c649a", "#433c72", color("#948cc0"))
    dune_layer(image, rng, 70, 14, 5, "#7a72a8", "#3e3870", color("#aaa2d4"))
    dune_layer(image, rng, 56, 12, 4, "#6e66a0", "#342e64", color("#9c94c8"))
    return image


def part2_ground(rng):
    """앞 사구: 오른쪽에 넓고 판판한 등성이가 솟아 여섯이 서고, 왼쪽은 낮아 메뉴가 놓인다.
    등성이 어깨는 둥글게 내려가고, 달을 등진 왼쪽 어깨는 그늘이다. 왼쪽 아래에서 등성이로 오르는 발자국, 선인장과 돌, 바람이 그린 모래 결."""
    image = blank()
    x = np.arange(W)
    units = x / 2 - UNITS_W / 2
    # 등성이 위(여섯이 선 24~176)는 판판하고, 그 밖은 매끈한 계단 함수(smoothstep)로 낮은 땅(24)까지 내려간다.
    low, flat_left, flat_right = 24, 22, 178

    def shoulder(t):
        t = np.clip(t, 0, 1)
        return t * t * (3 - 2 * t)

    rise = shoulder((units - (flat_left - 70)) / 70) * shoulder(((flat_right + 50) - units) / 50)
    height = low + (RIDGE_TOP - low) * rise + (1 - rise) * 1.5 * np.sin(x / 70.0)
    line = dot_y(height)
    region = below(line)
    depth = (YY - line[XX]).astype(float)
    paint(image, region, color("#2a2550"))
    paint(image, stepped(np.clip(1 - depth / 50, 0, 1), 4) * region, color("#3a3468"), 0.8)
    # 기울기(기둥마다 오르는 정도)로 어깨를 칠한다: 오른쪽으로 오르는 왼쪽 어깨는 달을 등져 어둡고, 내려가는 오른쪽 어깨는 밝다.
    slope = np.gradient(height)
    away = stepped(np.clip(slope / slope.max(), 0, 1)[XX], 3)
    toward = stepped(np.clip(-slope / -slope.min(), 0, 1)[XX], 3)
    fade = np.clip(1 - depth / 26, 0, 1)
    paint(image, away * fade * region, color("#1c1840"), 0.75)
    paint(image, toward * fade * region, color("#4a4482"), 0.6)
    ripples = region & (depth > 6) & (((YY + np.sin(XX / 25.0) * 4).astype(int) % 7) == 0)
    paint(image, ripples, color("#3e3870"), 0.7)
    paint(image, region & (depth < 2), MOONLIT, 0.85 - 0.45 * away)
    # 발자국: 왼쪽 아래에서 등성이로 올라온 일행의 자취(작은 점 둘씩, 오른발·왼발이 엇갈린다).
    for k in range(9):
        fx_units = flat_left - 68 + k * 8.5
        fx = dot_x(fx_units)
        fy = line[int(fx)] + 6 + (k % 2) * 3
        paint(image, ellipse(YY, XX, fx, fy, 2.5, 1.4), color("#16122e"), 0.9)
    for cx_units in (-262, -212, 268):
        cx = dot_x(cx_units)
        cy = line[int(np.clip(cx, 0, W - 1))] + 8
        stem = (np.abs(XX - cx) <= 2) & (YY >= cy - 20) & (YY < cy)
        arm_l = (np.abs(XX - (cx - 6)) <= 1.5) & (YY >= cy - 15) & (YY < cy - 8)
        arm_r = (np.abs(XX - (cx + 6)) <= 1.5) & (YY >= cy - 18) & (YY < cy - 11)
        joints = (np.abs(YY - (cy - 8)) <= 1) & (XX >= cx - 6) & (XX <= cx) | (np.abs(YY - (cy - 11)) <= 1) & (XX >= cx) & (XX <= cx + 6)
        cactus = stem | arm_l | arm_r | joints
        paint(image, cactus, color("#24384a"))
        paint(image, cactus & (XX > cx), color("#3e5a6a"))
    for rx_units in (-180, 236):
        rx = dot_x(rx_units)
        ry = line[int(rx)] + rng.uniform(8, 20)
        rock = ellipse(YY, XX, rx, ry, rng.uniform(6, 10), rng.uniform(3, 5)) & (YY < ry + 2)
        paint(image, rock, color("#2e2a48"))
        paint(image, rock & (YY < ry - 2), color("#5a5480"))
    return image


# ── 미리보기 ───────────────────────────────────────────

def layer(folder, name):
    """저장한 겹을 미리보기 크기(가상 px 하나 = 4점)로 키운다."""
    image = Image.open(folder / f"{name}.png").convert("RGBA")
    return image.resize((image.width * 2, image.height * 2), Image.NEAREST)


def place_train(frame, art, width_units, head_x, ground):
    head = Image.open(art / "PixelGen/SnakeTrain/head.png").convert("RGBA")
    body = Image.open(art / "PixelGen/SnakeTrain/body.png").convert("RGBA")
    ground_row = (UNITS_H - ground) * 4

    def groove(x):
        return np.sin(x * 2 * np.pi / GROOVE_LENGTH) * GROOVE_DEPTH

    for d in range(0, body.width // 4, 2):
        x = head_x + 14 + d
        strip = body.crop((d * 4, 0, d * 4 + 8, body.height))
        frame.alpha_composite(strip, (int((x + width_units / 2) * 4), int(ground_row + round(groove(x)) * 4 - (body.height - 8))))
    hx = int((head_x + width_units / 2) * 4 - head.width * 0.5769)
    frame.alpha_composite(head, (hx, int(ground_row + round(groove(head_x + 16)) * 4 - (head.height - 8))))


def place_people(frame, art, width_units, people, feet_units):
    feet = (UNITS_H - feet_units) * 4
    for name, x in people:
        picture = Image.open(art / f"WebGame/Characters/{name}/up_0.png").convert("RGBA")
        frame.alpha_composite(picture, (int((x + width_units / 2) * 4 - picture.width / 2), feet - picture.height + 4))


def preview(project, folder, width_units, back, front, ground=None, head_x=None, people=(), feet_units=0, extra=None):
    """겹을 화면 한 장으로 겹친다. 뱀열차(ground·head_x)와 인물은 있을 때만 얹는다."""
    art = project / "Assets/MiniPandoras/Art"
    left = int((UNITS_W - width_units) / 2 * 4)
    frame = Image.new("RGBA", (width_units * 4, UNITS_H * 4))
    for name in back:
        frame.alpha_composite(layer(folder, name).crop((left, 0, left + width_units * 4, UNITS_H * 4)))
    if extra is not None:
        extra(frame, width_units)
    if head_x is not None:
        place_train(frame, art, width_units, head_x, ground)
    for name in front:
        frame.alpha_composite(layer(folder, name).crop((left, 0, left + width_units * 4, UNITS_H * 4)))
    place_people(frame, art, width_units, people, feet_units)
    return frame


def sheet(shots, path):
    out = Image.new("RGBA", (shots[1].width, shots[0].height + shots[1].height + 16), (30, 30, 34, 255))
    out.alpha_composite(shots[0], (0, 0))
    out.alpha_composite(shots[1], (0, shots[0].height + 16))
    out.convert("RGB").save(path)


def main(project):
    rng = np.random.default_rng(20261109)
    title = project / "Assets/MiniPandoras/Art/PixelGen/UI/Title"
    docs = project / "Docs/ArtUpgrade/PixelGen"

    part1 = title / "Part1"
    save_layers(part1, {
        "sky": part1_sky(),
        "clouds": part1_clouds(rng),
        "far": part1_far(rng),
        "valley": part1_valley(rng),
        "fields": part1_fields(rng),
        "ground": part1_ground(rng),
    })
    shots = [preview(project, part1, w, ("sky", "clouds", "far", "valley", "fields"), ("ground",), PART1_GROUND, PART1_STOP_X, PART1_PEOPLE, PLATFORM_TOP) for w in (384, 480)]
    sheet(shots, docs / "title_part1_preview.png")
    print("Part1 done")

    part2 = title / "Part2"
    save_layers(part2, {
        "sky": part2_sky(),
        "stars_a": stars(11, 220, lambda i: i % 2 == 0, 110),
        "stars_b": stars(11, 220, lambda i: i % 2 == 1, 110),
        "far": part2_far(rng),
        "dunes": part2_dunes(rng),
        "ground": part2_ground(rng),
    })
    def narion(frame, width_units):
        bird = Image.open(part2 / "narion_0.png")
        bird = bird.resize((bird.width * 2, bird.height * 2), Image.NEAREST)
        cx, cy = NARION_AT
        frame.alpha_composite(bird, (int((cx + width_units / 2) * 4 - bird.width / 2), int((UNITS_H - cy) * 4 - bird.height / 2)))

    shots = [preview(project, part2, w, ("sky", "stars_a", "far", "dunes"), ("ground",), extra=narion) for w in (384, 480)]
    sheet(shots, docs / "title_part2_preview.png")
    print("Part2 done")


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
