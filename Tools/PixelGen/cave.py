"""호빗 소굴(hobbitCave, 7장)을 새로 짠다(다음 장들의 맵과 소품을 생성기로).

산속 바위 동굴 하나가 호빗 사기꾼들의 소굴이다. 웹게임은 네모난 돌 타일 방이지만 자연 동굴로 다시 짰다:
울퉁불퉁한 바위(위에서 본 바위 윗면, 바닥 북쪽 가장자리에는 드러난 벽면), 다져진 흙바닥과 납작한 돌·자갈·물웅덩이,
벽면 횃불과 부엌 모닥불이 어둠을 밝힌다. 호빗들이 마차 손님에게서 빼앗은 짐이 곳곳에 쌓여 있다:
북동쪽 구석에 훔친 짐 더미(자루·여행 가방·보물 상자·금화), 북서쪽에 짚 잠자리, 남서쪽에 창고(상자·통·손수레),
남동쪽에 부엌(솥을 건 모닥불, 탁자와 의자). 가운데는 7장 싸움(호빗 셋, 거대 호빗)과 루가가 들어오는 길이라 비운다.
남쪽 굴길이 산길(caveRoad)로 이어진다. 지점은 웹게임 칸 그대로다(giant, hobbit ×4, arrive, ruga, entrance).

사용: python Tools/PixelGen/cave.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/hobbitCave/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/hobbitCave_preview.png     미리보기
"""
import sys
from pathlib import Path

import numpy as np

import guild_furniture as gf
import interior
import pixelgen as pg
import terrain
from houses import IRON, WOOD, Canvas, outline
from layout import SUB, MapLayout

WIDTH, HEIGHT = 28, 18
T = pg.TILE
# 바닥 북쪽 가장자리에 드러난 바위 벽면의 높이(점)
FACE = 30
ROCK = pg.hexes("#141217", "#1d1b22", "#28262e", "#35333c", "#45434d", "#5a5862")
FLOOR = pg.hexes("#231d1a", "#30282a", "#3d3330", "#4b3f38", "#5a4c42", "#6c5c4e")
MOSS = pg.hexes("#1a2419", "#26341f")
CLOTH = pg.hexes("#4a3a2a", "#6a5238", "#8a6c48", "#a88a5e")
STRAW = pg.hexes("#5a4420", "#7a5e2c", "#9c7c3c", "#c0a050", "#dcc070")
GOLDEN = pg.hexes("#7a5410", "#b07c18", "#e0aa30", "#f8d868")
LEATHER = pg.hexes("#3a1e12", "#5a301c", "#7a4428", "#9a5a36")

# 동굴 방: 가운데 큰 방, 북쪽 가운데 거대 호빗이 버티는 둔덕, 네 귀퉁이 구석(가운데 x, y, 반지름 x, y, 칸), 남쪽 굴길(x0, x1, 위 y).
# 바위 테두리가 한두 칸은 남게 맵보다 작게 잡는다.
HALL = (13.5, 8.5, 11.0, 5.8)
THRONE = (13.5, 3.6, 3.0, 1.6)
NOOKS = [(4.8, 4.6, 3.0, 2.2), (22.4, 4.4, 3.2, 2.2), (4.4, 12.0, 2.8, 2.0), (22.6, 12.0, 3.0, 2.2)]
TUNNEL = (12.2, 15.8, 12.0)
# 바닥에 다져진 길: 굴 입구에서 가운데로, 가운데에서 네 구석으로(칸)
WORN = [[(13.9, 18), (13.9, 13), (13.5, 9)], [(13.5, 9), (5, 5)], [(13.5, 9), (22, 4.6)], [(13.5, 9), (4.6, 12)], [(13.5, 9), (22.4, 12)]]


def cave_floor(rng):
    """
    바닥인 점(True)과 바위인 점. 타원 몇 개를 큰 노이즈로 일그러뜨려 바위 가장자리가 굽이지고,
    흐리게 한 뒤 다시 나눠 점 몇 개짜리 바위 부스러기나 바닥 틈이 남지 않게 한다.
    """
    h, w = HEIGHT * T, WIDTH * T
    yy, xx = np.mgrid[0:h, 0:w]
    x, y = (xx + 0.5) / T, (yy + 0.5) / T
    wobble = (pg.fractal_noise(h, w, rng, ((72, 0.7), (28, 0.3))) - 0.5) * 0.16
    inside = np.full((h, w), -1.0)
    for cx, cy, rx, ry in [HALL, THRONE] + NOOKS:
        inside = np.maximum(inside, 1 - np.hypot((x - cx) / rx, (y - cy) / ry))
    x0, x1, top = TUNNEL
    # 남쪽 굴길: 조금 구불거리는 띠
    lane = np.where(y > top, 1 - np.abs(x - (x0 + x1) / 2 + np.sin(y * 1.3) * 0.25) / ((x1 - x0) / 2), -1)
    inside = np.maximum(inside, lane * 0.5)
    return terrain.box_blur((inside + wobble > 0).astype(float), 4) > 0.5


