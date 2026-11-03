"""판도라의 정원(hiddenGarden, 10장)을 새로 짠다(사용자 요청 2026-11-03: 10장 맵을 생성기로).

케이트의 공간 비틀기에 휘말려 떨어지는 숨겨진 정원. 판도라가 가장 아끼던 곳이라 성소처럼 꾸몄다(칸 25×16, 지점 자리 같다):
  - 흰 대리석 원형 광장(center): 금빛으로 새긴 일곱 꽃잎 축복 문양. 남쪽 도착 자리(arrive)에서 광장으로, 광장에서 북쪽 분수로 대리석 길.
  - 북쪽 분수: 대리석 둥근 수반에 빛나는 물, 가운데 수정 꽃. 분수 앞(exit)에 요정들이 빛의 문을 연다.
  - 흰 장미 아치 둘, 흰 꽃과 금빛 꽃이 가득한 풀밭, 흰 꽃 덤불, 둘레는 자작나무 숲.

사용: python Tools/PixelGen/hidden_garden.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/hiddenGarden/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/hiddenGarden_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import forest
import pixelgen as pg
from houses import Canvas, outline
from layout import LOGICAL, MapLayout

T = pg.TILE
WIDTH, HEIGHT = 25, 16
CENTER = (12.5, 7.5)
FOUNTAIN = (12.5, 3.5)
MARBLE = pg.hexes("#6e6a76", "#9c98a4", "#c4c0ca", "#e2dee6", "#f6f4f8")
GOLD = pg.hexes("#6a4a12", "#a8782a", "#dcae4a", "#f6dc8a", "#fff6cc")
SPRING = pg.hexes("#3a8aa0", "#5cbcd0", "#9ae6ec", "#e0fcff")
ROSE = pg.hexes("#c8c0c4", "#ece6ea", "#ffffff")
VINE = pg.hexes("#1e3a1e", "#2e5a2a", "#447a36")


def plaza(radius_tiles):
    """대리석 원형 광장(바닥 그림): 동심원 줄눈과 방사 줄눈으로 나눈 판석, 가운데 금빛 일곱 꽃잎 문양, 둘레 금테."""
    r = radius_tiles * T
    size = int(2 * r + 4)
    c = Canvas(size, size)
    cx = cy = size / 2
    d = np.hypot(c.xx + 0.5 - cx, (c.yy + 0.5 - cy) / 0.78)
    a = np.arctan2(c.yy + 0.5 - cy, c.xx + 0.5 - cx)
    disc = d <= r
    ring = np.floor(d / 14)
    tone = 0.62 + ((ring * 3 + np.floor((a + np.pi) / (np.pi / (6 + ring * 3))) * 7) % 5) / 22 - d / r * 0.12
    c.paint(disc, pg.quantize(np.clip(tone, 0, 0.999), MARBLE, dither=0.08))
    joints = disc & ((np.mod(d, 14) < 1) | (np.abs(np.mod((a + np.pi) * (6 + ring * 3) / np.pi, 1) - 0.5) > 0.47))
    c.paint(joints & (d > 6), MARBLE[1])
    c.paint(disc & (d > r - 3), GOLD[2])
    c.paint(disc & (d > r - 1.5), GOLD[1])
    # 일곱 꽃잎 문양(축복): 꽃잎마다 금빛 테, 가운데 빛나는 원.
    for k in range(7):
        ang = k / 7 * np.pi * 2 - np.pi / 2
        px, py = cx + np.cos(ang) * 14, cy + np.sin(ang) * 14 * 0.78
        petal = np.hypot((c.xx + 0.5 - px) / 7, (c.yy + 0.5 - py) / 5) <= 1
        c.paint(petal, GOLD[3])
        c.paint(petal & (np.hypot((c.xx + 0.5 - px) / 5, (c.yy + 0.5 - py) / 3.4) > 1), GOLD[1])
    centre = np.hypot((c.xx + 0.5 - cx) / 7, (c.yy + 0.5 - cy) / 5.5) <= 1
    c.paint(centre, GOLD[4])
    c.paint(centre & (np.hypot((c.xx + 0.5 - cx) / 7, (c.yy + 0.5 - cy) / 5.5) > 0.7), GOLD[2])
    return c.image


def marble_path(cells):
    """대리석 판석 길(바닥 그림, 맵 전체 크기): 반 장씩 어긋나게 깐 판석과 금빛 가장자리."""
    h, w = cells.shape[0] * T, cells.shape[1] * T
    c = Canvas(w, h)
    import terrain
    mask = terrain.cell_field(cells) > 0.55
    row = np.floor(c.yy / 12)
    col = np.floor((c.xx + (row % 2) * 8) / 16)
    tone = 0.6 + ((row * 5 + col * 3) % 4) / 16
    c.paint(mask, pg.quantize(np.clip(tone, 0, 0.999), MARBLE, dither=0.06))
    c.paint(mask & ((np.mod(c.yy, 12) < 1) | (np.mod(c.xx + (row % 2) * 8, 16) < 1)), MARBLE[1])
    edge = mask & pg.neighbours(~mask)
    c.paint(edge, GOLD[2])
    return c.image


def fountain(rng):
    """대리석 둥근 수반, 빛나는 물, 가운데 기둥 위 수정 꽃(세우는 것)."""
    w, h = 112, 104
    c = Canvas(w, h)
    cx, ground = w / 2, h - 4
    basin_y = ground - 14
    outer = c.ellipse(cx, basin_y, 52, 22)
    inner = c.ellipse(cx, basin_y - 3, 44, 16)
    side = c.rect(cx - 52, basin_y, cx + 52, ground) & (np.hypot((c.xx - cx) / 52, 0) <= 1)
    side |= c.ellipse(cx, ground, 52, 8) & (c.yy >= basin_y)
    u = (c.xx - (cx - 52)) / 104
    c.paint(side, pg.quantize(np.clip(0.75 - u * 0.4, 0, 0.999), MARBLE, dither=0.06))
    c.paint(outer, MARBLE[4])
    c.paint(outer & ~c.ellipse(cx, basin_y, 50, 20), MARBLE[2])
    c.paint(inner, SPRING[1])
    ripple = inner & (np.mod(np.hypot(c.xx - cx, (c.yy - basin_y + 3) * 2.6), 9) < 1.4)
    c.paint(ripple, SPRING[2])
    c.paint(inner & (c.yy < basin_y - 14), SPRING[0])
    c.paint(c.rect(cx - 52, basin_y + 6, cx + 52, basin_y + 8) & side, GOLD[2])
    # 가운데 기둥과 수정 꽃.
    pillar = c.rect(cx - 5, basin_y - 40, cx + 5, basin_y - 2)
    c.paint(pillar, MARBLE[3])
    c.paint(pillar & (c.xx < cx - 2), MARBLE[4])
    c.paint(c.rect(cx - 7, basin_y - 42, cx + 7, basin_y - 38), GOLD[2])
    for k in range(5):
        ang = -np.pi / 2 + (k - 2) * 0.55
        tip = (cx + np.cos(ang) * 22, basin_y - 44 + np.sin(ang) * 26)
        petal = c.polygon([(cx - 4, basin_y - 42), tip, (cx + 4, basin_y - 42)])
        c.paint(petal, SPRING[2])
        c.paint(petal & (c.xx < (cx + tip[0]) / 2), SPRING[3])
    c.paint(c.ellipse(cx, basin_y - 46, 4, 4), SPRING[3])
    return outline(c.image), (cx, ground)


def rose_arch(rng):
    """흰 장미 덩굴이 감긴 아치(세우는 것): 가는 기둥 둘과 둥근 아치, 잎과 흰 장미."""
    w, h = 80, 104
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    frame = (c.arch(cx - 34, ground - 92, cx + 34, ground) & ~c.arch(cx - 28, ground - 86, cx + 28, ground))
    c.paint(frame, pg.hexes("#d8d0c0")[0])
    c.paint(frame & (c.xx < cx), pg.hexes("#f0ead8")[0])
    leaves = frame.copy()
    for _ in range(5):
        leaves |= pg.neighbours(leaves)
    leaves &= (pg.noise(h, w, 3, rng) > 0.3) | frame
    c.paint(leaves & ~frame, VINE[1])
    c.paint(leaves & ~frame & (pg.bayer(h, w) > 0.7), VINE[2])
    ys, xs = np.nonzero(leaves)
    for i in rng.choice(len(xs), 22, replace=False):
        x, y = xs[i], ys[i]
        rose = c.ellipse(x, y, 3, 2.6)
        c.paint(rose, ROSE[1])
        c.paint(c.ellipse(x - 0.6, y - 0.6, 1.4, 1.2), ROSE[2])
        c.paint(c.rect(x, y, x + 1, y + 1), ROSE[0])
    return outline(c.image), (cx, ground)


def flower_bed(columns, rows, rng):
    """흰 꽃·금빛 꽃·연분홍 꽃이 빽빽한 꽃밭(바닥 그림): 둥근 테두리 안에 잎 바탕과 꽃송이."""
    w, h = columns * T, rows * T
    c = Canvas(w, h)
    bed = np.hypot((c.xx + 0.5 - w / 2) / (w / 2), (c.yy + 0.5 - h / 2) / (h / 2)) <= 1 - pg.noise(h, w, 6, rng) * 0.25
    c.paint(bed, VINE[1])
    c.paint(bed & (pg.noise(h, w, 3, rng) > 0.6), VINE[2])
    colors = [ROSE[2], ROSE[1], GOLD[3], GOLD[4], pg.hexes("#f4c8d8")[0]]
    for _ in range(int(columns * rows * 22)):
        x, y = rng.uniform(2, w - 2), rng.uniform(2, h - 2)
        if not bed[int(y), int(x)]:
            continue
        color = colors[int(rng.integers(0, len(colors)))]
        c.paint(c.ellipse(x, y, 1.8, 1.5), color)
        c.paint(c.rect(x, y, x + 1, y + 1), GOLD[2])
    return c.image


def build():
    m = MapLayout("hiddenGarden", "판도라의 정원", WIDTH, HEIGHT, music="void", weather="motes",
                  ambient={"color": "#fff4dc", "alpha": 0.15}, seed=271)
    rng = np.random.default_rng(272)
    garden = ((m.cx - 12.5) / 11.8) ** 2 + ((m.cy - 8.0) / 7.6) ** 2 <= 1
    m.kinds[garden] = "flowers"
    m.kinds[(pg.noise(HEIGHT, WIDTH, 2, rng) > 0.55) & garden] = "grass"
    woods = ~garden
    # 길은 광장 앞뒤에서 끊는다(광장 가운데 문양을 덮지 않게).
    path = np.zeros((HEIGHT, WIDTH), dtype=bool)
    path[5, 12] = True
    path[10:15, 12] = True
    path[4, 11:14] = True
    m.kinds[path] = "grass"
    m.kinds[6:10, 12] = "grass"
    # 큰 나무 수관이 정원을 덮지 않게, 정원에 맞닿은 숲은 흰 꽃 덤불로 둘러싼다.
    shades = forest.canopy_over(garden, side=1, rows=4)
    forest.plant(m, woods & ~shades, np.random.default_rng(273), mixes=(forest.BIRCHES, forest.BIRCHES, forest.BROADLEAF))
    for y, x in zip(*np.nonzero(woods & shades)):
        m.stamp("bush", (x, y))
    m.block_where(woods)
    for i, (x, y, cols, rows) in enumerate(((3.5, 5.5, 4, 2), (17.5, 5.5, 4, 2), (4.0, 10.0, 4, 3), (17.0, 10.0, 4, 3),
                                            (8.6, 12.6, 3, 2), (14.4, 12.6, 3, 2), (8.0, 2.4, 3, 2), (15.0, 2.4, 3, 2))):
        m.decal(f"flower_bed_{i}", flower_bed(cols, rows, np.random.default_rng(280 + i)), x * T, y * T)
    m.decal("marble_path", marble_path(path), 0, 0)
    disc = plaza(2.3)
    m.decal("plaza", disc, CENTER[0] * T - disc.shape[1] / 2, CENTER[1] * T - disc.shape[0] / 2)
    m.stand("fountain", fountain(np.random.default_rng(274)), FOUNTAIN[0], FOUNTAIN[1] + 1, block=(3.2, 1.4))
    m.light(FOUNTAIN[0] * LOGICAL, (FOUNTAIN[1] - 0.5) * LOGICAL, 46, "#bff4ff")
    for i, x in enumerate((7.0, 18.0)):
        m.stand(f"rose_arch_{i}", rose_arch(np.random.default_rng(275 + i)), x, 8.2, block=(2.4, 0.3))
    for x, y in ((4, 5), (20, 5), (5, 11), (19, 11), (9, 13), (16, 13), (3, 8), (21, 8)):
        m.stamp("bush", (x, y))
    m.border()
    m.point("exit", (12, 5))
    m.point("center", (12, 7))
    m.point("guard", (10, 9), (12, 9), (14, 9))
    m.point("arrive", (12, 12))
    m.point("rang", (13, 12))
    return m


def main(project):
    m = build()
    start = m.points["arrive"][0]
    for name, cells in m.points.items():
        for cell in cells:
            if not m.reachable(start, [cell]):
                print(f"도착에서 {name} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
