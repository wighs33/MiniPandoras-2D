"""숲 구역 심기(사용자 요청 2026-11-03: 여러 종류의 나무로 숲 같은 구역을).

plant(m, region, rng)는 칸 격자 마스크 region 안에 나무를 고르게 흩어 심는다(가까운 나무끼리 일정한 거리 이상 떨어지게,
줄지어 서지 않게). 나무 종류는 큰 잡음으로 나눈 무리마다 다르다: 소나무·가문비나무가 모인 침엽수 무리, 자작나무 숲,
참나무·둥근 활엽수·연두 잎 나무가 섞인 활엽수 숲. 숲 가장자리(지나다니는 칸과 맞닿은 줄)에는 어린 나무를 섞어
숲이 갑자기 끊기지 않게 한다. 맵 맨 위 줄에는 침엽수만 심는다(활엽수는 수관이 맵 밖으로 잘려 줄기만 보인다).
숲 바닥은 숲 그늘 바닥(terrain woods)으로 칠한다.

나무 종류는 맵 소품 id다(redraw_map.PROPS가 그림을 내보내고, 맵 들여오기가 나무로 알아본다).
"""
import numpy as np

import pixelgen as pg

# 무리마다 나무 종류의 비율
CONIFERS = {"pine": 0.45, "spruce": 0.45, "birch": 0.1}
BIRCHES = {"birch": 0.65, "maple": 0.2, "tree": 0.15}
BROADLEAF = {"oak": 0.32, "tree": 0.34, "maple": 0.2, "birch": 0.14}
# 맵 맨 위 줄: 수관이 맵 밖으로 잘려 줄기만 보이지 않게, 잎이 밑동 가까이까지 내려오는 침엽수만 심는다.
TOP_EDGE = {"spruce": 0.6, "pine": 0.4}
TOP_ROWS = 3
# 나무 종류마다 이웃과 떨어져야 하는 거리(칸). 큰 나무일수록 넓다.
SPACING = {"oak": 1.9, "tree": 1.6, "maple": 1.5, "birch": 1.2, "pine": 1.4, "spruce": 1.2, "sapling": 0.9}


def choose(rng, mix):
    """무리 비율대로 나무 종류 하나(자리는 그대로 두고 종류만 섞는 맵이 쓴다)."""
    return str(rng.choice(list(mix), p=list(mix.values())))


def canopy_over(open_cells, side=2, rows=5):
    """나무 밑동을 두면 수관(밑동에서 위로 rows칸, 옆으로 side칸)이 open_cells를 덮게 되는 칸."""
    height, width = open_cells.shape
    padded = np.pad(open_cells, ((rows, 0), (side, side)))
    covered = np.zeros_like(open_cells)
    for dy in range(0, rows + 1):
        for dx in range(-side, side + 1):
            covered |= padded[rows - dy:rows - dy + height, side + dx:side + dx + width]
    return covered


def lanes(open_cells, reach=2):
    """숲 사이 좁은 길: 좌우 양쪽(또는 위아래 양쪽) reach칸 안에 막힌 칸이 있는 열린 칸. 맵 밖은 막힌 것으로 본다."""
    height, width = open_cells.shape
    blocked = np.pad(~open_cells, reach, constant_values=True)

    def wall_within(dy, dx):
        hit = np.zeros_like(open_cells)
        for step in range(1, reach + 1):
            y, x = reach + dy * step, reach + dx * step
            hit |= blocked[y:y + height, x:x + width]
        return hit

    return open_cells & ((wall_within(0, -1) & wall_within(0, 1)) | (wall_within(-1, 0) & wall_within(1, 0)))


def zones(height, width, rng):
    """칸마다 숲 무리: 0 침엽수, 1 자작나무, 2 활엽수. 칸 몇 개 크기의 덩어리로 나뉜다."""
    big = pg.noise(height * 8, width * 8, 48, rng)[::8, ::8]
    small = pg.noise(height * 8, width * 8, 24, rng)[::8, ::8]
    return np.where(big < 0.4, 0, np.where(small > 0.68, 1, 2))


def mixed(m, cells, rng, mixes=(CONIFERS, BIRCHES, BROADLEAF)):
    """정해 둔 칸들에 나무를 심되 종류만 무리 비율대로 고른다(나무 자리가 길·집과 맞물린 맵). 맨 위 줄은 침엽수다."""
    zone = zones(m.height, m.width, rng)
    for x, y in cells:
        m.tree(x, y, choose(rng, TOP_EDGE if y < TOP_ROWS else mixes[zone[y, x]]))


def plant(m, region, rng, mixes=(CONIFERS, BIRCHES, BROADLEAF), floor=True, low=None):
    """
    region(칸 격자 마스크, 막힌 숲 칸) 안에 나무를 심고 심은 (x, y, 종류) 목록을 돌려준다.
    칸을 뒤섞어 하나씩 보며, 이미 심은 나무와 종류별 거리보다 가까우면 건너뛴다. 가장자리 칸은 어린 나무가 섞인다.
    low(칸 마스크)에는 어린 나무만 드문드문(셋에 하나꼴) 심는다: 큰 나무의 수관이 길을 덮을 칸(canopy_over).
    빈 low 칸은 부르는 쪽이 덤불 따위로 채운다.
    """
    zone = zones(m.height, m.width, rng)
    edge = region & pg.neighbours(~region)
    cells = list(zip(*np.nonzero(region)))
    rng.shuffle(cells)
    planted = []
    for y, x in cells:
        x, y = int(x), int(y)
        if low is not None and low[y, x]:
            if rng.random() > 0.35:
                continue
            kind = "sapling"
        elif edge[y, x] and y >= TOP_ROWS and rng.random() < 0.3:
            kind = "sapling"
        else:
            kind = choose(rng, TOP_EDGE if y < TOP_ROWS else mixes[zone[y, x]])
        reach = SPACING[kind] * rng.uniform(0.9, 1.15)
        if any(np.hypot(x - px, (y - py) * 1.15) < max(reach, SPACING[other] * 0.8) for px, py, other in planted):
            continue
        planted.append((x, y, kind))
        m.tree(x, y, kind)
    if floor:
        ground = region & np.isin(m.kinds, ("grass", "flowers", "tallgrass"))
        m.kinds[ground] = "woods"
    return planted
