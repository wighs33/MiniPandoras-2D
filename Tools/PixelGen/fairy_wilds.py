"""페어리가든 동쪽의 들판과 숲을 짠다(페어리가든도 분위기에 맞게 맵을 넓힌다).

요정 마을 동쪽 출구에서 이어진다. 낮의 꽃밭에서 저녁 냇가, 밤의 버섯 숲으로 깊어지고 끝에 요정의 샘이 있다.

    페어리가든 → 꽃잎 들판 ┬ 반딧불 냇가
                         └ 달빛 버섯 숲 → 요정의 샘

  petalMeadow    꽃잎 들판: 사람 키보다 큰 꽃이 핀 들판. 꽃가루가 날리고 자작나무 숲이 둘러싼다.
  fireflyBrook   반딧불 냇가: 해 질 녘의 굽은 냇물과 작은 못. 판자 다리를 건너면 못가에 상자가 있다. 반딧불이 떠다닌다.
  glowWood       달빛 버섯 숲: 밤처럼 어두운 침엽수림을 빛나는 큰 버섯이 밝힌다.
  fairySpring    요정의 샘: 숲 끝 빈터의 맑은 샘을 이끼 낀 돌 고리가 둘러싼다.

사용: python Tools/PixelGen/fairy_wilds.py <프로젝트 루트> [맵 id ...]
  Assets/MiniPandoras/Art/PixelGen/Maps/<맵 id>/…    맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/<맵 id>_preview.png       미리보기
"""
import math
import sys
from pathlib import Path

import numpy as np

import fairy_props as fp
import forest
import hidden_garden
import pixelgen as pg
from houses import Canvas, outline
from layout import LOGICAL, MapLayout

T = pg.TILE
STEM = pg.hexes("#25451f", "#3a6a2c", "#5a9440", "#86bc58")
PINK = pg.hexes("#a03a6a", "#d85a8e", "#f48ab0", "#fcc4d8")
SUN = pg.hexes("#b8741c", "#e8a828", "#fbd24a", "#fff0a0")
LILAC = pg.hexes("#5a3a9a", "#8058c8", "#ac86e8", "#d8c4f8")
SKY_BLUE = pg.hexes("#2a6aa8", "#4a98d0", "#84c4ec", "#c8e8f8")
CORE = pg.hexes("#7a4a14", "#c88a24", "#f4c64a")
CREAM = pg.hexes("#8a7a5a", "#b8a880", "#ddcfa8", "#f4ecd0")
TEAL_CAP = pg.hexes("#14505a", "#1c7c84", "#2eb4b0", "#7ff0dc")
VIOLET_CAP = pg.hexes("#3a2470", "#5a38a8", "#8a5cdc", "#c8a4f8")
MOSSY_STONE = pg.hexes("#3a3e46", "#585e68", "#7c838e", "#a4abb4")
REED = pg.hexes("#3e5a26", "#5e8234", "#88aa48")
CATTAIL = pg.hexes("#3e2414", "#6a3e20")
LILY = pg.hexes("#1e5a3a", "#2e8450", "#54b070")
GLOW_LIGHT = "#7ff0dc"
VIOLET_LIGHT = "#c8a4f8"
GARDEN = (forest.BIRCHES, forest.BIRCHES, forest.BROADLEAF)
PINES = (forest.CONIFERS, forest.CONIFERS, forest.CONIFERS)
FLOWER_BLOCK = (0.8, 0.3)
DUSK = {"color": "#1c1648", "alpha": 0.34}
NIGHT = {"color": "#0c1030", "alpha": 0.5}
NIGHT_KINDS = ("grass", "flowers", "tallgrass")


# ── 소품 ──────────────────────────────────────

