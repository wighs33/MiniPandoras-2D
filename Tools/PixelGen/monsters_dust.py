"""몬스터 1~5레벨: 주인 없는 소울더스트와 사람의 원념이 흔한 것에 깃들어 생긴 것들(monsters.py의 도감 차례).

  1 솜먼지      떠도는 더스트가 뭉친 털뭉치. 청록 눈 하나, 아래로 흩어지는 더스트 꼬리
  2 이삭도깨비  밭에 떨어진 더스트가 깃든 볏단. 이삭 머리, 새끼줄 허리띠, 씨앗 눈, 짚 다리
  3 깍지돌      강가 조약돌 셋이 더스트로 엉겨 붙은 것. 돌 틈과 눈에서 청록 빛, 이끼
  4 등롱버섯    숲 그늘 버섯 갓이 속에서부터 등불처럼 빛난다. 흰 대에 얼굴, 뿌리 발, 날리는 포자
  5 넝마귀      버려진 누더기에 원념이 깃들어 떠다닌다. 짝짝이 단추 눈, 기운 천 조각, 찢긴 자락
그리는 함수는 (방향, 장) → 점 그림이다(monster_kit.save_sheet가 세 방향 세 장을 저장한다).
"""
import numpy as np

import pixelgen as pg
from monster_kit import GLINT, ball, blob, canvas, chain, glow_eye, limb, shade, spark, speckle, stripes

# ── 1 솜먼지 ───────────────────────────────────────────

FLUFF = pg.hexes("#7a6f5c", "#a89b80", "#cfc2a4", "#ebe1c6", "#fffaee")
TEAL = pg.hexes("#0b3d46", "#16707a", "#36b1ac", "#88ecdc", "#e6fffa")


def dust_puff(view, frame):
    c = canvas(18, 20)
    bob = (0, -1, 1)[frame]
    cx, cy = 18, 17 + bob
    # 몸 아래로 흩어지는 더스트 알갱이. 장마다 조금씩 흘러내린다.
    for k, (dx, dy) in enumerate(((-4, 12), (3, 15), (-1, 19))):
        sway = (frame - 1) * (1 if k % 2 else -1)
        spark(c, cx + dx + sway, cy + dy + frame % 2, 1.5 if k == 0 else 1, TEAL[2], TEAL[4])
    body = blob(c, cx, cy, 11, FLUFF, wobble=0.9, lobes=7, phase=frame * 0.6)
    blob(c, cx + 4, cy + 5, 6, FLUFF, wobble=0.6, lobes=5, phase=2.0 + frame * 0.4, bias=-0.04)
    blob(c, cx - 5, cy + 4, 5.5, FLUFF, wobble=0.6, lobes=4, phase=0.5 + frame * 0.4)
    blob(c, cx - 2, cy - 11, 3.4, FLUFF, wobble=0.6, lobes=5, phase=1.0, bias=0.12)
    speckle(c, body & (c.yy > cy + 3), FLUFF[1], 0.18, 11, (0, bob))
    speckle(c, body & (c.yy < cy - 4) & (c.xx < cx), FLUFF[4], 0.2, 12, (0, bob))
    if view == "up":
        # 등: 더스트가 소용돌이친 자국
        for k in range(10):
            a = k * 0.7
            spark(c, cx + np.cos(a) * (2 + k * 0.6), cy - 1 + np.sin(a) * (2 + k * 0.6), 0.5, TEAL[2], TEAL[3])
        return c
    ex = cx + (5 if view == "right" else 0)
    rx = 4.6 if view == "down" else 3.0
    c.paint(c.ellipse(ex, cy, rx + 1.2, 5.8), TEAL[0])
    glow_eye(c, ex, cy, rx, 4.6, [TEAL[1], TEAL[2], TEAL[3]])
    c.paint(c.ellipse(ex + (1 if view == "right" else 0), cy + 0.4, max(0.8, rx * 0.3), 2.6), TEAL[0])
    c.paint(c.ellipse(ex - rx * 0.4, cy - 2, 1.1, 1.1), GLINT)
    return c


# ── 2 이삭도깨비 ────────────────────────────────────────