def faces(floor):
    """바닥 바로 위 바위 가운데 FACE점 높이만큼은 벽면이다(바닥에서 올려다본 바위의 옆면). 벽면 안에서 아래에서 잰 높이도 돌려준다."""
    face = np.zeros_like(floor)
    height = np.zeros(floor.shape, dtype=int)
    rock = ~floor
    for k in range(1, FACE + 1):
        above = np.zeros_like(floor)
        above[:-k] = floor[k:]
        cell = rock & above & ~face
        # 그 사이에 다른 바닥이 끼면(바위가 얇으면) 벽면이 아니다.
        face |= cell
        height[cell] = k
    return face, height


def paint_ground(floor, face, face_height, rng):
    h, w = floor.shape
    image = np.zeros((h, w, 3), dtype=np.uint8)
    rock = ~floor

    # 바위 윗면: 노이즈 높이를 비스듬한 빛으로 비춰 둥근 바위 덩어리처럼 보이게 한다.
    bumps = pg.fractal_noise(h, w, rng, ((56, 0.5), (18, 0.32), (5, 0.18)))
    gy, gx = np.gradient(bumps)
    shade = np.clip(0.42 + (-gx * 0.7 - gy * 0.9) * 22 + (bumps - 0.5) * 0.6, 0, 0.999)
    image[rock] = pg.quantize(shade, ROCK[:5], 0.12)[rock]
    moss = pg.fractal_noise(h, w, rng, ((20, 0.6), (6, 0.4))) > 0.74
    image[rock & moss & ~face & (shade > 0.35)] = MOSS[0]

    # 벽면: 세로 결이 진 바위, 위 끝은 밝은 턱, 아래로 갈수록 어둡다.
    grain = pg.fractal_noise(h, w, np.random.default_rng(3), ((3, 0.5), (40, 0.5)))
    grain = np.repeat(grain[:1], h, axis=0) * 0.6 + grain * 0.4
    depth = face_height / FACE
    level = np.clip(0.2 + depth * 0.5 + (grain - 0.5) * 0.6, 0, 0.999)
    image[face] = pg.quantize(level, ROCK[1:], 0.1)[face]
    lip = face & ~np.pad(face, ((1, 0), (0, 0)))[:-1]
    image[lip] = ROCK[5]

    # 바닥: 다진 흙. 벽 가까이일수록 어둡다(벽에서 떨어진 거리, 점).
    near = np.zeros((h, w))
    reach = rock.copy()
    for k in range(1, 22):
        reach = reach | pg.neighbours(reach)
        near[floor & reach & (near == 0)] = k
    near[floor & (near == 0)] = 22
    soil = pg.fractal_noise(h, w, rng, ((20, 0.5), (6, 0.3), (2, 0.2)))
    # 호빗들이 오가며 다진 길은 조금 밝다.
    yy, xx = np.mgrid[0:h, 0:w]
    worn = np.zeros((h, w))
    for path in WORN:
        for (ax, ay), (bx, by) in zip(path, path[1:]):
            a, b = np.array([ax, ay]) * T, np.array([bx, by]) * T
            ab = b - a
            t = np.clip(((xx - a[0]) * ab[0] + (yy - a[1]) * ab[1]) / (ab @ ab), 0, 1)
            gap = np.hypot(xx - (a[0] + ab[0] * t), yy - (a[1] + ab[1] * t))
            worn = np.maximum(worn, np.clip(1 - gap / (T * 0.9), 0, 1))
    light = np.clip(0.26 + np.minimum(near, 22) / 22 * 0.42 + (soil - 0.5) * 0.35 + worn * 0.1, 0, 0.999)
    image[floor] = pg.quantize(light, FLOOR, 0.14)[floor]
    # 벽면 아래 바닥에 드리운 그림자
    shadow = np.zeros_like(floor)
    for k in range(1, 8):
        shadow |= np.pad(face, ((k, 0), (0, 0)))[:-k]
    shadow &= floor
    image[shadow] = (image[shadow] * 0.7).astype(np.uint8)

    # 납작한 돌, 자갈, 갈라진 틈
    for _ in range(40):
        cx, cy = rng.uniform(0, w), rng.uniform(0, h)
        rx, ry = rng.uniform(4, 9), rng.uniform(3, 6)
        stone = (((np.arange(w)[None, :] - cx) / rx) ** 2 + ((np.arange(h)[:, None] - cy) / ry) ** 2 <= 1) & floor
        if stone.sum() < 10 or (stone & shadow).any():
            continue
        image[stone] = pg.STONE[2]
        top_edge = stone & ~np.pad(stone, ((1, 0), (0, 0)))[:-1]
        image[top_edge] = pg.STONE[3]
        image[stone & ~np.pad(stone, ((0, 1), (0, 0)))[1:]] = pg.STONE[1]
    pebbles = floor & (rng.random((h, w)) < 0.006)
    image[pebbles] = FLOOR[5]
    image[np.roll(pebbles, 1, axis=0) & floor] = FLOOR[1]
    for _ in range(14):
        x, y = rng.uniform(0, w), rng.uniform(0, h)
        angle = rng.uniform(0, np.pi)
        for _ in range(int(rng.integers(8, 22))):
            xi, yi = int(x), int(y)
            if 0 <= yi < h and 0 <= xi < w and floor[yi, xi]:
                image[yi, xi] = FLOOR[0]
            angle += rng.normal(0, 0.5)
            x, y = x + np.cos(angle), y + np.sin(angle) * 0.7

    # 바위와 바닥 사이 테두리: 바닥 쪽에 어두운 선을 둘러 바위가 솟아 보이게 한다.
    ring = floor & pg.neighbours(rock & ~face)
    image[ring] = pg.OUTLINE
    return image


