"""갈아엎기 시범 장면: 새 집 두 채와 바위 언덕, 옛 그림(지금 집·절벽)을 나란히 둔다.

사용: python Tools/PixelGen/redesign_sample.py <프로젝트 루트>  → Docs/ArtUpgrade/PixelGen/redesign_sample.png
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import objects
import pixelgen as pg
import redesign
import terrain

T = pg.TILE


def main(project):
    rng = np.random.default_rng(11)
    cols, rows = 24, 13
    image = terrain.grass(rows * T, cols * T, rng)
    # 바위 언덕 둘: 왼쪽은 새 그림, 오른쪽 아래는 옛 그림(비교)
    kinds = np.full((rows, cols), "grass", dtype=object)
    kinds[9:11, 2:8] = "cliff"
    new_rocks = terrain.region(kinds == "cliff", pg.fractal_noise(rows * T, cols * T, rng) - 0.5, 0.4)
    redesign.rocks(image, new_rocks, rng)
    old = np.full((rows, cols), "grass", dtype=object)
    old[9:11, 16:22] = "cliff"
    terrain.cliff(image, terrain.region(old == "cliff", pg.fractal_noise(rows * T, cols * T, rng) - 0.5, 0.25), rng)
    scene = Image.fromarray(np.dstack([image, np.full(image.shape[:2], 255, np.uint8)]), "RGBA")

    def stand(sprite, left, top):
        scene.alpha_composite(Image.fromarray(sprite, "RGBA"), (int(left), int(top)))

    # 새 집(5칸, 지붕 3줄, 벽 2줄)과 옛 집(같은 칸)
    sprite, wall_corner = redesign.house(5, 3, 2, [1, 3], [2], rng)
    stand(sprite, 2 * T - wall_corner[0], 7 * T - wall_corner[1])
    old_house, _ = objects.house(5, 3, 2, [1, 3], [2], rng)
    stand(old_house, 16 * T, 2 * T)
    scene = scene.resize((scene.width * 2, scene.height * 2), Image.NEAREST)
    hero = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/hero/down_0.png").convert("RGBA")
    scene.alpha_composite(hero, (int(9.5 * 64 - hero.width / 2), int(8 * 64 - hero.height)))
    out = project / "Docs/ArtUpgrade/PixelGen/redesign_sample.png"
    scene.save(out)
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
