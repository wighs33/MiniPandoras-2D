"""몬스터 11~15레벨: 저주·요정의 빛·기록에 깃든 더스트와 십이장 불·빙결의 더스트(monsters.py의 도감 차례).

 11 뿌리걸음이  저주받은 고목 뿌리의 원념. 네 개의 뿌리 다리로 걷는 엉킨 몸, 나무껍질 탈 얼굴, 보랏빛 저주 핏줄, 가지 뿔
 12 유리나방    판도라의 정원 수정꽃에 맺힌 더스트로 생긴 나방. 수정 날개(무늬 눈, 무지개 반짝임), 은빛 몸, 깃털 더듬이
 13 물어뜯는 서책 고목 서고의 옛 기록에 남은 원념. 가죽 표지가 턱처럼 열리고 책장이 이빨, 책갈피 혀, 금박 눈
 14 불씨깃      불(주작)의 더스트가 불꽃 깃털 다발이 된 것. 흰 새 탈 얼굴, 위로 솟는 불깃 볏, 아래로 흩날리는 꼬리불
 15 서리껍질    빙결(현무)의 더스트가 얼음에 깃든 것. 육각 얼음판 등딱지, 서리 송곳, 수정 다리 넷, 앞 구멍의 푸른 눈
"""
import numpy as np

import pixelgen as pg
from monster_kit import GLINT, ball, blob, canvas, chain, feather, glow_eye, limb, shade, spark, speckle, stripes, tilted

VOID = pg.hexes("#0a080c", "#16121a")

# ── 11 뿌리걸음이 ──────────────────────────────────────

ROOT = pg.hexes("#170f0a", "#2a1c13", "#43301f", "#5f442d", "#7e5c3e")
MASK = pg.hexes("#33261b", "#56412c", "#7d6242", "#a4865c", "#c8ac80", "#e4d0a8")
CURSE = pg.hexes("#24093a", "#55257f", "#9354d0", "#dcaaff")
MOSS = pg.hexes("#24361e", "#34502a", "#4c6c36")


def _root(c, points, r0, r1):
    chain(c, points, r0 + 0.8, r1 + 0.8, ROOT[:1])
    return chain(c, points, r0, r1, ROOT, bias=0.05)


