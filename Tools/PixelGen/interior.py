"""뱀탑 안(사용자 결정 2026-11-02: 뱀성은 여러 층이고 층마다 개성 있는 내부 구조).

탑이 둥글어서 층마다 둥근 방 하나다. RoundRoom이 방 바닥 그림을 그린다: 동심원으로 깐 돌판 바닥, 북쪽 반원의 벽면
(위에서 비스듬히 보면 벽 안쪽 면이 보인다), 남쪽 반원의 벽 윗면, 바깥 어둠. 벽면에는 횃불 받침·화살창·깃발을 걸고,
위층 계단은 벽면에 낸 아치 입구, 아래층 계단은 바닥에 뚫린 계단 구멍이다.
소품(기둥, 이층 침대, 무기 걸이, 허수아비, 긴 식탁, 금화 더미, 빈 보석대, 책장, 보물 상자)은 맵에 세우는 그림이다.
모두 점 격자(한 칸 32점) RGBA와 발밑 기준점(아래 가운데)을 돌려준다.
"""
import numpy as np

import castle
import pixelgen as pg
import serpent_sculpture
from houses import Canvas, IRON, WOOD, WARM, outline

T = pg.TILE
FLOOR = pg.hexes("#2a2624", "#3e3834", "#544c45", "#6a6157", "#80766a", "#968b7d")
VOID = pg.hexes("#0b0a0d")[0]
CARPET = pg.hexes("#3a0e12", "#5c1a1c", "#7e2a26", "#a03c30", "#c8a050")
INLAY = pg.hexes("#14281e", "#1f4030", "#2e5c44", "#c89a3a", "#e8c870")
STAIR_DARK = pg.hexes("#100e10", "#1e1a1c", "#2e2a2a", "#433d3a", "#5a524b")
SKY = pg.hexes("#7fa8d0", "#b8d4ec")
BLANKET = pg.hexes("#2a3a2a", "#3e5640", "#56745a", "#8a8a78")
GOLDEN = pg.hexes("#5a3a0c", "#9a6a18", "#d49a2a", "#f2cc5a", "#fff0b0")
BOOKS = pg.hexes("#6a1e1e", "#1e3e6a", "#2e5a2a", "#6a5a1e", "#4a2a5a", "#7a4a2a")
STRAW = pg.hexes("#5a4418", "#8a6a28", "#b89440", "#d8b860")


