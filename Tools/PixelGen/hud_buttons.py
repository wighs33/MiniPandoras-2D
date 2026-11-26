"""화면 조작의 버튼 그림과 왼쪽 아래 이동 패드.

버튼은 마름모다(2026-11-23 사용자 요청 「공격·조사·스킬·판도라 교체 버튼도 참고 그림처럼 마름모로」). 큰 행동 버튼 둘레에 작은 마름모들이
변을 맞대고 맞물린다. 테두리는 바깥 밤색 선과 굵은 선이고, 행동 버튼은 그 안에 가는 선을 한 겹 더 둘러 금빛이 번진다.

그림은 가상 px의 두 배 점으로 그린다. 고리는 회색조라서 게임이 스킬 색으로 물들인다.
이동 패드는 버튼 고리와 같은 금속 재료에 지침(Docs/UI_UX_GUIDE.md 3장)의 색을 바로 칠한다. 손을 대면 게임이 패드를 또렷하게 한다(MovePadView).

  Assets/MiniPandoras/Art/PixelGen/UI/Hud/disc.png, disc_large.png(흰 원판. 버튼 바탕, 다시 쓸 때까지의 원형 덮개)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/ring.png, ring_large.png(회색조 금속 고리)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/swap.png(교체 아이콘: 서로 쫓는 화살표 둘)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/diamond.png, diamond_large.png(흰 마름모. 버튼 바탕, 다시 쓸 때까지의 시계 덮개)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/diamond_frame.png, diamond_large_frame.png(회색조 테두리. 게임이 스킬 색·금빛으로 물들인다)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/diamond_large_glow.png(행동 버튼 둘레에 번지는 금빛, 버튼보다 사방으로 6 px 크다)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/action_attack.png, action_inspect.png, action_talk.png(행동 버튼 글자 뒤에 옅게 까는 칼·돋보기·말풍선)
  Assets/MiniPandoras/Art/PixelGen/UI/Hud/swap_arrows.png(마름모 교체 버튼 안의 화살표 둘, 판 없음)
  Docs/ArtUpgrade/PixelGen/hud_diamonds_preview.png(게임과 같은 자리로 맞물린 버튼 무리)
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

# 마름모 버튼(가상 px). 작은 칸은 대각선 26이고 28 간격의 마름모 격자에 놓인다. 행동 버튼은 작은 칸 두 개 폭(28 × 2 − 2)이다.
DIAMOND = 26
DIAMOND_LARGE = 54
GLOW = 6                           # 행동 버튼 둘레로 금빛이 번지는 폭
GLOW_GOLD = (255, 210, 120)
# 테두리 띠의 폭(마름모 가장자리에서 안쪽으로 잰 L1 거리, 점): 바깥 밤색 선, 굵은 선, 틈, 가는 선.
# 작은 칸은 스킬 아이콘이 꽉 차서 가는 선이 그림을 가로지르므로 한 겹만 두른다.
FRAME_BANDS = {DIAMOND: (2, 3, 0, 0), DIAMOND_LARGE: (2, 4, 4, 2)}

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


def diamond_field(size):
    """그림 좌표와, 마름모 가장자리에서 안쪽으로 잰 L1 거리(점). 가장자리 점이 0이고 밖은 음수다."""
    x, y, _ = field(size)
    return x, y, size * D / 2 - (np.abs(x) + np.abs(y))


def diamond(size):
    _, _, inside = diamond_field(size)
    rgba = np.zeros((*inside.shape, 4), np.uint8)
    rgba[inside >= 0] = (255, 255, 255, 255)
    return rgba


def diamond_frame(size):
    """두 겹 테두리. 굵은 선은 왼쪽 위 두 변이 밝고 오른쪽 아래 두 변이 조금 어둡다(빛이 왼쪽 위에서 온다)."""
    x, y, inside = diamond_field(size)
    edge, main, gap, thin = FRAME_BANDS[size]
    rgba = np.zeros((*inside.shape, 4), np.uint8)
    rgba[(inside >= 0) & (inside < edge)] = (*NIGHT_SKY, 255)
    band = (inside >= edge) & (inside < edge + main)
    lit = (x + y) < 0
    rgba[band & lit] = (255, 255, 255, 255)
    rgba[band & ~lit] = (205, 205, 205, 255)
    start = edge + main + gap
    rgba[(inside >= start) & (inside < start + thin)] = (175, 175, 175, 255)
    return rgba


def diamond_glow():
    """행동 버튼 둘레로 계단처럼 옅어지는 금빛. 그림은 버튼보다 사방으로 GLOW만큼 크다."""
    _, _, inside = diamond_field(DIAMOND_LARGE + GLOW * 2)
    outside = GLOW * D - inside
    rgba = np.zeros((*inside.shape, 4), np.uint8)
    for start, alpha in ((0, 160), (3, 105), (6, 60), (9, 28)):
        rgba[(outside >= start) & (outside < start + 3)] = (*GLOW_GOLD, alpha)
    return rgba


def action_icon(draw):
    """행동 버튼 글자 뒤에 옅게 까는 그림(30 px, 달빛 한 색). 게임이 Image 알파로 옅게 한다."""
    n = 30 * D
    canvas = Image.new("L", (n, n), 0)
    draw(ImageDraw.Draw(canvas), n)
    mask = np.array(canvas) > 127
    rgba = np.zeros((n, n, 4), np.uint8)
    rgba[mask] = (*MOON, 255)
    return rgba


def draw_sword(pen, n):
    """오른쪽 위를 겨눈 칼: 날, 칼끝, 코등이, 손잡이, 손잡이 끝."""
    pen.line([(13, n - 13), (n - 12, 12)], fill=255, width=7)
    pen.polygon([(n - 16, 8), (n - 6, 6), (n - 8, 16)], fill=255)
    pen.line([(8, n - 30), (30, n - 8)], fill=255, width=6)
    pen.line([(4, n - 4), (14, n - 14)], fill=255, width=6)
    pen.ellipse([0, n - 8, 8, n], fill=255)


def draw_magnifier(pen, n):
    """돋보기: 굵은 테의 알과 오른쪽 아래로 뻗은 손잡이."""
    pen.ellipse([6, 6, 40, 40], outline=255, width=7)
    pen.line([(36, 36), (n - 6, n - 6)], fill=255, width=10)


def draw_bubble(pen, n):
    """말풍선: 둥근 몸, 왼쪽 아래 꼬리, 가운데 점 셋(뚫린 자리)."""
    pen.rounded_rectangle([4, 8, n - 4, n - 18], radius=12, fill=255)
    pen.polygon([(14, n - 22), (28, n - 22), (10, n - 4)], fill=255)
    for cx in (18, 30, 42):
        pen.ellipse([cx - 4, 22, cx + 4, 30], fill=0)


def swap_arrows():
    """마름모 교체 버튼 안의 서로 쫓는 화살표 둘(16 px, 판 없음). 왼쪽 위가 밝다."""
    size = 16
    x, y, _ = field(size)
    arrows = arrow_mask(size, 8.5, 3.5, -170, 140) | arrow_mask(size, 8.5, 3.5, 10, 140)
    grown = arrows.copy()
    grown[1:, :] |= arrows[:-1, :]
    grown[:-1, :] |= arrows[1:, :]
    grown[:, 1:] |= arrows[:, :-1]
    grown[:, :-1] |= arrows[:, 1:]
    rgba = np.zeros((*x.shape, 4), np.uint8)
    rgba[grown & ~arrows] = INK
    rgba[arrows] = ARROW_DARK
    rgba[arrows & (x + y < 0)] = ARROW_LIGHT
    return rgba


def tinted(rgba, rgb, alpha=1.0):
    out = rgba.astype(float)
    out[..., :3] *= np.array(rgb) / 255
    out[..., 3] *= alpha
    return Image.fromarray(out.astype(np.uint8), "RGBA")


def diamond_preview(images, path):
    """풀밭 위에 게임과 같은 자리(TouchControls 프리팹, 오른쪽 아래가 원점)로 버튼 무리를 짜 맞춰 네 배로 보인다."""
    width, height = 110, 90
    sheet = Image.new("RGBA", (width * D, height * D), (84, 120, 62, 255))
    back = (10, 9, 18)

    def place(image, centre_x, centre_y):
        left = int(round((width + centre_x) * D - image.width / 2))
        top = int(round((height - centre_y) * D - image.height / 2))
        sheet.alpha_composite(image, (left, top))

    small = [(-47, 61, (120, 200, 255)), (-61, 47, (255, 150, 90)), (-61, 19, (110, 110, 120)), (-75, 33, (110, 110, 120))]
    for cx, cy, tint in small:
        place(tinted(images["diamond"], back, 0.78), cx, cy)
        place(tinted(images["diamond_frame"], tint), cx, cy)
    place(tinted(images["diamond"], back, 0.78), -19, 61)
    place(tinted(images["diamond_frame"], (255, 233, 160)), -19, 61)
    place(Image.fromarray(images["swap_arrows"], "RGBA"), -19, 61)
    place(Image.fromarray(images["diamond_large_glow"], "RGBA"), -33, 33)
    place(tinted(images["diamond_large"], back, 0.55), -33, 33)
    place(tinted(images["action_attack"], (255, 255, 255), 0.3), -33, 33)
    place(tinted(images["diamond_large_frame"], (255, 233, 160)), -33, 33)
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(path)


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
        "diamond": diamond(DIAMOND),
        "diamond_large": diamond(DIAMOND_LARGE),
        "diamond_frame": diamond_frame(DIAMOND),
        "diamond_large_frame": diamond_frame(DIAMOND_LARGE),
        "diamond_large_glow": diamond_glow(),
        "action_attack": action_icon(draw_sword),
        "action_inspect": action_icon(draw_magnifier),
        "action_talk": action_icon(draw_bubble),
        "swap_arrows": swap_arrows(),
        "pad_base": pad_base(),
        "pad_knob": pad_knob(),
    }
    for name, rgba in images.items():
        save(rgba, folder / f"{name}.png")

    preview([(images["ring"], (120, 220, 170)), (images["ring_large"], (255, 233, 160)), (images["swap"], (255, 255, 255))],
             root / "Docs/ArtUpgrade/PixelGen/hud_buttons_preview.png")
    pad_preview(images["pad_base"], images["pad_knob"], root / "Docs/ArtUpgrade/PixelGen/move_pad_preview.png")
    diamond_preview(images, root / "Docs/ArtUpgrade/PixelGen/hud_diamonds_preview.png")


if __name__ == "__main__":
    main()