def root_stalker(view, frame):
    c = canvas(30, 30)
    cx = 30
    bob = 1 if frame else 0
    side_view = view == "right"
    # 다리 넷: 무릎이 몸보다 높이 솟는 뿌리. 대각선 두 다리씩 번갈아 든다.
    if side_view:
        legs = [((cx - 8, 30), (cx - 16, 19), (cx - 21, 56), 0), ((cx - 2, 31), (cx - 7, 21), (cx - 7, 57), 1),
                ((cx + 4, 31), (cx + 10, 20), (cx + 12, 57), 0), ((cx + 9, 30), (cx + 18, 19), (cx + 23, 56), 1)]
    else:
        legs = [((cx - 6, 29), (cx - 13, 18), (cx - 16, 50), 1), ((cx + 6, 29), (cx + 13, 18), (cx + 16, 50), 0),
                ((cx - 8, 33), (cx - 19, 24), (cx - 22, 57), 0), ((cx + 8, 33), (cx + 19, 24), (cx + 22, 57), 1)]
    for hip, knee, foot, pair in legs:
        lifted = frame and (pair == frame - 1)
        foot = (foot[0], foot[1] - (3 if lifted else 0))
        hip = (hip[0], hip[1] - bob)
        _root(c, [hip, knee, foot], 2.6, 1.5)
        for toe in (-2, 0, 2):
            limb(c, foot, (foot[0] + toe, foot[1] + 2), 0.9, 0.5, ROOT, bias=-0.1)
    # 몸: 가로로 엉킨 뿌리 다발, 보랏빛 저주가 핏줄처럼 비친다.
    rx = 15 if side_view else 13
    for k in range(5):
        y = 26 + k * 2.2 - bob
        _root(c, [(cx - rx + 2, y + np.sin(k) * 1.5), (cx, y - 1 + k % 2 * 2), (cx + rx - 2, y + np.cos(k) * 1.5)], 2.6, 2.2)
    for k, (x0, x1) in enumerate(((-9, -2), (1, 9), (-5, 4))):
        limb(c, (cx + x0, 27 + k * 2 - bob), (cx + x1, 29 + k - bob), 0.6, 0.5, CURSE[1:])
    # 몸 아래로 늘어진 이끼 가닥
    for k in range(6):
        x = cx - 10 + k * 4 + (frame - 1)
        limb(c, (x, 34 - bob), (x + (frame - 1), 39 + k % 3 * 2 - bob), 0.8, 0.5, MOSS)
    # 가지 뿔
    hx = cx + (13 if side_view else 0)
    hy = 24 - bob
    for side in ((-1,) if side_view else (-1, 1)):
        base = (hx + side * 3, hy - 7)
        tip = (hx + side * 9 - (4 if side_view else 0), hy - 17)
        _root(c, [base, (base[0] + side * 2, base[1] - 5), tip], 1.4, 0.7)
        _root(c, [(base[0] + side * 2, base[1] - 5), (base[0] + side * 7, base[1] - 7)], 0.9, 0.5)
    if view == "up":
        knot = ball(c, hx, hy, 6.5, 6, ROOT, bias=0.1)
        speckle(c, knot, ROOT[4], 0.2, 111, (0, -bob))
        return c
    # 나무껍질 탈: 파낸 눈구멍 속 보랏빛, 쩍 벌어진 입
    mask = ball(c, hx, hy, 5.5 if side_view else 7, 8.5, MASK, bias=0.05)
    stripes(c, mask, 3, MASK[1], slope=0.15)
    if side_view:
        c.paint(c.ellipse(hx + 2.5, hy - 1.5, 1.8, 2.4), VOID[0])
        glow_eye(c, hx + 2.5, hy - 1.2, 1.2, 1.4, CURSE[1:])
        c.paint(c.rect(hx + 1, hy + 4, hx + 6, hy + 5), VOID[0])
    else:
        for ex in (hx - 3, hx + 3):
            c.paint(c.ellipse(ex, hy - 1.5, 2.0, 2.6), VOID[0])
            glow_eye(c, ex, hy - 1.2, 1.3, 1.5, CURSE[1:])
        c.paint(c.ellipse(hx, hy + 4.5, 3, 1.3), VOID[0])
    return c


# ── 12 유리나방 ────────────────────────────────────────

GLASS = pg.hexes("#56699a", "#7a92c2", "#a2bce2", "#c8dcf4", "#e8f4ff")
VEIN = np.array([88, 104, 148], dtype=np.uint8)
SILVER = pg.hexes("#5a566a", "#8a869c", "#b8b4c8", "#e2e0ee", "#ffffff")
COMPOUND = pg.hexes("#18142e", "#36305e")
RAINBOW = pg.hexes("#ffb0d0", "#ffe090", "#a0f0d0", "#b0c0ff")


def _wing(c, cx, cy, rx, ry, angle, root, eyespot):
    mask, u, v = tilted(c, cx, cy, rx, ry, angle)
    shade(c, mask, 0.72 - np.abs(v) * 0.15 + u * 0.08, GLASS, dither=0.18)
    ring = pg.neighbours(~mask) & mask
    c.paint(ring, GLASS[4])
    # 날개맥: 뿌리에서 부채처럼 퍼진다.
    for k in range(4):
        a = angle + (k - 1.5) * 0.35
        tip = (root[0] + np.cos(a) * rx * 1.6, root[1] + np.sin(a) * rx * 1.6)
        vein = limb(c, root, tip, 0.5, 0.5, GLASS[:1]) & mask
        c.paint(vein, VEIN)
    if eyespot:
        ex, ey = cx + np.cos(angle) * rx * 0.35, cy + np.sin(angle) * rx * 0.35
        c.paint(c.ellipse(ex, ey, 2.6, 2.6) & mask, GLASS[1])
        c.paint(c.ellipse(ex, ey, 1.6, 1.6) & mask, RAINBOW[3])
        c.paint(c.ellipse(ex - 0.4, ey - 0.4, 0.6, 0.6), GLINT)
    for k, color in enumerate(RAINBOW):
        gx, gy = cx + np.cos(angle + k) * rx * 0.55, cy + np.sin(angle + k * 1.7) * ry * 0.5
        c.paint(c.ellipse(gx, gy, 0.6, 0.6) & mask, color)
    return mask


