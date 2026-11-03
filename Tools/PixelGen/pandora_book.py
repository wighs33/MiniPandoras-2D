"""판도라 봉인서 창의 그림(사용자 요청 2026-11-03: 웹게임 게임 방식을 가져오되 그림은 생성기로). 웹게임 SealBookView를 따른다.

펼친 책 한 권(가죽 표지와 금박 테두리, 두 쪽, 책등 그림자), 판도라마다 빛나는 구슬 위 문양(활성화된 것과 잿빛 두 벌, 큰 것과 작은 것),
모은 더스트를 두르는 고리, 「활성화」 버튼, 닫기 단추. 글자는 그리지 않는다(창이 글자를 얹는다). 크기는 가상 px의 두 배 점이다.
  Assets/MiniPandoras/Art/PixelGen/UI/SealBook/book.png, button.png, close.png, ring.png, row.png, white.png
  Assets/MiniPandoras/Art/PixelGen/UI/SealBook/icon_<id>.png, icon_<id>_off.png        큰 문양(반지름 16 가상 px)
  Assets/MiniPandoras/Art/PixelGen/UI/SealBook/icon_<id>_s.png, icon_<id>_s_off.png    작은 문양(반지름 7)
  Docs/ArtUpgrade/PixelGen/seal_book_preview.png

사용: python Tools/PixelGen/pandora_book.py <프로젝트 루트>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

# 판도라 문양 빛깔은 웹게임 PANDORA_BOOK(battle.json)에서 읽는다. 문양 이름: star, horns, flame, sand, snake, crystal, wind.
OFF = ("#6b6672", "#b7b2bd", "#26232c")
LEATHER = pg.hexes("#24130f", "#2e1913", "#381f17", "#432620")
GOLD = pg.hexes("#7a5a2a", "#b08a46", "#d6b25e", "#f0d890")
PAGE = pg.hexes("#c8b088", "#d6c197", "#e2d2ad", "#efe2c3")


def glyph_mask(c, glyph, cx, cy, r):
    """구슬 위 흰 문양(웹게임 drawGlyph). 좌표는 반지름 1 기준이다."""
    u, v = (c.xx + 0.5 - cx) / r, (c.yy + 0.5 - cy) / r
    stroke = 0.14

    def poly(points):
        return c.polygon([(cx + x * r, cy + y * r) for x, y in points])

    def curve(points, width=stroke, samples=40):
        mask = np.zeros(c.xx.shape, dtype=bool)
        points = np.array(points)
        for t in np.linspace(0, 1, samples):
            # 점들을 잇는 매끈한 곡선(베지에)
            p = points.copy()
            while len(p) > 1:
                p = p[:-1] * (1 - t) + p[1:] * t
            x, y = p[0]
            mask |= (u - x) ** 2 + (v - y) ** 2 <= (width / 2) ** 2
        return mask

    if glyph == "star":
        # 네 갈래 별: 오목한 모양이라 가는 마름모 둘을 겹친다. 옆에 작은 별 둘.
        s, k = 0.66, 0.2
        star = poly([(0, -s), (k, 0), (0, s), (-k, 0)]) | poly([(-s, 0), (0, -k), (s, 0), (0, k)])
        return star | ((u - 0.42) ** 2 + (v + 0.42) ** 2 <= 0.08 ** 2) | ((u + 0.4) ** 2 + (v - 0.44) ** 2 <= 0.06 ** 2)
    if glyph == "horns":
        mask = (u ** 2 + (v - 0.32) ** 2 <= 0.13 ** 2)
        for side in (-1, 1):
            mask |= curve([(side * 0.12, 0.42), (side * 0.72, 0.1), (side * 0.42, -0.62)], 0.2) | curve([(side * 0.42, -0.62), (side * 0.5, -0.05), (side * 0.1, 0.14)], 0.16)
        return mask
    if glyph == "flame":
        # 아래가 둥글고 위가 뾰족한 불꽃, 가운데에 작은 속불꽃을 비워 둔다.
        outer = (u ** 2 / 0.38 ** 2 + (v - 0.22) ** 2 / 0.4 ** 2 <= 1) | poly([(-0.34, 0.12), (0.34, 0.12), (0.06, -0.7)])
        inner = (u ** 2 / 0.15 ** 2 + (v - 0.34) ** 2 / 0.2 ** 2 <= 1) | poly([(-0.13, 0.3), (0.13, 0.3), (0.02, -0.12)])
        return outer & ~inner
    if glyph == "crystal":
        mask = np.zeros(c.xx.shape, dtype=bool)
        for i in range(3):
            a = i * np.pi / 3
            dx, dy = np.cos(a) * 0.62, np.sin(a) * 0.62
            mask |= curve([(-dx, -dy), (dx, dy)], stroke, 30)
            for end in (-1, 1):
                bx, by = end * dx * 0.62, end * dy * 0.62
                nx, ny = -np.sin(a) * 0.16, np.cos(a) * 0.16
                mask |= curve([(bx + nx, by + ny), (end * dx * 0.84, end * dy * 0.84)], stroke, 12)
                mask |= curve([(bx - nx, by - ny), (end * dx * 0.84, end * dy * 0.84)], stroke, 12)
        return mask
    if glyph == "wind":
        a = np.linspace(np.pi * 0.9, np.pi * 2.25, 20)
        mask = curve([(np.cos(t) * 0.34 - 0.05, np.sin(t) * 0.34 - 0.08) for t in a[::4]], stroke, 60)
        return mask | curve([(-0.62, 0.18), (0.3, 0.18)], stroke) | curve([(-0.5, 0.42), (0.2, 0.42), (0.46, 0.18)], stroke)
    if glyph == "snake":
        return curve([(-0.5, 0.5), (-0.9, 0.15), (-0.1, -0.05), (-0.05, 0.12)], 0.17) | curve([(-0.05, 0.12), (0, 0.3), (0.55, 0.15), (0.3, -0.25)], 0.17) \
            | curve([(0.3, -0.25), (0.15, -0.5), (0.42, -0.58)], 0.17) | ((u - 0.44) ** 2 + (v + 0.58) ** 2 <= 0.14 ** 2)
    # 모래: 물결 세 줄
    mask = np.zeros(c.xx.shape, dtype=bool)
    for row in range(3):
        y = -0.36 + row * 0.34
        mask |= curve([(-0.6, y), (-0.3, y - 0.18), (-0.1, y + 0.18), (0.15, y)], stroke) | curve([(0.15, y), (0.35, y - 0.14), (0.5, y + 0.1), (0.6, y)], stroke)
    return mask


def icon(entry, radius, active):
    """빛나는 구슬과 문양. 활성화된 판도라는 둘레에 그 빛깔의 빛무리가 돈다."""
    color, light, dark = (entry["color"], entry["light"], entry["dark"]) if active else OFF
    rgb = pg.hexes(color, light, dark)
    size = int(radius * 3.6) | 1
    c = Canvas(size, size)
    cx = cy = size / 2
    d = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
    if active:
        halo = np.clip(1 - (d - radius * 0.6) / (radius * 1.0), 0, 1) * (d > radius)
        step = np.floor(halo * 3 + (pg.bayer(size, size) - 0.5) * 0.8) / 3
        c.image[..., :3] = rgb[0]
        c.image[..., 3] = np.clip(step * 110, 0, 255).astype(np.uint8)
    # 구슬: 왼쪽 위의 밝은 점에서 가장자리의 어두운 빛깔로
    orb = d <= radius
    t = np.clip(np.hypot(c.xx + 0.5 - (cx - radius * 0.35), c.yy + 0.5 - (cy - radius * 0.4)) / (radius * 1.35), 0, 1)
    palette = np.array([rgb[1], (rgb[1] * 0.5 + rgb[0] * 0.5).astype(np.uint8), rgb[0], (rgb[0] * 0.6 + rgb[2] * 0.4).astype(np.uint8), rgb[2]])
    shade = pg.quantize(t, palette, 0.12)
    c.paint(orb, shade)
    c.paint(orb & ~pg.neighbours(orb) & (d > radius - 1.2), rgb[2])
    mark = glyph_mask(c, entry["glyph"], cx, cy, radius) & orb
    c.paint(mark, pg.hexes("#ffffff" if active else "#d4cfd8")[0])
    return c.image


def book():
    """펼친 봉인서(가상 352×190): 둥근 가죽 표지, 금박 테두리와 네 귀 마름모, 두 쪽, 책등 그림자."""
    w, h = 704, 380
    c = Canvas(w, h)
    corner = 12
    cover = c.rect(corner, 0, w - corner, h) | c.rect(0, corner, w, h - corner)
    for x, y in ((corner, corner), (w - corner, corner), (corner, h - corner), (w - corner, h - corner)):
        cover |= c.ellipse(x, y, corner, corner)
    grain = pg.fractal_noise(h, w, np.random.default_rng(3), ((30, 0.6), (6, 0.4)))
    light = np.clip(0.9 - c.yy / h * 0.7 + (grain - 0.5) * 0.25, 0, 0.999)
    c.paint(cover, pg.quantize(light, LEATHER, 0.15))
    border = c.rect(6, 6, w - 6, h - 6) & ~c.rect(8, 8, w - 8, h - 8)
    c.paint(border, GOLD[1])
    for x, y in ((12, 12), (w - 12, 12), (12, h - 12), (w - 12, h - 12)):
        c.paint(np.abs(c.xx + 0.5 - x) + np.abs(c.yy + 0.5 - y) <= 6, GOLD[2])
    for left in (True, False):
        x0 = 20 if left else w // 2 + 4
        x1 = w // 2 - 4 if left else w - 20
        page = c.rect(x0, 16, x1, h - 16)
        across = (c.xx - x0) / (x1 - x0)
        t = across if left else 1 - across
        c.paint(page, pg.quantize(np.clip(1 - t * 0.75, 0, 0.999), PAGE, 0.18))
        # 쪽 가장자리의 종이 켜
        for k in range(1, 4):
            edge = c.rect(x0 - k * 2 if left else x1, 16 + k, x0 - k * 2 + 2 if left else x1 + 2, h - 16 - k) if left else c.rect(x1 + (k - 1) * 2, 16 + k, x1 + k * 2, h - 16 - k)
            c.paint(edge, PAGE[0])
    spine = np.abs(c.xx + 0.5 - w / 2) < 12
    shadow = np.clip(1 - np.abs(c.xx + 0.5 - w / 2) / 12, 0, 1)
    dark = (shadow * 3 + (pg.bayer(h, w) - 0.5) * 0.9 > 1) & spine & c.rect(0, 16, w, h - 16)
    c.paint(dark, pg.hexes("#a88a5e")[0])
    c.paint(dark & (np.abs(c.xx + 0.5 - w / 2) < 3), pg.hexes("#7a603a")[0])
    return c.image


def button():
    """「활성화」 버튼(가상 60×13): 진한 가죽빛, 금테."""
    w, h = 120, 26
    c = Canvas(w, h)
    body = c.rect(3, 0, w - 3, h) | c.rect(0, 3, w, h - 3) | c.ellipse(3, 3, 3, 3) | c.ellipse(w - 3, 3, 3, 3) | c.ellipse(3, h - 3, 3, 3) | c.ellipse(w - 3, h - 3, 3, 3)
    c.paint(body, pg.hexes("#3a1f0e")[0])
    c.paint(body & (c.yy < h / 2), pg.hexes("#5a3418")[0])
    c.paint(body & ~pg.neighbours(~body) == False, GOLD[2])
    c.paint(body & pg.neighbours(~body), GOLD[2])
    return c.image


def close():
    """닫기 단추(가상 22×22): 금테 두른 가죽 동그라미(× 글자는 창이 얹는다)."""
    c = Canvas(44, 44)
    disc = c.ellipse(22, 22, 16, 16)
    c.paint(disc, LEATHER[0])
    c.paint(disc & pg.neighbours(~disc), GOLD[1])
    return c.image


def ring():
    """더스트를 두르는 고리(반지름 8.5 가상 px, 흰색. 창이 빛깔을 입히고 모은 만큼만 채운다)."""
    c = Canvas(40, 40)
    d = np.hypot(c.xx + 0.5 - 20, c.yy + 0.5 - 20)
    c.paint((d >= 15.5) & (d <= 18.5), pg.hexes("#ffffff")[0])
    return c.image


def row():
    """고른 줄의 옅은 바탕(가상 158×18)."""
    c = Canvas(316, 36)
    c.image[..., :3] = pg.hexes("#785020")[0]
    c.image[..., 3] = 40
    return c.image


def white():
    """막대·바탕을 칠할 흰 칸 하나(창이 빛깔을 입히고 모은 만큼만 채운다)."""
    c = Canvas(4, 4)
    c.paint(c.rect(0, 0, 4, 4), pg.hexes("#ffffff")[0])
    return c.image


def save(image, path):
    Image.fromarray(pg.upscale(image), "RGBA").save(path)


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/UI/SealBook"
    out.mkdir(parents=True, exist_ok=True)
    battle = json.loads((project / "Assets/MiniPandoras/Art/WebGame/battle.json").read_text(encoding="utf-8"))
    pandoras = [p for p in battle["pandoras"] if p.get("glyph")]
    for name, image in (("book", book()), ("button", button()), ("close", close()), ("ring", ring()), ("row", row()), ("white", white())):
        save(image, out / f"{name}.png")
    for entry in pandoras:
        for suffix, radius in (("", 32), ("_s", 14)):
            save(icon(entry, radius, True), out / f"icon_{entry['id']}{suffix}.png")
            save(icon(entry, radius, False), out / f"icon_{entry['id']}{suffix}_off.png")
    # 미리보기: 책 위에 문양을 늘어놓는다.
    preview = Image.open(out / "book.png").convert("RGBA")
    for k, entry in enumerate(pandoras):
        for on in (True, False):
            mark = Image.open(out / f"icon_{entry['id']}{'' if on else '_off'}.png")
            preview.alpha_composite(mark, (60 + k * 90 * 2 // 2 + (0 if on else 0), 120 if on else 420))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/seal_book_preview.png")
    print(out, [p["id"] for p in pandoras])


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
