"""악마·불꽃·차원·모래 판도라의 스킬 아이콘. 판도라 색 원판에 동작을 나타내는 실루엣 하나를 얹는다.

버튼 지름 26가상px = 52점이고, 점 하나 = 2px로 키워 저장한다. 이름 없이도 읽히도록 글리프는 굵게 그린다.

  Assets/MiniPandoras/Art/PixelGen/UI/SkillIcons/<스킬 id>.png, lock.png
  Docs/ArtUpgrade/PixelGen/skill_icons_preview.png

사용: python Tools/PixelGen/skill_icons.py <프로젝트 루트>
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import pixelgen as pg
from skill_fx import finger, grid, over, polygon_mask, tongue, outline, blank

SIZE = 52
CENTER = (SIZE - 1) / 2
DEMON = pg.hexes("#24102f", "#4a1a5a", "#ffd6e6", "#ff86b6")
FLAME = pg.hexes("#6e180c", "#bb3a16", "#fff2c0", "#ffb648")
PHASE = pg.hexes("#241a66", "#3a2aa0", "#ddd6ff", "#7d6cff")
SAND = pg.hexes("#4a2e10", "#8a5f22", "#fff2c8", "#e3bb62")
LOCK = pg.hexes("#16141e", "#2c2a38", "#5e5a68", "#46424e")


def disc(colours):
    canvas = blank(SIZE, SIZE)
    x, y = grid(SIZE, SIZE)
    r = np.hypot(x - CENTER, y - CENTER)
    over(canvas, r <= SIZE / 2 - 0.5, colours[0])
    # 왼쪽 위가 밝은 둥근 면.
    over(canvas, np.hypot(x - CENTER + 3, y - CENTER + 3) <= SIZE / 2 - 5, colours[1])
    return canvas


def stamp(canvas, mask, colours):
    """글리프를 얹는다: 밝은 면, 오른쪽 아래의 그늘, 어두운 테두리."""
    glyph = blank(SIZE, SIZE)
    over(glyph, mask, colours[2])
    shaded = mask & ~np.roll(np.roll(mask, 2, axis=0), 2, axis=1)
    over(glyph, shaded, colours[3])
    outline(glyph, colours[0])
    solid = glyph[..., 3] > 0
    canvas[solid] = glyph[solid]


def thorn(base_x, base_y, tall, half, lean=0.0):
    return polygon_mask(SIZE, SIZE, [(base_x - half, base_y), (base_x + lean, base_y - tall), (base_x + half, base_y)])


def ellipse(cx, cy, rx, ry, thickness):
    x, y = grid(SIZE, SIZE)
    r = np.hypot((x - cx) / rx, (y - cy) / ry)
    return (r <= 1.0) & (r >= 1.0 - thickness)


def flame(cx, base, width, height, phase=0.0):
    x, y = grid(SIZE, SIZE)
    return tongue(x, y, width, height, cx, base, 0.1, phase, 1.0) > 0.03


def dark_hand():
    palm = (15.0, 34.0)
    x, y = grid(SIZE, SIZE)
    mask = np.hypot((x - palm[0]) / 8.0, (y - palm[1]) / 7.0) < 1.0
    for angle, grow in ((-82, 0.82), (-56, 1.0), (-30, 1.0), (-4, 0.82)):
        spot = (palm[0] + 3 + (angle + 43) * -0.04, palm[1] - 3)
        mask |= polygon_mask(SIZE, SIZE, finger(spot, angle, 33 * grow, 4.4))
    return mask


def spike_trap():
    return thorn(26, 40, 34, 8) | ellipse(26, 40, 19, 6.5, 0.34)


def spike_forest():
    return thorn(14, 42, 22, 6, -1) | thorn(38, 42, 25, 6, 1) | thorn(26, 42, 36, 7)


def black_wings():
    mask = np.zeros((SIZE, SIZE), bool)
    for mirror in (False, True):
        points = [(26, 26), (32, 12), (47, 7), (47, 22), (41, 22), (40, 31), (34, 30), (31, 39)]
        if mirror:
            points = [(51 - px, py) for px, py in points]
        mask |= polygon_mask(SIZE, SIZE, points)
    return mask


def ember_shot():
    x, y = grid(SIZE, SIZE)
    head = (33.0, 19.0)
    # 왼쪽 아래로 꼬리가 줄어든다.
    along = ((head[0] - x) + (y - head[1])) / math.sqrt(2)
    across = np.abs((head[0] - x) - (y - head[1])) / math.sqrt(2)
    tail = (along > 0) & (along < 28) & (across < 10.5 * (1 - along / 30))
    return (np.hypot(x - head[0], y - head[1]) < 11) | tail


def flame_dive():
    # 위에서 곧장 떨어지는 혜성과 부딪히는 바닥.
    x, y = grid(SIZE, SIZE)
    head = np.hypot(x - 26, y - 28) < 10
    tail = polygon_mask(SIZE, SIZE, [(16, 27), (21, 12), (24, 18), (26, 5), (29, 17), (32, 12), (36, 27)])
    return head | tail | ellipse(26, 41, 21, 5, 0.46)


def flame_circle():
    mask = ellipse(CENTER, CENTER + 1, 15, 15, 0.34)
    for angle in range(0, 360, 45):
        a = math.radians(angle - 90)
        px, py = CENTER + math.cos(a) * 14, CENTER + 1 + math.sin(a) * 14
        mask |= polygon_mask(SIZE, SIZE, [(px + math.cos(a + 1.4) * 4, py + math.sin(a + 1.4) * 4),
                                          (px + math.cos(a + 0.5) * 9, py + math.sin(a + 0.5) * 9),
                                          (px + math.cos(a - 1.4) * 4, py + math.sin(a - 1.4) * 4)])
    return mask


def sea_of_fire():
    return flame(11, 42, 15, 22, 1.0) | flame(41, 42, 15, 20, 3.0) | flame(19, 42, 15, 30, 4.0) | flame(33, 42, 15, 27, 2.0) | flame(26, 42, 17, 38, 0.0)


def person(cx):
    x, y = grid(SIZE, SIZE)
    head = np.hypot(x - cx, y - 17) < 6.5
    return head | polygon_mask(SIZE, SIZE, [(cx - 8, 40), (cx - 6, 25), (cx + 6, 25), (cx + 8, 40)])


def hollow(mask, thickness):
    core = mask.copy()
    for axis, step in ((0, thickness), (0, -thickness), (1, thickness), (1, -thickness)):
        core &= np.roll(mask, step, axis=axis)
    return mask & ~core


def phase_shift():
    # 가운데 몸 좌우로 겹친 잔상.
    body = person(26)
    around = hollow(person(14) | person(38), 2) & ~np.roll(body, 3, axis=1) & ~np.roll(body, -3, axis=1)
    return body | around


def space_leap():
    # 틈에서 오른쪽으로 뛰어나가는 화살표.
    crack = polygon_mask(SIZE, SIZE, [(14, 7), (20, 17), (13, 26), (20, 35), (15, 45), (25, 35), (18, 26), (25, 17)])
    arrow = polygon_mask(SIZE, SIZE, [(23, 22), (33, 22), (33, 13), (47, 26), (33, 39), (33, 30), (23, 30)])
    return crack | arrow


def chip(left, top, side):
    x, y = grid(SIZE, SIZE)
    return (x >= left) & (x < left + side) & (y >= top) & (y < top + side)


def dimension_cut():
    x, y = grid(SIZE, SIZE)
    along = ((x - CENTER) - (y - CENTER)) / math.sqrt(2)
    across = ((x - CENTER) + (y - CENTER)) / math.sqrt(2)
    slit = np.abs(across) < 6 * np.clip(1 - (along / 24) ** 2, 0, 1)
    return slit | chip(11, 13, 5) | chip(33, 34, 6) | chip(14, 22, 3) | chip(30, 28, 3)


def dimension_gate():
    arrow = polygon_mask(SIZE, SIZE, [(21, 23), (26, 23), (26, 19), (33, 26), (26, 33), (26, 29), (21, 29)])
    return ellipse(11, CENTER, 7.5, 18, 0.5) | ellipse(40, CENTER, 7.5, 18, 0.5) | arrow


def sand_grasp():
    """땅에서 솟은 손."""
    x, y = grid(SIZE, SIZE)
    mask = (np.abs(x - CENTER) < 7) & (y > 25) & (y < 42)
    mask |= ellipse(CENTER, 41, 17, 4.5, 1.0)
    for k, lean in enumerate((-9, -3, 3, 9)):
        mask |= polygon_mask(SIZE, SIZE, finger((CENTER + (k - 1.5) * 4, 27), -90 + lean * 1.6, 17 - abs(k - 1.5) * 2, 3.6))
    return mask


def quicksand():
    """가운데로 말려 드는 소용돌이."""
    x, y = grid(SIZE, SIZE)
    u, v = (x - CENTER) / 19.0, (y - CENTER - 1) / 12.0
    q = np.hypot(u, v)
    th = np.arctan2(v, u)
    arm = np.sin(th * 2 - 2.6 * np.log(np.maximum(q, 0.05))) > 0.25
    return (q < 1.0) & (q > 0.12) & arm


def heat_wave():
    """옆으로 흐르는 모래 세 줄."""
    x, y = grid(SIZE, SIZE)
    mask = np.zeros((SIZE, SIZE), dtype=bool)
    for row in (15.0, 26.0, 37.0):
        mask |= (np.abs(y - (row + np.sin(x * 0.42 + row) * 3.2)) < 2.6) & (np.abs(x - CENTER) < 18)
    return mask


def ra_judgment():
    """치솟는 모래 기둥과 빛살."""
    mask = polygon_mask(SIZE, SIZE, [(19, 43), (23, 12), (29, 12), (33, 43)]) | ellipse(CENTER, 42, 16, 4.5, 1.0)
    for angle in (-150, -120, -60, -30):
        a = math.radians(angle)
        px, py = CENTER + math.cos(a) * 12, 14 + math.sin(a) * 2
        mask |= polygon_mask(SIZE, SIZE, [(px - 2, py), (px + math.cos(a) * 11, py + math.sin(a) * 11), (px + 2, py)])
    return mask


def padlock():
    x, y = grid(SIZE, SIZE)
    body = (np.abs(x - CENTER) < 12) & (y > 23) & (y < 41)
    shackle = ellipse(CENTER, 23, 8.5, 10, 0.34) & (y < 24)
    return body | shackle


ICONS = (
    ("darkHand", DEMON, dark_hand), ("spikeTrap", DEMON, spike_trap), ("spikeForest", DEMON, spike_forest), ("blackWings", DEMON, black_wings),
    ("emberShot", FLAME, ember_shot), ("flameDive", FLAME, flame_dive), ("flameCircle", FLAME, flame_circle), ("seaOfFire", FLAME, sea_of_fire),
    ("phaseShift", PHASE, phase_shift), ("spaceLeap", PHASE, space_leap), ("dimensionCut", PHASE, dimension_cut), ("dimensionGate", PHASE, dimension_gate),
    ("sandGrasp", SAND, sand_grasp), ("quicksand", SAND, quicksand), ("heatWave", SAND, heat_wave), ("raJudgment", SAND, ra_judgment),
    ("lock", LOCK, padlock),
)


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/UI/SkillIcons"
    out.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (len(ICONS) * (SIZE * 4 + 8) + 8, SIZE * 4 + 16), (46, 74, 44, 255))
    for i, (name, colours, draw) in enumerate(ICONS):
        canvas = disc(colours)
        stamp(canvas, draw(), colours)
        Image.fromarray(pg.upscale(canvas), "RGBA").save(out / f"{name}.png")
        big = Image.fromarray(canvas, "RGBA").resize((SIZE * 4, SIZE * 4), Image.NEAREST)
        sheet.alpha_composite(big, (8 + i * (SIZE * 4 + 8), 8))
    sheet.save(project / "Docs/ArtUpgrade/PixelGen/skill_icons_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