def glass_moth(view, frame):
    c = canvas(34, 28)
    cx = 34
    bob = (0, -1, 1)[frame]
    by = 22 + bob
    flap = (0, -0.4, 0.3)[frame]
    for k in range(3):
        spark(c, cx - 6 + k * 6, by + 18 + (k + frame) % 3 * 2, 0.7, GLASS[3], GLINT)
    if view == "right":
        # 옆에서는 두 날개를 등 위로 모아 세운다.
        _wing(c, cx - 3, by - 9, 13, 6, -1.95 + flap, (cx, by - 1), False)
        _wing(c, cx - 1, by - 8, 14, 7, -1.75 + flap, (cx + 1, by - 1), True)
        body = limb(c, (cx + 1, by), (cx - 12, by + 4), 3.6, 1.6, SILVER)
        stripes(c, body, 3, SILVER[1], slope=-3)
        ball(c, cx + 2, by, 4, 4, SILVER, bias=0.1)
        ball(c, cx + 6, by - 1, 3, 2.8, SILVER, bias=0.1)
        c.paint(c.ellipse(cx + 7, by - 1.5, 1.6, 1.8), COMPOUND[1])
        c.paint(c.ellipse(cx + 6.6, by - 2.2, 0.6, 0.6), RAINBOW[2])
        chain(c, [(cx + 6, by - 4), (cx + 9, by - 10), (cx + 14, by - 12)], 0.6, 0.5, SILVER)
        return c
    for side in (-1, 1):
        _wing(c, cx + side * 9, by + 6, 8.5, 5.5, (np.pi if side < 0 else 0) + side * (0.7 + flap * 0.5), (cx + side * 2, by + 2), False)
    for side in (-1, 1):
        _wing(c, cx + side * 13, by - 5, 14, 8.5, (np.pi if side < 0 else 0) - side * (0.45 - flap), (cx + side * 2, by - 1), True)
    body = limb(c, (cx, by + 2), (cx, by + 14), 3.2, 1.4, SILVER)
    stripes(c, body & (np.mod(c.yy - bob, 3) == 0), 1, SILVER[1])
    thorax = blob(c, cx, by, 4.4, SILVER, wobble=0.5, lobes=7, bias=0.1)
    ball(c, cx, by - 5, 3.2, 3, SILVER, bias=0.12)
    for side in (-1, 1):
        stem = [(cx + side * 1.4, by - 7.5), (cx + side * 3.5, by - 12), (cx + side * 7, by - 15)]
        chain(c, stem, 0.6, 0.5, SILVER)
        for k in range(3):
            x, y = stem[1][0] + side * k * 1.2, stem[1][1] - k * 1.1
            limb(c, (x, y), (x + side * 1.5, y - 1.5), 0.4, 0.4, SILVER[3:])
    if view == "down":
        for side in (-1, 1):
            c.paint(c.ellipse(cx + side * 2.3, by - 5.5, 1.8, 2.0), COMPOUND[1])
            c.paint(c.ellipse(cx + side * 2.3 - 0.5, by - 6.2, 0.6, 0.6), RAINBOW[0 if side < 0 else 2])
    return c


# ── 13 물어뜯는 서책 ───────────────────────────────────

LEATHER = pg.hexes("#260a0e", "#461216", "#6a1e22", "#90302e", "#b24a42")
PAGES = pg.hexes("#867656", "#b4a27e", "#d8c8a0", "#f0e4c6")
GILT = pg.hexes("#6a4a12", "#b08a30", "#e8c868", "#fff0b0")
RIBBON = pg.hexes("#5a0a12", "#9a1626", "#d0383e")
MOUTH = np.array([26, 8, 10], dtype=np.uint8)


