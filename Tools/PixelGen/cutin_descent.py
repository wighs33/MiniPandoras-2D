"""4장 컷인 그림: 마당에서 올려다본 저녁 하늘이 갈라지고 카일이 내려온다(빛의 기둥이 뜬금없었다).

구름에 뚫린 구멍에서 빛이 쏟아지고, 카일은 왼쪽(제이미의 집)을 보고 내려온다. 아래에는 집과 하늘을 올려다보는 병사들이 어둡게 서 있어 크기를 알려 준다.
날개는 pixelfx 효과(Tools/PixelFx/fx/angel/wings-unfurl.js)를 CutInView가 인물 위에 겹쳐 튼다. 인물은 새로 그리지 않고 웹게임 그림을 쓴다.

  back.png   배경(768×432, 가상 화면 384×216의 두 배)
  kyle.png   카일 옆모습(도트 그대로 네 배, 발까지만 자른다)
  glow.png   날개가 펴질 때 차오르는 빛

카일의 자리(발밑과 키)는 Chapter4의 DescentFoot·DescentHeight와 같아야 한다. 미리보기는 AngelWingsUnfurl을 뽑은 뒤에 만들 수 있다.

사용: python Tools/PixelGen/cutin_descent.py <프로젝트 루트>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps

import pixelgen as pg
from houses import Canvas

W, H = 768, 432
SKY = pg.hexes("#12122e", "#1b1a42", "#272452", "#393064", "#553e76", "#7c5282", "#a86c82", "#d09078")
CLOUD = pg.hexes("#1a1838", "#282450", "#3e3268", "#5f447a", "#8f5c80", "#c98878", "#f4c088", "#fff0c4")
SILHOUETTE = pg.hexes("#0b0a18", "#100f22", "#17152c")
LIGHT = (420, 120)              # 구름 구멍의 가운데(점)
HOLE = (250, 150)               # 구멍의 가로·세로 반지름(점)
RAY = np.array([255, 232, 170])
MEMORY_TINT = np.array([0.96, 0.98, 0.93])
GROUND_Y = 376                  # 어두운 땅이 시작하는 높이(점)
FOOT = (150, 186)               # 카일의 발밑(가상 px)
HEIGHT = 96                     # 카일의 키(가상 px)
FIGURE_SCALE = 4
SOLDIER_HEIGHT = 44             # 멀리 선 병사의 키(점)
SOLDIER_FEET = 416
SOLDIERS_X = (250, 300, 372, 430, 502, 560, 640, 700)


def hole_distance(xx, yy):
    """구름 구멍 가운데에서 잰 거리. 1이 구멍 가장자리다."""
    return np.hypot((xx - LIGHT[0]) / HOLE[0], (yy - LIGHT[1]) / HOLE[1])


def sky(xx, yy):
    """아래로 갈수록 노을이 번지고 구멍 안쪽은 빛으로 하얗게 탄다."""
    dither = pg.bayer(H, W) - 0.5
    level = np.clip((yy / H * 6.4 + dither * 0.7).astype(int), 0, 7)
    image = SKY[level].astype(float)
    light = np.clip(1.25 - hole_distance(xx, yy), 0, 1)
    tone = np.clip((light * 6 + dither * 0.8 + 2).astype(int), 0, 7)
    lit = light > 0.12
    image[lit] = CLOUD[tone][lit]
    return image


def clouds(image, xx, yy, rng):
    """구멍을 둘러싼 구름. 구멍 가까이는 빛을 받아 밝고 멀수록 어둡다."""
    dither = pg.bayer(H, W) - 0.5
    noise = pg.fractal_noise(H, W, rng)
    distance = hole_distance(xx, yy)
    thin = np.clip(1.45 - distance, 0, 1) * 0.42         # 구멍 쪽으로 갈수록 구름이 옅어진다.
    fades = np.clip((yy - 250) / 110, 0, 1) * 0.3        # 지평선 쪽은 맑다.
    threshold = 0.44 + thin + fades
    cloud = (noise > threshold) & (distance > 0.85)
    # 구름 가장자리는 얇아 하늘 색에 가깝고 속으로 갈수록 두껍고 밝다.
    depth = np.clip((noise - threshold) / 0.13, 0, 1)
    bright = np.clip(1.85 - distance * 0.8, 0, 1)
    tone = np.clip((bright * 4.4 + depth * 2.6 + dither * 0.9).astype(int), 0, 7)
    image[cloud] = CLOUD[tone][cloud]


def rays(image, xx, yy):
    """구멍에서 땅으로 부채꼴로 내리는 빛줄기."""
    angle = np.arctan2(xx - LIGHT[0], yy - LIGHT[1] + 40)
    strength = np.zeros((H, W))
    for center, width, power in ((-0.72, 0.09, 0.16), (-0.42, 0.06, 0.2), (-0.14, 0.1, 0.26), (0.2, 0.07, 0.22), (0.5, 0.1, 0.18), (0.8, 0.06, 0.14)):
        strength = np.maximum(strength, power * (np.abs(angle - center) < width))
    fall = np.clip(1 - (yy - LIGHT[1]) / (H - LIGHT[1]) * 0.75, 0, 1)
    mix = (strength * fall * (yy > LIGHT[1]))[..., None]
    image[:] = image * (1 - mix) + RAY * mix


def fade_to_memory(image, xx, yy):
    """4장은 회상이라 세상은 빛바랜 색이고, 빛의 기둥처럼 신성한 빛만 제 색이다. 구름 구멍에서 멀수록 색을 뺀다."""
    faded = image.mean(axis=2, keepdims=True) * MEMORY_TINT
    keep = np.clip(1.55 - hole_distance(xx, yy) * 1.1, 0, 1)[..., None]
    image[:] = faded * (1 - keep) + image * keep


def ground(c, rng):
    """어둡게 가라앉은 숲과 땅, 왼쪽의 제이미 집."""
    x = -20
    while x < W + 20:
        width = int(rng.integers(30, 60))
        top = GROUND_Y - int(rng.integers(4, 30))
        c.paint(c.polygon([(x, GROUND_Y + 8), (x + width / 2, top), (x + width, GROUND_Y + 8)]), SILHOUETTE[1])
        x += int(width * 0.55)
    c.paint(c.rect(0, GROUND_Y, W, H), SILHOUETTE[0])
    c.paint(c.polygon([(36, GROUND_Y + 14), (120, 322), (204, GROUND_Y + 14)]), SILHOUETTE[2])
    c.paint(c.rect(60, GROUND_Y + 8, 180, H), SILHOUETTE[1])
    c.paint(c.rect(152, 322, 170, 362), SILHOUETTE[2])
    c.paint(c.rect(106, GROUND_Y + 22, 122, GROUND_Y + 40), np.array([96, 70, 48]))


def dark_figure(path, height):
    """하늘을 올려다보는 사람 그림자: 웹게임 뒷모습 그림을 줄여 어둡게 누른다. 위쪽 가장자리에 빛이 얇게 든다."""
    sprite = Image.open(path).convert("RGBA")
    sprite = sprite.crop(sprite.getbbox())
    width = int(sprite.width * height / sprite.height)
    pixels = np.array(sprite.resize((width, height), Image.BOX))
    pixels[..., 3] = np.where(pixels[..., 3] > 110, 255, 0)
    pixels[..., :3] = (pixels[..., :3] * np.array([0.3, 0.3, 0.42])).astype(np.uint8)
    solid = pixels[..., 3] > 0
    top_edge = solid & ~np.vstack([np.zeros((2, width), bool), solid[:-2]])
    pixels[top_edge, :3] = np.array([206, 150, 110])
    return Image.fromarray(pixels, "RGBA")


def watchers(image, project):
    soldier = dark_figure(project / "Assets/MiniPandoras/Art/WebGame/Characters/soldier/up_0.png", SOLDIER_HEIGHT)
    for x in SOLDIERS_X:
        image.alpha_composite(soldier, (x - soldier.width // 2, SOLDIER_FEET - soldier.height))


def back(project, rng):
    yy, xx = np.mgrid[0:H, 0:W]
    pixels = sky(xx, yy)
    clouds(pixels, xx, yy, rng)
    rays(pixels, xx, yy)
    fade_to_memory(pixels, xx, yy)
    c = Canvas(W, H)
    c.image[..., :3] = pixels.astype(np.uint8)
    c.image[..., 3] = 255
    ground(c, rng)
    image = Image.fromarray(c.image, "RGBA").convert("RGBA")
    watchers(image, project)
    return image.convert("RGB")


def glow():
    """구멍에서 번지는 빛. 날개가 펴지며 화면을 덮는다."""
    yy, xx = np.mgrid[0:H, 0:W]
    light = np.clip(1.5 - hole_distance(xx, yy) * 0.62, 0, 1) ** 1.3
    light = np.clip(np.floor(light * 7 + (pg.bayer(H, W) - 0.5) * 0.9) / 7, 0, 1)
    pixels = np.zeros((H, W, 4), np.uint8)
    pixels[..., :3] = np.array([255, 240, 200])
    pixels[..., 3] = (light * 120).astype(np.uint8)
    return pixels


def kyle(project):
    body = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/kyle/right_0.png").convert("RGBA")
    body = ImageOps.mirror(body.crop(body.getbbox()))
    return body.resize((body.width * FIGURE_SCALE, body.height * FIGURE_SCALE), Image.NEAREST)


def last_wing_frame(project):
    folder = project / "Assets/MiniPandoras/Art/PixelFx/AngelWingsUnfurl"
    info = json.loads((folder / "effect.json").read_text(encoding="utf-8"))
    width, height = info["frameSize"]
    index = info["frames"] - 1
    column, row = index % info["columns"], index // info["columns"]
    sheet = Image.open(folder / "color.png").convert("RGBA")
    return sheet.crop((column * width, row * height, (column + 1) * width, (row + 1) * height)), info["pivot"]


def preview(project, out, wings_root):
    """게임과 같은 배치로 겹쳐 본다(점 하나가 가상 px 반이다)."""
    image = Image.open(out / "back.png").convert("RGBA")
    image.alpha_composite(Image.open(out / "glow.png"))
    body = Image.open(out / "kyle.png")
    size = (int(body.width * HEIGHT * 2 / body.height), HEIGHT * 2)
    figure = body.resize(size, Image.NEAREST)
    feet = (FOOT[0] * 2, FOOT[1] * 2)
    wings, pivot = last_wing_frame(wings_root)
    left, top = feet[0] - pivot[0], feet[1] - pivot[1]
    # alpha_composite는 음수 자리를 받지 않아 화면에 든 부분만 잘라 겹친다. 게임처럼 날개가 인물 뒤다.
    crop = (max(0, -left), max(0, -top), wings.width, wings.height)
    image.alpha_composite(wings.crop(crop), (max(0, left), max(0, top)))
    image.alpha_composite(figure, (feet[0] - figure.width // 2, feet[1] - figure.height))
    image.save(project / "Docs/ArtUpgrade/PixelGen/cutin_descent_preview.png")


def main(project, wings_root=None):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/descent"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(47)
    back(project, rng).save(out / "back.png")
    Image.fromarray(glow(), "RGBA").save(out / "glow.png")
    kyle(project).save(out / "kyle.png")
    preview(project, out, wings_root or project)
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else None)
