"""9장 청룡: 바다를 가르고 솟아올라 뉴웨이브 호를 두 동강 내는 용(웹게임 seaDragon 자리).

동양의 용처럼 긴 몸이 물에서 S자로 솟고, 목을 굽혀 왼쪽(뉴웨이브 호 쪽)을 내려다보며 입을 벌린다.
  - 몸: 청록 비늘(초승달 비늘 무늬, 왼쪽 위에서 빛), 한쪽은 크림색 배비늘 띠, 등에는 금빛 지느러미가 톱니처럼 돋는다.
    몸통 앞쪽에 물에서 솟았다 다시 잠기는 고리 하나, 오른쪽에 말려 올라간 꼬리 끝과 꼬리 지느러미.
  - 머리: 긴 주둥이와 벌린 턱(이빨·혀), 금빛 눈과 세로 동공, 눈썹 뼈, 뒤로 뻗은 사슴뿔 둘, 턱 뒤의 갈기 지느러미, 늘어진 수염.
  - 물: 몸이 물에 닿는 곳마다 물거품 고리와 튀는 물방울.

  Assets/MiniPandoras/Art/PixelGen/Stage/sea_dragon.png   기준점은 아래 가운데(물에 닿는 줄)
  Docs/ArtUpgrade/PixelGen/sea_dragon_preview.png         바다 위에 세운 모습

점 격자(논리 px 하나 = 2점)에 그려 두 배로 키운다.

사용: python Tools/PixelGen/sea_dragon.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

W, H = 320, 320
WATER = H - 12                      # 물에 닿는 줄(점)

TEAL = pg.hexes("#0a2228", "#10343c", "#174a52", "#1f6066", "#2a787a", "#3e928a", "#62b0a0", "#94d0b8")
BELLY = pg.hexes("#6e5e44", "#9a8662", "#c0aa80", "#dccaa0", "#f0e4c4")
FIN = pg.hexes("#5e4214", "#9a6e26", "#cc9e42", "#ecc874", "#fff0bc")
MOUTH = pg.hexes("#2c0a10", "#6a1824", "#a8343e", "#d86068")
TEETH = pg.hexes("#b8b09a", "#f6f2e2")
EYE = pg.hexes("#a86a10", "#f2b42a", "#ffe68a")
FOAM = pg.hexes("#7fb2c6", "#c4e2ea", "#f2fafc")
SEA = pg.hexes("#0c2238", "#112e48", "#163a58", "#1d4868", "#255a7c")
OUTLINE = np.array([6, 18, 20], dtype=np.uint8)
LIGHT2 = np.array([-0.6, -0.8])

YY, XX = np.mgrid[0:H, 0:W].astype(float)


def catmull(points, per=40):
    """점들을 지나는 매끈한 곡선(카트멀롬). 끝점은 한 번 더 써서 곡선이 끝점에서 끝나게 한다."""
    p = [points[0]] + list(points) + [points[-1]]
    out = []
    for i in range(1, len(p) - 2):
        p0, p1, p2, p3 = (np.array(q, dtype=float) for q in p[i - 1:i + 3])
        for t in np.linspace(0, 1, per, endpoint=False):
            t2, t3 = t * t, t * t * t
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(np.array(points[-1], dtype=float))
    return np.array(out)


class Tube:
    """곡선을 따라 굵기가 변하는 몸 한 토막. 칠한 점마다 곡선 위 길이(u)와 옆으로 벗어난 정도(s, -1~1)를 기억한다."""

    def __init__(self, points, radius_from, radius_to, belly_side):
        self.spine = catmull(points)
        steps = np.diff(self.spine, axis=0)
        self.length = np.concatenate([[0], np.cumsum(np.hypot(steps[:, 0], steps[:, 1]))])
        tangent = np.gradient(self.spine, axis=0)
        tangent /= np.hypot(tangent[:, 0], tangent[:, 1])[:, None]
        self.tangent = tangent
        self.normal = np.stack([-tangent[:, 1], tangent[:, 0]], axis=1)
        t = self.length / self.length[-1]
        self.radius = radius_from + (radius_to - radius_from) * t
        self.belly_side = belly_side

    def paint(self, canvas, u_map, s_map, owner, tag):
        """뒤(첫 점)부터 앞(끝 점)으로 원을 찍어 몸을 채운다. 나중 원이 앞이다."""
        for c, n, tg, r, length in zip(self.spine, self.normal, self.tangent, self.radius, self.length):
            x0, x1 = int(c[0] - r - 1), int(c[0] + r + 2)
            y0, y1 = int(c[1] - r - 1), int(c[1] + r + 2)
            x0, y0, x1, y1 = max(0, x0), max(0, y0), min(W, x1), min(H, y1)
            if x0 >= x1 or y0 >= y1:
                continue
            dx, dy = XX[y0:y1, x0:x1] - c[0], YY[y0:y1, x0:x1] - c[1]
            inside = dx * dx + dy * dy <= r * r
            s = (dx * n[0] + dy * n[1]) / r * self.belly_side
            u = length + dx * tg[0] + dy * tg[1]
            s_map[y0:y1, x0:x1][inside] = s[inside]
            u_map[y0:y1, x0:x1][inside] = u[inside]
            owner[y0:y1, x0:x1][inside] = tag
            canvas[y0:y1, x0:x1][inside] = True

    def fins(self, rgba, every=14, size=15, skip_end=0.0):
        """등(배 반대쪽)에서 꼬리 쪽으로 누운 톱니 지느러미."""
        for i in range(4, len(self.spine) - 2, 1):
            if self.length[i] % every >= (self.length[i] - self.length[i - 1]) + 0.01:
                continue
            if self.length[i] > self.length[-1] * (1 - skip_end):
                continue
            c, n, tg, r = self.spine[i], self.normal[i], self.tangent[i], self.radius[i]
            back = n * self.belly_side
            base0 = c + back * (r - 2) + tg * 6
            base1 = c + back * (r - 2) - tg * 6
            apex = c + back * (r + size * (0.6 + 0.4 * r / 22)) - tg * 9
            shape = triangle(base0, base1, apex)
            light = np.clip(0.75 - (YY - apex[1]) * 0.004 + ((XX - c[0]) * tg[0] + (YY - c[1]) * tg[1]) * 0.03, 0, 0.999)
            rgba[shape, :3] = pg.quantize(light, FIN[1:], dither=0.1)[shape]
            rgba[shape, 3] = 255
            rib = shape & (np.abs((XX - c[0]) * tg[0] + (YY - c[1]) * tg[1] + 1) < 0.8)
            rgba[rib, :3] = FIN[1]


def triangle(p0, p1, p2):
    def side(a, b):
        return (b[0] - a[0]) * (YY - a[1]) - (b[1] - a[1]) * (XX - a[0])
    s0, s1, s2 = side(p0, p1), side(p1, p2), side(p2, p0)
    return ((s0 >= 0) & (s1 >= 0) & (s2 >= 0)) | ((s0 <= 0) & (s1 <= 0) & (s2 <= 0))


def shade_body(rgba, body, u_map, s_map):
    """비늘과 배비늘을 칠한다. s는 배 쪽이 -1, 등 쪽이 +1이다."""
    s = np.clip(s_map, -1, 1)
    depth = np.sqrt(np.clip(1 - s * s, 0, 1))
    # 둥근 몸통: 가운데가 밝고 가장자리로 갈수록 어둡다. 화면 위쪽 몸이 빛을 더 받는다.
    light = 0.25 + 0.55 * depth + 0.12 * (1 - YY / H)
    scale_u = np.mod(u_map, 9) / 9
    row = np.floor(u_map / 9)
    scale_v = np.mod(s * 4 + (row % 2) * 0.5, 1)
    crescent = (np.hypot(scale_u - 0.5, (scale_v - 0.15) * 1.6) > 0.42) & (np.hypot(scale_u - 0.5, (scale_v - 0.3) * 1.6) < 0.5)
    rgb = pg.quantize(np.clip(light, 0, 0.999), TEAL[1:], dither=0.1)
    dark = pg.quantize(np.clip(light - 0.2, 0, 0.999), TEAL[:-1], dither=0.1)
    rgb[crescent] = dark[crescent]
    glint = (np.hypot(scale_u - 0.42, (scale_v - 0.62) * 1.6) < 0.12) & (light > 0.62)
    rgb[glint] = TEAL[-1]
    belly = s < -0.35
    plate = np.mod(u_map, 8)
    belly_light = 0.3 + 0.6 * depth + 0.1 * (1 - YY / H) - (plate < 1.2) * 0.25
    belly_rgb = pg.quantize(np.clip(belly_light, 0, 0.999), BELLY, dither=0.1)
    rgb[belly] = belly_rgb[belly]
    edge = (s < -0.3) & (s >= -0.38)
    rgb[edge] = TEAL[1]
    rgba[body, :3] = rgb[body]
    rgba[body, 3] = 255


# ── 몸의 토막(점) ─────────────────────────────────────────

# 물에서 솟아 S자로 오르다 목을 굽혀 왼쪽을 내려다본다. 마지막 점이 목 끝(머리가 붙는 곳).
MAIN = [(206, 320), (212, 276), (244, 234), (250, 190), (224, 158), (180, 150), (148, 132), (134, 108), (132, 92)]
# 몸 앞쪽에서 물 위로 솟았다 다시 잠기는 고리.
HUMP = [(42, 320), (52, 288), (82, 272), (112, 286), (122, 320)]
# 오른쪽에 말려 올라간 꼬리.
TAIL = [(268, 320), (278, 292), (298, 272), (306, 248)]
HEAD_SCALE = 1.3


def paint_head(rgba):
    """
    긴 주둥이로 왼쪽을 보며 입을 벌린 머리(뿔·갈기·수염을 뒤에서부터). 모양은 머리통 가운데를 기준으로 HEAD_SCALE배 키워 그린다:
    격자 XX·YY를 그만큼 줄여 두고 그 위에 모양을 정한다.
    """
    global XX, YY
    hx, hy = 112, 70                                     # 머리통 가운데
    grid = XX, YY
    XX = hx + (grid[0] - hx) / HEAD_SCALE
    YY = hy + (grid[1] - hy) / HEAD_SCALE
    try:
        draw_head(rgba, hx, hy)
    finally:
        XX, YY = grid


def draw_head(rgba, hx, hy):
    # 사슴뿔 둘: 뒤로 뻗는 줄기와 가지.
    for offset, shade in ((8, 1), (0, 2)):
        for (x0, y0), (x1, y1), w in (((hx + 6 + offset, hy - 10), (hx + 34 + offset, hy - 44), 4),
                                      ((hx + 22 + offset, hy - 26), (hx + 14 + offset, hy - 46), 3),
                                      ((hx + 28 + offset, hy - 36), (hx + 44 + offset, hy - 48), 3)):
            for t in np.linspace(0, 1, 60):
                x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
                r = w * (1 - t * 0.5)
                disc = np.hypot(XX - x, YY - y) <= r
                rgba[disc, :3] = FIN[shade + 1]
                rgba[disc & (XX < x - r * 0.3), :3] = FIN[shade + 2]
                rgba[disc, 3] = 255
    # 턱 뒤 갈기: 머리 뒤로 퍼지는 지느러미 다섯.
    for k, angle in enumerate(np.linspace(-0.2, 1.1, 5)):
        base = np.array([hx + 10, hy + 6])
        tip = base + np.array([np.cos(angle), np.sin(angle)]) * (30 - k * 2)
        side = np.array([-np.sin(angle), np.cos(angle)]) * 6
        shape = triangle(base + side, base - side, tip)
        rgba[shape, :3] = FIN[2 if k % 2 else 3]
        rgba[shape, 3] = 255
    # 머리통과 위턱(주둥이), 아래턱.
    skull = ((XX - hx) / 22) ** 2 + ((YY - hy) / 17) ** 2 <= 1
    upper = triangle((hx - 4, hy - 12), (hx - 62, hy - 2), (hx - 58, hy + 8)) | triangle((hx - 4, hy - 12), (hx - 58, hy + 8), (hx - 4, hy + 8))
    snout_tip = ((XX - (hx - 60)) / 8) ** 2 + ((YY - (hy + 2)) / 7) ** 2 <= 1
    lower = triangle((hx - 2, hy + 12), (hx - 50, hy + 24), (hx - 48, hy + 31)) | triangle((hx - 2, hy + 12), (hx - 48, hy + 31), (hx + 4, hy + 24))
    mouth = triangle((hx - 4, hy + 6), (hx - 56, hy + 9), (hx - 48, hy + 25)) | triangle((hx - 4, hy + 6), (hx - 48, hy + 25), (hx - 2, hy + 14))
    head = skull | upper | snout_tip | lower
    light = np.clip(0.78 - (YY - (hy - 16)) / 60 + (hx - XX) / 220, 0, 0.999)
    rgba[mouth, :3] = MOUTH[1]
    rgba[mouth & (YY > hy + 16), :3] = MOUTH[0]
    rgba[mouth, 3] = 255
    tongue = triangle((hx - 8, hy + 16), (hx - 40, hy + 18), (hx - 36, hy + 22)) | triangle((hx - 8, hy + 16), (hx - 36, hy + 22), (hx - 8, hy + 21))
    rgba[tongue, :3] = MOUTH[3]
    rgba[tongue & (YY > hy + 19.5), :3] = MOUTH[2]
    rgba[head & ~mouth, :3] = pg.quantize(light, TEAL[2:], dither=0.1)[head & ~mouth]
    rgba[head, 3] = 255
    # 턱 아래 크림색(배비늘이 이어진다)과 콧등의 비늘 줄.
    jaw_belly = lower & (YY > hy + 22 + (XX - (hx - 48)) * 0.05)
    rgba[jaw_belly, :3] = BELLY[3]
    ridge = upper & (YY < hy - 8 + (hx - XX) * 0.16) & (np.mod(XX, 6) < 3)
    rgba[ridge, :3] = TEAL[5]
    # 이빨: 위턱 아래 끝과 아래턱 위 끝을 따라 톱니.
    for x in range(int(hx - 54), int(hx - 6), 6):
        t = (hx - 4 - x) / 52
        top_y = hy + 7 + t * 2
        fang = triangle((x, top_y), (x + 4, top_y), (x + 2, top_y + 5 + (x < hx - 46) * 2))
        rgba[fang, :3] = TEETH[1]
        rgba[fang & (XX > x + 2.5), :3] = TEETH[0]
        rgba[fang, 3] = 255
    for x in range(int(hx - 44), int(hx - 8), 7):
        t = (hx - 2 - x) / 48
        bottom_y = hy + 13 + t * 11
        fang = triangle((x, bottom_y), (x + 4, bottom_y - 0.5), (x + 1, bottom_y - 5))
        rgba[fang, :3] = TEETH[1]
        rgba[fang, 3] = 255
    # 눈썹 뼈와 금빛 눈, 세로 동공.
    brow = triangle((hx - 20, hy - 12), (hx + 6, hy - 18), (hx + 4, hy - 11))
    rgba[brow, :3] = FIN[3]
    rgba[brow, 3] = 255
    ex, ey = hx - 8, hy - 6
    eye = ((XX - ex) / 6) ** 2 + ((YY - ey) / 4) ** 2 <= 1
    rgba[eye, :3] = EYE[1]
    rgba[eye & (YY < ey - 1), :3] = EYE[2]
    rgba[eye & (np.abs(XX - ex) < 1), :3] = OUTLINE
    # 콧구멍.
    rgba[(np.hypot(XX - (hx - 58), YY - (hy - 2)) <= 1.6), :3] = TEAL[0]
    # 수염: 윗입술에서 늘어져 뒤로 흩날린다.
    for start, bend, length in (((hx - 54, hy + 2), 1.0, 70), ((hx - 46, hy + 6), 0.7, 56)):
        for i in range(length):
            t = i / length
            x = start[0] - 8 * np.sin(t * 2.2) + i * 0.55
            y = start[1] + i * 0.75 + np.sin(t * 5) * 6 * bend
            xi, yi = int(round(hx + (x - hx) * HEAD_SCALE)), int(round(hy + (y - hy) * HEAD_SCALE))
            if 0 <= xi < W and 0 <= yi < H:
                color = FIN[3] if t < 0.8 else FIN[4]
                rgba[yi, xi, :3] = color
                rgba[yi, xi, 3] = 255
                if t < 0.4 and yi + 1 < H:
                    rgba[yi + 1, xi, :3] = FIN[2]
                    rgba[yi + 1, xi, 3] = 255


def paint_water(rgba, rng):
    """몸이 물에 닿는 곳마다 물거품 고리와 튀는 물방울. 물 아래로 내려간 몸은 지운다."""
    n = pg.noise(H, W, 5, rng)
    rgba[YY > WATER + 4] = 0
    for cx, half in ((212, 34), (48, 22), (118, 22), (270, 24)):
        ring = (np.abs(XX - cx) <= half + n * 8) & (YY >= WATER - 4 - n * 4) & (YY <= WATER + 6 + n * 3)
        level = np.clip(0.4 + n * 0.7 - np.abs(YY - WATER) / 18, 0, 0.999)
        rgba[ring, :3] = pg.quantize(level, FOAM, dither=0.2)[ring]
        rgba[ring, 3] = 255
        for k in range(12):
            a = rng.uniform(np.pi * 1.05, np.pi * 1.95)
            d = rng.uniform(half * 0.6, half * 1.4)
            x, y = int(cx + np.cos(a) * d), int(WATER - 6 + np.sin(a) * d * 0.55)
            size = 2 if k % 3 else 3
            if 0 <= x < W - size and 0 <= y < H - size:
                rgba[y:y + size, x:x + size, :3] = FOAM[2 if k % 2 else 1]
                rgba[y:y + size, x:x + size, 3] = 255


def build():
    rng = np.random.default_rng(11)
    rgba = np.zeros((H, W, 4), dtype=np.uint8)
    # 배는 몸이 굽은 안쪽(고리는 아래, 목은 앞쪽 왼쪽), 지느러미는 그 반대쪽 등에 돋는다.
    tubes = [(Tube(HUMP, 19, 19, -1), 0.0), (Tube(TAIL, 18, 7, 1), 0.0), (Tube(MAIN, 29, 16, 1), 0.06)]
    for tube, skip in tubes:
        tube.fins(rgba, skip_end=skip)
        body = np.zeros((H, W), dtype=bool)
        u_map = np.zeros((H, W))
        s_map = np.zeros((H, W))
        owner = np.zeros((H, W), dtype=int)
        tube.paint(body, u_map, s_map, owner, 1)
        shade_body(rgba, body, u_map, s_map)
    # 꼬리 끝 지느러미.
    tip = np.array(TAIL[-1], dtype=float)
    fan = triangle(tip + (-8, 2), tip + (6, -26), tip + (16, -6)) | triangle(tip + (-4, 4), tip + (18, -4), tip + (14, 8))
    rgba[fan, :3] = FIN[3]
    rgba[fan & (XX > tip[0] + 8), :3] = FIN[2]
    rgba[fan, 3] = 255
    paint_head(rgba)
    out = pg.outline(rgba, OUTLINE)
    paint_water(out, rng)
    return out


def preview(sprite):
    rng = np.random.default_rng(3)
    sea_h, sea_w = H + 40, W + 60
    yy, xx = np.mgrid[0:sea_h, 0:sea_w]
    n = pg.noise(sea_h, sea_w, 9, rng)
    stripe = np.sin(yy * 0.9 + n * 6) * 0.5 + 0.5
    canvas = pg.quantize(np.clip(0.35 + stripe * 0.2 + n * 0.25, 0, 0.999), SEA, dither=0.2)
    pg.stamp(canvas, sprite, sea_w / 2, sea_h - 20, (W / 2, H))
    return canvas


def main(project):
    sprite = build()
    out = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(sprite), "RGBA").save(out / "sea_dragon.png")
    Image.fromarray(pg.upscale(preview(sprite)), "RGB").save(project / "Docs/ArtUpgrade/PixelGen/sea_dragon_preview.png")
    print(out / "sea_dragon.png")


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