STRAW = pg.hexes("#4a3210", "#7a5418", "#a87a26", "#d4a440", "#eccb6a", "#fbe8a4")
ROPE = pg.hexes("#24140a", "#4a2c18", "#6e462a", "#93683e")
SEED = np.array([34, 22, 12], dtype=np.uint8)


def _straw_leg(c, hip, foot):
    leg = limb(c, hip, foot, 3.4, 2.6, STRAW, bias=-0.05)
    stripes(c, leg, 2, STRAW[1])
    # 짚 끝이 발처럼 벌어진다.
    fx, fy = foot
    c.paint(c.polygon([(fx - 4, fy + 1), (fx + 4, fy + 1), (fx + 2, fy - 3), (fx - 2, fy - 3)]), STRAW[2])
    stripes(c, c.polygon([(fx - 4, fy + 1), (fx + 4, fy + 1), (fx + 2, fy - 3), (fx - 2, fy - 3)]), 2, STRAW[1], offset=1)


def _ear(c, root, tip, droop, seeds):
    """이삭 하나: 뿌리에서 끝으로 휘어 처지는 줄기에 낟알이 달린다."""
    (x0, y0), (x1, y1) = root, tip
    points = [(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t + droop * t * t) for t in np.linspace(0, 1, 5)]
    chain(c, points, 1.0, 0.7, STRAW, bias=0.1)
    for t in np.linspace(0.35, 1.0, seeds):
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t + droop * t * t
        ball(c, x, y, 1.6, 2.0, STRAW, bias=0.15)


def sheaf_imp(view, frame):
    c = canvas(22, 30)
    cx = 22
    lift = 1 if frame else 0
    # 다리: 걸음마다 한쪽 다리를 든다(옆에서는 앞뒤로 벌린다).
    if view == "right":
        swing = (0, 4, -4)[frame]
        for side in (-1, 1):
            _straw_leg(c, (cx + side * 1, 42), (cx + side * swing * 0.5 - (2 if side < 0 else -2), 58 - (2 if side * swing > 0 else 0)))
    else:
        for side in (-1, 1):
            up = 2 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
            _straw_leg(c, (cx + side * 5, 42), (cx + side * 6, 58 - up))
    top = 22 - lift
    narrow = 7.5 if view == "right" else 10
    body = ball(c, cx, top + 10, narrow, 13, STRAW)
    stripes(c, body, 2, STRAW[1])
    stripes(c, body & (c.xx < cx - 2), 4, STRAW[4], offset=1)
    # 머리의 이삭: 위로 솟았다가 끝이 바깥으로 처지는 이삭 일곱(볏단을 묶은 위쪽이 벌어진 모양).
    sway = (0, 1, -1)[frame]
    for k in range(-3, 4):
        spread = k * (1.6 if view == "right" else 2.4)
        _ear(c, (cx + k * 1.0, top + 2), (cx + spread * 2.4 + sway, top - 13 + abs(k) * 2.4), 1.5 + abs(k) * 1.4, 4)
    # 새끼줄 허리띠: 몸을 가로지르는 꼬인 줄과 앞의 매듭
    belt = body & (c.yy >= top + 14) & (c.yy < top + 18)
    shade(c, belt, 0.5 - (c.xx - cx) / 60, ROPE)
    stripes(c, belt, 3, ROPE[0], slope=1.0)
    stripes(c, belt, 3, ROPE[3], offset=1, slope=1.0)
    # 팔: 짚 다발이 걸음에 맞춰 앞뒤로 흔들린다.
    arm = (0, 2, -2)[frame]
    if view == "right":
        a = limb(c, (cx + 2, top + 8), (cx + 7 + arm, top + 17), 2.0, 1.4, STRAW, bias=0.05)
        stripes(c, a, 2, STRAW[1])
    else:
        for side in (-1, 1):
            a = limb(c, (cx + side * 9, top + 7), (cx + side * 13, top + 16 + side * arm * 0.5), 2.0, 1.4, STRAW, bias=0.05)
            stripes(c, a, 2, STRAW[1])
    if view == "up":
        return c
    if view == "down":
        ball(c, cx + 1, top + 17, 2.2, 2.0, ROPE, bias=0.1)
        for dx in (-1, 2):
            limb(c, (cx + dx, top + 18), (cx + dx * 1.5, top + 23), 0.9, 0.7, ROPE)
        for ex in (cx - 4, cx + 4):
            c.paint(c.ellipse(ex, top + 8, 1.7, 2.5), SEED)
            c.paint(c.ellipse(ex - 0.6, top + 7, 0.7, 0.7), GLINT)
        c.paint(c.polygon([(cx - 2, top + 12), (cx + 2, top + 12), (cx, top + 13.6)]), SEED)
    else:
        c.paint(c.ellipse(cx + 4.5, top + 8, 1.4, 2.4), SEED)
        c.paint(c.ellipse(cx + 4.0, top + 7, 0.6, 0.6), GLINT)
        c.paint(c.polygon([(cx + 4, top + 12), (cx + 7, top + 12), (cx + 6, top + 13.5)]), SEED)
    return c


