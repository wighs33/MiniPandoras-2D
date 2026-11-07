"""집을 따로 디자인하는 소품 생성기(맵 칸 모양을 따르지 말고, 좋은 도트 집을 다양하게).

집 한 채는 설계(Design)로 정한다: 너비, 층수, 지붕 모양(옆으로 긴 박공·앞이 세모인 맞배·둘을 합친 십자·사막의 평지붕), 지붕·벽 재료,
문·창·장식. 위에서 비스듬히 본 시점이라 앞벽이 보이고 지붕은 위에서 내려다본 면이 보인다. 빛은 왼쪽 위에서 온다.
크기는 인물에 맞춘다: 주인공 키가 약 77점(2.4칸)이라 한 층 84점, 문 높이 60~68점(주인공 키의 80~90%)이다.
한 칸 = 32점이고 그림은 2배로 키워 64px이다.

사용: python Tools/PixelGen/houses.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Houses/<이름>.png       집 그림(기준점: 벽 아래 가운데, houses.json)
  Docs/ArtUpgrade/PixelGen/houses_catalog.png             견본 모음(주인공과 함께)
"""
import json
import sys
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

T = pg.TILE
STORY = 84            # 한 층 높이(점)
EAVE = 10             # 처마가 벽 밖으로 나온 점 수
PLINTH = 10           # 벽 아래 돌 받침 높이
SHUTTER = 8           # 덧창 한 짝 너비
OUTLINE = np.array([24, 18, 20], dtype=np.uint8)

ROOFS = {
    "clay": pg.hexes("#3b1410", "#5e1f17", "#83301f", "#a8432a", "#c45a35", "#dd7a4c", "#f0a072"),
    "slate": pg.hexes("#1b2030", "#283044", "#37415a", "#4a5672", "#62708e", "#808ead", "#a7b3cb"),
    "thatch": pg.hexes("#3a2a12", "#5a4219", "#7d5d22", "#a07b30", "#c39b45", "#dcbc68", "#eed894"),
    "moss": pg.hexes("#13261d", "#1d3a2b", "#28513b", "#356b4c", "#47875f", "#62a275", "#86bf92"),
    "plum": pg.hexes("#20142a", "#332040", "#4a2d5a", "#634076", "#7e5692", "#9c74ad", "#c09ccb"),
    "copper": pg.hexes("#10282a", "#1a3c3c", "#245252", "#2f6a66", "#3f857c", "#5aa294", "#86c2b2"),
}
PLASTER = pg.hexes("#8f7d62", "#ae9b7c", "#c9b794", "#ddcfae", "#ece2c6", "#f6efda")
STONE = pg.hexes("#2b2a30", "#403e46", "#58555e", "#726e78", "#8e8a94", "#aca8b0", "#cac6cc")
WOOD = pg.hexes("#24170f", "#3d2818", "#5a3d24", "#7a5532", "#9a6f43", "#b98b58")
BRICK = pg.hexes("#3a1a16", "#5c2820", "#7c3829", "#9a4a33", "#b45f40", "#cc7a55")
GLASS = pg.hexes("#141c2a", "#1f2c42", "#33496b", "#5c7fa8", "#a8c4e0")
GLASS_DARK = pg.hexes("#0a0d14", "#10161f", "#18212e", "#232f40", "#34445a")
BRASS = pg.hexes("#3a2a10", "#6a4a18", "#9a6e24", "#c8963a", "#ecc876")
RUST = pg.hexes("#2a160e", "#4a2614", "#6e3a1c", "#93532a", "#b8743e")
PIPE = pg.hexes("#2e160c", "#5a2e1a", "#8a4a2a", "#b8693a", "#e09a62")
WARM = pg.hexes("#5a3a14", "#a8681c", "#e6a03a", "#ffd27a", "#fff1c4")
IRON = pg.hexes("#18181e", "#2a2a33", "#42424e", "#62626e", "#8a8a96")
CLOTH = {"green": pg.hexes("#1e3b2a", "#2d5a3e", "#3f7a55"), "blue": pg.hexes("#1d2c4a", "#2c4470", "#3f5f95"),
         "red": pg.hexes("#4a1a18", "#702824", "#963a32"), "teal": pg.hexes("#123a3a", "#1d5a58", "#2c7f7a"), "wood": WOOD[1:4]}
FLOWERS = pg.hexes("#e8607a", "#f2c94c", "#f5f0e6", "#b48ce0", "#ff9a5c")
LEAF = pg.hexes("#1c3a22", "#2c5a30", "#3f7a3c", "#5c9a48")
PAPER = pg.hexes("#b8a98a", "#e6dcc2", "#f7f1e0")
# 사막 마을(8장 힐리아 서부)의 흙벽돌 벽과 평지붕
ADOBE = pg.hexes("#7a5a3a", "#9c7650", "#bc9466", "#d4ae7e", "#e6c696", "#f2dab0")
POTTERY = pg.hexes("#5a2e1a", "#86452a", "#a85e38", "#c87a4c")


@dataclass
class Design:
    """집 한 채의 설계."""
    name: str
    columns: int                  # 벽 너비(칸)
    stories: int = 1
    roof: str = "side"            # side(옆으로 긴 박공) · front(앞이 세모) · cross(박공 + 입구 위로 나온 세모) · flat(사막의 평지붕)
    roof_material: str = "clay"   # ROOFS 열쇠
    walls: str = "timber"         # timber(회반죽+목조) · stone · planks · brick · plaster · adobe(흙벽돌)
    upper: str = ""               # 2층 벽 재료(비우면 walls)
    door: str = "plank"           # plank · arched · double · curtain(둥근 문간에 천을 늘어뜨린 사막 집 문)
    door_at: float = 0.5          # 문 가운데의 벽 위 자리(0 왼쪽 끝 ~ 1 오른쪽 끝)
    shutters: str = "green"       # CLOTH 열쇠, 비우면 덧창 없음
    arched_windows: bool = False  # 1층 창을 위가 둥근 돌 창으로
    flowers: bool = True
    chimney_at: float = 0.78      # 굴뚝 자리(0~1), 음수면 없음
    dormers: int = 0
    lit: bool = False             # 창에 불이 켜졌는지
    sign: str = ""                # inn · shop
    awning: str = ""              # 차양 색(CLOTH 열쇠), 비우면 없음
    ivy: bool = False
    lanterns: bool = False
    gable: str = "vent"           # 세모 박공벽 가운데: vent(둥근 환기창) · crest(모험가 길드 문장) · gear(뉴웨이브 톱니 문장)
    guild: bool = False           # 모험가 길드 장식: 깃발, 현판, 의뢰 게시판, 탑
    gears: bool = False           # 뉴웨이브 길드 장식: 벽의 톱니바퀴, 톱니 둥근 창, 구리 관, 쇠 굴뚝, 바람개비
    worn: bool = False            # 버려진 모습: 어둡고 금 간 창, 판자로 막은 창, 지붕 구멍과 이끼, 녹, 불 꺼진 등
    balcony: bool = False
    plot: str = "dirt"            # 집터: dirt(다진 흙 마당, 디딤돌, 꽃밭) · cobble(돌을 깐 앞뜰) · sand(모래 마당) · paved(돌바닥에 그대로 이어지는 앞뜰)
    ladder: bool = False          # 평지붕에 걸친 사다리
    cloth: str = "red"            # 천 문(curtain)과 지붕에 넌 깔개의 색(CLOTH 열쇠)
    seed: int = 1


# ── 그리기 도구 ──────────────────────────────────────────

class Canvas:
    """점 격자 RGBA에 모양 마스크로 칠하는 작은 화가."""

    def __init__(self, width, height):
        self.image = np.zeros((height, width, 4), dtype=np.uint8)
        self.yy, self.xx = np.mgrid[0:height, 0:width]

    @property
    def size(self):
        return self.image.shape[1], self.image.shape[0]

    def paint(self, mask, color):
        self.image[mask, :3] = color if np.ndim(color) == 1 else color[mask]
        self.image[mask, 3] = 255

    def rect(self, x0, y0, x1, y1):
        return (self.xx >= x0) & (self.xx < x1) & (self.yy >= y0) & (self.yy < y1)

    def ellipse(self, cx, cy, rx, ry):
        return ((self.xx + 0.5 - cx) / rx) ** 2 + ((self.yy + 0.5 - cy) / ry) ** 2 <= 1

    def arch(self, x0, top, x1, bottom):
        """위가 반원인 문·창 모양."""
        r = (x1 - x0) / 2
        return self.rect(x0, top + r, x1, bottom) | (self.ellipse(x0 + r, top + r, r, r) & (self.yy < top + r))

    def polygon(self, points):
        """볼록 다각형 안(꼭짓점 순서는 어느 방향이든 된다)."""
        inside = np.ones(self.xx.shape, dtype=bool)
        n = len(points)
        area = sum(points[i][0] * points[(i + 1) % n][1] - points[(i + 1) % n][0] * points[i][1] for i in range(n))
        sign = 1 if area > 0 else -1
        for i in range(n):
            (ax, ay), (bx, by) = points[i], points[(i + 1) % n]
            cross = (bx - ax) * (self.yy + 0.5 - ay) - (by - ay) * (self.xx + 0.5 - ax)
            inside &= cross * sign >= 0
        return inside

    def line(self, x0, y0, x1, y1, color, width=1):
        steps = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for k in range(steps):
            t = k / max(1, steps - 1)
            x, y = int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))
            self.image[max(0, y):y + width, max(0, x):x + width, :3] = color
            self.image[max(0, y):y + width, max(0, x):x + width, 3] = 255

    def first_row(self, x):
        """x 열에서 가장 위에 칠해진 줄."""
        rows = np.nonzero(self.image[:, x, 3])[0]
        return int(rows.min()) if len(rows) else self.size[1]


# ── 벽 재료 ────────────────────────────────────────────

def wall_texture(kind, height, width, rng):
    """벽 한 면의 무늬(RGB)."""
    if kind == "stone":
        return stone_wall(height, width, rng)
    if kind == "planks":
        return plank_wall(height, width, rng)
    if kind == "brick":
        return brick_wall(height, width, rng)
    if kind == "adobe":
        return adobe_wall(height, width, rng)
    return plaster_wall(height, width, rng)