def giant_flower(seed, petals):
    """사람 키보다 큰 꽃: 살짝 굽은 줄기, 넓은 잎, 여덟 장 꽃잎."""
    rng = np.random.default_rng(seed)
    w, h = 60, 94
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    sway = rng.uniform(-4, 4)
    top = 34
    for y in range(ground, top, -1):
        t = (ground - y) / (ground - top)
        x = cx + sway * t * t
        c.paint(c.rect(x - 2, y, x + 2, y + 1), STEM[1])
        c.paint(c.rect(x - 2, y, x - 1, y + 1), STEM[2])
    for side in (-1, 1):
        leaf_y = ground - 20 - (side > 0) * 9
        c.paint(c.ellipse(cx + side * 11, leaf_y, 12, 4.4), STEM[1])
        c.paint(c.ellipse(cx + side * 10, leaf_y - 1, 8, 2.2), STEM[3])
    head_x, head_y = cx + sway, 27
    turn = rng.uniform(0, math.pi / 4)
    for k in range(8):
        angle = turn + k * math.pi / 4
        px, py = head_x + math.cos(angle) * 15, head_y + math.sin(angle) * 12
        c.paint(c.ellipse(px, py, 10, 8), petals[1 if k % 2 else 2])
        c.paint(c.ellipse(px - 2, py - 2, 5, 3.4), petals[3])
    c.paint(c.ellipse(head_x, head_y, 8, 7), CORE[1])
    c.paint(c.ellipse(head_x - 2, head_y - 2, 4, 3), CORE[2])
    return outline(c.image), (cx, ground)


def giant_mushroom(seed, cap, width=78):
    """빛나는 큰 버섯: 굵은 대와 둥근 갓, 갓의 밝은 점, 갓 아래 주름."""
    rng = np.random.default_rng(seed)
    w, h = width + 8, int(width * 1.3)
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    cap_y = h * 0.42
    stem = c.polygon([(cx - width * 0.1, cap_y), (cx + width * 0.1, cap_y), (cx + width * 0.18, ground), (cx - width * 0.18, ground)])
    c.paint(stem, CREAM[2])
    c.paint(stem & (c.xx < cx - width * 0.03), CREAM[3])
    c.paint(stem & (c.xx > cx + width * 0.09), CREAM[1])
    dome = c.ellipse(cx, cap_y, width * 0.5, width * 0.38) & (c.yy <= cap_y + 2)
    c.paint(dome, cap[1])
    c.paint(dome & (c.yy < cap_y - width * 0.12), cap[2])
    gills = c.ellipse(cx, cap_y + 2, width * 0.46, width * 0.08)
    c.paint(gills, cap[0])
    c.paint(gills & (np.mod(c.xx, 5) < 1), CREAM[2])
    for _ in range(7):
        angle = rng.uniform(math.pi * 1.1, math.pi * 1.9)
        reach = rng.uniform(0.25, 0.8)
        sx, sy = cx + math.cos(angle) * width * 0.48 * reach, cap_y + math.sin(angle) * width * 0.34 * reach
        size = rng.uniform(3, 6)
        c.paint(c.ellipse(sx, sy, size, size * 0.75) & dome, cap[3])
    return outline(c.image), (cx, ground)


def reeds(seed):
    """물가 갈대 다발."""
    rng = np.random.default_rng(seed)
    w, h = 34, 48
    c = Canvas(w, h)
    for _ in range(7):
        x = rng.uniform(5, w - 5)
        tall = rng.uniform(22, 42)
        lean = rng.uniform(-4, 4)
        for k in range(int(tall)):
            t = k / tall
            c.paint(c.rect(x + lean * t * t, h - 2 - k, x + lean * t * t + 2, h - 1 - k), REED[1 + int(k % 7 < 2)])
        if rng.random() < 0.6:
            c.paint(c.ellipse(x + lean, h - 2 - tall, 2, 5), CATTAIL[1])
            c.paint(c.ellipse(x + lean - 1, h - 3 - tall, 1, 3), CATTAIL[0])
    return outline(c.image), (w / 2, h - 2)