# ── 3 깍지돌 ───────────────────────────────────────────

PEBBLE = pg.hexes("#2a2e36", "#434954", "#616874", "#838a94", "#a8aeb5", "#cdd2d6")
MOSS = pg.hexes("#2a4a22", "#3f6a2c", "#5e8a3a", "#86ac4e")


def _moss(c, mask, cy, ry, seed, shift):
    top = mask & (c.yy < cy - ry * 0.25)
    speckle(c, top, MOSS[1], 0.55, seed, shift)
    speckle(c, top & (c.yy < cy - ry * 0.55), MOSS[2], 0.5, seed + 1, shift)
    speckle(c, top & (c.yy < cy - ry * 0.7) & (c.xx < c.xx.mean()), MOSS[3], 0.35, seed + 2, shift)


def _seam(c, inner, outer):
    """두 돌이 맞닿은 틈(inner 돌의 테두리가 outer 돌 위에 걸친 곳)을 청록으로 빛낸다."""
    edge = pg.neighbours(inner) & ~inner & outer
    c.paint(edge, TEAL[2])


def pebble_bundle(view, frame):
    c = canvas(24, 22)
    roll = (0, -1, 1)[frame]
    hop = 1 if frame else 0
    shift = (roll, -hop)
    if view == "right":
        small = ball(c, 9 + roll, 24 - hop, 6, 5, PEBBLE)
        big = ball(c, 22 + roll, 30 - hop, 15, 11, PEBBLE)
        _seam(c, big, small)
        head_x = 32 + roll * 2
    elif view == "up":
        small = ball(c, 37 + roll, 24 - hop, 6, 5, PEBBLE, bias=-0.05)
        big = ball(c, 24 + roll, 30 - hop, 15, 11, PEBBLE)
        _seam(c, big, small)
        head_x = 24 + roll * 2
    else:
        small = ball(c, 11 + roll, 22 - hop, 6, 5, PEBBLE, bias=-0.08)
        big = ball(c, 24 + roll, 30 - hop, 15, 11, PEBBLE)
        _seam(c, big, small)
        head_x = 24 + roll * 2
    _moss(c, big, 30 - hop, 11, 31, shift)
    # 큰 돌의 금: 안에서 청록 빛이 샌다.
    for (x0, y0), (x1, y1) in (((14, 32), (18, 35)), ((18, 35), (17, 39)), ((30, 28), (34, 33))):
        limb(c, (x0 + roll, y0 - hop), (x1 + roll, y1 - hop), 0.6, 0.5, TEAL[1:4])
    head = ball(c, head_x, 15 - hop, 9, 8, PEBBLE, bias=0.05)
    _seam(c, head, big)
    _moss(c, head, 15 - hop, 8, 41, shift)
    if view == "down":
        for ex in (head_x - 4, head_x + 4):
            glow_eye(c, ex, 16 - hop, 2.4, 1.2, [TEAL[1], TEAL[3], TEAL[4]])
    elif view == "right":
        glow_eye(c, head_x + 5, 16 - hop, 1.8, 1.2, [TEAL[1], TEAL[3], TEAL[4]])
    return c


# ── 4 등롱버섯 ─────────────────────────────────────────

