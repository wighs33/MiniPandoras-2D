"""CHAPTER 2 타이틀 무대(달밤의 거대고목)의 그림. 설명은 title_stage.py에 있다.

  trunk.png·trunk_glow.png 거대고목의 줄기, branches.txt 가지 끝 자리(잎을 거는 곳, 줄기 밑동 가운데에서 잰 칸),
  leaf.png·leaf_glow.png 빛나는 잎 송이 여덟 모양, dune_0~2.png 사구, mesa_0~2.png 고원, sand.png 물가의 모래

사용: python Tools/PixelGen/title_moon.py <프로젝트 루트>
"""
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from title_stage import chiselled, output_folder, ramp, save, tone

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "HD2D"))
import draw_trees  # noqa: E402

BARK = ramp("#0f1226", "#1b2140", "#2b345c", "#41507c", "#5f73a0", "#8ba3c8", "#c3d6ea")
SAND = ramp("#1a1d33", "#292f4c", "#40496c", "#5f6b8f", "#8794b3", "#b2bfd6", "#dde6f2")
MESA = ramp("#14122c", "#221d43", "#342c5e", "#4a3f79", "#665894", "#8b7cb3")
LEAF_NIGHT = ["#082326", "#0f3d3f", "#1a6460", "#2f938a", "#6accb5", "#c8fff0"]
DOTS_PER_TILE = 32