def scatter(image, floor, cx, cy, radius, colors, count, rng):
    """그 둘레(점)에 바닥 위 작은 알갱이(짚 부스러기, 금화, 숯)를 흩뿌린다."""
    for _ in range(count):
        a, r = rng.uniform(0, np.pi * 2), radius * np.sqrt(rng.random())
        x, y = int(cx + np.cos(a) * r), int(cy + np.sin(a) * r * 0.7)
        if 0 <= y < floor.shape[0] - 1 and 0 <= x < floor.shape[1] - 1 and floor[y, x]:
            image[y, x:x + 2] = colors[int(rng.integers(0, len(colors)))]


def soot(image, floor, cx, cy, radius):
    """모닥불 둘레 바닥이 그을렸다."""
    yy, xx = np.mgrid[0:floor.shape[0], 0:floor.shape[1]]
    burnt = (np.hypot(xx - cx, (yy - cy) * 1.4) < radius) & floor
    image[burnt] = (image[burnt] * 0.72).astype(np.uint8)


def puddle(image, floor, cx, cy, rx, ry):
    """천장에서 떨어진 물이 고인 웅덩이: 어두운 물, 가장자리 젖은 흙, 반짝임 한 줄."""
    h, w = floor.shape
    yy, xx = np.mgrid[0:h, 0:w]
    # 둘레가 고르지 않게 노이즈로 일그러뜨린다.
    ripple = (pg.fractal_noise(h, w, np.random.default_rng(int(cx + cy)), ((8, 1.0),)) - 0.5) * 0.7
    reach = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 + ripple
    wet = (reach <= 1.8) & floor
    pool = (reach <= 1) & floor
    image[wet] = (image[wet] * 0.75).astype(np.uint8)
    image[pool] = pg.WATER[1]
    image[pool & (yy < cy - ry * 0.3)] = pg.WATER[2]
    image[pool & (np.abs(yy - cy + 1) < 0.5) & (np.abs(xx - cx + 2) < rx * 0.4)] = pg.WATER[5]


# ── 소품 ───────────────────────────────────────────────

def stalagmite(rng, height=34):
    """바닥에서 솟은 돌기둥: 아래가 넓고 끝이 뾰족하며, 왼쪽이 밝다."""
    w = 22
    c = Canvas(w, height + 2)
    cx, ground = w / 2, height
    body = c.polygon([(cx - 9, ground), (cx - 3, ground - height * 0.55), (cx - 0.5, 1), (cx + 1.5, ground - height * 0.5), (cx + 9, ground)])
    c.paint(body, ROCK[3])
    c.paint(body & (c.xx < cx - 1), ROCK[4])
    c.paint(body & (c.xx > cx + 3), ROCK[2])
    c.paint(body & (c.yy > ground - 4), ROCK[2])
    return outline(c.image), (int(cx), ground)


