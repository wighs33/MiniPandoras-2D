"""뱀성 꼭대기 돌뱀 머리의 HD-2D 모형(맵 바닥).

윗면은 걷는 곳이라 높이 0이고, 눈두덩과 보석 눈만 솟는다. 윗면 가장자리부터는 옆얼굴이 둥글게 내려가 볼·입술·아래턱으로 이어지고,
주둥이에는 엄니가 내려온다. 북쪽 목은 뒤로 휘어 내려가 보이지 않게 된다. 옆얼굴 그림은 서쪽 반만 그려 동쪽은 뒤집어 쓴다.
좌표는 머리 그림의 점(왼쪽 위 원점)이고 left만큼 옮겨 맵 자리로 둔다.
"""
import numpy as np

import pixelgen as pg
import terrain
from picture_model import Model, plumb
from serpent_sculpture import EYE, GEM, MOSS, SNOUT, STONE, TIP, head_half

ROW = 8                    # 윗면을 나누는 줄 간격(점)
EYE_ROWS = (-0.2, 0.36)    # 솟은 곳이 있어 잘게 나누는 구간(v)
COARSE, FINE = 10, 72      # 윗면을 가로로 나누는 수
IVORY = pg.hexes("#8a7a5c", "#c0b08a", "#e6dcba", "#f6f0dc")
# 옆얼굴의 단면: (가장자리에서 바깥으로 나온 점, 아래로 내려간 점, 그림 밝기)
PROFILE = [(0, 0, 0.66), (5, 1, 0.68), (10, 4, 0.66), (14, 9, 0.6), (17, 18, 0.56), (20, 32, 0.54), (22, 48, 0.52), (21, 62, 0.46),
           (17, 74, 0.36), (10, 82, 0.2), (7, 86, 0.14), (10, 90, 0.4), (12, 98, 0.46), (11, 112, 0.42), (6, 128, 0.34), (-6, 146, 0.24),
           (-30, 166, 0.14), (-70, 186, 0.06)]
LIP = 8                    # PROFILE에서 윗입술 끝
NECK_RADIUS, NECK_STEPS = 150, 8


def build(head, left, hatch):
    """머리 모형. hatch는 계단 구멍의 네모(맵 점: x0, y0, x1, y1)."""
    height, width = head.image.shape[:2]
    m = Model(foot=(0, 0), pivot=(0, 0))
    top = m.panel(head.image, origin=(left, 0), crop=False)
    rows = top_rows(height)
    add_top(m, top, head, left, width, height, rows)
    add_sides(m, left, width, height, rows)
    add_neck(m, top, left, width, height)
    add_fangs(m, left, width, height)
    add_hatch(m, hatch)
    return m


def top_rows(height):
    """윗면을 가로로 자르는 줄들: 곧은 곳은 일정한 간격, 둥근 주둥이는 각도로 나눠 끝이 뾰족해지지 않게 한다."""
    straight = np.arange(0, int((SNOUT + 1) / 2 * height), ROW)
    curve = SNOUT + (TIP - SNOUT) * np.sin(np.linspace(0, np.pi / 2, 18))
    return np.concatenate([straight, (curve + 1) / 2 * height])


def rim(row, width, height):
    """그 줄에서 가운데부터 머리 가장자리까지의 점."""
    return float(head_half(row / height * 2 - 1)) * width / 2


def add_top(m, panel, head, left, width, height, rows):
    """걷는 윗면. 눈이 있는 줄들만 잘게 나눠 눈두덩과 보석을 솟게 한다. 위에서 본 그림이라 그림을 땅 자리 그대로 입힌다."""
    cx = left + width / 2

    def corner(t, row):
        x = cx + t * rim(row, width, height)
        column = int(np.clip(x - left, 0, width - 1))
        rise = head.rise[int(min(row, height - 1)), column] if abs(t) < 1 else 0.0
        return (*plumb(x, row, float(rise)), x, row)

    for r0, r1 in zip(rows, rows[1:]):
        v = (r0 + r1) / height - 1
        fine = EYE_ROWS[0] < v < EYE_ROWS[1]
        turns = np.linspace(-1, 1, (FINE if fine else COARSE) + 1)
        steps = np.linspace(r0, r1, 3) if fine else (r0, r1)
        for y0, y1 in zip(steps, steps[1:]):
            for t0, t1 in zip(turns, turns[1:]):
                if fine and max(abs(t0), abs(t1)) < 0.5 and not near(t0, t1, turns):
                    continue
                m.face(panel, [corner(t0, y1), corner(t1, y1), corner(t1, y0), corner(t0, y0)])
        if fine:
            # 눈 사이 평평한 곳은 한 장이다.
            inner = [t for t in turns if abs(t) < 0.5]
            m.face(panel, [corner(inner[0], r1), corner(inner[-1], r1), corner(inner[-1], r0), corner(inner[0], r0)])


def near(t0, t1, turns):
    """잘게 나눈 칸 가운데 평평한 가운데 구간 밖에 걸친 칸인지."""
    inner = [t for t in turns if abs(t) < 0.5]
    return t0 < inner[0] or t1 > inner[-1]


