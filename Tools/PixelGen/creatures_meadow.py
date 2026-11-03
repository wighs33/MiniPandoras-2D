"""생물 도감 · 힐리아 초원(1~3레벨, 셋 다 귀여운 쪽).

  1 솜꼬리   두 귀가 민들레 꽃이고, 엉덩이에서 솟은 꽃대 끝이 몸보다 큰 씨앗 공인 들쥐꼴 짐승. 크림색 배·주둥이, 볼 솜털,
             가슴에 모은 앞발. 긴 뒷발로 통통 뛰고, 뛸 때마다 씨앗 공에서 씨앗이 떨어져 날린다
  2 잎마디   말린 잎 마디가 이어진 애벌레. 머리에 장밋빛 꽃봉오리, 덩굴손 더듬이, 마디마다 짧은 발
  3 햇갈기   해바라기 꽃잎이 갈기인 새끼 짐승. 뒤에서 보면 갈기 뒤쪽이 초록 꽃받침이고, 꼬리 끝은 해바라기 씨 다발
"""
import numpy as np

import pixelgen as pg
from creature_kit import INK, quadruped, small_face
from monster_kit import HOP, ball, canvas, chain, feather, limb, shade, spark

# ── 1 솜꼬리 ───────────────────────────────────────────

FUR = pg.hexes("#5a3a22", "#7e5430", "#a27444", "#c2955c", "#dcb67e")
BELLY = pg.hexes("#c4a47a", "#dcc49a", "#ecdcb8", "#f8eed6")
PUFF = pg.hexes("#9a9a92", "#b8b8b0", "#d2d2ca", "#e6e6de", "#f4f4ee", "#ffffff")
RAY = pg.hexes("#b07010", "#d49418", "#eab42c", "#f6d04e", "#ffe68a")
HEART = pg.hexes("#a85a06", "#c87810", "#e0961c")
STEM = pg.hexes("#3c6a2e", "#58883e", "#7aa656", "#9ec274")
SEED = pg.hexes("#6e5030", "#94724a")
NOSE = np.array([132, 74, 60], dtype=np.uint8)
# 떨어져 날아가는 씨앗 자리(씨앗 공 가운데 기준, 날아가는 쪽이 +x)
DRIFT = ((6.0, -12.0), (11.0, -6.0), (2.0, -16.0))


def _arc(p0, p1, p2, n=8):
    """세 점으로 정한 부드러운 곡선(2차 베지에) 위의 점들."""
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t ** 2 * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t ** 2 * p2[1]) for t in np.linspace(0, 1, n)]


def _flying_seed(c, x, y, tilt):
    """떨어진 씨앗 하나: 위에 우산처럼 펼친 갓털, 가는 부리, 밑에 갈색 씨."""
    for k in range(5):
        a = -np.pi / 2 + (k - 2) * 0.55
        limb(c, (x, y), (x + np.cos(a) * 1.7, y + np.sin(a) * 1.7), 0.28, 0.22, PUFF[3:])
    limb(c, (x, y), (x + tilt * 0.5, y + 2.4), 0.22, 0.22, PUFF[1:3])
    limb(c, (x + tilt * 0.5, y + 2.4), (x + tilt * 0.7, y + 3.6), 0.4, 0.3, SEED)


