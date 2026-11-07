"""웹게임 주인공 서기 그림(down·right·up)에 방망이를 쥐여 PixelLab 애니메이션의 첫 장을 만든다(224×224, 주인공은 (56, 48)에).

방망이는 4배로 그려 줄여서 웹게임 그림처럼 가장자리가 부드럽다. 손은 원래 그림의 손을 다시 얹어 방망이를 쥔 것처럼 보이게 한다.
주인공 그림은 건드리지 않는다(사용자 요청 2026-11-03: 지금 인물과 최대한 비슷하게). 결과와 PixelLab 주문은 Docs/ArtUpgrade/PixelLab/hero_swing.md.

  Docs/ArtUpgrade/PixelLab/hero_swing/first_(down|right|up).png, first_frames_preview.png

사용: python Tools/PixelLab/swing_first_frames.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

PROJECT = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
SRC = PROJECT / "Assets/MiniPandoras/Art/WebGame/Characters/hero"
OUT = PROJECT / "Docs/ArtUpgrade/PixelLab/hero_swing"
SIZE = 224
AT = (56, 48)
SS = 4
WOOD = np.array([[0x3e, 0x2a, 0x1d], [0x6a, 0x48, 0x31], [0x8e, 0x66, 0x45], [0xb0, 0x86, 0x5c], [0xcc, 0xa4, 0x76]], float)
TAPE = np.array([[0x2c, 0x25, 0x33], [0x4a, 0x40, 0x54]], float)
INK = np.array([0x16, 0x10, 0x0c], float)

# 방향마다 손잡이 끝(손 안)과 방망이 머리 끝(원래 그림 px).
BATS = {
    "down_0": ((33.0, 108.0), (13.0, 156.0)),
    "right_0": ((50.0, 107.0), (80.0, 150.0)),
    "up_0": ((76.0, 105.0), (97.0, 152.0)),
}


def bat_layer(grip, tip):
    """224×224 RGBA 방망이 한 자루(줄이기 전 4배 크기에서 그린다)."""
    n = SIZE * SS
    yy, xx = np.mgrid[0:n, 0:n] / SS
    g = np.array(grip) + AT
    t = np.array(tip) + AT
    d = t - g
    length = np.linalg.norm(d)
    u = d / length
    px, py = xx - g[0], yy - g[1]
    along = (px * u[0] + py * u[1]) / length
    across = px * -u[1] + py * u[0]
    a = np.clip(along, 0, 1)
    radius = 2.0 + 3.0 * a ** 1.3
    dist = np.hypot(px - a * d[0], py - a * d[1])
    body = dist <= radius
    outline = dist <= radius + 1.3
    # 둥근 단면처럼: 왼쪽 위 빛을 받는 쪽(가로 방향 음수 쪽)이 밝다.
    light = np.clip(0.55 - across / (radius + 0.01) * 0.45, 0, 0.999)
    rgb = np.zeros((n, n, 3))
    rgb[outline] = INK
    wood = WOOD[(light * len(WOOD)).astype(int)]
    rgb[body] = wood[body]
    tape = body & (along < 0.3) & (along > -0.02)
    rgb[tape] = TAPE[(light[tape] > 0.5).astype(int)]
    alpha = outline.astype(float)
    image = np.dstack([rgb, alpha * 255])
    small = image.reshape(SIZE, SS, SIZE, SS, 4)
    al = small[..., 3].mean(axis=(1, 3))
    col = (small[..., :3] * small[..., 3:4]).sum(axis=(1, 3)) / np.maximum(small[..., 3].sum(axis=(1, 3))[..., None], 1e-6)
    return np.dstack([col, al]).clip(0, 255).astype(np.uint8)


def hand_mask(sprite):
    """원래 그림의 손(아래쪽 살빛) 둘레를 조금 넓힌 자리."""
    r, g, b, al = (sprite[..., i].astype(int) for i in range(4))
    skin = (al > 150) & (r > 170) & (r - b > 35) & (np.arange(sprite.shape[0])[:, None] > 95)
    grown = skin.copy()
    for _ in range(2):
        p = np.pad(grown, 1)
        grown = p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:] | grown
    return grown & (al > 0)


def main():
    previews = []
    for name, (grip, tip) in BATS.items():
        sprite = np.array(Image.open(SRC / f"{name}.png").convert("RGBA"))
        canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        canvas.alpha_composite(Image.fromarray(sprite), AT)
        canvas.alpha_composite(Image.fromarray(bat_layer(grip, tip)))
        hands = np.zeros((SIZE, SIZE, 4), np.uint8)
        mask = hand_mask(sprite)
        region = hands[AT[1]:AT[1] + sprite.shape[0], AT[0]:AT[0] + sprite.shape[1]]
        region[mask] = sprite[mask]
        canvas.alpha_composite(Image.fromarray(hands))
        canvas.save(OUT / f"first_{name.split('_')[0]}.png")
        bg = Image.new("RGBA", (SIZE, SIZE), (110, 140, 100, 255))
        bg.alpha_composite(canvas)
        previews.append(bg)
    sheet = Image.new("RGBA", (SIZE * 3 + 20, SIZE), (255, 255, 255, 255))
    for i, im in enumerate(previews):
        sheet.paste(im, (i * (SIZE + 10), 0))
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(OUT / "first_frames_preview.png")


if __name__ == "__main__":
    main()