def sack(rng, tint=0):
    """묶은 자루: 불룩한 몸통, 끈으로 묶은 목."""
    c = Canvas(24, 26)
    cx, ground = 12, 24
    body = c.ellipse(cx, ground - 8, 10, 9)
    neck = c.polygon([(cx - 4, 5), (cx + 4, 5), (cx + 6, 10), (cx - 6, 10)])
    c.paint(body | neck, CLOTH[1 + tint])
    c.paint(body & (c.xx < cx - 2), CLOTH[2 + tint])
    c.paint(c.rect(cx - 5, 8, cx + 5, 10), CLOTH[0])
    c.paint(c.ellipse(cx, 4, 4, 2.5), CLOTH[2 + tint])
    return outline(c.image), (cx, ground)


def suitcase(rng, color=LEATHER):
    """빼앗은 여행 가방: 가죽 몸통, 손잡이, 놋쇠 걸쇠."""
    c = Canvas(30, 24)
    cx, ground = 15, 22
    body = c.rect(2, 7, 28, ground)
    c.paint(body, color[2])
    c.paint(c.rect(2, 7, 28, 10), color[3])
    c.paint(c.rect(2, 14, 28, 15), color[1])
    c.paint(c.rect(11, 2, 19, 4) | c.rect(11, 2, 13, 8) | c.rect(17, 2, 19, 8), color[0])
    for x in (6, 22):
        c.paint(c.rect(x, 9, x + 2, 12), GOLDEN[2])
    return outline(c.image), (cx, ground)


def loot_heap(rng):
    """훔친 짐 더미: 자루·가방·두루마리 천이 쌓이고 금화가 흘러내린다(세운 것 한 장)."""
    c = Canvas(96, 60)
    image = c.image
    pieces = [(sack(rng, 1), 18, 58), (suitcase(rng), 46, 58), (sack(rng), 72, 58), (suitcase(rng, pg.hexes("#1c2a3a", "#28405a", "#38587a", "#4a72a0")), 30, 40),
              (sack(rng, 1), 60, 42), (interior.gold_pile(rng), 50, 26)]
    for (sprite, pivot), fx, fy in pieces:
        sh, sw = sprite.shape[:2]
        left, top = fx - pivot[0], fy - pivot[1]
        region = image[max(0, top):top + sh, max(0, left):left + sw]
        src = sprite[max(0, -top):max(0, -top) + region.shape[0], max(0, -left):max(0, -left) + region.shape[1]]
        opaque = src[..., 3] > 0
        region[opaque] = src[opaque]
    # 바닥에 흘러내린 금화
    for _ in range(16):
        x, y = int(rng.uniform(8, 88)), int(rng.uniform(52, 59))
        image[y:y + 2, x:x + 2, :3] = GOLDEN[int(rng.integers(1, 4))]
        image[y:y + 2, x:x + 2, 3] = 255
    return image, (48, 58)


