"""코드로 그리는 도트 그림의 공통 재료: 점 격자, 팔레트, 잡음, 디더, 테두리.

게임 그림은 한 칸 64px이고 점 하나가 2px쯤으로 보인다(웹게임 인물 그림과 맞춘다). 그래서 한 칸 32점 격자에 그린 뒤
2배로 키운다. 모든 무작위는 seed에서 나와 다시 돌려도 같은 그림이 나온다.
지형은 terrain.py, 소품은 props.py, 맵 한 장 다시 그리기는 redraw_map.py에 있다.
"""
import numpy as np

TILE = 32          # 한 칸의 점 수
SCALE = 2          # 게임 그림은 점 하나 = 2px(한 칸 64px)
PERIOD = 4         # 끝없이 잇는 무늬(시범 장면)는 4×4칸마다 되풀이한다


def hexes(*codes):
    return np.array([[int(c[i:i + 2], 16) for i in (1, 3, 5)] for c in codes], dtype=np.uint8)


# 지금 게임의 인물 그림보다 너무 쨍하지 않게, 노란 기가 도는 차분한 색으로 맞췄다.
GRASS = hexes("#1f3324", "#2b472c", "#395f33", "#4a7539", "#5f8c40", "#7ea24b", "#a9bd63")
DIRT = hexes("#3d2c23", "#58402f", "#73563c", "#8d6c4a", "#a5845a", "#c0a274")
SAND = hexes("#6e5a3e", "#8e7650", "#ad9464", "#c7ae7a", "#dcc896")
STONE = hexes("#2f2f38", "#474855", "#62646f", "#82858f", "#a7aab3", "#cfd1d6")
WATER = hexes("#13293a", "#1a3a4f", "#22506a", "#2d6683", "#3f7f9c", "#6aa7bf", "#a9d3e0")
BARK = hexes("#231712", "#3a261c", "#553828", "#734d33", "#8f6440")
LEAF = hexes("#1b2e20", "#26412a", "#335a31", "#447337", "#588b3d", "#74a347", "#9cbd5a")
NEEDLE = hexes("#152520", "#1d342a", "#274536", "#335a42", "#43704f", "#5a8a5f")
WOOD = hexes("#2b1d16", "#4a3324", "#6b4a33", "#8c6646", "#ad855c")
OUTLINE = np.array([20, 22, 18], dtype=np.uint8)
FLOWERS = hexes("#e8e2cc", "#e9be5e", "#b3a3e0", "#e58f8f")
FLOWER_CORE = np.array([246, 210, 92], dtype=np.uint8)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0
# 빛은 왼쪽 위에서 온다(화면 아래가 +y).
LIGHT = np.array([-0.5, -0.7, 0.52])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


# ── 잡음 ───────────────────────────────────────────────

def smooth(t):
    return t * t * (3 - 2 * t)


def tileable_noise(size, cells, rng):
    """size×size에서 가장자리가 이어지는 부드러운 잡음(0~1). cells는 한 변의 격자 수."""
    grid = rng.random((cells, cells))
    coords = np.arange(size) / size * cells
    i0 = np.floor(coords).astype(int)
    t = smooth(coords - i0)
    i1 = (i0 + 1) % cells
    tx = t[None, :]
    ty = t[:, None]
    top = grid[i0][:, i0] * (1 - tx) + grid[i0][:, i1] * tx
    bottom = grid[i1][:, i0] * (1 - tx) + grid[i1][:, i1] * tx
    return top * (1 - ty) + bottom * ty


def noise(height, width, spacing, rng):
    """height×width의 부드러운 잡음(0~1). spacing은 격자 간격(점)."""
    gy, gx = height // spacing + 2, width // spacing + 2
    grid = rng.random((gy, gx))
    y = np.arange(height) / spacing
    x = np.arange(width) / spacing
    y0, x0 = np.floor(y).astype(int), np.floor(x).astype(int)
    ty, tx = smooth(y - y0)[:, None], smooth(x - x0)[None, :]
    top = grid[y0][:, x0] * (1 - tx) + grid[y0][:, x0 + 1] * tx
    bottom = grid[y0 + 1][:, x0] * (1 - tx) + grid[y0 + 1][:, x0 + 1] * tx
    return top * (1 - ty) + bottom * ty


def fractal_noise(height, width, rng, layers=((48, 0.55), (16, 0.3), (5, 0.15))):
    return sum(noise(height, width, spacing, rng) * weight for spacing, weight in layers)


def bayer(height, width):
    return BAYER[np.arange(height)[:, None] % 4, np.arange(width)[None, :] % 4]


def dither_levels(value, thresholds, spread=0.06):
    """값을 문턱마다 한 단계씩 나눈다. 문턱 근처는 4×4 순서 디더로 섞어 경계가 계단지지 않게 한다."""
    jitter = (bayer(*value.shape) - 0.5) * spread
    return sum((value + jitter > t).astype(int) for t in thresholds)


def quantize(light, palette, dither=0.1):
    """빛 세기(0~1)를 팔레트 단계로 나눈다. 단계 경계는 4×4 순서 디더로 살짝 섞는다."""
    threshold = (bayer(*light.shape) - 0.5) * dither
    index = np.clip(np.floor((light + threshold) * len(palette)), 0, len(palette) - 1).astype(int)
    return palette[index]


# ── 모양 ───────────────────────────────────────────────

def neighbours(mask):
    """네 이웃 가운데 하나라도 mask인 곳(가장자리 밖은 mask가 아니라고 본다)."""
    padded = np.pad(mask, 1)
    return padded[:-2, 1:-1] | padded[2:, 1:-1] | padded[1:-1, :-2] | padded[1:-1, 2:]


def outline(rgba, color=OUTLINE):
    """불투명한 곳 바깥 한 점에 진한 테두리를 두른다."""
    alpha = rgba[..., 3] > 0
    ring = neighbours(alpha) & ~alpha
    out = rgba.copy()
    out[ring, :3] = color
    out[ring, 3] = 255
    return out


def stamp(canvas, sprite, foot_x, foot_y, pivot):
    """RGB 캔버스에 RGBA 그림을 기준점(pivot)을 (foot_x, foot_y)에 맞춰 알파로 겹친다(점 단위)."""
    h, w = sprite.shape[:2]
    left, top = int(round(foot_x - pivot[0])), int(round(foot_y - pivot[1]))
    y0, x0 = max(0, top), max(0, left)
    y1, x1 = min(canvas.shape[0], top + h), min(canvas.shape[1], left + w)
    if y0 >= y1 or x0 >= x1:
        return
    part = sprite[y0 - top:y1 - top, x0 - left:x1 - left]
    alpha = part[..., 3:4] / 255.0
    region = canvas[y0:y1, x0:x1].astype(float)
    canvas[y0:y1, x0:x1] = (region * (1 - alpha) + part[..., :3] * alpha).astype(np.uint8)


def upscale(image):
    """점 격자 그림을 게임 그림 크기로(점 하나 = SCALE×SCALE px)."""
    return np.repeat(np.repeat(image, SCALE, axis=0), SCALE, axis=1)
