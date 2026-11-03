"""뱀성의 돌뱀 조각(사용자 결정 2026-11-02: 뱀은 살아 있는 게 아니라 조형물이고, 머리는 정면을 본다. 혀는 없다).

머리는 따로 그리는 소품이다. 모양을 높이 지도(어디가 솟고 어디가 패였는지)로 정하고, 높이의 기울기로 면의 방향을 구해
왼쪽 위에서 오는 빛을 받게 칠한다. 그래서 깎아 만든 돌처럼 둥근 머리통, 튀어나온 눈두덩, 패인 눈구멍과 콧구멍,
홈을 판 비늘판이 입체로 보인다. 눈구멍에는 붉게 빛나는 보석을 박고, 다문 위턱 아래로 엄니 둘이 내려온다.

  head_front(width, rng) → (RGBA 점 그림, 기준점: 아래 가운데)
"""
import numpy as np

import pixelgen as pg
import terrain

# 비바람에 바랜 돌(어두운 쪽부터)
STONE = pg.hexes("#221f20", "#363132", "#4b4545", "#625a57", "#7b726b", "#958b80", "#b0a597", "#cbc2b3")
GEM = pg.hexes("#2a0604", "#5e1006", "#a4240c", "#e2541c", "#ffa64a", "#ffe6a8")
MOSS = pg.hexes("#2c3a22", "#41552c", "#5c7236")
LIGHT = np.array([-0.55, -0.7, 0.55]) / np.linalg.norm([-0.55, -0.7, 0.55])


def gauss(u, v, cu, cv, su, sv):
    return np.exp(-(((u - cu) / su) ** 2 + ((v - cv) / sv) ** 2))


def ridge(u, v, start, end, width):
    """start에서 end까지 이어진 솟은 줄(눈두덩): 선분까지 거리로 높이를 준다."""
    (au, av), (bu, bv) = start, end
    du, dv = bu - au, bv - av
    t = np.clip(((u - au) * du + (v - av) * dv) / (du * du + dv * dv), 0, 1)
    distance = np.hypot(u - (au + t * du), v - (av + t * dv))
    return np.exp(-(distance / width) ** 2)


