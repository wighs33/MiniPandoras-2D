"""뱀열차 정류장 소품(사용자 요청 2026-11-02: 승강장이 승강장 같지 않다).

  platform(columns, stairs, rng)     돌로 쌓아 올린 승강장(바닥에 까는 그림): 판석 윗면, 선로 쪽 가장자리의 밝은 갓돌과 놋쇠 띠,
                                     앞(남쪽) 옹벽과 그 옹벽을 끊고 내려가는 돌계단
  serpent_groove(...)                뱀길의 S자 홈: 뱀열차가 늘 지나가 다져진 모래 홈과 밀려난 모래 둔덕(바닥에 까는 그림)
  station_board(rng)                 역명판: 쇠 기둥 둘에 건 초록 판에 뱀 문장과 「뱀열차 정류장」
  ticket_booth(rng)                  매표소: 판자벽의 작은 오두막, 불 켜진 창구와 차양, 지붕 위 뱀 문장 간판, 벽 등불
  luggage(rng)                       여행 가방 더미: 놋쇠 모서리의 큰 트렁크, 작은 가방, 끈 묶은 자루

세우는 것은 (RGBA 그림, 기준점)을 돌려준다(기준점은 바닥에 닿는 아래 가운데, layout.MapLayout.stand로 세운다).
바닥에 까는 것은 RGBA 그림만 돌려준다(layout.MapLayout.decal). 모두 점 격자(한 칸 32점)에 그린다.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFont

import pixelgen as pg
from houses import BRASS, CLOTH, IRON, PAPER, PLASTER, ROOFS, WARM, WOOD, Canvas, lantern, outline, plank_wall

T = pg.TILE
PLATFORM_DEPTH = 3 * T              # 승강장 윗면이 덮는 깊이(칸 셋)
PLATFORM_FACE = 24                  # 앞 옹벽 높이(점). 이 높이만큼 승강장이 땅보다 높아 보인다
COPING = 14                         # 선로 쪽 갓돌 띠 너비(점)
STONE = pg.STONE
# 판석은 밝기 차이를 작게 둔다(차이가 크면 바닥이 아니라 쌓은 돌벽처럼 보인다). 옹벽은 남쪽을 향해 빛을 덜 받아 어둡다.
PAVING = pg.hexes("#55575f", "#6c6e77", "#787a83", "#82848d", "#979aa2")
WALL = pg.hexes("#1f1f26", "#2c2d35", "#383a44", "#464854", "#5a5c68")
SNAKE = pg.hexes("#2c3e2e", "#52694a", "#6e8752", "#869c5e")   # 뱀열차 비늘 색(snake_train.py와 같은 녹색)
FONT = "C:/Windows/Fonts/malgunbd.ttf"


def platform(columns, stairs, rng):
    """
    돌로 쌓아 올린 승강장(바닥에 까는 그림, 왼쪽 위 기준). 위에서 비스듬히 본 모습이라 윗면과 앞(남쪽) 옹벽이 보인다.
    윗면: 선로 쪽 가장자리의 밝은 갓돌과 놋쇠 띠, 반 장씩 어긋나게 깐 같은 크기의 판석(이음매의 이끼, 잔금).
    옹벽: 쌓은 돌, 위의 갓돌, 발치의 그림자. stairs: 옹벽을 끊고 내려가는 돌계단의 (왼쪽, 오른쪽) 칸 목록(승강장 왼쪽 끝 기준).
    """
    w = columns * T
    face_top = PLATFORM_DEPTH
    h = face_top + PLATFORM_FACE + 4
    rgb = np.zeros((h, w, 3), dtype=np.uint8)
    alpha = np.zeros((h, w), dtype=np.uint8)

    # 판석: 같은 크기의 돌을 줄마다 반 장씩 어긋나게 깐다. 돌마다 밝기가 아주 조금 다르고, 윗변은 빛을 받는다.
    slab_w, slab_h = 32, 16
    for row, y in enumerate(range(COPING, face_top - 3, slab_h)):
        y1 = min(y + slab_h, face_top - 3)
        for x in range(-(row % 2) * slab_w // 2, w, slab_w):
            x0, x1 = max(0, x), min(w, x + slab_w)
            level = 2 if rng.random() < 0.6 else 3
            rgb[y:y1, x0:x1] = PAVING[level]
            rgb[y, x0:x1] = PAVING[4] if level == 3 else PAVING[3]
            rgb[y:y1, x0:x0 + 1] = PAVING[0]
            for _ in range(int((x1 - x0) * (y1 - y) / 50)):
                sx, sy = int(rng.integers(x0, max(x0 + 1, x1))), int(rng.integers(y + 1, max(y + 2, y1 - 1)))
                rgb[sy, sx] = PAVING[1]
            if rng.random() < 0.12:
                cx = int(rng.integers(x0 + 3, max(x0 + 4, x1 - 3)))
                for k in range(int(rng.integers(4, 9))):
                    rgb[min(y1 - 2, y + 3 + k // 2), min(w - 1, cx + k)] = PAVING[0]
        rgb[y1 - 1] = PAVING[0]
        for _ in range(int(w / 40)):
            mx = int(rng.integers(0, w))
            rgb[y1 - 1, mx:mx + int(rng.integers(1, 4))] = pg.GRASS[2]

    # 선로 쪽 갓돌: 크림색 돌 띠(턱의 진한 선, 빛 받는 윗줄), 안쪽의 놋쇠 띠와 박은 못
    rgb[0:COPING] = PLASTER[3]
    rgb[1:3] = PLASTER[4]
    for jx in range(int(rng.integers(0, 28)), w, 28):
        rgb[1:COPING - 5, jx:jx + 1] = PLASTER[1]
    rgb[COPING - 5] = PLASTER[2]
    rgb[0] = pg.OUTLINE
    rgb[COPING - 4:COPING - 1] = BRASS[3]
    rgb[COPING - 4] = BRASS[4]
    rgb[COPING - 1] = BRASS[1]
    for sx in range(8, w, 16):
        rgb[COPING - 3:COPING - 1, sx:sx + 1] = BRASS[2]

    # 앞 옹벽: 위의 갓돌(윗면 끝), 쌓은 돌 세 줄
    rgb[face_top - 3:face_top] = PAVING[4]
    rgb[face_top - 1] = PAVING[1]
    course_h = PLATFORM_FACE // 3
    for course in range(3):
        cy = face_top + course * course_h
        x = -int(rng.integers(0, 14)) - course * 11
        while x < w:
            block_w = int(rng.integers(18, 30))
            x0, x1 = max(0, x), min(w, x + block_w)
            rgb[cy:cy + course_h, x0:x1] = WALL[int(rng.choice([2, 2, 3])) - course // 2]
            rgb[cy, x0:x1] = WALL[4 - course]
            rgb[cy:cy + course_h, x0:x0 + 1] = WALL[0]
            x += block_w
        rgb[cy + course_h - 1] = WALL[0]
    alpha[:face_top + PLATFORM_FACE] = 255
    # 옹벽 발치 그림자
    rgb[face_top + PLATFORM_FACE:] = (16, 20, 18)
    alpha[face_top + PLATFORM_FACE:] = 100

    # 돌계단: 옹벽을 끊고 네 단으로 내려간다. 디딤판은 밝고 챌판은 어둡고, 양옆은 옹벽 돌이 감싼다.
    steps = 4
    for left, right in stairs:
        x0, x1 = left * T + 2, right * T - 2
        span = PLATFORM_FACE + 3
        for step in range(steps):
            sy = face_top - 3 + step * span // steps
            ey = face_top - 3 + (step + 1) * span // steps
            rgb[sy:ey, x0:x1] = PAVING[4 - step]
            rgb[ey - 2:ey, x0:x1] = WALL[3 - step // 2]
        rgb[face_top - 3:face_top + PLATFORM_FACE, x0 - 2:x0] = WALL[1]
        rgb[face_top - 3:face_top + PLATFORM_FACE, x1:x1 + 2] = WALL[1]
        alpha[face_top + PLATFORM_FACE:, x0:x1] = 0

    # 양 끝 테두리
    rgb[:face_top + PLATFORM_FACE, 0] = pg.OUTLINE
    rgb[:face_top + PLATFORM_FACE, w - 1] = pg.OUTLINE
    return np.dstack([rgb, alpha])


def serpent_groove(width, center, rng):
    """
    뱀길에 난 S자 홈(바닥에 까는 그림): 뱀열차가 늘 같은 물결을 따라 기어 다녀 모래가 다져진 홈과, 굽이 바깥으로 밀려난 모래 둔덕.
    center(x)는 x점 열에서 홈 가운데 줄(점)이다. 돌려주는 값: (그림, 그림 위 끝 점).
    """
    xs = np.arange(width)
    middle = center(xs)
    top = int(np.floor(middle.min())) - 13
    h = int(np.ceil(middle.max())) + 9 - top
    image = np.zeros((h, width, 4), dtype=np.uint8)
    yy = np.arange(h)[:, None] + top
    offset = yy - middle[None, :]
    dither = pg.bayer(h, width)
    # 다져진 홈: 몸이 닿는 줄(가운데)이 가장 짙고 위아래로 옅어진다.
    packed = (offset >= -9) & (offset <= 3)
    deep = (offset >= -4) & (offset <= 2)
    image[packed, :3] = pg.SAND[2]
    image[packed & (dither < 0.5 - np.abs(offset + 3) / 14), :3] = pg.SAND[1]
    image[deep, :3] = pg.SAND[1]
    image[deep & (dither < 0.35), :3] = pg.SAND[0]
    # 다져진 홈 안의 긁힌 결(비늘이 남긴 가로줄)
    streak = packed & (((xs[None, :] // 4 + (offset + 9).astype(int) * 5) % 13) == 0)
    image[streak, :3] = pg.SAND[0]
    image[packed, 3] = 210
    # 밀려난 모래 둔덕: 홈 아래(앞)는 빛을 받아 밝고, 위(뒤)는 그늘진다.
    ridge_below = (offset > 3) & (offset <= 7) & (dither < 1 - (offset - 3) / 5)
    image[ridge_below, :3] = pg.SAND[4]
    image[ridge_below, 3] = 180
    ridge_above = (offset < -9) & (offset >= -12) & (dither < 1 - (-9 - offset) / 4)
    image[ridge_above, :3] = pg.SAND[3]
    image[ridge_above, 3] = 150
    return image, top


def text_mask(text, size, threshold=110):
    """맑은 고딕 굵게로 쓴 글자를 점 격자 마스크로(작은 크기에서도 읽히게 문턱으로 자른다. 더 작으면 문턱을 낮춰 획이 끊기지 않게)."""
    font = ImageFont.truetype(FONT, size)
    left, top, right, bottom = font.getbbox(text)
    canvas = Image.new("L", (right - left + 2, bottom - top + 2), 0)
    ImageDraw.Draw(canvas).text((1 - left, 1 - top), text, font=font, fill=255)
    mask = np.array(canvas) > threshold
    rows, cols = np.nonzero(mask)
    return mask[rows.min():rows.max() + 1, cols.min():cols.max() + 1]


def snake_emblem(c, x0, y0, color, eye):
    """작은 뱀 문장: 옆으로 기어가는 S자 몸과 둥근 머리, 눈 한 점."""
    for k in range(14):
        y = y0 + 5 + 3 * np.sin(k * 0.55)
        c.paint(c.rect(x0 + 3 + k, int(round(y)), x0 + 4 + k, int(round(y)) + 2), color)
    c.paint(c.ellipse(x0 + 2.5, y0 + 5, 3, 2.6), color)
    c.paint(c.rect(x0 + 1, y0 + 4, x0 + 2, y0 + 5), eye)


def station_board(rng):
    """역명판: 쇠 기둥 둘에 건 짙은 초록 판. 놋쇠 테두리 안에 뱀 문장과 「뱀열차 정류장」."""
    text = text_mask("뱀열차 정류장", 12)
    th, tw = text.shape
    board_w, board_h = tw + 30, th + 10
    post_h = 20
    w, h = board_w + 4, board_h + post_h + 2
    c = Canvas(w, h)
    ground = h - 1
    left, top = 2, 1
    for px in (left + 6, left + board_w - 9):
        c.paint(c.rect(px, top + board_h - 2, px + 3, ground), IRON[2])
        c.paint(c.rect(px, top + board_h - 2, px + 1, ground), IRON[3])
        c.paint(c.rect(px - 2, ground - 2, px + 5, ground), IRON[1])
    board = c.rect(left, top, left + board_w, top + board_h)
    c.paint(board, CLOTH["green"][1])
    c.paint(c.rect(left, top, left + board_w, top + 2) | c.rect(left, top + board_h - 2, left + board_w, top + board_h)
            | c.rect(left, top, left + 2, top + board_h) | c.rect(left + board_w - 2, top, left + board_w, top + board_h), BRASS[3])
    c.paint(c.rect(left, top, left + board_w, top + 1), BRASS[4])
    snake_emblem(c, left + 5, top + board_h // 2 - 5, SNAKE[3], CLOTH["green"][0])
    tx, ty = left + 24, top + 5
    letters = np.zeros(c.xx.shape, dtype=bool)
    letters[ty:ty + th, tx:tx + tw] = text
    c.paint(letters, PAPER[2])
    return outline(c.image), (w // 2, ground)


def ticket_booth(rng):
    """매표소: 판자벽의 작은 오두막. 불 켜진 창구와 줄무늬 차양, 지붕 위 뱀 문장 간판, 벽 등불."""
    w, h = 76, 100
    c = Canvas(w, h)
    ground = h - 1
    wall_top = ground - 52
    x0, x1 = 8, w - 8
    walls = plank_wall(ground - wall_top, x1 - x0, rng)
    c.image[wall_top:ground, x0:x1, :3] = walls
    c.image[wall_top:ground, x0:x1, 3] = 255
    c.paint(c.rect(x0, ground - 6, x1, ground), WOOD[1])
    # 창구: 따뜻한 불빛, 가운데 창살, 아래 나무 선반
    win_x0, win_x1, win_top, win_bottom = x0 + 10, x1 - 10, wall_top + 12, ground - 18
    c.paint(c.rect(win_x0 - 2, win_top - 2, win_x1 + 2, win_bottom + 2), WOOD[1])
    c.paint(c.rect(win_x0, win_top, win_x1, win_bottom), WARM[3])
    c.paint(c.rect(win_x0, win_bottom - 6, win_x1, win_bottom), WARM[2])
    c.paint(c.rect(win_x0 + 6, win_top + 4, win_x1 - 6, win_top + 10), WARM[4])
    for bx in range(win_x0 + 7, win_x1, 9):
        c.paint(c.rect(bx, win_top, bx + 1, win_bottom), WOOD[2])
    c.paint(c.rect(win_x0 - 4, win_bottom, win_x1 + 4, win_bottom + 3), WOOD[4])
    c.paint(c.rect(win_x0 - 4, win_bottom, win_x1 + 4, win_bottom + 1), WOOD[5])
    # 줄무늬 차양
    awning_top = win_top - 9
    for k, ax in enumerate(range(win_x0 - 5, win_x1 + 5, 6)):
        stripe = CLOTH["teal"][2] if k % 2 else PAPER[1]
        c.paint(c.polygon([(ax, awning_top), (ax + 6, awning_top), (ax + 7, awning_top + 7), (ax + 1, awning_top + 7)]), stripe)
    c.paint(c.rect(win_x0 - 5, awning_top + 7, win_x1 + 6, awning_top + 8), CLOTH["teal"][0])
    # 지붕: 앞으로 비스듬히 내려온 구리 지붕
    roof = ROOFS["copper"]
    roof_top = wall_top - 22
    c.paint(c.polygon([(x0 - 6, wall_top + 2), (x1 + 6, wall_top + 2), (x1 - 2, roof_top), (x0 + 2, roof_top)]), roof[3])
    for ry in range(roof_top + 4, wall_top, 5):
        c.paint(c.rect(x0 - 4, ry, x1 + 4, ry + 1) & (c.image[..., 3] > 0), roof[2])
    c.paint(c.rect(x0 + 2, roof_top, x1 - 2, roof_top + 2), roof[5])
    c.paint(c.rect(x0 - 6, wall_top + 1, x1 + 6, wall_top + 3), roof[1])
    # 지붕 위 간판: 크림색 판에 뱀 문장
    sign_x0, sign_x1 = w // 2 - 13, w // 2 + 13
    c.paint(c.rect(sign_x0 + 5, roof_top - 3, sign_x0 + 7, roof_top + 1) | c.rect(sign_x1 - 7, roof_top - 3, sign_x1 - 5, roof_top + 1), IRON[2])
    c.paint(c.rect(sign_x0, roof_top - 17, sign_x1, roof_top - 3), PAPER[1])
    c.paint(c.rect(sign_x0, roof_top - 17, sign_x1, roof_top - 16), PAPER[2])
    snake_emblem(c, sign_x0 + 5, roof_top - 15, SNAKE[1], PAPER[2])
    lantern(c, x1 - 4, wall_top + 6)
    return outline(c.image), (w // 2, ground)


def luggage(rng):
    """여행 가방 더미: 놋쇠 모서리의 큰 가죽 트렁크, 그 위 작은 가방, 옆에 끈 묶은 자루."""
    w, h = 64, 44
    c = Canvas(w, h)
    ground = h - 1
    leather = pg.hexes("#3a2016", "#5a3220", "#7a4a2c", "#9a6440")
    trunk = c.rect(4, ground - 18, 42, ground)
    c.paint(trunk, leather[2])
    c.paint(c.rect(4, ground - 18, 42, ground - 16), leather[3])
    c.paint(c.rect(4, ground - 10, 42, ground - 8), leather[1])
    for bx in (4, 39):
        c.paint(c.rect(bx, ground - 18, bx + 3, ground), BRASS[3])
    c.paint(c.rect(20, ground - 12, 26, ground - 7), BRASS[4])
    bag = c.rect(12, ground - 30, 36, ground - 18)
    c.paint(bag, CLOTH["red"][1])
    c.paint(c.rect(12, ground - 30, 36, ground - 28), CLOTH["red"][2])
    c.paint(c.rect(20, ground - 34, 28, ground - 32) | c.rect(19, ground - 33, 21, ground - 30) | c.rect(27, ground - 33, 29, ground - 30), WOOD[1])
    sack = c.ellipse(52, ground - 9, 9, 10) & (c.yy < ground)
    c.paint(sack, PAPER[0])
    c.paint(sack & (c.xx < 50), PAPER[1])
    c.paint(c.rect(48, ground - 18, 56, ground - 16), WOOD[2])
    return outline(c.image), (w // 2, ground)


def station_clock(rng):
    """역 기둥 시계: 쇠 기둥 위에 놋쇠 테를 두른 둥근 시계(크림색 판, 눈금, 바늘)와 작은 뾰족 갓."""
    w, h = 30, 98
    c = Canvas(w, h)
    ground, cx, cy, r = h - 1, w // 2, 22, 12
    c.paint(c.rect(cx - 6, ground - 6, cx + 6, ground), IRON[1])
    c.paint(c.rect(cx - 6, ground - 6, cx + 6, ground - 5), IRON[3])
    c.paint(c.rect(cx - 2, cy + r, cx + 2, ground - 6), IRON[2])
    c.paint(c.rect(cx - 2, cy + r, cx - 1, ground - 6), IRON[3])
    c.paint(c.rect(cx - 4, cy + r, cx + 4, cy + r + 3), BRASS[2])
    c.paint(c.polygon([(cx - 7, cy - r + 1), (cx + 7, cy - r + 1), (cx, cy - r - 9)]), IRON[2])
    c.paint(c.rect(cx - 1, cy - r - 11, cx + 1, cy - r - 8), BRASS[3])
    c.paint(c.ellipse(cx, cy, r, r), BRASS[2])
    c.paint(c.ellipse(cx - 0.5, cy - 0.5, r - 0.5, r - 0.5) & (c.xx + c.yy < cx + cy), BRASS[3])
    c.paint(c.ellipse(cx, cy, r - 2.5, r - 2.5), PAPER[2])
    for k in range(12):
        a = k * np.pi / 6
        tx, ty = cx + np.sin(a) * (r - 4.5), cy - np.cos(a) * (r - 4.5)
        c.paint(c.rect(int(round(tx - 0.5)), int(round(ty - 0.5)), int(round(tx - 0.5)) + 1, int(round(ty - 0.5)) + 1), IRON[1])
    c.line(cx, cy, cx, cy - 7, IRON[0], 1)
    c.line(cx, cy, cx + 4, cy + 2, IRON[0], 1)
    c.paint(c.rect(cx - 1, cy - 1, cx + 1, cy + 1), CLOTH["red"][1])
    return outline(c.image), (cx, ground)


def timetable(rng):
    """시간표 게시판: 작은 지붕을 얹은 나무 판에 종이 시간표 두 장과 뱀 문장."""
    w, h = 48, 60
    c = Canvas(w, h)
    ground = h - 1
    board_top, board_bottom = 10, 38
    for px in (7, w - 10):
        c.paint(c.rect(px, board_bottom, px + 3, ground), WOOD[2])
        c.paint(c.rect(px, board_bottom, px + 1, ground), WOOD[3])
    c.paint(c.rect(3, board_top, w - 3, board_bottom), WOOD[3])
    c.paint(c.rect(3, board_top, w - 3, board_top + 1), WOOD[5])
    c.paint(c.rect(3, board_bottom - 2, w - 3, board_bottom), WOOD[1])
    c.paint(c.polygon([(0, board_top), (w, board_top), (w - 6, board_top - 8), (6, board_top - 8)]), ROOFS["copper"][3])
    c.paint(c.rect(1, board_top - 1, w - 1, board_top + 1), ROOFS["copper"][1])
    for x0 in (7, 26):
        c.paint(c.rect(x0, board_top + 4, x0 + 15, board_bottom - 4), PAPER[2])
        c.paint(c.rect(x0, board_top + 4, x0 + 15, board_top + 7), CLOTH["green"][1])
        for ly in range(board_top + 10, board_bottom - 5, 3):
            c.paint(c.rect(x0 + 2, ly, x0 + 2 + int(rng.integers(7, 13)), ly + 1), IRON[2])
    return outline(c.image), (w // 2, ground)
