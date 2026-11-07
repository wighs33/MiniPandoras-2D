"""3장 컷인 그림: 주인공이 디엘 등 뒤 조금 위에서 바라본 장면.

디엘은 화면 아래 가운데에서 등을 보이고, 병사들은 디엘 앞쪽 멀리 반원으로 서서 이쪽을 본다. 멀리 있으므로 디엘의 절반쯤 크기다.
날개는 pixelfx 효과(Tools/PixelFx/fx/demon/wings-unfurl.js)를 CutInView가 디엘 위에 겹쳐 튼다. 아지트는 카메라 뒤에 있어 보이지 않는다.
인물은 새로 그리지 않고 웹게임 그림(dielDemon 뒷모습, 병사 앞모습)을 쓴다.

  back.png      배경(768×432, 가상 화면 384×216의 두 배): 비 내리는 밤의 앞마당, 숲, 병사들
  diel.png      디엘 뒷모습(도트 그대로 네 배, 발까지만 자른다)
  glow.png      날개가 펴질 때 겹치는 보랏빛

디엘의 자리(발밑과 키)는 Chapter3의 WingsFoot·WingsHeight와 같아야 한다. 미리보기는 DemonWingsUnfurl을 뽑은 뒤에 만들 수 있다.

사용: python Tools/PixelGen/cutin_wings.py <프로젝트 루트>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

W, H = 768, 432
NIGHT = pg.hexes("#05060f", "#090b1a", "#0e1226", "#141a33")
GROUND = pg.hexes("#080f0e", "#0c1614", "#111f1b", "#172922", "#1e3329")
DIRT = pg.hexes("#14110f", "#1c1714", "#251e19")
TREE = pg.hexes("#04070c", "#070c12", "#0b1219")
RAIN = np.array([120, 140, 190])
PURPLE = np.array([150, 70, 230])

HORIZON = 72
FIGURE_SCALE = 4
FOOT = (192, 214)           # 디엘의 발밑(가상 px)
HEIGHT = 96                 # 디엘의 키(가상 px)
SOLDIER_OFFSETS = (0, -80, 80, -160, 160, -240, 240, -320, 320)     # 가운데에서 떨어진 거리(점)
SOLDIER_NEAR_FEET = 168     # 가운데 병사의 발(점). 양옆일수록 내려와 반원이 된다.
SOLDIER_SIDE_DROP = 46
SOLDIER_HEIGHT = 92         # 가운데 병사의 키(점). 디엘(192점)의 절반쯤이다.
SOLDIER_SIDE_GROWTH = 16


def treeline(c, rng):
    """먼 숲: 들쭉날쭉한 침엽수 머리."""
    x = -20
    while x < W + 20:
        width = int(rng.integers(34, 64))
        top = int(rng.integers(0, 40))
        shade = TREE[int(rng.integers(0, len(TREE)))]
        c.paint(c.polygon([(x, HORIZON + 6), (x + width / 2, top), (x + width, HORIZON + 6)]), shade)
        x += int(width * 0.6)


def back(project, rng):
    c = Canvas(W, H)
    t = np.clip(c.yy / HORIZON, 0, 1)
    level = np.clip((t * 3.4 + (pg.bayer(H, W) - 0.5) * 0.7).astype(int), 0, 3)
    c.paint(c.rect(0, 0, W, H), NIGHT[level])
    treeline(c, rng)

    depth = np.clip((c.yy - HORIZON) / (H - HORIZON), 0, 1)
    level = np.clip((depth * 4.5 + (pg.bayer(H, W) - 0.5) * 0.8).astype(int), 0, 4)
    ground = c.yy >= HORIZON
    c.image[ground, :3] = GROUND[level][ground]

    # 디엘 발밑에서 병사들 쪽으로 좁아지는 흙길(원근이 앞뒤를 알려 준다).
    half = 46 + depth * 150
    dirt = ground & (np.abs(c.xx - W / 2) < half)
    shade = np.clip((depth * 2.6 + (pg.bayer(H, W) - 0.5) * 0.8).astype(int), 0, 2)
    c.image[dirt, :3] = DIRT[shade][dirt]

    image = Image.fromarray(c.image, "RGBA").convert("RGBA")
    add_soldiers(image, project)
    pixels = np.array(image)
    add_rain(pixels, rng)
    return pixels[..., :3]


def add_soldiers(image, project):
    """디엘 앞쪽 멀리 이쪽을 보고 선 병사들. 멀수록 작고, 병사끼리 겹치면 가까운 쪽이 앞이다."""
    sprite = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/soldier/down_0.png").convert("RGBA")
    sprite = sprite.crop(sprite.getbbox())
    for offset in sorted(SOLDIER_OFFSETS, key=abs):
        side = (abs(offset) / 320) ** 2
        height = int(SOLDIER_HEIGHT + SOLDIER_SIDE_GROWTH * side)
        feet = int(SOLDIER_NEAR_FEET + SOLDIER_SIDE_DROP * side)
        small = shrink(sprite, height)
        image.paste(small, (W // 2 + offset - small.width // 2, feet - small.height), small)


def shrink(sprite, height):
    """줄이면서 가장자리가 흐려지지 않게 투명도를 둘로 나눈다. 밤이라 어둡게 눌러 둔다."""
    width = int(sprite.width * height / sprite.height)
    pixels = np.array(sprite.resize((width, height), Image.BOX))
    pixels[..., 3] = np.where(pixels[..., 3] > 110, 255, 0)
    pixels[..., :3] = (pixels[..., :3] * np.array([0.55, 0.55, 0.72])).astype(np.uint8)
    return Image.fromarray(pixels, "RGBA")


def add_rain(pixels, rng):
    """비: 살짝 기운 짧은 줄."""
    for _ in range(420):
        x = int(rng.integers(0, W))
        y = int(rng.integers(0, H))
        length = int(rng.integers(10, 26))
        for k in range(length):
            yy, xx = y + k, x - k // 4
            if 0 <= yy < H and 0 <= xx < W:
                pixels[yy, xx, :3] = (pixels[yy, xx, :3] * 0.72 + RAIN * 0.28).astype(np.uint8)


def diel(project):
    body = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/dielDemon/up_0.png").convert("RGBA")
    body = body.crop(body.getbbox())
    return body.resize((body.width * FIGURE_SCALE, body.height * FIGURE_SCALE), Image.NEAREST)


def glow():
    """디엘 둘레에서 번지는 보랏빛."""
    yy, xx = np.mgrid[0:H, 0:W]
    distance = np.hypot((xx - W / 2) / 360, (yy - 300) / 190)
    light = np.clip(1 - distance, 0, 1) ** 1.4
    light = np.clip(np.floor(light * 7 + (pg.bayer(H, W) - 0.5) * 0.9) / 7, 0, 1)
    pixels = np.zeros((H, W, 4), np.uint8)
    pixels[..., :3] = PURPLE
    pixels[..., 3] = (light * 110).astype(np.uint8)
    return pixels


def last_wing_frame(project):
    folder = project / "Assets/MiniPandoras/Art/PixelFx/DemonWingsUnfurl"
    info = json.loads((folder / "effect.json").read_text(encoding="utf-8"))
    width, height = info["frameSize"]
    index = info["frames"] - 1
    column, row = index % info["columns"], index // info["columns"]
    sheet = Image.open(folder / "color.png").convert("RGBA")
    return sheet.crop((column * width, row * height, (column + 1) * width, (row + 1) * height)), info["pivot"]


def preview(project, out):
    """게임과 같은 배치로 겹쳐 본다(점 하나가 가상 px 반이다)."""
    image = Image.open(out / "back.png").convert("RGBA")
    image.alpha_composite(Image.open(out / "glow.png"))
    body = Image.open(out / "diel.png")
    size = (int(body.width * HEIGHT * 2 / body.height), HEIGHT * 2)
    figure = body.resize(size, Image.NEAREST)
    feet = (FOOT[0] * 2, FOOT[1] * 2)
    image.alpha_composite(figure, (feet[0] - figure.width // 2, feet[1] - figure.height))
    wings, pivot = last_wing_frame(project)
    big = wings.resize((wings.width * 2, wings.height * 2), Image.NEAREST)
    image.alpha_composite(big, (feet[0] - pivot[0] * 2, feet[1] - pivot[1] * 2))
    image.save(project / "Docs/ArtUpgrade/PixelGen/cutin_wings_preview.png")


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/wings"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(53)
    Image.fromarray(back(project, rng), "RGB").save(out / "back.png")
    Image.fromarray(glow(), "RGBA").save(out / "glow.png")
    diel(project).save(out / "diel.png")
    preview(project, out)
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