class Limbs:
    """둥근 줄기와 가지. 굽은 선을 따라 공을 찍으며 깊이와 면의 방향을 적는다."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.depth = np.full((h, w), -1e9)
        self.nx = np.zeros((h, w))
        self.ny = np.zeros((h, w))
        self.along = np.zeros((h, w))

    def stroke(self, a, b, c, d, r0, r1, z=0.0):
        """네 점의 삼차 베지에를 따라 굵기가 r0에서 r1로 가늘어진다."""
        a, b, c, d = (np.array(p, dtype=float) for p in (a, b, c, d))
        for t in np.linspace(0, 1, 160):
            x, y = (1 - t) ** 3 * a + 3 * t * (1 - t) ** 2 * b + 3 * t * t * (1 - t) * c + t ** 3 * d
            r = r0 + (r1 - r0) * t ** 0.8
            x0, x1 = max(0, int(x - r) - 1), min(self.w, int(x + r) + 2)
            y0, y1 = max(0, int(y - r) - 1), min(self.h, int(y + r) + 2)
            if x0 >= x1 or y0 >= y1:
                continue
            yy, xx = np.mgrid[y0:y1, x0:x1]
            dx, dy = (xx - x) / r, (yy - y) / r
            d2 = dx * dx + dy * dy
            depth = np.where(d2 <= 1, np.sqrt(np.clip(1 - d2, 0, None)) * r + z, -1e9)
            front = depth > self.depth[y0:y1, x0:x1]
            for target, value in ((self.depth, depth), (self.nx, dx), (self.ny, dy), (self.along, np.full_like(dx, t * 200))):
                target[y0:y1, x0:x1][front] = value[front]

    @property
    def mask(self):
        return self.depth > -1e8


def grow(rng, w, h):
    """거대고목의 뼈대: 땅을 움켜쥔 뿌리, 꼬인 몸통, 하늘로 펼친 큰 가지들. 돌려주는 값: 그린 것, 가지 끝 자리."""
    limbs = Limbs(w, h)
    middle, foot = w / 2, h - 12
    for side in (-1, 1):
        for k, (reach, r) in enumerate([(300, 17), (235, 22), (170, 26), (110, 30), (60, 30)]):
            y = foot + rng.uniform(-4, 6)
            limbs.stroke((middle + side * 22, foot - 150 + k * 10), (middle + side * (40 + reach * 0.22), foot - 30),
                         (middle + side * reach * 0.6, y - 20 + k * 3), (middle + side * reach, y + 8), r * 1.5, 3, z=k * 3)
    for k in range(9):
        turn = k - 4
        x1 = middle + turn * 9 * rng.choice([-1, 1]) + rng.uniform(-8, 8)
        limbs.stroke((middle + turn * 14 + rng.uniform(-4, 4), foot), (middle + turn * 20, foot - 130),
                     (middle - turn * 10, foot - 240), (x1, foot - 330), 34, 24, z=10 - abs(turn) * 4)
    tips = []
    crown = foot - 320
    for k, (dx, dy, r) in enumerate([(-400, -70, 26), (-340, -190, 28), (-230, -280, 28), (-100, -330, 30), (30, -355, 30), (150, -325, 30),
                                     (270, -262, 28), (365, -165, 27), (410, -50, 25), (-280, -40, 18), (290, -20, 18)]):
        start = np.array((middle + dx * 0.08, crown + 20 + abs(dx) * 0.05))
        end = np.array((middle + dx, crown + dy))
        limbs.stroke(start, (middle + dx * 0.3, crown - 30 + dy * 0.15), (middle + dx * 0.72, crown + dy * 0.72 + 30), end, r, 5, z=6 - k % 3 * 5)
        tips.append(end)
        for _ in range(3):
            t = rng.uniform(0.45, 0.85)
            base = start + (end - start) * t + np.array((0, 8 * (1 - t)))
            out = base + np.array((rng.uniform(-70, 70) + dx * 0.08, -rng.uniform(20, 80)))
            bend = (base + out) / 2 + np.array((rng.uniform(-14, 14), 8))
            limbs.stroke(base, bend, bend, out, 9, 2)
            tips.append(out)
    return limbs, tips, middle, foot


def trunk(folder, rng):
    w, h = 900, 720
    limbs, tips, middle, foot = grow(rng, w, h)
    mask = limbs.mask
    yy, xx = np.mgrid[0:h, 0:w]
    grain = np.sin(limbs.nx * 13 + pg.noise(h, w, 26, rng) * 9 + limbs.along * 0.05) * 0.09 + (pg.fractal_noise(h, w, rng) - 0.5) * 0.42
    grain -= (pg.noise(h, w, 5, rng) > 0.68) * 0.12
    shade = 0.36 - limbs.nx * 0.12 - limbs.ny * 0.22 + grain
    # 밑동의 문: 빛이 새어 나온다.
    above_ground = yy < foot + 2
    door = (((xx - middle) / 24) ** 2 + ((yy - (foot - 44)) / 52) ** 2 < 1) & above_ground & mask
    rim = (((xx - middle) / 30) ** 2 + ((yy - (foot - 44)) / 59) ** 2 < 1) & above_ground & mask & ~door
    image = np.zeros((h, w, 4))
    image[..., :3] = tone(np.clip(shade, 0, 1), BARK)
    image[..., 3] = mask * 255
    image[rim, :3] = BARK[5]
    image[door, :3] = BARK[0]
    glow = np.zeros((h, w, 4))
    glow[..., 3] = 255
    inner = np.clip(1 - np.sqrt(((xx - middle) / 24) ** 2 + ((yy - (foot - 30)) / 52) ** 2), 0, 1)
    glow[door, :3] = np.array([255, 226, 150]) * (0.35 + 0.65 * inner[door])[:, None]
    # 껍질 틈을 흐르는 빛줄기
    veins = (np.abs(np.sin(limbs.nx * 3.2 + pg.noise(h, w, 40, rng) * 10)) < 0.05) & (pg.noise(h, w, 18, rng) > 0.55) & mask & ~door
    glow[veins, :3] = (70, 200, 170)
    # 줄기에 난 창
    for _ in range(16):
        x, y = int(middle + rng.uniform(-60, 60)), int(foot - rng.uniform(110, 300))
        if mask[y, x]:
            image[y - 4:y + 4, x - 3:x + 3, :3] = BARK[0]
            glow[y - 3:y + 3, x - 2:x + 2, :3] = (255, 214, 130)
    save(pg.upscale(image.astype(np.uint8)), folder, "trunk.png")
    save(pg.upscale(glow.astype(np.uint8)), folder, "trunk_glow.png")
    lines = [f"{(x - middle) / DOTS_PER_TILE:.3f} {(foot - y) / DOTS_PER_TILE:.3f}" for x, y in tips]
    (folder / "branches.txt").write_text("\n".join(lines), encoding="utf-8")


def leaves(folder):
    """빛나는 잎 송이 여덟 모양. HD-2D 나무의 잎 그림(Tools/HD2D/draw_trees.py)을 밤빛으로 물들인다."""
    draw_trees.LEAF = LEAF_NIGHT
    cell_w, cell_h = draw_trees.LEAF_CELL
    image = Image.new("RGBA", (cell_w * draw_trees.LEAF_VARIANTS, cell_h), (0, 0, 0, 0))
    rng = random.Random(12)
    for k in range(draw_trees.LEAF_VARIANTS):
        draw_trees.draw_leaf_clump(image, k * cell_w, 0, rng, k < 4)
    pixels = np.asarray(image).astype(float)
    glow = np.zeros_like(pixels)
    glow[..., 3] = 255
    bright = (pixels[..., 3] > 0) & (pixels[..., 1] > 180)
    glow[bright, :3] = pixels[bright, :3]
    sparkle = (pixels[..., 3] > 0) & (np.random.default_rng(4).random(pixels.shape[:2]) > 0.985)
    glow[sparkle, :3] = (190, 255, 225)
    save(pixels, folder, "leaf.png")
    save(glow, folder, "leaf_glow.png")


def dunes(folder, rng):
    """사구: 등성이가 굽이치고, 달을 향한 면은 밝고 그늘진 면은 어둡다. 바람결 무늬가 등성이를 따라 흐른다."""
    for i, (w, h, waves) in enumerate([(960, 150, 5), (960, 190, 4), (960, 240, 3)]):
        yy, xx = np.mgrid[0:h, 0:w]
        knots = np.linspace(0, w, waves * 2 + 1)
        heights = np.array([rng.uniform(0.4, 0.85) if k % 2 else rng.uniform(0.2, 0.42) for k in range(len(knots))]) * h
        line = np.interp(np.arange(w), knots, heights)
        kernel = np.hanning(141)
        line = np.convolve(np.pad(line, 70, mode="edge"), kernel / kernel.sum(), mode="valid")
        line = line * np.sin(np.linspace(0, np.pi, w)) ** 0.45
        below = yy - (h - line[None])
        side = np.clip(-np.gradient(line) * 2.2, -1, 1)[None] * np.exp(-np.clip(below, 0, None) / (h * 0.5))
        ripple = np.sin(below * 0.55 + np.sin(xx * 0.03 + i) * 3 + pg.noise(h, w, 60, rng) * 8) * 0.022 * np.clip(below / 20, 0, 1)
        shade = 0.5 + side * 0.36 + ripple - below / h * 0.22 + (pg.noise(h, w, 90, rng) - 0.5) * 0.2
        shade = np.where(below < 2, np.maximum(shade, 0.72), shade)
        image = np.zeros((h, w, 4))
        image[..., :3] = tone(np.clip(shade, 0, 1), SAND)
        image[..., 3] = (below > 0) * 255
        save(pg.upscale(image.astype(np.uint8)), folder, f"dune_{i}.png")


def mesas(folder, rng):
    """사막의 고원: 층층이 깎인 바위 탁자."""
    for i, (w, h) in enumerate([(330, 220), (240, 250), (170, 180)]):
        yy, xx = np.mgrid[0:h, 0:w]
        up = 1 - yy / h
        half = np.interp(up, [0, 0.18, 0.3, 0.62, 0.64, 0.9, 1.0], np.array([1.0, 0.82, 0.7, 0.62, 0.55, 0.52, 0.5]) * w / 2)
        jag = (pg.noise(h, w, 20, rng) - 0.5) * 22 + (pg.noise(h, w, 6, rng) - 0.5) * 7
        mask = (np.abs(xx - w / 2) + jag < half) & (yy > 6 + (pg.noise(h, w, 22, rng) - 0.5) * 14)
        image = chiselled(mask, rng, MESA, size=19, light=(0.5, 0.55), round_by=0.55)
        # 가로로 쌓인 지층
        layer = np.floor((yy + pg.noise(h, w, 60, rng) * 18) / 17) % 3 == 0
        image[layer & mask, :3] *= 0.84
        save(pg.upscale(image.astype(np.uint8)), folder, f"mesa_{i}.png")


def shore(folder, rng):
    size = 256
    noise = pg.tileable_noise(size, 8, rng) * 0.5 + pg.tileable_noise(size, 32, rng) * 0.3
    yy, _ = np.mgrid[0:size, 0:size]
    ripple = np.sin((yy + pg.tileable_noise(size, 4, rng) * 40) * 2 * math.pi / 16) * 0.12
    image = np.zeros((size, size, 4))
    image[..., :3] = tone(np.clip(noise + ripple + 0.1, 0, 1), SAND[1:6])
    image[..., 3] = 255
    save(image, folder, "sand.png")


def main(project):
    folder = output_folder(project)
    trunk(folder, np.random.default_rng(71))
    leaves(folder)
    dunes(folder, np.random.default_rng(73))
    mesas(folder, np.random.default_rng(79))
    shore(folder, np.random.default_rng(83))


if __name__ == "__main__":
    main(Path(sys.argv[1]))