def _seed_clock(c, x, y, r, flying, away, drift):
    """
    민들레 씨앗 공(꼬리 끝). 둥근 솜 덩이 가장자리를 작은 솜뭉치가 겹겹이 둘러 보송하고, 안쪽에는 가운데로 모이는 씨앗 대가 희미하게 비친다.
    겉으로 얼굴을 내민 갓털은 하얀 별꼴로 찍는다. 씨앗 flying개가 away 쪽(+1 오른쪽, −1 왼쪽)으로 떨어져 날아간다.
    """
    ball(c, x, y, r * 0.9, r * 0.9, PUFF, bias=0.08)
    for k in range(28):
        a = k * 2 * np.pi / 28
        reach = r * (0.84 + 0.13 * ((k * 5) % 7) / 6)
        facing = -(np.cos(a) + np.sin(a)) / np.sqrt(2)  # 왼쪽 위(빛 쪽)를 향할수록 1
        ball(c, x + np.cos(a) * reach, y + np.sin(a) * reach, 1.6, 1.6, PUFF, bias=0.05 + facing * 0.18)
    inner = c.ellipse(x, y, r * 0.78, r * 0.78)
    for k in range(16):
        a = k * 2 * np.pi / 16 + 0.2
        c.paint(limb(c, (x, y), (x + np.cos(a) * r * 0.75, y + np.sin(a) * r * 0.75), 0.22, 0.22, PUFF[2:3]) & inner
                & (c.ix % 2 == c.iy % 2), PUFF[2])
    c.paint(c.ellipse(x + 0.3, y + 0.3, 1.1, 1.1), PUFF[1])
    for k in range(10):
        a = k * 2.4 + 0.6
        d = r * (0.25 + 0.5 * ((k * 7) % 10) / 10)
        spark(c, x + np.cos(a) * d - 1.0, y + np.sin(a) * d - 1.0, 0.8, PUFF[4], PUFF[5])
    for k in range(flying):
        dx, dy = DRIFT[k]
        _flying_seed(c, x + (dx + drift * 0.8) * away, y + dy - drift * 1.2, -0.4 * away)


def _dandelion(c, x, y, frame, lean, size=1.0, back=False):
    """
    민들레 꽃 귀. 초록 꽃받침 컵 위로 노란 혀꽃이 부채처럼 겹겹이 핀다(바깥은 길고 진하게, 안쪽은 짧고 밝게, 밑동은 주황).
    뒤에서 보면(back) 꽃받침이 꽃 아래쪽을 덮고 총포 조각이 아래로 젖혀진다. 눌릴 때 납작해지고 뛸 때 솟는다.
    앞에서는 총포 조각을 그리지 않는다(이마 위의 눈썹처럼 보인다).
    """
    sx, sy, _ = HOP[frame]
    up = -np.pi / 2 + lean
    length = 5.6 * size * sy ** 2
    spread = 1.3 * sx
    for k in range(13):
        a = up + (k / 6 - 1) * spread
        reach = length * (1.0 if k % 2 == 0 else 0.86)
        feather(c, (x, y), (x + np.cos(a) * reach, y + np.sin(a) * reach), 1.6 * size, RAY[:4], bias=-0.1)
    for k in range(9):
        a = up + (k / 4 - 1) * spread * 0.7
        feather(c, (x, y - 0.3), (x + np.cos(a) * length * 0.6, y + np.sin(a) * length * 0.6), 1.5 * size, RAY[1:], bias=0.1)
    ball(c, x + np.cos(up) * 0.6, y + np.sin(up) * 0.6, 1.5 * size, 1.0 * size, HEART)
    if back:
        ball(c, x, y + 0.3, 2.4 * size, 1.8 * size, STEM)
        for side in (-1, 1):
            limb(c, (x + side * 1.4, y + 1.0), (x + side * 2.6, y + 2.4), 0.4, 0.3, STEM[:3])
    else:
        ball(c, x, y + 0.9, 1.5 * size, 1.0 * size, STEM[:3])


def _nose(c, x, y):
    c.paint(c.ellipse(x, y, 0.8, 0.5), NOSE)


