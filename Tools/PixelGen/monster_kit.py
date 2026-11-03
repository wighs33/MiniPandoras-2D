"""몬스터 그림의 공통 재료(monsters.py가 쓴다): 점 격자 위의 둥근 덩이·팔다리 명암, 눈, 테두리, 세 방향 걸음 시트 저장.

웹게임 인물 그림과 같은 규격이다. 논리 px 하나 = 점 둘이고, 점을 다시 두 배로 키워 논리 px 하나 = 4px가 된다.
아래·옆(오른쪽)·위 세 방향에 세 장씩 그린다: 0은 선 모습, 1·2는 엇갈린 걸음(게임이 걷는 동안 1·0·2·0 순서로 넘긴다).
기준점은 발밑 가운데이고, 왼쪽은 게임이 오른쪽을 뒤집어 쓴다. 땅 그림자는 게임이 그리므로 그림에 넣지 않는다.
떠다니는 몬스터는 그림 아래를 비워 두어 그림자 위에 떠 보이게 한다. 빛은 왼쪽 위에서 온다(pixelgen.LIGHT).
"""
import json

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

D = 2                                   # 논리 px 하나의 점 수
VIEWS = ("down", "right", "up")
FRAMES = 3
GLINT = np.array([255, 255, 255], dtype=np.uint8)


class FineCanvas(Canvas):
    """
    점 좌표 그대로 그리되 점 하나를 2×2 px로 곱게 칠하는 그림. 웹게임 인물 그림처럼 논리 px 하나 = 4px를 다 써서,
    생물 도감의 작은 눈·가는 입이 뭉개지지 않는다. 모양 판정(ellipse·polygon·limb …)은 px 가운데를 점 좌표로 바꾼 xx·yy로 하고,
    px 단위 무늬(체크 무늬)는 ix·iy로 한다. 다 그린 뒤 다시 키우지 않는다(save_sheet).
    """

    def __init__(self, width, height, scale=2):
        super().__init__(width * scale, height * scale)
        self.iy, self.ix = self.yy.copy(), self.xx.copy()
        self.yy = (self.iy + 0.5) / scale - 0.5
        self.xx = (self.ix + 0.5) / scale - 0.5


def canvas(width, height, fine=False):
    """논리 px 크기로 빈 그림을 만든다(좌표는 점 단위). fine이면 점 하나를 2×2 px로 곱게 칠한다(FineCanvas)."""
    return FineCanvas(width * D, height * D) if fine else Canvas(width * D, height * D)


def light_of(nx, ny, nz, bias=0.0):
    """겉면 방향(nx, ny, nz)이 받는 빛(0~1). 정면은 0.5보다 조금 밝다."""
    return np.clip((nx * pg.LIGHT[0] + ny * pg.LIGHT[1] + nz * pg.LIGHT[2]) * 0.55 + 0.45 + bias, 0, 0.999)


def ball(c, cx, cy, rx, ry, palette, bias=0.0, dither=0.12, clip=None):
    """명암을 넣은 둥근 덩이(타원체를 위에서 비춘 모습). 돌려주는 값: 칠한 곳."""
    nx = (c.xx + 0.5 - cx) / rx
    ny = (c.yy + 0.5 - cy) / ry
    r2 = nx ** 2 + ny ** 2
    mask = r2 <= 1
    if clip is not None:
        mask &= clip
    nz = np.sqrt(np.clip(1 - r2, 0, 1))
    c.paint(mask, pg.quantize(light_of(nx, ny, nz, bias), palette, dither))
    return mask


def blob(c, cx, cy, radius, palette, wobble=0.0, lobes=0, phase=0.0, squash=1.0, bias=0.0, dither=0.12):
    """가장자리가 물결치는 둥근 덩이(털뭉치·거품·불꽃 덩이). lobes 번 굽이치고 wobble 점만큼 들쭉날쭉하다."""
    dx = c.xx + 0.5 - cx
    dy = (c.yy + 0.5 - cy) / squash
    angle = np.arctan2(dy, dx)
    edge = radius + wobble * np.sin(angle * lobes + phase)
    r = np.hypot(dx, dy)
    mask = r <= edge
    t = np.clip(r / np.maximum(edge, 1e-6), 0, 1)
    nz = np.sqrt(np.clip(1 - t ** 2, 0, 1))
    nx, ny = dx / np.maximum(edge, 1e-6), dy / np.maximum(edge, 1e-6)
    c.paint(mask, pg.quantize(light_of(nx, ny, nz, bias), palette, dither))
    return mask


