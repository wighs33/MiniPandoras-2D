"""처음 들판(hillia, 1·2장 「힐리아 동부」)을 새로 짠다(사용자 요청 2026-11-03: 돌담 품질, 밋밋한 길과 그 둘레).

못·풀숲·꽃·덤불·바위·표지판 자리, 충돌(숲 칸 포함), 이야기 지점과 북쪽 출구는 웹게임 들판(Art/WebGame/Maps/hillia의
map.json·tiles.json)을 그대로 가져온다(1·2장 연출과 테스트가 그 칸을 쓴다). 바뀌는 것:
  · 웹게임 나무 90그루 → 막힌 숲 칸에 여러 종류(침엽수 무리, 자작나무 숲, 참나무·활엽수 숲, 가장자리 어린 나무)를 섞어 심은
    숲과 숲 그늘 바닥(forest.plant, 사용자 요청 2026-11-03)
  · 절벽 칸 두 줄로 그리던 담 → 막돌을 쌓은 돌담(wayside.stone_wall). 막히는 칸은 그대로다
  · 곧게 뻗은 흙길 → 살짝 굽이지며 폭이 달라지는 오솔길과, 가운데 밟혀 다져진 자국
  · 길가: 자갈, 풀 무더기, 들꽃, 토끼풀, 숲 가장자리의 버섯과 고사리
길은 지나다니는 칸(풀밭)만 바꾸므로 굽어도 막히는 곳이 없다. 이야기 인물이 서는 칸 둘레에는 길가 소품을 두지 않는다.

사용: python Tools/PixelGen/hillia.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/hillia/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/hillia_preview.png    미리보기
"""
import json
import sys
from pathlib import Path

import numpy as np

import forest
import pixelgen as pg
import terrain
import wayside
from layout import SUB, MapLayout

T = pg.TILE
WEB = "Assets/MiniPandoras/Art/WebGame/Maps/hillia"
# 웹게임 담(절벽 칸): 칸 x 범위와 줄. 돌담은 그 아래 줄 끝(칸 y 22)에 발을 딛는다.
WALLS = [(2, 8), (12, 17)]
WALL_GROUND = 22
# 오솔길: 북쪽 출구(17~18, 0)에서 착지 자리(17, 33) 아래까지 살짝 굽이진다. 표지판(19, 6)과 담 끝(17, 20~21)을 비켜 간다.
MAIN_PATH = [(17.5, -1), (17.5, 2), (16.8, 5.5), (16.9, 9), (18.2, 12.5), (18.6, 16), (18.3, 19.5), (18.4, 23), (17.4, 27),
             (17.5, 30.5), (17.6, 34), (17.8, 36.5)]
# 서쪽 갈림길: 불량배가 기다리는 서쪽 끝(0~2, 30)까지
WEST_PATH = [(17.5, 30.5), (14, 30.2), (10, 30.7), (6, 30.4), (2.5, 30.5), (-1, 30.5)]
# 길의 반폭(칸): 대개 한 칸, 표지판 앞과 착지 자리 둘레는 조금 넓고, 서쪽 갈림길은 좁다.
PATHS = [(MAIN_PATH, 1.0), (MAIN_PATH[2:4], 1.25), (MAIN_PATH[8:11], 1.35), (WEST_PATH, 0.8)]
# 1장 연출이 인물을 세우는 칸(지점 밖): 불량배 둘, 랑
STAGE = [(16, 32), (16, 33), (19, 33)]


def path_reach(x, y):
    """칸 자리 (x, y)가 길 가운데에서 반폭의 몇 배만큼 떨어졌는지(1보다 작으면 길 위)."""
    best = np.inf
    for points, half in PATHS:
        for (ax, ay), (bx, by) in zip(points, points[1:]):
            dx, dy = bx - ax, by - ay
            u = np.clip(((x - ax) * dx + (y - ay) * dy) / max(1e-9, dx * dx + dy * dy), 0, 1)
            best = min(best, np.hypot(x - (ax + u * dx), y - (ay + u * dy)) / half)
    return best