def fluff_tail(view, frame):
    c = canvas(28, 28, fine=True)
    c.outline_tint = 0.6
    sx, sy, lift = HOP[frame]
    cx, ground = 28, 54
    body_y = ground - 1.2 - lift - 6.2 * sy
    hy = body_y - 7.2 * sy
    head_rx, head_ry = 7.4 * sx, 6.6 * sy
    # 꼬리 씨앗 공은 몸을 늦게 따라온다: 내려앉을 때 한 번 더 내려가고, 뛸 때 덜 뜬다.
    lag = (0.0, 1.5, 2.5)[frame]
    flying = 3 if frame == 2 else 2

    if view == "right":
        # 3/4 얼굴(small_face)은 먼 눈·코·입이 주둥이에 몰려 지저분해서, 눈 하나의 옆얼굴로 그린다.
        hx = cx + 2.8
        puff = (cx - 11, hy - 6 + lag)
        stem = _arc((cx - 6, body_y + 1), (cx - 14, body_y - 1), puff)
        chain(c, stem, 0.9, 0.8, STEM)
        _seed_clock(c, *puff, 10, flying, -1, frame)
        _dandelion(c, hx - 3.6, hy - head_ry * 0.7, frame, -0.4, size=0.85)
        ball(c, cx - 0.5, ground - 1.6 - lift, 3.4 * sx, 1.2, FUR[:3])
        body = ball(c, cx, body_y, 8.0 * sx, 6.4 * sy, FUR)
        ball(c, cx + 3, body_y + 1.6, 4.4 * sx, 4.0 * sy, BELLY, clip=body)
        shade(c, body & ~c.ellipse(cx + 1.0, body_y + 2.0, 8.0 * sx, 6.4 * sy), 0.3, FUR[:3])
        ball(c, cx + 1.8, ground - 1.3 - lift, 4.2 * sx, 1.4, FUR[1:], bias=-0.05)
        for dy, reach in ((2.0, 9.0), (3.8, 8.0)):
            feather(c, (hx - 4, hy + dy), (hx - reach, hy + dy + 1.8), 2.2, FUR[1:4])
        ball(c, hx, hy, 7.0 * sx, head_ry, FUR)
        ball(c, hx + 5.0, hy + 2.4, 3.0 * sx, 2.0 * sy, BELLY, bias=0.05)
        ball(c, hx + 3.6, body_y - 0.2, 1.7, 1.3, FUR[1:], bias=0.05)
        _dandelion(c, hx + 0.2, hy - head_ry * 0.85, frame, 0.15)
        c.paint(c.ellipse(hx + 3.0, hy + 0.1, 0.8, 1.25), INK)
        _nose(c, hx + 7.4 * sx, hy + 1.5)
        mouth = c.ellipse(hx + 6.2 * sx, hy + 3.0, 0.8, 0.65) & ~c.ellipse(hx + 6.2 * sx, hy + 2.5, 0.8, 0.65) & (c.yy + 0.5 >= hy + 3.0)
        c.paint(mouth, INK)
        return c

    hx = cx
    back = view == "up"
    if back:
        # 뒤에서는 씨앗 공이 몸보다 카메라 쪽이라 맨 나중에 그리고, 앞모습보다 낮게 보인다.
        puff = (cx - 13, hy + 1 + lag)
    else:
        puff = (cx + 12, hy - 7 + lag)
        stem = _arc((cx + 4, body_y), (cx + 13, body_y - 1), puff)
        chain(c, stem, 0.9, 0.8, STEM)
        _seed_clock(c, *puff, 10, flying, 1, frame)
    for side in (-1, 1):
        foot = ball(c, cx + side * 4.2 * sx, ground - 1.3 - lift, 2.8 * sx, 1.5, FUR[1:] if not back else FUR[:3], bias=-0.05)
        if not back:
            for toe in (-0.8, 0.6):
                c.paint(foot & c.rect(cx + side * 4.2 * sx + toe, ground - 1.3 - lift, cx + side * 4.2 * sx + toe + 0.5, ground), FUR[1])
    body = ball(c, cx, body_y, 8.0 * sx, 6.4 * sy, FUR)
    if not back:
        ball(c, cx, body_y + 1.2, 5.2 * sx, 4.6 * sy, BELLY, clip=body)
    for side in (-1, 1):
        for dy, reach in ((1.0, 2.8), (2.9, 2.2)):
            root = (hx + side * (head_rx - 2.5), hy + dy)
            feather(c, root, (hx + side * (head_rx + reach), hy + dy + 1.7), 2.2, FUR[1:4])
    ball(c, hx, hy, head_rx, head_ry, FUR)
    ear_y = hy - head_ry * 0.78
    if back:
        for side in (-1, 1):
            _dandelion(c, hx + side * 5.0, ear_y, frame, side * 0.5, size=0.9, back=True)
        stem = _arc((cx - 1, body_y + 3), (cx - 10, body_y + 3), puff)
        chain(c, stem, 0.9, 0.8, STEM)
        ball(c, cx - 0.6, body_y + 3.2, 2.4, 1.9, BELLY, bias=0.05)
        _seed_clock(c, *puff, 10, flying, -1, frame)
        return c
    for side in (-1, 1):
        ball(c, cx + side * 2.3, body_y - 0.9, 1.7, 1.3, FUR[1:], bias=0.05)
        _dandelion(c, hx + side * 5.0, ear_y, frame, side * 0.5, size=0.9)
    ball(c, hx, hy + 2.6, 3.0 * sx, 1.9 * sy, BELLY, bias=0.05)
    small_face(c, hx, hy + 0.2, view, gap=3.1, eye=(0.8, 1.25), mouth="w")
    _nose(c, hx, hy + 0.2 + 1.25 + 0.8)
    return c