def adobe_wall(height, width, rng):
    """흙벽돌에 흙을 덧바른 벽: 따뜻한 모랫빛에 손으로 바른 자국 얼룩, 가는 금 몇 줄."""
    smear = pg.noise(height, width, 4, rng)
    image = ADOBE[np.where(smear > 0.84, 4, np.where(smear < 0.14, 2, 3))].copy()
    for _ in range(int(height * width / 900)):
        x, y = int(rng.integers(2, width - 2)), int(rng.integers(2, height - 2))
        for _ in range(int(rng.integers(3, 9))):
            image[y, x] = ADOBE[1]
            x, y = x + int(rng.integers(-1, 2)), y + 1
            if not (0 <= x < width and 0 <= y < height):
                break
    return image


def plaster_wall(height, width, rng):
    """회반죽: 거의 고른 면에 작은 얼룩 몇 점. 큰 무늬를 두면 위장 무늬처럼 보인다."""
    grain = pg.noise(height, width, 4, rng)
    image = PLASTER[np.where(grain > 0.78, 4, 3)].copy()
    for _ in range(int(height * width / 120)):
        x, y = rng.integers(0, width - 1), rng.integers(0, height)
        image[y, x:x + 2] = PLASTER[2]
    return image


def stone_wall(height, width, rng):
    """돌을 줄마다 어긋나게 쌓은 벽: 돌마다 밝기가 조금 다르고, 윗가장자리는 밝고 아랫가장자리는 어둡다."""
    image = np.zeros((height, width, 3), dtype=np.uint8)
    y = 0
    while y < height:
        course = int(rng.integers(8, 12))
        x = -int(rng.integers(0, 12))
        while x < width:
            w = int(rng.integers(12, 21))
            level = int(rng.choice([3, 4, 4, 4, 5]))
            x0, x1, y1 = max(0, x), min(width, x + w), min(height, y + course)
            image[y:y1, x0:x1] = STONE[level]
            image[y:y1, x0:x0 + 1] = STONE[1]
            if y + 1 < y1:
                image[y + 1, x0 + 1:x1] = STONE[min(6, level + 1)]
            image[y1 - 1:y1, x0:x1] = STONE[level - 2]
            x += w
        image[y:y + 1, :] = STONE[1]
        y += course
    return image