def _teeth(c, x0, x1, y, down):
    for x in np.arange(x0 + 1, x1 - 2, 4):
        tip = y + 3 if down else y - 3
        c.paint(c.polygon([(x, y), (x + 3, y), (x + 1.5, tip)]), PAGES[3])


def biting_tome(view, frame):
    c = canvas(30, 26)
    cx = 30
    bob = (0, -1, 1)[frame]
    gape = (5, 9, 2)[frame]
    if view == "up":
        # 뒤에서 보면 닫힌 뒤표지와 책등
        cover = c.rect(8, 10 + bob, 52, 34 + bob)
        shade(c, cover, 0.62 - (c.xx - 8) / 140 - (c.yy - 10) / 120, LEATHER)
        c.paint(cover & ~c.rect(10, 12 + bob, 50, 32 + bob), GILT[1])
        c.paint(c.rect(14, 20 + bob, 46, 21 + bob) | c.rect(18, 23 + bob, 42, 24 + bob), GILT[2])
        for x, y in ((8, 10), (49, 10), (8, 31), (49, 31)):
            c.paint(c.rect(x, y + bob, x + 3, y + 3 + bob), GILT[2])
        return c
    if view == "right":
        # 옆모습: 책등에서 두 표지가 턱처럼 벌어진다.
        spine = limb(c, (12, 12 + bob), (12, 30 + bob), 3, 3, LEATHER, bias=0.1)
        top_end, bottom_end = 10 + bob - gape * 0.6, 32 + bob + gape * 0.6
        upper = c.polygon([(12, 12 + bob), (54, top_end), (54, top_end + 4), (12, 16 + bob)])
        lower = c.polygon([(12, 26 + bob), (54, bottom_end - 4), (54, bottom_end), (12, 30 + bob)])
        c.paint(c.polygon([(13, 16 + bob), (52, top_end + 4), (52, bottom_end - 4), (13, 26 + bob)]), MOUTH)
        for poly in (c.polygon([(14, 16 + bob), (52, top_end + 4), (52, top_end + 7), (14, 18 + bob)]),
                     c.polygon([(14, 24 + bob), (52, bottom_end - 7), (52, bottom_end - 4), (14, 26 + bob)])):
            shade(c, poly, 0.75, PAGES)
        shade(c, upper, 0.68, LEATHER)
        shade(c, lower, 0.45, LEATHER)
        for k in range(5):
            x = 22 + k * 7
            yt = 16 + bob + (top_end + 7 - 16 - bob) * (x - 14) / 38
            c.paint(c.polygon([(x, yt), (x + 3, yt), (x + 1.5, yt + 3)]), PAGES[3])
            yb = 26 + bob + (bottom_end - 7 - 26 - bob) * (x - 14) / 38
            c.paint(c.polygon([(x, yb), (x + 3, yb), (x + 1.5, yb - 3)]), PAGES[3])
        chain(c, [(30, 21 + bob), (40, 24 + bob), (44, 34 + bob + frame), (42, 42 + bob)], 1.2, 1.0, RIBBON)
        c.paint(c.ellipse(44, top_end + 1.5, 3, 1.6) & upper, GILT[2])
        glow_eye(c, 44, top_end + 1.5, 1.6, 1.0, GILT[1:])
        return c
    # 앞모습: 위아래 표지가 턱이 되어 열리고, 책장 끝이 이빨처럼 맞물린다.
    top = 8 + bob
    upper = c.rect(6, top, 54, top + 6) | ball(c, 6, top + 3, 1, 3, LEATHER) | ball(c, 54, top + 3, 1, 3, LEATHER)
    shade(c, upper, 0.75 - (c.yy - top) * 0.06 - (c.xx - 6) / 200, LEATHER)
    page_top = c.rect(8, top + 6, 52, top + 10)
    shade(c, page_top, 0.75, PAGES)
    stripes(c, page_top, 2, PAGES[1], slope=0)
    mouth_top, mouth_bottom = top + 10, top + 14 + gape
    c.paint(c.rect(9, mouth_top, 51, mouth_bottom), MOUTH)
    _teeth(c, 8, 52, mouth_top, True)
    page_bottom = c.rect(8, mouth_bottom, 52, mouth_bottom + 4)
    shade(c, page_bottom, 0.7, PAGES)
    stripes(c, page_bottom, 2, PAGES[1], slope=0)
    _teeth(c, 8, 52, mouth_bottom, False)
    lower = c.rect(6, mouth_bottom + 4, 54, mouth_bottom + 9)
    shade(c, lower, 0.5 - (c.xx - 6) / 200, LEATHER)
    for x in (6, 51):
        c.paint(c.polygon([(x, top), (x + 4, top), (x + (0 if x < 30 else 4), top + 4)]), GILT[2])
        c.paint(c.polygon([(x, mouth_bottom + 9), (x + 4, mouth_bottom + 9), (x + (0 if x < 30 else 4), mouth_bottom + 5)]), GILT[2])
    # 책갈피 혀가 입 밖으로 늘어진다.
    chain(c, [(cx + 2, mouth_top + 2), (cx + 4, mouth_bottom + 2), (cx + 3 + frame, mouth_bottom + 12), (cx + 5, mouth_bottom + 17)], 1.4, 1.1, RIBBON)
    # 금박 눈 문양이 진짜 눈이다.
    c.paint(c.ellipse(cx, top + 3, 5, 2.6), GILT[1])
    glow_eye(c, cx, top + 3, 3.6, 1.9, [GILT[2], GILT[3], np.array([255, 255, 230], dtype=np.uint8)])
    c.paint(c.ellipse(cx, top + 3, 0.7, 1.8), MOUTH)
    # 양옆에 낱장이 날개처럼 팔락인다.
    flap = (0, -3, 2)[frame]
    for side in (-1, 1):
        x = cx + side * 25
        c.paint(c.polygon([(x, top + 10), (x + side * 5, top + 4 + flap), (x + side * 6, top + 12 + flap), (x + side * 1, top + 15)]), PAGES[2])
    return c