def load_web(project):
    folder = project / WEB
    data = json.loads((folder / "map.json").read_text(encoding="utf-8"))
    tiles = json.loads((folder / "tiles.json").read_text(encoding="utf-8"))["tiles"]
    return data, np.array(tiles, dtype=object)


def segments(points, step=0.8):
    """꺾은선을 따라 step 칸마다 (자리, 진행 방향 단위 벡터)."""
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        length = np.hypot(bx - ax, by - ay)
        direction = np.array([bx - ax, by - ay]) / max(length, 1e-9)
        for t in np.arange(0, length, step):
            yield np.array([ax, ay]) + direction * t, direction


def dress_wayside(m, paths, avoid, rng):
    """
    길가 꾸미기: 길 가장자리 바깥 풀밭에 자갈·들국화·토끼풀·들꽃 무리(바닥 그림)와 풀 무더기·들꽃·작은 바위·그루터기(세운 것)를
    흩뿌리고, 숲에 가까운 길가에는 버섯과 고사리를 둔다. 길 위에도 드문드문 자갈이 박혀 있다.
    """
    flat = {"pebbles": [wayside.pebbles(rng) for _ in range(3)], "daisies": [wayside.daisies(rng) for _ in range(3)],
            "clover": [wayside.clover(rng) for _ in range(3)], "flower_patch": [wayside.flower_patch(rng) for _ in range(4)]}
    standing = {"grass_clump": [wayside.grass_clump(rng) for _ in range(4)], "wildflower": [wayside.wildflower(rng) for _ in range(5)],
                "mushrooms": [wayside.mushrooms(rng) for _ in range(2)], "fern": [wayside.fern(rng) for _ in range(2)],
                "boulder": [wayside.boulder(rng) for _ in range(2)], "stump": [wayside.stump(rng)]}
    solid = m.collision().reshape(m.height, SUB, m.width, SUB).any(axis=(1, 3))
    forest = np.zeros_like(solid)
    for prop in m.props:
        forest[max(0, prop["y"] - 2):prop["y"] + 3, max(0, prop["x"] - 2):prop["x"] + 3] = True

    def free(x, y):
        cx, cy = int(np.floor(x)), int(np.floor(y))
        if not (0 <= cx < m.width and 0 <= cy < m.height) or solid[cy, cx] or m.kinds[cy, cx] == "water" or path_reach(x, y) < 1.15:
            return False
        return all(np.hypot(x - (ax + 0.5), y - (ay + 0.5)) > 1.3 for ax, ay in avoid)

    for points, half in paths:
        for at, direction in segments(points):
            normal = np.array([-direction[1], direction[0]])
            for side in (-1, 1):
                if rng.random() > 0.9:
                    continue
                x, y = at + normal * side * (half + rng.uniform(0.15, 1.2)) + direction * rng.uniform(-0.4, 0.4)
                if not free(x, y):
                    continue
                near_trees = forest[int(y), int(x)]
                kinds = ["grass_clump", "wildflower", "flower_patch", "pebbles", "daisies", "clover", "boulder", "stump", "mushrooms", "fern"]
                weights = ([0.2, 0.12, 0.1, 0.1, 0.08, 0.08, 0.06, 0.06, 0.1, 0.1] if near_trees else
                           [0.26, 0.16, 0.16, 0.1, 0.1, 0.1, 0.07, 0.05, 0.0, 0.0])
                kind = rng.choice(kinds, p=weights)
                place(m, kind, flat, standing, x, y, rng)
            if rng.random() < 0.22:
                x, y = at + normal * rng.uniform(-half * 0.6, half * 0.6)
                place(m, "pebbles", flat, standing, x, y, rng)


