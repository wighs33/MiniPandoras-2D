"""11장 나리온을 타고 나는 하늘 컷인 둘의 배경(웹게임 skyRidePanel 하늘길과 EndingScene 엔딩의 자리).

나리온과 탄 일행은 그대로 두고(Tools/WebGame/narion.py가 웹게임 원화로 구운 날갯짓 두 장을 컷인 인물로 넘긴다),
뒤의 하늘과 구름만 도트로 그린다(2026-11-09 사용자 요청: 예전 배경은 둥근 구름 몇 덩이뿐이라 밋밋했다).
두 장면 모두 화면 전체가 창이다(배경 back은 비어 있다). 겹을 깔고 컷인 화면이 겹마다 다른 빠르기로 옆으로 민다(빠르기는 SceneUI의 CutInView).

  skyRide/  하늘길: 페어리가든의 거대고목을 막 떠나 아이로스로 가는 맑은 낮
    sky     띠를 디더로 이은 하늘, 오른쪽 위 해와 햇무리, 높은 새털구름(멈춤)
    forest  발아래 멀리 펼쳐진 페어리가든의 숲: 둥근 나무 머리, 꽃 핀 나무, 굽이치는 강과 호수, 꽃잎 들판, 지평선으로 갈수록 옅어진다
    far     지평선의 뭉게구름 산맥
    near    숲 위에 흩어진 구름 바다 조각(사이로 숲이 보인다)
    front   바로 앞을 스치는 큰 뭉게구름
  ending/   엔딩: 아이로스 쪽으로 해가 지는 저녁
    sky     남보라에서 금빛으로 띠진 노을, 첫 별, 빛을 받은 새털구름, 해와 햇살, 해 앞에서 구름 바다를 뚫고 선 아이로스의 탑 그림자(멈춤)
    far     지평선의 구름 바다와 솟은 적란운
    near    가까운 구름 바다의 물결
    front   가까이 지나가는 구름 덩이(그늘진 몸에 금빛 테)

구름은 둥근 덩이를 겹친 것이다. 덩이마다 공의 면 방향으로 빛을 받아 팔레트 단계로 나누고(단계 경계는 4×4 디더),
앞 덩이 가장자리에 걸린 뒤 덩이에는 그늘 주름을, 빛 쪽 윤곽에는 밝은 테를 그린다. 아래쪽 덩이가 앞에 온다.
반투명은 쓰지 않는다(Unity는 선형 색 공간에서 섞어 웹게임과 색이 달라진다). 부드러운 가장자리는 불투명한 점을 디더 무늬로 흩어 낸다.
sky는 화면 크기, 나머지 겹은 화면 너비의 두 배로 가로로 되풀이한다(점 하나 = 가상 px 반, 두 배로 키운다).

사용: python Tools/PixelGen/cutin_sky.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from cutin_gods import color
from cutin_train import grid, stepped

W, H = 768, 432                    # 화면(가상 384×216)의 점 격자
STRIP = W * 2                      # 옆으로 되풀이하는 겹의 너비
SY, SX = grid(H, W)                # 하늘(화면 한 장)의 점 좌표
YY, XX = grid(H, STRIP)            # 되풀이하는 겹의 점 좌표
NO_FLOOR = 10 ** 6

# 컷인에서 나리온이 서는 자리(가상 px, 그림 아래 가운데)와 키. Chapter11.SkyRideBird*, StoryScenes.EndingBird*와 같다(미리보기에만 쓴다).
SKY_RIDE_BIRD = ((192, 160), 136)
ENDING_BIRD = ((214, 175), 75)


def dot(virtual):
    return virtual * 2


def tones(*codes):
    return np.array([color(c) for c in codes])


# ── 하늘 ───────────────────────────────────────────────

def band_sky(stops, bands):
    """하늘 한 장: 세로 그라디언트를 bands 띠로 끊고, 띠 경계는 4×4 디더로 잇는다. stops는 (가상 y, 색)."""
    rows = stepped(SY / (H - 1), bands) * (H - 1) / 2
    at = [y for y, _ in stops]
    image = np.ones((H, W, 4))
    image[..., :3] = np.stack([np.interp(rows, at, [color(c)[k] for _, c in stops]) for k in range(3)], axis=-1)
    return image


def blend(image, amount, rgb):
    """불투명한 그림에 색을 amount만큼 미리 섞는다."""
    a = amount[..., None]
    image[..., :3] = image[..., :3] * (1 - a) + np.asarray(rgb) * a


def glow(image, center, radius, rgb, strength, levels, stretch=1.0):
    """해무리: 가운데에서 멀어질수록 levels 단계로 옅어진다. stretch만큼 옆으로 퍼진다."""
    cx, cy = dot(center[0]), dot(center[1])
    d = np.hypot((SX - cx) / stretch, SY - cy) / radius
    blend(image, stepped(np.clip(1 - d, 0, 1) ** 1.6, levels) * strength, color(rgb))


def sun(image, center, radius, core, rim):
    cx, cy = dot(center[0]), dot(center[1])
    d = np.hypot(SX + 0.5 - cx, SY + 0.5 - cy)
    image[d <= dot(radius), :3] = color(rim)
    image[d <= dot(radius) - 2, :3] = color(core)


def streak_noise(height, width, sx, sy, rng, wrap=False):
    """가로로 길쭉한 부드러운 잡음(0~1): 격자 간격이 가로 sx점, 세로 sy점. wrap이면 가로 끝과 처음이 이어진다(width는 sx의 배수)."""
    gx = width // sx if wrap else width // sx + 2
    lattice = rng.random((height // sy + 2, gx))
    x, y = np.arange(width) / sx, np.arange(height) / sy
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    x1 = (x0 + 1) % gx if wrap else x0 + 1
    tx, ty = pg.smooth(x - x0)[None, :], pg.smooth(y - y0)[:, None]
    top = lattice[y0][:, x0] * (1 - tx) + lattice[y0][:, x1] * tx
    bottom = lattice[y0 + 1][:, x0] * (1 - tx) + lattice[y0 + 1][:, x1] * tx
    return top * (1 - ty) + bottom * ty


def cirrus(image, rng, top, bottom, threshold, body, body_amount, edge, lit_below):
    """
    높은 새털구름: 가로로 길게 끌린 옅은 줄을 top~bottom(가상 y) 사이에 흩는다.
    빛을 받는 가장자리(낮에는 윗변, 노을에는 해가 아래에서 비추는 밑변)는 edge 색이다.
    """
    n = streak_noise(H, W, 96, 4, rng) * 0.6 + streak_noise(H, W, 24, 2, rng) * 0.4
    # 하늘 전체에 고르게 깔리면 줄무늬처럼 보이므로, 큰 잡음으로 몇 무리에만 모은다.
    cluster = streak_noise(H, W, 192, 36, rng)
    v = SY / 2
    envelope = np.clip(np.sin(np.pi * (v - top) / (bottom - top)), 0, 1) * ((v > top) & (v < bottom))
    streak = n * envelope * (0.3 + 0.9 * cluster) > threshold
    outside = np.roll(~streak, -1 if lit_below else 1, axis=0)
    blend(image, streak * body_amount, color(body))
    blend(image, (streak & outside) * 0.85, color(edge))


def stars(image, rng, count, floor, rgb):
    for _ in range(count):
        x, y = int(rng.uniform(2, W - 3)), int(rng.uniform(2, dot(floor)))
        amount = rng.uniform(0.3, 0.9) * (1 - y / dot(floor))
        image[y, x, :3] = image[y, x, :3] * (1 - amount) + color(rgb) * amount


# ── 구름 ───────────────────────────────────────────────

class Mass:
    """둥근 덩이를 겹쳐 그린 결과: 점마다 덩이 번호(없으면 -1), 빛(0~1), 앞뒤 깊이."""

    def __init__(self, width=STRIP):
        self.owner = np.full((H, width), -1)
        self.light = np.zeros((H, width))
        self.depth = np.full((H, width), -np.inf)
        self.count = 0

    def add(self, lobes, light, rise=None, weight=0.0):
        """
        lobes: (x, y, 가로 반지름, 세로 반지름, 앞뒤, 바닥) 점 단위. 앞(값이 큰) 덩이가 뒤 덩이를 가리고, 바닥 아래는 자른다(평평한 밑면).
        덩이마다 공의 면 방향 (u, v, w)와 빛 방향의 내적으로 빛을 받는다(그늘 쪽도 아주 까맣지는 않게 감싼다).
        rise(위 y, 아래 y)를 주면 무더기 전체의 높이도 weight만큼 섞는다(위가 밝다). 덩이마다 따로 밝으면 비눗방울을 쌓은 것처럼 보인다.
        가로로 되풀이하므로 끝에 걸친 덩이는 반대쪽에도 그린다.
        """
        width = self.owner.shape[1]
        lx, ly, lz = np.asarray(light) / np.linalg.norm(light)
        for cx, cy, rx, ry, z, floor in lobes:
            for x in (cx - width, cx, cx + width):
                x0, x1 = max(0, int(x - rx) - 1), min(width, int(x + rx) + 2)
                y0, y1 = max(0, int(cy - ry) - 1), min(H, int(cy + ry) + 2)
                if x0 >= x1 or y0 >= y1:
                    continue
                gy, gx = np.mgrid[y0:y1, x0:x1]
                u, v = (gx + 0.5 - x) / rx, (gy + 0.5 - cy) / ry
                w = np.sqrt(np.clip(1 - u * u - v * v, 0, 1))
                surface = z + w * ry
                win = (u * u + v * v <= 1) & (gy <= floor) & (surface > self.depth[y0:y1, x0:x1])
                lit = np.clip((u * lx + v * ly + w * lz + 0.35) / 1.35, 0, 1)
                if rise is not None:
                    lit = (1 - weight) * lit + weight * (1 - np.clip((gy - rise[0]) / (rise[1] - rise[0]), 0, 1))
                self.depth[y0:y1, x0:x1][win] = surface[win]
                self.owner[y0:y1, x0:x1][win] = self.count
                self.light[y0:y1, x0:x1][win] = lit[win]
            self.count += 1
        return self

    @property
    def mask(self):
        return self.owner >= 0

    def levels(self, steps, ambient=0.2, crest=1, crest_from=0.45, dither=0.8):
        """
        빛을 steps 단계 번호로 나누고(경계는 디더), 덩이 윗변이 뒤 덩이나 하늘에 닿는 곳(볕을 받는 등성이)은 crest 단계 밝힌다.
        그래서 덩이 사이가 어두운 테두리가 아니라 밝은 등성이 줄로 나뉜다.
        """
        lit = ambient + (1 - ambient) * self.light
        index = np.clip(np.floor(lit * steps + (pg.bayer(*lit.shape) - 0.5) * dither), 0, steps - 1).astype(int)
        above, above_depth = shifted(self.owner, 1, 0, -1), shifted(self.depth, 1, 0, -np.inf)
        ridge = self.mask & (above != self.owner) & (above_depth < self.depth) & (self.light > crest_from)
        index[ridge] = np.minimum(index[ridge] + crest, steps - 1)
        return index


def shifted(a, dy, dx, fill):
    """한 칸 민 배열. 가로는 되풀이하므로 돌려 붙이고, 세로는 밖을 fill로 채운다."""
    out = np.roll(a, (dy, dx), axis=(0, 1))
    if dy > 0:
        out[:dy] = fill
    elif dy < 0:
        out[dy:] = fill
    return out


def around(mask):
    """네 이웃 가운데 하나라도 mask인 곳(가로는 되풀이한다)."""
    return shifted(mask, 1, 0, False) | shifted(mask, -1, 0, False) | shifted(mask, 0, 1, False) | shifted(mask, 0, -1, False)


def painted(mass, palette, rim_from=0.55, ambient=0.2, crest=1):
    """덩이 그림을 칠한다: 빛 단계마다 팔레트 색, 빛 쪽 윤곽(빛이 rim_from보다 센 곳)에는 가장 밝은 테."""
    index = mass.levels(len(palette), ambient, crest)
    edge = mass.mask & around(~mass.mask)
    index[edge & (mass.light > rim_from)] = len(palette) - 1
    image = np.zeros((H, mass.owner.shape[1], 4))
    image[..., :3] = palette[index]
    image[..., 3] = mass.mask
    return image


def fringe(image, rgb, reach, density=1.0):
    """구름 윤곽 밖 reach점 안에 불투명한 점을 디더 무늬로 흩어, 반투명 없이 가장자리를 부드럽게 한다."""
    inside = image[..., 3] > 0
    grown = inside.copy()
    fill = np.zeros(inside.shape)
    for k in range(1, reach + 1):
        bigger = grown | around(grown)
        fill[bigger & ~grown] = (1 - k / (reach + 1)) * density
        grown = bigger
    scatter = (fill > pg.bayer(*inside.shape) + 0.03) & ~inside
    image[scatter, :3] = rgb
    image[scatter, 3] = 1


def heap(rng, cx, base, width, height, squash=1.0, z=0.0, flat=True):
    """
    뭉게구름 한 무더기(점 단위): 밑면은 base에서 평평하고 가운데가 height까지 둥글게 솟는다.
    아래 단부터 위로 덩이를 쌓고, 단이 오를수록 무더기가 좁아지고 덩이가 작아진다.
    """
    lobes = []
    r0 = width * 0.15
    tiers = max(1, int(height / (r0 * squash * 0.7)))
    floor = base if flat else NO_FLOOR
    for k in range(tiers + 1):
        f = k / tiers
        half = width / 2 * np.sqrt(max(1 - f ** 1.5, 0.04))
        r = r0 * (1 - 0.45 * f)
        y = base - f * max(height - r * squash, 0)
        n = max(1, int(round(2 * half / (r * 1.1))))
        for j in range(n):
            x = cx - half + (j + 0.5) * 2 * half / n + rng.uniform(-0.35, 0.35) * r
            rr = r * rng.uniform(0.65, 1.4)
            ly = y + rng.uniform(-0.3, 0.3) * rr * squash
            lobes.append((x, ly, rr, rr * squash, z + (ly - base) * 0.6 + rng.uniform(0, 3), floor))
            # 윗단 덩이 둘레에 작은 혹을 붙여 꽃양배추처럼 울퉁불퉁한 윤곽을 낸다.
            if f > 0.3:
                for _ in range(rng.integers(1, 4)):
                    angle = rng.uniform(-2.6, -0.5)
                    small = rr * rng.uniform(0.3, 0.5)
                    sx, sy = x + np.cos(angle) * rr * 0.8, ly + np.sin(angle) * rr * squash * 0.8
                    lobes.append((sx, sy, small, small * squash, z + (sy - base) * 0.6 - 2, floor))
    return lobes


def heap_light(base, height):
    """무더기 전체의 높이로 밝기를 섞을 범위(위 y, 아래 y)."""
    return (base - height, base)


def sea(rng, top, bottom, rows, near_size, far_size, squash, keep=None):
    """
    구름 바다(점 단위): top에서 bottom까지 줄마다 납작한 덩이를 늘어놓는다. 아래 줄일수록 크고 앞이며, 줄 사이도 멀어진다(원근).
    keep(x, 줄의 깊이 0~1)이 거짓인 덩이는 비워 둔다(구름 사이 틈).
    """
    lobes = []
    for k in range(rows):
        f = k / max(rows - 1, 1)
        y = top + (bottom - top) * f ** 1.6
        r = far_size + (near_size - far_size) * f
        n = max(4, int(STRIP / (r * 1.15)))
        offset = rng.uniform(0, STRIP / n)
        for j in range(n):
            x = offset + j * STRIP / n + rng.uniform(-0.3, 0.3) * r
            if keep is not None and not keep(x, f):
                continue
            rr = r * rng.uniform(0.75, 1.3)
            ly = y + rng.uniform(-0.35, 0.35) * rr * squash
            lobes.append((x, ly, rr, rr * squash, ly * 0.6 + rng.uniform(0, 2), NO_FLOOR))
    return lobes


def patches(rng, spacing, threshold):
    """구름 바다의 틈: 가로로 이어지는 잡음(격자 간격 spacing점, STRIP의 약수)이 문턱보다 큰 곳만 남긴다. 줄의 깊이마다 무늬가 조금 어긋난다."""
    n = streak_noise(8, STRIP, spacing, 1, rng, wrap=True)
    return lambda x, f: n[int(f * 6), int(x) % STRIP] > threshold


# ── 하늘길(낮) ─────────────────────────────────────────

DAY_CLOUD = tones("#7f97c0", "#9cb2d6", "#bccde6", "#d9e4f2", "#eef3fa", "#ffffff")
DAY_FAR_CLOUD = tones("#a3b8d8", "#b6c8e2", "#c8d7eb", "#dbe5f2", "#edf2f9", "#fbfcfe")
DAY_LIGHT = (0.55, -0.75, 0.42)     # 해가 오른쪽 위에 있다
HORIZON = 120                       # 하늘길 지평선(가상 y)
HAZE = "#cfdfec"


def day_sky(rng):
    image = band_sky(((0, "#2e5fb4"), (45, "#4a80d2"), (85, "#76a6e2"), (108, "#a6caec"), (HORIZON, "#d2e4f0"), (216, HAZE)), 26)
    glow(image, (330, 30), 300, "#fff6dc", 0.5, 7, stretch=1.3)
    glow(image, (330, 30), 70, "#fffbea", 0.7, 4)
    cirrus(image, rng, 6, 80, 0.41, "#eaf2fb", 0.6, "#ffffff", lit_below=False)
    sun(image, (330, 30), 9, "#fffdf2", "#fff1c4")
    return image


def fairy_forest(rng):
    """
    발아래 멀리 펼쳐진 페어리가든의 숲. 지평선(HORIZON)에서 화면 아래로 올수록 가까워 나무 머리가 커진다.
    나무 머리는 띠마다 어긋난 격자에 놓은 둥근 덩이이고(아래 머리가 앞), 몇 그루는 꽃이 핀 분홍빛이다.
    그 위로 꽃잎 들판, 굽이치는 강과 냇물, 호수를 얹고, 구름 그림자를 드리운 뒤 지평선 쪽부터 옅은 하늘빛 안개를 씌운다.
    """
    top, bottom = dot(HORIZON), H + 8
    depth = np.clip((YY - top) / (bottom - top), 0, 1)          # 0 지평선 ~ 1 화면 아래
    crowns = Mass()
    edges = top + (bottom - top) * np.linspace(0, 1, 15) ** 1.3
    for b in range(14):
        ya, yb = edges[b], edges[b + 1]
        f = ((ya + yb) / 2 - top) / (bottom - top)
        size = 3 + 22 * f ** 1.2
        cols = int(round(STRIP / size))
        rows = max(1, int(round((yb - ya) / (size * 0.55))))
        for j in range(rows):
            cy = ya + (j + 0.5) * (yb - ya) / rows
            lobes = []
            for i in range(cols):
                x = (i + 0.5 + rng.uniform(-0.3, 0.3) + 0.5 * (j % 2)) * STRIP / cols
                y = cy + rng.uniform(-0.25, 0.25) * size * 0.5
                k = rng.uniform(0.8, 1.2)
                lobes.append((x, y, size * 0.62 * k, size * 0.5 * k, y * 0.5, NO_FLOOR))
            crowns.add(lobes, DAY_LIGHT)
    blossom = rng.random(crowns.count) < 0.06
    leaf = tones("#1d3b2c", "#2b5536", "#3d6e40", "#57894a", "#76a457", "#9cc068")
    pink = tones("#5c3a52", "#8a5670", "#b47890", "#d69cb0", "#efc4d0", "#fae0e6")
    index = crowns.levels(len(leaf), ambient=0.25)
    image = np.zeros((H, STRIP, 4))
    image[..., :3] = color("#173024")
    flowering = crowns.mask & blossom[np.maximum(crowns.owner, 0)]
    image[crowns.mask, :3] = leaf[index[crowns.mask]]
    image[flowering, :3] = pink[index[flowering]]

    # 꽃잎 들판: 숲 사이의 밝은 풀밭과 흩뿌린 꽃.
    meadow = np.zeros((H, STRIP), dtype=bool)
    for x, f, size in ((240, 0.55, 46), (610, 0.25, 30), (980, 0.75, 60), (1330, 0.4, 38)):
        y = top + (bottom - top) * f
        scale = 0.3 + 0.7 * f
        for shift in (-STRIP, 0, STRIP):
            meadow |= ((XX - x - shift) / (size * scale * 2)) ** 2 + ((YY - y) / (size * scale * 0.55)) ** 2 <= 1
    grass = np.where((pg.bayer(H, STRIP) > 0.5)[..., None], color("#8cc06c"), color("#9ccc78"))
    image[meadow, :3] = grass[meadow]
    petals = meadow & (rng.random((H, STRIP)) < 0.08 + 0.1 * depth)
    image[petals, :3] = tones("#f6b8cc", "#fbe7a0", "#ffffff")[rng.integers(0, 3, petals.sum())]

    # 강 하나와 냇물 하나: 지평선에서 화면 아래로 굽이치며 가까울수록 넓어진다. 물에는 하늘이 비친다.
    for x0, wide, phase in ((420, 20, 0.0), (1150, 9, 2.0)):
        center = x0 + (60 + 120 * depth) * np.sin(depth * 6.5 + phase)
        gap = np.abs((XX - center + STRIP / 2) % STRIP - STRIP / 2)
        half = (1 + wide * depth ** 1.3) / 2
        water = (gap <= half) & (YY >= top)
        bank = (gap <= half + 1 + depth) & (YY >= top) & ~water
        image[bank, :3] = color("#1a3326")
        image[water, :3] = color("#86b2de")
        image[water & (gap < half * 0.45) & (pg.bayer(H, STRIP) < 0.35), :3] = color("#cfe6f8")
    lake = ((XX - 760) / 70) ** 2 + ((YY - (top + (bottom - top) * 0.45)) / 14) ** 2 <= 1
    image[lake, :3] = color("#86b2de")
    image[lake & around(~lake), :3] = color("#1a3326")
    image[lake & (pg.bayer(H, STRIP) < 0.2) & (np.abs(YY - (top + (bottom - top) * 0.45)) < 5), :3] = color("#cfe6f8")

    # 구름 그림자: 위에 뜬 구름 바다가 숲에 드리운 얼룩(가로로 이어진다).
    shadow = (streak_noise(H, STRIP, 128, 18, rng, wrap=True) > 0.62) & (YY >= top)
    image[shadow, :3] *= 0.78

    # 지평선 쪽 안개: 멀수록 하늘빛에 묻힌다(띠를 디더로 이은 단계).
    blend(image, stepped(np.clip(1 - depth, 0, 1) ** 2.2, 7) * 0.92, color(HAZE))
    image[..., 3] = YY >= top
    return image


def day_far(rng):
    """지평선의 뭉게구름 산맥: 크고 작은 무더기가 겹쳐 늘어서고, 밑동은 지평선 안개 띠에 묻힌다."""
    mass = Mass()
    base = dot(HORIZON) + 4
    for x, width, height in ((60, 300, 120), (330, 200, 64), (520, 340, 150), (780, 240, 90), (980, 360, 110),
                             (1210, 220, 70), (1380, 300, 130)):
        mass.add(heap(rng, x, base, width, height, squash=0.85), DAY_LIGHT, heap_light(base, height), 0.55)
    image = painted(mass, DAY_FAR_CLOUD, rim_from=0.6)
    band = (YY >= base - 4) & (YY < base + 10) & ((YY < base + 4) | (pg.bayer(H, STRIP) < (base + 10 - YY) / 8))
    image[band, :3] = color(HAZE)
    image[band, 3] = 1
    return image


def day_near(rng):
    """숲 위에 흩어진 구름 바다 조각. 틈으로 숲이 보이고, 가까울수록 덩이가 크다."""
    lobes = sea(rng, dot(150), dot(214), 12, 40, 12, 0.3, keep=patches(rng, 256, 0.5))
    mass = Mass().add(lobes, DAY_LIGHT, (dot(140), dot(216)), 0.3)
    image = painted(mass, DAY_CLOUD, crest=2)
    fringe(image, color("#eef3fa"), 3, 0.8)
    return image


def day_front(rng):
    """바로 앞을 스치는 큰 뭉게구름: 화면 아래에서 솟아 빠르게 지나간다."""
    mass = Mass()
    for x, width, height in ((260, 380, 96), (820, 300, 70), (1290, 420, 110)):
        mass.add(heap(rng, x, H + 40, width, height + 40, squash=0.8, flat=False), DAY_LIGHT, (H - height, H + 20), 0.5)
    image = painted(mass, DAY_CLOUD)
    fringe(image, color("#ffffff"), 4)
    return image


def sky_ride(out, rng):
    layers = {"sky": day_sky(rng), "forest": fairy_forest(rng), "far": day_far(rng), "near": day_near(rng), "front": day_front(rng)}
    save_all(out / "skyRide", layers)
    return layers


# ── 엔딩(노을) ──────────────────────────────────────────

SUNSET_FAR = tones("#6c4478", "#8e5280", "#b66682", "#da8380", "#f2a87e", "#ffd49a")
SUNSET_NEAR = tones("#4c3268", "#6a3e74", "#90507c", "#bc6a7e", "#e48e7c", "#ffc58c")
SUNSET_FRONT = tones("#33244e", "#45305e", "#5c3c6c", "#7a4c76", "#c87472", "#ffc080")
SUNSET_LIGHT = (0.8, -0.45, 0.25)   # 해가 오른쪽 낮은 곳에서 비춘다(구름의 오른쪽 윗면이 금빛)
SUN = (318, 150)
SEA_TOP = 151                       # 지평선 구름 바다의 윗면(가상 y). 해가 반쯤 잠긴다


def sunset_sky(rng):
    image = band_sky(((0, "#1a1c48"), (36, "#2c2a62"), (72, "#563a7a"), (102, "#93497e"), (124, "#cc5f72"),
                      (138, "#ee8a60"), (148, "#ffb86a"), (160, "#ffdc9c"), (216, "#ffdc9c")), 30)
    stars(image, rng, 70, 60, "#fff3dc")
    glow(image, SUN, 420, "#ffcc84", 0.45, 8, stretch=2.4)
    cirrus(image, rng, 34, 124, 0.42, "#c47896", 0.55, "#ffc29a", lit_below=True)
    # 햇살: 해에서 위로 퍼지는 옅은 빛줄기.
    cx, cy = dot(SUN[0]), dot(SUN[1])
    angle = np.arctan2(SY - cy, SX - cx)
    distance = np.hypot(SX - cx, SY - cy)
    rays = (np.cos(angle * 22 + 0.7) > 0.72) & (SY < cy) & (distance > 40)
    blend(image, rays * stepped(np.clip(1 - distance / 360, 0, 1), 5) * 0.22, color("#ffe2a8"))
    glow(image, SUN, 80, "#fff0c4", 0.75, 5)
    sun(image, SUN, 13, "#fff7e0", "#ffe29a")
    airos(image)
    return image


def airos(image):
    """
    해 앞, 구름 바다 너머로 솟은 아이로스의 탑 그림자(첨단 도시라 구름을 뚫고 선다). 밑동은 구름 바다(far)가 가린다.
    가장 높은 탑 꼭대기에 붉은 항공등 하나.
    """
    base = dot(SEA_TOP + 6)
    shade = color("#c0606a")
    # (가운데 x, 꼭대기 y, 너비) 가상 px, 모양: slab 판 건물, needle 첨탑, step 계단 지붕, dome 둥근 지붕
    towers = ((288, 141, 4, "slab"), (295, 134, 2, "needle"), (302, 138, 5, "step"), (311, 123, 3, "needle"),
              (318, 131, 6, "step"), (327, 137, 4, "slab"), (334, 128, 2, "needle"), (342, 141, 7, "dome"), (350, 137, 3, "slab"))
    for x, top, width, kind in towers:
        x0, x1, y0 = dot(x - width / 2), dot(x + width / 2), dot(top)
        body = (SX >= x0) & (SX < x1) & (SY >= y0) & (SY < base)
        if kind == "needle":
            body |= (SX >= dot(x) - 1) & (SX < dot(x) + 1) & (SY >= y0 - 14) & (SY < base)
        elif kind == "step":
            body &= (SY >= y0 + 6) | ((SX >= x0 + 3) & (SX < x1 - 3))
        elif kind == "dome":
            roof = ((SX + 0.5 - dot(x)) / ((x1 - x0) / 2)) ** 2 + ((SY + 0.5 - y0 - 6) / 6) ** 2 <= 1
            body = ((SX >= x0) & (SX < x1) & (SY >= y0 + 5) & (SY < base)) | roof
        image[body, :3] = shade
    image[dot(123) - 15, dot(311), :3] = color("#ff4a3a")


def sunset_far(rng):
    """지평선의 구름 바다(촘촘한 납작 덩이)와, 그 뒤로 솟은 적란운. 해 쪽(오른쪽) 윗면이 금빛이다."""
    top = dot(SEA_TOP)
    # 바다는 지평선(해 쪽)이 밝고 가까울수록 어둡다. 덩이 하나하나의 명암은 약하게 둔다.
    mass = Mass().add(sea(rng, top, dot(186), 16, 30, 8, 0.26), SUNSET_LIGHT, (top - 4, dot(186)), 0.7)
    for x, width, height in ((150, 300, 140), (1220, 240, 100), (1440, 150, 56)):
        mass.add(heap(rng, x, top + 10, width, height, squash=0.9, z=-60), SUNSET_LIGHT, heap_light(top + 10, height), 0.5)
    image = painted(mass, SUNSET_FAR, rim_from=0.5, crest=2)
    image[YY >= dot(186), :3] = SUNSET_FAR[1]
    image[YY >= dot(186), 3] = 1
    return image


def sunset_near(rng):
    """가까운 구름 바다의 물결: 아래 줄일수록 크다. 덩이 사이 틈은 그늘 색으로 메워 바닥까지 덮는다."""
    mass = Mass().add(sea(rng, dot(172), H + 30, 12, 56, 16, 0.3), SUNSET_LIGHT, (dot(166), H), 0.65)
    image = painted(mass, SUNSET_NEAR, rim_from=0.5, crest=2)
    fringe(image, SUNSET_NEAR[3], 2, 0.7)
    gaps = (YY >= dot(180)) & (image[..., 3] == 0)
    image[gaps, :3] = SUNSET_NEAR[1]
    image[gaps, 3] = 1
    return image


def sunset_front(rng):
    """가까이 지나가는 구름 덩이: 해를 등져 몸은 그늘지고 해 쪽 윤곽만 금빛이다."""
    mass = Mass()
    for x, width, height in ((330, 360, 80), (1120, 420, 96)):
        mass.add(heap(rng, x, H + 40, width, height + 40, squash=0.75, flat=False), SUNSET_LIGHT, (H - height, H + 20), 0.45)
    image = painted(mass, SUNSET_FRONT, rim_from=0.45, ambient=0.1)
    fringe(image, SUNSET_FRONT[2], 3, 0.8)
    return image


def ending(out, rng):
    layers = {"sky": sunset_sky(rng), "far": sunset_far(rng), "near": sunset_near(rng), "front": sunset_front(rng)}
    save_all(out / "ending", layers)
    return layers


# ── 저장과 미리보기 ─────────────────────────────────────

def save_all(folder, layers):
    folder.mkdir(parents=True, exist_ok=True)
    for name, image in layers.items():
        rgba = np.clip(image, 0, 255).copy()
        rgba[..., 3] = np.clip(image[..., 3], 0, 1) * 255
        Image.fromarray(pg.upscale(rgba.astype(np.uint8)), "RGBA").save(folder / f"{name}.png")
    Image.new("RGBA", (W * 2, H * 2)).save(folder / "back.png")


def preview(project, folder, names, bird, scroll=0):
    """겹을 화면 한 장으로 겹치고(되풀이 겹은 scroll점만큼 민 자리) 날개를 든 나리온을 컷인과 같은 자리에 얹는다."""
    picture = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 255))
    for name in names:
        layer = Image.open(folder / f"{name}.png")
        if layer.width > W * 2:
            layer = Image.fromarray(np.roll(np.asarray(layer), -scroll * 2, axis=1))
        picture.alpha_composite(layer.crop((0, 0, W * 2, H * 2)))
    (foot_x, foot_y), height = bird
    sprite = Image.open(project / "Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly0.png")
    scale = height * 4 / sprite.height
    sprite = sprite.resize((round(sprite.width * scale), round(sprite.height * scale)), Image.NEAREST)
    picture.alpha_composite(sprite, (round(foot_x * 4 - sprite.width / 2), round((foot_y - height) * 4)))
    return picture.convert("RGB")


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns"
    rng = np.random.default_rng(111)
    sky_ride(out, rng)
    ending(out, rng)
    ride = preview(project, out / "skyRide", ("sky", "forest", "far", "near", "front"), SKY_RIDE_BIRD)
    end = preview(project, out / "ending", ("sky", "far", "near", "front"), ENDING_BIRD)
    sheet = Image.new("RGB", (W * 2, H * 4))
    sheet.paste(ride, (0, 0))
    sheet.paste(end, (0, H * 2))
    sheet.save(project / "Docs/ArtUpgrade/PixelGen/cutin_sky_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
