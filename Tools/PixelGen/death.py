"""사망 화면의 해골(쓰러지면 사망 표시와 해골이 뜬 뒤 고를 것이 나온다). 웹게임에는 없다.

정면을 보는 해골 하나: 어두운 뼈에 붉은 윤곽선을 살려 어둠 속에서 빛나 보이게 한다. 안쪽으로 처진 각진 눈구멍, 코 구멍,
광대 아래로 좁아지는 위턱과 이, 떨어져 나온 아래턱, 이마의 금. 윤곽선은 왼쪽 위가 밝고 오른쪽 아래가 어둡다.
둘레에 붉은 빛이 번지는 점 세 겹을 두른다.
창 그림처럼 가상 px의 두 배 점으로 그려 점 하나 = 2px로 키워 저장한다(가상 50×58).

  Assets/MiniPandoras/Art/PixelGen/UI/Death/skull.png

사용: python Tools/PixelGen/death.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

D = 2                       # 가상 px 하나 = 2점
BONE = pg.hexes("#6e121c", "#4c0c15", "#30070f")
LINE = pg.hexes("#ffb7a0", "#ff4f3e", "#c0222b")
INK = pg.hexes("#100308")[0]
GLOW = pg.hexes("#ff3a2a")[0]
HALO = 3                    # 빛이 번지는 두께(점). 그림 둘레에 이만큼 비워 둔다.
CX = 50                     # 얼굴 가운데(점)


def mirrored(points):
    """왼쪽 얼굴의 점들을 가운데 축으로 뒤집는다."""
    return [(CX * 2 - x, y) for x, y in points]


def light_of(c):
    """왼쪽 위에서 오는 빛의 세기(그 쪽이 1에 가깝다)."""
    return 1.0 - np.hypot((c.xx - 30) / 80.0, (c.yy - 12) / 96.0)


def rim(mask):
    """모양의 가장 바깥 한 겹."""
    return mask & pg.neighbours(~mask)


def body(c, shape):
    """뼈를 칠한다: 안쪽은 어둡게 세 단으로, 가장자리는 빛 쪽이 밝은 붉은 선으로."""
    light = light_of(c)
    c.paint(shape, BONE[2])
    c.paint(shape & (light >= 0.5), BONE[1])
    c.paint(shape & (light >= 0.68), BONE[0])
    edge = rim(shape)
    c.paint(edge, LINE[2])
    c.paint(edge & (light >= 0.42), LINE[1])
    c.paint(edge & (light >= 0.72), LINE[0])


def skull_shape(c):
    """머리뼈: 둥근 윗머리, 광대까지 넓은 얼굴, 그 아래 좁아지는 위턱."""
    cranium = c.ellipse(CX, 37, 37, 34)
    face = c.polygon([(15, 42), (85, 42), (78, 66), (22, 66)])
    maxilla = c.polygon([(30, 60), (70, 60), (67, 81), (33, 81)])
    return cranium | face | maxilla


def socket(c, points):
    """눈구멍: 코 쪽으로 처진 각진 구멍. 까맣게 파고 둘레에 붉은 선을 두른다."""
    hole = c.polygon(points)
    c.paint(pg.neighbours(hole) & ~hole, LINE[1])
    c.paint(hole, INK)


def nose(c):
    """코 구멍: 가운데 뼈를 사이에 둔 세모 둘."""
    left = [(49, 57), (49, 71), (42, 71)]
    for points in (left, mirrored(left)):
        hole = c.polygon(points)
        c.paint(pg.neighbours(hole) & ~hole, LINE[2])
        c.paint(hole, INK)


def teeth(c, x0, x1, top, bottom):
    """이: 붉은 이를 까만 세로 틈으로 나눈다."""
    row = c.rect(x0, top, x1, bottom)
    c.paint(row, LINE[1])
    c.paint(row & (c.xx >= CX + 6), LINE[2])
    c.paint(c.rect(x0, top, x1, top + 1), LINE[0])
    for x in range(x0 + 5, x1, 6):
        c.paint(c.rect(x, top, x + 2, bottom), INK)


def jaw(c):
    """아래턱: 머리뼈에서 떨어져 나와 살짝 벌어져 있다."""
    bone = c.polygon([(30, 87), (70, 87), (68, 100), (59, 107), (41, 107), (32, 100)])
    body(c, bone)
    teeth(c, 34, 67, 87, 93)


def crack(c):
    """이마의 금: 오른쪽 위에서 눈구멍 쪽으로 꺾이며 내려온다."""
    points = [(66, 7), (61, 15), (67, 21), (60, 30), (63, 36)]
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        c.line(x0, y0, x1, y1, LINE[1], width=2)


def halo(image):
    """그림 둘레에 붉은 빛을 두른다. 바깥 겹으로 갈수록 듬성듬성하고 옅다."""
    grown = image[:, :, 3] > 0
    checker = np.indices(grown.shape).sum(axis=0) % 2 == 0
    out = image.copy()
    for ring in range(HALO):
        band = pg.neighbours(grown) & ~grown
        pixels = band if ring == 0 else band & checker
        out[pixels, :3] = GLOW
        out[pixels, 3] = 110 - ring * 35
        grown = grown | band

    return out


def skull():
    """정면의 해골(가상 50×58)."""
    c = Canvas(50 * D, 58 * D)
    body(c, skull_shape(c))
    crack(c)
    left_eye = [(21, 43), (45, 49), (46, 56), (38, 63), (27, 63), (19, 53)]
    socket(c, left_eye)
    socket(c, mirrored(left_eye))
    nose(c)
    teeth(c, 34, 67, 75, 81)
    jaw(c)
    return halo(pg.outline(c.image, INK))


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/UI/Death"
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(skull()), "RGBA").save(out / "skull.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