def limb(c, p0, p1, r0, r1, palette, bias=0.0, dither=0.1):
    """끝으로 갈수록 굵기가 바뀌는 둥근 막대(팔다리·덩굴·촉수). 가로지르는 방향으로 명암을 넣는다."""
    (x0, y0), (x1, y1) = p0, p1
    dx, dy = x1 - x0, y1 - y0
    length2 = max(dx * dx + dy * dy, 1e-6)
    t = np.clip(((c.xx + 0.5 - x0) * dx + (c.yy + 0.5 - y0) * dy) / length2, 0, 1)
    ox, oy = c.xx + 0.5 - (x0 + t * dx), c.yy + 0.5 - (y0 + t * dy)
    r = r0 + (r1 - r0) * t
    d = np.hypot(ox, oy)
    mask = d <= r
    nx, ny = ox / np.maximum(r, 1e-6), oy / np.maximum(r, 1e-6)
    nz = np.sqrt(np.clip(1 - nx ** 2 - ny ** 2, 0, 1))
    c.paint(mask, pg.quantize(light_of(nx, ny, nz, bias), palette, dither))
    return mask


def chain(c, points, r0, r1, palette, bias=0.0):
    """점들을 차례로 잇는 굽은 막대. 굵기는 처음 r0에서 끝 r1로 고르게 바뀐다."""
    mask = np.zeros(c.xx.shape, dtype=bool)
    n = len(points) - 1
    for i in range(n):
        ra = r0 + (r1 - r0) * i / n
        rb = r0 + (r1 - r0) * (i + 1) / n
        mask |= limb(c, points[i], points[i + 1], ra, rb, palette, bias)
    return mask


def tilted(c, cx, cy, rx, ry, angle):
    """기울인 타원 안(날개·잎). angle은 라디안, rx 축이 그 각도를 향한다. 돌려주는 값: (안, 축 방향 거리 −1~1, 옆 거리 −1~1)."""
    dx, dy = c.xx + 0.5 - cx, c.yy + 0.5 - cy
    u = (dx * np.cos(angle) + dy * np.sin(angle)) / rx
    v = (-dx * np.sin(angle) + dy * np.cos(angle)) / ry
    return u ** 2 + v ** 2 <= 1, u, v


def feather(c, root, tip, width, palette, bias=0.0, core=None):
    """깃·불꽃 한 가닥: 뿌리에서 끝으로 부풀었다 가늘어지는 잎꼴. core가 있으면 가운데 줄기를 그 색으로 긋는다.
    빛은 뿌리 쪽과 가운데가 밝다(불꽃은 palette를 어두운 바깥 → 밝은 속 차례로 준다)."""
    (x0, y0), (x1, y1) = root, tip
    dx, dy = x1 - x0, y1 - y0
    length = max(np.hypot(dx, dy), 1e-6)
    ux, uy = dx / length, dy / length
    t = ((c.xx + 0.5 - x0) * ux + (c.yy + 0.5 - y0) * uy) / length
    side = (-(c.xx + 0.5 - x0) * uy + (c.yy + 0.5 - y0) * ux) / width
    half = np.sin(np.clip(t, 0, 1) * np.pi) ** 0.6 * 0.5 + 0.04
    mask = (t >= 0) & (t <= 1) & (np.abs(side) <= half)
    light = 0.85 - np.abs(side) / np.maximum(half, 1e-6) * 0.45 - t * 0.25 + bias
    shade(c, mask, light, palette)
    if core is not None:
        c.paint(mask & (np.abs(side) < 0.06) & (t < 0.85), core)
    return mask


def shade(c, mask, light, palette, dither=0.12):
    """칠할 곳에 빛 세기(배열 또는 수)를 팔레트로 칠한다."""
    light = np.broadcast_to(np.clip(light, 0, 0.999), c.xx.shape)
    c.paint(mask, pg.quantize(light, palette, dither))


def vertical_light(c, top, bottom, bias=0.0):
    """위가 밝고 아래가 어두운 빛(평평한 면의 명암)."""
    return np.clip(0.85 - (c.yy - top) / max(bottom - top, 1) * 0.6 + bias, 0, 0.999)


def eye(c, x, y, r, iris, pupil=None, white=None, glint=True):
    """눈 하나: 흰자(있으면), 홍채, 동공, 왼쪽 위 빛점."""
    if white is not None:
        c.paint(c.ellipse(x, y, r, r), white)
        c.paint(c.ellipse(x, y, r * 0.65, r * 0.65), iris)
    else:
        c.paint(c.ellipse(x, y, r, r), iris)
    if pupil is not None:
        c.paint(c.ellipse(x, y, r * 0.35, r * 0.35), pupil)
    if glint:
        c.paint(c.ellipse(x - r * 0.35, y - r * 0.35, max(0.6, r * 0.28), max(0.6, r * 0.28)), GLINT)


# 통통 뛰는 생물의 걸음(가로 배율, 세로 배율, 뜬 높이 점): 0 선 모습, 1 땅에 눌림, 2 늘어나며 뜸. 게임이 1·0·2·0 차례로 넘긴다.
HOP = ((1.0, 1.0, 0.0), (1.12, 0.86, 0.0), (0.9, 1.1, 4.0))


