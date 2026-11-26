"""타이틀 무대의 그림. 무대는 Unity에서 3D로 세우고(Editor/Stages), 여기서는 그 재료를 그린다.
이 파일은 세 무대가 함께 쓰는 칠하기와 프롤로그(차원공간의 여신)의 재료다. CHAPTER 1은 title_dawn.py, CHAPTER 2는 title_moon.py에 있다.

빛은 무대의 셰이더(Art/Shaders/Stage)가 비추므로, 그림에는 재료의 결과 면의 방향만 그리고 하늘빛은 칠하지 않는다.
"_glow"가 붙은 그림은 스스로 빛나는 자리다(검은 곳은 빛나지 않는다).

  pillar.png·pillar_glow.png 신전 기둥(원기둥에 감는 그림), rock_0~5.png 떠 있는 바위, rings_0~2.png 여신 뒤의 빛 고리,
  goddess.png 판도라 여신의 윤곽

사용: python Tools/PixelGen/title_stage.py <프로젝트 루트>
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

import pixelgen as pg
from silhouette import Brush, strands


def rgb(text):
    return np.array([int(text[i:i + 2], 16) for i in (1, 3, 5)], dtype=float)


def ramp(*texts):
    return np.array([rgb(t) for t in texts])


def save(image, folder, name):
    folder.mkdir(parents=True, exist_ok=True)
    array = np.clip(image, 0, 255).astype(np.uint8)
    Image.fromarray(array, "RGBA" if array.shape[2] == 4 else "RGB").save(folder / name)


def tone(values, palette, dither=0.9):
    """0~1 밝기를 팔레트 단으로. 단 경계만 디더로 섞는다."""
    h, w = values.shape
    level = values * (len(palette) - 1) + (pg.bayer(h, w) - 0.5) * dither
    return palette[np.clip(np.round(level).astype(int), 0, len(palette) - 1)]


# ---------------------------------------------------------------- 깎인 바위

def facets(h, w, rng, size):
    """돌을 쪼갠 면: 가까운 씨앗마다 한 면이고, 면마다 기운 방향이 다르다. 돌려주는 값: 면의 방향(왼쪽, 위), 면 경계까지의 거리."""
    count = max(6, int(h * w / size ** 2))
    xs, ys = rng.uniform(0, w, count), rng.uniform(0, h, count)
    nx, ny = rng.normal(0, 0.6, count), rng.normal(0, 0.6, count)
    yy, xx = np.mgrid[0:h, 0:w]
    wobble = (pg.fractal_noise(h, w, rng) - 0.5) * size * 0.7
    first = np.full((h, w), 1e9)
    second = np.full((h, w), 1e9)
    pick = np.zeros((h, w), dtype=int)
    for i in range(count):
        d = np.abs(xx + wobble - xs[i]) * 0.75 + np.abs(yy + wobble * 0.6 - ys[i]) * 0.9
        closer = d < first
        second = np.where(closer, first, np.minimum(second, d))
        first = np.where(closer, d, first)
        pick = np.where(closer, i, pick)
    return nx[pick], ny[pick], second - first


def chiselled(mask, rng, palette, size=15, light=(-0.55, 0.75), round_by=0.9):
    """실루엣을 깎인 돌로 칠한다. 둥근 부피(가장자리로 갈수록 기운다)에 면의 방향을 더해 빛을 받는다."""
    h, w = mask.shape
    yy, xx = np.mgrid[0:h, 0:w]
    inside_x = np.zeros((h, w))
    for y in range(h):
        row = np.nonzero(mask[y])[0]
        if len(row):
            center, half = (row[0] + row[-1]) / 2, max(1, (row[-1] - row[0]) / 2)
            inside_x[y] = (xx[y] - center) / half
    rows = np.nonzero(mask.any(axis=1))[0]
    top, bottom = rows[0], rows[-1]
    inside_y = 1 - 2 * (yy - top) / max(1, bottom - top)
    fx, fy, edge = facets(h, w, rng, size)
    normal_x = inside_x * round_by + fx * 0.8
    normal_y = inside_y * round_by * 0.8 + fy * 0.8
    shade = normal_x * light[0] + normal_y * light[1]
    shade = 0.42 + shade * 0.36 + (pg.fractal_noise(h, w, rng) - 0.5) * 0.22
    shade[edge < 1.1] -= 0.2
    image = np.zeros((h, w, 4))
    image[..., :3] = tone(np.clip(shade, 0, 1), palette)
    image[..., 3] = mask * 255
    # 윗변의 밝은 선과 바깥 어두운 선
    above = np.roll(mask, 1, axis=0)
    above[0] = False
    image[mask & ~above, :3] = palette[-1]
    return image


def rock_mask(w, h, rng, flat_top=0.22):
    """위는 평평하고 아래로 뾰족하게 매달린 바위."""
    yy, xx = np.mgrid[0:h, 0:w]
    across = np.abs(xx - w / 2) / (w / 2)
    noise = pg.fractal_noise(h, w, rng)
    down = np.clip((yy / h - flat_top) / (1 - flat_top), 0, 1)
    reach = (1 - down ** 1.35) * 0.93 + (noise - 0.5) * 0.5 * (0.25 + down)
    up = yy / h > flat_top * (0.35 + across ** 2 * 0.8) + (noise - 0.5) * 0.08
    return (across < reach) & up


ROCK = ramp("#0d0b22", "#191539", "#2a2455", "#413a78", "#615aa0", "#8f8cc9", "#c9cdf2")
MOSS = ramp("#12233f", "#1d3c63", "#2f6391", "#66a6cc")


def floating_rocks(folder, rng):
    for i, (w, h) in enumerate([(150, 190), (110, 130), (84, 120), (64, 60), (44, 52), (28, 26)]):
        mask = rock_mask(w, h, rng)
        save(pg.upscale(chiselled(mask, rng, ROCK, size=max(7, w // 8)).astype(np.uint8)), folder, f"rock_{i}.png")


# ---------------------------------------------------------------- 신전 기둥

MARBLE = ramp("#191738", "#2b2a55", "#454676", "#676b9c", "#9298c4", "#c3cae8", "#eef2ff")


def pillar(folder, rng):
    """원기둥에 한 바퀴 감는 그림(가로 = 둘레). 세로 홈(플루팅), 돌 마디, 금 간 곳, 빛나는 새김 띠."""
    w, h = 192, 384
    yy, xx = np.mgrid[0:h, 0:w]
    flutes = 16
    phase = (xx % (w / flutes)) / (w / flutes)
    groove = np.sin(phase * math.pi)
    noise = pg.fractal_noise(h, w, rng)
    shade = 0.36 + groove * 0.3 + (noise - 0.5) * 0.28
    joint = (yy % 96 < 2)
    shade[joint] -= 0.28
    shade[(yy % 96 == 2)] += 0.16
    # 금: 위에서 아래로 헤매는 어두운 선
    for _ in range(7):
        x, y = rng.uniform(0, w), rng.uniform(0, h * 0.8)
        for _ in range(int(rng.integers(40, 130))):
            x += rng.normal(0, 0.9)
            y += 1
            if y >= h:
                break
            shade[int(y), int(x) % w] -= 0.35
            shade[int(y), (int(x) + 1) % w] += 0.1
    # 이가 나간 자리
    chips = pg.fractal_noise(h, w, rng) > 0.73
    shade[chips] = 0.2 + noise[chips] * 0.25
    # 새김 띠 두 줄
    glow = np.zeros((h, w, 3))
    for center in (70, 306):
        band = np.abs(yy - center) < 17
        shade[band] = 0.3 + noise[band] * 0.12
        shade[np.abs(np.abs(yy - center) - 16) < 1.2] = 0.8
        for k in range(12):
            cx = int((k + 0.5) * w / 12)
            glyph = np.zeros((h, w), dtype=bool)
            for _ in range(4):
                x0, y0 = int(rng.integers(-4, 5)), int(rng.integers(-9, 10))
                x1, y1 = int(rng.integers(-4, 5)), int(rng.integers(-9, 10))
                for t in np.linspace(0, 1, 18):
                    if rng.random() < 0.97:
                        glyph[int(center + y0), int(cx + x0 + (x1 - x0) * t) % w] = True
                        glyph[int(center + y0 + (y1 - y0) * t), int(cx + x1) % w] = True
            glow[glyph] = (150, 205, 255)
    image = np.zeros((h, w, 4))
    image[..., :3] = tone(np.clip(shade, 0, 1), MARBLE)
    image[..., 3] = 255
    image[glow.any(axis=2), :3] = MARBLE[1]
    save(image, folder, "pillar.png")
    save(np.dstack([glow, np.full((h, w), 255)]), folder, "pillar_glow.png")


# ---------------------------------------------------------------- 여신 뒤의 빛 고리

def rings(folder, rng):
    size = 512
    c = size / 2

    def blank():
        picture = Image.new("L", (size, size), 0)
        return picture, ImageDraw.Draw(picture)

    def circle(draw, r, width, value):
        draw.ellipse([c - r, c - r, c + r, c + r], outline=value, width=width)

    def polar(r, degrees):
        a = math.radians(degrees)
        return c + math.cos(a) * r, c + math.sin(a) * r

    def diamond(draw, r, degrees, size_, value):
        x, y = polar(r, degrees)
        draw.polygon([(x, y - size_), (x + size_, y), (x, y + size_), (x - size_, y)], fill=value)

    layers = []
    # 바깥 고리: 두 겹 원, 눈금, 열두 마름모
    picture, draw = blank()
    circle(draw, 250, 2, 220)
    circle(draw, 238, 1, 130)
    for d in range(0, 360, 3):
        inner = 229 if d % 15 == 0 else 234
        draw.line([polar(inner, d), polar(238, d)], fill=200 if d % 15 == 0 else 110, width=1)
    for d in range(0, 360, 30):
        diamond(draw, 244, d, 6, 255)
    layers.append(picture)
    # 가운데 고리: 글자 띠와 별 모양 선
    picture, draw = blank()
    circle(draw, 240, 1, 120)
    circle(draw, 206, 2, 200)
    for k in range(48):
        d = k * 7.5
        for _ in range(3):
            a0, a1 = d + rng.uniform(-1.6, 1.6), d + rng.uniform(-1.6, 1.6)
            draw.line([polar(rng.uniform(212, 234), a0), polar(rng.uniform(212, 234), a1)], fill=180, width=2)
    for k in range(7):
        draw.line([polar(206, k * 360 / 7 - 90), polar(206, (k + 3) * 360 / 7 - 90)], fill=95, width=1)
        diamond(draw, 206, k * 360 / 7 - 90, 8, 255)
    layers.append(picture)
    # 안쪽 고리: 끊어진 호와 빛살
    picture, draw = blank()
    for k in range(6):
        draw.arc([c - 244, c - 244, c + 244, c + 244], k * 60 + 6, k * 60 + 44, fill=230, width=3)
        draw.arc([c - 226, c - 226, c + 226, c + 226], k * 60 + 24, k * 60 + 60, fill=120, width=1)
    for k in range(24):
        d = k * 15
        draw.line([polar(170, d), polar(214 if k % 2 else 200, d)], fill=150, width=1)
    circle(draw, 166, 1, 160)
    layers.append(picture)
    for i, layer in enumerate(layers):
        value = np.asarray(layer, dtype=float)
        image = np.dstack([np.full_like(value, 255)] * 3 + [value])
        save(image, folder, f"rings_{i}.png")


def goddess_layers():
    """판도라 여신의 윤곽 네 겹(1000×1500, 0~1): 몸, 머리카락, 날개옷, 빛(후광과 지팡이와 별)."""
    w, h = 1000, 1500
    hair = Brush(w, h, 41, 2)
    hair.ellipse(500, 262, 96, 84)
    hair.shape([(500, 214), (420, 240), (378, 310), (352, 420), (318, 520), (306, 640), (340, 750), (300, 870), (330, 980), (276, 1100), (372, 1030), (392, 1120), (424, 990), (450, 860), (470, 500)])
    hair.shape([(500, 214), (584, 240), (636, 300), (690, 372), (752, 420), (790, 510), (772, 610), (822, 700), (800, 810), (858, 930), (770, 880), (752, 990), (700, 880), (650, 930), (620, 760), (560, 500)])
    for points, widths in (
        ([(610, 260), (720, 290), (800, 250), (880, 290), (960, 240)], [18, 22, 17, 10, 0.6]),
        ([(650, 330), (770, 370), (850, 340), (910, 400), (985, 396)], [18, 22, 15, 9, 0.6]),
        ([(390, 300), (320, 340), (300, 420), (236, 450), (214, 520)], [16, 18, 14, 9, 0.6]),
        ([(330, 700), (262, 760), (280, 860), (210, 930), (220, 1010)], [16, 18, 13, 8, 0.6]),
        ([(790, 640), (860, 700), (850, 800), (910, 860), (900, 950)], [16, 18, 13, 8, 0.6])):
        hair.ribbon(points, widths)
    # 날개옷: 어깨 뒤로 감아 도는 긴 띠
    veil = Brush(w, h, 45, 2)
    veil.ribbon([(330, 520), (210, 420), (110, 300), (150, 150), (270, 120), (300, 210), (230, 240)], [4, 20, 30, 34, 26, 14, 1])
    veil.ribbon([(690, 520), (830, 470), (930, 560), (880, 700), (800, 800), (860, 930), (970, 960), (990, 1080)], [4, 22, 34, 30, 22, 30, 26, 1])
    veil.ribbon([(340, 570), (190, 640), (90, 760), (130, 900), (220, 1000), (140, 1120), (40, 1150)], [4, 20, 30, 30, 24, 22, 1])

    body = Brush(w, h, 43, 2)
    body.ellipse(500, 300, 50, 64)
    body.shape([(440, 330), (424, 270), (446, 214), (500, 190), (556, 214), (578, 270), (560, 334), (556, 290), (500, 262), (446, 290)])
    for points in (
        [(444, 260), (424, 340), (440, 430), (414, 520), (436, 610), (420, 680)], [(454, 280), (446, 370), (466, 470), (450, 560)],
        [(558, 260), (580, 340), (562, 430), (590, 520), (566, 620), (590, 700)], [(548, 280), (556, 370), (538, 470), (560, 570)]):
        body.ribbon(points, [14, 20, 19, 13, 0.6])
    body.ellipse(500, 372, 62, 54)
    body.shape([(480, 392), (398, 424), (392, 470), (428, 520), (452, 600), (446, 660), (420, 740), (400, 830), (600, 830), (582, 740), (556, 660), (550, 600), (574, 520), (610, 470), (602, 424), (520, 392)])
    # 앞으로 뻗어 손바닥을 편 왼팔과 늘어진 소매
    body.ribbon([(408, 446), (330, 500), (250, 520), (166, 486)], [34, 27, 22, 14])
    for k, (x, y) in enumerate(((112, 470), (106, 454), (116, 438), (134, 428))):
        body.ribbon([(164, 484), ((164 + x) / 2, (484 + y) / 2 + 4), (x, y)], [10, 7, 2])
    body.shape([(390, 470), (310, 500), (236, 516), (212, 560), (186, 680), (150, 800), (196, 776), (214, 846), (262, 750), (300, 800), (330, 690), (372, 620), (410, 540)])
    # 지팡이를 쥔 오른팔과 소매
    body.ribbon([(592, 446), (668, 520), (716, 520), (760, 448)], [34, 27, 23, 15])
    body.ellipse(764, 442, 19, 18)
    body.shape([(600, 480), (664, 520), (730, 500), (752, 560), (784, 690), (836, 800), (786, 786), (776, 860), (720, 770), (690, 816), (660, 690), (614, 560)])
    # 허리에서 흘러내려 갈라지는 긴 치마
    body.shape([(440, 680), (414, 750), (372, 870), (318, 1010), (262, 1150), (190, 1270), (110, 1350), (30, 1410), (150, 1400), (220, 1390), (200, 1470), (300, 1400), (352, 1380), (372, 1480), (440, 1390), (500, 1400), (530, 1496), (590, 1400), (660, 1390), (700, 1480), (760, 1390), (820, 1380), (900, 1440), (990, 1420), (900, 1350), (810, 1260), (736, 1140), (680, 1010), (628, 870), (586, 750), (560, 680)])

    light = Brush(w, h, 47, 2)
    light.ring(500, 268, 178, 178, 9)
    light.ring(500, 268, 138, 138, 4)
    for k in range(12):
        angle = math.tau * k / 12 - math.pi / 2
        c, q = math.cos(angle), math.sin(angle)
        inner, outer = 150, (268 if k == 0 else 226 if k % 3 == 0 else 208)
        if abs(math.degrees(angle) - 90) < 70:
            continue
        light.polygon([(500 + c * inner - q * 7, 268 + q * inner + c * 7), (500 + c * outer, 268 + q * outer), (500 + c * inner + q * 7, 268 + q * inner - c * 7)])
    light.line((770, 250), (738, 1290), 13)
    light.ellipse(772, 190, 44, 44)
    light.ring(772, 190, 84, 34, 8)
    light.ring(772, 190, 62, 62, 4)
    light.star(774, 62, 66, 0.22)
    for x, y, r in ((232, 120, 34), (118, 560, 26), (300, 60, 18), (876, 330, 26), (900, 130, 18), (60, 320, 16), (940, 700, 16), (130, 420, 40)):
        light.star(x, y, r)
    return body.mask(), hair.mask(), veil.mask(), light.mask()


def goddess(project, folder):
    """판도라 여신. 아직 정체를 모르는 인물이라 얼굴도 옷차림도 없이 윤곽만 있다(사용자 결정 2026-11-26).
    몸은 별이 뜬 밤하늘이고 윤곽에 금빛이 돈다. 머리카락은 그 뒤의 옅은 밤, 날개옷은 비치는 빛의 띠, 후광과 지팡이와 별은 빛이다."""
    body, hair, veil, light = goddess_layers()
    h, w = body.shape

    def blurred(mask, radius):
        return np.asarray(Image.fromarray((np.clip(mask, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius))).astype(float) / 255

    rng = np.random.default_rng(17)
    yy, xx = np.mgrid[0:h, 0:w]
    cloud = pg.fractal_noise(h, w, rng, layers=((190, 0.6), (70, 0.28), (26, 0.12)))
    drift = pg.fractal_noise(h, w, rng, layers=((240, 0.7), (90, 0.3)))
    low = np.clip((yy - 640) / 620, 0, 1)[..., None]

    def night(base, violet, blue, fade):
        sky = np.array(base) + np.clip(cloud * 1.8 - 0.66, 0, 1)[..., None] * np.array(violet) + np.clip(drift * 1.9 - 0.85, 0, 1)[..., None] * np.array(blue)
        return sky * (1 - low * fade) + np.array([126, 118, 220]) * low * fade

    front = night([8, 7, 32], [64, 42, 130], [20, 64, 124], 0.7)
    back = night([44, 38, 104], [60, 46, 110], [24, 50, 90], 0.7)
    for _ in range(1500):
        x, y = rng.integers(0, w - 4), rng.integers(0, h - 4)
        size = int(rng.choice([2, 2, 2, 3, 4]))
        (front if body[y, x] > 0.9 else back)[y:y + size, x:x + size] = np.array([214, 222, 255]) * rng.uniform(0.35, 1)

    def lit(sky, mask, color, wide, strength):
        line = np.clip((1 - blurred(mask, wide)) * 2.5, 0, 1)[..., None] ** 1.5 * strength
        haze = np.clip((1 - blurred(mask, wide * 5)) * 1.2, 0, 1)[..., None] ** 1.8
        return sky * (1 - line) + np.array(color) * line + haze * np.array(color) * 0.3

    front = lit(front, body, [255, 228, 170], 5, 1)
    back = lit(back, hair, [206, 192, 255], 4, 0.75)
    silk = lit(np.zeros((h, w, 3)) + np.array([196, 186, 255]), veil, [255, 246, 255], 6, 0.9)
    out = np.where((hair > 0.5)[..., None], back, silk)
    out = np.where((body > 0.5)[..., None], front, out)
    out = np.where(((light > 0.4) & (body < 0.5))[..., None], np.array([255, 226, 164]), out)
    silk_edge = np.clip((1 - blurred(veil, 6)) * 2, 0, 1)
    alpha = np.maximum.reduce([body, hair * 0.95, veil * (0.34 + 0.5 * silk_edge), light]) * 255
    save(np.dstack([out, alpha]), folder, "goddess.png")


def output_folder(project):
    folder = project / "Assets/MiniPandoras/Art/PixelGen/TitleStage"
    folder.mkdir(parents=True, exist_ok=True)
    return folder


def main(project):
    folder = output_folder(project)
    goddess(project, folder)
    floating_rocks(folder, np.random.default_rng(5))
    pillar(folder, np.random.default_rng(8))
    rings(folder, np.random.default_rng(3))


if __name__ == "__main__":
    main(Path(sys.argv[1]))