def carve(z, inside, u, v, rng, eye_zone, plate_band=0.4, plate_size=16):
    """높이 지도에 비늘판 홈을 파고, 왼쪽 위 빛으로 돌 명암을 칠한다. 돌려주는 값: (RGB, 빛 세기)."""
    height, width = z.shape
    _, big = terrain.cells(height, width, plate_size, rng)
    _, small = terrain.cells(height, width, max(5, plate_size // 2), rng)
    half_w = width // 2
    big = np.hstack([big[:, :half_w], big[:, :half_w][:, ::-1]])[:, :width]
    plates = np.where(np.abs(u) < plate_band, big, small)
    plates |= np.abs(u) < 0.012
    z = z - 0.07 * (plates & ~eye_zone)
    dz_dy, dz_dx = np.gradient(z * width * 0.5)
    normal = np.dstack([-dz_dx, -dz_dy, np.full_like(z, 7.0)])
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    light = np.clip(normal @ LIGHT * 0.95 + 0.12, 0, 1)
    light = np.where(plates & ~eye_zone, light * 0.72, light)
    rgb = pg.quantize(light, STONE, dither=0.12)
    moss = (pg.noise(height, width, 9, rng) > 0.78) & (light > 0.45) & inside & ~eye_zone
    rgb[moss] = MOSS[1 + (light[moss] > 0.7)]
    return rgb, light


def gem_eye(image, u, v, cu, cv, ru, rv, tilt=0.0, lid=True):
    """보석 눈: 가운데가 밝게 빛나는 타원, 세로 동공 홈, 빛 한 점, 둘레 짙은 돌. lid면 눈두덩 그늘이 위를 덮는다."""
    du, dv = u - cu, v - cv
    a = du * np.cos(tilt) + dv * np.sin(tilt)
    b = -du * np.sin(tilt) + dv * np.cos(tilt)
    depth = (a / ru) ** 2 + (b / rv) ** 2
    gem = depth <= 1
    level = np.select([depth < 0.15, depth < 0.45, depth < 0.8], [5, 4, 3], 2)
    level = np.where((b > 0.35 * rv) & (level > 2), level - 1, level)
    image[gem, :3] = GEM[level[gem]]
    image[gem & (np.abs(a) < 0.12 * ru), :3] = GEM[0]
    image[gem & (np.hypot((a + 0.4 * ru) / (0.2 * ru), (b + 0.45 * rv) / (0.3 * rv)) < 1), :3] = GEM[5]
    rim = ~gem & ((a / (ru * 1.2)) ** 2 + (b / (rv * 1.3)) ** 2 <= 1) & (image[..., 3] > 0)
    image[rim, :3] = STONE[0]
    if lid:
        image[gem & (b < -0.25 * rv), :3] = GEM[1]
        image[gem & (b < -0.6 * rv), :3] = STONE[0]


# 위에서 본 뱀 머리의 큰 비늘판(왼쪽 반, (u, v) 볼록 다각형, v는 뒤통수 −1 ~ 주둥이 1). 오른쪽은 좌우로 비춘다.
# 가운데를 가로지르는 판(주둥이 끝, 정수리 방패판)은 왼쪽 반과 오른쪽 반이 한 판이다.
TOP_PLATES = [
    ("rostral", True, [(0, 0.8), (-0.12, 0.82), (-0.13, 0.95), (0, 0.98)]),
    ("internasal", False, [(0, 0.58), (-0.2, 0.6), (-0.22, 0.8), (0, 0.8)]),
    ("prefrontal", False, [(0, 0.3), (-0.3, 0.33), (-0.33, 0.58), (0, 0.58)]),
    ("frontal", True, [(0, -0.2), (-0.16, -0.16), (-0.19, 0.3), (0, 0.3)]),
    ("supraocular", False, [(-0.2, -0.12), (-0.56, -0.04), (-0.6, 0.28), (-0.34, 0.32), (-0.21, 0.28)]),
    ("parietal", False, [(0, -0.64), (-0.38, -0.56), (-0.4, -0.15), (-0.18, -0.16), (0, -0.2)]),
]


def inside_polygon(u, v, points):
    """(u, v) 격자에서 볼록 다각형 안."""
    inside = np.ones(u.shape, dtype=bool)
    n = len(points)
    area = sum(points[i][0] * points[(i + 1) % n][1] - points[(i + 1) % n][0] * points[i][1] for i in range(n))
    sign = 1 if area > 0 else -1
    for i in range(n):
        (au, av), (bu, bv) = points[i], points[(i + 1) % n]
        inside &= ((bu - au) * (v - av) - (bv - av) * (u - au)) * sign >= 0
    return inside


def head_plates(u, v, rng):
    """비늘판 번호 지도와 판 사이 홈. 큰 판 밖은 작은 비늘(보로노이)."""
    plate = np.full(u.shape, -1)
    for k, (_, shared, points) in enumerate(TOP_PLATES):
        plate[inside_polygon(u, v, points)] = k
        plate[inside_polygon(-u, v, points)] = k if shared else k + 100
    height, width = u.shape
    _, small = terrain.cells(height, width, 9, rng)
    differs = np.zeros_like(small)
    differs[:, 1:] |= plate[:, 1:] != plate[:, :-1]
    differs[1:, :] |= plate[1:, :] != plate[:-1, :]
    groove = differs | (small & (plate < 0))
    return plate, groove


def head_top(width, height, rng=None):
    """
    위에서 내려다본 돌뱀 머리(뱀성 꼭대기, 경비대장과 싸우는 자리). 위(북쪽)는 목으로 이어지고, 아래(남쪽)는 둥근 주둥이.
    정수리 방패판·눈 위 판·콧등 판 같은 큰 비늘판을 좌우 대칭으로 새기고, 양옆에 튀어나온 눈두덩과 보석 눈,
    주둥이 끝에 콧구멍. 남쪽 가장자리 아래로는 머리 옆면이 보인다.
    돌려주는 값: (RGBA, 걸을 수 있는 윗면 마스크, 눈 자리 [(x, y, 반지름)] 점 단위).
    """
    rng = rng or np.random.default_rng(11)
    yy, xx = np.mgrid[0:height, 0:width]
    u = (xx + 0.5 - width / 2) / (width / 2)
    v = (yy + 0.5) / height * 2 - 1
    back = np.sqrt(np.clip(1 - ((v + 0.45) / 0.55) ** 2, 0, 1))
    taper = 1 - 0.55 * np.clip((v + 0.45) / 1.4, 0, 1) ** 1.3
    half = np.where(v < -0.45, np.maximum(back, 0.5), taper) * 0.92
    snout = np.clip((v - 0.7) / 0.26, 0, 1)
    half = half * np.sqrt(np.clip(1 - snout ** 2, 0, 1))
    top = (np.abs(u) < half) & (v < 0.96)

    eye_u, eye_v = 0.7, 0.12
    t = np.clip(u / np.maximum(half, 1e-3), -1, 1)
    z = np.sqrt(np.clip(1 - t ** 2, 0, 1))
    eye_zone = np.zeros_like(top)
    for side in (-1, 1):
        z += 0.3 * ridge(u, v, (side * 0.42, -0.06), (side * 0.74, 0.0), 0.06)
        z += 0.25 * gauss(u, v, side * eye_u, eye_v, 0.09, 0.12)
        eye_zone |= gauss(u, v, side * eye_u, eye_v, 0.11, 0.15) > 0.35
        z -= 0.25 * gauss(u, v, side * 0.16, 0.86, 0.04, 0.03)
    plate, groove = head_plates(u, v, rng)
    z = z - 0.08 * (groove & ~eye_zone)
    dz_dy, dz_dx = np.gradient(z * width * 0.5)
    normal = np.dstack([-dz_dx, -dz_dy, np.full_like(z, 7.0)])
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    light = np.clip(normal @ LIGHT * 0.95 + 0.12, 0, 1)
    light = np.where(groove & ~eye_zone, light * 0.62, light)
    # 큰 판은 조금 밝게 다듬은 돌에 돌결, 홈 바로 아래·오른쪽은 깎은 모서리가 빛을 받고 위·왼쪽은 그늘이 진다
    grain = pg.noise(height, width, 3, rng) - 0.5
    light = np.where(plate >= 0, light * 1.06 + grain * 0.08, light * 0.96)
    after = np.zeros_like(groove)
    after[2:, 2:] = groove[:-2, :-2]
    before = np.zeros_like(groove)
    before[:-2, :-2] = groove[2:, 2:]
    light = np.where(after & ~groove & (plate >= 0), light + 0.12, light)
    light = np.where(before & ~groove & (plate >= 0), light - 0.1, light)
    rgb = pg.quantize(np.clip(light, 0, 1), STONE, dither=0.1)
    moss = (pg.noise(height, width, 11, rng) > 0.86) & (plate < 0) & top & ~eye_zone
    rgb[moss] = MOSS[1]

    image = np.zeros((height, width, 4), dtype=np.uint8)
    # 남쪽 가장자리 아래로 보이는 머리 옆면(아래로 갈수록 어둡다)
    for k in range(1, 24):
        shifted = np.zeros_like(top)
        shifted[k:] = top[:-k]
        face = shifted & ~top & (v > -0.5) & (image[..., 3] == 0)
        image[face, :3] = STONE[max(0, 3 - k // 8)]
        image[face, 3] = 255
    image[top, :3] = rgb[top]
    image[top, 3] = 255
    eyes = []
    for side in (-1, 1):
        gem_eye(image, u, v, side * eye_u, eye_v, 0.065, 0.1, tilt=side * 0.3)
        eyes.append((width / 2 + side * eye_u * width / 2, (eye_v + 1) / 2 * height, 0.1 * width / 2))
        pit = (gauss(u, v, side * 0.16, 0.86, 0.03, 0.022) > 0.5) & top
        image[pit, :3] = STONE[0]
    walk = top & ~eye_zone
    return pg.outline(image), walk, eyes


def head_front(width=232, rng=None):
    """
    정면을 보며 조금 내려다보는 돌뱀 머리. 위는 넓적한 머리통, 아래로 좁아져 둥근 주둥이가 된다.
    눈두덩은 안쪽이 낮고 바깥이 높은 V자로 솟아 노려보는 얼굴이 된다.
    """
    rng = rng or np.random.default_rng(7)
    head_h = int(width * 1.0)
    fang_room = int(width * 0.16)
    height = head_h + fang_room
    yy, xx = np.mgrid[0:height, 0:width]
    u = (xx + 0.5 - width / 2) / (width / 2)
    v = (yy + 0.5) / head_h * 2 - 1

    # 윤곽: 머리통 위는 둥글고, 관자놀이(v ≈ −0.2)에서 가장 넓고, 아래로 좁아져 둥근 주둥이
    crown = np.sqrt(np.clip(1 - ((v + 0.2) / 0.8) ** 2, 0, 1))
    taper = 1 - 0.66 * np.clip((v + 0.2) / 1.2, 0, 1) ** 1.25
    half = np.where(v < -0.2, crown, taper) * 0.97
    snout = np.clip((v - 0.62) / 0.32, 0, 1)
    half = half * np.sqrt(np.clip(1 - snout ** 2, 0, 1))
    inside = (np.abs(u) < half) & (v > -1) & (v < 0.94)

    # 높이: 둥근 지붕 꼴 머리, 가운데 등줄, 눈두덩, 눈구멍, 콧구멍
    t = np.clip(u / np.maximum(half, 1e-3), -1, 1)
    z = np.sqrt(np.clip(1 - t ** 2, 0, 1)) * (0.85 + 0.15 * np.clip(-v, 0, 1))
    z += 0.06 * np.exp(-(u / 0.06) ** 2) * (v < 0.55)
    for side in (-1, 1):
        z += 0.42 * ridge(u, v, (side * 0.14, 0.0), (side * 0.76, -0.24), 0.08)
        z -= 0.42 * gauss(u, v, side * 0.48, 0.08, 0.17, 0.11)
        z -= 0.2 * gauss(u, v, side * 0.17, 0.72, 0.05, 0.035)
    # 비늘판: 머리 윗면은 큰 판, 옆과 주둥이는 작은 비늘. 판 사이 홈을 판다
    _, big = terrain.cells(height, width, 16, rng)
    _, small = terrain.cells(height, width, 7, rng)
    half_w = width // 2
    big = np.hstack([big[:, :half_w], big[:, :half_w][:, ::-1]])[:, :width]
    plates = np.where((np.abs(u) < 0.4) & (v < 0.45), big, small)
    plates |= (np.abs(u) < 0.012) & (v < 0.45)
    eye_zone = np.zeros_like(inside)
    for side in (-1, 1):
        eye_zone |= gauss(u, v, side * 0.48, 0.06, 0.2, 0.15) > 0.35
    z -= 0.07 * (plates & ~eye_zone)

    # 빛: 높이의 기울기로 면 방향을 구한다
    dz_dy, dz_dx = np.gradient(z * width * 0.5)
    normal = np.dstack([-dz_dx, -dz_dy, np.full_like(z, 7.0)])
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    light = np.clip(normal @ LIGHT * 0.95 + 0.12, 0, 1)
    light = np.where(plates & ~eye_zone, light * 0.72, light)
    rgb = pg.quantize(light, STONE, dither=0.12)

    # 풍화: 눈 아래로 흘러내린 물때, 머리 위 이끼
    for side in (-1, 1):
        stain = (np.abs(u - side * 0.48) < 0.05 + 0.02 * np.sin(v * 30)) & (v > 0.15) & (v < 0.6)
        rgb[stain & inside] = (rgb[stain & inside] * 0.78).astype(np.uint8)
    moss = (pg.noise(height, width, 9, rng) > 0.78) & (v < -0.4) & (light > 0.45)
    rgb[moss] = MOSS[1 + (light[moss] > 0.7)]

    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[inside, :3] = rgb[inside]
    image[inside, 3] = 255

    # 보석 눈: 노려보는 비스듬한 타원, 세로 동공 홈, 안쪽에서 번지는 빛
    for side in (-1, 1):
        cu, cv = side * 0.48, 0.09
        ru, rv = 0.15, 0.072
        tilt = side * -0.32
        du, dv = u - cu, v - cv
        ru_ = du * np.cos(tilt) + dv * np.sin(tilt)
        rv_ = -du * np.sin(tilt) + dv * np.cos(tilt)
        gem = (ru_ / ru) ** 2 + (rv_ / rv) ** 2 <= 1
        depth = (ru_ / ru) ** 2 + (rv_ / rv) ** 2
        level = np.select([depth < 0.15, depth < 0.45, depth < 0.8], [5, 4, 3], 2)
        level = np.where((rv_ > 0.35 * rv) & (level > 2), level - 1, level)
        image[gem, :3] = GEM[level[gem]]
        slit = gem & (np.abs(ru_) < 0.018)
        image[slit, :3] = GEM[0]
        glint = gem & (np.hypot((ru_ + 0.06) / 0.03, (rv_ + 0.035) / 0.025) < 1)
        image[glint, :3] = GEM[5]
        rim = ~gem & ((ru_ / (ru + 0.03)) ** 2 + (rv_ / (rv + 0.03)) ** 2 <= 1) & inside
        image[rim, :3] = STONE[0]
        # 눈두덩 그늘이 보석 위쪽을 덮어 노려보는 눈매가 된다
        lid = gem & (rv_ < -0.25 * rv)
        image[lid, :3] = GEM[1]
        image[gem & (rv_ < -0.6 * rv), :3] = STONE[0]

    # 콧구멍
    for side in (-1, 1):
        pit = gauss(u, v, side * 0.17, 0.72, 0.045, 0.03) > 0.5
        image[pit & inside, :3] = STONE[0]

    # 다문 입: 위턱 끝 홈과 그 아래 엄니 둘(위턱에서 아래로 길게, 끝이 안쪽으로 굽는다)
    lip = inside & (v > 0.86) & (v < 0.94) & (np.abs(u) < 0.3)
    image[lip, :3] = STONE[0]
    for side in (-1, 1):
        root_u = side * 0.24
        for k in range(fang_room + int(head_h * 0.06)):
            y = int(head_h * (1 + 0.86) / 2) - 2 + k
            if y >= height:
                break
            frac = k / (fang_room + head_h * 0.06)
            half_w = (1 - frac) * width * 0.035 + 0.6
            cx = width / 2 + (root_u - side * 0.05 * frac ** 2) * width / 2
            x0, x1 = int(round(cx - half_w)), int(round(cx + half_w))
            image[y, x0:x1 + 1, :3] = STONE[6]
            image[y, x0:x1 + 1, 3] = 255
            image[y, x0:x0 + max(1, (x1 - x0) // 3), :3] = STONE[7]
            image[y, x1, :3] = STONE[4]
    return pg.outline(image), (width // 2, height - 1)
