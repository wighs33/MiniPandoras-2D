"""화면 조작의 둥근 버튼 그림(모바일 게임처럼 공격 버튼 둘레에 둥근 버튼, 교체도 네모가 아닌 아이콘으로)과 왼쪽 아래 이동 패드.

그림은 가상 px의 두 배 점으로 그린다. 고리는 회색조라서 게임이 스킬 색으로 물들인다.
이동 패드는 버튼 고리와 같은 금속 재료에 지침(Docs/UI_UX_GUIDE.md 3장)의 색을 바로 칠한다. 손을 대면 게임이 패드를 또렷하게 한다(MovePadView).

  Assets/MiniPandoras/Art/PixelGen/UI/Hud/disc.png, disc_large.png(흰 원판. 버튼 바탕, 다시 쓸 때까지의 원형 덮개)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/ring.png, ring_large.png(회색조 금속 고리)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/swap.png(교체 아이콘: 서로 쫓는 화살표 둘)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/pad_base.png(이동 패드 바탕 60 px: 금속 고리, 옅은 밤색 안쪽, 다이얼 홈, 네 방향 꺾쇠)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/pad_knob.png(이동 패드 손잡이 28 px: 금속 테의 달빛 돌단추와 오른쪽 아래 그림자)
  Docs/ArtUpgrade/PixelGen/hud_buttons_preview.png, move_pad_preview.png(손을 대지 않은 패드와 오른쪽으로 민 패드)

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

# 이동 패드(가상 px). Unity의 Joystick(60)과 Knob(28)이 같은 크기다.
PAD = 60
PAD_RING = 5                       # 고리 두께(점)
KNOB = 28
ARROW_W, ARROW_H = 8, 5            # 방향 꺾쇠
ARROW_AT = 21                      # 꺾쇠가 놓이는 거리(손잡이 가장자리와 고리 사이)
NIGHT = (8, 20, 33)                # 지침의 색: 밤
NIGHT_SKY = (23, 42, 73)           # 밤하늘
NIGHT_SKY_LIGHT = (37, 53, 85)     # 밤하늘 밝은
MOON = (243, 233, 216)             # 달빛
FOG = (124, 136, 169)              # 안개
ARROW = (176, 186, 210)            # 꺾쇠(안개보다 밝게: 옅게 둔 패드에서도 보이게)
IDLE_ALPHA = 0.7                   # 손을 대지 않은 패드의 불투명도(Unity의 MovePadView와 같다)
# 금속 고리의 밝기 단계(ring_shape의 값)를 지침의 푸른 회색으로 옮긴다.
STEEL = {255: (198, 206, 226), 225: (160, 170, 198), 190: (124, 136, 169), 150: (86, 100, 136), EDGE: NIGHT_SKY}


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


def steel(rgba, body, level):
    """금속 고리를 밝기 단계마다 푸른 회색으로 칠한다."""
    for step, rgb in STEEL.items():
        rgba[body & (level == step)] = (*rgb, 255)


def pad_base():
    """
    이동 패드 바탕. 맵을 덜 가리도록 안쪽은 옅은 밤색이고, 둘레는 버튼과 같은 금속 고리(왼쪽 위에서 빛)다.
    고리 안쪽에 다이얼처럼 가는 홈을 한 줄 파고, 네 방향에 밝은 안개색 꺾쇠를 둔다(밀 수 있는 쪽을 알린다).
    """
    x, y, outer = field(PAD)
    r = np.hypot(x, y)
    rgba = np.zeros((*x.shape, 4), np.uint8)
    # 선형 색공간에서는 반투명이 계산보다 옅게 보여서 안쪽을 꽤 짙게 깐다.
    rgba[r <= outer - PAD_RING + 1] = (*NIGHT, 175)
    groove = np.abs(r - (outer - PAD_RING - 7)) <= 0.5
    rgba[groove] = (*NIGHT_SKY_LIGHT, 200)
    body, level = ring_shape(PAD, PAD_RING)
    steel(rgba, body, level)
    up = pad_arrow()
    centre = PAD * D // 2
    for turns_ccw, (dx, dy) in ((0, (0, -1)), (3, (1, 0)), (2, (0, 1)), (1, (-1, 0))):
        arrow = np.rot90(up, turns_ccw)
        h, w = arrow.shape[:2]
        top = centre + dy * ARROW_AT * D - h // 2
        left = centre + dx * ARROW_AT * D - w // 2
        mask = arrow[..., 3] > 0
        rgba[top:top + h, left:left + w][mask] = arrow[mask]
    return rgba


def pad_knob():
    """
    이동 패드 손잡이. 밤색 테두리 안에 금속 테, 그 안은 왼쪽 위가 밝은 달빛 돌단추이고, 가운데에 가는 홈이 있다.
    손잡이가 패드 위에 떠 보이도록 오른쪽 아래로 두 점 비낀 그림자를 깐다.
    """
    x, y, outer = field(KNOB)
    radius = outer - 2
    r = np.hypot(x, y)
    rgba = np.zeros((*x.shape, 4), np.uint8)
    rgba[np.hypot(x - 2, y - 2) <= radius] = (*NIGHT, 110)

    knob = r <= radius
    light = (-x - y) / (radius * math.sqrt(2))
    rim_level = np.select([light > 0.55, light > 0.0, light > -0.55], [255, 225, 190], 150)
    rim = knob & (r > radius - 4)
    steel(rgba, rim, rim_level)
    face = knob & (r <= radius - 4)
    rgba[face] = (*MOON, 255)
    rgba[face & (light > 0.4)] = (252, 247, 238, 255)
    rgba[face & (light < -0.3)] = (214, 203, 186, 255)
    rgba[face & (np.abs(r - radius * 0.42) <= 0.5)] = (190, 178, 160, 255)
    rgba[knob & (r > radius - 1)] = (*NIGHT, 255)
    return rgba


def pad_arrow():
    """방향 꺾쇠(위를 가리킨다). 밝은 안개색 굵기 2점의 「^」에 밤색 테두리를 두른다. 바탕이 돌려 네 방향에 붙인다."""
    w, h = ARROW_W * D, ARROW_H * D
    y, x = np.mgrid[0:h, 0:w] + 0.5
    apex_x, apex_y, foot_y = w / 2, 2.0, h - 2.0
    half = w / 2 - 2.0
    ink = np.zeros((h, w), bool)
    for side in (-1, 1):
        # 꼭짓점에서 양옆 아래로 내려가는 팔 하나(점에서 선분까지의 거리로 굵기 2점).
        ax, ay, bx, by = apex_x, apex_y, apex_x + side * half, foot_y
        t = np.clip(((x - ax) * (bx - ax) + (y - ay) * (by - ay)) / ((bx - ax) ** 2 + (by - ay) ** 2), 0, 1)
        ink |= np.hypot(x - (ax + t * (bx - ax)), y - (ay + t * (by - ay))) <= 1.1
    grown = ink.copy()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            grown |= np.roll(np.roll(ink, dy, 0), dx, 1)
    rgba = np.zeros((h, w, 4), np.uint8)
    rgba[grown & ~ink] = (*NIGHT, 255)
    rgba[ink] = (*ARROW, 255)
    return rgba


def pad_preview(base, knob, path):
    """풀밭 위에 손을 대지 않은 패드(게임에서 옅게 보인다)와 오른쪽으로 민 패드를 네 배로 보인다."""
    sheet = Image.new("RGBA", (PAD * D * 2 + 48, PAD * D + 32), (84, 120, 62, 255))
    centre = PAD * D // 2
    knob_image = Image.fromarray(knob, "RGBA")
    for column, (shift, alpha) in enumerate(((0, IDLE_ALPHA), (16 * D, 1.0))):
        layer = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
        left, top = 16 + column * (PAD * D + 16), 16
        layer.alpha_composite(Image.fromarray(base, "RGBA"), (left, top))
        layer.alpha_composite(knob_image, (left + centre + shift - knob_image.width // 2, top + centre - knob_image.height // 2))
        faded = np.array(layer)
        faded[..., 3] = (faded[..., 3] * alpha).astype(np.uint8)
        sheet.alpha_composite(Image.fromarray(faded, "RGBA"))
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(path)


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
        "pad_base": pad_base(),
        "pad_knob": pad_knob(),
    }
    for name, rgba in images.items():
        save(rgba, folder / f"{name}.png")

    preview([(images["ring"], (120, 220, 170)), (images["ring_large"], (255, 233, 160)), (images["swap"], (255, 255, 255))],
             root / "Docs/ArtUpgrade/PixelGen/hud_buttons_preview.png")
    pad_preview(images["pad_base"], images["pad_knob"], root / "Docs/ArtUpgrade/PixelGen/move_pad_preview.png")


if __name__ == "__main__":
    main()