def side_path(width, height, rows):
    """서쪽 가장자리를 북에서 남(주둥이 끝)으로 따라간 점들: [(가운데에서 잰 x, 줄, 바깥 방향, 따라온 길이)]."""
    points = np.array([(-rim(row, width, height), row) for row in rows])
    tangents = np.gradient(points, axis=0)
    tangents /= np.linalg.norm(tangents, axis=1, keepdims=True)
    normals = np.stack([-tangents[:, 1], tangents[:, 0]], axis=1)
    normals[-1] = (0, 1)
    lengths = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(points, axis=0), axis=1))])
    return [(x, row, n, s) for (x, row), n, s in zip(points, normals, lengths)]


def profile_lengths():
    """단면을 따라 잰 길이(옆얼굴 그림의 줄)."""
    points = np.array([(o, d) for o, d, _ in PROFILE], dtype=float)
    return np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(points, axis=0), axis=1))])


def add_sides(m, left, width, height, rows):
    """옆얼굴: 가장자리 길을 따라 단면을 쓸어 만든다. 동쪽은 서쪽을 뒤집는다."""
    path = side_path(width, height, rows)
    down = profile_lengths()
    panel = m.panel(side_picture(int(np.ceil(path[-1][3])) + 2, int(np.ceil(down[-1])) + 2), crop=False)
    cx = left + width / 2
    for mirror in (1, -1):
        def corner(i, j):
            x, row, (nx, ny), s = path[i]
            out, drop, _ = PROFILE[j]
            return (*plumb(cx + mirror * (x + nx * out), row + ny * out, -drop), s + 0.5, down[j] + 0.5)

        for i in range(len(path) - 1):
            for j in range(len(PROFILE) - 1):
                m.face(panel, [corner(i, j), corner(i + 1, j), corner(i + 1, j + 1), corner(i, j + 1)])


def side_picture(length, depth):
    """옆얼굴 그림(가로는 목에서 주둥이까지, 세로는 윗면 가장자리에서 아래턱까지): 윗입술·아랫입술은 큰 판, 나머지는 작은 비늘."""
    rng = np.random.default_rng(29)
    yy, xx = np.mgrid[0:depth, 0:length]
    down = profile_lengths()
    light = np.interp(yy, down, [shade for _, _, shade in PROFILE])
    _, small = terrain.cells(depth, length, 9, rng)
    lip_top, lip_end = down[LIP - 3], down[LIP + 1]
    jaw_end = down[LIP + 5]
    plates = np.zeros_like(small)
    for start, end, size, shift in ((lip_top, lip_end, 34, 0), (down[LIP + 2], jaw_end, 30, 13)):
        band = (yy >= start) & (yy < end)
        seam = ((xx + shift + (yy - start) * 0.25) % size < 1.5) | (np.abs(yy - start) < 1)
        plates = np.where(band, seam, plates)
        small = np.where(band, False, small)
    groove = small | plates.astype(bool)
    light = light * (1 + 0.16 * (pg.noise(depth, length, 7, rng) - 0.5)) + 0.05 * (pg.noise(depth, length, 2, rng) - 0.5)
    # 홈은 어둡고, 홈 바로 아래는 모서리가 빛을 받는다
    after = np.zeros_like(groove)
    after[2:, 1:] = groove[:-2, :-1]
    light = np.where(after & ~groove, light + 0.08, light)
    light = np.where(groove, light * 0.62, light)
    rgb = pg.quantize(np.clip(light, 0, 1), STONE, dither=0.1)
    # 빗물 자국과 가장자리에 늘어진 이끼
    stain = (pg.noise(1, length, 5, rng)[0] > 0.72)[None, :] & (yy > 14) & (yy < 60 + 30 * pg.noise(1, length, 9, rng))
    rgb[stain] = (rgb[stain] * 0.84).astype(np.uint8)
    hang = 6 + 26 * np.clip(pg.noise(1, length, 12, rng) - 0.45, 0, 1)
    moss = (yy < hang) & (pg.noise(depth, length, 4, rng) > 0.5) & (pg.noise(1, length, 12, rng) > 0.5)
    rgb[moss] = MOSS[1]
    return np.dstack([rgb, np.full((depth, length), 255)]).astype(np.uint8)


def add_neck(m, panel, left, width, height):
    """북쪽 목: 뒤로 휘며 아래로 내려간다. 윗면은 머리 그림의 북쪽 줄을 거꾸로 이어 입힌다."""
    cx, half = left + width / 2, rim(0, width, height)
    stone = m.panel(neck_picture(), crop=False)

    def corner(x, angle, out=0.0, drop=0.0):
        return plumb(cx + x, -NECK_RADIUS * np.sin(angle), -NECK_RADIUS * (1 - np.cos(angle)) - drop)

    angles = np.linspace(0, np.deg2rad(100), NECK_STEPS + 1)
    for a0, a1 in zip(angles, angles[1:]):
        y0, y1 = a0 * NECK_RADIUS, a1 * NECK_RADIUS
        shade = float(np.cos(a0 * 0.7))
        m.face(panel, [(*corner(-half, a0), cx - half, y0 + 1), (*corner(half, a0), cx + half, y0 + 1),
                       (*corner(half, a1), cx + half, y1 + 1), (*corner(-half, a1), cx - half, y1 + 1)], shade)
        for side in (-1, 1):
            for (o0, d0, s0), (o1, d1, _) in zip(PROFILE, PROFILE[1:]):
                m.face(stone, [(*corner(side * (half + o0), a0, drop=d0), y0 % 60, d0 % 60), (*corner(side * (half + o1), a0, drop=d1), y0 % 60, d1 % 60 + 1),
                               (*corner(side * (half + o1), a1, drop=d1), y0 % 60 + 12, d1 % 60 + 1), (*corner(side * (half + o0), a1, drop=d0), y0 % 60 + 12, d0 % 60)],
                       min(1.0, 0.3 + s0))


