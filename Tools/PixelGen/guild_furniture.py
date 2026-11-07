"""뉴웨이브 아지트 안의 가구와 바닥·벽 장식(길드 내부 그래픽 개선).

가구(세우는 것)는 (RGBA 그림, 발밑 기준점)을, 장식(바닥이나 벽면 위에 까는 것)은 RGBA 그림만 돌려준다.
낡은 아지트(지금)는 천을 덮거나 부서진 가구, 먼지·거미줄·판자로 막은 창이고, 전성기(6장 회상)는 손때 묻은 가구와 길드 장식이다.
길드 장식은 바깥 집과 같은 청록 천에 놋쇠 톱니다.
"""
import numpy as np

import pixelgen as pg
from houses import BRASS, CLOTH, IRON, LEAF, PAPER, WARM, WOOD, Canvas, gear, outline
from interior import BOOKS

SHEET = pg.hexes("#6e6a62", "#8e897e", "#aaa598", "#c4bfb0", "#d8d3c4")
DUST = pg.hexes("#4a4038", "#5e554b", "#746a5e")
WEB = pg.hexes("#9a968c", "#c8c4b8")
TEAL = CLOTH["teal"]
RUG = pg.hexes("#0f2c2c", "#164242", "#1f5a58", "#2c7470", "#c8963a", "#ecc876")
LEATHER = pg.hexes("#3a1c12", "#5a2c1a", "#7a4026", "#9a5634")
MATTRESS = pg.hexes("#7a6e58", "#a09276", "#c2b496", "#d8cbb0")
STAIN = pg.hexes("#6a5a40")[0]
MAT = pg.hexes("#4a3c28", "#6e5a3a", "#94805a", "#b4a076")


def covered(c, mask, rng):
    """먼지 덮개 천: 위가 밝고 아래로 주름이 늘어진다."""
    yy = c.yy[mask]
    top, bottom = yy.min(), yy.max()
    level = np.clip(((bottom - c.yy) / max(1, bottom - top) * 3).astype(int) + 1, 1, 4)
    folds = ((c.xx + (c.yy * 0.15).astype(int)) % 9 < 2) & (c.yy > top + (bottom - top) * 0.45)
    level = np.where(folds, np.maximum(level - 1, 0), level)
    c.image[mask, :3] = SHEET[level[mask]]
    c.image[mask, 3] = 255
    hem = mask & (c.yy >= bottom - 1)
    c.paint(hem, SHEET[0])


# ── 가구 ───────────────────────────────────────────────

