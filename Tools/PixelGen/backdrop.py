"""맵 밖에 보이는 배경을 코드로 그린다: 차원공간(별·성운·차원의 고리·멀리 떠 있는 바위섬).

배경은 카메라를 따라다니는 한 장(화면보다 조금 큰 26×15칸)이다. 별이 반짝이거나 빛이 천천히 떠도는 움직임은
Unity의 MapBackdrop이 이 그림 위에 얹는다. 웹게임 차원공간(field.renderBackground "void", AwakenScene.drawSpace)의
색과 구성을 따랐다.
"""
import numpy as np

import pixelgen as pg

WIDTH, HEIGHT = 26 * pg.TILE, 15 * pg.TILE

# 하늘 바탕(어둠 → 보랏빛)과 성운 색. 점마다 이 팔레트에서 가장 가까운 색으로 고른다.
SKY = pg.hexes("#05040f", "#0a0820", "#110c2c", "#1a1340", "#241a55", "#30226a", "#3d2c80",
               "#2a1a4a", "#3c2160", "#57307a", "#7a4294", "#16244a", "#1f3a6c", "#2e5590", "#4a3a9c", "#6a58c0")
STARS = pg.hexes("#ffffff", "#dfe6ff", "#bfe6ff", "#d8c8ff", "#a99be0")
ROCK = pg.hexes("#0b0918", "#161230", "#221b45", "#30285c", "#40367a", "#5a4f96")


def nearest(colors, palette):
    """RGB(0~255 실수) 그림의 점마다 팔레트에서 가장 가까운 색."""
    flat = colors.reshape(-1, 3)
    distance = ((flat[:, None, :] - palette[None, :, :].astype(float)) ** 2).sum(axis=2)
    return palette[distance.argmin(axis=1)].reshape(colors.shape).astype(np.uint8)


def glow(height, width, cx, cy, radius):
    """가운데가 1이고 반지름에서 0이 되는 부드러운 빛(제곱으로 줄어든다, 웹게임 glowCircle)."""
    yy, xx = np.mgrid[0:height, 0:width].astype(float)
    rest = np.clip(1 - np.hypot(xx - cx, yy - cy) / radius, 0, 1)
    return rest * rest


def void(seed=20261102):
    rng = np.random.default_rng(seed)
    h, w = HEIGHT, WIDTH
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    # 바탕: 위 가운데가 가장 밝은 보랏빛, 가장자리로 갈수록 어둠(AwakenScene의 방사 그라데이션)
    reach = np.hypot((xx - w * 0.5) / w, (yy - h * 0.32) / h * 1.4)
    base = np.clip(1 - reach * 1.35, 0, 1)
    color = np.stack([5 + base * 55, 4 + base * 40, 15 + base * 110], axis=2)
    # 성운: 웹게임 배경의 세 빛(보라, 파랑, 분홍)을 잡음으로 찢어 구름처럼
    clouds = pg.fractal_noise(h, w, rng, ((90, 0.5), (36, 0.3), (12, 0.2)))
    for (u, v, radius, rgb) in ((0.25, 0.35, 0.42, (120, 70, 210)), (0.78, 0.62, 0.38, (50, 100, 210)), (0.5, 0.08, 0.3, (190, 80, 170))):
        amount = glow(h, w, u * w, v * h, radius * w) * np.clip(clouds * 1.6 - 0.45, 0, 1)
        color += amount[..., None] * np.array(rgb) * 0.75
    # 차원의 고리: 기운 타원 셋, 점선
    for i in range(3):
        angle = 0.12 * (1 if i % 2 else -1) + i * 0.05
        a, b = (150 + i * 36) * 2, (44 + i * 14) * 2
        dx, dy = xx - w * 0.5, yy - h * 0.46
        rx = dx * np.cos(angle) + dy * np.sin(angle)
        ry = -dx * np.sin(angle) + dy * np.cos(angle)
        ring = np.abs(np.hypot(rx / a, ry / b) - 1) < 0.006
        dotted = ring & ((np.round(np.arctan2(ry / b, rx / a) * 120) % 3) == 0)
        color[dotted] += np.array([90, 80, 150]) * (0.35 + i * 0.12)
    color += (pg.bayer(h, w)[..., None] - 0.5) * 14
    image = nearest(np.clip(color, 0, 255), SKY)
    islands(image, rng)
    stars(image, rng)
    return image


