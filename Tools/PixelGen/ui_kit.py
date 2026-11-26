"""UI 키트의 부품 그림(Docs/UI_UX_GUIDE.md 5장). 모든 창이 이 부품을 함께 쓴다.

색은 지침의 표(웹게임 표지 「달빛 뱀열차」에서 뽑은 색)를 따른다.
가상 px 하나 = 2점으로 그린다(Unity에서 PPU 200, 점 필터). 늘려 쓰는 부품은 9-slice 테두리를 적어 둔다.

  Assets/MiniPandoras/Art/PixelGen/UI/Kit/frame.png   창 틀(32 px, 테두리 8). 밤하늘 판, 밝은 윗선과 어두운 아랫선, 깎은 모서리, 모서리의 등불 못
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/well.png    오목한 판(12 px, 테두리 3). 칸과 카드의 바탕. 윗선이 어둡고 아랫선이 밝아 파인 것처럼 보인다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/focus.png   고른 것의 테(12 px, 테두리 3). 등불색 한 줄, 가운데는 비었다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/well_focus.png 고른 오목한 판(12 px, 테두리 3). 바깥선이 등불색인 오목한 판. 그림을 바꿔 끼우는 목록용
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/button.png  버튼 판(16 px, 테두리 4). 회색조라서 Image 색으로 물들인다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/close.png   닫기 아이콘(12 px). 누르는 영역은 버튼 쪽에서 26 px로 둔다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/arrow.png   왼쪽을 가리키는 꺾쇠 아이콘(12 px). 오른쪽은 Image를 가로로 뒤집어 쓴다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/plate.png   작은 판(16 px, 테두리 4). 화면 위에 늘 떠 있는 HUD(체력 칸)의 판. 창 틀을 줄인 모양에 안쪽 가는 선
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/trough.png  막대 홈(6 px, 테두리 2). 밤색 테두리 안이 파였고 윗줄에 그늘이 진다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/bar_fill.png 막대 채움(4 px, 위아래 테두리 1). 회색조라서 Image 색으로 물들인다. 윗줄이 밝고 아랫줄이 어둡다
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/icon_heart.png 체력 아이콘(7 px). 붉은 하트
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/icon_mana.png  마나 아이콘(5 px). 푸른 마름모
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/menu.png    메뉴 아이콘(12 px). 가로줄 셋
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/skip.png    건너뛰기 아이콘(12 px). 오른쪽을 가리키는 겹 세모
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/profile.png 프로필 아이콘(12 px). 머리와 어깨
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/settings.png 설정 아이콘(12 px). 톱니바퀴
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/hud_book.png 필드 HUD의 봉인서 아이콘(16 px). 금 장식의 붉은 가죽 책(판 없이 떠 있는 HUD, 2026-11-22 참고 그림)
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/hud_map.png  필드 HUD의 지도 아이콘(16 px). 세 번 접힌 양피지 지도
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/hud_gear.png 필드 HUD의 메뉴 아이콘(16 px). 금빛 톱니바퀴
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/minimap_ring.png 미니맵 테두리(46 px). 밤색 선 사이의 금빛 띠, 안은 비었다(2026-11-23 참고 그림)
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/minimap_disk.png 미니맵의 둥근 창(46 px). 맵 그림을 자르는 밤색 원
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/minimap_player.png / minimap_player_diagonal.png 미니맵의 주인공(7 px). 위·왼쪽 위를 가리키는 금빛 세모
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/minimap_goal.png 미니맵의 목적지(4×9 px). 금빛 「!」
  Assets/MiniPandoras/Art/PixelGen/UI/Kit/minimap_person.png / minimap_enemy.png / minimap_exit.png 미니맵의 점(5 px). 사람은 흰색, 몬스터는 붉은색, 출구는 푸른색
  Docs/ArtUpgrade/PixelGen/vitals_preview.png         체력 칸 미리보기(판, 얼굴 자리, 두 막대, 아이콘. 네 배)

사용: python Tools/PixelGen/ui_kit.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

D = 2                       # 가상 px 하나 = 2점
NIGHT = (8, 20, 33)         # 밤
SKY = (23, 42, 73)          # 밤하늘
SKY_LIGHT = (58, 75, 114)   # 판의 밝은 윗선
SKY_DARK = (13, 24, 40)     # 판의 어두운 아랫선
INNER = (37, 53, 85)        # 밤하늘 밝은
WELL = (15, 29, 51)         # 오목한 판의 바닥
MOON = (243, 233, 216)      # 달빛
LANTERN = (222, 175, 118)   # 등불
LANTERN_LIGHT = (247, 212, 173)
LANTERN_DARK = (138, 106, 62)
WARNING = (200, 81, 61)     # 경고(체력 하트)
WARNING_LIGHT = (236, 140, 112)
MANA = (106, 168, 232)      # 마나(전투 화면의 마나 막대와 같은 색)
MANA_LIGHT = (196, 226, 250)


def blank(width, height):
    return np.zeros((height * D, width * D, 4), np.uint8)


def fill(rgba, x, y, w, h, color):
    """가상 px 단위 사각형을 칠한다."""
    rgba[y * D:(y + h) * D, x * D:(x + w) * D] = (*color, 255)


def plate(width, height, top, bottom, body, outline):
    """깎은 모서리 판: 바깥 먹선 1 px, 윗·왼쪽 밝은 선, 아래·오른쪽 어두운 선, 모서리는 1 px 깎는다."""
    rgba = blank(width, height)
    fill(rgba, 1, 0, width - 2, height, outline)
    fill(rgba, 0, 1, width, height - 2, outline)
    fill(rgba, 1, 1, width - 2, height - 2, body)
    fill(rgba, 2, 1, width - 4, 1, top)
    fill(rgba, 1, 2, 1, height - 4, top)
    fill(rgba, 2, height - 2, width - 4, 1, bottom)
    fill(rgba, width - 2, 2, 1, height - 4, bottom)
    return rgba


def frame():
    """창 틀 32×32 px, 테두리 8 px. 안쪽 4 px 자리에 가는 선을 한 번 더 두르고, 네 모서리에 등불 못을 박는다."""
    size = 32
    rgba = plate(size, size, SKY_LIGHT, SKY_DARK, SKY, NIGHT)
    inset = 4
    fill(rgba, inset, inset, size - inset * 2, 1, INNER)
    fill(rgba, inset, size - inset - 1, size - inset * 2, 1, INNER)
    fill(rgba, inset, inset, 1, size - inset * 2, INNER)
    fill(rgba, size - inset - 1, inset, 1, size - inset * 2, INNER)
    for x, y in ((2, 2), (size - 4, 2), (2, size - 4), (size - 4, size - 4)):
        fill(rgba, x, y, 2, 2, LANTERN)
        rgba[y * D, x * D] = (*LANTERN_LIGHT, 255)
        rgba[(y + 2) * D - 1, (x + 2) * D - 1] = (*LANTERN_DARK, 255)
    return rgba


def well():
    """오목한 판 12×12 px, 테두리 3 px. 판의 바깥선과 반대로 위·왼쪽이 어둡고 아래·오른쪽이 밝다."""
    return plate(12, 12, NIGHT, INNER, WELL, NIGHT)


def well_focus():
    """고른 오목한 판 12×12 px, 테두리 3 px. 오목한 판의 바깥선을 등불색으로 바꾼 것. 그림 하나를 바꿔 끼워 고른 줄을 보이는 목록이 쓴다."""
    return plate(12, 12, NIGHT, INNER, WELL, LANTERN)


def focus():
    """고른 것의 테 12×12 px, 테두리 3 px. 등불색 한 줄에 모서리를 깎고, 안쪽 모서리에 밝은 점을 찍는다."""
    size = 12
    rgba = blank(size, size)
    fill(rgba, 1, 0, size - 2, 1, LANTERN)
    fill(rgba, 1, size - 1, size - 2, 1, LANTERN)
    fill(rgba, 0, 1, 1, size - 2, LANTERN)
    fill(rgba, size - 1, 1, 1, size - 2, LANTERN)
    for x, y in ((1, 1), (size - 2, 1), (1, size - 2), (size - 2, size - 2)):
        fill(rgba, x, y, 1, 1, LANTERN_LIGHT)
    return rgba


def button():
    """버튼 판 16×16 px, 테두리 4 px. 흰 판이라 Image 색이 곧 버튼 색이 되고, 윗선은 밝게 아랫선은 어둡게 남는다."""
    return plate(16, 16, (255, 255, 255), (150, 150, 150), (215, 215, 215), (40, 40, 40))


def icon(ink):
    """아이콘 모양(참인 칸)을 달빛으로 칠하고 둘레 1 px를 밤색 테두리로 두른다. 밝은 그림 위에서도 보인다."""
    size = ink.shape[0]
    outline = np.zeros_like(ink)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            outline |= np.roll(np.roll(ink, dy, 0), dx, 1)
    rgba = blank(size, size)
    for y in range(size):
        for x in range(size):
            if ink[y, x]:
                fill(rgba, x, y, 1, 1, MOON)
            elif outline[y, x]:
                fill(rgba, x, y, 1, 1, NIGHT)
    return rgba


def close():
    """닫기 아이콘 12×12 px: 달빛 X에 먹색 테두리. 글꼴의 × 기호 대신 쓴다."""
    size = 12
    ink = np.zeros((size, size), bool)
    for i in range(2, size - 2):
        ink[i, i] = ink[i, min(i + 1, size - 1)] = True
        j = size - 1 - i
        ink[i, j] = ink[i, max(j - 1, 0)] = True
    return icon(ink)


def arrow():
    """꺾쇠 아이콘 12×12 px: 왼쪽을 가리키는 굵기 2 px의 「<」. 글꼴의 ◀ 기호 대신 쓴다."""
    size = 12
    ink = np.zeros((size, size), bool)
    for y in range(1, size - 1):
        x = 3 + int(abs(y - 5.5))
        ink[y, x] = ink[y, x + 1] = True
    return icon(ink)


def menu_icon():
    """메뉴 아이콘 12×12 px: 달빛 가로줄 셋(굵기 2 px)에 밤색 테두리. 「메뉴」 글자 버튼 대신 쓴다."""
    size = 12
    ink = np.zeros((size, size), bool)
    for y in (2, 5, 8):
        ink[y:y + 2, 2:size - 2] = True
    return icon(ink)


def skip_icon():
    """건너뛰기 아이콘 12×12 px: 오른쪽을 가리키는 겹 세모. 글꼴의 ▶▶ 기호 대신 쓴다."""
    size = 12
    ink = np.zeros((size, size), bool)
    for left in (1, 6):
        for y in range(2, 10):
            reach = 4 - abs(y - 5.5) + 0.5
            ink[y, left:left + int(reach) + 1] = True
    return icon(ink)


def ink_rows(rows):
    """글자 그림(#가 칠할 칸)을 아이콘 모양으로."""
    return np.array([[key == "#" for key in row] for row in rows], bool)


def profile_icon():
    """프로필 아이콘 12×12 px: 머리와 어깨. 「프로필」 글자 버튼 대신 쓴다."""
    return icon(ink_rows([
        "............",
        "....####....",
        "...######...",
        "...######...",
        "...######...",
        "....####....",
        "............",
        "...######...",
        "..########..",
        "..########..",
        "..########..",
        "............",
    ]))


def settings_icon():
    """설정 아이콘 12×12 px: 이 여덟 개의 톱니바퀴, 가운데가 뚫렸다. 「설정」 글자 버튼 대신 쓴다."""
    return icon(ink_rows([
        "............",
        ".....##.....",
        "..#.####.#..",
        "..########..",
        "..###..###..",
        ".###....###.",
        ".###....###.",
        "..###..###..",
        "..########..",
        "..#.####.#..",
        ".....##.....",
        "............",
    ]))


def hud_plate():
    """작은 판 16×16 px, 테두리 4 px. 창 틀처럼 깎은 모서리와 밝은 윗선·어두운 아랫선이고, 안쪽 2 px 자리에 가는 선을 한 번 더 두른다.
    맵 위에 늘 떠 있으니 반투명이 아니라 불투명한 판이다(지침 2장: 어두운 유리판을 쓰지 않는다)."""
    size = 16
    rgba = plate(size, size, SKY_LIGHT, SKY_DARK, SKY, NIGHT)
    inset = 2
    fill(rgba, inset + 1, inset, size - (inset + 1) * 2, 1, INNER)
    fill(rgba, inset + 1, size - inset - 1, size - (inset + 1) * 2, 1, INNER)
    fill(rgba, inset, inset + 1, 1, size - (inset + 1) * 2, INNER)
    fill(rgba, size - inset - 1, inset + 1, 1, size - (inset + 1) * 2, INNER)
    return rgba


def trough():
    """막대 홈 6×6 px, 테두리 2 px. 밤색 테두리 안은 오목한 판의 바닥색이고, 윗줄은 그늘이 져서 파인 것처럼 보인다.
    높이 4인 막대(마나)도 테두리·그늘·바닥·테두리 네 줄이 다 보인다."""
    size = 6
    rgba = blank(size, size)
    fill(rgba, 0, 0, size, size, NIGHT)
    fill(rgba, 1, 1, size - 2, size - 2, WELL)
    fill(rgba, 1, 1, size - 2, 1, SKY_DARK)
    return rgba


def bar_fill():
    """막대 채움 4×4 px, 위아래 테두리 1 px. 회색조(위 255, 가운데 220, 아래 170)라서 Image 색으로 물들인다.
    채움이 2 px뿐인 막대에서는 밝은 윗줄과 어두운 아랫줄만 남는다."""
    rgba = blank(4, 4)
    for row, level in enumerate((255, 220, 220, 170)):
        fill(rgba, 0, row, 4, 1, (level, level, level))
    return rgba


def pixel_icon(rows, palette):
    """글자 그림(한 글자 = 한 px)을 색으로 칠한다. 점(.)은 비운다."""
    height, width = len(rows), len(rows[0])
    rgba = blank(width, height)
    for y, row in enumerate(rows):
        for x, key in enumerate(row):
            if key != ".":
                fill(rgba, x, y, 1, 1, palette[key])
    return rgba


def icon_heart():
    """체력 아이콘 7×7 px: 밤색 테두리의 붉은 하트, 왼쪽 위에 빛."""
    return pixel_icon([
        ".##.##.",
        "#lr#rr#",
        "#lrrrr#",
        "#rrrrr#",
        ".#rrr#.",
        "..#r#..",
        "...#...",
    ], {"#": NIGHT, "r": WARNING, "l": WARNING_LIGHT})


def icon_mana():
    """마나 아이콘 5×5 px: 밤색 테두리의 푸른 마름모, 왼쪽 위에 빛."""
    return pixel_icon([
        "..#..",
        ".#l#.",
        "#lbb#",
        ".#b#.",
        "..#..",
    ], {"#": NIGHT, "b": MANA, "l": MANA_LIGHT})


LEATHER = (156, 46, 48)
LEATHER_LIGHT = (204, 86, 74)
LEATHER_DARK = (98, 26, 34)
PAGE = (232, 220, 192)       # 양피지
PAGE_SHADE = (196, 174, 132)
INK_GREEN = (88, 132, 70)
INK_BLUE = (78, 122, 168)
GOLD_DARK = (150, 104, 44)


def outlined(colors):
    """색 판(None은 빈칸)에 밤색 바깥선을 두른다. 판 없이 맵 위에 떠 있어도 읽히게 하는 선이다."""
    height, width = len(colors), len(colors[0])
    rgba = blank(width, height)
    for y in range(height):
        for x in range(width):
            if colors[y][x] is not None:
                fill(rgba, x, y, 1, 1, colors[y][x])
            elif any(0 <= y + dy < height and 0 <= x + dx < width and colors[y + dy][x + dx] is not None
                     for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                fill(rgba, x, y, 1, 1, NIGHT)
    return rgba


def hud_book():
    """봉인서 아이콘 16×16 px: 왼쪽에 등이 있는 붉은 가죽 책. 오른쪽과 아래로 양피지 쪽이 비치고, 표지 가운데에 금 마름모, 모서리에 금 장식."""
    size = 16
    colors = [[None] * size for _ in range(size)]
    for y in range(2, 15):
        for x in range(4, 15):
            colors[y][x] = PAGE_SHADE if (x + y) % 3 == 0 and x > 12 else PAGE          # 비치는 쪽(오른쪽·아래)
    for y in range(1, 14):
        for x in range(1, 13):
            if x <= 2:
                colors[y][x] = LEATHER_DARK                                             # 책등
            elif y <= 2 or x == 3:
                colors[y][x] = LEATHER_LIGHT                                            # 빛 받는 윗변·등 옆
            elif y >= 12 or x >= 11:
                colors[y][x] = LEATHER_DARK
            else:
                colors[y][x] = LEATHER
    for (x, y) in ((11, 1), (12, 1), (12, 2), (11, 13), (12, 13), (12, 12)):
        colors[y][x] = LANTERN                                                          # 모서리 장식
    for (x, y), c in {(7, 5): LANTERN_LIGHT, (6, 6): LANTERN_LIGHT, (7, 6): LANTERN, (8, 6): LANTERN,
                      (5, 7): LANTERN, (6, 7): LANTERN, (7, 7): GOLD_DARK, (8, 7): LANTERN, (9, 7): GOLD_DARK,
                      (6, 8): LANTERN, (7, 8): GOLD_DARK, (8, 8): GOLD_DARK, (7, 9): GOLD_DARK}.items():
        colors[y][x] = c                                                                # 가운데 금 마름모
    for y in (4, 10):
        colors[y][1] = LANTERN                                                          # 등의 금 띠
        colors[y][2] = GOLD_DARK
    return outlined(colors)


def hud_map():
    """지도 아이콘 16×16 px: W자로 세 번 접힌 양피지 지도. 왼쪽을 보는 칸은 밝고 오른쪽을 보는 칸은 그늘지며, 풀빛 땅과 푸른 물길, 붉은 표시가 있다."""
    size = 16
    colors = [[None] * size for _ in range(size)]
    # 접힌 선의 위 끝 높이: (x, 위 끝 y). 사이는 곧게 잇는다. 아래 끝은 그보다 10 px 아래다.
    folds = [(1, 4), (5, 2), (10, 4), (14, 2)]
    for (x0, y0), (x1, y1) in zip(folds, folds[1:]):
        bright = y1 < y0
        for x in range(x0, x1 + 1):
            top = round(y0 + (y1 - y0) * (x - x0) / (x1 - x0))
            for y in range(top, top + 11):
                colors[y][x] = PAGE if bright else PAGE_SHADE
    marks = {
        INK_GREEN: [(2, 7), (3, 7), (3, 8), (4, 8), (7, 6), (8, 6), (8, 7), (12, 6), (12, 7), (13, 8)],
        INK_BLUE: [(2, 11), (3, 11), (4, 12), (5, 12), (6, 11), (7, 11), (8, 10), (9, 10)],
        WARNING: [(11, 9), (13, 9), (12, 10), (11, 11), (13, 11)],
    }
    for color, spots in marks.items():
        for (x, y) in spots:
            if colors[y][x] is not None:
                colors[y][x] = color
    return outlined(colors)


def hud_gear():
    """메뉴 아이콘 16×16 px: 이가 여덟 개인 금빛 톱니바퀴. 왼쪽 위가 밝고 오른쪽 아래가 짙으며 가운데가 뚫렸다."""
    import math
    size = 16
    colors = [[None] * size for _ in range(size)]
    centre = 7.5
    for y in range(size):
        for x in range(size):
            dx, dy = x - centre, y - centre
            r = math.hypot(dx, dy)
            # 이는 45°마다, 가운데에서 ±13° 폭이다.
            off = (math.degrees(math.atan2(dy, dx)) + 360) % 45
            tooth = min(off, 45 - off) < 13
            if r > (7.4 if tooth else 4.9) or r < 2.2:
                continue
            light = -(dx + dy) / max(r, 0.01) / 1.414
            if r < 3.3:
                colors[y][x] = GOLD_DARK                                                # 구멍 둘레의 턱
            elif light > 0.3:
                colors[y][x] = LANTERN_LIGHT
            elif light > -0.3:
                colors[y][x] = LANTERN
            else:
                colors[y][x] = GOLD_DARK
    return outlined(colors)


MINIMAP_SIZE = 46
MINIMAP_CENTRE = (MINIMAP_SIZE - 1) / 2
# 미니맵 테두리의 바깥 반지름과, 맵이 보이는 안쪽 원의 반지름(가운데에서 잰 거리, 가상 px).
MINIMAP_OUTER = 22.6
MINIMAP_VIEW = 19.2


def minimap_circle(radius):
    """가운데에서 radius 안에 드는 칸."""
    import math
    return [[math.hypot(x - MINIMAP_CENTRE, y - MINIMAP_CENTRE) <= radius for x in range(MINIMAP_SIZE)] for y in range(MINIMAP_SIZE)]


def minimap_ring():
    """
    미니맵 테두리 46×46 px: 밤색 바깥선, 왼쪽 위가 밝은 금빛 띠, 밤색 안쪽 선. 안쪽은 비어 있어 맵이 비친다.
    두 선은 띠 가운데 바깥(안쪽 원)과 맞닿은 칸으로 골라 끊기지 않는 한 줄이 되게 한다.
    """
    import math
    outer, view = minimap_circle(MINIMAP_OUTER), minimap_circle(MINIMAP_VIEW)
    rgba = blank(MINIMAP_SIZE, MINIMAP_SIZE)

    def touches(grid, x, y, wanted):
        return any(not (0 <= x + dx < MINIMAP_SIZE and 0 <= y + dy < MINIMAP_SIZE) and not wanted
                   or 0 <= x + dx < MINIMAP_SIZE and 0 <= y + dy < MINIMAP_SIZE and grid[y + dy][x + dx] == wanted
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))

    for y in range(MINIMAP_SIZE):
        for x in range(MINIMAP_SIZE):
            if not outer[y][x] or view[y][x]:
                continue
            if touches(outer, x, y, False) or touches(view, x, y, True):
                fill(rgba, x, y, 1, 1, NIGHT)
                continue
            dx, dy = x - MINIMAP_CENTRE, y - MINIMAP_CENTRE
            light = -(dx + dy) / math.hypot(dx, dy) / 1.414
            fill(rgba, x, y, 1, 1, LANTERN_LIGHT if light > 0.75 else LANTERN if light > -0.4 else GOLD_DARK)
    return rgba


def minimap_disk():
    """미니맵의 둥근 창 46×46 px: 테두리 안쪽 선까지 채운 밤색 원. 맵 그림을 이 모양으로 자르고(Mask), 맵 밖은 이 색으로 보인다."""
    view = minimap_circle(MINIMAP_VIEW + 1)
    rgba = blank(MINIMAP_SIZE, MINIMAP_SIZE)
    for y in range(MINIMAP_SIZE):
        for x in range(MINIMAP_SIZE):
            if view[y][x]:
                fill(rgba, x, y, 1, 1, NIGHT)
    return rgba


def from_rows(rows, palette):
    """글자 줄로 그린 작은 그림('.'은 빈칸)에 밤색 바깥선을 두른다. 둘레에 한 칸씩 자리를 늘린다."""
    width = len(rows[0]) + 2
    colors = [[None] * width for _ in range(len(rows) + 2)]
    for y, row in enumerate(rows):
        for x, key in enumerate(row):
            if key != ".":
                colors[y + 1][x + 1] = palette[key]
    return outlined(colors)


def minimap_player(diagonal):
    """
    미니맵 가운데의 주인공: 금빛 세모(7×6 px). 대각선(6×6 px)은 왼쪽 위를 가리키는 직각 세모다.
    도트 그림을 비스듬히 돌리면 계단이 깨지므로 미니맵은 이 둘을 90°씩만 돌려 여덟 방향을 낸다.
    """
    if diagonal:
        return from_rows([
            "aaab",
            "aab.",
            "ab..",
            "b...",
        ], {"a": LANTERN_LIGHT, "b": LANTERN})
    return from_rows([
        "..a..",
        ".aab.",
        ".aab.",
        "aaabb",
    ], {"a": LANTERN_LIGHT, "b": LANTERN})


def minimap_goal():
    """미니맵의 목적지 4×9 px: 금빛 「!」."""
    return from_rows([
        "ab",
        "ab",
        "ab",
        "ab",
        "..",
        "ab",
        "ab",
    ], {"a": LANTERN_LIGHT, "b": LANTERN})


def minimap_dot(light, color):
    """미니맵의 점 5×5 px: 왼쪽 위가 밝은 3×3 점. 사람·몬스터·출구가 색만 달리해 쓴다."""
    return from_rows([
        ".a.",
        "abb",
        ".b.",
    ], {"a": light, "b": color})


def vitals_preview(root, parts):
    """체력 칸을 게임과 같은 자리(가상 px)로 짜 맞춘 모습. 얼굴 자리는 오목한 판만 둔다. 네 배로 키워 저장한다."""
    width, height = 132, 30
    canvas = Image.new("RGBA", ((width + 8) * D, (height + 8) * D), (84, 120, 62, 255))

    def nine(rgba, border, w, h):
        """9-slice를 w×h px로 늘린다(가운데는 늘이고 테두리는 그대로)."""
        b = border * D
        src = Image.fromarray(rgba, "RGBA")
        out = Image.new("RGBA", (w * D, h * D))
        sw, sh = src.size
        xs = [(0, b, 0, b), (b, sw - b, b, w * D - b), (sw - b, sw, w * D - b, w * D)]
        ys = [(0, b, 0, b), (b, sh - b, b, h * D - b), (sh - b, sh, h * D - b, h * D)]
        for sx0, sx1, dx0, dx1 in xs:
            for sy0, sy1, dy0, dy1 in ys:
                if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                    out.paste(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
        return out

    def tint(rgba, rgb):
        tinted = rgba.astype(float)
        tinted[..., :3] *= np.array(rgb) / 255
        return Image.fromarray(tinted.astype(np.uint8), "RGBA")

    def at(x, y_top):
        return (4 + x) * D, (4 + y_top) * D

    canvas.alpha_composite(nine(parts["plate"], 4, width, height), at(0, 0))
    canvas.alpha_composite(nine(parts["well"], 3, 24, 24), at(3, height - 27))
    for y_top, h, ratio, colour, icon, icon_at in ((15, 6, 0.7, (130, 222, 124), parts["icon_heart"], (31, 14)),
                                                    (24, 4, 0.45, MANA, parts["icon_mana"], (32, 23))):
        canvas.alpha_composite(nine(parts["trough"], 2, 87, h), at(40, y_top))
        filled = int(85 * ratio)
        canvas.alpha_composite(tint(np.array(nine(parts["bar_fill"], 1, filled, h - 2)), colour), at(41, y_top + 1))
        canvas.alpha_composite(Image.fromarray(icon, "RGBA"), at(*icon_at))
    path = root / "Docs/ArtUpgrade/PixelGen/vitals_preview.png"
    canvas.resize((canvas.width * 2, canvas.height * 2), Image.NEAREST).save(path)
    print(path.relative_to(root).as_posix())


def main(root):
    kit = root / "Assets/MiniPandoras/Art/PixelGen/UI/Kit"
    kit.mkdir(parents=True, exist_ok=True)
    outputs = {
        "frame.png": frame(),
        "well.png": well(),
        "focus.png": focus(),
        "well_focus.png": well_focus(),
        "button.png": button(),
        "close.png": close(),
        "arrow.png": arrow(),
        "plate.png": hud_plate(),
        "trough.png": trough(),
        "bar_fill.png": bar_fill(),
        "icon_heart.png": icon_heart(),
        "icon_mana.png": icon_mana(),
        "menu.png": menu_icon(),
        "skip.png": skip_icon(),
        "profile.png": profile_icon(),
        "settings.png": settings_icon(),
        "hud_book.png": hud_book(),
        "hud_map.png": hud_map(),
        "hud_gear.png": hud_gear(),
        "minimap_ring.png": minimap_ring(),
        "minimap_disk.png": minimap_disk(),
        "minimap_player.png": minimap_player(False),
        "minimap_player_diagonal.png": minimap_player(True),
        "minimap_goal.png": minimap_goal(),
        "minimap_person.png": minimap_dot(MOON, (196, 186, 170)),
        "minimap_enemy.png": minimap_dot(WARNING_LIGHT, WARNING),
        "minimap_exit.png": minimap_dot(MANA_LIGHT, MANA),
    }
    for name, rgba in outputs.items():
        Image.fromarray(rgba, "RGBA").save(kit / name)
        print((kit / name).relative_to(root).as_posix(), rgba.shape[1], "x", rgba.shape[0])
    vitals_preview(root, {name[:-4]: rgba for name, rgba in outputs.items()})


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
