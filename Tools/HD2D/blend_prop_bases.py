"""HD-2D 시험 장면 소품(PixelLab 나무·바위·통나무)의 발밑 풀 치마를 잔디 색으로 바꾼다.

PixelLab 소품은 발밑에 샛노란 연두 풀 치마를 그려서 내놓는다. 장면에서는 그 둘레에 잔디 포기(draw_lawn.py)가 깔리므로
치마가 혼자 튀는 노란 얼룩이 된다(참고 게임은 나무 밑동이 잔디에 그대로 묻힌다). 그래서 그림 아래쪽 띠에서 풀색 도트만
밝기 순서를 지킨 채 잔디 색(draw_lawn.RAMP)으로 바꾼다. 나무의 위쪽 잎은 건드리지 않는다.

바위·통나무·그루터기의 이끼도 같은 까닭으로 잔디 색으로 바꾼다(형광 연두가 혼자 튄다).

원본: Tools/HD2D/pixellab/props/<이름>.png → 결과: Assets/MiniPandoras/Art/HD2DTest/Props/<이름>.png
사용: python Tools/HD2D/blend_prop_bases.py <Unity 프로젝트 루트>
"""
import colorsys
import sys
from pathlib import Path

from PIL import Image

from draw_lawn import RAMP

HERE = Path(__file__).resolve().parent
# 그림 높이에서 아래로 이만큼이 치마가 있는 띠다. 바위·통나무는 이끼도 형광 연두라 그림 전체의 풀색을 바꾼다.
BASE_BAND = {"oak": 0.12, "young": 0.1, "pine": 0.1, "boulder": 1.0, "log": 1.0, "stump": 1.0}
# 잔디 포기의 보통 밝기 다섯 칸(밑동 → 끝).
LAWN = RAMP[1:6]


def is_grass(r, g, b):
    hue, saturation, value = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return 0.15 < hue < 0.36 and saturation > 0.35 and value > 0.3


def lawn_colour(r, g, b):
    value = max(r, g, b) / 255
    index = min(len(LAWN) - 1, int((value - 0.3) / 0.7 * len(LAWN)))
    hex_colour = LAWN[max(0, index)]
    return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))


def main(project):
    out = Path(project) / "Assets/MiniPandoras/Art/HD2DTest/Props"
    for name, band in BASE_BAND.items():
        image = Image.open(HERE / "pixellab" / "props" / f"{name}.png").convert("RGBA")
        pixels = image.load()
        top = int(image.height * (1 - band))
        changed = 0
        for y in range(top, image.height):
            for x in range(image.width):
                r, g, b, a = pixels[x, y]
                if a > 0 and is_grass(r, g, b):
                    pixels[x, y] = lawn_colour(r, g, b) + (a,)
                    changed += 1
        image.save(out / f"{name}.png")
        print(name, changed)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else ".")