CAP = pg.hexes("#5a1a10", "#8a2c14", "#b8461a", "#e0702a", "#f4a050", "#ffd890")
CAP_GLOW = pg.hexes("#c86a1a", "#f0a030", "#ffd860", "#fff4c0")
STEM = pg.hexes("#5e5040", "#8a7c66", "#b8ac94", "#e2dac6", "#f6f1e4")
HANGER = pg.hexes("#1e1a18", "#3e3632", "#6a5e52")
FACE = np.array([40, 26, 20], dtype=np.uint8)


def lantern_cap(view, frame):
    c = canvas(22, 30)
    cx = 22
    bob = 1 if frame else 0
    # 뿌리 발: 걸음마다 한쪽을 든다.
    for side in (-1, 1):
        up = 2 if (frame == 1 and side < 0) or (frame == 2 and side > 0) else 0
        fx = cx + side * (3 if view == "right" else 5) + (side * 2 if view == "right" and frame else 0)
        ball(c, fx, 55 - up, 3.6, 2.6, STEM, bias=-0.15)
    stem = limb(c, (cx, 30 - bob), (cx, 50), 6.0, 7.2, STEM)
    speckle(c, stem & (c.xx > cx + 2), STEM[1], 0.25, 51, (0, -bob))
    # 작은 팔
    sway = (0, 1, -1)[frame]
    if view == "right":
        ball(c, cx + 5, 41 - bob + sway, 2.2, 2.0, STEM, bias=0.1)
    else:
        for side in (-1, 1):
            ball(c, cx + side * 7.5, 41 - bob + side * sway, 2.2, 2.0, STEM, bias=0.1)
    # 갓: 종 모양, 속에서부터 빛나는 종이 등롱처럼 가운데가 밝다. 아랫면은 빛나는 주름.
    top = 6 - bob
    cy = top + 15
    nx = (c.xx + 0.5 - cx) / 19
    ny = (c.yy + 0.5 - cy) / 15
    cap = (nx ** 2 + ny ** 2 <= 1) & (c.yy < cy + 6)
    light = 0.95 - nx ** 2 * 0.75 - np.clip(-ny, 0, 1) * 0.25 + np.clip(ny, 0, 1) * 0.1
    shade(c, cap, light, CAP, dither=0.15)
    # 등롱 살: 위에서 아래로 휘는 줄 일곱
    for k in range(-3, 4):
        x = cx + k * 5.2 * np.sqrt(np.clip(1 - ny ** 2, 0, 1))
        c.paint(cap & (np.abs(c.xx + 0.5 - x) < 0.55) & (c.yy > top + 2), CAP[1])
    gills = (nx ** 2 <= 0.85) & (c.yy >= cy + 5) & (c.yy < cy + 8)
    shade(c, gills, 0.75 - nx ** 2 * 0.5, CAP_GLOW)
    stripes(c, gills, 3, CAP_GLOW[0])
    # 갓 꼭지: 등롱을 매다는 고리
    ball(c, cx, top + 0.5, 3, 2, HANGER, bias=0.1)
    c.paint(c.ellipse(cx, top - 2.5, 2.2, 2.2) & ~c.ellipse(cx, top - 2.5, 1.1, 1.1), HANGER[1])
    # 날리는 포자
    for k, (sx, sy) in enumerate(((-15, 30), (14, 26), (-9, 14), (16, 12))):
        if (k + frame) % 3 != 2:
            spark(c, cx + sx + (frame - 1), sy - frame - bob, 1, CAP_GLOW[2], CAP_GLOW[3])
    if view == "up":
        return c
    fx = cx + (4 if view == "right" else 0)
    eyes = (fx + 1.5,) if view == "right" else (fx - 3.5, fx + 3.5)
    for ex in eyes:
        c.paint(c.ellipse(ex, 36 - bob, 1.3, 2.0), FACE)
        c.paint(c.ellipse(ex - 0.5, 35.2 - bob, 0.6, 0.6), GLINT)
    c.paint(c.ellipse(fx + (2.5 if view == "right" else 0), 41 - bob, 1.3, 1.0), FACE)
    return c


# ── 5 넝마귀 ───────────────────────────────────────────

CLOTH = pg.hexes("#2a2626", "#433b39", "#62574f", "#83766a", "#a39582", "#c2b49c")
PATCH = pg.hexes("#2e3050", "#4a4e7a", "#6e74a4", "#9aa0c8")
BUTTON = pg.hexes("#2a1a10", "#6a4a2a", "#b08850", "#e8c888")
HOLLOW = pg.hexes("#100c12", "#1c1620")
THREAD = np.array([214, 200, 168], dtype=np.uint8)