def eye_level_floor(ground, eye, horizon=112.0, depth=104.0, spread=75.0):
    """
    맵 바닥(점 격자 RGBA)을 그 칸에 누운 눈높이에서 북쪽으로 바라본 모습. 화면 384×216 논리 px(= 768×432점)이고,
    지평선(horizon) 아래로 가까운 바닥은 크게, 먼 바닥은 작게 그린다(원근). 돌섬 밖은 투명해 배경이 비친다.
    - 화면 y의 바닥까지 거리 z = depth / (y − horizon) 칸, 화면 x의 옆 거리 = (x − 192) × z / spread 칸.
    - 멀수록 차원공간 빛(FOG)이 섞인다. 한 점마다 2×2로 나눠 봐 먼 곳이 지글거리지 않게 한다.
    """
    fog = np.array([30.0, 22.0, 64.0])
    height, width = 216 * 2, 384 * 2
    out = np.zeros((height, width, 4), dtype=float)
    ys, xs = np.mgrid[0:height, 0:width].astype(float)
    for oy in (0.25, 0.75):
        for ox in (0.25, 0.75):
            sy, sx = (ys + oy) / 2, (xs + ox) / 2
            below = sy > horizon + 0.5
            z = np.where(below, depth / np.maximum(sy - horizon, 0.5), np.inf)
            mx = (eye[0] + (sx - 192) * z / spread) * pg.TILE
            my = (eye[1] - z) * pg.TILE
            inside = below & (mx >= 0) & (my >= 0) & (mx < ground.shape[1]) & (my < ground.shape[0])
            sample = np.zeros((height, width, 4))
            sample[inside] = ground[my[inside].astype(int), mx[inside].astype(int)]
            alpha = sample[..., 3:4] / 255
            haze = np.clip((z - 1.5) / 7, 0, 0.6)[..., None]
            rgb = sample[..., :3] * (1 - haze) + fog * haze
            out[..., :3] += rgb * alpha / 4
            out[..., 3:4] += alpha / 4
    # 겹친 평균은 알파가 곱해진 색이라 알파로 나눠 되돌린다.
    covered = out[..., 3] > 0
    out[covered, :3] /= out[covered, 3:4]
    out[..., 3] *= 255
    return np.clip(out, 0, 255).astype(np.uint8)


def islands(image, rng):
    """멀리 떠 있는 작은 바위섬 몇 개(윗면은 밝고 아래로 뾰족하게 좁아진다)."""
    h, w = image.shape[:2]
    for cx, cy, size in ((0.12, 0.78, 34), (0.86, 0.22, 24), (0.7, 0.88, 18), (0.33, 0.12, 14)):
        cx, cy = int(cx * w), int(cy * h)
        half = size // 2
        for x in range(-half, half + 1):
            u = x / half
            top = int(round(-2 - 2 * (1 - u * u)))
            depth = int(round(size * 0.55 * np.sqrt(max(0, 1 - u * u)) * rng.uniform(0.75, 1.0)))
            for y in range(top, depth):
                level = 4 if y <= top + 1 else 3 if u < -0.3 else 2 if y < depth * 0.5 else 1
                image[cy + y, cx + x] = ROCK[level]
            image[cy + depth, cx + x] = ROCK[0]


def stars(image, rng):
    """별: 대부분 한 점, 몇은 밝은 십자. 반짝임은 Unity가 따로 얹는다."""
    h, w = image.shape[:2]
    for _ in range(260):
        x, y = rng.integers(1, w - 1), rng.integers(1, h - 1)
        image[y, x] = STARS[rng.choice([1, 2, 3, 4, 4, 4])]
    for _ in range(14):
        x, y = rng.integers(2, w - 2), rng.integers(2, h - 2)
        image[y, x] = STARS[0]
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            image[y + dy, x + dx] = STARS[3]