# ── 2 잎마디 ───────────────────────────────────────────

LEAF_ROLL = pg.hexes("#1e3e1e", "#2c5a2a", "#40783a", "#5a944a", "#7aae5c")
BUD = pg.hexes("#6a2a44", "#94405e", "#b85c7a", "#d4829a")
LEGS = pg.hexes("#263e1c", "#3e5a2a")


def _roll(c, x, y, r):
    """말린 잎 마디 하나: 둥근 마디에 잎맥이 빙 둘러 감기고, 마디 끝에 잎 가장자리가 겹친다."""
    seg = ball(c, x, y, r, r * 0.9, LEAF_ROLL)
    vein = c.ellipse(x - r * 0.15, y, r * 0.75, r * 0.66) & ~c.ellipse(x - r * 0.05, y - 0.5, r * 0.75, r * 0.66)
    c.paint(seg & vein, LEAF_ROLL[1])
    c.paint(seg & (np.abs(c.xx + 0.5 - (x + r * 0.55)) < 0.5), LEAF_ROLL[1])
    return seg


def leaflet(view, frame):
    c = canvas(28, 22, fine=True)
    wave = frame * 1.3
    if view == "right":
        segs = [(10 + k * 8, 34 - 2.0 * np.sin(k * 1.4 + wave), 5.4 + k * 0.35) for k in range(4)]
        for x, y, r in segs:
            for leg in (-1.5, 1.5):
                limb(c, (x + leg, y + r * 0.6), (x + leg + (frame - 1) * 0.8, 41), 0.8, 0.7, LEGS)
            _roll(c, x, y, r)
        hx, hy = 44, 29 - 1.5 * np.sin(4 * 1.4 + wave)
    else:
        down = view == "down"
        segs = [(28 + 1.5 * np.sin(k * 1.4 + wave), 30 - k * 4.2 if down else 21 + k * 3.6, 6.2 - k * 0.45 if down else 4.8 + k * 0.45) for k in range(4)]
        for k in (range(3, -1, -1) if down else range(4)):
            x, y, r = segs[k]
            for leg in (-1, 1):
                limb(c, (x + leg * r * 0.7, y + r * 0.5), (x + leg * r * 0.9, y + r * 0.9 + 1), 0.8, 0.7, LEGS)
            _roll(c, x, y, r)
        hx, hy = (28, 31) if down else (28, 15)
    head_r = 6.2 if view != "up" else 5
    ball(c, hx, hy, head_r, head_r * 0.92, LEAF_ROLL, bias=0.05)
    bud_y = hy - head_r * 0.85
    for side in (-1, 1):
        chain(c, [(hx + side * 2.2, bud_y + 1), (hx + side * 4.5, bud_y - 3.5), (hx + side * 6.5, bud_y - 4), (hx + side * 6.9, bud_y - 2.4)], 0.5, 0.4, LEAF_ROLL[2:])
        ball(c, hx + side * 2, bud_y, 2.2, 3.0, BUD, bias=-0.05)
    ball(c, hx, bud_y - 0.8, 2.6, 3.6, BUD, bias=0.05)
    c.paint(c.polygon([(hx - 1.4, bud_y - 3.2), (hx + 1.4, bud_y - 3.2), (hx + 0.3, bud_y - 6.2)]), BUD[2])
    small_face(c, hx + (1.5 if view == "right" else 0), hy + 1, view, gap=2.6, eye=(0.8, 1.2), mouth="w")
    return c


