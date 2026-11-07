"""10장 페어리가든의 소품(다음 장들의 맵과 소품을 생성기로).

  giant_trunk(rng)          거대고목 밑동(세우는 것): 맵 위 끝까지 솟은 굵은 줄기와 양옆으로 퍼진 판뿌리, 세로로 깊게 팬 껍질 골,
                            이끼·담쟁이·선반 버섯, 높이 박힌 요정 창. 밑동에 고목 안(1층 뿌리 홀)으로 드는 뿌리 아치 문(treeGate).
                            쉼터는 고목 3층의 방이라 밑동에는 문이 하나다
  trunk_roots(rng)          밑동 앞 땅 위로 뻗은 뿌리(바닥 그림)
  mushroom_house(rng, cap)  요정의 버섯 집: 크림색 굵은 대에 둥근 문·창·꽃 상자, 점박이 갓과 작은 굴뚝
  fairy_lantern(rng)        말린 덩굴 기둥 끝에 매단 초롱꽃 등불
  glow_shrooms(rng)         빛나는 작은 버섯 무리
  feast_table(rng, night)   그루터기 연회 탁자: 낮에는 꽃병, 밤(연회)에는 접시·과일·잔·촛불
  lily_pads(rng)            냇물 위 연잎과 꽃(바닥 그림)

빛은 왼쪽 위에서 온다. 점 격자(한 칸 32점)에 그린다. (그림, 기준점) 기준점은 땅에 닿는 가운데다.
"""
import numpy as np

import pixelgen as pg
from houses import BRASS, WARM, WOOD, Canvas, outline

T = pg.TILE
BARK = pg.hexes("#1e1712", "#2e241b", "#433426", "#5a4733", "#725c43", "#8c7656", "#a8916c")
MOSS = pg.hexes("#24401f", "#335a2a", "#477836", "#62963f", "#86b452")
FUNGUS = pg.hexes("#7a5a32", "#b88a4a", "#dcb46c", "#f2dca0")
STEM = pg.hexes("#8a7a62", "#b8a88a", "#d8cbb0", "#efe6d2", "#fbf6ea")
CAPS = {
    "red": pg.hexes("#5a1616", "#8e2222", "#bc3a2c", "#e05a3e", "#f4866a"),
    "blue": pg.hexes("#16264e", "#223c72", "#30589a", "#4878bc", "#74a2dc"),
    "violet": pg.hexes("#2e1a48", "#46286a", "#60388e", "#7e50b0", "#a478d2"),
}
SPOT = pg.hexes("#d8d0c0", "#f6f2e8")
GLASS = WARM
GLOW = pg.hexes("#1e5a5e", "#2e8a8a", "#52c4b4", "#9cf0dc", "#e4fff6")
DOOR = pg.hexes("#2a3a22", "#3c5230", "#527040", "#6c8c52")
FRUIT = pg.hexes("#c8402c", "#e8a030", "#8a3cb0", "#e8d860")


def bark_texture(c, mask, rng, left, right, grain=13):
    """둥근 줄기의 껍질: 세로 골이 굽이치고, 왼쪽이 밝고 오른쪽으로 갈수록 어둡다. 골은 잡음으로 흔들린다."""
    w, h = c.size
    wave = pg.noise(h, w, 24, rng)
    u = np.clip((c.xx - left) / max(1, right - left), 0, 1)
    shade = 0.62 - 0.38 * u + 0.18 * np.sin(u * np.pi) - 0.12 * (u < 0.1)
    ridge = np.mod(c.xx + wave * 10 + np.sin(c.yy / 17.0) * 2, grain) / grain
    level = shade + 0.12 * np.sin(ridge * np.pi) - 0.22 * (ridge < 0.16)
    level += (pg.noise(h, w, 5, rng) - 0.5) * 0.1
    c.paint(mask, pg.quantize(np.clip(level, 0, 0.999), BARK, dither=0.1))