def plank_wall(height, width, rng):
    """가로 판자벽: 판자 사이 어두운 틈, 나뭇결, 판자 끝 이음매."""
    image = np.zeros((height, width, 3), dtype=np.uint8)
    board_h = 7
    for y in range(height):
        board, within = y // board_h, y % board_h
        level = 3 + (board % 3 == 1) - (board % 4 == 2) - (within == board_h - 1) * 2 + (within == 0)
        image[y, :] = WOOD[int(np.clip(level, 0, 5))]
    for board in range(height // board_h + 1):
        joint = int(rng.integers(0, 60))
        for x in range(joint, width, 60):
            image[board * board_h:board * board_h + board_h - 1, x:x + 1] = WOOD[1]
    for _ in range(int(height * width / 60)):
        x, y = rng.integers(0, width - 4), rng.integers(0, height)
        if y % board_h not in (0, board_h - 1):
            image[y, x:x + int(rng.integers(3, 7))] = WOOD[2]
    return image


def brick_wall(height, width, rng):
    """벽돌: 한 장 12×5점, 줄마다 반 장 어긋나고 가끔 색이 다른 벽돌."""
    image = np.zeros((height, width, 3), dtype=np.uint8)
    odd = rng.random((height // 5 + 2, width // 12 + 2))
    for y in range(height):
        row, within = y // 5, y % 5
        for x in range(width):
            shifted = x + (row % 2) * 6
            joint = within == 4 or shifted % 12 == 0
            if joint:
                image[y, x] = PLASTER[1]
                continue
            level = 3 + (odd[row, shifted // 12] > 0.85) - (odd[row, shifted // 12] < 0.12)
            image[y, x] = BRICK[level + (within == 0)]
    return image


def timber_frame(c, x0, y0, width, height):
    """회반죽 벽 위의 나무 뼈대: 위·가운데·아래 들보, 칸마다 기둥, 칸마다 방향을 바꾸는 버팀목(창이 덮는다)."""
    rail = y0 + int(height * 0.58)
    bottom = y0 + height
    for k, bx in enumerate(range(x0, x0 + width, T)):
        right = min(bx + T, x0 + width)
        if k % 2 == 0:
            c.line(bx + 3, bottom - 5, right - 3, rail + 3, WOOD[2], 3)
            c.line(bx + 3, y0 + 5, right - 3, rail - 3, WOOD[2], 3)
        else:
            c.line(bx + 3, rail + 3, right - 3, bottom - 5, WOOD[2], 3)
            c.line(bx + 3, rail - 3, right - 3, y0 + 5, WOOD[2], 3)
    for y, thick in ((y0, 5), (rail - 1, 3), (bottom - 5, 5)):
        c.paint(c.rect(x0, y, x0 + width, y + thick), WOOD[2])
        c.paint(c.rect(x0, y, x0 + width, y + 1), WOOD[4])
        c.paint(c.rect(x0, y + thick - 1, x0 + width, y + thick), WOOD[1])
    for x in list(range(x0, x0 + width - 4, T)) + [x0 + width - 4]:
        c.paint(c.rect(x, y0, x + 4, bottom), WOOD[2])
        c.paint(c.rect(x, y0, x + 1, bottom), WOOD[4])
        c.paint(c.rect(x + 3, y0, x + 4, bottom), WOOD[1])


# ── 창·문·장식 ─────────────────────────────────────────

def window_footprint(design, upper=False, wide=24):
    arched = design.arched_windows and not upper
    sides = 6 if arched or not design.shutters else 2 * SHUTTER + 4
    return wide + sides + 4


def window(c, cx, top, design, wide=24, tall=30, arched=False, shutters=None, flowers=None):
    """
    창: 덧창(설계 색), 틀, 십자 창살, 유리(위는 어둡고 비스듬한 빛 한 줄), 창턱과 꽃 상자.
    arched는 위가 둥근 돌 창(덧창 없음)이다.
    """
    shutters = design.shutters if shutters is None else shutters
    flowers = (design.flowers if flowers is None else flowers) and not design.worn
    x0, x1 = cx - wide // 2, cx - wide // 2 + wide
    glass = WARM if design.lit else GLASS_DARK if design.worn else GLASS
    if shutters and not arched:
        sh = CLOTH[shutters]
        for sx in (x0 - 3 - SHUTTER, x1 + 3):
            c.paint(c.rect(sx, top - 1, sx + SHUTTER, top + tall + 1), sh[1])
            for y in range(top + 2, top + tall, 3):
                c.paint(c.rect(sx + 2, y, sx + SHUTTER - 2, y + 1), sh[0])
            c.paint(c.rect(sx, top - 1, sx + 1, top + tall + 1), sh[2])
            c.paint(c.rect(sx + SHUTTER - 1, top - 1, sx + SHUTTER, top + tall + 1), sh[0])
    shape = c.arch if arched else c.rect
    frame = shape(x0 - 3, top - 3, x1 + 3, top + tall + 3)
    c.paint(frame, STONE[4] if arched else WOOD[1])
    if arched:
        c.paint(frame & (c.xx < x0 - 1), STONE[5])
        c.paint(c.rect(cx - 2, top - 4, cx + 2, top + 1), STONE[5])
    pane = shape(x0, top, x1, top + tall)
    c.paint(pane, glass[1])
    c.paint(pane & (c.yy >= top + tall // 2), glass[2])
    c.paint(pane & (c.yy < top + 2), glass[0])
    glint = pane & ((c.xx - x0) - (c.yy - top) >= 3) & ((c.xx - x0) - (c.yy - top) < 6)
    c.paint(glint, glass[4] if design.lit else glass[3])
    c.paint(pane & (np.abs(c.xx + 0.5 - cx) < 1.5), WOOD[2])
    c.paint(pane & (np.abs(c.yy + 0.5 - (top + tall * 0.55)) < 1.5), WOOD[2])
    sill_color = STONE[5] if arched else WOOD[4]
    c.paint(c.rect(x0 - 5, top + tall + 3, x1 + 5, top + tall + 6), sill_color)
    c.paint(c.rect(x0 - 5, top + tall + 5, x1 + 5, top + tall + 6), STONE[2] if arched else WOOD[1])
    if flowers:
        box = top + tall + 6
        c.paint(c.rect(x0 - 2, box, x1 + 2, box + 7), WOOD[2])
        c.paint(c.rect(x0 - 2, box, x1 + 2, box + 1), WOOD[4])
        c.paint(c.rect(x0 - 2, box + 6, x1 + 2, box + 7), WOOD[0])
        rng = np.random.default_rng(cx * 31 + top)
        for x in range(x0 - 1, x1 + 1, 2):
            c.paint(c.rect(x, box - 3 - (x // 2) % 2, x + 2, box + 1), LEAF[1 + (x // 2) % 3])
        for _ in range(wide // 3):
            x = int(rng.integers(x0, x1 - 1))
            c.paint(c.rect(x, box - 5 + int(rng.integers(0, 2)), x + 2, box - 3), FLOWERS[rng.integers(0, len(FLOWERS))])
    if design.worn:
        # 금 간 유리, 그리고 창 셋에 둘은 판자 두 장을 엇걸어 막았다
        c.line(x0 + wide * 0.7, top + 1, x0 + wide * 0.45, top + tall * 0.5, GLASS_DARK[4], 1)
        if (cx // 9 + top) % 3 != 0:
            for k, (y_left, y_right) in enumerate(((0.22, 0.38), (0.72, 0.6))):
                ya, yb = top + tall * y_left, top + tall * y_right
                c.line(x0 - 4, ya, x1 + 3, yb, WOOD[3 - k], 4)
                c.line(x0 - 4, ya, x1 + 3, yb, WOOD[4 - k], 1)
                c.paint(c.rect(x0 - 2, int(ya) + 1, x0, int(ya) + 3) | c.rect(x1, int(yb) + 1, x1 + 2, int(yb) + 3), IRON[3])


DOORS = {"plank": (30, 60), "arched": (34, 64), "double": (46, 68), "curtain": (30, 58)}


def curtain_door(c, cx, ground, x0, x1, top, bottom, cloth):
    """사막 집 문: 두껍게 바른 흙 테두리의 둥근 문간, 어두운 안, 위에서 늘어뜨린 줄무늬 천(한쪽을 묶어 올렸다), 모래 돌 한 단."""
    rim = c.arch(x0 - 5, top - 5, x1 + 5, bottom)
    c.paint(rim, ADOBE[4])
    c.paint(rim & (c.xx >= x1 + 1), ADOBE[2])
    opening = c.arch(x0, top, x1, bottom)
    c.paint(opening, pg.hexes("#1e140e")[0])
    cloth_rgb = CLOTH[cloth]
    hang = opening & (c.yy < top + (bottom - top) * 0.78) & ~((c.xx > cx + 4) & (c.yy > top + (bottom - top) * 0.32))
    c.paint(hang, cloth_rgb[1])
    c.paint(hang & ((c.xx - x0) % 8 < 2), cloth_rgb[2])
    c.paint(hang & (c.yy > top + (bottom - top) * 0.7), cloth_rgb[0])
    c.paint(c.rect(cx + 2, top + (bottom - top) * 0.3, cx + 7, top + (bottom - top) * 0.36), POTTERY[3])
    c.paint(c.rect(x0 - 9, ground - 6, x1 + 9, ground - 2), ADOBE[3])
    c.paint(c.rect(x0 - 9, ground - 6, x1 + 9, ground - 5), ADOBE[5])
    return x0 - 5, x1 + 5


def door(c, cx, ground, kind, cloth="red"):
    """
    문: 돌 문틀, 세로 판자, 쇠 띠와 못, 손잡이 고리, 앞에 돌계단 두 단.
    arched는 윗부분이 둥글고 맨 위에 쐐기돌이 있다. double은 두 짝.
    """
    w, h = DOORS[kind]
    x0, x1 = cx - w // 2, cx - w // 2 + w
    bottom = ground - 6
    top = bottom - h
    if kind == "curtain":
        return curtain_door(c, cx, ground, x0, x1, top, bottom, cloth)
    shape = c.rect if kind == "plank" else c.arch
    frame = shape(x0 - 5, top - 5, x1 + 5, bottom)
    c.paint(frame, STONE[4])
    c.paint(frame & (c.xx < x0 - 2), STONE[5])
    c.paint(frame & (c.xx >= x1 + 2), STONE[2])
    if kind != "plank":
        c.paint(c.rect(cx - 3, top - 6, cx + 3, top + 1), STONE[5])
        c.paint(c.rect(cx - 3, top, cx + 3, top + 1), STONE[2])
    body = shape(x0, top, x1, bottom)
    c.paint(body, WOOD[3])
    for x in range(x0, x1, 6):
        c.paint(body & (c.xx == x), WOOD[1])
        c.paint(body & (c.xx == x + 1), WOOD[4])
    c.paint(body & ~shape(x0 + 2, top + 2, x1, bottom), WOOD[0])
    if kind == "double":
        c.paint(body & (np.abs(c.xx + 0.5 - cx) < 1.5), WOOD[0])
    for y in (top + (w // 2 if kind != "plank" else 0) + 12, bottom - 16):
        band = c.rect(x0 + 2, y, x1, y + 3) & body
        c.paint(band, IRON[2])
        c.paint(band & (c.yy == y), IRON[3])
        for x in range(x0 + 4, x1, 7):
            c.paint(c.rect(x, y + 1, x + 1, y + 2), IRON[4])
    handle_y = bottom - h // 2
    handles = (cx - 6, cx + 4) if kind == "double" else (x1 - 8,)
    for hx in handles:
        ring = c.ellipse(hx + 1, handle_y + 2, 3, 3) & ~c.ellipse(hx + 1, handle_y + 2, 1.6, 1.6)
        c.paint(ring, IRON[4])
    for k, (inset, level) in enumerate(((7, 4), (11, 3))):
        y = ground - 6 + k * 3
        c.paint(c.rect(x0 - inset, y, x1 + inset, y + 3), STONE[level + 1])
        c.paint(c.rect(x0 - inset, y, x1 + inset, y + 1), STONE[6])
        c.paint(c.rect(x0 - inset, y + 2, x1 + inset, y + 3), STONE[level - 1])
    return x0 - 5, x1 + 5


def lantern(c, x, y, lit=True):
    """벽에 단 등불: 쇠 받침과 유리(켜지면 따뜻하다), 꼭대기 고리."""
    glow = (WARM[3], WARM[4]) if lit else (GLASS_DARK[2], GLASS_DARK[3])
    c.paint(c.rect(x - 1, y, x + 2, y + 3), IRON[2])
    c.paint(c.rect(x - 5, y + 3, x + 6, y + 5), IRON[2])
    c.paint(c.rect(x - 4, y + 5, x + 5, y + 17), IRON[1])
    c.paint(c.rect(x - 3, y + 6, x + 4, y + 16), glow[0])
    c.paint(c.rect(x - 1, y + 8, x + 2, y + 13), glow[1])
    c.paint(c.rect(x - 5, y + 17, x + 6, y + 19), IRON[2])


def ivy(c, x0, y0, width, height, rng):
    """벽을 타고 오르는 담쟁이: 아래가 빽빽하고 위로 갈수록 성기다. 잎 한 장은 2×2점."""
    for _ in range(int(width * height / 7)):
        x = int(rng.integers(x0, x0 + width))
        y = int(y0 + height - rng.random() ** 0.55 * height)
        if rng.random() < 0.9 - (x - x0) / width * 0.5:
            c.paint(c.rect(x, y, x + 2, y + 2), LEAF[int(rng.integers(0, 4))])


def hanging_sign(c, x, y, kind):
    """벽에서 튀어나온 쇠 팔에 매단 나무 간판. 여관은 잔, 가게는 빵."""
    c.paint(c.rect(x, y, x + 24, y + 2), IRON[2])
    c.line(x, y + 8, x + 8, y + 2, IRON[2], 1)
    board = c.rect(x + 4, y + 6, x + 26, y + 26)
    c.paint(board, WOOD[3])
    c.paint(c.rect(x + 4, y + 6, x + 26, y + 7), WOOD[5])
    c.paint(c.rect(x + 4, y + 25, x + 26, y + 26), WOOD[1])
    c.paint(c.rect(x + 7, y + 2, x + 8, y + 6) | c.rect(x + 22, y + 2, x + 23, y + 6), IRON[2])
    if kind == "gear":
        c.paint(board, CLOTH["teal"][1])
        c.paint(c.rect(x + 4, y + 6, x + 26, y + 7), CLOTH["teal"][2])
        gear(c, x + 15, y + 16, 7, 8, BRASS, open_spokes=False)
    elif kind == "inn":
        c.paint(c.rect(x + 10, y + 11, x + 18, y + 22), WARM[2])
        c.paint(c.rect(x + 18, y + 13, x + 21, y + 19), WARM[2])
        c.paint(c.rect(x + 19, y + 14, x + 20, y + 18), WOOD[3])
        c.paint(c.rect(x + 10, y + 11, x + 18, y + 13), PAPER[2])
    else:
        loaf = c.ellipse(x + 15, y + 17, 7, 5)
        c.paint(loaf, WARM[2])
        c.paint(loaf & (c.yy < y + 15), WARM[3])
        for k in range(3):
            c.paint(c.rect(x + 11 + k * 3, y + 14, x + 12 + k * 3, y + 17), WARM[1])


def awning(c, x0, x1, y, color):
    """가게 차양: 줄무늬 천, 아래 끝은 물결, 밑에 그늘."""
    stripes = (CLOTH[color][1], PLASTER[5])
    for x in range(x0, x1):
        stripe = stripes[(x - x0) // 6 % 2]
        bottom = y + 14 + (2 if (x - x0) % 6 in (2, 3) else 0)
        c.paint(c.rect(x, y, x + 1, bottom), stripe)
        c.paint(c.rect(x, y, x + 1, y + 2), (stripe * 0.75).astype(np.uint8))
        c.paint(c.rect(x, bottom, x + 1, bottom + 3), (c.image[bottom, x, :3] * 0.6).astype(np.uint8))


def flat_roof(c, x0, x1, wall_top, depth, design, rng):
    """
    사막 집의 평지붕: 위에서 내려다본 흙 지붕 면(둘레에 낮은 난간 턱), 앞 끝의 두께, 앞벽에 튀어나온 들보 끝(비가스),
    물을 흘려보내는 나무 홈통. 지붕 위에 항아리와 넌 깔개를 놓는다.
    """
    top = wall_top - depth
    slab = c.rect(x0 - 3, top, x1 + 3, wall_top + 4)
    c.paint(slab, ADOBE[3])
    grain = pg.noise(c.image.shape[0], c.image.shape[1], 5, rng)
    c.paint(slab & (grain > 0.7), ADOBE[4])
    parapet = slab & ~c.rect(x0 + 3, top + 5, x1 - 3, wall_top - 2)
    c.paint(parapet, ADOBE[5])
    c.paint(parapet & (c.xx >= x1 - 3), ADOBE[3])
    inner = c.rect(x0 + 3, top + 5, x1 - 3, top + 7)
    c.paint(inner, ADOBE[2])
    c.paint(c.rect(x0 - 3, wall_top - 2, x1 + 3, wall_top + 4), ADOBE[2])
    c.paint(c.rect(x0 - 3, wall_top - 2, x1 + 3, wall_top - 1), ADOBE[4])
    for x in range(x0 + 6, x1 - 4, 15):
        c.paint(c.rect(x, wall_top + 4, x + 4, wall_top + 8), WOOD[2])
        c.paint(c.rect(x, wall_top + 4, x + 4, wall_top + 5), WOOD[4])
    spout_x = x0 + 4
    c.paint(c.rect(spout_x - 6, wall_top, spout_x + 2, wall_top + 3), WOOD[3])
    # 지붕 위 살림: 항아리 둘과 줄무늬 깔개
    for k in range(2):
        px = int(x1 - 18 - k * 12)
        py = int(top + 12 + k * 3)
        pot = c.ellipse(px, py + 4, 5, 5) | c.rect(px - 2, py - 1, px + 3, py + 1)
        c.paint(pot, POTTERY[2])
        c.paint(pot & (c.xx < px - 1), POTTERY[3])
        c.paint(c.ellipse(px, py, 2.5, 1.2), POTTERY[0])
    rug = c.rect(x0 + 10, top + 10, x0 + 10 + min(40, (x1 - x0) // 3), wall_top - 6)
    c.paint(rug, CLOTH[design.cloth][1])
    c.paint(rug & ((c.yy - top) % 6 < 2), CLOTH[design.cloth][2])


def ladder(c, x, ground, roof_y):
    """앞벽에 기대 세운 나무 사다리(땅에서 지붕 턱 위까지)."""
    for rail in (x, x + 10):
        c.paint(c.rect(rail, roof_y - 8, rail + 2, ground), WOOD[3])
        c.paint(c.rect(rail, roof_y - 8, rail + 1, ground), WOOD[4])
    for y in range(roof_y - 4, ground - 2, 9):
        c.paint(c.rect(x + 2, y, x + 10, y + 2), WOOD[2])


def chimney(c, cx, top, bottom):
    """돌 굴뚝: 지붕에서 솟는다. 위에 테두리 돌과 어두운 구멍."""
    c.paint(c.rect(cx - 8, top + 4, cx + 8, bottom), STONE[3])
    for y in range(top + 8, bottom, 6):
        c.paint(c.rect(cx - 8, y, cx + 8, y + 1), STONE[1])
        off = 0 if (y // 6) % 2 else 5
        for x in range(cx - 8 + off, cx + 8, 10):
            c.paint(c.rect(x, y - 5, x + 1, y), STONE[1])
    c.paint(c.rect(cx - 8, top + 4, cx - 6, bottom), STONE[5])
    c.paint(c.rect(cx + 6, top + 4, cx + 8, bottom), STONE[2])
    c.paint(c.rect(cx - 10, top, cx + 10, top + 5), STONE[5])
    c.paint(c.rect(cx - 10, top + 4, cx + 10, top + 5), STONE[2])
    c.paint(c.rect(cx - 6, top + 1, cx + 6, top + 3), STONE[0])


# ── 지붕 ───────────────────────────────────────────────

def cover(c, mask, material, top, bottom, rng, side_light=0):
    """지붕면을 재료대로 덮는다."""
    if material == "thatch":
        thatch(c, mask, ROOFS[material], top, bottom, rng, side_light)
    else:
        shingles(c, mask, ROOFS[material], top, bottom, rng, side_light)


def shingles(c, mask, palette, top, bottom, rng, side_light=0):
    """
    기와 줄: 줄마다 반 장 어긋난 조각, 조각 아랫가장자리는 어둡고 윗가장자리는 밝다.
    처마로 갈수록 조금 밝다. side_light는 면 전체의 밝기 보정(그늘진 옆면 등).
    """
    rows, tab = 6, 8
    ys, xs = np.nonzero(mask)
    course = (ys - top) // rows
    within = (ys - top) % rows
    tile = (xs + (course % 2) * (tab // 2)) // tab
    rng_levels = np.random.default_rng(int(rng.integers(0, 1 << 30)))
    # 대부분은 같은 밝기, 가끔 한 장만 다르게: 고르게 섞으면 지붕에 큰 얼룩이 진다
    jitter = rng_levels.choice([-1, 0, 1], p=[0.06, 0.88, 0.06], size=(course.max() + 2, tile.max() + 2))
    depth = (ys - top) / max(1, bottom - top)
    level = 3 + np.round(depth * 1.2).astype(int) + side_light + jitter[course, tile]
    level = np.where(within == rows - 1, level - 2, level)
    level = np.where(within == 0, level + 1, level)
    edge = (xs + (course % 2) * (tab // 2)) % tab == 0
    level = np.where(edge & (within < rows - 1), level - 1, level)
    c.image[ys, xs, :3] = palette[np.clip(level, 0, len(palette) - 1)]
    c.image[ys, xs, 3] = 255


def thatch(c, mask, palette, top, bottom, rng, side_light=0):
    """
    초가: 짚 다발을 세로 가닥으로 그린다. 두툼한 단이 몇 겹 겹치고, 단의 아랫단은 그늘이 지며,
    가닥마다 단 끝이 조금씩 들쭉날쭉하다.
    """
    ys, xs = np.nonzero(mask)
    layer = 12
    strands = np.random.default_rng(int(rng.integers(0, 1 << 30)))
    reach = strands.integers(-2, 3, size=c.size[0] + 2)
    tone = strands.choice([-1, 0, 0, 1], size=c.size[0] + 2)
    depth = (ys - top) / max(1, bottom - top)
    within = (ys - top + reach[xs]) % layer
    level = 3 + np.round(depth * 1.3).astype(int) + side_light + tone[xs]
    level = np.where(within >= layer - 3, level - 2 + (within == layer - 3), level)
    level = np.where(within <= 1, level + 1, level)
    c.image[ys, xs, :3] = palette[np.clip(level, 0, len(palette) - 1)]
    c.image[ys, xs, 3] = 255


def side_gable_roof(c, x0, x1, eave_y, height, material, rng):
    """
    옆으로 긴 박공지붕: 앞 지붕면이 처마에서 용마루까지 올라가며 양끝이 조금 안으로 들어온다(박공 끝이 비스듬히 보인다).
    양끝에 박공 판자, 용마루에 덮개 기와. 돌려주는 값: 용마루 y.
    """
    palette = ROOFS[material]
    ridge_y = eave_y - height
    lean = height // 4
    face = c.polygon([(x0 - EAVE, eave_y), (x1 + EAVE, eave_y), (x1 + EAVE - lean, ridge_y), (x0 - EAVE + lean, ridge_y)])
    cover(c, face, material, ridge_y, eave_y, rng)
    c.line(x0 - EAVE, eave_y, x0 - EAVE + lean, ridge_y, WOOD[4], 3)
    c.line(x1 + EAVE - 3, eave_y, x1 + EAVE - lean - 3, ridge_y, WOOD[1], 3)
    left, right = x0 - EAVE + lean, x1 + EAVE - lean
    if material == "thatch":
        c.paint(c.rect(left, ridge_y - 5, right, ridge_y + 2), palette[2])
        c.paint(c.rect(left, ridge_y - 5, right, ridge_y - 4), palette[4])
        for x in range(left + 6, right, 14):
            c.paint(c.rect(x, ridge_y - 5, x + 2, ridge_y + 2), WOOD[1])
        eave = c.rect(x0 - EAVE, eave_y - 3, x1 + EAVE, eave_y + 2) & ((c.xx * 7 + c.yy * 3) % 5 != 0)
        c.paint(eave, palette[2])
        c.paint(c.rect(x0 - EAVE, eave_y + 1, x1 + EAVE, eave_y + 2) & ((c.xx % 3) != 0), palette[1])
    else:
        c.paint(c.rect(left, ridge_y - 4, right, ridge_y + 1), palette[5])
        c.paint(c.rect(left, ridge_y - 4, right, ridge_y - 3), palette[6])
        c.paint(c.rect(left, ridge_y + 1, right, ridge_y + 2), palette[1])
        for x in range(left, right, 7):
            c.paint(c.rect(x, ridge_y - 4, x + 1, ridge_y + 1), palette[3])
        c.paint(c.rect(x0 - EAVE, eave_y - 1, x1 + EAVE, eave_y + 2), palette[1])
    return ridge_y


def front_gable_roof(c, x0, x1, eave_y, height, material, rng, wall_kind, ornament="vent", worn=False):
    """
    앞이 세모인 맞배지붕: 앞에 세모 박공벽이 서고, 그 뒤로 지붕 두 면이 물러난다(왼쪽 면은 빛, 오른쪽 면은 그늘).
    박공벽 가운데에는 둥근 환기창이나 길드 문장이 있다. 돌려주는 값: 지붕 꼭대기 y.
    """
    palette = ROOFS[material]
    cx = (x0 + x1) / 2
    apex = eave_y - height
    back = int(height * 0.5)
    left_plane = c.polygon([(x0 - EAVE, eave_y), (cx, apex), (cx, apex - back), (x0 - EAVE, eave_y - back)])
    right_plane = c.polygon([(x1 + EAVE, eave_y), (cx, apex), (cx, apex - back), (x1 + EAVE, eave_y - back)])
    cover(c, left_plane, material, apex - back, eave_y, rng, side_light=1)
    cover(c, right_plane, material, apex - back, eave_y, rng, side_light=-1)
    c.paint(c.rect(int(cx) - 2, apex - back - 1, int(cx) + 2, apex + 2), palette[6 if material != "thatch" else 2])
    gable = c.polygon([(x0 + 2, eave_y), (x1 - 2, eave_y), (cx, apex + 9)])
    c.paint(gable, wall_texture(wall_kind, c.size[1], c.size[0], rng))
    if wall_kind == "timber":
        for x in range(int(cx) - 2 - T, x0, -T):
            c.paint(gable & (c.xx >= x) & (c.xx < x + 4), WOOD[2])
        for x in range(int(cx) - 2 + T, x1, T):
            c.paint(gable & (c.xx >= x) & (c.xx < x + 4), WOOD[2])
        c.paint(gable & (np.abs(c.xx + 0.5 - cx) < 2), WOOD[2])
        beam = eave_y - int(height * 0.32)
        c.paint(gable & (c.yy >= beam) & (c.yy < beam + 3), WOOD[2])
    # 박공벽 아래(벽과 만나는 곳)의 그늘
    c.paint(gable & (c.yy >= eave_y - 3), (c.image[..., :3] * 0.7).astype(np.uint8))
    if ornament == "crest":
        crest(c, int(cx), int(eave_y - height * 0.47), max(10, int(height * 0.2)))
    elif ornament == "gear":
        newwave_emblem(c, int(cx), int(eave_y - height * 0.42), max(10, int(height * 0.3)), worn)
    else:
        vy = eave_y - height * 0.4
        c.paint(c.ellipse(cx, vy, 8, 8), WOOD[1])
        c.paint(c.ellipse(cx, vy, 5.5, 5.5), GLASS[1])
        c.paint(c.ellipse(cx, vy, 5.5, 5.5) & (np.abs(c.xx + 0.5 - cx) < 1), WOOD[2])
    c.line(x0 - EAVE, eave_y, cx, apex, WOOD[4], 3)
    c.line(x1 + EAVE - 3, eave_y, cx - 1, apex, WOOD[2], 3)
    return apex - back


def dormer(c, cx, base_y, material, design, rng):
    """지붕창: 지붕에서 솟은 작은 세모 지붕 아래 창 하나."""
    w, h = 30, 28
    c.paint(c.rect(cx - w // 2 + 3, base_y - h + 10, cx + w // 2 - 3, base_y + 2), PLASTER[3])
    c.paint(c.rect(cx + w // 2 - 6, base_y - h + 10, cx + w // 2 - 3, base_y + 2), PLASTER[1])
    window(c, cx, base_y - h + 14, design, wide=14, tall=14, shutters="", flowers=False)
    roof_mask = c.polygon([(cx - w // 2 - 3, base_y - h + 12), (cx + w // 2 + 3, base_y - h + 12), (cx, base_y - h - 6)])
    cover(c, roof_mask, material, base_y - h - 6, base_y - h + 12, rng, side_light=1)
    c.line(cx - w // 2 - 3, base_y - h + 12, cx, base_y - h - 6, WOOD[4], 2)
    c.line(cx + w // 2 + 1, base_y - h + 12, cx, base_y - h - 6, WOOD[2], 2)


# ── 길드 장식 ───────────────────────────────────────────

def crest(c, cx, cy, r):
    """길드 문장: 엇갈린 두 칼 위에 금테 두른 파란 방패, 가운데 금빛 별."""
    for sx in (-1, 1):
        tip = (cx + sx * int(r * 1.5), cy - int(r * 1.5))
        hilt = (cx - sx * int(r * 1.3), cy + int(r * 1.3))
        c.line(hilt[0], hilt[1], tip[0], tip[1], IRON[4], 2)
        gx, gy = cx - sx * int(r * 1.0), cy + int(r * 1.0)
        c.line(gx - 3, gy - 3 * sx * -1, gx + 3, gy + 3 * sx * -1, WARM[2], 2)
        c.paint(c.rect(hilt[0] - 1, hilt[1] - 1, hilt[0] + 2, hilt[1] + 2), WARM[1])
    rim = [(cx - r - 2, cy - r - 2), (cx + r + 2, cy - r - 2), (cx + r + 2, cy + r * 0.3), (cx, cy + r * 1.35), (cx - r - 2, cy + r * 0.3)]
    face = [(cx - r, cy - r), (cx + r, cy - r), (cx + r, cy + r * 0.3), (cx, cy + r * 1.15), (cx - r, cy + r * 0.3)]
    c.paint(c.polygon(rim), WARM[2])
    inner = c.polygon(face)
    c.paint(inner, CLOTH["blue"][1])
    c.paint(inner & (c.xx < cx), CLOTH["blue"][2])
    c.paint(inner & (c.yy < cy - r + 2), CLOTH["blue"][0])
    star = c.polygon([(cx, cy - r * 0.6), (cx + r * 0.45, cy), (cx, cy + r * 0.6), (cx - r * 0.45, cy)])
    c.paint(star, WARM[3])
    c.paint(star & (c.xx < cx) & (c.yy < cy), WARM[4])


def banner(c, x, y, color, length=46):
    """길드 깃발: 쇠 막대에 매단 긴 천, 문장, 아래 끝은 제비 꼬리."""
    cloth = CLOTH[color]
    c.paint(c.rect(x - 3, y, x + 17, y + 3), IRON[2])
    c.paint(c.rect(x - 4, y - 1, x - 2, y + 4) | c.rect(x + 16, y - 1, x + 18, y + 4), WARM[2])
    body = c.rect(x, y + 3, x + 14, y + length)
    notch = c.polygon([(x - 1, y + length), (x + 15, y + length), (x + 7, y + length - 7)])
    cloth_mask = body & ~notch
    c.paint(cloth_mask, cloth[1])
    c.paint(cloth_mask & (c.xx < x + 2), cloth[2])
    c.paint(cloth_mask & (c.xx >= x + 12), cloth[0])
    c.paint(cloth_mask & ((c.yy == y + 7) | (c.yy == y + length - 12)), WARM[2])
    diamond = c.polygon([(x + 7, y + 13), (x + 12, y + 21), (x + 7, y + 29), (x + 2, y + 21)])
    c.paint(diamond, WARM[3])
    c.paint(c.polygon([(x + 7, y + 17), (x + 9, y + 21), (x + 7, y + 25), (x + 5, y + 21)]), cloth[0])


def guild_plaque(c, cx, y):
    """발코니 난간 앞의 길드 현판: 금테 두른 나무판에 방패와 글자 자리 줄."""
    x0, x1 = cx - 30, cx + 30
    c.paint(c.rect(x0 - 2, y - 2, x1 + 2, y + 16), WARM[1])
    c.paint(c.rect(x0, y, x1, y + 14), WOOD[2])
    c.paint(c.rect(x0, y, x1, y + 1), WOOD[4])
    c.paint(c.polygon([(cx - 5, y + 2), (cx + 5, y + 2), (cx + 5, y + 8), (cx, y + 12), (cx - 5, y + 8)]), CLOTH["blue"][2])
    c.paint(c.rect(cx - 1, y + 3, cx + 1, y + 10), WARM[3])
    for side in (-1, 1):
        for k in range(3):
            gx = cx + side * (11 + k * 6) - (4 if side < 0 else 0)
            c.paint(c.rect(gx, y + 5, gx + 4, y + 9), WARM[3])


def quest_board(c, x0, ground, rng):
    """의뢰 게시판: 작은 판자 지붕 아래 나무판에 의뢰서 몇 장을 빨간 핀으로 꽂았다. 다리 둘이 땅에 박힌다."""
    w, h = 40, 30
    top = ground - 18 - h
    c.paint(c.rect(x0 + 4, top + h, x0 + 8, ground - 1) | c.rect(x0 + w - 8, top + h, x0 + w - 4, ground - 1), WOOD[2])
    c.paint(c.rect(x0 - 2, top - 2, x0 + w + 2, top + h + 2), WOOD[1])
    surface = c.rect(x0, top, x0 + w, top + h)
    c.paint(surface, WOOD[4])
    c.paint(surface & ((c.xx + c.yy * 3) % 7 == 0), WOOD[3])
    c.paint(c.polygon([(x0 - 6, top - 2), (x0 + w + 6, top - 2), (x0 + w + 2, top - 9), (x0 - 2, top - 9)]), WOOD[2])
    c.paint(c.rect(x0 - 2, top - 9, x0 + w + 2, top - 8), WOOD[5])
    for px, py, pw, ph in ((3, 4, 11, 13), (16, 2, 10, 9), (28, 5, 9, 14), (15, 14, 12, 13), (4, 19, 9, 9)):
        paper = c.rect(x0 + px, top + py, x0 + px + pw, top + py + ph)
        c.paint(paper, PAPER[2])
        c.paint(paper & (c.xx >= x0 + px + pw - 1), PAPER[0])
        for ly in range(top + py + 4, top + py + ph - 1, 3):
            c.paint(c.rect(x0 + px + 2, ly, x0 + px + pw - 2 - int(rng.integers(0, 3)), ly + 1), PAPER[0])
        c.paint(c.rect(x0 + px + pw // 2, top + py, x0 + px + pw // 2 + 2, top + py + 2), FLOWERS[0])


def tower(c, x0, ground, height, material, rng):
    """둥근 돌탑과 원뿔 지붕, 꼭대기 깃발(모험가 길드 옆)."""
    w = 40
    body_top = ground - height
    body = c.rect(x0, body_top, x0 + w, ground)
    c.paint(body, stone_wall(c.size[1], c.size[0], rng))
    rounding = np.abs(c.xx + 0.5 - (x0 + w / 2)) / (w / 2)
    c.paint(body & (rounding > 0.75) & (c.xx < x0 + w / 2), STONE[5])
    c.paint(body & (rounding > 0.7) & (c.xx >= x0 + w / 2), (c.image[..., :3] * 0.62).astype(np.uint8))
    c.paint(c.rect(x0 - 2, ground - PLINTH, x0 + w + 2, ground), STONE[3])
    c.paint(c.rect(x0 - 2, ground - PLINTH, x0 + w + 2, ground - PLINTH + 1), STONE[5])
    plain = Design("tower", 1, lit=True)
    for k, wy in enumerate((body_top + 22, body_top + 22 + STORY)):
        if wy + 30 < ground - 20:
            window(c, x0 + w // 2, wy, plain, wide=12, tall=20, arched=True, flowers=False)
    c.paint(c.rect(x0 - 3, body_top - 2, x0 + w + 3, body_top + 4), STONE[4])
    c.paint(c.rect(x0 - 3, body_top - 2, x0 + w + 3, body_top - 1), STONE[6])
    cone_h = 64
    cone_left = c.polygon([(x0 - 7, body_top), (x0 + w / 2, body_top), (x0 + w / 2, body_top - cone_h)])
    cone_right = c.polygon([(x0 + w / 2, body_top), (x0 + w + 7, body_top), (x0 + w / 2, body_top - cone_h)])
    cover(c, cone_left, material, body_top - cone_h, body_top, rng, side_light=1)
    cover(c, cone_right, material, body_top - cone_h, body_top, rng, side_light=-1)
    c.paint(c.rect(x0 - 7, body_top - 2, x0 + w + 7, body_top + 1), ROOFS[material][1])
    mast_x = x0 + w // 2
    c.paint(c.rect(mast_x - 1, body_top - cone_h - 16, mast_x + 1, body_top - cone_h + 2), IRON[3])
    c.paint(c.ellipse(mast_x, body_top - cone_h - 17, 2, 2), WARM[3])
    flag = c.polygon([(mast_x + 1, body_top - cone_h - 15), (mast_x + 16, body_top - cone_h - 11), (mast_x + 1, body_top - cone_h - 6)])
    c.paint(flag, CLOTH["blue"][2])


# ── 뉴웨이브 길드 장식 ────────────────────────────────────

def gear(c, cx, cy, radius, teeth, metal, open_spokes=True):
    """
    톱니바퀴: 이가 난 테, 바퀴살, 굴대. 빛을 받는 왼쪽 위는 밝고 오른쪽 아래는 어둡다.
    큰 바퀴는 바퀴살 사이를 비워 뒤의 벽이 보인다. metal은 어두운 쪽부터 밝은 쪽 5색.
    """
    dx, dy = c.xx + 0.5 - cx, c.yy + 0.5 - cy
    distance, angle = np.hypot(dx, dy), np.arctan2(dy, dx)
    depth = max(2, radius // 5)
    tooth = (angle / (2 * np.pi) * teeth) % 1 < 0.5
    body = distance <= np.where(tooth, radius, radius - depth)
    rim = radius - depth - max(3, radius // 5)
    hub = max(2, radius // 4)
    if open_spokes and radius >= 12:
        spoke = np.zeros_like(body)
        for k in range(3):
            a = k * np.pi / 3 + 0.3
            spoke |= np.abs(-dx * np.sin(a) + dy * np.cos(a)) <= max(1.5, radius / 10)
        body &= (distance >= rim) | (distance <= hub + 1) | spoke
    light = (-dx - dy) / (radius * 1.4)
    level = np.where(light > 0.3, 3, np.where(light < -0.3, 1, 2))
    c.image[body, :3] = metal[level[body]]
    c.image[body, 3] = 255
    c.paint(body & (np.abs(distance - rim) < 0.8), metal[0])
    c.paint(body & tooth & (distance > radius - depth) & (light > 0.1), metal[4])
    c.paint(distance <= hub, metal[3])
    c.paint(distance <= hub * 0.45, metal[0])


def newwave_emblem(c, cx, cy, radius, worn):
    """뉴웨이브 문장: 놋쇠 톱니바퀴 안 청록 원판에 흰 물결 한 줄. 버려진 집에서는 쇠가 녹슬고 색이 바랬다."""
    gear(c, cx, cy, radius, 12, RUST if worn else BRASS, open_spokes=False)
    inner = radius - max(2, radius // 5) - 3
    cloth = CLOTH["teal"]
    disc = c.ellipse(cx, cy, inner, inner)
    c.paint(disc, cloth[0] if worn else cloth[1])
    c.paint(disc & (c.xx < cx) & (c.yy < cy), cloth[1] if worn else cloth[2])
    wave_y = cy + np.sin((c.xx + 0.5 - cx) / inner * np.pi * 1.5) * inner * 0.3
    c.paint(disc & (np.abs(c.yy + 0.5 - wave_y) < 1.3), PAPER[0] if worn else PAPER[2])


def gear_works(c, cx, cy, worn):
    """벽에 단 톱니바퀴 한 쌍: 큰 놋쇠 바퀴와 오른쪽 위로 맞물린 작은 쇠 바퀴."""
    big, small = 28, 15
    reach = big + small - 4
    gear(c, cx + reach * 0.5, cy - reach * 0.866, small, 8, RUST if worn else IRON)
    gear(c, cx, cy, big, 14, RUST if worn else BRASS)


def pipe(c, x, top, bottom, worn):
    """벽을 타고 내려오는 구리 관: 이음 고리, 벽 고정 쇠, 가운데 밸브 바퀴. 버려진 집은 푸르게 삭았다."""
    metal = ROOFS["copper"][1:6] if worn else PIPE
    c.paint(c.rect(x - 2, top, x + 3, bottom), metal[2])
    c.paint(c.rect(x - 2, top, x - 1, bottom), metal[3])
    c.paint(c.rect(x - 1, top, x, bottom), metal[4])
    c.paint(c.rect(x + 2, top, x + 3, bottom), metal[1])
    for y in range(top + 10, bottom - 4, 22):
        c.paint(c.rect(x - 4, y, x + 5, y + 3), metal[3])
        c.paint(c.rect(x - 4, y + 2, x + 5, y + 3), metal[0])
        c.paint(c.rect(x + 5, y + 8, x + 8, y + 10), IRON[2])
    elbow_y = top + (bottom - top) // 2
    c.paint(c.rect(x + 3, elbow_y - 2, x + 12, elbow_y + 3), metal[2])
    c.paint(c.rect(x + 3, elbow_y - 2, x + 12, elbow_y - 1), metal[4])
    c.paint(c.rect(x + 10, elbow_y - 4, x + 13, elbow_y + 5), metal[1])
    valve = np.array([CLOTH["red"][0], CLOTH["red"][1], CLOTH["red"][2], CLOTH["red"][2], PAPER[1]])
    gear(c, x + 1, elbow_y + 16, 5, 6, valve, open_spokes=False)
    if worn:
        c.paint(c.rect(x - 1, elbow_y + 4, x + 1, bottom), ROOFS["copper"][4])


def smokestack(c, cx, top, bottom, worn):
    """쇠 굴뚝: 둥근 관에 띠 셋, 위에 넓은 테와 고깔 모자."""
    metal = RUST if worn else IRON
    c.paint(c.rect(cx - 5, top + 8, cx + 5, bottom), metal[2])
    c.paint(c.rect(cx - 5, top + 8, cx - 3, bottom), metal[3])
    c.paint(c.rect(cx + 3, top + 8, cx + 5, bottom), metal[1])
    for y in range(top + 14, bottom - 2, 9):
        c.paint(c.rect(cx - 6, y, cx + 6, y + 2), metal[1])
    c.paint(c.rect(cx - 7, top + 5, cx + 7, top + 8), metal[3])
    c.paint(c.rect(cx - 2, top + 2, cx + 2, top + 5), metal[0])
    hat = c.polygon([(cx - 9, top + 2), (cx + 9, top + 2), (cx, top - 6)])
    c.paint(hat, metal[2])
    c.paint(hat & (c.xx < cx), metal[3])


def weathervane(c, cx, top, worn):
    """지붕 꼭대기 바람개비: 쇠 막대, 작은 톱니, 화살."""
    metal = RUST if worn else IRON
    c.paint(c.rect(cx - 1, top - 26, cx + 1, top + 2), metal[3])
    gear(c, cx, top - 12, 5, 6, RUST if worn else BRASS, open_spokes=False)
    y = top - 24
    c.paint(c.rect(cx - 10, y, cx + 10, y + 1), metal[3])
    c.paint(c.polygon([(cx + 9, y - 3), (cx + 14, y), (cx + 9, y + 4)]), metal[3])
    c.paint(c.polygon([(cx - 12, y - 3), (cx - 8, y), (cx - 12, y + 4)]), metal[2])


def roof_damage(c, roof, palette, spots, rng):
    """버려진 지붕: 몇 군데 기와가 빠져 서까래가 보이고, 처마 쪽에 이끼가 낀다."""
    height, width = c.size[1], c.size[0]
    for x, y in spots:
        if not roof[y, x]:
            continue
        rx, ry = int(rng.integers(9, 13)), int(rng.integers(5, 7))
        ragged = (pg.noise(height, width, 3, rng) - 0.5) * 0.5
        hole = (((c.xx + 0.5 - x) / rx) ** 2 + ((c.yy + 0.5 - y) / ry) ** 2 <= 1 + ragged) & roof
        rim = c.ellipse(x, y, rx + 2, ry + 2) & roof & ~hole
        c.paint(rim, palette[1])
        c.paint(hole, palette[0])
        c.paint(hole & ((c.xx - x) % 6 == 0), WOOD[2])
        c.paint(hole & ((c.xx - x) % 6 == 1), WOOD[1])
        for _ in range(4):
            tx, ty = x + int(rng.integers(-rx - 6, rx + 6)), y + ry + int(rng.integers(2, 8))
            if 0 <= ty < height and 0 <= tx < width - 4 and roof[ty, tx]:
                c.paint(c.rect(tx, ty, tx + 5, ty + 2), palette[4])
                c.paint(c.rect(tx, ty + 2, tx + 5, ty + 3), palette[1])
    # 이끼: 큰 덩어리 몇 개, 가장자리만 점점이 흩어진다
    field = pg.noise(height, width, 14, rng)
    edge_dots = rng.random((height, width)) < 0.45
    moss = roof & ((field > 0.72) | ((field > 0.66) & edge_dots))
    c.paint(moss, LEAF[1])
    c.paint(moss & (field > 0.78), LEAF[2])
    c.paint(moss & (field > 0.78) & (rng.random((height, width)) < 0.25), LEAF[3])


# ── 집 한 채 ───────────────────────────────────────────

def window_centers(lo, hi, footprint):
    """lo~hi 사이에 창을 고르게 놓은 가운데 자리들."""
    count = int((hi - lo) // footprint)
    if count <= 0:
        return []
    step = (hi - lo) / count
    return [int(lo + step * (k + 0.5)) for k in range(count)]


def heights(design):
    """벽 높이와 지붕 높이(점)."""
    wall_w = design.columns * T
    if design.roof == "flat":
        return design.stories * STORY, int(min(64, 30 + wall_w * 0.12))
    roof_h = int(wall_w * 0.46) if design.roof == "front" else int(min(112, 44 + wall_w * 0.2))
    return design.stories * STORY, roof_h


def build(design):
    """설계대로 집 한 채를 그린다. 돌려주는 값: (RGBA 점 그림, 기준점(벽 아래 가운데) 점 좌표)."""
    rng = np.random.default_rng(design.seed)
    wall_w = design.columns * T
    walls_h, roof_h = heights(design)
    tower_w = 44 if design.guild else 0
    sign_w = 22 if design.sign else 0
    width = wall_w + EAVE * 2 + 2 + max(tower_w, sign_w)
    height = 120 + roof_h + walls_h + 6
    c = Canvas(width, height)
    x0, x1 = EAVE + 1, EAVE + 1 + wall_w
    ground = height - 6
    wall_top = ground - walls_h

    for story in range(design.stories):
        kind = design.upper if story > 0 and design.upper else design.walls
        y0 = ground - (story + 1) * STORY
        c.paint(c.rect(x0, y0, x1, y0 + STORY), wall_texture(kind, height, width, rng))
        if kind == "timber":
            timber_frame(c, x0, y0, wall_w, STORY)
    # 벽 양 모서리: 모서리 돌(돌·벽돌 벽) 또는 굵은 기둥(나무 벽)
    for edge_x, light in ((x0, True), (x1 - 6, False)):
        if design.walls in ("stone", "brick"):
            for k, y in enumerate(range(ground - STORY, ground, 9)):
                cw = 8 if k % 2 == 0 else 6
                qx = edge_x if light else x1 - cw
                c.paint(c.rect(qx, y, qx + cw, y + 9), STONE[5 if light else 3])
                c.paint(c.rect(qx, y + 8, qx + cw, y + 9), STONE[1])
    if design.walls == "adobe":
        # 흙벽은 아래쪽이 모래바람에 닳아 조금 어둡다.
        c.paint(c.rect(x0, ground - PLINTH, x1, ground) & (c.image[..., 3] > 0), ADOBE[2])
        c.paint(c.rect(x0, ground - PLINTH, x1, ground - PLINTH + 1), ADOBE[3])
    else:
        c.paint(c.rect(x0 - 2, ground - PLINTH, x1 + 2, ground), STONE[3])
        c.paint(c.rect(x0 - 2, ground - PLINTH, x1 + 2, ground - PLINTH + 1), STONE[5])
        c.paint(c.rect(x0 - 2, ground - 1, x1 + 2, ground), STONE[1])
        for x in range(x0 + 4, x1, 11):
            c.paint(c.rect(x, ground - PLINTH + 1, x + 1, ground), STONE[1])
    if design.stories > 1:
        floor_y = ground - STORY
        c.paint(c.rect(x0 - 3, floor_y - 3, x1 + 3, floor_y + 3), WOOD[2])
        c.paint(c.rect(x0 - 3, floor_y - 3, x1 + 3, floor_y - 2), WOOD[4])
        c.paint(c.rect(x0 - 3, floor_y + 2, x1 + 3, floor_y + 3), WOOD[0])
        for x in range(x0 + 6, x1 - 4, 16):
            c.paint(c.rect(x, floor_y + 3, x + 4, floor_y + 6), WOOD[1])

    if design.ivy:
        ivy(c, x0, wall_top + 10, int(wall_w * 0.3), walls_h - 12, rng)
    if design.ivy and design.worn:
        ivy(c, x1 - int(wall_w * 0.22), wall_top + 30, int(wall_w * 0.22), walls_h - 32, rng)

    # 뉴웨이브 길드는 왼쪽 끝에 구리 관, 오른쪽 끝에 톱니바퀴가 붙어 창을 그만큼 비킨다
    left_reserve, right_reserve = (16, 60) if design.gears else (0, 0)

    # 1층: 문, 문 양쪽의 장식 자리, 남는 벽에 창
    door_cx = int(x0 + wall_w * design.door_at)
    door_left, door_right = door(c, door_cx, ground, design.door, design.cloth)
    left_clear, right_clear = door_left - 6, door_right + 6
    if design.lanterns:
        lantern(c, door_left - 8, ground - STORY + 12, not design.worn)
        lantern(c, door_right + 7, ground - STORY + 12, not design.worn)
        left_clear, right_clear = left_clear - 12, right_clear + 12
    if design.guild:
        quest_board(c, left_clear - 44, ground, rng)
        left_clear -= 50
    ground_top = ground - STORY + 16
    footprint = window_footprint(design)
    left_windows = window_centers(x0 + 3 + left_reserve, left_clear, footprint)
    for cx in left_windows + window_centers(right_clear, x1 - 3 - right_reserve, footprint):
        window(c, cx, ground_top, design, arched=design.arched_windows)
    if design.awning and left_windows:
        awning(c, x0 + 3, left_clear + 4, ground_top - 9, design.awning)

    # 2층: 발코니가 있으면 가운데에 발코니 문과 깃발, 나머지는 창
    if design.stories > 1:
        upper_top = ground - 2 * STORY + 14
        upper_footprint = window_footprint(design, upper=True)
        if design.balcony:
            window(c, door_cx, upper_top - 2, design, wide=22, tall=STORY - 20, shutters="", flowers=False)
            centers = window_centers(x0 + 4, door_cx - 46, upper_footprint) + window_centers(door_cx + 46, x1 - 4, upper_footprint)
        elif design.gears:
            # 가운데는 톱니 테를 두른 둥근 창
            gear(c, door_cx, upper_top + 16, 17, 12, RUST if design.worn else BRASS, open_spokes=False)
            porthole = c.ellipse(door_cx, upper_top + 16, 11, 11)
            c.paint(porthole, (WARM if design.lit else GLASS_DARK if design.worn else GLASS)[1])
            c.paint(porthole & (c.yy > upper_top + 16), (WARM if design.lit else GLASS_DARK if design.worn else GLASS)[2])
            c.paint(porthole & ((np.abs(c.xx + 0.5 - door_cx) < 1) | (np.abs(c.yy + 0.5 - upper_top - 16) < 1)), IRON[1])
            centers = window_centers(x0 + 4 + left_reserve, door_cx - 26, upper_footprint) + \
                window_centers(door_cx + 26, x1 - 4 - right_reserve, upper_footprint)
        else:
            centers = window_centers(x0 + 4, x1 - 4, upper_footprint)
        for cx in centers:
            window(c, cx, upper_top, design)
        if design.guild:
            banner(c, door_cx - 37, upper_top - 8, "blue")
            banner(c, door_cx + 23, upper_top - 8, "blue")
        if design.balcony:
            floor_y = ground - STORY
            bx0, bx1 = door_cx - 44, door_cx + 44
            c.paint(c.rect(bx0 - 2, floor_y - 3, bx1 + 2, floor_y + 5), WOOD[2])
            c.paint(c.rect(bx0 - 2, floor_y - 3, bx1 + 2, floor_y - 2), WOOD[5])
            for x in range(bx0, bx1, 6):
                c.paint(c.rect(x, floor_y - 20, x + 3, floor_y - 3), WOOD[3])
                c.paint(c.rect(x, floor_y - 20, x + 1, floor_y - 3), WOOD[5])
            c.paint(c.rect(bx0 - 2, floor_y - 23, bx1 + 2, floor_y - 19), WOOD[4])
            c.paint(c.rect(bx0 - 2, floor_y - 19, bx1 + 2, floor_y - 18), WOOD[1])
            if design.guild:
                guild_plaque(c, door_cx, floor_y - 17)

    if design.sign:
        hanging_sign(c, x1, ground - STORY + 12, design.sign)
    if design.gears:
        pipe(c, x0 + 9, wall_top + 2, ground - PLINTH, design.worn)
        gear_works(c, x1 - 33, ground - STORY + 8, design.worn)
        # 문 가운데 놋쇠 톱니 문고리
        gear(c, door_cx, ground - 6 - DOORS[design.door][1] + 30, 6, 8, RUST if design.worn else BRASS, open_spokes=False)

    # 처마 그늘(벽 윗부분)
    shade = c.rect(x0, wall_top, x1, wall_top + 12) & (c.image[..., 3] > 0)
    factor = np.clip(0.5 + (c.yy - wall_top) * 0.045, 0.5, 1.0)
    c.image[shade, :3] = (c.image[shade, :3] * factor[shade][:, None]).astype(np.uint8)

    upper_walls = design.upper or design.walls
    if design.roof == "flat":
        flat_roof(c, x0, x1, wall_top, roof_h, design, rng)
        if design.ladder:
            ladder(c, x1 - 22, ground - 2, wall_top + 2)
    elif design.roof == "front":
        front_gable_roof(c, x0, x1, wall_top + 6, roof_h, design.roof_material, rng, upper_walls, design.gable, design.worn)
    else:
        side_gable_roof(c, x0, x1, wall_top + 6, roof_h, design.roof_material, rng)
        roof = (c.yy < wall_top + 4) & (c.image[..., 3] > 0)
        if design.worn:
            spots = [(int(x0 + wall_w * fx), int(wall_top + 6 - roof_h * fy)) for fx, fy in ((0.14, 0.5), (0.86, 0.35), (0.76, 0.72))]
            roof_damage(c, roof, ROOFS[design.roof_material], spots, rng)
        if design.roof == "cross":
            wing = int(min(wall_w * 0.46, 6 * T))
            front_gable_roof(c, door_cx - wing // 2 + EAVE, door_cx + wing // 2 - EAVE, wall_top + 12, int(wing * 0.52),
                             design.roof_material, rng, upper_walls, design.gable, design.worn)
    if design.dormers:
        if design.roof == "cross":
            spots = [0.13, 0.87] if design.dormers == 2 else [0.13]
        else:
            spots = [(k + 1) / (design.dormers + 1) for k in range(design.dormers)]
        for spot in spots:
            dormer(c, int(x0 + wall_w * spot), wall_top - roof_h // 2 + 6, design.roof_material, design, rng)
    if design.chimney_at >= 0:
        chimney_x = int(x0 + wall_w * design.chimney_at)
        surface = c.first_row(chimney_x)
        chimney(c, chimney_x, surface - 18, surface + 12)
    if design.gears:
        for spot in (0.16, 0.84):
            stack_x = int(x0 + wall_w * spot)
            surface = c.first_row(stack_x)
            smokestack(c, stack_x, surface - 30, surface + 12, design.worn)
        weathervane(c, door_cx, c.first_row(door_cx), design.worn)
    if design.guild:
        tower(c, x1 + 4, ground, walls_h + 40, design.roof_material, rng)

    shadow_band = c.rect(x0 - 8, ground, x1 + 8 + tower_w, ground + 4) & (c.image[..., 3] == 0)
    c.image[shadow_band] = [20, 26, 18, 110]
    image = outline(c.image)
    rows = np.nonzero(image[..., 3].any(axis=1))[0]
    image = image[rows.min():]
    return image, (x0 + wall_w // 2, image.shape[0] - 6)


# ── 집터 ───────────────────────────────────────────────

PLOT_SIDE = 14        # 처마 끝 밖으로 집터가 더 나간 점 수
PLOT_FRONT = {"dirt": 40, "cobble": 60, "sand": 36, "paved": 20}   # 벽 앞 마당 깊이(점)


def plot(design):
    """
    집터: 집 밑과 앞마당에 까는 바닥 그림. 맵 바닥 위, 인물·소품 아래에 깔려 다른 바닥과 겹쳐도 된다.
    집이 가리는 뒤쪽까지 깔아 두어 옆에서 보면 집이 마당 위에 선 것처럼 보인다.
    가장자리는 맵 흙길과 같은 방식(흙 테두리 한 줄, 바깥 풀 그늘, 넘어온 풀잎)이라 맵 풀밭과 이어진다.
    돌려주는 값: (RGBA 점 그림, 기준점(집과 같은 벽 아래 가운데) 점 좌표).
    """
    import terrain
    rng = np.random.default_rng(design.seed + 500)
    wall_w = design.columns * T
    walls_h, roof_h = heights(design)
    tower_w = 44 if design.guild else 0
    margin = 2
    left = EAVE + PLOT_SIDE
    width = left * 2 + wall_w + tower_w + margin * 2
    back = walls_h + int(roof_h * 0.6)
    height = back + PLOT_FRONT[design.plot] + margin * 2
    x0 = margin + left
    ground = margin + back
    yy, xx = np.mgrid[0:height, 0:width]

    # 둥근 네모에 잡음을 얹은 모양
    cx, cy = (width - 1) / 2, (height - 1) / 2
    half_w, half_h = width / 2 - margin - 1, height / 2 - margin - 1
    radius = 14
    dx = np.maximum(0, np.abs(xx - cx) - (half_w - radius))
    dy = np.maximum(0, np.abs(yy - cy) - (half_h - radius))
    wobble = (pg.noise(height, width, 7, rng) - 0.5) * 6
    area = np.hypot(dx, dy) <= radius + wobble - 3

    image = np.zeros((height, width, 4), dtype=np.uint8)
    rgb = terrain.cobble(height, width, rng) if design.plot == "cobble" else terrain.sand(height, width, rng) if design.plot == "sand" \
        else terrain.dirt(height, width, rng)
    door_cx = int(x0 + wall_w * design.door_at)
    door_w = DOORS[design.door][0]
    if design.plot == "dirt":
        # 꽃밭: 벽 앞 문 양쪽에 검은 흙 띠, 잎과 꽃, 앞에 작은 돌을 줄지어 둘렀다
        for bx0, bx1 in ((x0 + 4, door_cx - door_w // 2 - 12), (door_cx + door_w // 2 + 12, x0 + wall_w - 4)):
            if bx1 - bx0 < 12 or not design.flowers:
                continue
            bed = (xx >= bx0) & (xx < bx1) & (yy >= ground + 3) & (yy < ground + 11)
            rgb[bed] = pg.DIRT[0]
            for _ in range((bx1 - bx0) * 2):
                fx, fy = int(rng.integers(bx0, bx1 - 1)), int(rng.integers(ground + 3, ground + 10))
                rgb[fy, fx] = LEAF[int(rng.integers(1, 4))]
                if rng.random() < 0.3:
                    rgb[fy, fx] = FLOWERS[int(rng.integers(0, len(FLOWERS)))]
            for sx in range(bx0, bx1 - 2, 4):
                rgb[ground + 11:ground + 13, sx:sx + 3] = STONE[4]
                rgb[ground + 13, sx:sx + 3] = STONE[2]
        # 문에서 마당 끝까지 디딤돌
        y = ground + 9
        while y < height - margin - 6:
            sx = door_cx + int(rng.integers(-2, 3))
            stone = ((xx - sx) / 8) ** 2 + ((yy - y) / 4) ** 2 <= 1
            rgb[stone] = STONE[4]
            rgb[stone & (yy < y - 1)] = STONE[5]
            rgb[stone & (yy > y + 2)] = STONE[2]
            y += 11
    elif design.plot == "cobble":
        # 돌을 깐 앞뜰: 벽을 따라 넓은 판석 한 줄
        slab = (yy >= ground) & (yy < ground + 8) & (xx >= x0 - 6) & (xx < x0 + wall_w + 6)
        rgb[slab] = STONE[4]
        rgb[slab & ((xx - x0) % 16 == 0)] = STONE[2]
        rgb[slab & (yy == ground + 7)] = STONE[2]
    # 벽 바로 앞 낙숫물 자갈
    drip = (yy >= ground) & (yy < ground + 3) & (xx >= x0 - 4) & (xx < x0 + wall_w + tower_w + 4)
    gravel = rng.random((height, width)) > 0.5
    rgb[drip & gravel] = STONE[3]
    rgb[drip & ~gravel] = STONE[2]

    if design.worn:
        # 버려진 마당: 풀이 덮어 흙은 얼룩으로만 남았다
        overgrown = pg.noise(height, width, 9, rng) > 0.45
        grass = terrain.grass(height, width, rng)
        rgb[overgrown] = grass[overgrown]
    image[area, :3] = rgb[area]
    image[area, 3] = 255
    # 가장자리: 안쪽 한 줄은 어둡게, 바깥 한 줄은 짙은 풀(맵 흙길 경계와 같다), 풀잎이 조금 넘어온다
    edge = area & pg.neighbours(~area)
    if design.plot in ("sand", "paved"):
        # 모래 마당과 돌바닥 앞뜰은 둘레 바닥과 그대로 이어져 테두리를 두르지 않는다.
        return image, (x0 + wall_w // 2, ground)
    image[edge, :3] = pg.STONE[1] if design.plot == "cobble" else pg.DIRT[1]
    rim = ~area & pg.neighbours(area)
    image[rim, :3] = pg.GRASS[1]
    image[rim, 3] = 255
    ys, xs = np.nonzero(edge)
    for y, x, roll in zip(ys, xs, rng.random(len(ys))):
        if roll < 0.22:
            image[y, x, :3] = pg.GRASS[4]
    return image, (x0 + wall_w // 2, ground)


def outline(image):
    alpha = image[..., 3] == 255
    ring = pg.neighbours(alpha) & ~alpha
    out = image.copy()
    out[ring, :3] = OUTLINE
    out[ring, 3] = 255
    return out


# ── 견본 ───────────────────────────────────────────────

CATALOG = [
    Design("cottage_red", 5, roof_material="clay", walls="timber", shutters="green", seed=3),
    Design("cottage_thatch", 4, roof="front", roof_material="thatch", walls="planks", shutters="wood", door_at=0.3, ivy=True, chimney_at=0.8,
           seed=5),
    Design("stone_house", 5, roof_material="slate", walls="stone", door="arched", shutters="blue", dormers=1, chimney_at=0.2, seed=7),
    Design("narrow_house", 3, stories=2, roof="front", roof_material="plum", walls="brick", upper="timber", shutters="blue",
           chimney_at=0.85, seed=17),
    Design("longhouse", 7, roof_material="thatch", walls="timber", shutters="wood", door_at=0.3, ivy=True, chimney_at=0.85, seed=19),
    Design("townhouse", 5, stories=2, roof_material="clay", walls="stone", upper="timber", shutters="red", door_at=0.3,
           arched_windows=True, dormers=2, seed=9),
    Design("bakery", 6, roof="cross", roof_material="clay", walls="brick", door="arched", door_at=0.62, awning="red", sign="shop",
           shutters="", lanterns=True, chimney_at=0.15, seed=11),
    Design("farmhouse", 6, roof="front", roof_material="moss", walls="planks", shutters="green", door_at=0.35, ivy=True, seed=15),
    Design("inn", 8, stories=2, roof="cross", roof_material="clay", walls="stone", upper="timber", door="double", sign="inn",
           arched_windows=True, lit=True, lanterns=True, balcony=True, shutters="green", plot="cobble", seed=13),
    Design("adventurers_guild", 12, stories=2, roof="cross", roof_material="slate", walls="stone", door="double",
           shutters="blue", arched_windows=True, lanterns=True, gable="crest", guild=True, balcony=True, dormers=2, lit=True,
           chimney_at=0.75, plot="cobble", seed=21),
    Design("newwave_guild", 10, stories=2, roof="cross", roof_material="copper", walls="brick", upper="planks", door="double",
           shutters="", lanterns=True, gable="gear", gears=True, sign="gear", lit=True, chimney_at=-1, seed=51),
    Design("sand_house", 5, roof="flat", walls="adobe", door="curtain", cloth="red", shutters="wood", flowers=False, chimney_at=-1,
           ladder=True, plot="sand", seed=61),
    Design("sand_shop", 6, roof="flat", walls="adobe", door="curtain", cloth="blue", shutters="", flowers=False, chimney_at=-1,
           awning="blue", door_at=0.65, plot="sand", seed=63),
    Design("newwave_guild_old", 10, stories=2, roof="cross", roof_material="copper", walls="brick", upper="planks", door="double",
           shutters="", lanterns=True, gable="gear", gears=True, worn=True, ivy=True, chimney_at=-1, seed=51),
]


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Houses"
    out.mkdir(parents=True, exist_ok=True)
    built, meta = [], []
    for design in CATALOG:
        image, pivot = build(design)
        yard, yard_pivot = plot(design)
        Image.fromarray(pg.upscale(image), "RGBA").save(out / f"{design.name}.png")
        Image.fromarray(pg.upscale(yard), "RGBA").save(out / f"{design.name}_plot.png")
        meta.append({"id": design.name, "width": image.shape[1], "height": image.shape[0], "pivotX": pivot[0], "pivotY": pivot[1],
                     "plotWidth": yard.shape[1], "plotHeight": yard.shape[0], "plotPivotX": yard_pivot[0], "plotPivotY": yard_pivot[1]})
        built.append((design.name, image, pivot, yard, yard_pivot))
    (out / "houses.json").write_text(json.dumps({"houses": meta}, indent=1), encoding="utf-8")
    catalog(project, built)


def catalog(project, built):
    """견본 모음: 풀밭 위에 집을 줄지어 세우고, 첫 줄 끝에 주인공을 세워 크기를 비교한다."""
    import terrain
    rng = np.random.default_rng(2)
    rows = [built[0:5], built[5:8], built[8:10], built[10:]]
    gap = 30
    front = PLOT_FRONT["cobble"] + 8
    width = max(sum(entry[3].shape[1] for entry in row) + gap * (len(row) + 1) for row in rows) + 60
    row_heights = [max(entry[1].shape[0] for entry in row) + front for row in rows]
    ground = terrain.grass(sum(row_heights), width, rng)
    scene = Image.fromarray(np.dstack([ground, np.full(ground.shape[:2], 255, np.uint8)]), "RGBA")
    y = 0
    feet = []
    for row, h in zip(rows, row_heights):
        x = gap
        foot_y = y + h - front
        for _, img, pivot, yard, yard_pivot in row:
            foot_x = x + yard_pivot[0]
            scene.alpha_composite(Image.fromarray(yard, "RGBA"), (foot_x - yard_pivot[0], foot_y - yard_pivot[1]))
            scene.alpha_composite(Image.fromarray(img, "RGBA"), (foot_x - pivot[0], foot_y - pivot[1]))
            feet.append((foot_x, foot_y))
            x += yard.shape[1] + gap
        y += h
    scene = scene.resize((scene.width * 2, scene.height * 2), Image.NEAREST)
    hero = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/hero/down_0.png").convert("RGBA")
    # 첫 집 문 앞 마당에 세운다(그림 아래 끝 = 발밑)
    door_x = feet[0][0] + int(CATALOG[0].columns * T * (CATALOG[0].door_at - 0.5))
    scene.alpha_composite(hero, ((door_x + 34) * 2 - hero.width // 2, (feet[0][1] + 30) * 2 - hero.height + 8))
    path = project / "Docs/ArtUpgrade/PixelGen/houses_catalog.png"
    scene.save(path)
    print(path)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