# ── 3 햇갈기 ───────────────────────────────────────────

CUB = pg.hexes("#5e3c1c", "#82562a", "#a6743a", "#c4924e", "#dab070")
PETAL = pg.hexes("#8a5a0c", "#b47e12", "#d4a224", "#e8c044", "#f4d870")
DISC = pg.hexes("#2e1c0e", "#4a3018", "#664422")
SEPAL = pg.hexes("#1e4420", "#2e6230", "#468440")
MUZZLE = pg.hexes("#c4a272", "#dcc090")


def _mane(c, x, y, r, back, frame):
    """해바라기 갈기: 머리 둘레로 꽃잎 열여섯이 퍼지고, 뒤에서 보면 꽃받침이 초록으로 보인다."""
    for k in range(16):
        a = k * 2 * np.pi / 16 + frame * 0.04
        feather(c, (x + np.cos(a) * r * 0.55, y + np.sin(a) * r * 0.55), (x + np.cos(a) * (r + 5.5), y + np.sin(a) * (r + 5.5)), 3.8, SEPAL if back else PETAL)
    if back:
        ball(c, x, y, r * 0.8, r * 0.8, SEPAL, bias=-0.05)


def sun_cub(view, frame):
    c = canvas(28, 26, fine=True)
    cx = 28 + (-2 if view == "right" else 0)

    def tail(c, x, y):
        tip = (x - 7, y - 6 + (frame - 1)) if view == "right" else (x + 6, y + 5 + (frame - 1))
        chain(c, [(x, y), ((x + tip[0]) / 2, (y + tip[1]) / 2 - 1), tip], 1.0, 0.8, CUB)
        ball(c, tip[0], tip[1] - 0.5, 2.4, 2.4, DISC, bias=0.1)

    def head(c, x, y):
        hx, hy = x + (2 if view == "right" else 0), y - 1
        _mane(c, hx, hy, 7.5, view == "up", frame)
        if view == "up":
            return
        ball(c, hx, hy, 7.2, 6.8, CUB, bias=0.05)
        ring = c.ellipse(hx, hy, 7.9, 7.5) & ~c.ellipse(hx, hy, 7.2, 6.8)
        c.paint(ring & (np.mod(c.ix + c.iy, 2) == 0), DISC[1])
        for side in (-1, 1):
            ex = hx + side * 5.2 + (1 if view == "right" else 0)
            ball(c, ex, hy - 5.8, 1.9, 1.7, CUB, bias=0.1)
            c.paint(c.ellipse(ex, hy - 5.6, 0.9, 0.8), DISC[2])
        mx = hx + (2.4 if view == "right" else 0)
        ball(c, mx, hy + 2.8, 3.4, 2.2, MUZZLE, bias=0.1)
        c.paint(c.ellipse(mx, hy + 1.6, 1.0, 0.7), DISC[0])
        small_face(c, hx + (1.2 if view == "right" else 0), hy - 1.2, view, gap=3.2, eye=(0.85, 1.25), mouth="w")

    quadruped(c, view, frame, cx, 50, (10 if view == "right" else 8.5, 6.5, 13), (2.1, 0.55), CUB, head, tail)
    return c