def _button(c, x, y, r, palette):
    ball(c, x, y, r, r, palette, bias=0.1)
    for dx, dy in ((-0.6, -0.6), (0.6, -0.6), (-0.6, 0.6), (0.6, 0.6)):
        c.paint(c.ellipse(x + dx * r * 0.5, y + dy * r * 0.5, 0.5, 0.5), palette[0])


def rag_wraith(view, frame):
    c = canvas(24, 34)
    bob = (0, -1, 1)[frame]
    sway = (0, -2, 2)[frame]
    lean = 3 if view == "right" else 0
    cx, hood_y = 24 + lean, 15 + bob
    top, hem = 20 + bob, 54 + bob
    y = c.yy + 0.5
    t = np.clip((y - top) / (hem - top), 0, 1)
    centre = cx - lean * t * 2 + sway * t
    half = 8.5 + t * 7
    body = (y >= top) & (np.abs(c.xx + 0.5 - centre) <= half)
    # 찢긴 자락: 아래 끝이 들쭉날쭉하고 장마다 나부낀다.
    tatter = hem + 3.5 * np.abs(np.sin((c.xx + 0.5) * 0.85 + frame * 0.9)) + 2.5 * np.abs(np.sin((c.xx + 0.5) * 0.31 + 1.0))
    body &= y <= tatter
    folds = 0.58 + 0.22 * np.sin((c.xx + 0.5 - centre) * 0.75) - (c.xx + 0.5 - centre) / 40 - t * 0.2
    shade(c, body, folds, CLOTH)
    speckle(c, body & (y > hem - 6), CLOTH[1], 0.25, 71, (sway, bob))
    # 소매: 양옆으로 늘어진 천, 끝이 찢겨 있다.
    arm = (0, 1, -1)[frame]
    sleeves = ((1,),) if view == "right" else ((-1,), (1,))
    for (side,) in sleeves:
        sx = cx + side * (6 if view == "right" else 8)
        s = limb(c, (sx, top + 4), (sx + side * 6, top + 17 + side * arm), 3.2, 2.4, CLOTH, bias=-0.05)
        ex, ey = sx + side * 6, top + 17 + side * arm
        c.paint(c.polygon([(ex - 3, ey), (ex + 3, ey), (ex + side * 2, ey + 5)]), CLOTH[2])
    hood = ball(c, cx, hood_y, 11, 10.5, CLOTH, bias=0.05)
    stripes(c, hood & (c.yy < hood_y - 3), 5, CLOTH[2], slope=0.4)
    if view == "up":
        patch = c.rect(cx - 6, top + 12, cx + 1, top + 19)
        shade(c, patch, 0.6, PATCH)
        c.paint(patch & ~c.rect(cx - 5, top + 13, cx, top + 18) & (np.mod(c.xx + c.yy, 2) == 0), THREAD)
        return c
    patch = c.rect(cx - 9 + lean, top + 13, cx - 3 + lean, top + 19)
    shade(c, patch, 0.6, PATCH)
    c.paint(patch & ~c.rect(cx - 8 + lean, top + 14, cx - 4 + lean, top + 18) & (np.mod(c.xx + c.yy, 2) == 0), THREAD)
    if view == "down":
        c.paint(c.ellipse(cx, hood_y + 2, 7.5, 6.5), HOLLOW[0])
        _button(c, cx - 3, hood_y + 1, 2.6, BUTTON)
        _button(c, cx + 3.5, hood_y + 0.5, 2.0, PATCH)
        for k in range(5):
            c.paint(c.ellipse(cx - 3 + k * 1.5, hood_y + 5.5 + (0.6 if k in (1, 3) else 0), 0.5, 0.6), THREAD)
    else:
        c.paint(c.ellipse(cx + 6, hood_y + 2, 4.5, 6), HOLLOW[0])
        _button(c, cx + 6.5, hood_y + 1, 2.2, BUTTON)
        for k in range(3):
            c.paint(c.ellipse(cx + 5 + k * 1.4, hood_y + 5.5, 0.5, 0.6), THREAD)
    return c
