"""7장 호빗 마차를 타고 산길을 달리는 컷인(사용자 요청 2026-11-03: 다음 장들의 그래픽을 생성기로). 웹게임 carriagePanel을 다시 그린다.

화면 전체가 창이다(배경 back은 비어 있다). 뒤에 풍경 네 겹을 깔고 컷인 화면이 겹마다 다른 빠르기로 옆으로 민다:
하늘은 멈추고, 먼 산은 느리게, 언덕과 길가 나무는 빠르게, 길은 더 빠르게. 그 앞에 짐수레에 앉은 일행(주인공·랑·디엘)과
수레를 끄는 호빗 둘을 네 장(cart_0~3)으로 그려 컷인 화면이 넘긴다: 호빗이 걷고, 바퀴살이 돌고, 수레가 덜컹인다.
인물은 웹게임 인물 그림(Art/WebGame/Characters/<id>/right_*.png, 가상 px 하나에 4px)을 그대로 붙인다.
대화창이 아래 4분의 1을 덮으므로 수레와 길은 그 위에 둔다(웹게임보다 30px 위).
  back.png               비어 있는 한 장(장면 크기)
  sky.png, far.png, hills.png, road.png   풍경 겹(sky는 화면 크기, 나머지는 화면 너비의 두 배로 가로로 되풀이)
  cart_0.png ~ cart_3.png                  수레와 일행(장면 크기, 나머지는 비어 있다)

사용: python Tools/PixelGen/cutin_carriage.py <프로젝트 루트>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from cutin_gods import color, lighten
from cutin_train import ellipse, grid, mask_paint, periodic, stepped

W, H = 768, 432
STRIP = W * 2
# 짐칸에 앉은 일행과, 작아서 짐칸 테두리에 가려지지 않게 올려 앉힌 높이(가상 px, 랑은 짐 위에 앉는다)
RIDERS = (("hero", 0), ("rang", 9), ("diel", 0))
# 가상 px: 수레 왼쪽, 짐칸 윗면, 일행이 앉는 간격, 호빗이 서는 자리, 길(위·아래)
CART_X, CART_Y = 130, 112
ROAD_TOP, ROAD_BOTTOM = 118, 146
WOOD = [color(c) for c in ("#3a2a1a", "#6a4a2a", "#8a6238", "#a88050")]
FRAMES = 4


def sky():
    yy, xx = grid(H, W)
    t = stepped(np.clip(yy / 220, 0, 1), 12)[..., None]
    image = np.zeros((H, W, 4))
    image[..., :3] = color("#7aa8dc") * (1 - t) + color("#f4dcb0") * t
    image[..., 3] = 1
    # 해와 햇무리, 옅은 구름 몇 덩이
    halo = np.clip(1 - np.hypot(xx - 600, yy - 64) / 90, 0, 1)
    mask_paint(image, stepped(halo, 5), color("#fff2cc"), 0.5)
    mask_paint(image, ellipse(yy, xx, 600, 64, 16, 16).astype(float), color("#fff8e0"))
    rng = np.random.default_rng(5)
    for cx, cy, size in ((140, 52, 22), (330, 34, 16), (470, 92, 18), (700, 130, 14)):
        puff = np.zeros((H, W), dtype=bool)
        for dx, dy, r in ((0, 0, 1.0), (1.1, -0.4, 0.8), (-1.1, 0.1, 0.7), (2.0, 0.2, 0.6)):
            puff |= ellipse(yy, xx, cx + dx * size, cy + dy * size, r * size * 1.4, r * size)
        mask_paint(image, puff.astype(float), color("#ffffff"), 0.7)
        mask_paint(image, (puff & (yy > cy + size * 0.3)).astype(float), color("#e8ecf4"), 0.6)
    return image


def far(rng):
    """먼 산줄기: 푸른 회색 봉우리와 눈 덮인 꼭대기, 산기슭의 옅은 안개."""
    yy, xx = grid(H, STRIP)
    image = np.zeros((H, STRIP, 4))
    ridge = 150 + periodic(STRIP, rng, 5, 26)[None, :] + periodic(STRIP, rng, 13, 6)[None, :]
    body = yy >= ridge
    mask_paint(image, body.astype(float), color("#7a8aa8"))
    mask_paint(image, (body & (yy < ridge + 10) & (ridge < 140)).astype(float), color("#eef2f8"))
    shade = body & (np.gradient(ridge, axis=1) > 0.3)
    mask_paint(image, shade.astype(float), color("#6a7896"))
    mask_paint(image, (body & (yy > 196)).astype(float), color("#9aaac0"), 0.6)
    return image


def hills(rng):
    """초록 언덕과 길가 숲: 둥근 언덕 위로 나무 덩이가 줄지어 서 있다."""
    yy, xx = grid(H, STRIP)
    image = np.zeros((H, STRIP, 4))
    top = 196 + periodic(STRIP, rng, 4, 12)[None, :]
    land = yy >= top
    mask_paint(image, land.astype(float), color("#7a9a52"))
    mask_paint(image, (land & (yy < top + 4)).astype(float), color("#94b462"))
    for k in range(18):
        x = k * STRIP / 18 + rng.uniform(-14, 14)
        base = 214 + rng.uniform(-6, 6)
        r = rng.uniform(18, 28)
        for dx in (-STRIP, 0, STRIP):
            crown = ellipse(yy, xx, x + dx, base - r * 0.8, r, r * 0.9)
            mask_paint(image, crown.astype(float), color("#4a6a32"))
            mask_paint(image, (crown & (xx < x + dx - r * 0.2) & (yy < base - r * 0.9)).astype(float), color("#5e8040"))
            trunk = (np.abs(xx + 0.5 - (x + dx)) < 3) & (yy > base - r * 0.2) & (yy < base + 14)
            mask_paint(image, trunk.astype(float), color("#5a3c28"))
    return image


def road(rng):
    """흙길과 길섶: 바퀴 자국 두 줄, 자갈, 아래쪽 풀밭(대화창 뒤로 들어간다)."""
    yy, xx = grid(H, STRIP)
    image = np.zeros((H, STRIP, 4))
    top, bottom = ROAD_TOP * 2, ROAD_BOTTOM * 2
    mask_paint(image, (yy >= top - 6).astype(float), color("#86a04e"))
    lane = (yy >= top) & (yy < bottom)
    mask_paint(image, lane.astype(float), color("#b89868"))
    mask_paint(image, (lane & (yy < top + 3)).astype(float), color("#cdb084"))
    for rut in (top + 18, top + 40):
        mask_paint(image, ((yy >= rut) & (yy < rut + 3) & lane).astype(float), color("#a08050"))
    for _ in range(70):
        x, y = rng.integers(0, STRIP - 12), rng.integers(top + 4, bottom - 4)
        image[y:y + 2, x:x + int(rng.integers(3, 9)), :3] = color("#a08050") if rng.random() < 0.6 else color("#d8c098")
    for _ in range(120):
        x, y = rng.integers(0, STRIP - 3), rng.integers(bottom + 2, H - 4)
        image[y:y + 3, x:x + 1, :3] = color("#5e7a36")
        image[y + 1:y + 3, x + 1:x + 2, :3] = color("#6e8a40")
    mask_paint(image, ((yy >= bottom) & (yy < bottom + 4)).astype(float), color("#6e8a40"))
    return image


def character(project, sheet_id, frame):
    """웹게임 인물 그림 한 장(오른쪽을 본다)과 기준점(가상 px)."""
    folder = project / f"Assets/MiniPandoras/Art/WebGame/Characters/{sheet_id}"
    sheet = json.loads((folder / "sheet.json").read_text(encoding="utf-8"))
    return Image.open(folder / f"right_{frame}.png").convert("RGBA"), sheet


def cart(frame):
    """수레(짐칸·끌채·바퀴 둘)를 점으로 그린다. 바퀴살은 장마다 조금씩 돈다. 돌려주는 값: RGBA 점 그림."""
    yy, xx = grid(H, W)
    image = np.zeros((H, W, 4))
    x0, y0 = CART_X * 2, CART_Y * 2
    bed = (xx >= x0) & (xx < x0 + 128) & (yy >= y0) & (yy < y0 + 28)
    mask_paint(image, bed.astype(float), WOOD[1])
    mask_paint(image, (bed & (yy < y0 + 6)).astype(float), WOOD[2])
    for board in range(1, 4):
        mask_paint(image, (bed & (np.abs(xx + 0.5 - (x0 + board * 32)) < 1) & (yy > y0 + 6)).astype(float), WOOD[0])
    shaft = (xx >= x0 + 128) & (xx < x0 + 188) & (yy >= y0 + 12) & (yy < y0 + 16)
    mask_paint(image, shaft.astype(float), WOOD[2])
    angle = frame * np.pi / 4 / FRAMES
    for wx in (x0 + 28, x0 + 100):
        wy = y0 + 34
        rim = ellipse(yy, xx, wx, wy, 14, 14) & ~ellipse(yy, xx, wx, wy, 10, 10)
        mask_paint(image, rim.astype(float), WOOD[0])
        for k in range(4):
            a = angle + k * np.pi / 4
            spoke = np.abs((xx + 0.5 - wx) * np.sin(a) - (yy + 0.5 - wy) * np.cos(a)) < 1.2
            mask_paint(image, (spoke & ellipse(yy, xx, wx, wy, 11, 11)).astype(float), WOOD[3])
        mask_paint(image, ellipse(yy, xx, wx, wy, 3, 3).astype(float), color("#2a2a30"))
    outline = (image[..., 3] > 0)
    ring = pg.neighbours(outline) & ~outline
    mask_paint(image, ring.astype(float), pg.OUTLINE.astype(float))
    return image


def cart_frame(project, frame):
    """수레와 일행 한 장(최종 크기, 4px = 가상 px 하나). 덜컹임은 0, 1, 2, 1px."""
    bump = (0, 1, 2, 1)[frame]
    dots = cart(frame)
    rgba = np.clip(dots, 0, 255).copy()
    rgba[..., 3] = np.clip(dots[..., 3], 0, 1) * 255
    picture = Image.fromarray(pg.upscale(rgba.astype(np.uint8)), "RGBA")
    shifted = Image.new("RGBA", picture.size)
    shifted.alpha_composite(picture, (0, -bump * 4))
    # 짐칸에 앉은 일행: 아래 38%를 잘라 앉은 모습으로, 짐칸 앞에 서는 순서는 뒤에서부터.
    composed = Image.new("RGBA", picture.size)
    for i, (sheet_id, lift) in enumerate(RIDERS):
        image, sheet = character(project, sheet_id, 0)
        cut = int(image.height * 0.62)
        seated = image.crop((0, 0, image.width, cut))
        x = CART_X + 16 + i * 16 - sheet["anchorX"]
        top = CART_Y - bump - sheet["height"] * 0.62 + 4 - lift
        composed.alpha_composite(seated, (int(round(x * 4)), int(round(top * 4))))
    composed.alpha_composite(shifted)
    # 수레를 끄는 호빗 둘: 걷는 그림을 장마다 넘기고, 걸음마다 살짝 뛴다.
    steps = (1, 0, 2, 0)
    for k, hx in enumerate((CART_X + 100, CART_X + 118)):
        image, sheet = character(project, "hobbit", steps[(frame + k) % FRAMES])
        hop = (0, 1, 0, 1)[(frame + k) % FRAMES]
        x, foot = hx - sheet["anchorX"], CART_Y + 22 - hop
        composed.alpha_composite(image, (int(round(x * 4)), int(round((foot - sheet["anchorY"]) * 4))))
    return composed


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/carriage"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(71)
    layers = {"sky": sky(), "far": far(rng), "hills": hills(rng), "road": road(rng)}

    def save(name, image):
        rgba = np.clip(image, 0, 255).copy()
        rgba[..., 3] = np.clip(image[..., 3], 0, 1) * 255
        Image.fromarray(pg.upscale(rgba.astype(np.uint8)), "RGBA").save(out / f"{name}.png")

    for name, image in layers.items():
        save(name, image)
    Image.new("RGBA", (W * 2, H * 2)).save(out / "back.png")
    frames = [cart_frame(project, k) for k in range(FRAMES)]
    for k, frame in enumerate(frames):
        frame.save(out / f"cart_{k}.png")
    preview = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 255))
    for name in layers:
        preview.alpha_composite(Image.open(out / f"{name}.png").crop((0, 0, W * 2, H * 2)))
    preview.alpha_composite(frames[0])
    preview.convert("RGB").save(project / "Docs/ArtUpgrade/PixelGen/cutin_carriage_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