def straw_bed(rng):
    """짚을 깔고 누더기 담요를 덮은 잠자리(바닥에 까는 것)."""
    c = Canvas(56, 30)
    straw = c.ellipse(28, 15, 26, 13)
    c.paint(straw, STRAW[2])
    c.paint(straw & ((c.xx * 5 + c.yy * 3 + (c.xx * c.yy) % 7) % 9 == 0), STRAW[4])
    c.paint(straw & ((c.xx * 3 + c.yy * 7) % 11 == 0), STRAW[1])
    blanket = c.polygon([(14, 8), (46, 6), (50, 22), (12, 24)])
    c.paint(blanket, pg.hexes("#5a3a5a")[0])
    c.paint(blanket & ((c.xx // 5 + c.yy // 5) % 2 == 0), pg.hexes("#7a4e6a")[0])
    c.paint(c.ellipse(9, 14, 6, 4), pg.hexes("#c8b898")[0])
    return outline(c.image)


def campfire(rng):
    """돌을 둘러 쌓은 모닥불 자리와 엇갈린 장작(불꽃은 맵의 움직이는 불이 얹는다)."""
    c = Canvas(40, 26)
    cx, ground = 20, 22
    for k in range(9):
        a = k / 9 * np.pi * 2
        sx, sy = cx + np.cos(a) * 15, ground - 7 + np.sin(a) * 7
        stone = c.ellipse(sx, sy, 4, 3)
        c.paint(stone, pg.STONE[3])
        c.paint(stone & (c.yy > sy), pg.STONE[2])
    c.paint(c.ellipse(cx, ground - 7, 10, 4.5), pg.hexes("#1a1210")[0])
    c.line(cx - 9, ground - 4, cx + 7, ground - 10, WOOD[2], 3)
    c.line(cx - 7, ground - 10, cx + 9, ground - 4, WOOD[1], 3)
    c.paint(c.ellipse(cx, ground - 7, 5, 2.5), pg.hexes("#e07030")[0])
    return outline(c.image), (cx, ground)


def tripod_pot(rng):
    """모닥불 위에 세운 나무 세 발과 매단 솥."""
    c = Canvas(44, 52)
    cx, ground = 22, 50
    for fx in (4, 40):
        c.line(fx, ground, cx, 2, WOOD[2], 2)
    c.line(cx + 2, ground - 4, cx, 2, WOOD[1], 2)
    c.line(cx, 3, cx, 20, IRON[1], 1)
    pot = c.ellipse(cx, 27, 9, 7) & (c.yy > 21)
    c.paint(pot, IRON[1])
    c.paint(pot & (c.xx < cx - 3), IRON[2])
    c.paint(c.rect(cx - 9, 21, cx + 10, 23), IRON[2])
    c.paint(c.ellipse(cx, 21.5, 7, 1.2), pg.hexes("#8a5a2a")[0])
    return outline(c.image), (cx, ground)


def hand_cart(rng):
    """짐을 실은 손수레(호빗 마차 장사의 손수레): 나무 짐칸, 바퀴 둘, 앞으로 뻗은 손잡이."""
    c = Canvas(64, 40)
    ground = 38
    c.paint(c.rect(6, 12, 48, 26), WOOD[2])
    c.paint(c.rect(6, 12, 48, 15), WOOD[3])
    for x in (14, 28, 42):
        c.paint(c.rect(x, 15, x + 1, 26), WOOD[1])
    c.line(48, 22, 62, 30, WOOD[1], 2)
    for wx in (14, 40):
        wheel = c.ellipse(wx, ground - 8, 8, 8) & ~c.ellipse(wx, ground - 8, 5, 5)
        c.paint(wheel, WOOD[1])
        c.paint(c.ellipse(wx, ground - 8, 2, 2), IRON[2])
        c.line(wx - 5, ground - 8, wx + 5, ground - 8, WOOD[1], 1)
        c.line(wx, ground - 13, wx, ground - 3, WOOD[1], 1)
    sack_sprite, pivot = sack(rng)
    image = c.image
    left, top = 26 - pivot[0], 14 - pivot[1]
    opaque = sack_sprite[..., 3] > 0
    ys, xs = np.nonzero(opaque)
    keep = (top + ys >= 0)
    image[top + ys[keep], left + xs[keep]] = sack_sprite[ys[keep], xs[keep]]
    return outline(image), (32, ground)


def torch_bracket(m, image, face_height, x, rng):
    """그 칸 열 벽면 가운데쯤에 쇠 횃불 받침을 그리고 움직이는 불꽃(torch)과 흔들리는 빛을 둔다."""
    column = x * T + 16
    rows = np.nonzero(face_height[:, column] > 0)[0]
    if len(rows) == 0:
        return
    bottom = rows.max()
    row = int((bottom - FACE * 0.55) // T)
    fx, fy = x * T + 16, row * T + 14
    image[fy:fy + 4, fx - 3:fx + 4] = IRON[2]
    image[fy + 4:fy + 16, fx - 1:fx + 2] = IRON[1]
    image[fy + 14:fy + 17, fx - 4:fx + 5] = IRON[2]
    m.fire(x, row, "torch")
    m.light(x * 16 + 8, row * 16 + 7, 46, "#ffab4a", flicker=True)


def build():
    m = MapLayout("hobbitCave", "호빗 소굴", WIDTH, HEIGHT, theme="cave", music="castle",
                  ambient={"color": "#120a06", "alpha": 0.42}, seed=131)
    rng = np.random.default_rng(131)
    floor = cave_floor(rng)
    face, face_height = faces(floor)
    image = paint_ground(floor, face, face_height, rng)
    puddle(image, floor, 3.8 * T, 9.8 * T, 16, 8)
    puddle(image, floor, 17.2 * T, 4.3 * T, 12, 6)
    scatter(image, floor, 4.6 * T, 4.8 * T, 70, STRAW[1:4], 140, rng)
    scatter(image, floor, 22.4 * T, 5.4 * T, 60, GOLDEN[1:], 40, rng)
    soot(image, floor, 23.5 * T, 12.4 * T, 44)
    scatter(image, floor, 23.5 * T, 12.4 * T, 40, pg.hexes("#141010", "#2a2220"), 60, rng)

    # 충돌: 한 칸을 4×4로 나눈 칸마다 바위가 40%를 넘으면 막는다.
    piece = T // SUB
    rock_share = (~floor).reshape(HEIGHT * SUB, piece, WIDTH * SUB, piece).mean(axis=(1, 3))
    m.solid |= rock_share > 0.4
    m.border()

    # 벽면 횃불(북쪽 벽면과 굴길 입구 양옆)
    for x in (7, 12, 16, 21):
        torch_bracket(m, image, face_height, x, rng)
    m.paint_ground = lambda: image

    # ── 북서쪽 짚 잠자리 ──
    for k, (x, y) in enumerate(((2.6, 3.4), (4.6, 5.6))):
        m.decal(f"straw_bed_{k}", straw_bed(np.random.default_rng(140 + k)), x * T, y * T)
    m.stand("lantern_nook", gf.lantern(rng), 6.6, 3.9, block=(0.5, 0.4))
    m.light(6.6 * 16, 3.5 * 16, 30, "#ffc870", flicker=True)

    # ── 북동쪽 훔친 짐 더미 ──
    m.stand("loot_heap", loot_heap(rng), 22.6, 5.2, block=(3, 1.2))
    m.stand("chest_loot", interior.treasure_chest(rng), 24.8, 5.5, block=(1.3, 0.7))
    m.stand("open_chest", interior.open_chest(rng), 19.8, 4.2, block=(1.3, 0.7))

    # ── 남서쪽 창고: 상자·통·손수레 ──
    for kind, x, y in (("crate", 2, 11), ("barrel", 3, 11), ("crate", 2, 12), ("barrel", 5, 13)):
        m.thing(kind, x, y)
    m.stand("hand_cart", hand_cart(rng), 7.4, 12.6, block=(2.2, 0.8))

    # ── 남동쪽 부엌: 모닥불과 솥, 탁자와 의자 ──
    m.stand("campfire", campfire(rng), 23.5, 12.7, block=(1.2, 0.6))
    m.stand("tripod_pot", tripod_pot(rng), 23.5, 12.8)
    m.fire(23, 12, "fire")
    m.light(23.5 * 16, 12.2 * 16, 64, "#ff9a40", flicker=True)
    m.stand("table_kitchen", gf.round_table(rng), 20.8, 12.6, block=(1.4, 0.6))
    m.stand("stool_kitchen_a", gf.stool(rng), 19.7, 12.8, block=(0.5, 0.3))
    m.stand("stool_kitchen_b", gf.stool(rng), 21.9, 12.8, block=(0.5, 0.3))
    m.stand("sack_kitchen", sack(rng), 24.8, 11.2, block=(0.6, 0.4))

    # ── 바위 기둥(벽 가까이) ──
    for k, (x, ground) in enumerate(((3.0, 8.2), (24.0, 8.6), (10.0, 3.4), (17.2, 13.6))):
        m.stand(f"stalagmite_{k}", stalagmite(np.random.default_rng(150 + k), 30 + 6 * (k % 2)), x, ground, block=(0.6, 0.4))

    # ── 7장 지점(웹게임 칸): 거대 호빗, 호빗 넷, 덮치는 자리, 루가가 서는 자리, 입구 ──
    m.point("giant", (13, 3))
    m.point("chest", (13, 2))
    m.point("hobbit", (8, 5), (18, 5), (7, 9), (19, 9))
    m.point("arrive", (13, 9))
    m.point("ruga", (13, 14))
    m.point("entrance", (13, 17))
    m.warp("entrance", "hobbitTrail", "mouthArrive", "down")
    # 입구 굴길 아래 끝은 맵 밖으로 이어진다.
    m.keep_open(12, 17, 15, 18)
    return m


def main(project):
    m = build()
    for goal in ("giant", "hobbit", "arrive", "ruga"):
        for cell in m.points[goal]:
            if not m.reachable(m.points["entrance"][0], [cell]):
                print(f"입구에서 {goal} {cell}에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