# ── 14 불씨깃 ──────────────────────────────────────────

FIRE = pg.hexes("#5a0a06", "#a01a08", "#d8400c", "#f47a1c", "#ffb840", "#fff0a0")
WHITE_MASK = pg.hexes("#8a7a70", "#c0b4a8", "#e6ddd2", "#fffaf2")
MASK_PAINT = np.array([184, 32, 42], dtype=np.uint8)
EMBER_EYE = pg.hexes("#a01a08", "#ffb840", "#fff6c0")


def cinder_plume(view, frame):
    c = canvas(28, 34)
    cx = 28 + (2 if view == "right" else 0)
    bob = (0, -1, 1)[frame]
    flick = (0, 1.5, -1.5)[frame]
    back = -4 if view == "right" else 0
    # 아래로 흩날리는 꼬리불(뒤로 쓸린다)
    for k in range(-2, 3):
        feather(c, (cx + k * 1.5, 38 + bob), (cx + k * 5 + back * 1.5 + flick * (1 if k % 2 else -1), 62 - abs(k) * 3 + bob), 5.5, FIRE, bias=-0.05)
    # 양옆 불깃 날개
    for side in ((1,) if view == "right" else (-1, 1)):
        feather(c, (cx + side * 4, 31 + bob), (cx + side * 21 + back, 22 + bob - flick), 6, FIRE)
        feather(c, (cx + side * 4, 34 + bob), (cx + side * 19 + back, 38 + bob + flick), 5, FIRE, bias=-0.05)
    # 위로 솟는 불깃 볏
    for k in range(-2, 3):
        feather(c, (cx + k * 1.2, 27 + bob), (cx + k * 5.5 + back * 1.5, 2 + abs(k) * 3.5 + bob - flick * (1 if k % 2 else -1)), 6.5, FIRE, bias=0.05)
    blob(c, cx, 31 + bob, 8.5, FIRE[2:], wobble=1.0, lobes=7, phase=frame, bias=0.2)
    for k in range(4):
        spark(c, cx - 12 + k * 8 + flick, 8 + (k * 7 + frame * 5) % 18 + bob, 0.6, FIRE[4], FIRE[5])
    if view == "up":
        return c
    # 흰 새 탈: 붉은 칠, 아래로 굽은 부리, 불이 비치는 눈구멍
    face_x = cx + (3 if view == "right" else 0)
    mask = ball(c, face_x, 29 + bob, 5.5 if view == "right" else 6.5, 7.5, WHITE_MASK, bias=0.1)
    c.paint(mask & (c.yy < 24 + bob) & (np.abs(c.xx + 0.5 - face_x) < 1.2), MASK_PAINT)
    if view == "right":
        c.paint(c.polygon([(face_x + 3, 30 + bob), (face_x + 10, 33 + bob), (face_x + 3, 34 + bob)]), WHITE_MASK[2])
        c.paint(c.ellipse(face_x + 2, 27.5 + bob, 1.6, 1.9), VOID[0])
        glow_eye(c, face_x + 2, 27.5 + bob, 1.0, 1.3, EMBER_EYE)
    else:
        c.paint(c.polygon([(face_x - 2.5, 31 + bob), (face_x + 2.5, 31 + bob), (face_x, 37 + bob)]), WHITE_MASK[2])
        c.paint(c.polygon([(face_x, 31 + bob), (face_x + 2.5, 31 + bob), (face_x, 37 + bob)]), WHITE_MASK[1])
        for ex in (face_x - 3, face_x + 3):
            c.paint(c.ellipse(ex, 27.5 + bob, 1.7, 1.9), VOID[0])
            glow_eye(c, ex, 27.5 + bob, 1.1, 1.3, EMBER_EYE)
        for side in (-1, 1):
            c.paint(c.ellipse(face_x + side * 4.5, 31 + bob, 1.2, 0.6), MASK_PAINT)
    return c


