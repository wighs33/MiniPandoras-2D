"""5장 뱀열차: 뱀과 열차가 하나로 합쳐진 판타지 짐승(사용자 요청 2026-11-02, "뱀이 기계화되는 게 아니라 진짜 뱀처럼").

놋쇠 테로 이은 원통 객차(웹게임 snakeCar·snakeTail) 대신 하나로 이어진 살아 있는 뱀 몸통을 그린다:
비늘로 덮인 옆구리, 크림색 배비늘, 등지느러미, 비늘 살 사이에 뼈 테로 박힌 불 켜진 창과 올라타는 문,
가늘어지다 끝이 살짝 들린 꼬리. 게임은 몸통 그림을 세로 띠로 잘라 땅에 놓인 S자 물결을 따라 세우므로(SnakeTrain.cs),
몸통은 곧게 그린다.
머리도 순한 얼굴로 새로 그린다(사용자 요청 2026-11-02, "열차인데 공격적인 얼굴은 안 맞다"): 웹게임 머리의 벌린 입·송곳니·세로 동공 대신
다문 입에 살짝 올라간 입꼬리, 크고 둥근 동공과 반짝임, 살짝 내려온 편안한 눈꺼풀, 둥근 주둥이, 몸통 배비늘로 이어지는 크림색 턱.

  head.png  머리: 목은 몸통과 같은 비늘 무늬로 이어지고, 목 끝은 뒤의 몸통과 섞이게 성기게 지운다
  tongue_0~3.png  날름거리는 갈래 혀 네 장(사용자 요청 2026-11-02, 생동감): 살짝 내민 끝, 반쯤, 다 내밀어 갈래를 벌림, 갈래 끝을 떪.
            오른쪽 끝 가운데가 혀뿌리다. SnakeTrain이 머리 그림 TONGUE_ROOT 자리(입선 바로 안)에 머리 뒤로 붙여 몇 초마다 튼다
  body.png  몸통: 머리 기준점 뒤 BODY_START에서 꼬리 끝 BODY_END까지. 땅에 닿는 줄은 아래에서 2 논리 px 위다
  Docs/ArtUpgrade/PixelGen/snake_train_preview.png  물결 위에 세운 모습

자리는 머리 기준점에서 꼬리 쪽으로 잰 논리 px이고 SnakeTrain.cs와 같은 값을 쓴다(몸통 시작, 문, 물결).
몸통은 점 격자(논리 px 하나 = 2점)에 그려 두 배로 키운다(게임 그림은 점 하나 = 2px).

사용: python Tools/PixelGen/snake_train.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

# ── 자리(논리 px) ─────────────────────────────────────────
BODY_START, BODY_END = 14, 336      # 몸통 그림이 덮는 구간(앞쪽은 머리 목 밑에 숨는다)
TAIL_FROM = 284                     # 여기서부터 꼬리가 가늘어진다
DOOR_AT = 102                       # 올라타는 문(웹게임 셋째 객차 자리)
WINDOWS = [d for d in range(30, 280, 17) if abs(d - DOOR_AT) > 12]
GROOVE_LENGTH, GROOVE_DEPTH = 140, 7    # 땅 위 S자 물결(미리 보기만 쓴다. 게임 값은 SnakeTrain.cs)
NECK = 16                           # 머리는 목 자리의 물결 높이에 선다(몸통과 이어지는 곳이 어긋나지 않게)

# ── 점 격자 ──────────────────────────────────────────────
DOT = 2                             # 논리 px 하나 = 2점
WIDTH = (BODY_END - BODY_START) * DOT
HEIGHT = 81
GROUND = HEIGHT - 4                 # 몸이 땅에 닿는 줄. 아래 4점(2 논리 px)은 땅 그림자
GIRTH = 68                          # 몸통 굵기(점). 머리 목 굵기와 같다
SCALE_W, SCALE_H = 12, 7

# 웹게임 뱀 그림의 비늘 색에 맞춘 녹색(진한 쪽부터), 배비늘, 뼈 테, 창 불빛
SCALES = pg.hexes("#1c2a20", "#2c3e2e", "#3e5640", "#52694a", "#5c7449", "#6e8752", "#869c5e")
BELLY = pg.hexes("#7a6a58", "#a8957c", "#cbb699", "#e2cfb3", "#efe2c8")
BONE = pg.hexes("#6f6150", "#b4a284", "#ddd0b0")
GLOW = pg.hexes("#8a4a1c", "#d9822e", "#f2b04a", "#ffd77a", "#fff0b8")
OUTLINE = np.array([16, 22, 17], dtype=np.uint8)
SHADOW = np.array([10, 8, 20, 72], dtype=np.uint8)
WARM = np.array([255, 190, 110])

# ── 머리(점, 머리 그림 기준) ─────────────────────────────
HEAD_WIDTH = 104                    # 52 논리 px: 주둥이 끝(기준점 앞 30 px)부터 목 끝(뒤 22 px)까지
HEAD_HEIGHT = 88
HEAD_GROUND = HEAD_HEIGHT - 4
# 머리 그림 점을 몸통 그림 점으로 옮기는 차이. 목의 비늘 무늬가 몸통과 이어지게 같은 좌표로 무늬를 깐다.
TO_BODY_X = (BODY_START + 30) * DOT
TO_BODY_Y = HEAD_GROUND - GROUND
NECK_TOP = HEAD_GROUND - GIRTH
EYE = (34, 42)                      # 눈 가운데
EYE_RADIUS = 10
NECK_FADE = 16                      # 목 끝을 성기게 지우는 폭(점)
IRIS = pg.hexes("#9c6a1e", "#d99a32", "#efc15a")
PUPIL = np.array([27, 20, 16])
SPARKLE = np.array([255, 251, 232])


def mouth_line(x):
    """다문 입: 주둥이 앞 아래에서 뒤로 완만하게 내려갔다가, 입꼬리에서 올라간다(웃는 입). 입선은 x 3~48점에 긋는다."""
    return 70 - 0.0049 * (x - 30) ** 2 - np.maximum(0, x - 41)


def chin_top(xx, bx):
    """크림색 턱의 위 끝: 앞은 입선 바로 아래, 입꼬리 뒤로 가며 몸통 배비늘 경계 높이로 올라가고 비늘 끝을 따라 물결진다."""
    belly_top = NECK_TOP + (1 - BELLY_SHARE) * GIRTH
    scallop = 0.035 * GIRTH * (1 - ((bx % SCALE_W) - SCALE_W / 2) ** 2 / (SCALE_W / 2) ** 2)
    back = belly_top + (70 - belly_top) * np.clip((70 - xx) / 30, 0, 1) + scallop
    return np.where(xx <= 40, mouth_line(xx), back)


TONGUE_ROOT = (10, 67)              # 머리 그림에서 혀뿌리 자리(점): 주둥이 앞 입선 바로 안. SnakeTrain.cs와 같은 값
TONGUE_WIDTH, TONGUE_HEIGHT = 34, 16
# 큰 머리에서도, 밤빛 아래에서도 보이게 굵고 밝게 칠한다.
TONGUE = pg.hexes("#6a1428", "#d8405c", "#ff86a0")


def tongue(frame):
    """
    갈래 혀 한 장(점): 오른쪽 끝 가운데(혀뿌리)에서 왼쪽으로 내민다. 굵기 3점(윗줄 밝고 아랫줄 어둡다), 끝은 두 갈래.
    0 살짝 내민 끝, 1 반쯤 내밀고 갈래가 조금 벌어짐, 2 다 내밀어 갈래를 위아래로 벌림, 3 갈래 끝을 떨어 오므림.
    """
    length = (8, 17, 27, 27)[frame]
    image = np.zeros((TONGUE_HEIGHT, TONGUE_WIDTH, 4), dtype=np.uint8)
    middle = TONGUE_HEIGHT // 2
    tip = TONGUE_WIDTH - length

    def dot(x, y, color):
        if 0 <= x < TONGUE_WIDTH and 0 <= y < TONGUE_HEIGHT:
            image[y, x, :3] = color
            image[y, x, 3] = 255

    for x in range(tip, TONGUE_WIDTH):
        dot(x, middle - 2, TONGUE[2])
        dot(x, middle - 1, TONGUE[1])
        dot(x, middle, TONGUE[0])
    # 갈래: 혀끝에서 위아래로 갈라지는 두 가닥(가닥마다 2점 굵기). 프레임마다 벌어진 정도가 다르다.
    spread = {0: 0, 1: 2, 2: 5, 3: 3}[frame]
    reach = {0: 0, 1: 3, 2: 6, 3: 6}[frame]
    for k in range(1, reach + 1):
        rise = int(round(k * spread / reach))
        dot(tip - k, middle - 2 - rise, TONGUE[2])
        dot(tip - k, middle - 1 - rise, TONGUE[1])
        dot(tip - k, middle + rise, TONGUE[1])
        dot(tip - k, middle + 1 + rise, TONGUE[0])
    return Image.fromarray(pg.upscale(image), "RGBA")


def head_shape(xx, yy):
    """둥근 머리통과 둥근 주둥이, 몸통 굵기로 이어지는 목. 바닥은 땅에 닿는다."""
    skull = ((xx - 56) / 46) ** 2 + ((yy - 50) / 36) ** 2 <= 1
    snout = ((xx - 26) / 24) ** 2 + ((yy - 58) / 24) ** 2 <= 1
    neck = (xx >= 70) & (yy >= NECK_TOP)
    return (skull | snout | neck) & (yy <= HEAD_GROUND)


def add_eye(rgb, xx, yy):
    """크고 둥근 눈: 호박색 홍채에 큰 둥근 동공과 반짝임 둘. 위는 눈꺼풀이 살짝 덮어 편안해 보인다."""
    ex, ey = EYE
    r = np.hypot(xx - ex, yy - ey)
    ball = r <= EYE_RADIUS - 0.5
    rgb[(r <= EYE_RADIUS + 0.5) & ~ball] = OUTLINE
    rgb[ball] = IRIS[1]
    rgb[ball & (yy > ey + 4)] = IRIS[0]
    rgb[ball & (yy < ey - 2) & (xx < ex)] = IRIS[2]
    rgb[np.hypot(xx - (ex - 1), yy - (ey + 1)) <= 6.5] = PUPIL
    rgb[np.hypot(xx - (ex - 4), yy - (ey - 3)) <= 2.3] = SPARKLE
    rgb[np.hypot(xx - (ex + 3), yy - (ey + 5)) <= 1.2] = SPARKLE
    lid = ey - 7 + 0.035 * (xx - ex) ** 2
    rgb[ball & (yy < lid)] = SCALES[4]
    rgb[(r <= EYE_RADIUS + 0.5) & (np.abs(yy - lid) < 0.7)] = OUTLINE


def head():
    yy, xx = np.mgrid[0:HEAD_HEIGHT, 0:HEAD_WIDTH].astype(float)
    shape = head_shape(xx, yy)
    bx, by = xx - TO_BODY_X, yy - TO_BODY_Y
    v = (yy - 14) / (HEAD_GROUND - 14)
    # 얼굴(주둥이와 눈 둘레)은 비늘 테 선을 긋지 않아 매끈하다.
    face = (((xx - 26) / 24) ** 2 + ((yy - 58) / 24) ** 2 <= 1) | (np.hypot(xx - EYE[0], yy - EYE[1]) < 16)
    rgb = scale_colors(bx, by, v, smooth=face).astype(float)
    neck_v = (yy - NECK_TOP) / GIRTH
    rgb[(xx >= 70) & (neck_v > 0.02) & (neck_v < 0.07)] = SCALES[1]

    # 크림색 턱(몸통 배비늘로 이어진다)과 웃는 입선, 입선 위 밝은 윗입술
    top = chin_top(xx, bx)
    chin = yy > top
    rgb = np.where(chin[..., None], belly_colors(bx, (yy - top) / np.maximum(HEAD_GROUND - top, 1)), rgb)
    mouth = mouth_line(xx)
    rgb[(xx <= 40) & (yy <= mouth) & (yy > mouth - 2.5)] = SCALES[5]
    rgb[(xx >= 3) & (xx <= 48) & (np.abs(yy - mouth) < 0.9)] = OUTLINE

    # 주둥이 위 콧구멍, 눈
    rgb[(np.abs(xx - 8) < 1.5) & (np.abs(yy - 49) < 1)] = OUTLINE
    add_eye(rgb, xx, yy)

    rgba = np.zeros((HEAD_HEIGHT, HEAD_WIDTH, 4), dtype=np.uint8)
    rgba[..., :3] = rgb.astype(np.uint8)
    rgba[..., 3] = shape * 255
    rgba = pg.outline(rgba, OUTLINE)
    # 목 끝은 뒤에 이어지는 몸통 비늘이 비치도록 순서 디더로 성기게 지운다.
    keep = (HEAD_WIDTH - xx) / NECK_FADE
    rgba[pg.bayer(*xx.shape) >= keep, 3] = 0
    return Image.fromarray(pg.upscale(rgba), "RGBA")


# ── 몸통 ────────────────────────────────────────────────

def profile(d):
    """그 자리(논리 px) 몸통의 위·아래 끝(점). 꼬리는 가늘어지며 끝이 살짝 들린다."""
    t = np.clip((d - TAIL_FROM) / (BODY_END - TAIL_FROM), 0, 1)
    girth = np.maximum(GIRTH * (1 - t) ** 0.85, 2)
    bottom = GROUND - 16 * t ** 2.2
    return bottom - girth, bottom


BELLY_SHARE = 0.30                  # 배비늘이 차지하는 아래쪽 몫


def scale_pattern(xx, yy):
    """
    물고기 비늘 무늬: 줄마다 반 칸 어긋난 둥근 비늘, 위 줄이 아래 줄을 덮어 비늘마다 둥근 아래 끝이 보인다.
    xx, yy는 몸통 그림 기준 점 좌표다(머리도 같은 좌표로 넘겨 목에서 무늬가 이어진다). 돌려주는 값은 비늘 안 좌표(-1~1)다.
    """
    ex = np.zeros(xx.shape)
    ey = np.zeros(xx.shape)
    first, last = int(np.floor(yy.min() / SCALE_H)) - 2, int(np.ceil(yy.max() / SCALE_H)) + 2
    for row in range(last, first - 1, -1):
        cy = row * SCALE_H
        shift = (row % 2) * SCALE_W / 2
        cx = np.floor((xx - shift) / SCALE_W) * SCALE_W + shift + SCALE_W / 2
        sx = (xx - cx) / (SCALE_W * 0.62)
        sy = (yy - cy) / (SCALE_H * 1.35)
        inside = sx ** 2 + sy ** 2 <= 1
        ex = np.where(inside, sx, ex)
        ey = np.where(inside, sy, ey)
    return ex, ey


def scale_colors(xx, yy, v, smooth=None):
    """
    비늘 한 장의 명암(위 비늘에 덮인 윗부분은 그늘, 가운데는 밝고, 왼쪽 위가 반짝인다. 아래 둥근 끝은 진한 선)과
    둥근 몸의 명암(v: 등 0 ~ 배 1. 등은 멀어지며 어둡고, 옆구리 윗부분이 가장 밝고, 배 쪽으로 다시 어둡다).
    smooth인 곳은 비늘 테 선을 긋지 않는다.
    """
    ex, ey = scale_pattern(xx, yy)
    level = 3 + (ey > 0.5).astype(int) + ((ex < -0.05) & (ey > 0.45) & (ey < 0.8)).astype(int)
    shade = np.select([v < 0.07, v < 0.17, v < 0.5, v < 0.62], [-3, -2, 0, -1], -2)
    level = np.clip(level + shade, 1, len(SCALES) - 1)
    colors = SCALES[level]
    rim = (ex ** 2 + ey ** 2 > 0.74) & (ey > 0.15)
    if smooth is not None:
        rim &= ~smooth
    colors[rim] = SCALES[np.clip(level[rim] - 3, 0, None)]
    return colors


def belly_mask(xx, v):
    """배비늘 자리: 비늘 끝을 따라 물결진 경계 아래."""
    scallop = 0.035 * (1 - ((xx % SCALE_W) - SCALE_W / 2) ** 2 / (SCALE_W / 2) ** 2)
    return v > 1 - BELLY_SHARE + scallop


def belly_colors(xx, within):
    """세로로 늘어선 배판, 아래로 갈수록 어둡다(within: 배비늘 위 끝 0 ~ 아래 끝 1)."""
    level = 4 - pg.dither_levels(np.clip(within, 0, 1), [0.3, 0.75, 0.92], 0.12)
    plate_line = (xx % 9) == 0
    return BELLY[np.where(plate_line, np.maximum(level - 2, 0), level)]


def body_colors(top, bottom, yy, xx):
    """비늘 옆구리와 등줄기, 배비늘."""
    v = (yy - top) / np.maximum(bottom - top, 1)
    colors = scale_colors(xx, yy, v)
    colors[(v > 0.02) & (v < 0.07)] = SCALES[1]
    belly = belly_mask(xx, v)
    colors = np.where(belly[..., None], belly_colors(xx, (v - (1 - BELLY_SHARE)) / BELLY_SHARE), colors)
    edge = belly & ~np.roll(belly, 1, axis=0)
    colors[edge] = SCALES[1]
    return colors


def add_fins(rgba, top, d):
    """등지느러미: 꼬리 쪽으로 기운 작은 삼각 지느러미가 등을 따라 줄지어 선다. 목과 꼬리 끝에서는 작아진다."""
    height, width = rgba.shape[:2]
    for x0 in range(4, width - 10, 14):
        size = float(np.clip((d[x0] - 24) / 20, 0, 1) * np.clip((BODY_END - 14 - d[x0]) / 40, 0, 1))
        if size < 0.25:
            continue
        base, tall, lean = 9 * size, 7 * size, 5 * size
        root = top[x0]
        for x in range(int(x0), int(x0 + base + lean) + 1):
            if x >= width:
                break
            # 앞 변(왼쪽)은 완만하게 올라가고 뒤 변은 꼬리 쪽으로 기울어 떨어진다.
            rise = (x - x0) / max(base * 0.7, 1)
            fall = (x0 + base + lean - x) / max(base * 0.3 + lean, 1)
            h = int(round(tall * min(rise, fall, 1)))
            for y in range(int(root) - h, int(root) + 1):
                if 0 <= y < height:
                    rgba[y, x, :3] = SCALES[3] if y == int(root) - h or x == int(x0) else SCALES[2]
                    rgba[y, x, 3] = 255


def window_mask(xx, yy, cx, cy, half_w, half_h):
    """위가 둥근 아치 창(점)."""
    arch_y = cy - half_h + half_w
    in_rect = (np.abs(xx - cx) <= half_w) & (yy >= arch_y) & (yy <= cy + half_h)
    in_arch = ((xx - cx) ** 2 + (yy - arch_y) ** 2 <= half_w ** 2) & (yy < arch_y)
    return in_rect | in_arch


def add_opening(rgb, alpha, xx, yy, cx, cy, half_w, half_h, rim, door=False):
    """비늘 살에 박힌 뼈 테 창(또는 문): 진한 테두리, 뼈 테, 안쪽 따뜻한 불빛과 가운데 칸막이. 둘레 비늘이 불빛에 물든다."""
    outer = window_mask(xx, yy, cx, cy, half_w + rim + 1, half_h + rim + 1)
    frame = window_mask(xx, yy, cx, cy, half_w + rim, half_h + rim)
    inner = window_mask(xx, yy, cx, cy, half_w, half_h)
    near = np.hypot((xx - cx) / (half_w + 9), (yy - cy) / (half_h + 9))
    spill = np.clip(1 - near, 0, 1) * 0.3
    spill = np.clip(np.floor(spill * 3 + (pg.bayer(*xx.shape) - 0.5) * 0.8) / 3, 0, None) * alpha
    rgb[:] = np.clip(rgb * (1 - spill[..., None]) + WARM * spill[..., None], 0, 255)
    rgb[outer & alpha.astype(bool)] = OUTLINE
    rgb[frame] = BONE[1]
    rgb[frame & (yy < cy - half_h * 0.2) & (xx < cx)] = BONE[2]
    rgb[frame & (yy > cy + half_h * 0.6)] = BONE[0]
    light = np.hypot((xx - cx) / (half_w + 1), (yy - cy + half_h * 0.2) / (half_h + 1))
    glow_level = pg.dither_levels(1 - light, [0.15, 0.35, 0.55, 0.75], 0.1)
    rgb[inner] = GLOW[glow_level[inner]]
    if door:
        # 문 안쪽 바닥은 어둡고, 문지방은 뼈로 된 턱이다.
        rgb[inner & (yy > cy + half_h * 0.7)] = GLOW[1]
        rgb[frame & (yy > cy + half_h)] = BONE[0]
    else:
        rgb[inner & (np.abs(xx - cx) < 0.5)] = GLOW[0]
        rgb[inner & (np.abs(yy - (cy + half_h * 0.15)) < 0.5)] = GLOW[0]
    alpha[outer] = 1


def body():
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH].astype(float)
    d = BODY_START + np.arange(WIDTH) / DOT
    top, bottom = profile(d)
    inside = (yy >= top[None, :]) & (yy <= bottom[None, :])
    rgb = body_colors(top[None, :], bottom[None, :], yy, xx).astype(float)
    alpha = inside.astype(float)

    mid = top[0] + GIRTH * 0.40
    for at in WINDOWS:
        add_opening(rgb, alpha, xx, yy, (at - BODY_START) * DOT, mid, 4.5, 6, rim=1.5)
    door_top, door_bottom = top[0] + GIRTH * 0.2, GROUND - 3
    add_opening(rgb, alpha, xx, yy, (DOOR_AT - BODY_START) * DOT, (door_top + door_bottom) / 2, 7,
                (door_bottom - door_top) / 2, rim=2, door=True)

    rgba = np.zeros((HEIGHT, WIDTH, 4), dtype=np.uint8)
    rgba[..., :3] = rgb.astype(np.uint8)
    rgba[..., 3] = (alpha > 0) * 255
    add_fins(rgba, top, d)
    rgba = pg.outline(rgba, OUTLINE)
    # 몸이 닿은 땅에 납작한 그림자(꼬리 끝이 들린 곳까지는 닿지 않는다).
    shadow = (yy >= GROUND - 1) & (yy <= GROUND + 3) & (rgba[..., 3] == 0) & (d[None, :] < BODY_END - 6)
    rgba[shadow] = SHADOW
    return Image.fromarray(pg.upscale(rgba), "RGBA")


def preview(head_image, body_image, tongue_image, path):
    """물결(땅 위 S자 홈) 위에 몸통 띠를 세운 모습. 게임(SnakeTrain)과 같은 방법으로 2 논리 px 띠마다 높이를 바꾼다."""
    px = 4                                  # 논리 px 하나 = 그림 4px
    width, height = (BODY_END + 70) * px, 80 * px
    canvas = Image.new("RGBA", (width, height), (196, 168, 120, 255))
    head_x, ground = 40 * px, 60 * px

    def groove(d):
        return int(round(np.sin((d + 40) * 2 * np.pi / GROOVE_LENGTH) * GROOVE_DEPTH)) * px

    pivot_y = body_image.height - 2 * px
    for d in range(BODY_START, BODY_END, 2):
        left = (d - BODY_START) * px
        strip = body_image.crop((left, 0, left + 2 * px, body_image.height))
        canvas.alpha_composite(strip, (head_x + d * px, ground + groove(d) - pivot_y))
    head_left, head_top = head_x - 30 * px, ground + groove(NECK) - (head_image.height - 2 * px)
    root_x, root_y = head_left + TONGUE_ROOT[0] * 2, head_top + TONGUE_ROOT[1] * 2
    canvas.alpha_composite(tongue_image, (root_x - tongue_image.width, root_y - tongue_image.height // 2))
    canvas.alpha_composite(head_image, (head_left, head_top))
    canvas.save(path)


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/SnakeTrain"
    out.mkdir(parents=True, exist_ok=True)
    head_image = head()
    head_image.save(out / "head.png")
    body_image = body()
    body_image.save(out / "body.png")
    tongues = [tongue(frame) for frame in range(4)]
    for frame, image in enumerate(tongues):
        image.save(out / f"tongue_{frame}.png")
    preview(head_image, body_image, tongues[2], project / "Docs/ArtUpgrade/PixelGen/snake_train_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
