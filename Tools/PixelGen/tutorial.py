"""튜토리얼 안내의 그림(어디를 눌러야 하는지 일일이 짚어 주는 튜토리얼). 웹게임에는 없다.

눌러야 할 버튼을 가리키는 금빛 화살표 하나(위를 가리킨다. 아래를 가리킬 때는 게임이 뒤집는다).
창 그림처럼 가상 px의 두 배 점으로 그려 점 하나 = 2px로 키워 저장한다(가상 12×14).

  Assets/MiniPandoras/Art/PixelGen/UI/Tutorial/arrow.png

사용: python Tools/PixelGen/tutorial.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas
from pandora_loadout import GOLD

D = 2                       # 가상 px 하나 = 2점
INK = pg.hexes("#0a0d1c")[0]


def arrow():
    """위를 가리키는 화살표(가상 12×14): 세모 머리와 굵은 대, 왼쪽 위에서 오는 빛으로 밝은 면과 그늘, 짙은 테두리."""
    w, h = 12 * D, 14 * D
    c = Canvas(w, h)
    head = c.polygon([(w / 2, 1), (w - 1, 13), (1, 13)])
    # 테두리가 그림 안에 들어가게 아래 끝에 한 점을 비워 둔다.
    shaft = c.rect(w / 2 - 4, 12, w / 2 + 4, h - 2)
    shape = head | shaft
    c.paint(shape, GOLD[2])
    c.paint(shape & (c.xx >= w / 2), GOLD[1])
    c.paint(shape & (c.xx < w / 2) & (c.xx >= w / 2 - 2) & (c.yy >= 6), GOLD[3])
    return pg.outline(c.image, INK)


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/UI/Tutorial"
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(arrow()), "RGBA").save(out / "arrow.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