# ── 15 서리껍질 ────────────────────────────────────────

ICE = pg.hexes("#1c3656", "#2c5680", "#4880aa", "#76accc", "#a8d4e8", "#def4ff")
CRYSTAL = pg.hexes("#24425e", "#3e6e94", "#6ea6cc", "#b4e0f4", "#ecfcff")
FROST_EYE = pg.hexes("#0a2a6a", "#2a6ae0", "#8ac8ff", "#f0ffff")


def _shard(c, a, b, width):
    """각진 수정 조각: a에서 b로 가는 길쭉한 마름모, 왼쪽 면은 밝고 오른쪽 면은 어둡다."""
    (x0, y0), (x1, y1) = a, b
    mx, my = (x0 + x1) / 2, (y0 + y1) / 2
    dx, dy = x1 - x0, y1 - y0
    n = max(np.hypot(dx, dy), 1e-6)
    px, py = -dy / n * width, dx / n * width
    left = c.polygon([(x0, y0), (mx + px, my + py), (x1, y1)])
    right = c.polygon([(x0, y0), (mx - px, my - py), (x1, y1)])
    lit = left if px < 0 else right
    c.paint(left | right, CRYSTAL[1])
    c.paint(lit, CRYSTAL[3])
    c.paint(c.polygon([(x0, y0), ((x0 + mx) / 2 + (px if lit is left else -px) * 0.4, (y0 + my) / 2), (mx, my)]) & lit, CRYSTAL[4])
    return left | right


