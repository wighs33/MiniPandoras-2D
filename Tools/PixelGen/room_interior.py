"""나무 집 안(아지트, 디엘의 어린 시절 집 …)의 바닥 그림을 그리는 도구.

위에서 비스듬히 보는 모습이라 방마다 북쪽 벽의 안쪽 면(나무 들보·회벽·판자 징두리)이 보이고, 방 사이 벽과 바깥 벽은
윗면만 보인다. 바닥은 가로로 깐 판자다. 벽면은 바닥 첫 줄 바로 위 두 칸에 서므로 방 위에는 벽 줄이 둘 있어야 한다.
창·벽난로·벽면 문·위층 계단·바깥 문지방은 그 자리를 주면 그린다. 모두 점 단위(한 칸 32점)로 그린다.
"""
import numpy as np

import pixelgen as pg
from houses import BRASS, IRON, PLASTER, STONE, WOOD, Canvas

T = pg.TILE
FACE = 56                       # 북쪽 벽 안쪽 면의 높이(점). 두 칸에서 벽 윗면 몫을 뺐다.
FLOORBOARD = pg.hexes("#2a1b10", "#45301e", "#563b25", "#66472c", "#755234", "#865f3c")
CAP = pg.hexes("#100a08", "#1e140e", "#2c1e14", "#4a3422")
GLASS = pg.hexes("#22324a", "#3c5878", "#6a8cae", "#b4cce0")
SOOT = pg.hexes("#120c0a", "#22180f", "#34261a")
DAYLIGHT = pg.hexes("#8a7a52", "#d8c88e", "#f4e8b8", "#fffbe6")


def face_top(row):
    """바닥 첫 줄이 row인 방의 북쪽 벽면 위 끝 y(점). 벽면은 그 줄 바로 위에 선다."""
    return row * T - FACE