def glow_eye(c, x, y, rx, ry, colors):
    """빛나는 눈(어둠 속 빛 두 점 같은 것): 바깥 테에서 가운데로 갈수록 밝다. colors는 어두운 것부터."""
    for k, color in enumerate(colors):
        s = 1 - k / len(colors)
        c.paint(c.ellipse(x, y, max(0.6, rx * s), max(0.6, ry * s)), color)


def spark(c, x, y, size, color, core=GLINT):
    """십자 반짝임(더스트 알갱이·빛가루)."""
    c.paint((np.abs(c.xx + 0.5 - x) < 0.6) & (np.abs(c.yy + 0.5 - y) <= size), color)
    c.paint((np.abs(c.yy + 0.5 - y) < 0.6) & (np.abs(c.xx + 0.5 - x) <= size), color)
    c.paint((np.abs(c.xx + 0.5 - x) < 0.6) & (np.abs(c.yy + 0.5 - y) < 0.6), core)


def stripes(c, mask, period, color, offset=0, slope=0.0):
    """칠한 곳 위에 period 점마다 줄(볏짚 결·책장·판자).slope는 줄의 기울기."""
    c.paint(mask & (np.mod(np.floor(c.xx + c.yy * slope) + offset, period) == 0), color)


def speckle(c, mask, color, density, seed, shift=(0, 0)):
    """칠한 곳에 흩뿌린 점 무늬(녹·이끼·모래알). 무늬는 seed로 정해지고 몸이 움직인 만큼(shift = (dx, dy)) 함께 밀려,
    장마다 무늬가 떨리지 않는다."""
    field = np.random.default_rng(seed).random(c.xx.shape)
    field = np.roll(field, (shift[1], shift[0]), axis=(0, 1))
    c.paint(mask & (field < density), color)


def mirror(c):
    """좌우를 뒤집는다(아래·위 그림을 좌우 대칭으로 그린 뒤 비대칭 장식만 따로 얹을 때)."""
    c.image = c.image[:, ::-1].copy()


def _neighbour_colour(image, alpha):
    """px마다 네 이웃 가운데 칠한 곳 색의 평균."""
    rgb = np.pad(image[..., :3].astype(float) * alpha[..., None], ((1, 1), (1, 1), (0, 0)))
    count = np.pad(alpha.astype(float), 1)
    total = rgb[:-2, 1:-1] + rgb[2:, 1:-1] + rgb[1:-1, :-2] + rgb[1:-1, 2:]
    n = count[:-2, 1:-1] + count[2:, 1:-1] + count[1:-1, :-2] + count[1:-1, 2:]
    return total / np.maximum(n, 1)[..., None]


def finish(c, outline, width=1):
    """
    바깥에 진한 테두리를 width 점 두께로 두르고 RGBA 점 그림을 돌려준다.
    그림에 outline_tint(0~1)가 있으면 테두리를 맞닿은 곳 색의 어두운 쪽으로 그만큼 물들인다.
    흰 솜·노란 꽃까지 한 갈색 테에 갇히면 무겁고 탁해 보여서다(도트 그림의 '셀아웃').
    """
    image = c.image
    tint = getattr(c, "outline_tint", 0.0)
    for k in range(width):
        alpha = image[..., 3] > 0
        ring = pg.neighbours(alpha) & ~alpha
        color = np.asarray(outline, dtype=float)
        if tint:
            near = _neighbour_colour(image, alpha)[ring]
            color = near if k else color * (1 - tint) + near * 0.45 * tint
        image[ring, :3] = np.asarray(color).astype(np.uint8)
        image[ring, 3] = 255
    return image


def save_sheet(folder, monster_id, size, draw, outline, outline_width=1):
    """
    세 방향 세 장을 그려 웹게임 인물 시트처럼 저장한다(down_0.png … sheet.json). 돌려주는 값: 방향별 그림(최종 크기).
    보통 그림은 점을 두 배로 키우고, 곱게 칠한 그림(FineCanvas)은 이미 최종 크기라 그대로 둔다. 테두리 두께는 그 그림의 px 단위다.
    """
    folder.mkdir(parents=True, exist_ok=True)
    width, height = size
    sheets = {}
    for view in VIEWS:
        frames = []
        for frame in range(FRAMES):
            drawn = draw(view, frame)
            image = finish(drawn, outline, outline_width)
            if not isinstance(drawn, FineCanvas):
                image = pg.upscale(image)
            Image.fromarray(image, "RGBA").save(folder / f"{view}_{frame}.png")
            frames.append(image)
        sheets[view] = frames
    sheet = {
        "id": monster_id, "width": width, "height": height, "anchorX": width // 2, "anchorY": height - 1,
        "framesDown": FRAMES, "framesRight": FRAMES, "framesUp": FRAMES, "hasPortrait": False,
    }
    (folder / "sheet.json").write_text(json.dumps(sheet, ensure_ascii=False, indent=1), encoding="utf-8")
    return sheets
