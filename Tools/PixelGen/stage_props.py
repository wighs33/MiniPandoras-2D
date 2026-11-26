"""컷인 무대의 소품 그림. 무대는 Editor/Stages/CutIns가 세운다.

  cradle.png 아기가 잠든 요람 배, cracks.png 갈라진 세계의 틈, erad_tower.png·_glow.png 에라드의 탑,
  tower_top.png·_glow.png 돌뱀 탑의 위쪽(원래 크기), bridge.png 나무다리 판자,
  parchment.png 떨어진 지도, planks.png 널벽, beam.png 들보, cloud_day/dusk_0~3.png 구름, city.png·_glow.png 아이로스의 스카이라인,
  mountain_0~1.png 산줄기, seat.png 객실 의자, panel.png 객실 벽널, curtain.png 커튼, lamp.png 등, cart_0~3.png 수레와 호빗, dirt.png 흙길,
  demon_wings.png·_glow.png 디엘의 날개(정면), demon_wings_back.png·demon_wings_glow_back.png 뒤에서 본 디엘의 날개, diel_seated·diel_back·kyle.png 한 사람이 주인공인 장면의 인물, kyle_wings.png 카일의 천사 날개

사용: python Tools/PixelGen/stage_props.py <프로젝트 루트>
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

import castle
import pixelgen as pg
import cutin_castle
import cutin_descent
from title_dawn import clouds_of
from title_stage import chiselled, ramp, save, tone

IRON = ramp("#0c0812", "#1a121f", "#2b1e2f", "#403044", "#5c485e", "#80687e")
WOOD = ramp("#1e120c", "#3a2316", "#573621", "#77502f", "#986c42", "#bd9160")
STONE = ramp("#1d1c2b", "#2f2f42", "#46475c", "#62647a", "#83869b", "#a8acbd", "#cfd2dc")
DAY = ramp("#5f7cb4", "#86a1cf", "#aec3e3", "#d2dff1", "#ecf2fa", "#ffffff", "#ffffff")
DUSK = ramp("#4a3a72", "#6f4a84", "#a5608d", "#d97f86", "#f6a77d", "#ffcf93", "#fff0c4")
NIGHT = ramp("#12182e", "#1d2746", "#2c3a62", "#425481", "#6077a2", "#8fa5c8", "#c8d6ea")
PALE = ramp("#6a6258", "#857c70", "#a1988a", "#bdb4a5", "#d6cfc2", "#ebe6dc", "#fbf8f2")


def put(folder, name, image):
    save(pg.upscale(image.astype(np.uint8)), folder, name)


def empty(w, h):
    return np.zeros((h, w, 4))


def cradle(folder):
    """아기가 잠든 작은 배 모양 요람."""
    w, h = 160, 104
    image = empty(w, h)
    yy, xx = np.mgrid[0:h, 0:w]

    def spot(x, y, rx, ry):
        return ((xx - x) / rx) ** 2 + ((yy - y) / ry) ** 2 < 1

    def fill(mask, colour, shade=None):
        image[mask, :3] = np.array(colour)[None] * (1 if shade is None else shade[mask][:, None])
        image[mask, 3] = 255

    soft = 1.06 - (yy - 20) / 130 + (xx - 80) / 600
    fill(spot(50, 40, 34, 18) & (yy < 54), (236, 228, 214), soft)                 # 베개
    fill(spot(104, 48, 50, 26) & (yy < 54), (206, 222, 246), soft)                # 이불
    for k in range(4):
        fill((np.abs(xx - (78 + k * 18) - (yy - 40) * 0.3) < 1.2) & spot(104, 48, 48, 24) & (yy < 54), (174, 194, 228))
    fill(spot(54, 33, 17, 16), (252, 220, 188), soft)                             # 얼굴
    fill(spot(50, 23, 16, 9) & (yy < 26), (112, 78, 60), soft)                    # 머리카락
    fill(spot(42, 29, 5, 10), (112, 78, 60), soft)
    for x in (50, 62):
        fill((np.abs(yy - 35 - ((xx - x) / 3) ** 2 * 0.6) < 0.9) & (np.abs(xx - x) < 4), (92, 60, 56))  # 감은 눈
    fill(spot(46, 39, 4, 2.4) | spot(66, 39, 4, 2.4), (246, 178, 160))            # 볼
    fill(spot(57, 42, 2.4, 1.2), (206, 120, 110))
    grain = np.sin(yy * 0.75 + pg.noise(h, w, 12, np.random.default_rng(2)) * 5) * 0.1 + (pg.noise(h, w, 8, np.random.default_rng(1)) - 0.5) * 0.3
    hull = spot(80, 50, 76, 46) & (yy > 52)
    shade = np.clip(0.62 + (xx - 80) / 260 - (yy - 52) / 110 + grain, 0, 1)
    image[hull, :3] = tone(shade, WOOD)[hull]
    for k in range(4):
        seam = np.abs(yy - 62 - k * 9 - ((xx - 80) / 60) ** 2 * 2) < 0.9
        image[seam & hull, :3] = WOOD[1]
    rim = (np.abs(yy - 53) < 3) & (np.abs(xx - 80) < 79)
    image[rim, :3] = WOOD[5]
    image[rim & (yy > 53), :3] = WOOD[3]
    image[hull | rim, 3] = 255
    put(folder, "cradle.png", image)


def cracks(folder):
    size = 1024
    picture = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(picture)
    rng = np.random.default_rng(12)

    def crack(x, y, angle, length, width, depth):
        while length > 0 and width > 0.7:
            step = rng.uniform(16, 34)
            angle += rng.normal(0, 0.34)
            nx, ny = x + math.cos(angle) * step, y + math.sin(angle) * step
            draw.line([(x, y), (nx, ny)], fill=255, width=max(1, int(width)))
            if depth > 0 and rng.random() < 0.22:
                crack(nx, ny, angle + rng.choice([-1, 1]) * rng.uniform(0.4, 1.0), length * 0.55, width * 0.6, depth - 1)
            x, y, length, width = nx, ny, length - step, width * 0.95

    for k in range(9):
        crack(size / 2, size / 2, k / 9 * 2 * math.pi + rng.normal(0, 0.15), 470, 15, 3)
    value = np.asarray(picture).astype(float)
    yy, xx = np.mgrid[0:size, 0:size]
    value *= np.hypot(xx - size / 2, yy - size / 2) < size * 0.49
    save(np.dstack([np.full_like(value, 255)] * 3 + [value]), folder, "cracks.png")


def erad_tower(folder):
    w, h = 420, 900
    rng = np.random.default_rng(22)
    picture = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(picture)
    cx = w / 2

    def spire(x, base, top, half, taper):
        draw.polygon([(x - half, base), (x - half * taper, top + (base - top) * 0.22), (x, top), (x + half * taper, top + (base - top) * 0.22), (x + half, base)], fill=255)

    spire(cx, h, 10, 92, 0.44)
    for side in (-1, 1):
        for k, (dx, top, half) in enumerate([(64, 210, 30), (108, 360, 34), (150, 520, 38), (184, 680, 26)]):
            spire(cx + side * dx, h, top, half, 0.5)
            for fin in range(3):
                y = top + 120 + fin * 90
                draw.polygon([(cx + side * (dx + half * 0.5), y), (cx + side * (dx + half * 0.5 + 26), y - 46), (cx + side * (dx + half * 0.5 + 4), y + 34)], fill=255)
    for y in (180, 300, 430, 570, 720):
        half = 36 + (y - 10) / h * 70
        draw.polygon([(cx - half - 16, y), (cx - half - 24, y - 22), (cx + half + 24, y - 22), (cx + half + 16, y), (cx + half, y + 18), (cx - half, y + 18)], fill=255)
    mask = np.asarray(picture) > 127
    image = chiselled(mask, rng, IRON, size=34, light=(-0.4, 0.7), round_by=0.9)
    yy, xx = np.mgrid[0:h, 0:w]
    seam = ((yy % 22) < 1.5) & mask
    image[seam, :3] *= 0.7
    glow = empty(w, h)
    glow[..., 3] = 255
    for _ in range(150):
        x = int(cx + rng.normal(0, 70))
        y = int(rng.uniform(120, h - 30))
        if 4 < x < w - 4 and mask[y, x - 3:x + 4].all():
            tall = int(rng.integers(6, 14))
            glow[y:y + tall, x - 1:x + 2, :3] = (255, 96, 84)
            image[y - 1:y + tall + 1, x - 2:x + 3, :3] = IRON[0]
    # 꼭대기의 눈
    ring = np.abs(np.hypot((xx - cx) / 1.0, (yy - 150) / 1.5) - 15) < 3
    core = np.hypot(xx - cx, (yy - 150) / 1.8) < 6
    glow[(ring | core) & mask, :3] = (255, 120, 96)
    put(folder, "erad_tower.png", image)
    put(folder, "erad_tower_glow.png", glow)


def castle_parts(folder):
    tower = castle.serpent_tower(np.random.default_rng(9))[0].astype(float)
    rgb = tower[..., :3]
    lit = (tower[..., 3] > 0) & (rgb[..., 0] > 195) & (rgb[..., 1] > 70) & (rgb[..., 0] - rgb[..., 2] > 110)
    glow = np.zeros_like(tower)
    glow[..., 3] = 255
    glow[lit, :3] = rgb[lit]
    # 성문에 가리지 않는 위쪽만 쓴다.
    put(folder, "tower_top.png", tower[:540])
    put(folder, "tower_top_glow.png", glow[:540])
    w, h = 64, 128
    yy, xx = np.mgrid[0:h, 0:w]
    rng = np.random.default_rng(5)
    grain = pg.noise(h, w, 10, rng)
    shade = 0.55 + (grain - 0.5) * 0.5 - ((yy % 16) < 2) * 0.35
    image = np.dstack([tone(np.clip(shade, 0, 1), WOOD), np.full((h, w), 255)])
    put(folder, "bridge.png", image)


def parchment(folder):
    """떨어진 지도: 컷인에서 쓰던 그림의 종이 부분."""
    image, _ = cutin_castle.fallen_map(np.random.default_rng(43))
    h, w = image.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    turn = np.radians(-6)
    du, dv = xx - cutin_castle.MAP_CENTER[0], yy - cutin_castle.MAP_CENTER[1]
    u, v = du * np.cos(turn) - dv * np.sin(turn), du * np.sin(turn) + dv * np.cos(turn)
    paper = (np.abs(u) < cutin_castle.MAP_HALF[0] - 1) & (np.abs(v) < cutin_castle.MAP_HALF[1] - 1)
    out = np.dstack([image, paper * 255]).astype(float)
    ys, xs = np.nonzero(paper)
    out = out[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    put(folder, "parchment.png", np.asarray(Image.fromarray(out.astype(np.uint8), "RGBA").rotate(-6, expand=True, resample=Image.NEAREST)).astype(float))


def wood(folder):
    rng = np.random.default_rng(31)
    w, h = 256, 192
    yy, xx = np.mgrid[0:h, 0:w]
    board = xx // 24
    tone_of = np.random.default_rng(4).uniform(-0.18, 0.18, 12)[board % 12]
    grain = pg.noise(h, w, 4, rng) * 0.4 + pg.noise(h, w, 30, rng) * 0.6
    grain = np.sin(xx * 0.9 + grain * 9) * 0.06 + (grain - 0.5) * 0.3
    shade = 0.46 + tone_of + grain - ((xx % 24) < 2) * 0.35 + ((xx % 24) == 2) * 0.1
    image = np.dstack([tone(np.clip(shade, 0, 1), WOOD), np.full((h, w), 255)])
    put(folder, "planks.png", image)
    w, h = 192, 32
    yy, xx = np.mgrid[0:h, 0:w]
    grain = pg.noise(h, w, 5, rng)
    shade = 0.52 + np.sin(yy * 0.7 + grain * 6) * 0.1 + (grain - 0.5) * 0.3 + (yy < 3) * 0.2 - (yy > h - 4) * 0.25
    put(folder, "beam.png", np.dstack([tone(np.clip(shade, 0, 1), WOOD), np.full((h, w), 255)]))


def city(folder):
    """노을 너머 아이로스: 겹겹의 첨탑과 불 켜진 창."""
    w, h = 720, 300
    rng = np.random.default_rng(51)
    image = empty(w, h)
    glow = empty(w, h)
    glow[..., 3] = 255
    shades = ramp("#4b2a5e", "#5f3468", "#78406f", "#8f4f74")
    for layer in range(3):
        colour = shades[3 - layer]
        x = -20
        while x < w:
            width = int(rng.integers(16, 52))
            centre = max(0.0, 1 - abs((x + width / 2) / w * 2 - 1))
            tall = int((30 + rng.uniform(0.2, 1) * 150 * centre ** 0.8) * (0.6 + layer * 0.2))
            x0, x1 = max(0, x), min(w, x + width)
            if x1 > x0:
                top = h - tall
                image[top:, x0:x1, :3] = colour
                image[top:, x0:x1, 3] = 255
                image[top:, x0:x0 + max(1, width // 6), :3] = colour * 1.2
                if rng.random() < 0.6:
                    mid = (x0 + x1) // 2
                    spike = int(rng.uniform(14, 60) * centre)
                    for k in range(spike):
                        half = max(0, int((1 - k / spike) * width * 0.22))
                        if top - k >= 0:
                            image[top - k, max(0, mid - half):min(w, mid + half + 1), :3] = colour
                            image[top - k, max(0, mid - half):min(w, mid + half + 1), 3] = 255
                for _ in range(int(tall * width / 190)):
                    wx, wy = int(rng.uniform(x0, x1)), int(rng.uniform(top + 4, h))
                    glow[wy:wy + 2, wx:wx + 1, :3] = (255, 220, 150)
            x += int(width * rng.uniform(0.6, 1.3))
    put(folder, "city.png", image)
    put(folder, "city_glow.png", glow)


def mountains(folder):
    """눈 덮인 산줄기. 비스듬히 흘러내리는 두 갈래 능선을 겹쳐 빛 받는 면과 그늘진 면을 나눈다."""
    for i, (w, h, seed) in enumerate([(960, 260, 61), (960, 180, 62)]):
        rng = np.random.default_rng(seed)
        line = np.zeros(w)
        for scale, weight in ((14, 1.0), (30, 0.5), (64, 0.24), (150, 0.08)):
            line += np.interp(np.arange(w), np.linspace(0, w, scale), rng.uniform(0, 1, scale)) * weight
        line = line / line.max() * h * 0.94 * np.sin(np.linspace(0, math.pi, w)) ** 0.35
        yy, xx = np.mgrid[0:h, 0:w]
        mask = yy > h - line[None]
        field = np.zeros((h, w))
        for spacing, weight in ((46, 1.0), (22, 0.55), (10, 0.24)):
            for lean in (0.62, -0.62):
                wide = pg.noise(h, w + h, spacing, rng)
                slide = (yy * lean if lean > 0 else h + yy * lean).astype(int)
                field += (1 - np.abs(wide[yy, np.minimum(xx + slide, w + h - 1)] - 0.5) * 2) * weight
        light = np.clip(0.46 - np.gradient(field, axis=1) * 3.4 + np.gradient(field, axis=0) * 1.2, 0, 1)
        image = empty(w, h)
        image[..., :3] = tone(light * 0.8 + 0.06, ramp("#23263c", "#363c56", "#4d5673", "#69738f", "#8b95ad", "#b0b9cb"))
        below = yy - (h - line[None])
        # 눈: 높이 솟은 곳일수록 깊이 덮고, 아래 끝은 골짜기를 따라 흘러내린다.
        snow = mask & (below < (line[None] - h * 0.3) * 0.72 - (field - 1.6) * 46)
        image[snow, :3] = tone(np.clip(light * 1.15 + 0.02, 0, 1), ramp("#7f93bd", "#a5b7d8", "#cbd8ec", "#eaf0f9", "#ffffff"))[snow]
        image[..., 3] = mask * 255
        put(folder, f"mountain_{i}.png", image)


def dirt(folder):
    """수레가 다니는 흙길. 이어 붙는 그림."""
    rng = np.random.default_rng(33)
    size = 256
    noise = pg.tileable_noise(size, 8, rng) * 0.45 + pg.tileable_noise(size, 32, rng) * 0.35 + pg.tileable_noise(size, 64, rng) * 0.2
    image = np.dstack([tone(noise, ramp("#4a3726", "#654c34", "#806346", "#9a7a58")), np.full((size, size), 255)])
    for _ in range(90):
        x, y, r = rng.integers(4, size - 4), rng.integers(4, size - 4), rng.integers(1, 3)
        image[y - r:y + r, x - r:x + r + 1, :3] = (150, 132, 108)
        image[y + r - 1, x - r:x + r + 1, :3] = (92, 72, 52)
    save(image, folder, "dirt.png")


def curve(points, count=60):
    """굽은 선(베지어) 위의 점."""
    out = []
    for t in np.linspace(0, 1, count):
        p = np.array(points, dtype=float)
        while len(p) > 1:
            p = p[:-1] * (1 - t) + p[1:] * t
        out.append(p[0])
    return np.array(out)


def demon_wings(folder):
    """디엘의 악마 날개 한 쌍(가운데가 등). 굽은 팔뼈 끝의 갈고리, 다섯 갈래 손가락뼈, 찢긴 날개막. _glow는 막을 타는 보랏빛 핏줄."""
    w, h, ss = 250, 320, 4
    rng = np.random.default_rng(77)
    shoulder, elbow, wrist = (4, 196), (84, 92), (150, 44)
    arm = np.vstack([curve([shoulder, (30, 120), elbow]), curve([elbow, (112, 60), wrist])])
    fingers = [
        curve([wrist, (206, 46), (246, 128)]),
        curve([wrist, (200, 96), (228, 212)]),
        curve([wrist, (172, 130), (184, 272)]),
        curve([elbow, (132, 160), (126, 300)]),
        curve([elbow, (84, 170), (68, 286)]),
        curve([shoulder, (10, 220), (20, 262)]),
    ]
    hub = [np.array(wrist)] * 3 + [np.array(elbow)] * 2 + [np.array(shoulder)]
    skin = Image.new("L", (w * ss, h * ss), 0)
    bone = Image.new("L", (w * ss, h * ss), 0)
    fold = Image.new("L", (w * ss, h * ss), 0)
    vein = Image.new("L", (w * ss, h * ss), 0)
    brush, ink, crease, blood = (ImageDraw.Draw(i) for i in (skin, bone, fold, vein))

    def big(points):
        return [(float(x) * ss, float(y) * ss) for x, y in points]

    def edge(a, b, center):
        """두 손가락뼈 끝 사이의 막 끝: 안으로 파이고 찢겨 있다."""
        middle = (a + b) / 2 * 0.62 + center * 0.38
        line = curve([a, middle, b], 40)
        out = line.copy()
        toward = center - (a + b) / 2
        toward /= np.linalg.norm(toward)
        for k in range(1, 39):
            out[k] += toward * rng.uniform(0, 5) * (1.6 if k % 5 == 0 else 0.5)
        return out

    for k in range(5):
        first, second = fingers[k], fingers[k + 1]
        center = (hub[k] + hub[k + 1]) / 2
        rim = edge(first[-1], second[-1], center)
        inner = [] if np.allclose(hub[k], hub[k + 1]) else list(curve([hub[k + 1], hub[k]], 8)) if k != 4 else list(curve([shoulder, (30, 120), elbow]))
        panel = list(first) + list(rim) + list(second[::-1]) + inner
        brush.polygon(big(panel), fill=255)
        for j in range(1, 6):
            t = j / 6
            end = rim[int(t * 39)]
            start = hub[k] * (1 - t) + hub[k + 1] * t
            bend = (start + end) / 2 + (second[30] - first[30]) * 0.06 * rng.uniform(-1, 1)
            line = curve([start, bend, end * 0.94 + start * 0.06], 30)
            crease.line(big(line), fill=150 + 60 * (j % 2), width=int(ss * 1.6))
        for j in range(3):
            t = rng.uniform(0.2, 0.8)
            end = rim[int(t * 39)] * 0.8 + center * 0.2
            line = curve([first[int(rng.uniform(10, 40))], (first[40] + end) / 2 + rng.uniform(-10, 10, 2), end], 24)
            blood.line(big(line), fill=200, width=int(ss * 1.2))
            blood.line(big(curve([line[12], (line[12] + second[44]) / 2, second[int(rng.uniform(34, 54))]], 16)), fill=140, width=ss)
        blood.line(big(rim), fill=255, width=int(ss * 1.3))
        # 막에 난 구멍
        for j in range(2):
            t = rng.uniform(0.25, 0.75)
            x, y = rim[int(t * 39)] * rng.uniform(0.66, 0.82) + center * 0.26
            rx, ry = rng.uniform(1.5, 4), rng.uniform(4, 9)
            if 0 < x < w:
                brush.ellipse([(x - rx) * ss, (y - ry) * ss, (x + rx) * ss, (y + ry) * ss], fill=0)
                blood.ellipse([(x - rx - 1) * ss, (y - ry - 1) * ss, (x + rx + 1) * ss, (y + ry + 1) * ss], outline=220, width=ss)

    def limb(points, r0, r1, value):
        n = len(points)
        for k, (x, y) in enumerate(points):
            r = (r0 + (r1 - r0) * k / (n - 1)) * ss
            ink.ellipse([x * ss - r, y * ss - r, x * ss + r, y * ss + r], fill=value)

    limb(arm, 8, 5, 255)
    for k, finger in enumerate(fingers):
        dense = np.array([finger[min(59, int(i))] * (1 - i % 1) + finger[min(59, int(i) + 1)] * (i % 1) for i in np.arange(0, 59, 0.25)])
        limb(dense, 3.4 if k < 5 else 5, 0.7, 230)
    limb(curve([wrist, (164, 6), (134, 4), (126, 26)], 160), 6.5, 0.8, 255)     # 팔목의 갈고리
    limb(curve([elbow, (74, 62), (54, 66)], 100), 5, 0.6, 240)                  # 팔꿈치의 가시
    for k in (14, 30, 46, 70, 92):
        x, y = arm[k]
        limb(curve([(x, y), (x - 8, y - 8), (x - 11, y - 14)], 40), 3, 0.5, 220)

    def down(image):
        return np.asarray(image.resize((w, h), Image.LANCZOS)).astype(float) / 255

    skin, bone, fold, vein = down(skin), down(bone), down(fold), down(vein)
    near = np.asarray(Image.fromarray((bone * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(9))).astype(float) / 255
    yy, xx = np.mgrid[0:h, 0:w]
    sheen = 0.3 + near * 0.5 + (fold - 0.35) * 0.3 * (fold > 0.1) + (pg.fractal_noise(h, w, rng) - 0.5) * 0.4 + (1 - yy / h) * 0.14
    half = empty(w, h)
    body = skin > 0.5
    half[body, :3] = tone(np.clip(sheen, 0, 1), ramp("#0a0712", "#150e22", "#231735", "#35234d", "#4d3369", "#6b4a8a"))[body]
    solid = bone > 0.5
    gy, gx = np.gradient(np.asarray(Image.fromarray((bone * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6))).astype(float) / 255)
    lump = np.clip(0.45 + (gy * 0.9 + gx * 0.5) * 3.2 + (pg.noise(h, w, 5, rng) - 0.5) * 0.3, 0, 1)
    half[solid, :3] = tone(lump, ramp("#0d0a14", "#1f1a2a", "#383146", "#5c526e", "#8e849f", "#cfc8dc"))[solid]
    half[..., 3] = np.where(body | solid, 255, 0)
    glow = empty(w, h)
    glow[..., 3] = 255
    strength = np.clip(vein * 1.1, 0, 1) * body * ~solid
    glow[..., :3] = strength[..., None] * np.array([196, 96, 255])
    put(folder, "demon_wings.png", np.concatenate([half[:, ::-1], half], axis=1))
    put(folder, "demon_wings_glow.png", np.concatenate([glow[:, ::-1], glow], axis=1))


# 뒤에서 본 디엘의 날개(3장 컷인). 정면용 날개는 두 날개가 한 점에서 만나서, 등 뒤 카메라 쪽에 두면 몸을 덮고 몸 너머에 두면 가슴에 달린 것처럼 보인다
# (2026-11-26 사용자 지적). 그래서 반쪽씩 나눠 양 어깨뼈에서 돋게 하고 바깥으로 기울인다.
BACK_GAP = 0.02            # 뿌리 사이 반폭(정면용 날개 폭의 몫). 무대가 날개 폭을 14칸으로 그리므로 0.28칸, 디엘의 어깨뼈 자리다
BACK_TILT = 12             # 바깥(위)으로 기울인 각(도)
FRONT_JOINT = 0.6          # 정면용 날개에서 이음매의 높이(그림 위에서)
BACK_ROOT_ABOVE_BOTTOM = 0.4   # 뒤에서 본 날개 그림에서 뿌리의 높이(그림 아래에서). 무대가 이 자리를 등 위쪽에 맞춘다


def wings_from_behind(folder):
    """demon_wings.png·_glow.png를 반쪽씩 기울여 벌린 demon_wings_back.png·demon_wings_glow_back.png. 정면용과 점 크기가 같고, 두 그림의 자리도 같다."""

    def tilted_right_half(wings, fill):
        """오른쪽 날개를 뿌리(왼쪽 가장자리의 이음매 높이)가 판 가운데 (반폭, 높이)에 오게 놓고 바깥으로 기울인다."""
        w, h = wings.size
        half = wings.crop((w // 2, 0, w, h))
        board = Image.new("RGBA", (half.width * 2, h * 2), fill)
        board.paste(half, (half.width, h - round(h * FRONT_JOINT)))
        return board.rotate(BACK_TILT, resample=Image.NEAREST, center=(half.width, h), fillcolor=fill)

    colour = Image.open(folder / "demon_wings.png").convert("RGBA")
    w, h = colour.size
    board_root = (w // 2, h)
    # 자리는 색 그림의 보이는 범위로 한 번 정한다(빛 그림은 온통 불투명해서 범위를 알 수 없다).
    left_edge, top, right_edge, bottom = tilted_right_half(colour, (0, 0, 0, 0)).getbbox()
    height = math.ceil(max((h - top) / (1 - BACK_ROOT_ABOVE_BOTTOM), (bottom - h) / BACK_ROOT_ABOVE_BOTTOM))
    gap = round(w * BACK_GAP)
    width = 2 * (gap + max(right_edge - board_root[0], board_root[0] - left_edge))
    root_y = round(height * (1 - BACK_ROOT_ABOVE_BOTTOM))
    for name, fill in (("demon_wings", (0, 0, 0, 0)), ("demon_wings_glow", (0, 0, 0, 255))):
        right = tilted_right_half(Image.open(folder / f"{name}.png").convert("RGBA"), fill)
        left = right.transpose(Image.FLIP_LEFT_RIGHT)
        out = Image.new("RGBA", (width, height), fill)
        for side, root_x, side_root_x in ((right, width // 2 + gap, board_root[0]), (left, width // 2 - gap, left.width - board_root[0])):
            layer = Image.new("RGBA", (width, height), fill)
            layer.paste(side, (root_x - side_root_x, root_y - board_root[1]))
            # 색은 겹쳐 그리고, 빛은 둘 중 밝은 쪽을 남긴다(빛 그림은 바탕까지 불투명한 검정이다).
            out = Image.alpha_composite(out, layer) if fill[3] == 0 else ImageChops.lighter(out, layer)
        save(np.asarray(out).astype(float), folder, f"{name}_back.png")


def carriage(folder):
    """수레와 호빗: 컷인에서 쓰던 장면 그림에서 그 부분만 오린다."""
    source = folder.parent / "CutIns/carriage"
    frames = [np.asarray(Image.open(source / f"cart_{k}.png").convert("RGBA")).astype(float) for k in range(4)]
    box = np.any([f[..., 3] > 0 for f in frames], axis=0)
    ys, xs = np.nonzero(box)
    for k, frame in enumerate(frames):
        save(frame[ys.min():ys.max() + 1, xs.min():xs.max() + 1], folder, f"cart_{k}.png")


def heroes(project, folder):
    """한 사람이 주인공인 장면의 그림: 난간의 디엘, 등 돌린 디엘, 옆모습 카일(모두 컷인에서 쓰던 도트)과 카일의 천사 날개.

    카일은 웹게임 원화 대신 도트를 쓴다(2026-11-26 사용자 판정: 도트 캐릭터가 더 낫다).
    날개는 옛 컷인과 같은 pixelfx 천사 날개의 다 펼친 장이다. 무대의 펼침(_Reveal)은 그림 가로 가운데에서 바깥으로 번지므로,
    날개 뿌리(effect.json의 pivot)가 가로 가운데에 오도록 왼쪽을 비운다. 뿌리는 그림 아래에서 (높이 - pivot y)만큼 위다.
    """
    source = folder.parent / "CutIns"
    for file, name in (("railing/diel.png", "diel_seated.png"), ("wings/diel.png", "diel_back.png"), ("descent/kyle.png", "kyle.png")):
        save(np.asarray(Image.open(source / file).convert("RGBA")).astype(float), folder, name)
    wings, (root_x, _) = cutin_descent.last_wing_frame(project)
    padded = Image.new("RGBA", (wings.width + wings.width - 2 * root_x, wings.height))
    padded.alpha_composite(wings, (wings.width - 2 * root_x, 0))
    save(np.asarray(padded).astype(float), folder, "kyle_wings.png")


def compartment(folder):
    rng = np.random.default_rng(71)
    w, h = 160, 160
    yy, xx = np.mgrid[0:h, 0:w]
    frame = (np.minimum(np.minimum(xx % 80, 79 - xx % 80), np.minimum(yy % 80, 79 - yy % 80)))
    grain = pg.noise(h, w, 6, rng)
    shade = 0.42 + np.sin(xx * 0.6 + grain * 7) * 0.05 + (grain - 0.5) * 0.22 + (frame < 4) * 0.18 - ((frame >= 4) & (frame < 6)) * 0.26 + ((frame >= 10) & (frame < 12)) * 0.12
    put(folder, "panel.png", np.dstack([tone(np.clip(shade, 0, 1), WOOD), np.full((h, w), 255)]))

    w, h = 110, 70
    image = empty(w, h)
    yy, xx = np.mgrid[0:h, 0:w]
    velvet = ramp("#240a12", "#46121e", "#6b1d2a", "#942c38", "#bd4a4c")
    back = (np.abs(xx - w / 2) < 50) & (yy > 4) & (yy < 44) & ~((yy < 12) & (np.abs(xx - w / 2) > 44))
    tuft = np.sin(xx * 0.4) * np.sin(yy * 0.4) * 0.18
    image[back, :3] = tone(np.clip(0.5 + tuft - (yy - 24) / 90, 0, 1), velvet)[back]
    seat = (np.abs(xx - w / 2) < 53) & (yy >= 44) & (yy < 58)
    image[seat, :3] = tone(np.clip(0.75 - (yy - 44) / 24 + tuft * 0.5, 0, 1), velvet)[seat]
    legs = (yy >= 58) & ((np.abs(xx - 8) < 4) | (np.abs(xx - w + 8) < 4))
    image[legs, :3] = WOOD[2]
    image[back | seat | legs, 3] = 255
    put(folder, "seat.png", image)

    w, h = 40, 130
    yy, xx = np.mgrid[0:h, 0:w]
    folds = np.sin(xx * 0.62 + np.sin(yy * 0.03) * 2) * 0.3
    velvet = ramp("#2a0a14", "#521424", "#7e2232", "#ab3644", "#d25c5c")
    image = np.dstack([tone(np.clip(0.5 + folds - yy / h * 0.15, 0, 1), velvet), np.full((h, w), 255)])
    image[(yy > 96) & (yy < 102), :3] = (222, 178, 96)
    put(folder, "curtain.png", image)

    w, h = 26, 44
    image = empty(w, h)
    glow = empty(w, h)
    glow[..., 3] = 255
    yy, xx = np.mgrid[0:h, 0:w]
    bracket = (np.abs(xx - 13) < 2) & (yy < 12) | (np.abs(yy - 12) < 2) & (np.abs(xx - 13) < 9) | (np.abs(yy - 34) < 2) & (np.abs(xx - 13) < 7)
    bulb = ((xx - 13) / 8) ** 2 + ((yy - 23) / 11) ** 2 < 1
    image[bulb, :3] = (255, 226, 160)
    image[bracket, :3] = (196, 150, 74)
    image[bulb | bracket, 3] = 255
    glow[bulb & ~bracket, :3] = (255, 214, 140)
    put(folder, "lamp.png", image)
    put(folder, "lamp_glow.png", glow)


def main(project):
    folder = project / "Assets/MiniPandoras/Art/PixelGen/CutInStage"
    folder.mkdir(parents=True, exist_ok=True)
    cradle(folder)
    cracks(folder)
    erad_tower(folder)
    castle_parts(folder)
    parchment(folder)
    wood(folder)
    city(folder)
    mountains(folder)
    carriage(folder)
    dirt(folder)
    demon_wings(folder)
    wings_from_behind(folder)
    compartment(folder)
    heroes(project, folder)
    for name, palette, seed in (("day", DAY, 81), ("dusk", DUSK, 82), ("night", NIGHT, 83), ("pale", PALE, 84)):
        clouds_of(folder, np.random.default_rng(seed), palette, f"cloud_{name}")


if __name__ == "__main__":
    main(Path(sys.argv[1]))