class RoundRoom:
    """둥근 탑 한 층의 바닥 그림. 크기와 자리는 칸 단위, 그리는 것은 점 단위."""

    def __init__(self, width, height, center, radius, face=2.6, seed=1):
        self.c = Canvas(width * T, height * T)
        self.cx, self.cy, self.r = center[0] * T, center[1] * T, radius * T
        self.face = int(face * T)
        self.rng = np.random.default_rng(seed)
        yy, xx = self.c.yy + 0.5, self.c.xx + 0.5
        self.distance = np.hypot(xx - self.cx, yy - self.cy)
        self.angle = np.arctan2(yy - self.cy, xx - self.cx)
        self.floor = self.distance <= self.r
        self.c.image[..., :3] = VOID
        self.c.image[..., 3] = 255

    # ── 기본 틀 ──

    def floor_top(self, x_dots):
        """그 열에서 바닥 둥근 가장자리의 북쪽 끝 y(점)."""
        return self.cy - np.sqrt(max(0.0, self.r ** 2 - (x_dots - self.cx) ** 2))

    def paint_floor(self):
        """동심원으로 깐 돌판: 고리마다 판 수가 늘고, 판마다 밝기가 조금 다르다. 이음매는 어둡고 판 윗가장자리는 밝다."""
        ring_w = 34
        ring = (self.distance // ring_w).astype(int)
        count = np.maximum(6, (2 * np.pi * (ring + 0.5) * ring_w / 44).astype(int))
        turn = (self.angle + np.pi) / (2 * np.pi)
        slab = np.floor(turn * count).astype(int)
        shade = (np.sin(ring * 12.9898 + slab * 78.233) * 43758.5453) % 1
        level = 2 + (shade > 0.55) + (shade > 0.85)
        seam = ((self.distance % ring_w) < 2) | (((turn * count) % 1) * (2 * np.pi * (ring + 0.5) * ring_w / count) < 2)
        lit = ((self.distance % ring_w) >= 2) & ((self.distance % ring_w) < 4)
        image = FLOOR[np.where(seam, 1, np.where(lit, np.minimum(level + 1, 5), level))]
        c = self.c
        c.paint(self.floor, image)
        # 벽 가까이는 어둡다(벽 그늘)
        near = self.floor & (self.distance > self.r - 22)
        c.image[near, :3] = (c.image[near, :3] * 0.8).astype(np.uint8)

    def paint_walls(self):
        """북쪽 반원은 벽 안쪽 면(돌 쌓기, 위로 갈수록 어둡다), 둘레는 벽 윗면 띠."""
        c = self.c
        band = (self.distance > self.r) & (self.distance <= self.r + 18)
        c.paint(band, castle.STONE[3])
        c.paint(band & ((np.degrees(self.angle) * 3).astype(int) % 7 == 0), castle.STONE[1])
        c.paint(band & (self.distance <= self.r + 3), castle.STONE[5])
        masonry = castle.masonry(c.size[1], c.size[0], self.rng)
        for x in range(int(self.cx - self.r), int(self.cx + self.r) + 1):
            top = self.floor_top(x + 0.5)
            if top > self.cy:
                continue
            y0, y1 = int(top - self.face), int(top)
            if y1 <= 0:
                continue
            ys = np.arange(max(0, y0), y1)
            c.image[ys, x, :3] = masonry[ys, x]
            fade = np.clip((ys - y0) / self.face, 0, 1)
            c.image[ys, x, :3] = (c.image[ys, x, :3] * (0.55 + 0.45 * fade[:, None])).astype(np.uint8)
            c.image[max(0, y0):max(0, y0) + 3, x, :3] = castle.STONE[5]
            c.image[max(0, y1 - 3):y1, x, :3] = castle.STONE[1]
        # 벽면 바로 앞 바닥에 드리운 그늘
        under = self.floor & (self.c.yy < self.cy) & (self.distance > self.r - 14)
        c.image[under, :3] = (c.image[under, :3] * 0.75).astype(np.uint8)

    # ── 벽면 장식 ──

    def face_y(self, x_tiles):
        """그 칸 열 가운데에서 벽면 아래 끝(바닥과 만나는 곳) y(점)."""
        return self.floor_top(x_tiles * T + T / 2)

    def torch(self, layout, x):
        """벽면의 쇠 횃불 받침. 불꽃은 맵의 움직이는 불(torch)이 얹는다(칸 왼쪽 위에서 (8, 7) 논리 px)."""
        bottom = self.face_y(x)
        row = int((bottom - self.face * 0.55) // T)
        fx, fy = x * T + 16, row * T + 14
        c = self.c
        c.paint(c.rect(fx - 3, fy, fx + 4, fy + 4), IRON[2])
        c.paint(c.rect(fx - 1, fy + 4, fx + 2, fy + 16), IRON[1])
        c.paint(c.rect(fx - 4, fy + 14, fx + 5, fy + 17), IRON[2])
        layout.fire(x, row, "torch")
        layout.light(x * 16 + 8, row * 16 + 7, 44, "#ffab4a", flicker=True)

    def slit_window(self, x):
        """화살창: 벽면 가운데쯤 좁은 틈으로 하늘이 보인다."""
        c = self.c
        bottom = self.face_y(x)
        cx, top = x * T + 16, int(bottom - self.face * 0.78)
        c.paint(c.rect(cx - 5, top - 3, cx + 6, top + 30), castle.STONE[5])
        c.paint(c.arch(cx - 3, top, cx + 4, top + 28), SKY[0])
        c.paint(c.arch(cx - 3, top, cx + 4, top + 28) & (c.xx < cx), SKY[1])

    def banner(self, x):
        castle.flag_banner(self.c, x * T + 9, int(self.face_y(x) - self.face + 10), 44)

    def shield(self, x):
        """벽에 건 둥근 방패: 엇갈린 창 둘 위에 청록 바탕, 금테와 뱀 문장."""
        c = self.c
        cx, cy = x * T + 16, int(self.face_y(x) - self.face * 0.5)
        for side in (-1, 1):
            c.line(cx - side * 20, cy + 20, cx + side * 20, cy - 22, WOOD[2], 2)
            c.paint(c.polygon([(cx + side * 20 - 3, cy - 20), (cx + side * 20 + 3, cy - 20), (cx + side * 22, cy - 28)]), IRON[4])
        c.paint(c.ellipse(cx, cy, 15, 15), castle.GOLD[1])
        c.paint(c.ellipse(cx, cy, 13, 13), castle.BANNER[2])
        c.paint(c.ellipse(cx, cy, 13, 13) & (c.xx < cx) & (c.yy < cy), castle.BANNER[3])
        castle.serpent_emblem(c, cx, cy, 7)

    def tapestry(self, x, columns=3):
        """벽에 건 큰 걸개: 금테 두른 녹색 천에 똬리 튼 뱀 문양, 아래는 술 장식. x는 가운데(칸)."""
        c = self.c
        cx = int(x * T)
        w = columns * T - 12
        top = int(self.face_y(x) - self.face + 6)
        bottom = int(self.face_y(x) - 8)
        c.paint(c.rect(cx - w // 2 - 4, top - 3, cx + w // 2 + 4, top + 1), WOOD[2])
        cloth = c.rect(cx - w // 2, top, cx + w // 2, bottom)
        c.paint(cloth, castle.BANNER[1])
        c.paint(cloth & (c.xx < cx - w // 2 + 6), castle.BANNER[2])
        c.paint(cloth & ~c.rect(cx - w // 2 + 3, top + 3, cx + w // 2 - 3, bottom - 3), castle.GOLD[2])
        cy, r = (top + bottom) // 2, min(w, bottom - top) * 0.32
        d = np.hypot(c.xx + 0.5 - cx, c.yy + 0.5 - cy)
        a = np.arctan2(c.yy + 0.5 - cy, c.xx + 0.5 - cx)
        coil = (d < r) & (np.abs(((d / r) * 2.4 - a / (2 * np.pi)) % 1 - 0.5) < 0.12)
        c.paint(coil, castle.GOLD[2])
        c.paint(c.ellipse(cx, cy, r * 0.22, r * 0.22), castle.GOLD[3])
        for fx in range(cx - w // 2, cx + w // 2, 3):
            c.paint(c.rect(fx, bottom, fx + 1, bottom + 4), castle.GOLD[1])

    def painting(self, x, rng):
        """벽에 건 금테 그림: 노을 하늘 아래 산꼭대기 뱀탑."""
        c = self.c
        cx, cy = x * T + 16, int(self.face_y(x) - self.face * 0.52)
        w, h = 30, 22
        c.paint(c.rect(cx - w // 2 - 3, cy - h // 2 - 3, cx + w // 2 + 3, cy + h // 2 + 3), GOLDEN[2])
        c.paint(c.rect(cx - w // 2 - 3, cy - h // 2 - 3, cx + w // 2 + 3, cy - h // 2 - 2), GOLDEN[4])
        c.paint(c.rect(cx - w // 2 - 3, cy + h // 2 + 2, cx + w // 2 + 3, cy + h // 2 + 3), GOLDEN[0])
        canvas = c.rect(cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2)
        c.paint(canvas, pg.hexes("#e8a060")[0])
        c.paint(canvas & (c.yy < cy - 3), pg.hexes("#c86a50")[0])
        c.paint(canvas & (c.yy < cy - 7), pg.hexes("#6a4a7a")[0])
        hill = canvas & (c.yy > cy + 3 - np.abs(c.xx - cx) * 0.25)
        c.paint(hill, pg.hexes("#3a4a3a")[0])
        c.paint(canvas & (np.abs(c.xx - cx) < 3) & (c.yy > cy - 6), castle.STONE[2])
        c.paint(canvas & (np.abs(c.xx - cx) < 1) & (c.yy > cy - 9) & (c.yy <= cy - 6), castle.STONE[1])

    def emblem(self, x):
        castle.serpent_emblem(self.c, x * T + 16, int(self.face_y(x) - self.face * 0.55), 12)

    def doorway(self, x):
        """
        위층으로 오르는 아치 입구: 벽면을 뚫은 어두운 아치 안으로 돌계단이 올라가며 어둠에 잠긴다.
        돌려주는 값: 아치 바로 아래 바닥 칸들(위층으로 가는 포탈 칸).
        """
        c = self.c
        cx = x * T
        bottom = int(min(self.floor_top(cx - 20), self.floor_top(cx + 20))) + 2
        top = bottom - int(self.face * 0.86)
        frame = c.arch(cx - 30, top - 6, cx + 30, bottom)
        c.paint(frame, castle.STONE[5])
        c.paint(frame & (c.xx < cx - 26), castle.STONE[6])
        c.paint(c.rect(cx - 5, top - 8, cx + 5, top + 2), castle.STONE[6])
        hole = c.arch(cx - 24, top, cx + 24, bottom)
        for y in range(top, bottom):
            depth = (bottom - y) / (bottom - top)
            row = hole & (c.yy == y)
            step = (bottom - y) % 7
            level = 4 if step == 0 else 3 if step < 3 else 2
            c.paint(row, STAIR_DARK[max(0, level - int(depth * 4))])
        row = int(bottom // T)
        return [(x - 1, row), (x, row)]

    def stairwell(self, x, y):
        """아래층으로 내려가는 계단 구멍(2×2칸): 낮은 돌 난간 안에서 계단이 아래로 어두워진다. 돌려주는 값: 막을 칸들."""
        c = self.c
        x0, y0 = x * T, y * T
        rail = c.rect(x0 - 4, y0 - 4, x0 + 2 * T + 4, y0 + 2 * T + 4)
        c.paint(rail, castle.STONE[4])
        c.paint(rail & (c.yy < y0 - 2), castle.STONE[6])
        hole = c.rect(x0, y0, x0 + 2 * T, y0 + 2 * T)
        for k in range(0, 2 * T, 6):
            band = hole & (c.yy >= y0 + k) & (c.yy < y0 + k + 6)
            level = max(0, 4 - k // 12)
            c.paint(band, STAIR_DARK[level])
            c.paint(band & (c.yy == y0 + k), STAIR_DARK[min(4, level + 1)])
        return [(x + dx, y + dy) for dx in range(2) for dy in range(2)]

    def entrance(self, x):
        """1층 남쪽 출입문: 벽 윗면 띠를 끊고 문지방 돌과 양옆 문기둥. 돌려주는 값: 문 안쪽 칸들."""
        c = self.c
        cx = x * T
        bottom = int(self.cy + self.r)
        sill = c.rect(cx - 30, bottom - 10, cx + 30, bottom + 20)
        c.paint(sill, castle.STONE[4])
        c.paint(sill & (c.yy % 6 == 0), castle.STONE[2])
        for px in (cx - 34, cx + 28):
            c.paint(c.rect(px, bottom - 14, px + 6, bottom + 20), castle.STONE[6])
            c.paint(c.rect(px + 5, bottom - 14, px + 6, bottom + 20), castle.STONE[2])
        row = int((bottom - 4) // T)
        return [(x - 1, row), (x, row)]

    # ── 바닥 장식 ──

    def carpet(self, x0, y0, x1, y1):
        """붉은 양탄자와 금실 테두리(칸 단위 네모)."""
        c = self.c
        area = c.rect(x0 * T, y0 * T, x1 * T, y1 * T) & self.floor
        c.paint(area, CARPET[2])
        c.paint(area & ((c.xx + c.yy) % 9 == 0), CARPET[1])
        inner = c.rect(x0 * T + 4, y0 * T, x1 * T - 4, y1 * T)
        c.paint(area & ~inner, CARPET[4])
        c.paint(area & ~c.rect(x0 * T + 2, y0 * T, x1 * T - 2, y1 * T), CARPET[0])

    def mosaic(self, x, y, radius):
        """바닥 가운데 돌뱀 모자이크: 금빛 테두리 원 안에 똬리 튼 뱀 문양."""
        c = self.c
        mx, my, r = x * T, y * T, radius * T
        d = np.hypot(c.xx + 0.5 - mx, c.yy + 0.5 - my)
        a = np.arctan2(c.yy + 0.5 - my, c.xx + 0.5 - mx)
        disc = d <= r
        c.paint(disc, INLAY[1])
        c.paint(disc & ((c.xx // 4 + c.yy // 4) % 2 == 0), INLAY[0])
        spiral = disc & (np.abs(((d / r) * 3.2 - a / (2 * np.pi)) % 1 - 0.5) < 0.16) & (d > r * 0.12)
        c.paint(spiral, INLAY[2])
        c.paint(spiral & (np.abs(((d / r) * 3.2 - a / (2 * np.pi)) % 1 - 0.5) < 0.05), INLAY[3])
        c.paint(c.ellipse(mx, my, r * 0.16, r * 0.16), INLAY[4])
        c.paint((d > r - 5) & (d <= r), INLAY[3])
        c.paint((d > r) & (d <= r + 2), FLOOR[1])

    def glow(self, x, y, radius, color):
        """천장에서 내려온 빛기둥이 바닥에 비춘 둥근 빛(바닥색을 밝힌다)."""
        c = self.c
        d = np.hypot(c.xx + 0.5 - x * T, c.yy + 0.5 - y * T) / (radius * T)
        lit = (d < 1) & self.floor
        mix = np.clip(1 - d, 0, 1)[lit][:, None] * 0.5
        c.image[lit, :3] = (c.image[lit, :3] * (1 - mix) + np.array(color) * mix).astype(np.uint8)

    def serpent_breach(self, points, radius):
        """벽을 뚫고 방 안으로 들어온 돌뱀 몸통(바닥에 누운 조각). 들어온 자리 둘레에 깨진 벽돌 조각."""
        c = self.c
        castle.serpent_tube(c, points, radius)
        for x, y, r in (points[0], points[-1]):
            for _ in range(26):
                rx, ry = x + self.rng.normal(0, r * 0.9), y + self.rng.normal(0, r * 0.6)
                size = int(self.rng.integers(3, 7))
                chunk = c.rect(int(rx), int(ry), int(rx) + size, int(ry) + size - 1) & (self.distance <= self.r + 18)
                c.paint(chunk, castle.STONE[int(self.rng.integers(2, 6))])

    def ground(self):
        return self.c.image[..., :3].copy()

    def blocked_cells(self, margin=0.45):
        """바닥 둥근 가장자리 밖(가장자리에서 margin칸 안쪽까지)의 칸."""
        height, width = self.c.size[1] // T, self.c.size[0] // T
        yy, xx = np.mgrid[0:height, 0:width]
        d = np.hypot((xx + 0.5) * T - self.cx, (yy + 0.5) * T - self.cy) / T
        return d > self.r / T - margin


# ── 소품 ───────────────────────────────────────────────

def pillar(rng, height=104):
    """돌기둥: 넓은 받침과 머리, 둥근 몸통에 감아 오르는 뱀 홈."""
    w = 34
    c = Canvas(w, height)
    cx, ground = w // 2, height - 2
    castle.cylinder(c, 6, w - 6, 14, ground - 10, rng)
    for k in range(0, height, 2):
        y = ground - 14 - k
        x = cx + np.sin(k / 9) * 9
        if 18 < y < ground - 12:
            c.paint(c.rect(int(x) - 1, y, int(x) + 2, y + 2), castle.STONE[1])
    for y0, y1 in ((ground - 12, ground), (2, 16)):
        cap = c.rect(1, y0, w - 1, y1)
        c.paint(cap, castle.STONE[4])
        c.paint(cap & (c.yy == y0), castle.STONE[6])
        c.paint(cap & (c.yy == y1 - 1), castle.STONE[2])
    return outline(c.image), (cx, ground)


def bunk_bed(rng):
    """이층 침대: 나무 틀, 담요 두 장, 베개, 옆 사다리."""
    w, h = 66, 78
    c = Canvas(w, h)
    ground = h - 2
    for x in (2, w - 8):
        c.paint(c.rect(x, 4, x + 6, ground), WOOD[2])
        c.paint(c.rect(x, 4, x + 2, ground), WOOD[4])
    for top in (14, 46):
        c.paint(c.rect(8, top, w - 8, top + 16), BLANKET[1])
        c.paint(c.rect(8, top, w - 8, top + 4), BLANKET[2])
        c.paint(c.rect(10, top + 2, 22, top + 8), BLANKET[3])
        c.paint(c.rect(8, top + 16, w - 8, top + 20), WOOD[3])
        c.paint(c.rect(8, top + 19, w - 8, top + 20), WOOD[1])
    for y in range(20, ground - 4, 8):
        c.paint(c.rect(w - 14, y, w - 8, y + 2), WOOD[4])
    return outline(c.image), (w // 2, ground)


def weapon_rack(rng):
    """무기 걸이: 나무 틀에 창 셋과 칼 둘, 아래에 둥근 방패 하나."""
    w, h = 66, 92
    c = Canvas(w, h)
    ground = h - 2
    c.paint(c.rect(4, 20, w - 4, 26), WOOD[3])
    c.paint(c.rect(4, ground - 22, w - 4, ground - 16), WOOD[3])
    for x in (4, w - 10):
        c.paint(c.rect(x, 14, x + 6, ground), WOOD[2])
    for k, x in enumerate((14, 24, 34)):
        c.paint(c.rect(x, 4, x + 2, ground - 6), WOOD[4])
        c.paint(c.polygon([(x - 3, 10), (x + 5, 10), (x + 1, 0)]), IRON[4])
    for x in (44, 52):
        c.paint(c.rect(x, 8, x + 3, 52), IRON[4])
        c.paint(c.rect(x + 2, 8, x + 3, 52), IRON[2])
        c.paint(c.rect(x - 3, 52, x + 6, 55), WARM[1])
        c.paint(c.rect(x, 55, x + 3, 64), WOOD[1])
    shield = c.ellipse(46, ground - 14, 13, 12)
    c.paint(shield, castle.BANNER[2])
    c.paint(shield & ~c.ellipse(46, ground - 14, 10, 9), IRON[3])
    c.paint(c.ellipse(46, ground - 14, 3, 3), castle.GOLD[3])
    return outline(c.image), (w // 2, ground)


def dummy(rng):
    """허수아비 과녁: 나무 기둥에 짚 몸통, 가로 팔, 자루 머리, 꽂힌 화살 하나."""
    w, h = 40, 76
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(cx - 2, 30, cx + 3, ground), WOOD[2])
    c.paint(c.rect(4, 30, w - 4, 35), WOOD[3])
    body = c.ellipse(cx, 44, 11, 15)
    c.paint(body, STRAW[2])
    c.paint(body & ((c.yy % 4) == 0), STRAW[1])
    c.paint(body & (c.xx < cx - 4), STRAW[3])
    head = c.ellipse(cx, 18, 9, 10)
    c.paint(head, pg.hexes("#c8b48a")[0])
    c.paint(head & (c.xx > cx + 3), pg.hexes("#9a8862")[0])
    c.paint(c.rect(cx - 9, 24, cx + 10, 27), STRAW[0])
    c.line(cx + 3, 42, cx + 14, 36, WOOD[4], 1)
    c.paint(c.rect(cx + 13, 34, cx + 16, 37), pg.hexes("#d8d0c0")[0])
    return outline(c.image), (cx, ground)


def long_table(rng, columns=4):
    """긴 식탁과 양쪽 긴 의자, 위에 잔·접시·빵."""
    w, h = columns * T, 56
    c = Canvas(w, h)
    ground = h - 2
    c.paint(c.rect(2, ground - 10, w - 2, ground - 4), WOOD[2])
    c.paint(c.rect(2, ground - 10, w - 2, ground - 9), WOOD[4])
    c.paint(c.rect(4, 16, w - 4, 34), WOOD[3])
    c.paint(c.rect(4, 16, w - 4, 18), WOOD[5])
    c.paint(c.rect(4, 34, w - 4, 38), WOOD[1])
    for x in (8, w - 12):
        c.paint(c.rect(x, 38, x + 4, ground - 10), WOOD[1])
    c.paint(c.rect(2, 4, w - 2, 10), WOOD[2])
    for k, x in enumerate(range(14, w - 14, 18)):
        if k % 2 == 0:
            c.paint(c.rect(x, 20, x + 5, 27), IRON[3])
            c.paint(c.rect(x + 1, 20, x + 4, 22), WARM[2])
        else:
            c.paint(c.ellipse(x + 3, 26, 6, 3), pg.hexes("#d8d0c0")[0])
            c.paint(c.ellipse(x + 3, 25, 3, 2), WARM[1])
    return outline(c.image), (w // 2, ground)


def gold_pile(rng):
    """금화 더미: 둥근 무더기에 동전 반짝임, 보석 몇 알."""
    w, h = 54, 34
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    pile = c.ellipse(cx, ground - 8, 25, 12) | c.ellipse(cx - 6, ground - 14, 14, 10) | c.ellipse(cx + 8, ground - 12, 12, 8)
    c.paint(pile, GOLDEN[2])
    c.paint(pile & (c.yy > ground - 6), GOLDEN[1])
    c.paint(pile & ((c.xx * 3 + c.yy * 5) % 11 == 0), GOLDEN[3])
    c.paint(pile & ((c.xx * 7 + c.yy * 3) % 23 == 0), GOLDEN[4])
    for gx, gy, col in ((cx - 8, ground - 14, "#c02030"), (cx + 9, ground - 10, "#2a60c0"), (cx + 2, ground - 18, "#30a050")):
        c.paint(c.rect(gx, gy, gx + 3, gy + 3), pg.hexes(col)[0])
    return outline(c.image), (cx, ground)


def treasure_chest(rng):
    """보물 상자: 둥근 뚜껑, 금빛 띠와 자물쇠."""
    w, h = 44, 38
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    body = c.rect(3, 16, w - 3, ground)
    lid = c.ellipse(cx, 16, w / 2 - 3, 12) & (c.yy < 17)
    c.paint(body | lid, WOOD[3])
    c.paint(lid & (c.yy < 9), WOOD[4])
    c.paint(c.rect(3, 15, w - 3, 18), GOLDEN[2])
    for x in (8, w - 11):
        c.paint(c.rect(x, 5, x + 3, ground), GOLDEN[2])
    c.paint(c.rect(cx - 3, 15, cx + 3, 23), GOLDEN[3])
    c.paint(c.rect(cx - 1, 19, cx + 1, 21), WOOD[0])
    return outline(c.image), (cx, ground)


def bookshelf(rng):
    """책장: 네 칸 선반에 색색 책과 두루마리."""
    w, h = 66, 100
    c = Canvas(w, h)
    ground = h - 2
    c.paint(c.rect(2, 2, w - 2, ground), WOOD[1])
    c.paint(c.rect(2, 2, w - 2, 6), WOOD[3])
    for k in range(4):
        y0 = 8 + k * 22
        x = 6
        while x < w - 8:
            bw = int(rng.integers(4, 8))
            bh = int(rng.integers(13, 19))
            if rng.random() < 0.12:
                c.paint(c.ellipse(x + 5, y0 + 16, 5, 3), pg.hexes("#e8dcc0")[0])
                x += 11
                continue
            c.paint(c.rect(x, y0 + 19 - bh, x + bw, y0 + 19), BOOKS[int(rng.integers(0, len(BOOKS)))])
            c.paint(c.rect(x, y0 + 21 - bh, x + bw, y0 + 22 - bh), WARM[2])
            x += bw + 1
        c.paint(c.rect(2, y0 + 19, w - 2, y0 + 22), WOOD[3])
    return outline(c.image), (w // 2, ground)


def armor_stand(rng):
    """갑옷 걸이: 나무 십자 받침에 쇠 투구와 가슴판, 어깨받이, 금빛 뱀을 수놓은 녹색 겉옷."""
    w, h = 44, 86
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(cx - 12, ground - 5, cx + 12, ground), WOOD[2])
    c.paint(c.rect(cx - 12, ground - 5, cx + 12, ground - 4), WOOD[4])
    c.paint(c.rect(cx - 2, 30, cx + 2, ground - 4), WOOD[3])
    chest = c.polygon([(cx - 14, 30), (cx + 14, 30), (cx + 11, 58), (cx - 11, 58)])
    c.paint(chest, IRON[2])
    c.paint(chest & (c.xx < cx - 2), IRON[3])
    c.paint(chest & (c.xx < cx - 8), IRON[4])
    for y in (38, 46):
        c.paint(chest & (c.yy == y), IRON[1])
    for side in (-1, 1):
        pad = c.ellipse(cx + side * 15, 32, 7, 5)
        c.paint(pad, IRON[3])
        c.paint(pad & (c.yy > 33), IRON[1])
    tabard = c.rect(cx - 8, 50, cx + 8, 74)
    c.paint(tabard, castle.BANNER[2])
    c.paint(tabard & (c.xx < cx - 4), castle.BANNER[3])
    c.paint(c.polygon([(cx - 8, 74), (cx + 8, 74), (cx, 80)]), castle.BANNER[1])
    for k in range(12):
        sx = cx + np.sin(k / 11 * np.pi * 2) * 3
        c.paint(c.rect(int(sx) - 1, 56 + k, int(sx) + 1, 57 + k), castle.GOLD[2])
    helm = c.ellipse(cx, 18, 10, 11) & (c.yy < 26)
    c.paint(helm, IRON[3])
    c.paint(helm & (c.xx < cx - 3), IRON[4])
    c.paint(c.rect(cx - 7, 18, cx + 8, 20), IRON[0])
    c.paint(c.rect(cx - 1, 8, cx + 1, 26), IRON[2])
    c.paint(c.ellipse(cx, 6, 3, 4), castle.BANNER[3])
    return outline(c.image), (cx, ground)


def footlocker(rng):
    """침대 발치 사물함: 나무 궤짝에 쇠 띠 둘과 자물쇠."""
    w, h = 34, 26
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(2, 6, w - 2, ground), WOOD[3])
    c.paint(c.rect(2, 6, w - 2, 10), WOOD[4])
    c.paint(c.rect(2, 10, w - 2, 11), WOOD[1])
    for x in (7, w - 10):
        c.paint(c.rect(x, 6, x + 3, ground), IRON[2])
    c.paint(c.rect(cx - 2, 11, cx + 2, 16), IRON[4])
    return outline(c.image), (cx, ground)


def weapon_barrel(rng):
    """무기 통: 나무 통에 창 자루와 칼자루가 비스듬히 꽂혀 있다."""
    import objects
    body, pivot = objects.barrel(rng)
    w, h = 34, body.shape[0] + 34
    c = Canvas(w, h)
    for x0, lean, tip in ((8, -5, True), (15, 2, False), (22, 6, True), (18, -2, False)):
        c.line(x0, 40, x0 + lean, 4, WOOD[2] if tip else IRON[3], 2)
        if tip:
            c.paint(c.polygon([(x0 + lean - 3, 8), (x0 + lean + 3, 8), (x0 + lean, 0)]), IRON[4])
        else:
            c.paint(c.rect(x0 + lean - 4, 10, x0 + lean + 5, 13), castle.GOLD[1])
    top = h - body.shape[0]
    left = (w - body.shape[1]) // 2
    opaque = body[..., 3] > 0
    c.image[top:top + body.shape[0], left:left + body.shape[1]][opaque] = body[opaque]
    return outline(c.image), (w // 2, h - 1)


def archery_target(rng):
    """활쏘기 과녁: 나무 세 발 받침에 짚 과녁(붉은·흰 고리), 꽂힌 화살 둘."""
    w, h = 44, 62
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for dx in (-14, 0, 14):
        c.line(cx + dx * 0.3, 26, cx + dx, ground, WOOD[2], 2)
    face = c.ellipse(cx, 24, 17, 17)
    c.paint(face, STRAW[2])
    for r, color in ((14, pg.hexes("#f0ece0")[0]), (11, pg.hexes("#b8322a")[0]), (8, pg.hexes("#f0ece0")[0]),
                     (5, pg.hexes("#b8322a")[0]), (2.5, castle.GOLD[3])):
        c.paint(c.ellipse(cx, 24, r, r), color)
    c.paint(face & ~c.ellipse(cx, 24, 15, 15), STRAW[0])
    for ax, ay in ((cx + 4, 20), (cx - 6, 28)):
        c.line(ax, ay, ax + 9, ay - 6, WOOD[4], 1)
        c.paint(c.rect(ax + 8, ay - 8, ax + 11, ay - 5), pg.hexes("#e8e0d0")[0])
    return outline(c.image), (cx, ground)


def notice_board(rng):
    """근무표 게시판: 모험가 길드 의뢰 게시판과 같은 나무판(houses.quest_board)."""
    from houses import quest_board
    c = Canvas(48, 62)
    quest_board(c, 4, 61, rng)
    return outline(c.image), (24, 61)


def display_case(rng):
    """유리 진열장: 금테 두른 나무 받침 위 유리 상자 안에 금관과 금잔, 보석 하나."""
    w, h = 46, 66
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(3, ground - 22, w - 3, ground), WOOD[2])
    c.paint(c.rect(3, ground - 22, w - 3, ground - 20), GOLDEN[2])
    c.paint(c.rect(6, ground - 16, w - 6, ground - 4), WOOD[1])
    glass = c.rect(5, 6, w - 5, ground - 22)
    c.paint(glass, pg.hexes("#3a5468")[0])
    c.paint(glass & (c.yy > ground - 34), pg.hexes("#2e4456")[0])
    # 금관
    crown = c.rect(cx - 12, 24, cx - 1, 31)
    c.paint(crown, GOLDEN[2])
    for k in range(3):
        c.paint(c.polygon([(cx - 12 + k * 4, 24), (cx - 9 + k * 4, 24), (cx - 10.5 + k * 4, 18)]), GOLDEN[3])
    c.paint(c.rect(cx - 8, 26, cx - 5, 29), pg.hexes("#c02030")[0])
    # 금잔
    cup = c.ellipse(cx + 8, 22, 6, 4) | c.rect(cx + 7, 22, cx + 10, 30) | c.rect(cx + 4, 30, cx + 13, 32)
    c.paint(cup, GOLDEN[2])
    c.paint(cup & (c.xx < cx + 7), GOLDEN[3])
    c.paint(c.rect(cx - 2, ground - 28, cx + 2, ground - 24), pg.hexes("#3080e0")[0])
    for k in range(4):
        c.paint(glass & ((c.xx - c.yy + k) == 8), pg.hexes("#a8c8e0")[0])
    c.paint(c.rect(5, 4, w - 5, 7), GOLDEN[2])
    for x in (5, w - 7):
        c.paint(c.rect(x, 6, x + 2, ground - 22), GOLDEN[1])
    return outline(c.image), (cx, ground)


def gold_bars(rng):
    """금괴 더미: 세 단으로 쌓은 금괴, 윗면이 밝게 빛난다."""
    w, h = 48, 30
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for row, count in enumerate((4, 3, 2)):
        y = ground - 8 - row * 7
        x0 = cx - count * 6
        for k in range(count):
            x = x0 + k * 12
            bar = c.polygon([(x + 1, y), (x + 11, y), (x + 12, y + 7), (x, y + 7)])
            c.paint(bar, GOLDEN[2])
            c.paint(bar & (c.yy < y + 3), GOLDEN[3])
            c.paint(bar & (c.yy == y), GOLDEN[4])
            c.paint(bar & (c.xx >= x + 10), GOLDEN[1])
    return outline(c.image), (cx, ground)


def open_chest(rng):
    """열린 보물 상자: 젖혀진 뚜껑 안쪽, 넘쳐흐르는 금화, 걸쳐 나온 진주 목걸이."""
    w, h = 50, 50
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    lid = c.rect(5, 6, w - 5, 22)
    c.paint(lid, WOOD[2])
    c.paint(lid & (c.yy > 16), WOOD[1])
    c.paint(c.rect(5, 6, w - 5, 8), GOLDEN[2])
    body = c.rect(4, 26, w - 4, ground)
    c.paint(body, WOOD[3])
    c.paint(c.rect(4, 26, w - 4, 29), GOLDEN[2])
    for x in (9, w - 12):
        c.paint(c.rect(x, 26, x + 3, ground), GOLDEN[2])
    heap = c.ellipse(cx, 26, 20, 8)
    c.paint(heap, GOLDEN[2])
    c.paint(heap & ((c.xx * 3 + c.yy * 5) % 9 == 0), GOLDEN[3])
    c.paint(heap & ((c.xx * 7 + c.yy * 3) % 19 == 0), GOLDEN[4])
    for k in range(9):
        c.paint(c.ellipse(w - 9 + np.sin(k * 0.7) * 3, 28 + k * 2, 1.4, 1.4), pg.hexes("#f4f0e8")[0])
    for k in range(6):
        sx, sy = int(rng.integers(2, w - 4)), int(rng.integers(ground - 4, ground))
        c.paint(c.rect(sx, sy, sx + 3, sy + 2), GOLDEN[3])
    return outline(c.image), (cx, ground)


def urn(rng):
    """장식 항아리: 청록 유약에 금빛 띠, 좁은 목과 넓은 몸통."""
    w, h = 28, 44
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    body = c.ellipse(cx, 26, 12, 15) | c.rect(cx - 5, 4, cx + 5, 14) | c.ellipse(cx, 4, 7, 3)
    c.paint(body, castle.BANNER[2])
    c.paint(body & (c.xx < cx - 4), castle.BANNER[3])
    c.paint(body & (c.xx > cx + 6), castle.BANNER[1])
    for y in (14, 26, 36):
        c.paint(body & (c.yy >= y) & (c.yy < y + 2), GOLDEN[2])
    c.paint(c.rect(cx - 6, ground - 3, cx + 6, ground), GOLDEN[1])
    return outline(c.image), (cx, ground)


def rope_barrier(rng, columns=6):
    """벨벳 줄 차단봉: 세 칸마다 놋쇠 기둥, 기둥 사이에 처진 붉은 줄."""
    w, h = columns * T, 34
    c = Canvas(w, h)
    ground = h - 2
    posts = [int(x) for x in np.linspace(5, w - 5, columns // 3 + 1)]
    for left, right in zip(posts, posts[1:]):
        xs = np.arange(left, right)
        sag = 8 + np.sin((xs - left) / (right - left) * np.pi) * 8
        for x, y in zip(xs, sag):
            c.paint(c.rect(int(x), int(y), int(x) + 1, int(y) + 3), CARPET[3])
            c.paint(c.rect(int(x), int(y) + 2, int(x) + 1, int(y) + 3), CARPET[1])
    for x in posts:
        c.paint(c.rect(x - 1, 6, x + 1, ground - 2), GOLDEN[2])
        c.paint(c.ellipse(x, 5, 3, 3), GOLDEN[3])
        c.paint(c.ellipse(x, ground - 1, 4, 2), GOLDEN[1])
    return outline(c.image), (w // 2, ground)


def counting_desk(rng):
    """보물 관리인의 책상: 장부, 깃펜, 동전 더미, 작은 천칭."""
    w, h = 66, 48
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(3, 16, w - 3, 30), WOOD[3])
    c.paint(c.rect(3, 16, w - 3, 18), WOOD[5])
    c.paint(c.rect(3, 30, w - 3, 33), WOOD[1])
    for x in (6, w - 10):
        c.paint(c.rect(x, 33, x + 4, ground), WOOD[2])
    c.paint(c.rect(8, 18, 26, 27), pg.hexes("#efe6cc")[0])
    c.paint(c.rect(16, 18, 18, 27), pg.hexes("#c8b88a")[0])
    for y in range(20, 26, 2):
        c.paint(c.rect(10, y, 15, y + 1), WOOD[1])
        c.paint(c.rect(19, y, 24, y + 1), WOOD[1])
    c.line(26, 26, 32, 10, pg.hexes("#f4f0e8")[0], 1)
    for k, x in enumerate((36, 42)):
        for level in range(3 + k):
            c.paint(c.rect(x, 24 - level * 2, x + 5, 26 - level * 2), GOLDEN[2 + (level % 2)])
    c.paint(c.rect(53, 8, 55, 25), GOLDEN[1])
    c.paint(c.rect(47, 9, 61, 10), GOLDEN[2])
    for px in (48, 60):
        c.paint(c.ellipse(px, 14, 4, 1.5), GOLDEN[3])
    return outline(c.image), (cx, ground)


def stone_bench(rng, columns=2):
    """돌 의자: 두 다리 위 돌판, 붉은 방석 하나."""
    w, h = columns * T, 30
    c = Canvas(w, h)
    ground = h - 2
    for x in (4, w - 12):
        c.paint(c.rect(x, 14, x + 8, ground), castle.STONE[3])
        c.paint(c.rect(x, 14, x + 2, ground), castle.STONE[5])
    seat = c.rect(1, 8, w - 1, 16)
    c.paint(seat, castle.STONE[4])
    c.paint(seat & (c.yy < 10), castle.STONE[6])
    c.paint(seat & (c.yy == 15), castle.STONE[2])
    c.paint(c.rect(6, 4, w // 2, 9), CARPET[2])
    c.paint(c.rect(6, 4, w // 2, 5), CARPET[3])
    return outline(c.image), (w // 2, ground)


def planter(rng):
    """큰 화분: 청록 유약 항아리에 늘어진 양치 잎."""
    w, h = 44, 62
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    pot = c.ellipse(cx, ground - 12, 13, 11) | c.rect(cx - 12, ground - 26, cx + 12, ground - 12)
    c.paint(pot, castle.BANNER[2])
    c.paint(pot & (c.xx < cx - 5), castle.BANNER[3])
    c.paint(pot & (c.xx > cx + 7), castle.BANNER[1])
    c.paint(c.rect(cx - 14, ground - 28, cx + 14, ground - 24), castle.GOLD[1])
    leaf = pg.hexes("#1e3a22", "#2e5a30", "#46803c", "#6aa84a")
    for k in range(9):
        angle = -np.pi / 2 + (k - 4) * 0.36
        length = 22 - abs(k - 4) * 2
        for s in np.linspace(0, 1, 30):
            x = cx + np.cos(angle) * length * s
            y = ground - 28 + np.sin(angle) * length * s + (s ** 2) * 10 * abs(np.cos(angle))
            size = 3 - s * 1.5
            c.paint(c.ellipse(x, y, size, size), leaf[1 + int(s * 2.9)])
    return outline(c.image), (cx, ground)


def banner_stand(rng):
    """깃발대: 세 발 받침의 쇠 기둥에 뱀 문장 깃발."""
    w, h = 32, 92
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for dx in (-10, 0, 10):
        c.line(cx, ground - 10, cx + dx, ground, IRON[2], 2)
    c.paint(c.rect(cx - 1, 6, cx + 1, ground - 8), IRON[3])
    c.paint(c.ellipse(cx, 5, 3, 3), castle.GOLD[3])
    castle.flag_banner(c, cx - 7, 12, 50)
    return outline(c.image), (cx, ground)


def altar(rng):
    """제단: 뱀 문양을 새긴 돌 상 위에 제물 그릇과 촛불 둘."""
    w, h = 70, 46
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    body = c.rect(6, 16, w - 6, ground)
    c.paint(body, castle.STONE[3])
    c.paint(body & (c.xx < 10), castle.STONE[5])
    c.paint(c.rect(2, 12, w - 2, 18), castle.STONE[5])
    c.paint(c.rect(2, 12, w - 2, 13), castle.STONE[7])
    castle.serpent_emblem(c, cx, ground - 12, 7)
    bowl = c.ellipse(cx, 11, 9, 4)
    c.paint(bowl, castle.GOLD[1])
    c.paint(c.ellipse(cx, 10, 7, 2), pg.hexes("#8a2018")[0])
    for x in (12, w - 14):
        c.paint(c.rect(x - 2, 2, x + 2, 12), pg.hexes("#f2ead6")[0])
        c.paint(c.ellipse(x, 0, 2, 3), WARM[3])
    return outline(c.image), (cx, ground)


def candelabra(rng):
    """금빛 촛대: 세 가지 받침에 켜진 초."""
    w, h = 30, 60
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.ellipse(cx, ground - 2, 8, 3), GOLDEN[1])
    c.paint(c.rect(cx - 1, 22, cx + 2, ground - 2), GOLDEN[2])
    c.paint(c.rect(cx - 11, 22, cx + 12, 24), GOLDEN[2])
    for x in (cx - 11, cx, cx + 10):
        c.paint(c.rect(x - 1, 14, x + 2, 22), pg.hexes("#f2ead6")[0])
        c.paint(c.ellipse(x + 0.5, 10, 2, 3.5), WARM[3])
        c.paint(c.ellipse(x + 0.5, 11, 1, 2), WARM[4])
    return outline(c.image), (cx, ground)


def jewel_pedestal(rng):
    """
    빈 보석대: 돌 받침 위 붉은 방석은 비었고, 씌워 두었던 유리 덮개는 깨져 조각이 흩어졌다(루나가 훔쳐 갔다).
    """
    w, h = 56, 74
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(cx - 20, ground - 12, cx + 20, ground), castle.STONE[4])
    c.paint(c.rect(cx - 20, ground - 12, cx + 20, ground - 10), castle.STONE[6])
    castle.cylinder(c, cx - 11, cx + 11, ground - 44, ground - 12, rng)
    c.paint(c.rect(cx - 16, ground - 50, cx + 16, ground - 44), castle.STONE[5])
    c.paint(c.ellipse(cx, ground - 52, 12, 5), CARPET[2])
    c.paint(c.ellipse(cx - 2, ground - 54, 6, 2), CARPET[3])
    glass = c.arch(cx - 13, ground - 76, cx + 13, ground - 50) & ~c.arch(cx - 11, ground - 74, cx + 11, ground - 50)
    broken = glass & ((c.xx > cx + 2) | (c.yy < ground - 66))
    c.paint(glass & ~broken, pg.hexes("#bfe0f0")[0])
    for _ in range(9):
        sx, sy = int(rng.integers(cx - 24, cx + 24)), int(rng.integers(ground - 6, ground))
        c.paint(c.rect(sx, sy, sx + 2, sy + 1), pg.hexes("#d8f0fa")[0])
    return outline(c.image), (cx, ground)