def standing_stone(seed):
    """이끼 낀 선돌: 앞면에 빛나는 요정 무늬가 새겨졌다."""
    rng = np.random.default_rng(seed)
    w, h = 36, 66
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    lean = rng.uniform(-3, 3)
    stone = c.polygon([(cx - 13, ground), (cx - 10 + lean, 8), (cx + 4 + lean, 3), (cx + 12 + lean, 12), (cx + 14, ground)])
    c.paint(stone, MOSSY_STONE[1])
    c.paint(stone & (c.xx < cx - 2), MOSSY_STONE[2])
    c.paint(stone & (c.xx > cx + 8), MOSSY_STONE[0])
    moss = stone & (pg.fractal_noise(h, w, rng, ((9, 0.7), (3, 0.3))) > 0.58)
    c.paint(moss, STEM[2])
    for y in (24, 34, 44):
        c.paint(c.ellipse(cx, y, 3.4, 2.4) & stone, TEAL_CAP[3])
        c.paint(c.ellipse(cx, y, 1.6, 1.1) & stone, TEAL_CAP[2])
    return outline(c.image), (cx, ground)


def lily(seed):
    """못 위 수련잎 한 장(바닥에 까는 그림)."""
    rng = np.random.default_rng(seed)
    w, h = 26, 16
    c = Canvas(w, h)
    pad = c.ellipse(13, 8, 11, 6.4)
    c.paint(pad, LILY[1])
    c.paint(c.ellipse(11, 7, 7, 3.6), LILY[2])
    if rng.random() < 0.5:
        c.paint(c.ellipse(10, 7, 3, 2.4), PINK[3])
        c.paint(c.ellipse(10, 7, 1.4, 1.1), CORE[2])
    image = c.image
    image[c.polygon([(13, 8), (24, 4), (24, 12)]), 3] = 0      # 수련잎의 갈라진 틈
    return image


# ── 맵 ─────────────────────────────────────────

def plant_edges(m, walk, seed, mixes):
    """맵 가장자리를 숲으로 두른다. 길 곁에는 어린 나무만 심어 수관이 길을 덮지 않게 한다."""
    edge = (m.cy < 3) | (m.cy > m.height - 3.5) | (m.cx < 3) | (m.cx > m.width - 3)
    woods = edge & ~walk & np.isin(m.kinds, NIGHT_KINDS)
    forest.plant(m, woods, np.random.default_rng(seed), mixes, low=woods & forest.canopy_over(m.kinds == "path"))
    m.block_where(woods)
    m.border()


def petal_meadow():
    width, height = 46, 30
    m = MapLayout("petalMeadow", "꽃잎 들판", width, height, music="field", seed=421)
    rng = np.random.default_rng(422)
    west, east, north = [(-1, 12.5), (10, 13), (20, 15), (30, 14)], [(30, 14), (38, 16), (width + 1, 17)], [(30, 14), (30, 7), (30, -1)]
    m.kinds[pg.noise(height, width, 5, rng) > 0.5] = "flowers"
    m.kinds[pg.noise(height, width, 4, rng) > 0.72] = "tallgrass"
    for road in (west, east, north):
        m.line("path", road, 2.0)
    m.stamp("rock", (8, 7), (37, 24), (19, 25))
    walk = m.near_line(west, 2.4) | m.near_line(east, 2.4) | m.near_line(north, 2.4)
    plant_edges(m, walk, 423, GARDEN)
    for k, (x, y, columns, rows) in enumerate(((6, 8, 5, 3), (15, 4, 6, 2), (33, 5, 5, 3), (4, 17, 6, 3), (14, 18, 6, 3), (25, 19, 7, 3), (36, 19, 5, 3))):
        m.decal(f"flower_bed_{k}", hidden_garden.flower_bed(columns, rows, np.random.default_rng(436 + k)), x * T, y * T)
    palette = (PINK, SUN, LILAC, SKY_BLUE)
    spots = ((7, 11), (13, 9), (17, 6), (24, 8), (35, 8), (40, 11), (6, 20), (12, 22), (16, 19), (23, 21), (28, 24), (34, 21), (40, 23))
    for k, (x, ground) in enumerate(spots):
        m.stand(f"flower_{k % 8}", giant_flower(424 + k % 8, palette[k % 8 % len(palette)]), x, ground, block=FLOWER_BLOCK)
    for k, (x, ground) in enumerate(((21.5, 12.6), (37.6, 13.6))):
        m.stand(f"meadow_lantern_{k}", fp.fairy_lantern(np.random.default_rng(433 + k)), x, ground)
        m.light((x + 0.3) * LOGICAL, ground * LOGICAL - 26, 30, GLOW_LIGHT)
    m.point("west", (0, 12), (0, 13))
    m.point("westArrive", (2, 13))
    m.point("east", (width - 1, 16), (width - 1, 17))
    m.point("eastArrive", (width - 3, 17))
    m.point("north", (29, 0), (30, 0))
    m.point("northArrive", (30, 2))
    m.warp("west", "fairyGarden", "roadArrive", "left")
    m.warp("east", "fireflyBrook", "westArrive", "right")
    m.warp("north", "glowWood", "southArrive", "up")
    return m


