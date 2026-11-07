"""글라이더 화면의 하트 체력(화살을 세 번 맞으면 사망). 웹게임에는 없다.

왼쪽 위가 밝은 빨간 하트와, 잃었을 때 보이는 빈 하트. 노을 하늘 위에서도 보이게 어두운 윤곽선을 두른다.
가상 13×11px을 가상 1px = 2점으로 키워 저장한다(다른 글라이더 그림과 같은 밀도).

  Assets/MiniPandoras/Art/PixelGen/Glider/heart_full.png
  Assets/MiniPandoras/Art/PixelGen/Glider/heart_empty.png

사용: python Tools/PixelGen/glider_hearts.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

SHAPE = [
    "..###...###..",
    ".#rrr#.#rrr#.",
    "#rhhrr#rrrrs#",
    "#rhrrrrrrrrs#",
    "#rrrrrrrrrrs#",
    ".#rrrrrrrrs#.",
    "..#rrrrrrs#..",
    "...#rrrrs#...",
    "....#rrs#....",
    ".....#s#.....",
    "......#......",
]
SCALE = 2
INK = pg.hexes("#2a0d16")[0]
RED, LIGHT, SHADE, EMPTY = pg.hexes("#e8403e", "#ffb4a0", "#a8222e", "#4a2a34")
FULL = {"#": INK, "r": RED, "h": LIGHT, "s": SHADE}
HOLLOW = {"#": INK, "r": EMPTY, "h": EMPTY, "s": EMPTY}


def heart(colors):
    pixels = np.zeros((len(SHAPE), len(SHAPE[0]), 4), np.uint8)
    for y, row in enumerate(SHAPE):
        for x, mark in enumerate(row):
            if mark != ".":
                pixels[y, x, :3] = colors[mark]
                pixels[y, x, 3] = 255

    return Image.fromarray(pixels, "RGBA").resize((pixels.shape[1] * SCALE, pixels.shape[0] * SCALE), Image.NEAREST)


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Glider"
    heart(FULL).save(out / "heart_full.png")
    heart(HOLLOW).save(out / "heart_empty.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
