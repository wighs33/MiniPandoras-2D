"""화면 조작의 둥근 버튼 그림(모바일 게임처럼 공격 버튼 둘레에 둥근 버튼, 교체도 네모가 아닌 아이콘으로).

그림은 가상 px의 두 배 점으로 그린다. 고리는 회색조라서 게임이 스킬 색으로 물들인다.

  Assets/MiniPandoras/Art/PixelGen/UI/Hud/disc.png, disc_large.png(흰 원판. 버튼 바탕, 다시 쓸 때까지의 원형 덮개)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/ring.png, ring_large.png(회색조 금속 고리)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/swap.png(교체 아이콘: 서로 쫓는 화살표 둘)
  Docs/ArtUpgrade/PixelGen/hud_buttons_preview.png

사용: python Tools/PixelGen/hud_buttons.py <프로젝트 루트>
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

D = 2                  # 가상 px 하나 = 2점
SLOT = 26              # 스킬·판도라 칸의 지름(가상 px)
LARGE = 44             # 공격 버튼의 지름
SWAP = 18              # 교체 아이콘의 지름
EDGE = 96              # 고리 안팎 어두운 줄의 밝기
GOLD = np.array((232, 195, 90))
ARROW_LIGHT = (255, 233, 160, 255)
ARROW_DARK = (201, 150, 58, 255)
INK = (10, 13, 28, 255)
BACK = (14, 19, 40, 235)


def field(size):
    """그림 가운데를 원점으로 하는 점 좌표와 가장 바깥 반지름."""
    n = size * D
    centre = (n - 1) / 2
    y, x = np.mgrid[0:n, 0:n]
    return x - centre, y - centre, n / 2 - 0.5


def disc(size):
    x, y, outer = field(size)
    rgba = np.zeros((*x.shape, 4), np.uint8)
    rgba[np.hypot(x, y) <= outer] = (255, 255, 255, 255)
    return rgba


def ring_shape(size, thickness):
    """고리의 모양과 밝기(왼쪽 위에서 빛이 온다)."""
    x, y, outer = field(size)
    r = np.hypot(x, y)
    body = (r <= outer) & (r > outer - thickness)
    light = (-x - y) / (outer * math.sqrt(2))
    level = np.select([light > 0.55, light > 0.0, light > -0.55], [255, 225, 190], 150)
    level = np.where((r > outer - 1) | (r <= outer - thickness + 1), EDGE, level)
    return body, level


def ring(size, thickness):
    body, level = ring_shape(size, thickness)
    rgba = np.zeros((*body.shape, 4), np.uint8)
    rgba[..., :3] = level[..., None]
    rgba[..., 3] = body * 255
    return rgba


def arrow_mask(size, radius, thickness, start, sweep):
    """시계 방향으로 도는 화살표 하나의 모양. 각도는 그림 좌표(아래가 +)다."""
    x, y, _ = field(size)
    n = x.shape[0]
    r = np.hypot(x, y)
    angle = (np.degrees(np.arctan2(y, x)) - start) % 360
    shaft = (np.abs(r - radius) <= thickness / 2) & (angle <= sweep)
    head = Image.new("L", (n, n), 0)
    tip = math.radians(start + sweep)
    centre = (n - 1) / 2
    px, py = centre + radius * math.cos(tip), centre + radius * math.sin(tip)
    nx, ny = math.cos(tip), math.sin(tip)
    tx, ty = -math.sin(tip), math.cos(tip)
    ImageDraw.Draw(head).polygon(
        [(px + nx * 5.5, py + ny * 5.5), (px - nx * 5.5, py - ny * 5.5), (px + tx * 6.5, py + ty * 6.5)], fill=255)
    return shaft | (np.array(head) > 0)


def swap_icon():
    size = SWAP
    x, y, outer = field(size)
    rgba = np.zeros((*x.shape, 4), np.uint8)
    rgba[np.hypot(x, y) <= outer] = BACK
    body, level = ring_shape(size, 3)
    rgba[body, :3] = (GOLD * (level[body, None] / 255)).astype(np.uint8)
    rgba[body, 3] = 255

    arrows = arrow_mask(size, 8.5, 3.5, -170, 140) | arrow_mask(size, 8.5, 3.5, 10, 140)
    grown = arrows.copy()
    grown[1:, :] |= arrows[:-1, :]
    grown[:-1, :] |= arrows[1:, :]
    grown[:, 1:] |= arrows[:, :-1]
    grown[:, :-1] |= arrows[:, 1:]
    rgba[grown & ~arrows & ~body] = INK
    rgba[arrows] = ARROW_DARK
    rgba[arrows & (x + y < 0)] = ARROW_LIGHT
    return rgba


def save(rgba, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(path)


def preview(images, path):
    """풀밭 색 위에 스킬 색으로 물들인 모습을 네 배로 보인다."""
    sheet = Image.new("RGBA", (300, 110), (84, 120, 62, 255))
    x = 8
    for rgba, tint in images:
        tinted = rgba.astype(float)
        tinted[..., :3] *= np.array(tint) / 255
        picture = Image.fromarray(tinted.astype(np.uint8), "RGBA")
        sheet.alpha_composite(picture, (x, 8))
        x += picture.width + 8

    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(path)


def main():
    root = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    folder = root / "Assets/MiniPandoras/Art/PixelGen/UI/Hud"
    images = {
        "disc": disc(SLOT),
        "disc_large": disc(LARGE),
        "ring": ring(SLOT, 3),
        "ring_large": ring(LARGE, 4),
        "swap": swap_icon(),
    }
    for name, rgba in images.items():
        save(rgba, folder / f"{name}.png")

    preview([(images["ring"], (120, 220, 170)), (images["ring_large"], (255, 233, 160)), (images["swap"], (255, 255, 255))],
             root / "Docs/ArtUpgrade/PixelGen/hud_buttons_preview.png")


if __name__ == "__main__":
    main()