def firefly_brook():
    width, height = 44, 28
    m = MapLayout("fireflyBrook", "반딧불 냇가", width, height, music="field", weather="motes", ambient=DUSK, seed=441)
    rng = np.random.default_rng(442)
    m.kinds[pg.noise(height, width, 4, rng) > 0.66] = "tallgrass"
    m.kinds[pg.noise(height, width, 3, rng) > 0.76] = "flowers"
    m.line("water", [(13, -1), (16, 7), (23, 13), (22, 20), (28, 29)], 4.6)
    m.disc("water", 36, 9, 4.8)
    m.line("water", [(36, 9), (29, 11), (24, 13)], 2.2)
    road = [(-1, 17), (10, 16.6), (19, 15), (30, 15), (37, 18), (39, 19)]
    m.line("path", road, 2.0)
    m.rect("bridge", 19, 14, 28, 17)
    m.stamp("rock", (6, 9), (31, 22), (40, 15), (12, 23))
    walk = m.near_line(road, 2.4)
    plant_edges(m, walk, 443, GARDEN)
    for k, (x, ground) in enumerate(((13, 9), (18.6, 5), (19.4, 11), (27, 18.6), (19.6, 21), (31, 7), (41.2, 7), (31, 13), (26, 23))):
        m.stand(f"reeds_{k % 3}", reeds(444 + k % 3), x, ground, block=(0.8, 0.3))
    for k, (x, y) in enumerate(((33.2, 7.6), (36.8, 10.4), (38.6, 7.4), (34.4, 11), (15.6, 3.2), (24.6, 19))):
        m.decal(f"lily_{k}", lily(450 + k), x * T, y * T)
    for k, (x, ground) in enumerate(((17.4, 13.6), (29.4, 13.6), (41, 21))):
        m.stand(f"brook_lantern_{k}", fp.fairy_lantern(np.random.default_rng(457 + k)), x, ground)
        m.light((x + 0.3) * LOGICAL, ground * LOGICAL - 26, 40, GLOW_LIGHT)
    m.point("west", (0, 16), (0, 17))
    m.point("westArrive", (2, 17))
    m.point("chest", (40, 19))
    m.warp("west", "petalMeadow", "eastArrive", "left")
    return m