def neck_picture():
    """목 옆의 작은 비늘(72×72)."""
    rng = np.random.default_rng(5)
    _, seams = terrain.cells(72, 72, 9, rng)
    light = 0.5 + 0.1 * (pg.noise(72, 72, 5, rng) - 0.5)
    rgb = pg.quantize(np.where(seams, light * 0.6, light), STONE, dither=0.1)
    return np.dstack([rgb, np.full((72, 72), 255)]).astype(np.uint8)


def add_fangs(m, left, width, height):
    """윗입술에서 내려오는 엄니: 주둥이 앞의 큰 것 둘과 볼을 따라 작은 것들. 여섯모뿔이다."""
    ivory = np.zeros((32, 8, 4), dtype=np.uint8)
    ivory[..., 3] = 255
    for y in range(32):
        ivory[y, :, :3] = IVORY[3 if y < 6 else 2 if y < 20 else 1]
    ivory[:, 6:, :3] = IVORY[1]
    panel = m.panel(ivory, crop=False)
    rows = np.linspace(0, height * (TIP + 1) / 2, 900)
    path = side_path(width, height, rows)
    total = path[-1][3]
    cx = left + width / 2
    for back, size, length in ((96, 13, 62), (150, 7, 26), (196, 6, 22), (250, 6, 20), (320, 5, 18), (400, 5, 16)):
        x, row, (nx, ny), _ = min(path, key=lambda p: abs(p[3] - (total - back)))
        out, drop, _ = PROFILE[LIP]
        for mirror in (1, -1):
            base_x, base_y = x + nx * (out - 3), row + ny * (out - 3)
            for k in range(6):
                a0, a1 = k * np.pi / 3, (k + 1) * np.pi / 3
                ring = [(base_x + size * np.cos(a), base_y + size * np.sin(a)) for a in (a0, a1)]
                tip = (base_x + nx * 3, base_y + ny * 3)
                m.face(panel, [(*plumb(cx + mirror * ring[0][0], ring[0][1], -drop + 6), 1, 1),
                               (*plumb(cx + mirror * ring[1][0], ring[1][1], -drop + 6), 7, 1),
                               (*plumb(cx + mirror * tip[0], tip[1], -drop - length), 4, 31)])


def add_hatch(m, hatch):
    """목에 낸 계단 구멍: 돌 테를 두르고, 안은 어둠 속으로 내려가는 계단이다."""
    x0, y0, x1, y1 = hatch
    frame = np.zeros((12, 16, 4), dtype=np.uint8)
    frame[..., 3] = 255
    frame[..., :3] = STONE[5]
    frame[:2, :, :3] = STONE[7]
    frame[8:, :, :3] = STONE[3]
    stone = m.panel(frame, crop=False)
    rise, wide = 9, 7
    for left, back, right, front in ((x0 - wide, y0 - wide, x1 + wide, y0), (x0 - wide, y1, x1 + wide, y1 + wide), (x0 - wide, y0, x0, y1), (x1, y0, x1 + wide, y1)):
        m.face(stone, [(*plumb(left, front, rise), 1, 7), (*plumb(right, front, rise), 15, 7), (*plumb(right, back, rise), 15, 2), (*plumb(left, back, rise), 1, 2)])
        for (ax, ay), (bx, by) in (((left, front), (right, front)), ((left, back), (left, front)), ((right, back), (right, front))):
            m.face(stone, [(*plumb(ax, ay, 0), 1, 11), (*plumb(bx, by, 0), 15, 11), (*plumb(bx, by, rise), 15, 7), (*plumb(ax, ay, rise), 1, 7)])
    steps = np.zeros((32, 8, 4), dtype=np.uint8)
    steps[..., 3] = 255
    dark = pg.hexes("#08080a", "#131216", "#201e24", "#2e2b32", "#3e3a42")
    for k in range(5):
        steps[k * 6:k * 6 + 7, :, :3] = dark[k]
        steps[k * 6 + 5:k * 6 + 7, :, :3] = dark[max(0, k - 1)]
    well = m.panel(steps, crop=False)
    m.face(well, [(*plumb(x0, y1, 2), 1, 30), (*plumb(x1, y1, 2), 7, 30), (*plumb(x1, y0, 2), 7, 1), (*plumb(x0, y0, 2), 1, 1)])
