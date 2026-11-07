"""아지트 뒤 숲(forest)에 6장의 뒤틀린 모습을 더한다(매듭을 풀면 뒤틀린 공간이 정말 맞춰지도록).

웹게임 숲을 다시 그린 맵이라 바닥·나무·충돌·지점·포탈은 그대로 두고 모습 셋을 얹는다. 모습마다 다른 것은 바닥 위에 까는 그림과 떠 있는 땅 조각뿐이고
충돌은 셋이 같다(매듭 자리와 접히는 길은 그대로 걸어 다닌다). 모습 그림은 layout.py 모습 기능을 쓰고, 맵 들여오기가 묶음을 나눈다.

  normal     매듭을 다 풀었거나 6장 밖. 지금까지의 숲 그대로다.
  twisted    매듭 둘이 남았다. 땅이 칸 덩어리로 어긋나 길이 끊겨 보이고, 어긋난 틈이 보랏빛으로 갈라지며, 땅 조각이 떠 있다.
  loosened   매듭 하나가 남았다. 같은 덩어리가 절반만 어긋나고 금이 옅으며, 떠 있는 조각이 줄고 낮아졌다.

사용: python Tools/PixelGen/forest_twist.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/forest/map.json, twist_*.png, ...    맵 데이터와 그림
  Docs/ArtUpgrade/PixelGen/forest_preview.png, forest_twisted_preview.png, forest_loosened_preview.png
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas, outline
from layout import SUB, MapLayout
from web_map import WEB_TREES, read

T = pg.TILE
MAP_ID = "forest"
WALK_CHECK = ((3, 10), (26, 9), (22, 5), (22, 6), (22, 7))      # 매듭 두 곳과 접히는 길(Chapter6): 세 모습 모두 걸을 수 있어야 한다
MIN_SIDE, MAX_SIDE = 3, 6       # 땅 덩어리 한 변(칸)
SHIFT = 26                      # 덩어리가 어긋나는 가장 큰 거리(점)
EDGE_WOBBLE = 3                 # 덩어리 경계가 칸 선에서 흔들리는 폭(점)
LIFT_SHARE = 0.3                # 떠오른 덩어리 비율
LIFT = 10                       # 떠오른 덩어리 앞면 높이(점)
RIFT = pg.hexes("#07040f", "#1a0a33", "#3f1a7a", "#7a3fd8", "#c49aff", "#f2e4ff")
EARTH = pg.DIRT
# (가운데 칸 x, 발밑 칸 y, 너비 점, 뜬 높이 점). 길과 지점을 덮지 않는 숲 틈에 띄운다.
TWISTED_PIECES = ((12.6, 5.6, 84, 40), (17.8, 3.4, 66, 34), (6.6, 9.8, 60, 30), (24.8, 12.6, 72, 36), (14.6, 16.4, 64, 30), (20.6, 10.8, 54, 26))
LOOSENED_PIECES = ((12.6, 5.6, 66, 18), (24.8, 12.6, 54, 16), (14.6, 16.4, 48, 14))


def split(rect, rng, out):
    """네모를 한 변이 MIN_SIDE~MAX_SIDE칸인 덩어리로 자른다."""
    x0, y0, x1, y1 = rect
    w, h = x1 - x0, y1 - y0
    if w > MAX_SIDE or (w >= MIN_SIDE * 2 and rng.random() < 0.4):
        cut = int(rng.integers(x0 + MIN_SIDE, x1 - MIN_SIDE + 1))
        split((x0, y0, cut, y1), rng, out)
        split((cut, y0, x1, y1), rng, out)
    elif h > MAX_SIDE or (h >= MIN_SIDE * 2 and rng.random() < 0.4):
        cut = int(rng.integers(y0 + MIN_SIDE, y1 - MIN_SIDE + 1))
        split((x0, y0, x1, cut), rng, out)
        split((x0, cut, x1, y1), rng, out)
    else:
        out.append(rect)


def chunk_ids(width, height, rects, rng):
    """점마다 어느 덩어리인지. 경계가 칸 선을 따르되 조금 흔들려 자로 그은 금이 되지 않는다."""
    h, w = height * T, width * T
    labels = np.zeros((height, width), dtype=int)
    for index, (x0, y0, x1, y1) in enumerate(rects):
        labels[y0:y1, x0:x1] = index
    yy, xx = np.mgrid[0:h, 0:w]
    bend_x = ((pg.fractal_noise(h, w, rng, ((26, 0.6), (9, 0.4))) - 0.5) * 2 * EDGE_WOBBLE * 2).astype(int)
    bend_y = ((pg.fractal_noise(h, w, rng, ((26, 0.6), (9, 0.4))) - 0.5) * 2 * EDGE_WOBBLE * 2).astype(int)
    return labels[np.clip(yy + bend_y, 0, h - 1) // T, np.clip(xx + bend_x, 0, w - 1) // T]


def near_edge(ids, reach):
    """다른 덩어리와 reach점 안에 닿은 점."""
    edge = np.zeros(ids.shape, dtype=bool)
    edge[:-1, :] |= ids[:-1, :] != ids[1:, :]
    edge[1:, :] |= ids[:-1, :] != ids[1:, :]
    edge[:, :-1] |= ids[:, :-1] != ids[:, 1:]
    edge[:, 1:] |= ids[:, :-1] != ids[:, 1:]
    for _ in range(reach - 1):
        edge = edge | pg.neighbours(edge)
    return edge


def twisted_ground(ground, ids, shifts, lifted, amount, rng):
    """
    덩어리마다 바닥을 밀어 어긋난 땅 그림(점, RGBA)을 만든다. amount는 어긋남의 크기(1이면 다 어긋났다, 0.5면 절반).
    덩어리 안에서 바닥만 밀리고 덩어리 자리는 그대로라 맵 가장자리에 빈틈이 생기지 않는다.
    """
    h, w = ground.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    dx, dy = shifts[ids, 0] * amount, shifts[ids, 1] * amount
    out = ground[np.clip(yy - dy.astype(int), 0, h - 1), np.clip(xx - dx.astype(int), 0, w - 1)].copy()

    # 떠오른 덩어리: 아래 경계 밖으로 흙벽이 드러나고 그 아래에 그늘이 진다.
    lift = int(LIFT * amount)
    rising = lifted[ids]
    below = np.roll(rising, lift, axis=0) & ~rising
    below[:lift, :] = False
    out[below] = EARTH[2]
    out[below & (pg.noise(h, w, 4, rng) > 0.55)] = EARTH[1]
    out[np.roll(rising, 2, axis=0) & ~rising] = EARTH[4]
    shade = np.roll(rising, lift + 5, axis=0) & ~rising & ~below
    shade[:lift + 5, :] = False
    out[shade] = (out[shade] * 0.62).astype(np.uint8)

    # 어긋난 틈: 가운데는 깊은 어둠, 둘레로 보랏빛이 번진다. 절반만 풀리면 틈이 좁아지고 빛이 바닥에 섞여 옅다.
    wide, core = (5, 3) if amount >= 1 else (3, 1)
    outer, inner = near_edge(ids, wide), near_edge(ids, core)
    grain = pg.noise(h, w, 3, rng)
    if amount >= 1:
        out[outer] = RIFT[3]
        out[outer & (grain > 0.6)] = RIFT[4]
        out[inner] = RIFT[1]
        out[inner & (grain < 0.32)] = RIFT[0]
        spark = inner & (rng.random((h, w)) < 0.05)
        out[spark] = RIFT[5]
    else:
        out[outer] = (out[outer] * 0.6 + RIFT[3] * 0.4).astype(np.uint8)
        out[inner] = (out[inner] * 0.35 + RIFT[2] * 0.65).astype(np.uint8)
    return np.dstack([out, np.full((h, w), 255, np.uint8)])


def floating_piece(seed, width, hover):
    """
    떠 있는 땅 조각: 풀 덮인 윗면, 아래로 뾰족해지는 흙덩이, 보랏빛 금, 그 아래 땅에 진 그늘.
    그림 아래 끝이 땅에 닿는 자리다(그늘까지 그림에 넣어 기준점이 그림 안에 있다).
    """
    rng = np.random.default_rng(seed)
    top_h, depth = int(width * 0.34), int(width * 0.62)
    shadow_h = max(8, width // 4)
    w, h = width + 8, top_h + depth + hover + shadow_h
    c = Canvas(w, h)
    cx, top_y = w / 2, 2
    # 흙덩이: 윗면 아래로 들쭉날쭉 좁아진다
    edge = top_y + top_h * 0.55
    body = np.zeros(c.xx.shape, dtype=bool)
    tip_y = edge + depth
    bumps = rng.uniform(0.75, 1.1, width // 5 + 2)
    for x in range(w):
        t = abs(x + 0.5 - cx) / (width / 2)
        if t < 1:
            bottom = edge + (depth * (1 - t) ** 0.8) * bumps[int(x / 5) % len(bumps)]
            body[int(edge):int(min(tip_y, bottom)), x] = True
    c.paint(body, EARTH[2])
    c.paint(body & (c.xx < cx - width * 0.12), EARTH[3])
    c.paint(body & (c.xx > cx + width * 0.2), EARTH[1])
    c.paint(body & (pg.noise(h, w, 5, rng) > 0.62), EARTH[1])
    # 풀 덮인 윗면
    turf = c.ellipse(cx, top_y + top_h / 2, width / 2, top_h / 2)
    c.paint(turf, pg.GRASS[3])
    c.paint(turf & (c.yy < top_y + top_h * 0.4), pg.GRASS[4])
    c.paint(turf & (pg.noise(h, w, 3, rng) > 0.66), pg.GRASS[5])
    c.paint(turf & (c.yy > top_y + top_h * 0.72), pg.GRASS[2])
    # 보랏빛 금
    x, y = cx - width * 0.1, edge + 3
    for _ in range(int(depth * 0.7)):
        x += rng.integers(-1, 2)
        y += 1
        if body[int(y), int(x)]:
            c.paint(c.rect(int(x), int(y), int(x) + 2, int(y) + 1), RIFT[4])
    image = outline(c.image)
    # 땅에 진 그늘: 옅은 타원을 점무늬로
    yy, xx = c.yy, c.xx
    shadow = ((xx + 0.5 - cx) / (width * 0.42)) ** 2 + ((yy + 0.5 - (h - shadow_h / 2 - 1)) / (shadow_h / 2)) ** 2 <= 1
    shadow &= pg.bayer(h, w) < 0.7
    image[shadow & (image[..., 3] == 0)] = (24, 14, 44, 150)
    return image, (w // 2, h - 1)


def build(project):
    art = project / "Assets/MiniPandoras/Art"
    folder = art / f"PixelGen/Maps/{MAP_ID}"
    data = read(art / f"WebGame/Maps/{MAP_ID}/map.json")
    width, height = data["width"], data["height"]
    m = MapLayout(MAP_ID, data["name"], width, height, theme=data["theme"], music=data["music"], weather=data["weather"],
                  ambient=data["ambient"], background=data["background"], seed=sum(map(ord, MAP_ID)))

    # 다시 그린 바닥·나무와 웹게임 충돌·지점·포탈은 그대로다. 칸 종류는 충돌을 더하지 않도록 풀밭으로 둔다.
    ground = np.asarray(Image.open(folder / "ground.png").convert("RGB"))[::pg.SCALE, ::pg.SCALE].copy()
    m.paint_ground = lambda: ground
    m.solid[:] = np.array([[cell == "1" for cell in row] for row in data["collision"]], dtype=bool)
    m.props = [prop for prop in data["props"] if prop["id"] not in WEB_TREES] + read(folder / "trees.json")["props"]
    m.uprights = read(folder / "uprights.json")["uprights"]
    for point in data["points"]:
        m.point(point["name"], *[(cell["x"], cell["y"]) for cell in point["cells"]])
    m.warps = list(data["warps"])

    rng = np.random.default_rng(611)
    rects = []
    split((0, 0, width, height), rng, rects)
    ids = chunk_ids(width, height, rects, rng)
    shifts = rng.integers(-SHIFT, SHIFT + 1, (len(rects), 2))
    shifts[np.hypot(shifts[:, 0], shifts[:, 1]) < SHIFT * 0.5] //= -1
    shifts[np.abs(shifts).max(axis=1) < SHIFT // 2] = (SHIFT, -SHIFT // 2)
    lifted = rng.random(len(rects)) < LIFT_SHARE

    with m.look("normal"):
        pass
    with m.look("twisted"):
        m.decal("twist_ground_full", twisted_ground(ground, ids, shifts, lifted, 1.0, np.random.default_rng(612)), 0, 0)
        for k, (x, y, w, hover) in enumerate(TWISTED_PIECES):
            m.stand(f"twist_piece_{k}", floating_piece(620 + k, w, hover), x, y)
    with m.look("loosened"):
        m.decal("twist_ground_half", twisted_ground(ground, ids, shifts, lifted, 0.5, np.random.default_rng(613)), 0, 0)
        for k, (x, y, w, hover) in enumerate(LOOSENED_PIECES):
            m.stand(f"twist_piece_low_{k}", floating_piece(630 + k, w, hover), x, y)
    return m, data


def main(project):
    m, data = build(project)
    original = np.array([[cell == "1" for cell in row] for row in data["collision"]], dtype=bool)
    for look in m.look_solid:
        solid = m.collision(look)
        if (solid != original).any():
            print(f"[{look}] 충돌이 웹게임 숲과 다르다")
        for x, y in WALK_CHECK:
            if solid[y * SUB:(y + 1) * SUB, x * SUB:(x + 1) * SUB].any():
                print(f"[{look}] 칸 ({x}, {y})이 막혀 있다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