def glow_wood():
    width, height = 40, 34
    m = MapLayout("glowWood", "달빛 버섯 숲", width, height, music="sorrow", weather="motes", ambient=NIGHT, seed=461)
    road = [(20, height + 1), (18, 27), (24, 20), (15, 13), (22, 7), (21, -1)]
    clearings = [(18, 27, 7, 4.4), (24, 20, 7.5, 4), (15, 13, 7.5, 4.2), (22, 7, 7, 3.6)]
    open_ground = m.near_line(road, 2.8)
    for cx, cy, rx, ry in clearings:
        open_ground |= ((m.cx - cx) / rx) ** 2 + ((m.cy - cy) / ry) ** 2 <= 1
    m.kinds[~open_ground] = "tallgrass"
    m.line("path", road, 2.0)
    woods = ~open_ground
    forest.plant(m, woods, np.random.default_rng(462), PINES, low=woods & forest.canopy_over(m.kinds == "path"))
    m.block_where(woods)
    m.border()
    caps = ((TEAL_CAP, 78), (VIOLET_CAP, 64), (TEAL_CAP, 58))
    spots = ((13, 26.6), (24.4, 29), (29, 20.6), (19, 17.6), (10, 13.6), (19.6, 11.6), (17, 7.4), (27, 6.6))
    for k, (x, ground) in enumerate(spots):
        cap, size = caps[k % len(caps)]
        m.stand(f"big_shroom_{k % len(caps)}", giant_mushroom(463 + k % len(caps), cap, size), x, ground, block=(size / T * 0.34, 0.5))
        m.light(x * LOGICAL, (ground - 3) * LOGICAL, 58, GLOW_LIGHT if cap is TEAL_CAP else VIOLET_LIGHT, flicker=True)
    for k, (x, ground) in enumerate(((15, 29), (22, 23), (12, 11), (24, 9))):
        m.stand(f"glow_shrooms_{k}", fp.glow_shrooms(np.random.default_rng(470 + k)), x, ground)
    m.point("south", (19, height - 1), (20, height - 1))
    m.point("southArrive", (20, height - 3))
    m.point("north", (20, 0), (21, 0))
    m.point("northArrive", (21, 2))
    m.warp("south", "petalMeadow", "northArrive", "down")
    m.warp("north", "fairySpring", "southArrive", "up")
    return m


def fairy_spring():
    width, height = 30, 24
    m = MapLayout("fairySpring", "요정의 샘", width, height, music="sorrow", weather="motes", ambient=NIGHT, seed=481)
    glade = ((m.cx - 15) / 12.5) ** 2 + ((m.cy - 11) / 8.6) ** 2 <= 1
    road = [(15, height + 1), (15, 17)]
    open_ground = glade | m.near_line(road, 2.6)
    m.kinds[~open_ground] = "tallgrass"
    m.kinds[pg.noise(height, width, 3, np.random.default_rng(482)) > 0.62] = "flowers"
    m.kinds[~open_ground] = "tallgrass"
    m.line("path", road, 2.0)
    m.disc("water", 15, 10, 3.8)
    woods = ~open_ground
    forest.plant(m, woods, np.random.default_rng(483), PINES)
    m.block_where(woods)
    m.border()
    for k in range(8):
        angle = k * math.pi / 4 + math.pi / 8
        x, ground = 15 + math.cos(angle) * 6.4, 10.4 + math.sin(angle) * 4.8
        m.stand(f"ring_stone_{k % 3}", standing_stone(484 + k % 3), x, ground, block=(1.0, 0.4))
    m.light(15 * LOGICAL, 10 * LOGICAL, 90, GLOW_LIGHT, flicker=True)
    for k, (x, y) in enumerate(((13.2, 8.8), (16, 10.6))):
        m.decal(f"spring_lily_{k}", lily(490 + k), x * T, y * T)
    for k, (x, ground) in enumerate(((5, 12), (25, 12), (9, 17), (21, 17))):
        m.stand(f"spring_shrooms_{k}", fp.glow_shrooms(np.random.default_rng(492 + k)), x, ground)
    m.point("chest", (15, 4))
    m.point("south", (14, height - 1), (15, height - 1))
    m.point("southArrive", (15, height - 3))
    m.warp("south", "glowWood", "northArrive", "down")
    return m


MAPS = {
    "petalMeadow": (petal_meadow, "westArrive", ("eastArrive", "northArrive")),
    "fireflyBrook": (firefly_brook, "westArrive", ("chest",)),
    "glowWood": (glow_wood, "southArrive", ("northArrive",)),
    "fairySpring": (fairy_spring, "southArrive", ("chest",)),
}


def main(project, only):
    for map_id, (build, start, goals) in MAPS.items():
        if only and map_id not in only:
            continue
        m = build()
        for goal in goals:
            for cell in m.points[goal]:
                if not m.reachable(m.points[start][0], [cell]):
                    print(f"{map_id}: {start}에서 {goal} {cell}에 닿지 못한다")
        print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), set(sys.argv[2:]))
