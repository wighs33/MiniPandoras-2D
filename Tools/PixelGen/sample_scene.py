"""코드로 그린 지형·소품으로 게임 한 화면(24×14칸)을 꾸며, 주인공을 세워 크기와 그림체를 본다(시범).

사용: python Tools/PixelGen/sample_scene.py <프로젝트 루트>
  Docs/ArtUpgrade/PixelGen/sample_scene.png  게임 화면 크기(1536×896px)의 시범 장면
  Docs/ArtUpgrade/PixelGen/parts.png         소품 모음(2배)
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

COLUMNS, ROWS = 24, 14


def path_corners():
    """모서리 격자(25×15)에서 흙인 곳: 왼쪽 아래에서 오른쪽 위로 굽어 가는 길과 작은 공터."""
    corners = np.zeros((ROWS + 1, COLUMNS + 1), dtype=bool)
    points = [(-1, 11), (5, 10), (9, 7), (14, 7), (18, 4), (25, 3)]
    yy, xx = np.mgrid[0:ROWS + 1, 0:COLUMNS + 1]
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        t = np.clip(((xx - x0) * (x1 - x0) + (yy - y0) * (y1 - y0)) / ((x1 - x0) ** 2 + (y1 - y0) ** 2), 0, 1)
        d = np.hypot(xx - (x0 + t * (x1 - x0)), yy - (y0 + t * (y1 - y0)))
        corners |= d < 1.2
    corners |= np.hypot(xx - 11, (yy - 8) * 1.3) < 2.6
    return corners


def compose_ground(rng):
    """모서리 격자로 칸마다 모서리 타일을 골라 흙 모양을 잇고, 풀밭·흙 무늬를 입힌 뒤 경계를 칠한다."""
    grass = pg.grass_texture(rng)
    dirt = pg.dirt_texture(rng)
    masks = pg.corner_masks(rng)
    corners = path_corners()
    height, width = ROWS * pg.TILE, COLUMNS * pg.TILE
    is_dirt = np.zeros((height, width), dtype=bool)
    for row in range(ROWS):
        for column in range(COLUMNS):
            index = corners[row, column] * 1 + corners[row, column + 1] * 2 + corners[row + 1, column] * 4 + corners[row + 1, column + 1] * 8
            is_dirt[row * pg.TILE:(row + 1) * pg.TILE, column * pg.TILE:(column + 1) * pg.TILE] = masks[index]
    period = grass.shape[0]
    tile_y = np.arange(height) % period
    tile_x = np.arange(width) % period
    image = np.where(is_dirt[..., None], dirt[tile_y][:, tile_x], grass[tile_y][:, tile_x])
    return pg.paint_border(image, is_dirt, rng)


def paste(canvas, sprite, foot_x, foot_y, pivot):
    """sprite의 기준점(pivot)을 캔버스의 (foot_x, foot_y)에 맞춰 알파로 겹친다."""
    canvas.alpha_composite(Image.fromarray(sprite, "RGBA"), (int(foot_x - pivot[0]), int(foot_y - pivot[1])))


def main(project):
    rng = np.random.default_rng(20261102)
    ground = compose_ground(rng)
    scene = Image.fromarray(np.dstack([ground, np.full(ground.shape[:2], 255, np.uint8)]), "RGBA")

    props = [("tree", x, y, seed) for x, y, seed in ((3.5, 3.6, 1), (20.5, 10.2, 2), (7.2, 13.4, 3), (22.6, 13.8, 4), (1.2, 7.0, 5))]
    props += [("bush", x, y, seed) for x, y, seed in ((12.5, 4.2, 6), (16.8, 9.6, 7), (5.5, 6.2, 8), (2.4, 12.6, 9), (19.6, 6.6, 10))]
    props += [("rock", x, y, seed) for x, y, seed in ((8.6, 9.8, 11), (14.9, 5.1, 12), (17.6, 12.2, 13))]
    # 발밑이 위쪽인 것부터 그린다(아래쪽 소품이 앞에 온다).
    for kind, x, y, seed in sorted(props, key=lambda p: p[2]):
        sprite, pivot = getattr(pg, kind)(np.random.default_rng(seed))
        fx, fy = x * pg.TILE, y * pg.TILE
        shade = pg.shadow(int(sprite.shape[1] * 0.8), max(6, int(sprite.shape[1] * 0.22)))
        paste(scene, shade, fx + 3, fy + 1, (shade.shape[1] / 2, shade.shape[0] / 2))
        paste(scene, sprite, fx, fy, pivot)

    scene = scene.resize((scene.width * pg.SCALE, scene.height * pg.SCALE), Image.NEAREST)
    # 주인공(웹게임 그림, 이미 64px/칸)을 길 위 공터에 세운다. 발밑 = 그림 아래 가운데.
    hero = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/hero/down_0.png").convert("RGBA")
    hx, hy = 11 * 64, int(8.9 * 64)
    scene.alpha_composite(hero, (hx - hero.width // 2, hy - hero.height))

    out = project / "Docs/ArtUpgrade/PixelGen"
    out.mkdir(parents=True, exist_ok=True)
    scene.save(out / "sample_scene.png")

    parts = Image.new("RGBA", (700, 290), (40, 40, 46, 255))
    for i, kind in enumerate(("tree", "bush", "rock")):
        sprite, _ = getattr(pg, kind)(np.random.default_rng(i + 1))
        image = Image.fromarray(sprite, "RGBA")
        parts.alpha_composite(image.resize((image.width * pg.SCALE, image.height * pg.SCALE), Image.NEAREST), (10 + i * 230, 10))
    parts.save(out / "parts.png")
    print(out / "sample_scene.png")


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
