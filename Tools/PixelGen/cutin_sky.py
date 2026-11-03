"""11장 나리온을 타고 나는 하늘 컷인 둘(사용자 요청 2026-11-03: 다음 장들의 그래픽을 생성기로).
웹게임 skyRidePanel(하늘길)과 EndingScene(1부 엔딩)을 다시 그린다.

두 장면 모두 화면 전체가 창이다(배경 back은 비어 있다). 뒤에 풍경 겹을 깔고 컷인 화면이 겹마다 다른 빠르기로 옆으로 민다.
나리온과 탄 일행은 narion.py가 그린 날갯짓 두 장(Art/PixelGen/Stage/narion_fly0·1.png)을 컷인 인물로 넘긴다.
  skyRide/   하늘길: 맑은 낮 하늘과 해(sky, 멈춤), 발아래로 멀어지는 숲(forest), 먼 구름(far)·가까운 구름(near)·바로 앞 구름(front)
  ending/    1부 엔딩: 노을 하늘과 아이로스 쪽에 낮게 걸린 해(sky), 멀리 펼쳐진 구름 바다(far), 그 위 구름 덩이와 구름 바다 바닥(near),
             가까이 지나가는 구름(front)
겹은 반투명으로 겹치지 않고, 그 높이의 하늘 색과 웹게임처럼(sRGB) 미리 섞은 불투명 색으로 칠한다
(Unity는 선형 색 공간에서 섞어 옅은 흰 구름이 웹게임보다 훨씬 밝아진다).
sky는 화면 크기, 나머지 겹은 화면 너비의 두 배로 가로로 되풀이한다(점 하나 = 가상 px 반, 두 배로 키운다).

사용: python Tools/PixelGen/cutin_sky.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from cutin_gods import color
from cutin_train import ellipse, grid, mask_paint, stepped

W, H = 768, 432
STRIP = W * 2
# 구름 한 덩이를 이루는 원 넷(웹게임 cloudRow: 가운데에서 떨어진 정도와 반지름, 덩이 크기 배)
PUFF = ((0, 0, 1.0), (1.1, -0.4, 0.8), (-1.1, 0.1, 0.7), (2.0, 0.2, 0.6))


def gradient(stops):
    """세로 그라디언트 한 줄(높이 H, 점마다 RGB). stops는 (0~1 위치, 색)."""
    t = np.linspace(0, 1, H)
    where = [s for s, _ in stops]
    return np.stack([np.interp(t, where, [color(c)[k] for _, c in stops]) for k in range(3)], axis=-1)


def sky_image(stops, sun, sun_radius, glow_radius, glow_color, glow_alpha):
    """하늘 한 장: 단계를 끊은 그라디언트, 햇무리, 해."""
    yy, xx = grid(H, W)
    column = gradient(stops)
    image = np.zeros((H, W, 4))
    t = stepped(yy / (H - 1), 14)
    image[..., :3] = column[(t * (H - 1)).astype(int)]
    image[..., 3] = 1
    sx, sy = sun[0] * 2, sun[1] * 2
    halo = np.clip(1 - np.hypot(xx - sx, yy - sy) / (glow_radius * 2), 0, 1)
    mask_paint(image, stepped(halo, 5), color(glow_color), glow_alpha)
    mask_paint(image, ellipse(yy, xx, sx, sy, sun_radius * 2, sun_radius * 2).astype(float), color("#fff4d8"))
    return image, column


def cloud_row(behind, y, size, count, tint, alpha, rng, floor=None):
    """
    되풀이하는 구름 줄 한 겹: 덩이 count개를 고르게 놓는다(웹게임 cloudRow, 덩이마다 높이를 0·4·8px 엇갈림).
    behind(y) → 그 높이에서 구름 뒤에 보이는 색. 구름은 그 색과 tint를 alpha로 섞은 불투명 색이고 아랫면은 조금 어둡다.
    floor가 있으면 그 높이부터 아래는 구름 바다 바닥(불투명)으로 채운다.
    """
    yy, xx = grid(H, STRIP)
    image = np.zeros((H, STRIP, 4))
    puffs = np.zeros((H, STRIP), dtype=bool)
    shade = np.zeros((H, STRIP), dtype=bool)
    spacing = STRIP / count
    for i in range(count):
        cx = (i * spacing + rng.uniform(-0.15, 0.15) * spacing) * 1.0
        cy = (y + ((i * 13) % 3) * 4) * 2
        r = size * 2
        for wrap in (-STRIP, 0, STRIP):
            puff = np.zeros((H, STRIP), dtype=bool)
            for dx, dy, k in PUFF:
                puff |= ellipse(yy, xx, cx + wrap + dx * r, cy + dy * r, k * r, k * r)
            puffs |= puff
            shade |= puff & (yy > cy + r * 0.35)
    if floor is not None:
        # 바닥에 닿은 덩이 아랫면의 그늘은 바닥 위에 가는 띠로만 남으므로 지운다.
        shade &= yy < floor * 2 - 10
        puffs |= yy >= floor * 2
    lit = np.array([pg_mix(behind(row), color(tint), alpha) for row in range(H)])
    dim = np.array([pg_mix(behind(row), color(tint) * 0.93, alpha) for row in range(H)])
    image[..., :3] = np.where(shade[..., None], dim[yy], lit[yy])
    image[..., 3] = puffs
    return image


def pg_mix(back, front, alpha):
    """웹게임 캔버스처럼 sRGB 값끼리 섞는다."""
    return back * (1 - alpha) + front * alpha


def forest(column, rng):
    """발아래 멀어지는 숲: 화면 아래 끝에 걸친 둥근 나무 덩이 열일곱(웹게임 반지름 28, 간격 44)."""
    yy, xx = grid(H, STRIP)
    image = np.zeros((H, STRIP, 4))
    count = 17
    for i in range(count):
        cx = i * STRIP / count
        for wrap in (-STRIP, 0, STRIP):
            crown = ellipse(yy, xx, cx + wrap, (216 + 10) * 2, 56, 56)
            mask_paint(image, crown.astype(float), color("#7aa47a"))
            mask_paint(image, (crown & (xx < cx + wrap - 14) & (yy < 410)).astype(float), color("#8cb68a"))
            mask_paint(image, (crown & (xx > cx + wrap + 22)).astype(float), color("#6a946c"))
    return image


def sky_ride(out, rng):
    stops = ((0, "#6fa8e0"), (0.65, "#bfe0f4"), (1, "#fbe6c4"))
    sky, column = sky_image(stops, (324, 38), 10, 44, "#fff4d2", 0.55)
    behind = lambda row: column[row]
    layers = {
        "sky": sky,
        "forest": forest(column, rng),
        "far": cloud_row(behind, 64, 9, 9, "#ffffff", 0.55, rng),
        "near": cloud_row(behind, 170, 12, 8, "#ffffff", 0.7, rng),
        # 바로 앞 구름은 지평선 하늘과 숲에 걸쳐 있어, 덩이 안에 이음선이 생기지 않게 지평선 색 하나와 섞는다.
        "front": cloud_row(lambda row: column[H - 40], 196, 16, 8, "#ffffff", 0.85, rng),
    }
    save_all(out / "skyRide", layers)
    return layers


def ending(out, rng):
    stops = ((0, "#3a4a7a"), (0.45, "#c07a7a"), (0.75, "#f4c07a"), (1, "#ffe6a8"))
    sky, column = sky_image(stops, (318, 150), 13, 90, "#ffe2a0", 0.55)
    floor_color = color("#f6d2bc")
    behind = lambda row: column[row]
    layers = {
        "sky": sky,
        "far": cloud_row(behind, 176, 16, 10, "#f8d8c0", 0.75, rng),
        "near": cloud_row(behind, 196, 22, 9, "#f2c8b4", 0.9, rng, floor=206),
        "front": cloud_row(lambda row: floor_color, 214, 18, 9, "#ffe8d8", 0.8, rng),
    }
    # 구름 바다 바닥은 웹게임처럼 그 위 구름 덩이를 덮지 않고 같은 겹에서 이어 칠한다(바닥 색 그대로).
    near = layers["near"]
    near[412:, :, :3] = floor_color
    save_all(out / "ending", layers)
    return layers


def save_all(folder, layers):
    folder.mkdir(parents=True, exist_ok=True)
    for name, image in layers.items():
        rgba = np.clip(image, 0, 255).copy()
        rgba[..., 3] = np.clip(image[..., 3], 0, 1) * 255
        Image.fromarray(pg.upscale(rgba.astype(np.uint8)), "RGBA").save(folder / f"{name}.png")
    Image.new("RGBA", (W * 2, H * 2)).save(folder / "back.png")


def preview(project, folder, names, bird_height):
    """겹을 화면 한 장으로 겹치고 날개를 든 나리온을 얹은 미리보기."""
    picture = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 255))
    for name in names:
        picture.alpha_composite(Image.open(folder / f"{name}.png").crop((0, 0, W * 2, H * 2)))
    bird = Image.open(project / "Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly0.png")
    scale = bird_height * 4 / bird.height
    bird = bird.resize((int(bird.width * scale), int(bird.height * scale)), Image.NEAREST)
    return picture, bird


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns"
    rng = np.random.default_rng(111)
    sky_ride(out, rng)
    ending(out, rng)
    docs = project / "Docs/ArtUpgrade/PixelGen"
    ride, bird = preview(project, out / "skyRide", ("sky", "forest", "far", "near", "front"), 136)
    ride.alpha_composite(bird, (48 * 4, 24 * 4))
    end, small = preview(project, out / "ending", ("sky", "far", "near", "front"), 75)
    end.alpha_composite(small, (int((214 - small.width / 8) * 4), (175 - 75) * 4))
    sheet = Image.new("RGB", (W * 2, H * 4))
    sheet.paste(ride.convert("RGB"), (0, 0))
    sheet.paste(end.convert("RGB"), (0, H * 2))
    sheet.save(docs / "cutin_sky_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