class RoomInterior:
    """방(바닥 칸 네모)들의 바닥 그림. areas는 (x0, y0, x1, y1) 칸 네모 목록이고 끝 칸은 들지 않는다."""

    def __init__(self, width, height, areas, seed):
        self.width, self.height = width, height
        self.c = Canvas(width * T, height * T)
        self.rng = np.random.default_rng(seed)
        self.floor = np.zeros((height, width), dtype=bool)
        for x0, y0, x1, y1 in areas:
            self.floor[y0:y1, x0:x1] = True
        self.dots = np.kron(self.floor, np.ones((T, T), dtype=bool))

    def wall_starts(self, x):
        """그 열에서 바닥이 시작하는 줄들(바로 위가 벽인 바닥 줄). 그 위마다 벽면이 선다."""
        column = self.floor[:, x]
        return [y for y in range(1, self.height) if column[y] and not column[y - 1]]

    def paint_base(self):
        """벽 윗면 → 판자 바닥 → 북쪽 벽면 → 그늘 순서로 칠한다. 창·벽난로 같은 것은 그 뒤에 그린다."""
        self.paint_caps()
        self.paint_floor()
        self.paint_faces()
        self.paint_shadows()

    def paint_caps(self):
        """벽 윗면: 짙은 나무 위에 드문드문 결, 바닥과 맞닿은 가장자리는 밝은 테."""
        c = self.c
        c.paint(c.rect(0, 0, *c.size), CAP[1])
        c.paint(((c.xx * 5 + c.yy * 11) % 37 == 0), CAP[0])
        near = pg.neighbours(self.dots) | pg.neighbours(pg.neighbours(self.dots))
        c.paint(near & ~self.dots, CAP[3])

    def paint_floor(self):
        """가로로 깐 판자: 줄마다 이음매 자리가 다르고, 가끔 한 장씩 밝기가 다르며 결과 옹이가 지나간다."""
        c = self.c
        image = np.zeros(c.image.shape[:2], dtype=int)
        for row in range(c.size[1] // 8):
            y0 = row * 8
            x = int(self.rng.integers(-60, 0))
            while x < c.size[0]:
                length = int(self.rng.integers(56, 120))
                # 대부분 같은 밝기이고 가끔 한 장씩 밝거나 어둡다(판자마다 다르면 얼룩덜룩해 보인다).
                level = 3 + int(self.rng.choice([-1, 0, 0, 0, 1]))
                image[y0:y0 + 8, max(0, x):x + length] = level
                image[y0:y0 + 8, max(0, x + length - 1):x + length] = 0
                x += length
            image[y0, :] = 0
            image[y0 + 1, :] = np.maximum(image[y0 + 1, :], 0) + (image[y0 + 1, :] > 0)
        grain = ((c.xx * 7 + (c.yy // 8) * 13) % 29 == 0) & (image > 1)
        image = np.where(grain, image - 1, image)
        knots = (self.rng.random(image.shape) < 0.0015) & (image > 0)
        image = np.where(knots, 1, image)
        c.paint(self.dots, FLOORBOARD[np.clip(image, 0, 5)])

    def paint_faces(self):
        """북쪽 벽 안쪽 면: 위는 나무 들보와 회벽, 아래는 판자 징두리와 굽도리. 방 왼쪽 끝부터 세 칸마다 나무 기둥."""
        c = self.c
        plaster = pg.noise(c.size[1], c.size[0], 6, self.rng)
        for x, row in ((x, row) for x in range(self.width) for row in self.wall_starts(x)):
            bottom, top = row * T, face_top(row)
            area = c.rect(x * T, top, (x + 1) * T, bottom)
            depth = np.clip((c.yy - top) / FACE, 0, 1)
            level = np.clip((plaster * 1.4 + depth * 2.2).astype(int) + 1, 1, 4)
            c.image[area, :3] = PLASTER[level[area]]
            c.image[area, 3] = 255
            wainscot = area & (c.yy >= bottom - 22)
            c.paint(wainscot, WOOD[3])
            c.paint(wainscot & ((c.xx % 16) == 0), WOOD[1])
            c.paint(wainscot & ((c.xx % 16) == 1), WOOD[4])
            c.paint(area & (c.yy >= bottom - 24) & (c.yy < bottom - 21), WOOD[4])
            c.paint(area & (c.yy >= bottom - 4), WOOD[1])
            c.paint(area & (c.yy < top + 7), WOOD[2])
            c.paint(area & (c.yy < top + 2), WOOD[4])
            c.paint(area & (c.yy >= top + 6) & (c.yy < top + 7), WOOD[0])
            run_x0 = x
            while run_x0 > 0 and self.floor[row, run_x0 - 1] and row in self.wall_starts(run_x0 - 1):
                run_x0 -= 1
            if (x - run_x0) % 3 == 0:
                post = area & (c.xx >= x * T) & (c.xx < x * T + 5) & (c.yy < bottom - 24)
                c.paint(post, WOOD[2])
                c.paint(post & (c.xx == x * T), WOOD[4])

    def paint_shadows(self):
        """벽면 바로 앞 바닥과 서쪽 벽 곁 바닥에 드리운 그늘."""
        c = self.c
        shade = np.ones(c.image.shape[:2])
        for x, row in ((x, row) for x in range(self.width) for row in self.wall_starts(x)):
            bottom = row * T
            band = (c.xx >= x * T) & (c.xx < (x + 1) * T) & (c.yy >= bottom) & (c.yy < bottom + 10)
            shade = np.where(band, np.minimum(shade, 0.62 + (c.yy - bottom) / 10 * 0.38), shade)
        left_wall = self.dots & ~np.roll(self.dots, 4, axis=1)
        shade = np.where(left_wall, np.minimum(shade, 0.78), shade)
        mask = self.dots & (shade < 1)
        c.image[mask, :3] = (c.image[mask, :3] * shade[mask][:, None]).astype(np.uint8)

    def window(self, x, row):
        """벽면의 나무 창: 십자 창살, 하늘빛 유리와 비스듬한 반사, 아래 창턱. x는 가운데(칸), row는 그 방 바닥 첫 줄."""
        c = self.c
        cx, top = int(x * T), face_top(row) + 11
        c.paint(c.rect(cx - 14, top - 2, cx + 14, top + 30), WOOD[1])
        glass = c.rect(cx - 11, top + 1, cx + 11, top + 27)
        c.paint(glass, GLASS[1])
        c.paint(glass & (c.yy > top + 16), GLASS[0])
        c.paint(glass & ((c.xx - c.yy) % 13 < 2) & (c.yy < top + 14), GLASS[3])
        c.paint(glass & ((c.xx == cx) | (c.xx == cx - 1) | (c.yy == top + 13)), WOOD[2])
        c.paint(c.rect(cx - 16, top + 28, cx + 16, top + 32), WOOD[4])

    @staticmethod
    def window_rect(x, row):
        """창 그림이 차지하는 네모(점, 왼쪽·위·너비·높이): 판자로 막을 때 쓴다."""
        cx, top = int(x * T), face_top(row) + 11
        return cx - 15, top - 3, 30, 34

    def fireplace(self, x, row):
        """돌 벽난로: 벽면을 덮는 굴뚝 몸, 나무 선반, 검게 그을린 아궁이, 바닥으로 나온 화덕돌."""
        c = self.c
        cx, top, bottom = int(x * T), face_top(row), row * T
        breast = c.rect(cx - 32, top, cx + 32, bottom)
        stones = (((c.xx + (c.yy // 9 % 2) * 7) % 14) < 13) & ((c.yy % 9) < 8)
        c.paint(breast, STONE[2])
        c.paint(breast & stones, STONE[3])
        c.paint(breast & stones & ((c.xx + c.yy) % 11 == 0), STONE[4])
        c.paint(c.rect(cx - 36, bottom - 30, cx + 36, bottom - 25), WOOD[3])
        c.paint(c.rect(cx - 36, bottom - 30, cx + 36, bottom - 29), WOOD[4])
        mouth = c.arch(cx - 15, bottom - 22, cx + 15, bottom)
        c.paint(mouth, SOOT[0])
        c.paint(mouth & (c.yy < bottom - 14), SOOT[1])
        hearth = c.rect(cx - 36, bottom, cx + 36, bottom + 9)
        c.paint(hearth, STONE[3])
        c.paint(hearth & (c.yy < bottom + 2), STONE[5])
        c.paint(hearth & ((c.xx - cx) % 18 == 0), STONE[1])

    def wall_door(self, x, row):
        """벽면에 낸 두꺼운 나무문: 쇠 경첩, 놋쇠 손잡이. x는 문이 난 칸 열."""
        c = self.c
        cx, top, bottom = int((x + 0.5) * T), face_top(row), row * T
        c.paint(c.rect(cx - 15, top + 6, cx + 15, bottom), WOOD[1])
        door = c.rect(cx - 12, top + 9, cx + 12, bottom)
        c.paint(door, WOOD[3])
        c.paint(door & ((c.xx - cx) % 6 == 0), WOOD[2])
        for y in (top + 16, bottom - 12):
            c.paint(c.rect(cx - 12, y, cx - 2, y + 3), IRON[2])
        c.paint(c.ellipse(cx + 7, top + 32, 2.5, 2.5), BRASS[3])

    def back_door(self, x, row):
        """
        뒷문(방문처럼 생겼다): 거친 돌을 둘러 쌓은 벽에 낸 낮은 나무 쪽문. 판자 폭이 들쭉날쭉하고 Z자 띠장을 댔으며,
        옆으로 밀어 둔 나무 빗장과 쇠 걸쇠가 있다. 문틈과 문 밑으로 바깥 빛이 새어 들어와 바닥에 깔린다. x는 문이 난 칸 열.
        """
        c = self.c
        cx, bottom = int((x + 0.5) * T), row * T
        door_w, door_h = 28, 44
        x0, x1, top = cx - door_w // 2, cx + door_w // 2, bottom - door_h

        # 회벽을 뚫고 다시 쌓은 듯한 거친 돌 테두리
        rim = c.rect(x0 - 8, top - 8, x1 + 8, bottom)
        stones = (((c.xx + (c.yy // 7 % 2) * 6) % 13) < 12) & ((c.yy % 7) < 6)
        c.paint(rim, STONE[1])
        c.paint(rim & stones, STONE[2])
        c.paint(rim & stones & ((c.xx * 3 + c.yy * 5) % 17 < 4), STONE[3])
        c.paint(c.rect(x0 - 3, top - 3, x1 + 3, bottom), WOOD[0])

        # 들쭉날쭉한 판자: 폭이 다르고 밝기도 조금씩 다르다
        edges = [x0, x0 + 6, x0 + 11, x0 + 18, x0 + 23, x1 - 2]
        for k, (left, right) in enumerate(zip(edges, edges[1:])):
            plank = c.rect(left, top, right, bottom)
            c.paint(plank, WOOD[2 + k % 3])
            c.paint(c.rect(left, top, left + 1, bottom), WOOD[0])
            c.paint(plank & ((c.xx * 2 + c.yy) % 11 == 0), WOOD[1])
        # 띠장 둘과 비스듬한 버팀목
        for y in (top + 5, bottom - 10):
            c.paint(c.rect(x0, y, x1 - 2, y + 4), WOOD[3])
            c.paint(c.rect(x0, y, x1 - 2, y + 1), WOOD[5])
            c.paint(c.rect(x0, y + 3, x1 - 2, y + 4), WOOD[1])
        c.line(x0 + 2, bottom - 11, x1 - 5, top + 9, WOOD[1], 3)
        c.line(x0 + 2, bottom - 12, x1 - 5, top + 8, WOOD[4], 1)

        # 나무 빗장: 쇠 걸쇠 둘에 끼운 굵은 막대를 왼쪽으로 밀어 두었다
        bar_y = top + 21
        c.paint(c.rect(x0 - 7, bar_y, x1 - 8, bar_y + 5), WOOD[4])
        c.paint(c.rect(x0 - 7, bar_y, x1 - 8, bar_y + 1), WOOD[5])
        c.paint(c.rect(x0 - 7, bar_y + 4, x1 - 8, bar_y + 5), WOOD[1])
        for bx in (x0 + 2, x0 + 16):
            c.paint(c.rect(bx, bar_y - 2, bx + 4, bar_y + 7), IRON[2])
            c.paint(c.rect(bx, bar_y - 2, bx + 1, bar_y + 7), IRON[4])
        c.paint(c.rect(x1 - 4, bar_y - 2, x1 + 2, bar_y + 7), IRON[1])

        # 쇠 띠 두 줄, 문틈으로 새는 바깥 빛(오른쪽 틈과 문 밑)
        for y in (top + 6, bottom - 9):
            c.paint(c.rect(x0, y, x0 + 9, y + 2), IRON[3])
        gap = c.rect(x1 - 2, top, x1, bottom)
        c.paint(gap, DAYLIGHT[2])
        c.paint(gap & ((c.yy % 5) == 0), DAYLIGHT[3])
        c.paint(c.rect(x0, bottom - 2, x1, bottom), DAYLIGHT[1])
        c.paint(c.rect(x1 - 2, bottom - 2, x1, bottom), DAYLIGHT[3])

        # 문 밑으로 바닥에 깔린 빛: 문에서 멀어질수록 점이 성기다
        spill_y = c.yy - bottom
        spill = (spill_y >= 0) & (spill_y < 14) & (c.xx >= x0 - 2 + spill_y // 2) & (c.xx < x1 + 8 + spill_y // 2)
        fade = spill & (pg.bayer(*c.image.shape[:2]) > spill_y / 14)
        c.image[fade, :3] = (c.image[fade, :3] * 0.55 + DAYLIGHT[1] * 0.45).astype(np.uint8)

    def stairs(self, x, y):
        """위층으로 오르는 나무 계단(2×2칸, 왼쪽 위 칸 x, y): 벽면을 뚫고 북쪽으로 올라가며 어두워진다. 왼쪽에 난간."""
        c = self.c
        x0, x1 = x * T, (x + 2) * T
        top, bottom = face_top(y), (y + 2) * T
        for k, y_step in enumerate(range(bottom, top, -8)):
            fade = k / max(1, (bottom - top) / 8)
            step = c.rect(x0 + 4, y_step - 8, x1 - 2, y_step)
            level = max(0, 4 - int(fade * 4))
            c.paint(step, WOOD[level])
            c.paint(step & (c.yy == y_step - 8), WOOD[min(5, level + 1)])
            c.paint(step & (c.yy >= y_step - 2), WOOD[max(0, level - 1)])
        c.paint(c.rect(x0, top, x0 + 4, bottom), WOOD[2])
        c.paint(c.rect(x0, top, x0 + 1, bottom), WOOD[4])
        c.paint(c.rect(x0 - 2, bottom - 14, x0 + 6, bottom), WOOD[1])

    def entrance(self, x, y):
        """남쪽 바깥 문: 벽 윗면을 끊고 판자 문지방, 양옆 문기둥. (x, y)는 문지방 칸."""
        c = self.c
        sill = c.rect(x * T, y * T, (x + 1) * T, (y + 1) * T)
        c.paint(sill, FLOORBOARD[3])
        c.paint(sill & (c.yy % 8 == 0), FLOORBOARD[0])
        for px in (x * T - 5, (x + 1) * T):
            c.paint(c.rect(px, y * T - 6, px + 5, (y + 1) * T), WOOD[2])
            c.paint(c.rect(px, y * T - 6, px + 1, (y + 1) * T), WOOD[4])

    def ground(self):
        return self.c.image[..., :3].copy()


def face_decal(m, name, image, x, row):
    """벽면 장식을 징두리 위 회벽 가운데쯤에 건다. x는 가운데(칸), row는 그 방 바닥 첫 줄."""
    h, w = image.shape[:2]
    m.decal(name, image, x * T - w / 2, face_top(row) + (FACE - 22 - h) / 2 + 2)


def floor_decal(m, name, image, x, y):
    """바닥 장식을 칸 (x, y)에 왼쪽 위를 맞춰 깐다."""
    m.decal(name, image, x * T, y * T)