def sheet_lump(rng, width, height):
    """먼지 덮개 천을 씌운 키 큰 가구(책장·찬장): 위가 둥글게 처지고 아래로 주름이 늘어진다."""
    c = Canvas(width, height)
    body = c.rect(3, 8, width - 3, height - 1) | c.ellipse(width / 2, 10, width / 2 - 3, 7)
    body &= ~c.polygon([(width - 3, height - 14), (width + 2, height - 1), (width - 3, height + 2)])
    covered(c, body, rng)
    return outline(c.image), (width // 2, height - 2)

def armchair(rng, sheet=False):
    """가죽 안락의자(앞에서 조금 위로 본 모습). sheet면 먼지 덮개 천을 씌웠다."""
    w, h = 42, 48
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    back = c.rect(7, 4, w - 7, ground - 18) | c.ellipse(cx, 8, 14, 6)
    arms = c.rect(3, ground - 26, 11, ground - 4) | c.rect(w - 11, ground - 26, w - 3, ground - 4)
    seat = c.rect(9, ground - 20, w - 9, ground - 8)
    legs = c.rect(6, ground - 4, 9, ground) | c.rect(w - 9, ground - 4, w - 6, ground)
    if sheet:
        body = back | arms | seat | c.rect(3, ground - 8, w - 3, ground - 1)
        covered(c, body, rng)
        return outline(c.image), (cx, ground)
    c.paint(back, LEATHER[1])
    c.paint(back & (c.yy < 14), LEATHER[2])
    for bx in (cx - 8, cx, cx + 8):
        for by in (12, 20):
            c.paint(c.rect(bx, by, bx + 1, by + 1), LEATHER[0])
    c.paint(seat, LEATHER[2])
    c.paint(seat & (c.yy < ground - 17), LEATHER[3])
    c.paint(arms, LEATHER[1])
    c.paint(arms & (c.yy < ground - 23), LEATHER[3])
    c.paint(legs, WOOD[1])
    c.paint(c.rect(9, ground - 8, w - 9, ground - 5), LEATHER[0])
    return outline(c.image), (cx, ground)


def round_table(rng, broken=False, mugs=True):
    """둥근 나무 탁자. broken이면 다리가 부러져 상판이 기울어 바닥에 닿았다."""
    w, h = 50, 40
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    if broken:
        top = c.polygon([(4, ground - 18), (w - 6, ground - 6), (w - 10, ground), (2, ground - 10)])
        c.paint(top, WOOD[3])
        c.paint(top & (c.yy < ground - 12), WOOD[4])
        c.line(30, ground - 18, 36, ground - 4, WOOD[1], 3)
        c.line(14, ground - 2, 22, ground - 6, WOOD[2], 2)
        return outline(c.image), (cx, ground)
    c.paint(c.rect(cx - 3, ground - 16, cx + 3, ground - 2), WOOD[1])
    c.paint(c.ellipse(cx, ground - 1, 12, 3), WOOD[1])
    top = c.ellipse(cx, ground - 22, 22, 9)
    c.paint(top, WOOD[3])
    c.paint(top & (c.yy < ground - 25), WOOD[4])
    c.paint(c.ellipse(cx, ground - 18, 22, 5) & ~top, WOOD[2])
    if mugs:
        for mx in (cx - 9, cx + 6):
            c.paint(c.rect(mx, ground - 30, mx + 5, ground - 23), PAPER[1])
            c.paint(c.rect(mx, ground - 30, mx + 5, ground - 29), WARM[3])
            c.paint(c.rect(mx + 5, ground - 28, mx + 7, ground - 25), PAPER[0])
        c.paint(c.ellipse(cx + 1, ground - 21, 4, 2), WARM[2])
    return outline(c.image), (cx, ground)


def stool(rng, toppled=False):
    """세 발 나무 걸상. toppled면 옆으로 쓰러졌다."""
    w, h = 22, 22
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    if toppled:
        c.paint(c.ellipse(5, ground - 6, 4, 6), WOOD[3])
        for dy in (-4, 0, 4):
            c.line(7, ground - 6 + dy, w - 3, ground - 5 + dy // 2, WOOD[1], 2)
        return outline(c.image), (cx, ground)
    for dx in (-6, 0, 6):
        c.line(cx, ground - 12, cx + dx, ground, WOOD[1], 2)
    seat = c.ellipse(cx, ground - 13, 9, 4)
    c.paint(seat, WOOD[3])
    c.paint(seat & (c.yy < ground - 14), WOOD[4])
    return outline(c.image), (cx, ground)


def bed(rng, made=True, broken=False):
    """
    벽에 머리를 붙인 1인 침대(발치에서 내려다본 모습): 나무 머리판, 베개, 이불.
    made가 아니면 얼룩진 맨 매트리스이고, broken이면 다리가 부러져 한쪽이 주저앉았다.
    """
    w, h = 34, 76
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    head = c.rect(2, 2, w - 2, 20) | c.ellipse(cx, 4, 15, 4)
    c.paint(head, WOOD[2])
    c.paint(head & (c.yy < 8), WOOD[4])
    c.paint(c.rect(5, 10, w - 5, 18), WOOD[1])
    frame = c.rect(2, 18, w - 2, ground)
    c.paint(frame, WOOD[2])
    c.paint(c.rect(2, ground - 4, w - 2, ground), WOOD[1])
    mattress = c.rect(4, 18, w - 4, ground - 5)
    if broken:
        # 왼쪽 앞다리가 부러져 그쪽 모서리가 바닥으로 내려앉았다.
        sunk = c.polygon([(1, ground - 16), (20, ground + 1), (1, ground + 1)])
        c.image[sunk, 3] = 0
        mattress &= ~c.polygon([(3, ground - 18), (22, ground - 3), (3, ground - 3)])
    if made:
        c.paint(mattress, MATTRESS[2])
        blanket = mattress & (c.yy > 32)
        c.paint(blanket, TEAL[1])
        c.paint(blanket & (c.yy < 36), TEAL[2])
        c.paint(blanket & ((c.xx - 4) % 9 == 0), TEAL[0])
        pillow = c.ellipse(cx, 25, 11, 5)
        c.paint(pillow, PAPER[2])
        c.paint(pillow & (c.yy > 26), PAPER[1])
    else:
        c.paint(mattress, MATTRESS[1])
        c.paint(mattress & ((c.xx - 4) % 6 < 2), MATTRESS[0])
        c.paint(mattress & c.ellipse(cx + 4, 46, 6, 4), STAIN)
        c.paint(mattress & (c.yy < 22), MATTRESS[2])
    return outline(c.image), (cx, ground)


def guild_desk(rng, sheet=False):
    """단장의 큰 책상: 서랍 달린 나무 몸통 위에 펼친 지도, 서류 더미, 잉크병, 놋쇠 톱니 문진. sheet면 덮개 천."""
    w, h = 96, 46
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    body = c.rect(4, ground - 22, w - 4, ground)
    top = c.rect(1, ground - 30, w - 1, ground - 20)
    if sheet:
        covered(c, body | top | c.rect(0, ground - 34, w, ground - 28), rng)
        c.paint(c.rect(w - 26, ground - 40, w - 12, ground - 33), PAPER[0])
        return outline(c.image), (cx, ground)
    c.paint(body, WOOD[2])
    for dx in (8, w - 30):
        c.paint(c.rect(dx, ground - 18, dx + 22, ground - 10), WOOD[3])
        c.paint(c.rect(dx + 9, ground - 15, dx + 13, ground - 13), BRASS[3])
    c.paint(c.rect(34, ground - 18, w - 34, ground), WOOD[1])
    c.paint(top, WOOD[3])
    c.paint(c.rect(1, ground - 30, w - 1, ground - 28), WOOD[4])
    mapped = c.rect(14, ground - 36, 52, ground - 24)
    c.paint(mapped, PAPER[1])
    c.paint(mapped & ((c.xx * 3 + c.yy * 5) % 23 < 2), pg.hexes("#7a8a5a")[0])
    c.line(18, ground - 30, 46, ground - 33, pg.hexes("#a03c30")[0], 1)
    for k in range(4):
        c.paint(c.rect(60 + k, ground - 34 - k * 2, 78 + k, ground - 26 - k * 2), PAPER[2 - k % 2])
    c.paint(c.rect(84, ground - 34, 89, ground - 28), IRON[1])
    c.paint(c.rect(85, ground - 37, 88, ground - 34), IRON[2])
    gear(c, 56, ground - 30, 4, 6, BRASS, open_spokes=False)
    return outline(c.image), (cx, ground)


def wardrobe(rng, empty=False):
    """옷장: 두 쪽 문과 놋쇠 손잡이. empty면 한쪽 문이 열려 안이 비었다."""
    w, h = 46, 88
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(3, 6, w - 3, ground), WOOD[2])
    c.paint(c.rect(1, 2, w - 1, 8), WOOD[3])
    c.paint(c.rect(1, 2, w - 1, 4), WOOD[4])
    for x0 in (6, cx + 1):
        c.paint(c.rect(x0, 11, x0 + 16, ground - 6), WOOD[3])
        c.paint(c.rect(x0 + 3, 15, x0 + 13, ground - 10), WOOD[2])
    c.paint(c.rect(cx - 3, 40, cx - 1, 46) | c.rect(cx + 2, 40, cx + 4, 46), BRASS[3])
    if empty:
        c.paint(c.rect(cx + 1, 11, w - 6, ground - 6), pg.hexes("#140e0a")[0])
        c.paint(c.rect(cx + 3, 18, w - 8, 20), IRON[2])
        door = c.polygon([(w - 6, 11), (w + 0, 16), (w + 0, ground - 2), (w - 6, ground - 6)])
        c.paint(door, WOOD[3])
    c.paint(c.rect(3, ground - 4, w - 3, ground), WOOD[1])
    return outline(c.image), (cx, ground)


def workbench(rng):
    """톱니 작업대: 바이스, 크고 작은 톱니, 렌치와 망치, 기름통."""
    w, h = 70, 50
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for x0 in (5, w - 10):
        c.paint(c.rect(x0, ground - 20, x0 + 5, ground), WOOD[1])
    c.paint(c.rect(5, ground - 8, w - 5, ground - 6), WOOD[1])
    top = c.rect(2, ground - 26, w - 2, ground - 18)
    c.paint(top, WOOD[3])
    c.paint(c.rect(2, ground - 26, w - 2, ground - 24), WOOD[4])
    c.paint(c.rect(6, ground - 36, 18, ground - 26), IRON[2])
    c.paint(c.rect(4, ground - 33, 20, ground - 30), IRON[3])
    gear(c, 34, ground - 33, 9, 9, BRASS)
    gear(c, 48, ground - 30, 5, 7, pg.hexes("#2e160c", "#5a2e1a", "#8a4a2a", "#b8693a", "#e09a62"), open_spokes=False)
    c.line(56, ground - 28, 64, ground - 32, IRON[3], 2)
    c.paint(c.rect(62, ground - 34, 66, ground - 30), IRON[2])
    c.paint(c.rect(22, ground - 14, 30, ground - 2), IRON[1])
    c.paint(c.rect(22, ground - 14, 30, ground - 12), IRON[3])
    return outline(c.image), (cx, ground)


def lantern(rng):
    """바닥에 놓은 작은 기름 등불(디엘의 것): 쇠 테 안에 따뜻한 유리."""
    w, h = 16, 24
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    c.paint(c.rect(cx - 5, ground - 3, cx + 5, ground), IRON[2])
    glass = c.rect(cx - 4, ground - 14, cx + 4, ground - 3)
    c.paint(glass, WARM[3])
    c.paint(c.rect(cx - 2, ground - 11, cx + 2, ground - 5), WARM[4])
    c.paint(glass & ((c.xx == cx - 4) | (c.xx == cx + 3)), IRON[2])
    c.paint(c.rect(cx - 5, ground - 17, cx + 5, ground - 14), IRON[2])
    c.paint(c.rect(cx - 2, ground - 21, cx + 2, ground - 17) & ~c.rect(cx - 1, ground - 20, cx + 1, ground - 17), IRON[3])
    return outline(c.image), (cx, ground)


def coat_rack(rng):
    """옷걸이 기둥에 걸린 청록 망토와 모자."""
    w, h = 28, 82
    c = Canvas(w, h)
    cx, ground = w // 2, h - 2
    for dx in (-8, 0, 8):
        c.line(cx, ground - 8, cx + dx, ground, WOOD[1], 2)
    c.paint(c.rect(cx - 1, 6, cx + 2, ground - 6), WOOD[2])
    c.line(cx - 9, 12, cx + 9, 8, WOOD[2], 2)
    cloak = c.polygon([(cx - 3, 12), (cx + 5, 12), (cx + 11, 50), (cx - 9, 50)])
    c.paint(cloak, TEAL[1])
    c.paint(cloak & (c.xx < cx - 2), TEAL[2])
    hat = c.ellipse(cx - 7, 12, 7, 3) | c.rect(cx - 11, 5, cx - 3, 12)
    c.paint(hat, LEATHER[1])
    return outline(c.image), (cx, ground)


# ── 바닥 장식 ───────────────────────────────────────────

def rug(columns, rows, rng):
    """길드 양탄자: 짙은 청록 바탕, 놋쇠 실 테두리와 모서리 톱니, 가운데 큰 톱니 문양."""
    w, h = int(columns * pg.TILE), int(rows * pg.TILE)
    c = Canvas(w, h)
    body = c.rect(0, 0, w, h)
    c.paint(body, RUG[2])
    c.paint(body & ((c.xx + c.yy) % 7 == 0), RUG[1])
    c.paint(body & ~c.rect(6, 6, w - 6, h - 6), RUG[0])
    c.paint(body & ~c.rect(8, 8, w - 8, h - 8) & c.rect(6, 6, w - 6, h - 6), RUG[4])
    c.paint(body & ~c.rect(14, 14, w - 14, h - 14) & c.rect(12, 12, w - 12, h - 12), RUG[3])
    for x, y in ((14, 14), (w - 14, 14), (14, h - 14), (w - 14, h - 14)):
        gear(c, x, y, 5, 6, BRASS, open_spokes=False)
    gear(c, w / 2, h / 2, min(w, h) * 0.22, 10, pg.hexes("#123a3a", "#1d5a58", "#2c7f7a", "#3f9a92", "#6ac2b4"))
    c.paint(c.ellipse(w / 2, h / 2, min(w, h) * 0.08, min(w, h) * 0.08), RUG[5])
    fringe = (c.xx % 3 == 0) & ((c.yy < 2) | (c.yy >= h - 2))
    c.paint(fringe, PAPER[1])
    return c.image


def runner(columns, rows, rng):
    """좁고 긴 붉은 깔개(숙소·복도)."""
    w, h = int(columns * pg.TILE), int(rows * pg.TILE)
    c = Canvas(w, h)
    body = c.rect(0, 0, w, h)
    c.paint(body, CLOTH["red"][1])
    c.paint(body & ((c.xx // 6 + c.yy // 6) % 2 == 0), CLOTH["red"][0])
    c.paint(body & ~c.rect(3, 3, w - 3, h - 3), BRASS[2])
    return c.image


def door_mat(rng, worn=False):
    """뒷문 앞 신발 깔개(바닥 장식): 거친 짚 깔개에 줄무늬 둘. 낡으면 해지고 먼지가 앉았다."""
    w, h = 40, 22
    c = Canvas(w, h)
    mat = c.rect(1, 1, w - 1, h - 1)
    c.paint(mat, MAT[2])
    c.paint(mat & ((c.xx + c.yy * 2) % 4 == 0), MAT[1])
    c.paint(mat & ((c.xx * 2 + c.yy) % 7 == 0), MAT[3])
    c.paint(mat & ~c.rect(3, 3, w - 3, h - 3), MAT[0])
    for x in (9, w - 12):
        c.paint(c.rect(x, 3, x + 3, h - 3), TEAL[1])
    if worn:
        holes = (pg.noise(h, w, 6, rng) > 0.62) | (rng.random((h, w)) < 0.05)
        c.image[holes & ~c.rect(6, 4, w - 6, h - 4), 3] = 0
        c.paint(mat & (pg.noise(h, w, 8, rng) > 0.55) & (c.image[..., 3] > 0), DUST[2])
    return c.image


def boots(rng, toppled=False):
    """벗어 둔 가죽 장화 한 켤레. toppled면 한 짝이 쓰러졌다."""
    w, h = 26, 22
    c = Canvas(w, h)
    ground = h - 2

    def boot(x, base):
        c.paint(c.rect(x, base - 15, x + 7, base), LEATHER[2])
        c.paint(c.rect(x, base - 15, x + 2, base), LEATHER[3])
        c.paint(c.rect(x, base - 5, x + 11, base), LEATHER[1])
        c.paint(c.rect(x, base - 1, x + 11, base), LEATHER[0])
        c.paint(c.rect(x, base - 16, x + 7, base - 14), LEATHER[3])

    boot(1, ground)
    if toppled:
        c.paint(c.rect(13, ground - 7, 25, ground), LEATHER[2])
        c.paint(c.rect(13, ground - 7, 25, ground - 5), LEATHER[3])
        c.paint(c.rect(21, ground - 8, 25, ground), LEATHER[1])
    else:
        boot(13, ground)
    return outline(c.image), (w // 2, ground)


def broom(rng):
    """벽에 기대 세운 빗자루."""
    w, h = 18, 66
    c = Canvas(w, h)
    ground = h - 2
    c.line(12, 2, 9, ground - 16, WOOD[3], 2)
    c.line(13, 2, 10, ground - 16, WOOD[4], 1)
    head = c.polygon([(6, ground - 18), (13, ground - 18), (16, ground), (2, ground)])
    c.paint(head, MAT[2])
    c.paint(head & (c.xx % 3 == 0), MAT[1])
    c.paint(c.rect(6, ground - 19, 13, ground - 15), IRON[2])
    return outline(c.image), (w // 2, ground)


def cushion(rng):
    """창가 걸터앉는 자리의 방석."""
    c = Canvas(36, 14)
    pad = c.ellipse(18, 7, 17, 6)
    c.paint(pad, CLOTH["red"][1])
    c.paint(pad & (c.yy < 6), CLOTH["red"][2])
    c.paint(c.rect(17, 6, 19, 8), BRASS[3])
    return outline(c.image)


def dust(columns, rows, rng):
    """바닥에 내려앉은 먼지 얼룩(반투명하지 않게 점을 흩뿌린다)."""
    w, h = int(columns * pg.TILE), int(rows * pg.TILE)
    c = Canvas(w, h)
    field = pg.noise(h, w, 10, rng)
    edge = np.hypot((c.xx - w / 2) / (w / 2), (c.yy - h / 2) / (h / 2))
    patch = (field > 0.52 + edge * 0.35) & ((c.xx + c.yy * 3) % 2 == 0)
    c.paint(patch, DUST[1])
    c.paint(patch & (field > 0.7 + edge * 0.2), DUST[2])
    return c.image


def debris(rng):
    """부서진 판자 조각과 나뭇잎, 잔해."""
    c = Canvas(70, 40)
    for _ in range(6):
        x, y = rng.uniform(6, 60), rng.uniform(6, 32)
        angle = rng.uniform(-0.6, 0.6)
        length = rng.uniform(14, 26)
        c.line(x, y, x + np.cos(angle) * length, y + np.sin(angle) * length, WOOD[int(rng.integers(2, 4))], 3)
    for _ in range(10):
        x, y = rng.uniform(4, 66), rng.uniform(4, 36)
        c.paint(c.ellipse(x, y, 2, 1.5), pg.hexes("#7a5a2a", "#8a6a2a", "#5a4a2a")[int(rng.integers(0, 3))])
    return outline(c.image)


def bedroll(rng):
    """바닥에 편 디엘의 침낭과 둘둘 만 담요."""
    c = Canvas(74, 30)
    mat = c.rect(2, 8, 64, 28)
    c.paint(mat, CLOTH["green"][1])
    c.paint(mat & (c.yy < 12), CLOTH["green"][2])
    c.paint(mat & ((c.xx - 2) % 12 == 0), CLOTH["green"][0])
    roll = c.ellipse(64, 16, 8, 10)
    c.paint(roll, PAPER[0])
    c.paint(c.ellipse(64, 16, 4, 6), SHEET[1])
    c.paint(c.rect(4, 12, 18, 24), PAPER[1])
    return outline(c.image)


def papers(rng):
    """바닥에 흩어진 서류와 찢어진 지도 조각."""
    c = Canvas(64, 36)
    for _ in range(7):
        x, y = rng.uniform(6, 54), rng.uniform(4, 26)
        sheet = c.polygon([(x, y), (x + 10, y + rng.uniform(-2, 2)), (x + 11, y + 8), (x + 1, y + 9)])
        c.paint(sheet, PAPER[int(rng.integers(0, 3))])
    return outline(c.image)


def ashes():
    """식은 벽난로의 재와 타다 남은 장작."""
    c = Canvas(26, 12)
    c.paint(c.ellipse(13, 8, 12, 4), DUST[0])
    c.paint(c.ellipse(13, 8, 9, 3), DUST[2])
    c.line(5, 7, 19, 9, pg.hexes("#1a1210")[0], 2)
    c.line(8, 9, 20, 6, pg.hexes("#2a1c14")[0], 2)
    return c.image


# ── 벽면 장식 ───────────────────────────────────────────

def boards(width, height, rng):
    """창을 막은 판자: 엇갈려 박은 나무판 서너 장과 못."""
    c = Canvas(width, height)
    for k in range(4):
        y = 3 + k * (height - 8) / 3 + rng.uniform(-2, 2)
        tilt = rng.uniform(-3, 3)
        band = c.polygon([(0, y + tilt), (width, y - tilt), (width, y - tilt + 6), (0, y + tilt + 6)])
        c.paint(band, WOOD[int(rng.integers(2, 4))])
        c.paint(band & (c.yy < y + 2), WOOD[4])
        for nx in (3, width - 4):
            c.paint(c.rect(nx, int(y + 2), nx + 1, int(y + 3)), IRON[3])
    return outline(c.image)


def cobweb(size, corner):
    """구석 거미줄: corner는 'left'(왼쪽 위) 또는 'right'(오른쪽 위)."""
    c = Canvas(size, size)
    ox = 0 if corner == "left" else size - 1
    dx = np.abs(c.xx - ox)
    d, a = np.hypot(dx, c.yy), np.arctan2(c.yy, dx)
    spokes = (np.abs(((a / (np.pi / 2)) * 5) % 1 - 0.5) > 0.44) & (d < size)
    rings = (np.abs(d % (size / 4) - 1) < 0.6) & (d < size - 2)
    c.paint(spokes | rings, WEB[0])
    c.paint((spokes | rings) & (d < size * 0.4), WEB[1])
    return c.image


def guild_banner(rng, torn=False):
    """벽에 거는 청록 길드 깃발: 놋쇠 막대, 가운데 톱니 문장, 아래가 제비 꼬리. torn이면 찢기고 빛바랬다."""
    w, h = 28, 48
    c = Canvas(w, h)
    cloth = c.polygon([(3, 4), (w - 3, 4), (w - 3, h - 2), (w / 2, h - 10), (3, h - 2)])
    palette = SHEET[:3] if torn else TEAL
    if torn:
        cloth &= ~c.polygon([(w - 3, 22), (w - 10, 30), (w - 3, 40)])
        cloth &= ~c.rect(3, 36, 10, h)
    c.paint(cloth, palette[1])
    c.paint(cloth & (c.xx < 7), palette[2])
    c.paint(cloth & (c.xx > w - 7), palette[0])
    c.paint(c.rect(0, 2, w, 5), BRASS[2] if not torn else IRON[2])
    if not torn:
        gear(c, w / 2, 20, 7, 8, BRASS, open_spokes=False)
    else:
        c.paint(c.ellipse(w / 2, 20, 6, 6) & cloth, SHEET[0])
    return outline(c.image)


def gear_clock(rng):
    """벽시계: 놋쇠 톱니 테 안의 흰 판과 바늘."""
    c = Canvas(30, 30)
    gear(c, 15, 15, 14, 12, BRASS, open_spokes=False)
    c.paint(c.ellipse(15, 15, 9, 9), PAPER[2])
    for k in range(12):
        a = k * np.pi / 6
        c.paint(c.rect(int(15 + np.cos(a) * 7), int(15 + np.sin(a) * 7), int(15 + np.cos(a) * 7) + 1, int(15 + np.sin(a) * 7) + 1), IRON[1])
    c.line(15, 15, 15, 9, IRON[0], 1)
    c.line(15, 15, 19, 17, IRON[0], 1)
    return outline(c.image)


def wall_map(rng):
    """벽에 핀으로 꽂은 지도: 숲·강·길과 붉은 표시."""
    w, h = 44, 30
    c = Canvas(w, h)
    paper = c.rect(1, 1, w - 1, h - 1)
    c.paint(paper, PAPER[1])
    field = pg.noise(h, w, 8, rng)
    c.paint(paper & (field > 0.58), pg.hexes("#8aa070")[0])
    c.line(4, 22, 38, 6, pg.hexes("#6a8ab0")[0], 2)
    c.line(6, 8, 30, 26, pg.hexes("#a08060")[0], 1)
    for x, y in ((12, 12), (30, 18)):
        c.paint(c.rect(x, y, x + 3, y + 3), pg.hexes("#c0302a")[0])
    for x, y in ((2, 2), (w - 4, 2), (2, h - 4), (w - 4, h - 4)):
        c.paint(c.rect(x, y, x + 2, y + 2), BRASS[3])
    return outline(c.image)


def shelf_books(rng, empty=False):
    """벽에 붙인 선반 두 줄의 책(빈 선반이면 쓰러진 책 두어 권과 거미줄)."""
    w, h = 60, 38
    c = Canvas(w, h)
    for y0 in (14, 34):
        c.paint(c.rect(0, y0, w, y0 + 3), WOOD[3])
        c.paint(c.rect(0, y0, w, y0 + 1), WOOD[4])
        x = 3
        while x < w - 6:
            bw = int(rng.integers(3, 6))
            if empty and rng.random() < 0.75:
                x += bw + 3
                continue
            bh = int(rng.integers(8, 12))
            c.paint(c.rect(x, y0 - bh, x + bw, y0), BOOKS[int(rng.integers(0, len(BOOKS)))])
            x += bw + 1
    return outline(c.image)


def sconce(rng, lit=True):
    """벽 등잔: 놋쇠 받침과 유리 갓. 꺼진 등은 그을린 유리다."""
    c = Canvas(14, 22)
    c.paint(c.rect(5, 14, 9, 21), BRASS[2])
    c.paint(c.rect(3, 18, 11, 20), BRASS[3])
    glass = c.ellipse(7, 9, 5, 6)
    c.paint(glass, WARM[3] if lit else DUST[0])
    if lit:
        c.paint(c.ellipse(7, 10, 2, 3), WARM[4])
    return outline(c.image)
