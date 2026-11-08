"""UI 키트의 부품 그림(Docs/UI_UX_GUIDE.md 5장). 모든 창이 이 부품을 함께 쓴다.

색은 지침의 표(웹게임 표지 「달빛 뱀열차」에서 뽑은 색)를 따른다.
가상 px 하나 = 2점으로 그린다(Unity에서 PPU 200, 점 필터). 늘려 쓰는 부품은 9-slice 테두리를 적어 둔다.

  Assets/MiniPandoras/Art/PixelGen/UI/Kit/frame.png   창 틀(32 px, 테두리 8). 밤하늘 판, 밝은 윗선과 어두운 아랫선, 깎은 모서리, 모서리의 등불 못
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/well.png    오목한 판(12 px, 테두리 3). 칸과 카드의 바탕. 윗선이 어둡고 아랫선이 밝아 파인 것처럼 보인다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/focus.png   고른 것의 테(12 px, 테두리 3). 등불색 한 줄, 가운데는 비었다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/button.png  버튼 판(16 px, 테두리 4). 회색조라서 Image 색으로 물들인다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/close.png   닫기 아이콘(12 px). 누르는 영역은 버튼 쪽에서 26 px로 둔다

사용: python Tools/PixelGen/ui_kit.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

D = 2                       # 가상 px 하나 = 2점
NIGHT = (8, 20, 33)         # 밤
SKY = (23, 42, 73)          # 밤하늘
SKY_LIGHT = (58, 75, 114)   # 판의 밝은 윗선
SKY_DARK = (13, 24, 40)     # 판의 어두운 아랫선
INNER = (37, 53, 85)        # 밤하늘 밝은
WELL = (15, 29, 51)         # 오목한 판의 바닥
MOON = (243, 233, 216)      # 달빛
LANTERN = (222, 175, 118)   # 등불
LANTERN_LIGHT = (247, 212, 173)
LANTERN_DARK = (138, 106, 62)


def blank(width, height):
    return np.zeros((height * D, width * D, 4), np.uint8)


def fill(rgba, x, y, w, h, color):
    """가상 px 단위 사각형을 칠한다."""
    rgba[y * D:(y + h) * D, x * D:(x + w) * D] = (*color, 255)


def plate(width, height, top, bottom, body, outline):
    """깎은 모서리 판: 바깥 먹선 1 px, 윗·왼쪽 밝은 선, 아래·오른쪽 어두운 선, 모서리는 1 px 깎는다."""
    rgba = blank(width, height)
    fill(rgba, 1, 0, width - 2, height, outline)
    fill(rgba, 0, 1, width, height - 2, outline)
    fill(rgba, 1, 1, width - 2, height - 2, body)
    fill(rgba, 2, 1, width - 4, 1, top)
    fill(rgba, 1, 2, 1, height - 4, top)
    fill(rgba, 2, height - 2, width - 4, 1, bottom)
    fill(rgba, width - 2, 2, 1, height - 4, bottom)
    return rgba


def frame():
    """창 틀 32×32 px, 테두리 8 px. 안쪽 4 px 자리에 가는 선을 한 번 더 두르고, 네 모서리에 등불 못을 박는다."""
    size = 32
    rgba = plate(size, size, SKY_LIGHT, SKY_DARK, SKY, NIGHT)
    inset = 4
    fill(rgba, inset, inset, size - inset * 2, 1, INNER)
    fill(rgba, inset, size - inset - 1, size - inset * 2, 1, INNER)
    fill(rgba, inset, inset, 1, size - inset * 2, INNER)
    fill(rgba, size - inset - 1, inset, 1, size - inset * 2, INNER)
    for x, y in ((2, 2), (size - 4, 2), (2, size - 4), (size - 4, size - 4)):
        fill(rgba, x, y, 2, 2, LANTERN)
        rgba[y * D, x * D] = (*LANTERN_LIGHT, 255)
        rgba[(y + 2) * D - 1, (x + 2) * D - 1] = (*LANTERN_DARK, 255)
    return rgba


def well():
    """오목한 판 12×12 px, 테두리 3 px. 판의 바깥선과 반대로 위·왼쪽이 어둡고 아래·오른쪽이 밝다."""
    return plate(12, 12, NIGHT, INNER, WELL, NIGHT)


def focus():
    """고른 것의 테 12×12 px, 테두리 3 px. 등불색 한 줄에 모서리를 깎고, 안쪽 모서리에 밝은 점을 찍는다."""
    size = 12
    rgba = blank(size, size)
    fill(rgba, 1, 0, size - 2, 1, LANTERN)
    fill(rgba, 1, size - 1, size - 2, 1, LANTERN)
    fill(rgba, 0, 1, 1, size - 2, LANTERN)
    fill(rgba, size - 1, 1, 1, size - 2, LANTERN)
    for x, y in ((1, 1), (size - 2, 1), (1, size - 2), (size - 2, size - 2)):
        fill(rgba, x, y, 1, 1, LANTERN_LIGHT)
    return rgba


def button():
    """버튼 판 16×16 px, 테두리 4 px. 흰 판이라 Image 색이 곧 버튼 색이 되고, 윗선은 밝게 아랫선은 어둡게 남는다."""
    return plate(16, 16, (255, 255, 255), (150, 150, 150), (215, 215, 215), (40, 40, 40))


def close():
    """닫기 아이콘 12×12 px: 달빛 X에 먹색 테두리. 글꼴의 × 기호 대신 쓴다."""
    size = 12
    ink = np.zeros((size, size), bool)
    for i in range(2, size - 2):
        ink[i, i] = ink[i, min(i + 1, size - 1)] = True
        j = size - 1 - i
        ink[i, j] = ink[i, max(j - 1, 0)] = True
    outline = np.zeros_like(ink)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            outline |= np.roll(np.roll(ink, dy, 0), dx, 1)
    rgba = blank(size, size)
    for y in range(size):
        for x in range(size):
            if ink[y, x]:
                fill(rgba, x, y, 1, 1, MOON)
            elif outline[y, x]:
                fill(rgba, x, y, 1, 1, NIGHT)
    return rgba


def main(root):
    kit = root / "Assets/MiniPandoras/Art/PixelGen/UI/Kit"
    kit.mkdir(parents=True, exist_ok=True)
    outputs = {
        "frame.png": frame(),
        "well.png": well(),
        "focus.png": focus(),
        "button.png": button(),
        "close.png": close(),
    }
    for name, rgba in outputs.items():
        Image.fromarray(rgba, "RGBA").save(kit / name)
        print((kit / name).relative_to(root).as_posix(), rgba.shape[1], "x", rgba.shape[0])


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