def moss_patches(c, mask, rng, amount=0.62, spacing=14):
    w, h = c.size
    blot = pg.fractal_noise(h, w, rng, ((spacing, 0.7), (5, 0.3)))
    lit = 0.6 - (c.yy / h) * 0.2 + (pg.bayer(h, w) - 0.5) * 0.15
    moss = mask & (blot > amount)
    c.paint(moss, pg.quantize(np.clip(lit + (blot - amount) * 2, 0, 0.999), MOSS[1:], dither=0.1))
    c.paint(moss & ~pg.neighbours(~moss) & (blot > amount + 0.08) & (pg.bayer(h, w) > 0.7), MOSS[4])


def shelf_fungus(c, cx, cy, width):
    """줄기에 붙은 반달 선반 버섯 한 장(윗면 밝고 아랫면 그늘, 가장자리 띠)."""
    cap = c.ellipse(cx, cy, width / 2, width / 4) & (c.yy <= cy + 1)
    c.paint(cap, FUNGUS[2])
    c.paint(cap & (c.yy < cy - width / 8), FUNGUS[3])
    c.paint(c.ellipse(cx, cy + 1, width / 2, 2) & ~cap, FUNGUS[0])
    c.paint(cap & (c.yy > cy - 1), FUNGUS[1])


def fairy_window(c, cx, cy, r, rng):
    """줄기에 박힌 둥근 요정 창: 나무 테, 따뜻한 불빛, 열십자 창살, 아래 꽃 상자."""
    c.paint(c.ellipse(cx, cy, r + 2, r + 2), WOOD[2])
    c.paint(c.ellipse(cx, cy, r + 1, r + 1), WOOD[4])
    glass = c.ellipse(cx, cy, r, r)
    c.paint(glass, GLASS[2])
    c.paint(glass & (c.yy < cy - r * 0.2) & (c.xx < cx + r * 0.3), GLASS[3])
    c.paint(glass & ((np.abs(c.xx - cx) < 1) | (np.abs(c.yy - cy) < 1)), WOOD[2])
    box = c.rect(cx - r - 2, cy + r + 1, cx + r + 3, cy + r + 5)
    c.paint(box, WOOD[3])
    for k in range(int(2 * r + 2)):
        if rng.random() < 0.6:
            x = int(cx - r - 1 + k)
            y = int(cy + r - rng.integers(0, 2))
            c.paint(c.rect(x, y, x + 2, y + 2), pg.FLOWERS[rng.integers(0, len(pg.FLOWERS))])