def place(m, kind, flat, standing, x, y, rng):
    """길가 소품 한 개를 칸 자리(소수) (x, y)에 둔다. 같은 종류는 몇 벌의 그림을 돌려 쓴다."""
    if kind in flat:
        index = int(rng.integers(0, len(flat[kind])))
        image = flat[kind][index]
        m.decal(f"{kind}_{index}", image, x * T - image.shape[1] / 2, y * T - image.shape[0] / 2)
    else:
        index = int(rng.integers(0, len(standing[kind])))
        image, pivot = standing[kind][index]
        m.upright(f"{kind}_{index}", image, x * T, y * T, pivot, y * 16)


def build(project):
    data, kinds = load_web(project)
    m = MapLayout("hillia", data["name"], data["width"], data["height"], music=data["music"], seed=21)
    rng = np.random.default_rng(22)

    # ── 웹게임 바탕: 칸 종류(담과 곧은 흙길은 풀밭으로 비운다), 충돌 ──
    m.kinds[:] = np.where(np.isin(kinds, ("cliff", "path")), "grass", kinds)
    m.solid[:] = np.array([[v == "1" for v in row] for row in data["collision"]])

    # ── 숲: 웹게임에서 통째로 막힌 풀밭 칸(담 칸 빼고)에 여러 종류의 나무를 섞어 심는다.
    #    못·바위 둘레처럼 칸 일부만 막힌 칸은 숲이 아니다 ──
    blocked = m.solid.reshape(m.height, SUB, m.width, SUB).all(axis=(1, 3))
    walls = np.zeros_like(blocked)
    for x0, x1 in WALLS:
        walls[WALL_GROUND - 2:WALL_GROUND, x0:x1] = True
    forest.plant(m, blocked & (m.kinds == "grass") & ~walls, np.random.default_rng(24))
    # 길 위에 있던 꽃 칸은 풀밭으로(길에 묻히는 꽃밭이 없게)
    for y, x in zip(*np.nonzero(m.kinds == "flowers")):
        if path_reach(x + 0.5, y + 0.5) < 1.2:
            m.kinds[y, x] = "grass"

    # ── 굽이진 오솔길: 칸이 아니라 점마다 칠해 바닥 그림에 넣는다(맵 전체 크기의 덮는 그림은 작은 바닥 소품을 가린다) ──
    def paint_ground():
        image = terrain.draw_outdoor(m.kinds.tolist(), seed=m.seed)
        wayside.dirt_path(image, PATHS, np.random.default_rng(23))
        return image

    m.paint_ground = paint_ground

    # ── 돌담: 웹게임 담 칸 위에 세운다(막히는 칸은 웹게임 충돌 그대로) ──
    for index, (x0, x1) in enumerate(WALLS):
        m.stand(f"stone_wall_{index}", wayside.stone_wall(x1 - x0, np.random.default_rng(31 + index)), (x0 + x1) / 2, WALL_GROUND)

    # ── 이야기 지점과 북쪽 출구(웹게임 그대로) ──
    for point in data["points"]:
        m.point(point["name"], *[(cell["x"], cell["y"]) for cell in point["cells"]], blocked=point["name"] == "sign")
    for warp in data["warps"]:
        m.warp(warp["point"], warp["map"], warp["target"], warp["dir"])

    # ── 길가 꾸미기(이야기 인물이 서는 칸 둘레는 비운다) ──
    avoid = [cell for cells in m.points.values() for cell in cells] + STAGE
    dress_wayside(m, [(MAIN_PATH, 1.0), (WEST_PATH, 0.8)], avoid, rng)
    return m


def main(project):
    m = build(project)
    landing = m.points["landing"][0]
    for goal in [m.points["north"][0], m.points["thug2"][0], m.points["rang"][0]] + m.points["dog"]:
        if not m.reachable(landing, [goal]):
            print(f"착지 자리에서 {goal}에 닿지 못한다")
    # 맵 이동 테스트는 착지 자리에서 곧장 북쪽으로 걸어 출구에 닿는다.
    if not m.route_is_clear(landing, f"U{landing[1]}"):
        print("착지 자리에서 북쪽 출구까지 곧은 길이 막혔다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
