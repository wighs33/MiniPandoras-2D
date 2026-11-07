"""나리온: 요정들이 기르는 거대한 새(11장 정거장·하늘길·엔딩. 웹게임 gfx/narion.ts의 nariPerch·nariFly 자리).

오른쪽을 보는 커다란 새를 옆에서 그린다: 남색 등과 머리, 크림색 가슴과 배, 청록 날개깃(끝은 크림색), 뒤로 길게 뻗은 꼬리깃,
보랏빛 볏, 갈고리진 금빛 부리, 금빛 눈테. 등에는 꽃 안장(잎 덩굴을 감은 난간 두 줄, 양 끝에 분홍 꽃).
  perch   앉은 모습: 날개를 몸에 접고 금빛 다리로 선다
  fly0    날개를 위로 펼친 모습(다리는 배 밑에 접는다)
  fly1    날개를 아래로 내린 모습
웹게임과 같은 논리 좌표를 쓴다: 그림 288×136, 안장 높이 40, 발이 닿는 줄 109, 탄 사람 자리 x = 117 + 18i(꼬리 쪽부터 여섯).

  Assets/MiniPandoras/Art/PixelGen/Stage/narion_perch.png        앉은 새(아무도 타지 않음)
  Assets/MiniPandoras/Art/PixelGen/Stage/narion_perch_1~6.png    앉은 새와 꼬리 쪽부터 그만큼 탄 일행(정거장에서 한 사람씩 탈 때)
  Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly0.png, narion_fly1.png   날갯짓 두 장, 여섯이 다 탄 모습(정거장을 떠날 때, 하늘길·엔딩 컷인)
  Docs/ArtUpgrade/PixelGen/narion_preview.png

점 격자(논리 px 하나 = 2점)에 그려 두 배로 키운다(게임 그림은 논리 px 하나 = 4px).
사용: python Tools/PixelGen/narion.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

D = 2
W, H = 288, 136                    # 논리 px
SEAT_Y, GROUND_Y = 40, 109
RIDERS = ["largo", "isabel", "aurora", "diel", "hero", "rang"]
BLUE = pg.hexes("#141c4a", "#1e2c6a", "#2a3e8c", "#3c56aa", "#5672c4", "#7a94dc")
TEAL = pg.hexes("#0e3a52", "#165a78", "#227c9c", "#3aa0bc", "#6ac6d6", "#a8e4ea")
CREAM = pg.hexes("#a8987a", "#cfc2a2", "#e8dec4", "#f6f0e0", "#fffaf0")
GOLD = pg.hexes("#6a4a12", "#a8782a", "#dcae4a", "#f6dc8a")
CREST = pg.hexes("#4a2a7a", "#6a44a8", "#9a74d4", "#c8acf0")
LEAF = pg.hexes("#1e4a24", "#2e6a32", "#4a8a44", "#6aa85a")
BLOSSOM = pg.hexes("#c0587a", "#ec8eac", "#ffc8d8", "#fff0f4")
TALON = pg.hexes("#1a1a24", "#3a3a4a")
OUTLINE = np.array([12, 14, 34], dtype=np.uint8)


def seat_x(index):
    return 117 + index * 18


def P(x, y):
    """논리 좌표를 점으로."""
    return x * D, y * D


def shade_level(c, mask, light, palette, dither=0.12):
    c.paint(mask, pg.quantize(np.clip(light, 0, 0.999), palette, dither=dither))


def feather(c, root, tip, width, palette, light_bias=0.0):
    """깃털 하나: 뿌리에서 끝으로 가늘어지는 긴 잎꼴, 가운데 깃대. 끝 쪽 1/4은 크림색."""
    (x0, y0), (x1, y1) = P(*root), P(*tip)
    dx, dy = x1 - x0, y1 - y0
    length = max(1.0, np.hypot(dx, dy))
    ux, uy = dx / length, dy / length
    t = ((c.xx - x0) * ux + (c.yy - y0) * uy) / length
    s = (-(c.xx - x0) * uy + (c.yy - y0) * ux) / (width * D)
    half = np.sin(np.clip(t, 0, 1) * np.pi) ** 0.6 * 0.5 + 0.05
    vane = (t >= 0) & (t <= 1) & (np.abs(s) <= half)
    light = 0.62 - s * 0.6 - t * 0.15 + light_bias
    shade_level(c, vane, light, palette)
    c.paint(vane & (t > 0.78), CREAM[2])
    c.paint(vane & (t > 0.78) & (s > 0.15), CREAM[1])
    c.paint(vane & (np.abs(s) < 0.06) & (t < 0.85), palette[0])
    return vane


def blob(c, cx, cy, rx, ry):
    x, y = P(cx, cy)
    return c.ellipse(x, y, rx * D, ry * D)


def tail(c):
    """뒤로 길게 뻗은 꼬리깃 일곱(바깥 깃부터 그려 안쪽이 위에 온다)."""
    for k in range(7):
        root = (128 - k * 2, 66 + k * 2.2)
        tip = (14 + k * 6 + (k % 2) * 4, 48 + k * 7)
        feather(c, root, tip, 13 - k * 0.6, TEAL if k % 2 else BLUE, light_bias=0.05 * (k % 3))


def body(c):
    """몸통·가슴·목·머리. 등은 남색, 가슴과 배는 크림색 깃털 비늘."""
    torso = blob(c, 172, 76, 60, 19) | blob(c, 226, 68, 24, 23) | blob(c, 122, 72, 20, 11)
    neck = np.zeros_like(torso)
    for k in range(24):
        t = k / 23
        x, y = 234 + 16 * t, 62 - 32 * t
        neck |= blob(c, x, y, 13 - 2 * t, 13 - 2 * t)
    head = blob(c, 256, 30, 15, 13)
    shape = torso | neck | head
    yy, xx = c.yy / D, c.xx / D
    light = 0.72 - (yy - 20) / 120 - (xx < 140) * 0.08
    shade_level(c, shape, light, BLUE)
    # 크림색은 목 앞과 가슴, 몸 아랫면 띠만(등은 남색이 덮는다). 가슴의 경계는 목을 따라 내려오다 배 쪽으로 휘어
    # 세로로 곧게 잘린 선이 생기지 않게 한다.
    front = np.where(yy < 62, 236 + (62 - yy) * 0.5, 236 - np.clip(yy - 62, 0, None) ** 1.6 * 0.9)
    belly = shape & (((xx > front) & (yy > 34)) | (yy > 84 - (xx - 150) * 0.04))
    chest_light = 0.75 - (yy - 60) / 90
    shade_level(c, belly, chest_light, CREAM)
    scales = belly & (np.mod(xx * 0.5 + np.floor(yy / 3) * 1.5, 3) < 0.5)
    c.paint(scales, CREAM[1])
    back_scales = shape & ~belly & (np.mod(xx * 0.4 + np.floor(yy / 4) * 1.3, 4) < 0.5) & (yy > 40)
    c.paint(back_scales, BLUE[2])
    return shape


def head_details(c):
    """볏, 부리, 눈."""
    for k in range(4):
        root = (252 - k * 3, 21 + k)
        tip = (230 - k * 6, 7 + k * 3)
        feather(c, root, tip, 6 - k * 0.5, CREST)
    beak = c.polygon([P(267, 25), P(283, 31), P(282, 38), P(277, 42), P(269, 37)])
    hook = c.polygon([P(279, 35), P(284, 31), P(284, 42), P(279, 44)])
    c.paint(beak | hook, GOLD[2])
    c.paint(beak & (c.yy < P(0, 30)[1]), GOLD[3])
    c.paint(c.polygon([P(269, 36), P(280, 38), P(277, 40), P(269, 38)]), GOLD[1])
    eye_x, eye_y = P(260, 26)
    c.paint(c.ellipse(eye_x, eye_y, 4.2, 4.2), GOLD[2])
    c.paint(c.ellipse(eye_x, eye_y, 3, 3), OUTLINE)
    c.paint(c.ellipse(eye_x - 1, eye_y - 1, 1.2, 1.2), CREAM[4])


def folded_wing(c):
    """몸에 접은 날개: 덮깃(남색) 한 줄, 둘째 깃(청록) 한 줄, 뒤로 길게 뻗은 첫째 깃."""
    for k in range(8):
        feather(c, (214 - k * 9, 66 + (k % 2) * 2), (126 - k * 6, 92 + k * 1.2), 11, TEAL, 0.05)
    for k in range(7):
        feather(c, (226 - k * 10, 60), (176 - k * 10, 82), 10, TEAL, 0.12)
    for k in range(8):
        feather(c, (232 - k * 9, 56), (204 - k * 9, 70), 8, BLUE, 0.15)


def spread_wing(c, up):
    """펼친 날개(가까운 쪽): 어깨에서 위(또는 아래)로 부채처럼 퍼지는 깃 열둘."""
    # 위로 든 날개는 탄 사람들 뒤로 올라가 머리 위로 깃끝이 보이게 높이 세우되, 그림 위 끝은 넘지 않는다.
    shoulder = (206, 58)
    for k in range(12):
        a = np.radians((104 + k * 5) if up else (238 - k * 5))
        reach = (104 if up else 112) - abs(k - 5) * 4
        lift = 0.62 if up else 0.5
        tip = (shoulder[0] + np.cos(a) * reach, shoulder[1] - np.sin(a) * reach * lift)
        feather(c, (shoulder[0] - k * 4, shoulder[1] + (k % 2)), tip, 13, TEAL if k % 3 else BLUE, 0.04 * (k % 2))
    # 어깨의 덮깃.
    for k in range(6):
        feather(c, (222 - k * 8, 58), (196 - k * 9, 50 if up else 76), 9, BLUE, 0.15)


def legs(c, perched):
    # 다리 윗동은 배 깃털 속에서 시작해 몸에서 떠 보이지 않게 한다.
    if perched:
        for x in (190, 204):
            c.paint(c.polygon([P(x - 2, 90), P(x + 3, 90), P(x + 4, 106), P(x - 1, 106)]), GOLD[1])
            c.paint(c.polygon([P(x - 2, 90), P(x, 90), P(x + 1, 106), P(x - 1, 106)]), GOLD[2])
            for dx in (-5, 0, 5):
                c.paint(c.polygon([P(x + 1, 105), P(x + 2, 107), P(x + 3 + dx, 110), P(x + 1 + dx, 110)]), GOLD[1])
                c.paint(c.ellipse(*P(x + 3 + dx, 110), 1.5 * D, 1.0 * D), TALON[0])
    else:
        c.paint(blob(c, 202, 95, 6, 3), GOLD[1])
        c.paint(blob(c, 205, 96, 3, 1.5), TALON[1])


def saddle(c):
    """꽃 안장: 등을 따라 놓은 잎 덩굴 난간 두 줄, 양 끝에 꽃다발."""
    x0, x1 = 104, 240
    for y in (SEAT_Y - 1, SEAT_Y + 3):
        rail = c.rect(x0 * D, y * D, x1 * D, (y + 2) * D)
        c.paint(rail, GOLD[1])
        c.paint(rail & (np.mod(c.xx, 6) < 3), GOLD[2])
        c.paint(rail & (np.mod(c.xx + 3, 12) < 2), LEAF[2])
    for cx in (110, 232):
        c.paint(blob(c, cx, SEAT_Y + 1, 10, 7), LEAF[1])
        c.paint(blob(c, cx - 2, SEAT_Y - 1, 7, 4), LEAF[3])
        for k, (dx, dy) in enumerate(((-5, -2), (2, -4), (5, 1), (-1, 2))):
            c.paint(blob(c, cx + dx, SEAT_Y + dy, 2.4, 2.4), BLOSSOM[1])
            c.paint(blob(c, cx + dx - 0.6, SEAT_Y + dy - 0.6, 1.0, 1.0), BLOSSOM[3])


def bird(pose):
    c = Canvas(W * D, H * D)
    tail(c)
    body(c)
    if pose == "perch":
        folded_wing(c)
    else:
        spread_wing(c, up=pose == "fly0")
    head_details(c)
    legs(c, pose == "perch")
    saddle(c)
    image = c.image
    alpha = image[..., 3] > 0
    ring = pg.neighbours(alpha) & ~alpha
    image[ring, :3] = OUTLINE
    image[ring, 3] = 255
    return image


def with_riders(project, image, riders=RIDERS):
    """새 그림(최종 크기 RGBA)에 탄 사람을 얹는다(웹게임 drawNarion: 오른쪽을 보는 첫 그림, 위 67%만, 랑은 0.8배 전신).
    안장 앞쪽 난간(웹게임과 같은 x 108~215, 안장 높이부터 6)을 다시 덮어, 탄 사람의 허리가 안장 안에 들어간다."""
    import json
    picture = Image.fromarray(image, "RGBA") if not isinstance(image, Image.Image) else image.copy()
    bird_only = picture.copy()
    scale = picture.width / W
    for i, rider in enumerate(riders):
        folder = project / f"Assets/MiniPandoras/Art/WebGame/Characters/{rider}"
        sheet = json.loads((folder / "sheet.json").read_text(encoding="utf-8"))
        source = Image.open(folder / "right_0.png").convert("RGBA")
        cut = 1.0 if rider == "rang" else 0.67
        size = 0.8 if rider == "rang" else 1.0
        part = source.crop((0, 0, source.width, int(round(source.height * cut))))
        part = part.resize((int(round(part.width * size)), int(round(part.height * size))), Image.NEAREST)
        left = (seat_x(i) - sheet["anchorX"] * size) * scale
        top = (SEAT_Y + 3 - sheet["height"] * cut * size) * scale
        picture.alpha_composite(part, (int(round(left)), int(round(top))))
    if riders:
        box = tuple(int(v * scale) for v in (108, SEAT_Y, 215, SEAT_Y + 6))
        picture.alpha_composite(bird_only.crop(box), box[:2])
    return picture


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    out.mkdir(parents=True, exist_ok=True)
    frames = {pose: pg.upscale(bird(pose)) for pose in ("perch", "fly0", "fly1")}
    Image.fromarray(frames["perch"], "RGBA").save(out / "narion_perch.png")
    for count in range(1, len(RIDERS) + 1):
        with_riders(project, frames["perch"], RIDERS[:count]).save(out / f"narion_perch_{count}.png")
    for pose in ("fly0", "fly1"):
        with_riders(project, frames[pose]).save(out / f"narion_{pose}.png")
    preview = Image.new("RGBA", (W * 4, H * 4 * 3), (150, 190, 230, 255))
    for k, name in enumerate(("perch_6", "fly0", "fly1")):
        preview.alpha_composite(Image.open(out / f"narion_{name}.png"), (0, k * H * 4))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/narion_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