def ivy(c, x0, y0, length, rng):
    """위에서 늘어진 담쟁이 줄기와 잎."""
    x = float(x0)
    for y in range(int(y0), int(y0 + length)):
        x += rng.uniform(-0.6, 0.6)
        c.paint(c.rect(x, y, x + 1, y + 1), MOSS[1])
        if y % 4 == 0:
            side = -2 if (y // 4) % 2 else 2
            c.paint(c.ellipse(x + side, y, 2.4, 1.8), MOSS[3])
            c.paint(c.ellipse(x + side - 0.6, y - 0.6, 1.2, 0.9), MOSS[4])


WIDTH_TILES = 15                  # 밑동 그림 너비(칸): 판뿌리까지
TRUNK_HEIGHT = 6                  # 땅에서 맵 위 끝까지(칸)
GATE_X, INN_X = 7.0, 10.0         # 그림 왼쪽에서 잰 문과 옆 창(옛 쉼터 문 자리)의 가운데(칸)


def giant_trunk(rng):
    w, h = WIDTH_TILES * T, TRUNK_HEIGHT * T + 6
    ground = TRUNK_HEIGHT * T
    c = Canvas(w, h)
    left, right = 1.5 * T, 13.5 * T
    # 줄기: 위쪽은 맵 위로 이어지고, 아래로 내려오며 판뿌리가 양옆으로 퍼진다.
    flare = np.clip((c.yy - 2.2 * T) / (ground - 2.2 * T), 0, 1) ** 2.2
    l_edge = left + 0.9 * T - flare * 1.9 * T + np.sin(c.yy / 21.0) * 3
    r_edge = right - 0.9 * T + flare * 1.9 * T + np.sin(c.yy / 19.0 + 1) * 3
    trunk = (c.xx >= l_edge) & (c.xx <= r_edge) & (c.yy < ground)
    # 판뿌리 넷: 밑동에서 땅으로 흘러내리는 굵은 뿌리 등.
    roots = np.zeros_like(trunk)
    for base, reach, side in ((left + 1.2 * T, 1.6 * T, -1), (left + 3.2 * T, 0.7 * T, -1), (right - 3.0 * T, 0.7 * T, 1), (right - 1.2 * T, 1.6 * T, 1)):
        top = ground - 2.2 * T
        t = np.clip((c.yy - top) / (ground + 4 - top), 0, 1)
        centre = base + side * reach * t ** 1.5
        half = 10 + 12 * t
        roots |= (np.abs(c.xx - centre) <= half) & (c.yy >= top) & (c.yy <= ground + 4 - 3 * np.abs(c.xx - centre) / half)
    body = trunk | roots
    bark_texture(c, body, rng, left - 0.5 * T, right + 0.5 * T)
    moss_patches(c, body, rng)
    # 뿌리 등 위 그늘 띠와 뿌리 사이 어둠.
    c.paint(roots & ~trunk & (c.yy > ground - 6), BARK[1])
    for x0, y0, wd in ((3.0, 2.2, 18), (11.2, 1.5, 22), (2.6, 4.0, 14), (12.3, 3.6, 16)):
        shelf_fungus(c, x0 * T, y0 * T, wd)
    for x0, y0, r in ((4.4, 1.2, 5), (8.6, 0.7, 6), (11.6, 2.6, 4.5)):
        fairy_window(c, x0 * T, y0 * T, r, rng)
    for x0, y0, length in ((2.4 * T, 0, 70), (5.8 * T, 0, 46), (12.9 * T, 0, 60), (9.7 * T, 0, 30)):
        ivy(c, x0, y0, length, rng)

    # 고목 안으로 드는 문(treeGate): 비틀린 뿌리 테를 두른 높은 아치, 안쪽 깊이 따뜻한 빛, 뿌리 문턱.
    gx = GATE_X * T + T / 2
    frame = c.arch(gx - 34, ground - 92, gx + 34, ground)
    opening = c.arch(gx - 26, ground - 84, gx + 26, ground)
    c.paint(frame, BARK[4])
    c.paint(frame & (c.xx < gx - 28), BARK[5])
    c.paint(frame & ~opening & (np.mod(c.xx + c.yy, 9) < 2), BARK[2])
    c.paint(opening, BARK[0])
    deep = c.ellipse(gx, ground - 30, 16, 26) & opening
    c.paint(deep, WARM[0])
    c.paint(c.ellipse(gx, ground - 26, 9, 16) & opening, WARM[1])
    c.paint(c.ellipse(gx, ground - 22, 4, 8) & opening, WARM[2])
    c.paint(c.rect(gx - 30, ground - 5, gx + 30, ground) & frame, BARK[3])
    c.paint(c.rect(gx - 30, ground - 5, gx + 30, ground - 4) & frame, BARK[5])

    # 문 옆 창 둘과 등불(고목 안 1층 홀의 불빛이 비친다).
    ix = INN_X * T + T / 2
    fairy_window(c, ix, ground - 40, 7, rng)
    fairy_window(c, ix - 38, ground - 40, 6, rng)
    lamp_x = ix + 18
    c.line(lamp_x, ground - 76, lamp_x, ground - 58, WOOD[1], 2)
    c.line(lamp_x - 8, ground - 76, lamp_x + 1, ground - 76, WOOD[1], 2)
    c.paint(c.ellipse(lamp_x - 7, ground - 66, 4, 6), BRASS[2])
    c.paint(c.ellipse(lamp_x - 7, ground - 66, 2.6, 4.4), WARM[3])
    return outline(c.image), (w / 2, ground)


def trunk_roots(rng):
    """밑동 앞 땅 위로 기어 나온 뿌리 줄기들(바닥 그림, 왼쪽 위 기준). 뿌리 위는 걸을 수 있다."""
    w, h = WIDTH_TILES * T, int(1.6 * T)
    c = Canvas(w, h)
    for base, length, angle, thick in ((1.2, 2.2, 2.6, 6), (2.4, 1.6, 2.2, 5), (12.6, 1.6, 0.9, 5), (13.6, 2.2, 0.5, 6),
                                       (4.6, 1.0, 1.9, 4), (10.8, 1.0, 1.2, 4)):
        x0, y0 = base * T, 2
        for k in range(int(length * T)):
            t = k / (length * T)
            x = x0 + np.cos(angle) * k + np.sin(k / 9.0) * 2
            y = y0 + np.sin(angle) * k * 0.45 + t * 6
            r = thick * (1 - t * 0.7)
            disc = c.ellipse(x, y, r, r * 0.7)
            c.paint(disc, BARK[3])
            c.paint(disc & (c.yy < y - r * 0.2), BARK[5])
            c.paint(disc & (c.yy > y + r * 0.35), BARK[1])
    image = outline(c.image)
    return image


def mushroom_house(rng, cap="red", width=150):
    """요정의 버섯 집. 대(벽)에 둥근 문과 창, 꽃 상자, 갓에 흰 점과 작은 굴뚝."""
    colors = CAPS[cap]
    h = int(width * 1.08)
    c = Canvas(width, h)
    cx, ground = width / 2, h - 2
    stem_w, stem_h = width * 0.52, h * 0.46
    stem = c.ellipse(cx, ground - stem_h * 0.5, stem_w / 2, stem_h * 0.62) & (c.yy <= ground)
    stem |= c.rect(cx - stem_w / 2 + 3, ground - stem_h * 0.6, cx + stem_w / 2 - 3, ground)
    u = (c.xx - (cx - stem_w / 2)) / stem_w
    c.paint(stem, pg.quantize(np.clip(0.8 - 0.55 * u + 0.15 * np.sin(np.clip(u, 0, 1) * np.pi), 0, 0.999), STEM, dither=0.08))
    # 갓: 넓은 반구, 아래 테 그늘, 흰 점.
    cap_cy = ground - stem_h - 2
    cap_rx, cap_ry = width / 2 - 2, h * 0.36
    dome = c.ellipse(cx, cap_cy, cap_rx, cap_ry) & (c.yy <= cap_cy + cap_ry * 0.32)
    light = 0.82 - ((c.xx - cx) / cap_rx) * 0.35 - ((c.yy - (cap_cy - cap_ry)) / (cap_ry * 1.3)) * 0.4
    c.paint(dome, pg.quantize(np.clip(light, 0, 0.999), colors, dither=0.1))
    rim = dome & (c.yy > cap_cy + cap_ry * 0.32 - 4)
    c.paint(rim, colors[1])
    gills = c.ellipse(cx, cap_cy + cap_ry * 0.32, cap_rx * 0.82, 5) & ~dome
    c.paint(gills, STEM[1])
    c.paint(gills & (np.mod(c.xx, 4) == 0), STEM[0])
    for _ in range(9):
        a = rng.uniform(np.pi * 1.05, np.pi * 1.95)
        d = rng.uniform(0.25, 0.85)
        sx, sy = cx + np.cos(a) * cap_rx * d, cap_cy + np.sin(a) * cap_ry * d * 0.9
        r = rng.uniform(3, 6.5) * (1 - d * 0.3)
        spot = c.ellipse(sx, sy, r, r * 0.75) & dome
        c.paint(spot, SPOT[0])
        c.paint(spot & (c.yy < sy) & (c.xx < sx + r * 0.3), SPOT[1])
    # 작은 굴뚝.
    chim_x = cx + cap_rx * 0.42
    chimney = c.rect(chim_x - 4, cap_cy - cap_ry * 0.85, chim_x + 4, cap_cy - cap_ry * 0.45)
    c.paint(chimney, BARK[3])
    c.paint(c.rect(chim_x - 5, cap_cy - cap_ry * 0.85 - 3, chim_x + 5, cap_cy - cap_ry * 0.85), BARK[2])
    # 둥근 문과 창, 꽃 상자, 문 앞 디딤돌.
    door = c.arch(cx - 11, ground - 36, cx + 11, ground)
    c.paint(c.arch(cx - 13, ground - 38, cx + 13, ground), WOOD[2])
    c.paint(door, pg.quantize(np.clip(0.65 - (c.xx - cx + 11) / 40, 0, 0.999), DOOR, dither=0.05))
    c.paint(door & (np.mod(c.xx - cx + 11, 6) == 0), DOOR[0])
    c.paint(c.ellipse(cx + 6, ground - 17, 1.8, 1.8), BRASS[4])
    for wx in (cx - stem_w * 0.3, cx + stem_w * 0.3):
        fairy_window(c, wx, ground - stem_h * 0.62, 5, rng)
    c.paint(c.ellipse(cx, ground - 1, 15, 3) & (c.yy >= ground - 2), pg.STONE[3])
    return outline(c.image), (cx, ground)


def fairy_lantern(rng):
    """말린 덩굴 기둥과 매단 초롱꽃 등불(빛은 맵 빛이 따로 비춘다)."""
    w, h = 26, 76
    c = Canvas(w, h)
    x = 8.0
    for y in range(h - 1, 14, -1):
        x += np.sin(y / 9.0) * 0.18
        c.paint(c.rect(x, y, x + 2, y + 1), WOOD[2])
        c.paint(c.rect(x, y, x + 1, y + 1), WOOD[4])
    # 꼭대기에서 말려 앞으로 굽은 고리.
    for k in range(18):
        a = np.pi - k / 17 * np.pi * 1.1
        px, py = x + 6 + np.cos(a) * 6, 15 - np.sin(a) * 7
        c.paint(c.rect(px, py, px + 2, py + 2), WOOD[3])
    bell = c.ellipse(x + 12, 24, 5, 6) & (c.yy >= 19)
    c.paint(bell, GLOW[2])
    c.paint(bell & (c.yy < 23) & (c.xx < x + 12), GLOW[4])
    c.paint(c.rect(x + 7, 29, x + 18, 31) & c.ellipse(x + 12, 29, 6, 3), GLOW[1])
    c.paint(c.ellipse(x + 12, 18, 3, 1.5), MOSS[3])
    for k in range(3):
        c.paint(c.ellipse(x - 2 + k * 2, 50 + k * 9, 2.2, 1.4), MOSS[3])
    return outline(c.image), (x + 1, h - 1)


def glow_shrooms(rng):
    """빛나는 청록 갓의 작은 버섯 서넛."""
    w, h = 30, 22
    c = Canvas(w, h)
    for _ in range(int(rng.integers(3, 5))):
        sx = rng.uniform(5, w - 5)
        tall = rng.uniform(7, 14)
        r = rng.uniform(3, 5)
        c.paint(c.rect(sx - 1, h - 1 - tall, sx + 1, h - 1), STEM[3])
        cap = c.ellipse(sx, h - 1 - tall, r, r * 0.7) & (c.yy <= h - 1 - tall + 1)
        c.paint(cap, GLOW[2])
        c.paint(cap & (c.yy < h - 2 - tall) & (c.xx < sx), GLOW[4])
    return outline(c.image), (w / 2, h - 1)


def feast_table(rng, night):
    """그루터기 연회 탁자: 나이테가 보이는 윗면과 껍질 옆면, 뿌리. 낮에는 꽃병, 밤에는 음식과 촛불."""
    w, h = 84, 64
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    side = c.rect(cx - 34, ground - 30, cx + 34, ground) | c.ellipse(cx, ground, 34, 6)
    bark_texture(c, side, rng, cx - 34, cx + 34, grain=8)
    top = c.ellipse(cx, ground - 30, 36, 14)
    c.paint(top, FUNGUS[2])
    rings = np.hypot((c.xx - cx) / 36, (c.yy - (ground - 30)) / 14)
    c.paint(top & (np.mod(rings * 7, 1) < 0.18), FUNGUS[1])
    c.paint(top & (rings > 0.9), BARK[4])
    for dx in (-36, 36):
        c.paint(c.ellipse(cx + dx * 1.05, ground - 2, 7, 4), BARK[3])
    if night:
        for px, py in ((cx - 18, ground - 33), (cx + 14, ground - 27), (cx - 2, ground - 24)):
            c.paint(c.ellipse(px, py, 8, 3.5), STEM[4])
            c.paint(c.ellipse(px, py, 6, 2.5), STEM[2])
            for k in range(3):
                c.paint(c.ellipse(px - 3 + k * 3, py - 1.5, 2, 2), FRUIT[int(rng.integers(0, len(FRUIT)))])
        for px, py in ((cx + 22, ground - 35), (cx - 26, ground - 26)):
            c.paint(c.rect(px - 2, py - 7, px + 2, py), BRASS[3])
            c.paint(c.rect(px - 2, py - 7, px - 1, py), BRASS[4])
        c.paint(c.rect(cx + 4, ground - 46, cx + 7, ground - 36), STEM[4])
        c.paint(c.ellipse(cx + 5.5, ground - 49, 2, 3.5), WARM[3])
        c.paint(c.ellipse(cx + 5.5, ground - 48, 1, 1.8), WARM[4])
    else:
        vase = c.ellipse(cx, ground - 36, 6, 7)
        c.paint(vase, CAPS["blue"][3])
        c.paint(vase & (c.xx < cx - 2), CAPS["blue"][4])
        for k in range(6):
            fx, fy = cx - 8 + k * 3.2, ground - 46 - (k % 3) * 2
            c.line(cx, ground - 40, fx, fy, MOSS[2])
            c.paint(c.ellipse(fx, fy, 2.2, 2.2), pg.FLOWERS[k % len(pg.FLOWERS)])
    return outline(c.image), (cx, ground)


def lily_pads(columns, rows, rng):
    """냇물 위 연잎(갈라진 둥근 잎)과 꽃 몇 송이(바닥 그림, 왼쪽 위 기준)."""
    c = Canvas(columns * T, rows * T)
    for _ in range(columns * rows * 2):
        x, y = rng.uniform(4, columns * T - 4), rng.uniform(4, rows * T - 4)
        r = rng.uniform(3.5, 6)
        pad = c.ellipse(x, y, r, r * 0.7)
        notch = c.polygon([(x, y), (x + r * 1.2, y - r * 0.3), (x + r * 1.2, y + r * 0.3)])
        c.paint(pad & ~notch, MOSS[2])
        c.paint(pad & ~notch & (c.yy < y - r * 0.2), MOSS[3])
        if rng.random() < 0.3:
            c.paint(c.ellipse(x - 1, y - 1, 2, 1.6), pg.FLOWERS[0])
            c.paint(c.ellipse(x - 1, y - 1.4, 0.9, 0.8), pg.FLOWERS[3])
    return c.image


def root_pillar(rng):
    """뿌리가 꼬여 오른 기둥(고목 1층): 굵은 뿌리 세 가닥이 서로 감아 오르고, 아래는 바닥으로 퍼지며 위는 천장으로 사라진다."""
    w, h = 40, 108
    c = Canvas(w, h)
    cx, ground = w / 2, h - 2
    for k in range(3):
        phase = k * 2 * np.pi / 3
        for y in range(4, int(ground)):
            t = (ground - y) / (ground - 4)
            spread = 5 + 9 * max(0.0, 0.25 - t) * 4
            x = cx + np.sin(y / 9.0 + phase) * spread
            front = np.cos(y / 9.0 + phase) > 0
            r = 6.5
            disc = c.ellipse(x, y, r, 2.2)
            if front or k == 0:
                c.paint(disc, BARK[4 if front else 2])
                c.paint(disc & (c.xx < x - 2), BARK[5 if front else 3])
                c.paint(disc & (c.xx > x + 3), BARK[2])
    moss_patches(c, c.image[..., 3] > 0, rng, amount=0.66, spacing=8)
    c.paint(c.ellipse(cx, ground - 1, 17, 3) & (c.yy >= ground - 2), BARK[3])
    return outline(c.image), (cx, ground)


STALL_GOODS = (pg.hexes("#e8a830", "#f6d070"), pg.hexes("#e06a8a", "#f6b8c8"), pg.hexes("#c84a3a", "#efe2c8"), pg.hexes("#5a8ad0", "#a8d0f0"))


def fairy_stall(rng, kind):
    """요정 노점: 잎으로 엮은 차양 아래 나무 판대, 판대 위 물건(꿀단지·꽃다발·버섯·물약병)."""
    w, h = 76, 70
    c = Canvas(w, h)
    ground = h - 2
    for x in (6, w - 9):
        c.paint(c.rect(x, 18, x + 3, ground), BARK[3])
    roof = c.polygon([(0, 24), (w, 24), (w - 8, 8), (8, 8)])
    c.paint(roof, MOSS[2])
    for x in range(0, w, 7):
        leaf = c.ellipse(x + 3, 24, 4.5, 4) & (c.yy > 22)
        c.paint(leaf, MOSS[3] if (x // 7) % 2 else MOSS[1])
    c.paint(roof & (np.mod(c.xx + c.yy, 9) < 2), MOSS[3])
    table = c.rect(3, 40, w - 3, 48)
    c.paint(table, BARK[4])
    c.paint(c.rect(3, 40, w - 3, 42), BARK[5])
    c.paint(c.rect(5, 48, w - 5, ground), BARK[2])
    goods = STALL_GOODS[kind % len(STALL_GOODS)]
    for k, fx in enumerate((14, 28, 44, 60)):
        if kind % 4 == 0:
            jar = c.ellipse(fx, 35, 5, 5)
            c.paint(jar, goods[0])
            c.paint(jar & (c.xx < fx - 1), goods[1])
            c.paint(c.rect(fx - 4, 29, fx + 4, 31), BARK[2])
        elif kind % 4 == 1:
            for dx, dy in ((-3, 0), (3, -1), (0, -4), (0, 2)):
                c.paint(c.ellipse(fx + dx, 34 + dy, 2.6, 2.6), goods[(k + dx) % 2])
            c.paint(c.rect(fx - 1, 36, fx + 1, 40), MOSS[2])
        elif kind % 4 == 2:
            cap = c.ellipse(fx, 34, 6, 4) & (c.yy <= 35)
            c.paint(c.rect(fx - 2, 34, fx + 2, 40), goods[1])
            c.paint(cap, goods[0])
            c.paint(cap & (np.mod(c.xx + c.yy, 5) == 0), goods[1])
        else:
            c.paint(c.rect(fx - 3, 31, fx + 3, 40), goods[0])
            c.paint(c.rect(fx - 1, 27, fx + 1, 31), goods[1])
            c.paint(c.rect(fx - 3, 33, fx - 1, 38), goods[1])
    return outline(c.image), (w / 2, ground)