def _hex_plates(c, dome, cx, cy, size, bias_seed):
    """등딱지를 육각 얼음판으로 나눈다: 가장 가까운 판 가운데가 바뀌는 곳이 판 경계다."""
    centres = []
    for row in range(-3, 4):
        for col in range(-5, 6):
            centres.append((cx + col * size * 1.73 + (row % 2) * size * 0.87, cy + row * size * 1.5))
    centres = np.array(centres)
    d = (c.xx[..., None] + 0.5 - centres[:, 0]) ** 2 + ((c.yy[..., None] + 0.5 - centres[:, 1]) * 1.25) ** 2
    plate = np.argmin(d, axis=-1)
    edge = (plate != np.roll(plate, 1, axis=0)) | (plate != np.roll(plate, 1, axis=1))
    rng = np.random.default_rng(bias_seed)
    return plate, edge & dome, rng.uniform(-0.08, 0.08, len(centres))


def frost_shell(view, frame):
    c = canvas(36, 28)
    cx = 36 + (-2 if view == "right" else 0)
    bob = 1 if frame else 0
    side_view = view == "right"
    # 수정 다리 넷: 대각선 둘씩 번갈아 든다.
    if side_view:
        legs = [((cx - 14, 40), (cx - 22, 54), 0), ((cx - 4, 42), (cx - 8, 55), 1), ((cx + 6, 42), (cx + 9, 55), 0), ((cx + 15, 40), (cx + 23, 54), 1)]
    else:
        legs = [((cx - 11, 38), (cx - 19, 51), 1), ((cx + 11, 38), (cx + 19, 51), 0), ((cx - 15, 42), (cx - 24, 55), 0), ((cx + 15, 42), (cx + 24, 55), 1)]
    for hip, foot, pair in legs:
        up = 3 if frame and pair == frame - 1 else 0
        knee = ((hip[0] + foot[0]) / 2 + (foot[0] - hip[0]) * 0.2, hip[1] + 4 - bob)
        _shard(c, (hip[0], hip[1] - bob), knee, 2.4)
        _shard(c, knee, (foot[0], foot[1] - up), 2.2)
    # 등딱지
    cy = 34 - bob
    dome_mask = (((c.xx + 0.5 - cx) / 25) ** 2 + ((c.yy + 0.5 - cy) / 15) ** 2 <= 1) & (c.yy < cy + 8)
    nx = (c.xx + 0.5 - cx) / 25
    ny = (c.yy + 0.5 - cy) / 15
    nz = np.sqrt(np.clip(1 - nx ** 2 - ny ** 2, 0, 1))
    plate, edges, plate_bias = _hex_plates(c, dome_mask, cx, cy - 2, 4.6, 151)
    light = np.clip((nx * pg.LIGHT[0] + ny * pg.LIGHT[1] + nz * pg.LIGHT[2]) * 0.55 + 0.45 + plate_bias[plate], 0, 0.999)
    shade(c, dome_mask, light, ICE)
    c.paint(edges, ICE[5])
    c.paint(dome_mask & (c.yy >= cy + 6), ICE[1])
    # 서리 송곳
    for k, (dx, h) in enumerate(((-14, 7), (-7, 11), (0, 13), (7, 10), (14, 7))):
        x = cx + dx * (0.8 if side_view else 1)
        base_y = cy - 15 * np.sqrt(max(0.0, 1 - (dx / 25) ** 2)) + 3
        _shard(c, (x, base_y + 2), (x + (k - 2) * 0.8, base_y - h), 2.2)
    if view == "up":
        return c
    if side_view:
        c.paint(c.ellipse(cx + 21, cy + 4, 4.5, 4) & dome_mask, VOID[0])
        glow_eye(c, cx + 21.5, cy + 3, 1.6, 1.3, FROST_EYE[1:])
        for k in range(2):
            ball(c, cx + 27 + k * 3 + frame, cy + 3 - k, 1.6, 1.4, ICE[3:], bias=0.2)
    else:
        mouth = c.ellipse(cx, cy + 7, 10, 5) & (c.yy < cy + 8)
        c.paint(mouth, VOID[0])
        for ex in (cx - 4, cx + 4):
            glow_eye(c, ex, cy + 5, 1.7, 1.3, FROST_EYE[1:])
        if frame:
            for k in range(3):
                ball(c, cx - 3 + k * 3, cy + 11 + k % 2, 1.6, 1.3, ICE[3:], bias=0.25)
    return c
