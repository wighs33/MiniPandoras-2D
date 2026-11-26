"""연출에 나오는 큰 것들의 HD-2D 모형: 9장 군함·널다리·청룡·모래벽, 11장 나리온, 뱀열차 머리.

  - 군함: 갑판은 뉴웨이브 호와 같은 높이(0)에 눕고, 바깥 뱃전은 바다까지 늘어지며, 돛대는 갑판 위에 선다.
  - 청룡·나리온·뱀머리: 그림 한 장을 세우고, 가장자리에서 안쪽으로 들어갈수록 앞으로 부풀린다(부조). 둥근 몸에 해가 비친다.
  - 널다리는 두 갑판에 걸쳐 눕고, 모래벽은 윗면을 눕힌 상자다.
뱀열차 몸통은 길을 따라 휘므로 게임(World.SnakeTrainBody)이 그림을 통에 감아 만든다.

  Assets/MiniPandoras/Art/PixelGen/Stage/models.png·models.json   들여오기는 Editor/StageModels(메뉴 MiniPandoras/Import Stage Models)

사용: python Tools/PixelGen/stage_models.py <프로젝트 루트>  (warship·sea_dragon·snake_train·sand_wall.py, narion.py 뒤에)
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import picture_model
import pixelgen as pg
import prop_models
import ship
import warship
from picture_model import Model, standing

T = pg.TILE
# 부풀린 부조: 가장자리에서 이만큼(점) 안쪽이면 가장 두껍다, 두께(점), 격자 한 칸(점)
PUFFS = {
    "sea_dragon": dict(reach=22, thick=20, step=4), "train_head": dict(reach=34, thick=24, step=4),
    "narion": dict(reach=40, thick=30, step=4),
}


# ── 부조 ──────────────────────────────────────────────

def inner_distance(painted, reach):
    """칠한 점마다 가장 가까운 빈 점까지의 거리(reach에서 멈춘다). 한 점씩 깎으며 세되 가로·세로와 대각선을 번갈아 둥글게 깎는다."""
    distance = np.zeros(painted.shape)
    left = np.pad(painted, 1)
    for k in range(reach):
        distance += left[1:-1, 1:-1]
        straight = left[1:-1, 1:-1] & left[:-2, 1:-1] & left[2:, 1:-1] & left[1:-1, :-2] & left[1:-1, 2:]
        if k % 2:
            straight &= left[:-2, :-2] & left[:-2, 2:] & left[2:, :-2] & left[2:, 2:]
        left = np.pad(straight, 1)
    return distance


def puffed(image, pivot, reach, thick, step, unit=1.0):
    """
    세운 그림을 앞으로 부풀린 모형. unit은 그림 점 하나가 맵 점 몇 개인지(나리온은 2).
    두께는 원의 단면을 따라, 가장자리에서는 급하게 안쪽에서는 완만하게 는다.
    """
    m = Model(foot=tuple(pivot), pivot=tuple(pivot), smooth=True)
    panel = m.panel(image, crop=False)
    h, w = image.shape[:2]
    px, ground = pivot
    reach = max(2, int(round(reach / unit)))
    depth = inner_distance(image[..., 3] > 0, reach) / reach
    depth = thick * np.sqrt(np.clip(1 - (1 - depth) ** 2, 0, 1))
    for _ in range(3):
        padded = np.pad(depth, 1)
        depth = (padded[:-2, 1:-1] + padded[2:, 1:-1] + padded[1:-1, :-2] + padded[1:-1, 2:] + 4 * depth) / 8

    def corner(x, row):
        bulge = depth[min(row, h - 1), min(x, w - 1)] if row < h and x < w else 0.0
        return px + (x - px) * unit, ground + bulge, (ground - row) * unit, x, row

    for row in range(0, h, step):
        for x in range(0, w, step):
            x1, row1 = min(x + step, w), min(row + step, h)
            if image[row:row1, x:x1, 3].any():
                m.face(panel, [corner(x, row1), corner(x1, row1), corner(x1, row), corner(x, row)])
    return m


# ── 군함 ──────────────────────────────────────────────

def warship_model():
    """군함 모형. 뉴웨이브 호의 높이(ship.ship_relief)와 같게: 갑판 0, 북쪽 뱃전은 솟고, 바깥 뱃전은 바다(−SIDE)까지 늘어진다."""
    hull, masts = warship.layers()
    hull = pg.outline(hull, warship.OUTLINE)
    h, w = hull.shape[:2]
    m = Model(foot=(w / 2, h), pivot=(w / 2, h))
    panel = m.panel(hull, crop=False)
    north, south, inside = ship.deck_edges(w)
    # 그림은 좌우를 뒤집어 뱃머리가 왼쪽이다.
    north, south, inside = north[::-1], south[::-1], inside[::-1]
    columns = np.nonzero(inside)[0]
    bow, stern = columns[0], columns[-1] + 1
    cap, rail, side = ship.CAP, ship.FACE_H, ship.SIDE

    def edge(values, x):
        return float(values[int(min(max(x, bow), stern - 1))])

    for x0, x1 in prop_models.strips(bow, stern, 60):
        n0, n1, s0, s1 = edge(north, x0), edge(north, x1), edge(south, x0) + cap, edge(south, x1) + cap
        n0, n1 = min(n0, s0 - cap), min(n1, s1 - cap)
        r0, r1 = min(n0 + rail, s0), min(n1 + rail, s1)
        # 갑판, 북쪽 뱃전의 안쪽 면과 윗면, 남쪽 바깥 뱃전
        m.face(panel, [(x0, s0, 0, x0, s0), (x1, s1, 0, x1, s1), (x1, r1, 0, x1, r1), (x0, r0, 0, x0, r0)])
        m.face(panel, [(x0, r0, 0, x0, r0), (x1, r1, 0, x1, r1), (x1, r1, r1 - n1, x1, n1), (x0, r0, r0 - n0, x0, n0)])
        m.face(panel, [(x0, n0 + rail, rail, x0, n0), (x1, n1 + rail, rail, x1, n1), (x1, n1 + rail - cap - 3, rail, x1, n1 - cap - 3), (x0, n0 + rail - cap - 3, rail, x0, n0 - cap - 3)])
        m.face(panel, [(x0, s0, -side, x0, s0 + side), (x1, s1, -side, x1, s1 + side), (x1, s1, 0, x1, s1), (x0, s0, 0, x0, s0)])
    # 뱃머리 밖으로 뻗은 이물대
    first = int(np.nonzero(hull[..., 3].any(axis=0))[0].min())
    rows = np.nonzero(hull[:, first:bow, 3].any(axis=1))[0]
    prop_models.flat(m, panel, first, bow, int(rows.min()), min(int(rows.max()) + 1, int(south[bow]) + cap + 4), 0)
    for image, left, top, deck in masts:
        sail = m.panel(image, origin=(left, top))
        (sx, sy), (sw, sh) = sail.origin, sail.size
        standing(m, sail, sx, sx + sw, sy, sy + sh, deck, deck - (sy + sh))
    return m


def plank_model(image):
    """널다리: 두 갑판 위에 조금 떠서 걸친다. 앞 가장자리는 두께가 보인다."""
    h, w = image.shape[:2]
    m = Model(foot=(w / 2, h), pivot=(w / 2, h))
    panel = m.panel(image, crop=False)
    lift, front = 7, h - 6
    prop_models.flat(m, panel, 0, w, 0, front, lift)
    standing(m, panel, 0, w, front, h, front + lift, lift - (h - front))
    return m


def sand_wall_model(image):
    h, w = image.shape[:2]
    m = Model(foot=(w / 2, h), pivot=(w / 2, h))
    prop_models.block(m, image, h, 0, T)
    return m


# ── 모으기 ─────────────────────────────────────────────

def load(path, dot):
    return np.array(Image.open(path).convert("RGBA"))[::dot, ::dot]


def build(project):
    art = project / "Assets/MiniPandoras/Art/PixelGen"
    stage = art / "Stage"
    models = {"warship": warship_model(), "warship_plank": plank_model(load(stage / "warship_plank.png", 2)),
              "sand_wall": sand_wall_model(load(stage / "sand_wall.png", 2))}
    dragon = load(stage / "sea_dragon.png", 2)
    models["sea_dragon"] = puffed(dragon, (dragon.shape[1] / 2, dragon.shape[0]), **PUFFS["sea_dragon"])
    head = load(art / "SnakeTrain/head.png", 2)
    models["train_head"] = puffed(head, (head.shape[1] * 0.5769231, head.shape[0] - 4), **PUFFS["train_head"])
    for path in sorted(stage.glob("narion_*.png")):
        bird = load(path, 4)
        models[path.stem] = puffed(bird, (bird.shape[1] / 2, bird.shape[0] * (1 - 0.1985294)), unit=2.0, **PUFFS["narion"])
    return models


def main(project):
    folder = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    picture_model.write(folder, build(project))
    print(folder / "models.json")


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
