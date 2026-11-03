"""6장 첫 장면 뱀열차 객실(밤)의 컷인(사용자 요청 2026-11-03: 다음 장의 맵과 그래픽). 웹게임 trainWindowPanel을 생성기로 다시 그린다.

등불이 켜진 나무 객실의 창으로 달빛 어린 들판이 흘러간다. 창 유리 자리는 배경에서 비워 두고, 그 뒤에 풍경 네 겹을 둔다
(컷인 화면이 겹마다 다른 빠르기로 옆으로 민다: 하늘은 멈추고, 먼 산은 느리게, 나무는 빠르게, 바로 앞 풀숲은 더 빠르게).
뱀열차는 살아 있는 뱀과 열차가 합쳐진 짐승이라, 천장 들보는 갈비뼈처럼 휘고 창 아래 징두리에는 비늘 무늬가 있다.
  back.png    객실(가상 384×216): 판벽, 갈비뼈 들보, 창틀과 가운데 창살, 커튼, 벽등 둘, 비늘 징두리, 창턱의 찻잔과 책, 앞쪽 붉은 좌석.
  glow.png    벽등 빛무리, 창턱에 떨어지는 달빛, 유리에 비친 희미한 반사.
  sky.png     창 크기의 밤하늘과 달(움직이지 않는다).
  far.png, near.png, close.png   가로로 되풀이되는 먼 산과 들, 나무와 덤불·울타리, 바로 앞 풀숲과 이정표 돌(창 너비의 두 배).
창 자리 WINDOW(가상 px)는 컷인 장면 등록(DialogueUiBuilder.TrainWindow)과 같다.

사용: python Tools/PixelGen/cutin_train.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from cutin_gods import WHITE, color, lighten

W, H = 768, 432
WINDOW = (72, 26, 240, 104)                      # 가상 px: 왼쪽, 위, 너비, 높이
WX, WY, WW, WH = (v * 2 for v in WINDOW)         # 점
STRIP = WW * 2                                   # 되풀이 그림 너비(점)
WOOD = [color(c) for c in ("#160f0c", "#1e1612", "#2a1e18", "#3a2a1f", "#4a3426", "#5e4432", "#7a5a40")]
VELVET = [color(c) for c in ("#2a0c10", "#3e1418", "#4e2020", "#682a2c", "#84383a")]
BRASS = [color(c) for c in ("#5a4018", "#8a6424", "#b88a38", "#e0b860", "#fff0b0")]


def stepped(value, levels):
    """0~1 값을 levels 단계로 끊고 경계는 4×4 디더로 섞는다(창 밖 겹은 장면과 크기가 달라 디더도 그 크기로)."""
    return np.clip(np.floor(value * levels + (pg.bayer(*value.shape) - 0.5) * 0.9) / levels, 0, 1)


def mask_paint(image, mask, rgb, alpha=1.0):
    a = (mask * alpha)[..., None]
    image[..., :3] = image[..., :3] * (1 - a) + rgb * a
    if image.shape[-1] == 4:
        image[..., 3] = np.maximum(image[..., 3], mask * alpha)


def grid(height, width):
    return np.mgrid[0:height, 0:width]


def ellipse(yy, xx, cx, cy, rx, ry):
    return ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 <= 1


def periodic(width, rng, cycles, amplitude):
    """가로로 이어지는(끝과 처음이 맞물리는) 물결 높이: 정수 번 되풀이하는 사인을 겹친다."""
    x = np.arange(width) / width * 2 * np.pi
    return sum(amplitude / (k ** 0.8) * np.sin(x * cycles * k + rng.uniform(0, 6.3)) for k in range(1, 5))


# ── 객실 ───────────────────────────────────────────────

def carriage(rng):
    """객실 배경(RGBA): 창 유리 자리는 비어 있다."""
    yy, xx = grid(H, W)
    image = np.zeros((H, W, 4))
    image[..., 3] = 1
    # 판벽: 24점 판자, 판자마다 조금씩 밝기가 다르고 이음 홈이 짙다. 등불 쪽이 따뜻하게 밝다.
    board = (xx // 24) % 3
    image[..., :3] = np.where((board == 0)[..., None], WOOD[2], np.where((board == 1)[..., None], WOOD[3] * 0.92, WOOD[2] * 1.05))
    mask_paint(image, (xx % 24 == 0).astype(float), WOOD[1])
    warm = np.maximum(np.exp(-np.hypot(xx - 84, (yy - 132) * 0.9) / 120), np.exp(-np.hypot(xx - 684, (yy - 132) * 0.9) / 120))
    mask_paint(image, stepped(warm, 5), color("#b06a30"), 0.35)

    # 갈비뼈처럼 휜 천장 들보와 놋쇠 못
    mask_paint(image, (yy < 18).astype(float), WOOD[1])
    for cx in range(0, W + 96, 96):
        rib = ellipse(yy, xx, cx, 34, 60, 30) & ~ellipse(yy, xx, cx, 38, 52, 26) & (yy < 34)
        mask_paint(image, rib.astype(float), WOOD[4])
        mask_paint(image, (rib & (yy < 14)).astype(float), WOOD[5])
    mask_paint(image, ((yy >= 16) & (yy < 22)).astype(float), WOOD[4])
    mask_paint(image, ((yy >= 16) & (yy < 17)).astype(float), WOOD[6])
    for x in range(12, W, 32):
        mask_paint(image, ellipse(yy, xx, x, 19, 1.6, 1.6).astype(float), BRASS[2])

    # 창 아래 징두리: 놋쇠 테 사이의 비늘 무늬(뱀열차의 몸)
    band = (yy >= WY + WH + 26) & (yy < 300)
    scale_row = (yy - (WY + WH + 26)) // 8
    sx = (xx + (scale_row % 2) * 8) % 16
    scale = ((sx - 8) / 8) ** 2 + (((yy - (WY + WH + 26)) % 8) / 8) ** 2 < 1
    mask_paint(image, band.astype(float), color("#2a2a1c"))
    mask_paint(image, (band & scale).astype(float), color("#3c3c26"))
    mask_paint(image, (band & scale & ((yy - (WY + WH + 26)) % 8 < 2)).astype(float), color("#55553a"))
    for y in (WY + WH + 24, 298):
        mask_paint(image, ((yy >= y) & (yy < y + 2)).astype(float), BRASS[2])

    # 창틀: 두꺼운 나무 테, 안쪽 모서리 빗면, 가운데 창살, 아래 창턱
    frame = (xx >= WX - 12) & (xx < WX + WW + 12) & (yy >= WY - 12) & (yy < WY + WH + 14)
    mask_paint(image, frame.astype(float), WOOD[4])
    mask_paint(image, (frame & ((xx < WX - 9) | (yy < WY - 9))).astype(float), WOOD[5])
    mask_paint(image, (frame & ((xx >= WX + WW + 9) | (yy >= WY + WH + 11))).astype(float), WOOD[3])
    bevel = (xx >= WX - 3) & (xx < WX + WW + 3) & (yy >= WY - 3) & (yy < WY + WH + 3)
    mask_paint(image, bevel.astype(float), WOOD[2])
    sill = (xx >= WX - 22) & (xx < WX + WW + 22) & (yy >= WY + WH + 10) & (yy < WY + WH + 22)
    mask_paint(image, sill.astype(float), WOOD[5])
    mask_paint(image, (sill & (yy < WY + WH + 13)).astype(float), WOOD[6])
    mask_paint(image, (sill & (yy >= WY + WH + 20)).astype(float), WOOD[2])
    glass = (xx >= WX) & (xx < WX + WW) & (yy >= WY) & (yy < WY + WH)
    mullion = glass & (np.abs(xx + 0.5 - (WX + WW / 2)) < 4)
    image[glass & ~mullion, 3] = 0
    mask_paint(image, mullion.astype(float), WOOD[4])
    mask_paint(image, (mullion & (xx < WX + WW / 2 - 2)).astype(float), WOOD[5])
    mask_paint(image, (mullion & (np.abs(yy - (WY + WH / 2)) < 4)).astype(float), BRASS[2])

    # 창 위 놋쇠 패: 기어가는 뱀 문장
    plate = (np.abs(xx + 0.5 - (WX + WW / 2)) < 22) & (yy >= 24) & (yy < 36)
    mask_paint(image, plate.astype(float), BRASS[2])
    mask_paint(image, (plate & (yy < 26)).astype(float), BRASS[3])
    for k in range(26):
        x = WX + WW / 2 - 13 + k
        y = 30 + 2.4 * np.sin(k * 0.5)
        mask_paint(image, ellipse(yy, xx, x, y, 1.4, 1.4).astype(float), BRASS[0])
    mask_paint(image, ellipse(yy, xx, WX + WW / 2 + 14, 29, 2.4, 2).astype(float), BRASS[0])

    # 커튼: 놋쇠 막대에 걸려 창틀 양옆을 덮고, 금빛 끈으로 묶었다가 아래로 퍼진다.
    mask_paint(image, ((yy >= 40) & (yy < 44) & (xx >= WX - 44) & (xx < WX + WW + 44)).astype(float), BRASS[2])
    for side in (-1, 1):
        edge = WX - 12 if side < 0 else WX + WW + 12
        inner = WX + 8 if side < 0 else WX + WW - 8
        outer = edge - side * 40
        t = np.clip((yy - 44) / 150, 0, 1)
        pinch = np.where(yy < 194, (1 - t) * inner + t * (edge + side * 6), edge + side * 6 - side * (yy - 194) * 0.35)
        lo, hi = np.minimum(outer, pinch), np.maximum(outer, pinch)
        curtain = (xx >= lo) & (xx < hi) & (yy >= 44) & (yy < WY + WH + 20)
        folds = (np.sin((xx - outer) / 6.0) * 0.5 + 0.5)
        tone = stepped(np.clip(0.3 + 0.5 * folds - 0.25 * (yy > 194), 0, 1), 4)
        for k, level in enumerate((0.0, 0.25, 0.5, 0.75)):
            mask_paint(image, (curtain & (tone >= level)).astype(float), VELVET[k + 1])
        tie = curtain & (np.abs(yy - 194) < 3)
        mask_paint(image, tie.astype(float), BRASS[3])
        tassel = ellipse(yy, xx, edge + side * 2, 204, 3, 6)
        mask_paint(image, tassel.astype(float), BRASS[2])

    # 벽등: 놋쇠 받침, 유리 등피 안의 불꽃
    for lx in (84, 684):
        mask_paint(image, ((np.abs(xx - lx) < 2) & (yy >= 100) & (yy < 124)).astype(float), BRASS[1])
        mask_paint(image, ((np.abs(xx - lx) < 10) & (yy >= 122) & (yy < 126)).astype(float), BRASS[2])
        chimney = ellipse(yy, xx, lx, 140, 9, 15) & (yy < 152)
        mask_paint(image, chimney.astype(float), color("#ffd890"))
        mask_paint(image, ellipse(yy, xx, lx, 142, 4, 8).astype(float), color("#fff0c0"))
        mask_paint(image, (chimney & ~ellipse(yy, xx, lx, 140, 8, 14)).astype(float), BRASS[3], 0.8)
        mask_paint(image, ((np.abs(xx - lx) < 11) & (yy >= 152) & (yy < 157)).astype(float), BRASS[1])

    # 창턱 위 찻잔과 책
    cup_x, sill_top = WX + 56, WY + WH + 10
    mask_paint(image, ellipse(yy, xx, cup_x, sill_top + 1, 12, 3).astype(float), color("#d8d0c0"))
    cup = (np.abs(xx - cup_x) < 7) & (yy >= sill_top - 12) & (yy < sill_top)
    mask_paint(image, cup.astype(float), color("#e8e0d0"))
    mask_paint(image, (cup & (xx > cup_x + 3)).astype(float), color("#b8b0a0"))
    mask_paint(image, ellipse(yy, xx, cup_x, sill_top - 12, 7, 2).astype(float), color("#6a3a1a"))
    handle = ellipse(yy, xx, cup_x + 9, sill_top - 6, 4, 4) & ~ellipse(yy, xx, cup_x + 9, sill_top - 6, 2, 2)
    mask_paint(image, handle.astype(float), color("#e8e0d0"))
    book = (xx >= WX + WW - 96) & (xx < WX + WW - 52) & (yy >= sill_top - 8) & (yy < sill_top)
    mask_paint(image, book.astype(float), color("#2c4a6a"))
    mask_paint(image, (book & (yy < sill_top - 6)).astype(float), color("#ece2c6"))
    mask_paint(image, (book & (np.abs(xx - (WX + WW - 74)) < 1)).astype(float), BRASS[2])

    # 앞쪽 좌석 등받이: 붉은 벨벳, 단추로 누빈 마름모 무늬, 나무 윗난간과 놋쇠 끝장식
    seat = yy >= 300 + 6 * np.cos((xx - W / 2) / W * np.pi)
    mask_paint(image, seat.astype(float), VELVET[2])
    diamond = (((xx + yy) % 36 < 2) | ((xx - yy) % 36 < 2)) & seat & (yy > 318)
    mask_paint(image, diamond.astype(float), VELVET[1])
    buttons = seat & (yy > 318) & (((xx + yy) % 36 < 3) & ((xx - yy) % 36 < 3))
    mask_paint(image, buttons.astype(float), VELVET[4])
    rail = seat & (yy < 312 + 6 * np.cos((xx - W / 2) / W * np.pi))
    mask_paint(image, rail.astype(float), WOOD[4])
    mask_paint(image, (rail & (yy < 304 + 6 * np.cos((xx - W / 2) / W * np.pi))).astype(float), WOOD[6])
    mask_paint(image, (seat & (yy > 318) & (yy < 322)).astype(float), color("#ff9a5a"), 0.08)
    return image


def carriage_glow(rng):
    """빛 장: 벽등 빛무리, 창턱의 달빛, 유리 위 희미한 비스듬한 반사."""
    yy, xx = grid(H, W)
    glow = np.zeros((H, W, 4))
    for lx in (84, 684):
        halo = stepped(np.exp(-np.hypot(xx - lx, (yy - 140) * 1.1) / 46), 6)
        mask_paint(glow, halo, color("#ffb45a"), 0.55)
        mask_paint(glow, ellipse(yy, xx, lx, 142, 5, 9).astype(float), color("#fff4d0"))
    moon = stepped(np.exp(-np.hypot((xx - (WX + WW - 92)) * 0.5, yy - (WY + WH + 14)) / 30), 4) * (yy > WY + WH + 8) * (yy < WY + WH + 24)
    mask_paint(glow, moon, color("#a8b8e8"), 0.45)
    glass = (xx >= WX) & (xx < WX + WW) & (yy >= WY) & (yy < WY + WH) & (np.abs(xx + 0.5 - (WX + WW / 2)) >= 4)
    streak = glass & ((((xx + yy * 0.8) % 140) < 10) | (((xx + yy * 0.8) % 140) > 128))
    mask_paint(glow, streak.astype(float), WHITE, 0.07)
    return glow


# ── 창 밖 ──────────────────────────────────────────────

def sky(rng):
    """창 크기의 밤하늘(멈춘 겹): 아래로 옅어지는 남빛, 조용한 별, 둥근 달과 달무리, 달빛 받은 가는 구름."""
    yy, xx = grid(WH, WW)
    t = stepped(yy / WH, 10)[..., None]
    image = np.zeros((WH, WW, 4))
    image[..., :3] = color("#070a1c") * (1 - t) + color("#2a3458") * t
    image[..., 3] = 1
    for _ in range(70):
        x, y = rng.integers(0, WW), rng.integers(0, int(WH * 0.6))
        image[y, x, :3] = lighten(color("#8890b8"), rng.random() * 0.8)
    mx, my = WW - 92, 44
    halo = stepped(np.exp(-np.hypot(xx - mx, yy - my) / 34), 5)
    mask_paint(image, halo, color("#b8c4f0"), 0.3)
    moon = ellipse(yy, xx, mx, my, 16, 16)
    mask_paint(image, moon.astype(float), color("#f0ecd0"))
    for cx, cy, r in ((mx - 5, my - 4, 4), (mx + 5, my + 5, 5), (mx + 7, my - 6, 2.5)):
        mask_paint(image, (moon & ellipse(yy, xx, cx, cy, r, r)).astype(float), color("#d8d2b4"))
    for cx, cy, w in ((120, 60, 90), (300, 34, 70), (210, 92, 60)):
        cloud = ellipse(yy, xx, cx, cy, w, 4) | ellipse(yy, xx, cx + w * 0.3, cy - 3, w * 0.5, 4)
        mask_paint(image, cloud.astype(float), color("#3a4470"), 0.8)
        mask_paint(image, (cloud & ~np.roll(cloud, 2, axis=0)).astype(float), color("#8a94c0"), 0.6)
    return image


def far(rng):
    """먼 산 두 겹(달빛 받은 능선)과 그 앞의 어두운 들, 들 위에 희미하게 깔린 달빛(되풀이)."""
    yy, xx = grid(WH, STRIP)
    image = np.zeros((WH, STRIP, 4))
    for base, amp, cycles, tone in ((96, 22, 3, "#232c50"), (118, 16, 5, "#1a2240")):
        ridge = base + periodic(STRIP, rng, cycles, amp)[None, :]
        hills = yy >= ridge
        mask_paint(image, hills.astype(float), color(tone))
        mask_paint(image, (hills & (yy < ridge + 2)).astype(float), lighten(color(tone), 0.25))
    field = yy >= 138 + periodic(STRIP, rng, 4, 2)[None, :]
    mask_paint(image, field.astype(float), color("#18261f"))
    mask_paint(image, (field & (yy < 141)).astype(float), color("#4a5a80"), 0.5)
    return image


def near(rng):
    """가까운 나무(둥근 활엽수와 뾰족한 침엽수)와 덤불, 낮은 울타리(되풀이, 위쪽 끝만 달빛을 받는다)."""
    yy, xx = grid(WH, STRIP)
    image = np.zeros((WH, STRIP, 4))
    dark, rim = color("#0c1610"), color("#2e4a4a")
    shape = np.zeros((WH, STRIP), dtype=bool)
    x = 0
    while x < STRIP:
        kind, r, tall = rng.random(), rng.uniform(12, 20), rng.uniform(40, 60)
        for cx in (x - STRIP, x, x + STRIP):
            if kind < 0.45:
                shape |= ellipse(yy, xx, cx, 150 - r, r, r * 1.1) | ((np.abs(xx - cx) < 2) & (yy >= 140) & (yy < 166))
            elif kind < 0.8:
                shape |= (yy >= 162 - tall) & (yy < 162) & (np.abs(xx - cx) < (yy - (162 - tall)) * 0.28)
            else:
                shape |= ellipse(yy, xx, cx, 160, 18, 8)
        x += int(rng.integers(26, 64))
    fence = (yy >= 156) & (yy < 166) & ((xx % 40 < 3) | (np.abs(yy - 159) < 1))
    shape |= fence | (yy >= 164)
    mask_paint(image, shape.astype(float), dark)
    mask_paint(image, (shape & ~np.roll(shape, 2, axis=0) & (yy < 164)).astype(float), rim, 0.8)
    return image


def close(rng):
    """바로 앞 풀숲과 가끔 지나가는 이정표 돌(되풀이, 가장 빠르게 흐른다)."""
    yy, xx = grid(WH, STRIP)
    image = np.zeros((WH, STRIP, 4))
    dark = color("#060c08")
    grass = yy >= 188 + periodic(STRIP, rng, 9, 6)[None, :]
    for x in range(0, STRIP, 5):
        tall = rng.integers(8, 22)
        lean = rng.uniform(-0.3, 0.3)
        for k in range(tall):
            y = WH - 1 - 14 - k
            px = int(x + lean * k) % STRIP
            grass[max(0, y), px] = True
    for stone_x in (int(STRIP * 0.3), int(STRIP * 0.78)):
        grass |= (np.abs(xx - stone_x) < 7) & (yy >= 158) & (yy < 200)
        grass |= ellipse(yy, xx, stone_x, 158, 7, 4)
    mask_paint(image, grass.astype(float), dark)
    return image


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/train"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(31)
    back, glow = carriage(rng), carriage_glow(rng)
    layers = {"sky": sky(rng), "far": far(rng), "near": near(rng), "close": close(rng)}

    def save(name, image):
        rgba = np.clip(image, 0, 255).copy()
        rgba[..., 3] = np.clip(image[..., 3], 0, 1) * 255
        Image.fromarray(pg.upscale(rgba.astype(np.uint8)), "RGBA").save(out / f"{name}.png")

    save("back", back)
    save("glow", glow)
    for name, image in layers.items():
        save(name, image)
    # 미리보기: 창 뒤에 네 겹을 처음 자리로 깔고 객실·빛을 얹는다.
    preview = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 255))
    for name in layers:
        strip = Image.open(out / f"{name}.png")
        preview.alpha_composite(strip.crop((0, 0, WW * 2, WH * 2)), (WX * 2, WY * 2))
    preview.alpha_composite(Image.open(out / "back.png"))
    preview.alpha_composite(Image.open(out / "glow.png"))
    preview.convert("RGB").save(project / "Docs/ArtUpgrade/PixelGen/cutin_trainWindow_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
