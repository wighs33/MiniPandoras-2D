"""판도라 정착 창의 그림(웹게임 게임 방식을 가져오되 그림은 생성기로). 웹게임 LoadoutView를 따른다.

남색 창(금테 두 줄, 둥근 귀), 왼쪽 마법진(세 겹 원, 동서남북 장식, 세 자리를 잇는 삼각형, 초승달과 삼각 문양, 은은한 푸른 빛),
마법진의 두 바깥 원 사이를 도는 룬 띠(눈금과 점을 번갈아, 창이 천천히 돌린다), 가운데 네 갈래 별, 판도라를 놓는 자리 원,
자리 위 빛(고른 자리·끌고 있는 자리), 오른쪽 목록의 줄 바탕(보통·고른 줄), 정착한 자리 번호 표식. 글자는 창이 얹는다.
크기는 가상 px의 두 배 점이다(창 그림은 점 하나 = 2px로 키워 저장한다). 판도라 문양은 봉인서 그림(pandora_book.py)을 함께 쓴다.

  Assets/MiniPandoras/Art/PixelGen/UI/Loadout/panel.png, circle.png, runes.png, star.png, slot.png, slot_glow.png, row.png, row_focus.png, badge.png
  Docs/ArtUpgrade/PixelGen/loadout_preview.png

사용: python Tools/PixelGen/pandora_loadout.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

D = 2                              # 가상 px 하나 = 2점
NAVY = pg.hexes("#0a0d1c", "#10152c", "#171d3a")
GOLD = pg.hexes("#6a5226", "#b8954e", "#c9a55a", "#e2c26e", "#f4e3a8")
BLUE = np.array([120, 140, 255], dtype=np.uint8)
CIRCLE_R = 72                      # 마법진 반지름(가상 px)
SLOTS = [(0, -44), (-42, 26), (42, 26)]   # 마법진 가운데에서 잰 세 자리(가상 px)


def rounded(c, x0, y0, x1, y1, r):
    body = c.rect(x0 + r, y0, x1 - r, y1) | c.rect(x0, y0 + r, x1, y1 - r)
    for x, y in ((x0 + r, y0 + r), (x1 - r, y0 + r), (x0 + r, y1 - r), (x1 - r, y1 - r)):
        body |= c.ellipse(x, y, r, r)
    return body


def panel():
    """창 바탕(가상 364×200): 위에서 아래로 짙어지는 남색, 금테 두 줄(안쪽은 옅게)."""
    w, h = 364 * D, 200 * D
    c = Canvas(w, h)
    body = rounded(c, 0, 0, w, h, 5 * D)
    c.paint(body, pg.quantize(np.clip(1 - c.yy / h, 0, 0.999), NAVY, dither=0.15))
    outer = body & pg.neighbours(~body)
    for _ in range(1):
        outer |= pg.neighbours(outer) & body
    c.paint(outer, GOLD[1])
    inner_body = rounded(c, 3 * D, 3 * D, w - 3 * D, h - 3 * D, 3 * D)
    inner = inner_body & pg.neighbours(~inner_body)
    c.paint(inner, GOLD[0])
    return c.image


def ring(c, cx, cy, r, color, width=1.0):
    d = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
    c.paint(np.abs(d - r) <= width * D / 2, color)


def circle():
    """마법진의 멈춰 있는 부분(가상 160×160): 푸른 빛, 세 겹 원, 동서남북 장식, 세 자리 삼각형, 초승달·삼각 문양."""
    size = 160 * D
    c = Canvas(size, size)
    cx = cy = size / 2
    r = CIRCLE_R * D
    d = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
    glow = np.clip(1 - (d - r * 0.2) / (r * 0.9), 0, 1)
    step = np.floor(glow * 4 + (pg.bayer(size, size) - 0.5) * 0.9) / 4
    c.image[..., :3] = BLUE
    c.image[..., 3] = np.clip(step * 42, 0, 255).astype(np.uint8)
    for rr in (r, r - 9 * D, r - 30 * D):
        ring(c, cx, cy, rr, GOLD[2])
    for k in range(4):
        a = k * np.pi / 2
        px, py = cx + np.cos(a) * r, cy + np.sin(a) * r
        pts = [(px + np.cos(a) * 6 * D, py + np.sin(a) * 6 * D), (px + np.cos(a + np.pi / 2) * 3 * D, py + np.sin(a + np.pi / 2) * 3 * D),
               (px - np.cos(a) * 4 * D, py - np.sin(a) * 4 * D), (px - np.cos(a + np.pi / 2) * 3 * D, py - np.sin(a + np.pi / 2) * 3 * D)]
        c.paint(c.polygon(pts), GOLD[3])
    corners = [(cx + x * D, cy + y * D) for x, y in SLOTS]
    for (ax, ay), (bx, by) in zip(corners, corners[1:] + corners[:1]):
        c.line(ax, ay, bx, by, GOLD[1], 1)
    # 초승달(왼쪽)과 작은 삼각형(오른쪽).
    mx, my = cx - r * 0.55, cy - r * 0.2
    moon = (np.abs(np.hypot(c.xx + 0.5 - mx, c.yy + 0.5 - my) - 4 * D) <= D / 2) & (np.arctan2(c.yy + 0.5 - my, c.xx + 0.5 - mx) % (2 * np.pi) > np.pi * 0.3)
    moon &= (np.arctan2(c.yy + 0.5 - my, c.xx + 0.5 - mx) % (2 * np.pi) < np.pi * 1.7)
    c.paint(moon, GOLD[1])
    tx, ty = cx + r * 0.55, cy - r * 0.27
    for (ax, ay), (bx, by) in (((tx, ty), (tx + 4 * D, ty + 6 * D)), ((tx + 4 * D, ty + 6 * D), (tx - 4 * D, ty + 6 * D)), ((tx - 4 * D, ty + 6 * D), (tx, ty))):
        c.line(ax, ay, bx, by, GOLD[1], 1)
    return c.image


def runes():
    """두 바깥 원 사이를 도는 룬 띠(가상 160×160): 48칸에 눈금과 점을 번갈아 새긴다."""
    size = 160 * D
    c = Canvas(size, size)
    cx = cy = size / 2
    r = CIRCLE_R * D
    for i in range(48):
        a = i * np.pi * 2 / 48
        cos, sin = np.cos(a), np.sin(a)
        if i % 2:
            c.paint(c.ellipse(cx + cos * (r - 4.5 * D), cy + sin * (r - 4.5 * D), 0.9 * D, 0.9 * D), GOLD[3])
        else:
            c.line(cx + cos * (r - 7 * D), cy + sin * (r - 7 * D), cx + cos * (r - 2 * D), cy + sin * (r - 2 * D), GOLD[2], 1)
    return c.image


def star():
    """가운데 네 갈래 별(가상 14×14)."""
    size = 14 * D
    c = Canvas(size, size)
    cx = cy = size / 2
    s, k = 6 * D, 1.4 * D
    pts = [(cx, cy - s), (cx + k, cy - k), (cx + s, cy), (cx + k, cy + k), (cx, cy + s), (cx - k, cy + k), (cx - s, cy), (cx - k, cy - k)]
    for i in range(0, 8, 2):
        c.paint(c.polygon([pts[i], pts[(i + 1) % 8], (cx, cy), pts[(i - 1) % 8]]), GOLD[4])
    return c.image


def slot():
    """판도라를 놓는 자리(가상 44×44): 짙은 원판에 금테 두 줄."""
    size = 44 * D
    c = Canvas(size, size)
    cx = cy = size / 2
    d = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
    c.paint(d <= 17 * D, NAVY[0])
    c.paint(np.abs(d - 17 * D) <= 0.6 * D, GOLD[2])
    c.paint(np.abs(d - 19.5 * D) <= 0.3 * D + 0.3, GOLD[2])
    return c.image


def slot_glow():
    """고른 자리·끌고 있는 판도라가 닿은 자리 위의 금빛(가상 44×44, 창이 깜빡이게 짙기를 바꾼다)."""
    size = 44 * D
    c = Canvas(size, size)
    cx = cy = size / 2
    d = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
    c.image[..., :3] = pg.hexes("#ffdc8c")[0]
    c.image[..., 3] = np.where(d <= 22 * D, 150, 0).astype(np.uint8)
    return c.image


def row(focused):
    """목록 한 줄의 바탕(가상 154×20). 고른 줄은 금빛이 돌고 테를 두른다."""
    w, h = 154 * D, 20 * D
    c = Canvas(w, h)
    c.image[..., :3] = pg.hexes("#e2c26e")[0] if focused else pg.hexes("#ffffff")[0]
    # 웹게임 투명도(고른 줄 0.16, 보통 0.04)는 sRGB에서 섞은 값이다. Unity는 선형 공간에서 섞어 옅은 흰빛이 훨씬 밝아지므로 그만큼 낮춘다.
    c.image[..., 3] = 16 if focused else 3
    if focused:
        edge = c.rect(0, 0, w, h) & ~c.rect(1, 1, w - 1, h - 1)
        c.paint(edge, GOLD[3])
    return c.image


def badge():
    """목록 줄 오른쪽 끝, 정착한 자리 번호를 얹는 금빛 동그라미(가상 11×11)."""
    size = 11 * D + 1
    c = Canvas(size, size)
    c.paint(c.ellipse(size / 2, size / 2, 5.5 * D, 5.5 * D), GOLD[2])
    return c.image


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/UI/Loadout"
    out.mkdir(parents=True, exist_ok=True)
    parts = {"panel": panel(), "circle": circle(), "runes": runes(), "star": star(), "slot": slot(), "slot_glow": slot_glow(),
             "row": row(False), "row_focus": row(True), "badge": badge()}
    for name, image in parts.items():
        pg.save_png(image, out / f"{name}.png")
    # 미리보기: 창에 마법진과 자리 셋, 줄 넷을 얹는다(창 가상 좌표 그대로, 점 하나 = 2px).
    scale = D * pg.SCALE
    preview = Image.open(out / "panel.png").convert("RGBA")
    circle_image = Image.open(out / "circle.png")
    centre = (102 * scale, 114 * scale)
    preview.alpha_composite(circle_image, (centre[0] - circle_image.width // 2, centre[1] - circle_image.height // 2))
    runes_image = Image.open(out / "runes.png")
    preview.alpha_composite(runes_image, (centre[0] - runes_image.width // 2, centre[1] - runes_image.height // 2))
    star_image = Image.open(out / "star.png")
    preview.alpha_composite(star_image, (centre[0] - star_image.width // 2, centre[1] - star_image.height // 2))
    slot_image = Image.open(out / "slot.png")
    for x, y in SLOTS:
        preview.alpha_composite(slot_image, (centre[0] + x * scale - slot_image.width // 2, centre[1] + y * scale - slot_image.height // 2))
    for i in range(4):
        preview.alpha_composite(Image.open(out / ("row_focus.png" if i == 0 else "row.png")), (202 * scale, (48 + i * 22) * scale))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/loadout_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
