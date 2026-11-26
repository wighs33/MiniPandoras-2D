"""CHAPTER 1 타이틀 무대(동틀 녘의 뱀성)의 그림. 설명은 title_stage.py에 있다.

  castle.png·castle_glow.png 바위산 위의 뱀성(성 그림은 맵과 같은 castle.py의 것을 절반 크기로), ridge_0~2.png 숲 능선,
  cloud_0~3.png 아침 구름, soil.png 언덕의 땅, bird_0~2.png 멀리 나는 새

사용: python Tools/PixelGen/title_dawn.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import castle
import pixelgen as pg
from title_stage import chiselled, output_folder, ramp, save, tone

CRAG = ramp("#1d1c2b", "#2f2f42", "#46475c", "#62647a", "#83869b", "#a8acbd", "#cfd2dc")
GREENS = ramp("#1f2f22", "#31492c", "#4a6b36", "#6c8f45", "#97b45c")
CLOUD = ramp("#5c5585", "#7d6798", "#ad7c9f", "#dc9a9b", "#f9bf95", "#ffe0ab", "#fff6da")
WIDTH, HEIGHT = 760, 700
MIDDLE = WIDTH // 2
PLATEAU = 430               # 성벽이 서는 줄


def halve(image):
    """절반 크기로(도트를 골라 줄인다)."""
    picture = Image.fromarray(image.astype(np.uint8), "RGBA")
    return np.asarray(picture.resize((picture.width // 2, picture.height // 2), Image.NEAREST)).astype(float)


def over(canvas, image, left, top):
    h, w = image.shape[:2]
    y0, x0 = max(0, top), max(0, left)
    y1, x1 = min(canvas.shape[0], top + h), min(canvas.shape[1], left + w)
    part = image[y0 - top:y1 - top, x0 - left:x1 - left]
    solid = part[..., 3] > 127
    canvas[y0:y1, x0:x1][solid] = part[solid]


def stand(canvas, image, x, foot):
    over(canvas, image, int(x) - image.shape[1] // 2, int(foot) - image.shape[0])


def crag_into(canvas, rng, seed, peak, steps, size):
    """바위산을 깎아 얹는다. steps는 (높이 비율, 반폭) 목록이다."""
    height, width = canvas.shape[:2]
    yy, xx = np.mgrid[0:height, 0:width]
    half = np.interp(1 - yy / height, [s[0] for s in steps], [s[1] for s in steps])
    jag = (pg.noise(height, width, 26, rng) - 0.5) * 34 + (pg.noise(height, width, 9, rng) - 0.5) * 12
    mask = np.abs(xx - peak) + jag < half
    over(canvas, chiselled(mask, np.random.default_rng(seed), CRAG, size=size, light=(-0.35, 0.85), round_by=0.75), 0, 0)


def stairs(canvas):
    turns = [(MIDDLE, PLATEAU + 8), (MIDDLE + 34, 480), (MIDDLE - 30, 540), (MIDDLE + 24, 600), (MIDDLE, 668)]
    for (x0, y0), (x1, y1) in zip(turns, turns[1:]):
        for y in range(y0, y1):
            x = int(x0 + (x1 - x0) * (y - y0) / (y1 - y0))
            half = 7 + (y - PLATEAU) // 40
            canvas[y, x - half:x + half, :3] = CRAG[5 if (y // 2) % 2 else 3]
            canvas[y, x - half:x + half, 3] = 255


def pines(canvas, rng):
    """바위에 붙은 침엽수."""
    for _ in range(90):
        x, y = int(rng.uniform(20, WIDTH - 20)), int(rng.uniform(PLATEAU + 30, HEIGHT - 6))
        column = np.nonzero(canvas[:, x, 3])[0]
        if len(column) == 0 or y < column[0] + 4 or abs(x - MIDDLE) < 34:
            continue
        pine(canvas, x, y, int(rng.uniform(7, 15)), 2)


def pine(canvas, x, foot, size, light):
    h, w = canvas.shape[:2]
    tall = size * 2
    step = max(3, size // 4)
    for k in range(tall):
        y = foot - tall + k
        if not 0 <= y < h:
            continue
        half = max(1, int(k / tall * size * 0.55 + (k % step) * 0.4))
        x0, x1 = max(0, x - half), min(w, x + half)
        canvas[y, x0:x1, :3] = GREENS[0 if k % step == 0 else 1]
        canvas[y, x0:max(x0, x - half // 3), :3] = GREENS[light]
        canvas[y, x0:x1, 3] = 255


def castle_picture(folder):
    rng = np.random.default_rng(61)
    canvas = np.zeros((HEIGHT, WIDTH, 4))
    cone = halve(castle.tower(np.random.default_rng(4), width=76, body_h=190, roof="cone")[0])
    slim = halve(castle.tower(np.random.default_rng(7), width=64, body_h=240, roof="cone")[0])
    small = halve(castle.tower(np.random.default_rng(4), width=76, body_h=150, roof="cone")[0])
    tower = halve(castle.serpent_tower(np.random.default_rng(9))[0])
    wall = halve(castle.curtain_wall(9, np.random.default_rng(3))[0])
    gate = halve(castle.gatehouse(np.random.default_rng(2), closed=True)[0])

    # 뒤쪽 봉우리 둘과 그 위의 탑
    crag_into(canvas, rng, 5, 190, [(0, 170), (0.3, 120), (0.46, 64), (0.5, 22), (0.52, 0), (1, -60)], 22)
    crag_into(canvas, rng, 6, 590, [(0, 160), (0.28, 120), (0.42, 62), (0.455, 24), (0.47, 0), (1, -60)], 22)
    stand(canvas, cone, 192, 356)
    stand(canvas, cone, 590, 384)
    # 가운데: 돌뱀이 감아 오른 탑과 양옆의 가는 탑, 성이 앉은 바위산
    stand(canvas, slim, MIDDLE - 118, PLATEAU - 8)
    stand(canvas, slim, MIDDLE + 118, PLATEAU - 2)
    stand(canvas, tower, MIDDLE, PLATEAU + 4)
    crag_into(canvas, rng, 8, MIDDLE, [(0, 330), (0.16, 268), (0.3, 214), (0.36, 192), (0.378, 176), (0.385, -80), (1, -80)], 26)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    turf = (canvas[..., 3] > 0) & (yy >= PLATEAU - 2) & (np.abs(xx - MIDDLE) < 196)
    turf &= yy < PLATEAU + 5 + pg.noise(HEIGHT, WIDTH, 8, rng) * 12
    canvas[turf, :3] = tone(pg.fractal_noise(HEIGHT, WIDTH, rng), GREENS)[turf]
    for x in (MIDDLE - 48 - wall.shape[1] // 2, MIDDLE + 48 + wall.shape[1] // 2):
        stand(canvas, wall, x, PLATEAU + 6)
    stand(canvas, gate, MIDDLE, PLATEAU + 8)
    for x in (MIDDLE - 48 - wall.shape[1], MIDDLE + 48 + wall.shape[1]):
        stand(canvas, small, x, PLATEAU + 12)
    stairs(canvas)
    # 앞쪽 낮은 바위와 망루
    crag_into(canvas, rng, 11, 92, [(0, 84), (0.1, 72), (0.18, 52), (0.2, 40), (0.205, -60), (1, -60)], 20)
    crag_into(canvas, rng, 12, 668, [(0, 84), (0.1, 74), (0.15, 60), (0.172, 42), (0.178, -60), (1, -60)], 20)
    stand(canvas, small, 92, HEIGHT * 0.8 + 4)
    stand(canvas, small, 668, HEIGHT * 0.828 + 4)
    pines(canvas, rng)

    # 스스로 빛나는 자리: 창의 불과 돌뱀의 눈
    colour = canvas[..., :3]
    lit = (canvas[..., 3] > 0) & (colour[..., 0] > 195) & (colour[..., 1] > 70) & (colour[..., 0] - colour[..., 2] > 110)
    glow = np.zeros((HEIGHT, WIDTH, 4))
    glow[..., 3] = 255
    glow[lit, :3] = colour[lit]
    save(pg.upscale(canvas.astype(np.uint8)), folder, "castle.png")
    save(pg.upscale(glow.astype(np.uint8)), folder, "castle_glow.png")


def ridges(folder, rng):
    """숲 능선: 잡음 언덕선 위에 침엽수를 세운다. 앞쪽일수록 나무가 크다."""
    for i, (w, h, base, swing, size) in enumerate([(960, 150, 70, 46, 9), (960, 170, 80, 50, 15), (960, 200, 92, 56, 24)]):
        line = np.interp(np.arange(w), np.linspace(0, w, 13), base + rng.uniform(-1, 1, 13) * swing)
        # 양 끝은 물가로 내려앉는다(잘린 옆면이 보이지 않게).
        line = line * np.sin(np.linspace(0, np.pi, w)) ** 0.6 - 6
        yy, _ = np.mgrid[0:h, 0:w]
        shade = 0.35 + (pg.fractal_noise(h, w, rng) - 0.5) * 0.7 - (yy - (h - line)) / h * 0.2
        image = np.zeros((h, w, 4))
        image[..., :3] = tone(np.clip(shade, 0, 1), GREENS)
        image[..., 3] = (yy > h - line) * 255
        rows = sorted(((rng.uniform(0, w), rng.uniform(-size, h * 0.5)) for _ in range(int(w / size * 5))), key=lambda row: row[1])
        for x, down in rows:
            foot = h - line[min(w - 1, int(x))] + down
            if line[min(w - 1, int(x))] < size:
                continue
            pine(image, int(x), int(foot), int(size * rng.uniform(0.8, 1.4)), 3 if down < h * 0.12 else 2)
        save(pg.upscale(image.astype(np.uint8)), folder, f"ridge_{i}.png")


def clouds_of(folder, rng, palette, name):
    """구름 네 장: 둥근 송이를 쌓는다. 팔레트의 뒤쪽이 빛을 받는 쪽이다(아래에서 올라오는 빛)."""
    for i, (w, h, count) in enumerate([(340, 120, 34), (260, 84, 26), (420, 96, 40), (180, 56, 18)]):
        yy, xx = np.mgrid[0:h, 0:w]
        depth = np.full((h, w), -1e9)
        nx, ny = np.zeros((h, w)), np.zeros((h, w))
        for _ in range(count):
            t = rng.uniform(-1, 1)
            r = h * 0.3 * (1 - t * t * 0.75) * rng.uniform(0.55, 1.05)
            cx = w / 2 + t * (w / 2 - h * 0.2)
            cy = h * 0.8 - rng.uniform(0, 1) * (h * 0.5) * (1 - abs(t) ** 1.6)
            d2 = (xx - cx) ** 2 + ((yy - cy) * 1.1) ** 2
            z = np.where(d2 < r * r, np.sqrt(np.clip(r * r - d2, 0, None)) + rng.uniform(0, h * 0.1), -1e9)
            front = z > depth
            depth = np.where(front, z, depth)
            nx = np.where(front, (xx - cx) / r, nx)
            ny = np.where(front, (yy - cy) / r, ny)
        mask = (depth > -1e8) & (yy < h * 0.84 + (pg.noise(h, w, 12, rng) - 0.5) * 8)
        shade = 0.5 + ny * 0.34 - nx * 0.08 + (pg.fractal_noise(h, w, rng) - 0.5) * 0.18
        shade = np.where(yy > h * 0.74, shade + 0.12, shade)
        image = np.zeros((h, w, 4))
        image[..., :3] = tone(np.clip(shade, 0, 1), palette, dither=1.1)
        image[..., 3] = mask * 255
        save(pg.upscale(image.astype(np.uint8)), folder, f"{name}_{i}.png")


def soil(folder, rng):
    """언덕의 땅(풀 포기 사이로 조금 비친다). 이어 붙는 그림."""
    size = 256
    noise = pg.tileable_noise(size, 8, rng) * 0.6 + pg.tileable_noise(size, 32, rng) * 0.4
    image = np.zeros((size, size, 4))
    image[..., :3] = tone(noise, ramp("#1c2a1c", "#28401f", "#3a5828", "#4f7030"))
    image[..., 3] = 255
    save(image, folder, "soil.png")


def birds(folder):
    """멀리 나는 새: 날갯짓 세 장."""
    shapes = [
        ["..#.......#..", "...##...##...", ".....###.....", "......#......"],
        [".............", "..##.....##..", "....#####....", "......#......"],
        [".............", ".....###.....", "...##.#.##...", "..#.......#.."],
    ]
    for i, rows in enumerate(shapes):
        image = np.zeros((6, 14, 4))
        for y, row in enumerate(rows):
            for x, dot in enumerate(row):
                if dot == "#":
                    image[y + 1, x] = (46, 42, 66, 255)
        save(pg.upscale(image.astype(np.uint8)), folder, f"bird_{i}.png")


def main(project):
    folder = output_folder(project)
    castle_picture(folder)
    ridges(folder, np.random.default_rng(17))
    clouds_of(folder, np.random.default_rng(19), CLOUD, "cloud")
    soil(folder, np.random.default_rng(23))
    birds(folder)


if __name__ == "__main__":
    main(Path(sys.argv[1]))
