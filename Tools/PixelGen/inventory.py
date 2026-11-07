"""인벤토리 창과 아이템의 그림(불량배의 방망이를 주워 인벤토리에서 장착). 웹게임에는 없는 창이다.

창은 판도라 정착 창(pandora_loadout.py)과 같은 남색 창에 금테 두 줄이고, 그 위에 왼쪽 능력치 칸·오른쪽 위 장착 칸·그 아래 가방 칸·오른쪽 아래 정보 칸을 새겨 둔다.
장비창은 없다(옷은 바꿔 입지 않고 무기만 가방에서 장착한다).
아이템 칸, 고른 칸의 금빛 테, 아이템 문양(방망이·회복약·더스트 결정·뱀신의 눈), 맵 바닥에 떨어진 방망이도 함께 그린다. 글자는 창이 얹는다.
창 그림은 가상 px의 두 배 점으로 그려 점 하나 = 2px로 키워 저장한다. 바닥의 방망이는 맵 그림과 같이 한 칸 32점이다.

  Assets/MiniPandoras/Art/PixelGen/UI/Inventory/panel.png, cell.png, cell_focus.png, item_<아이템>.png
  Assets/MiniPandoras/Art/PixelGen/Items/bat_ground.png, bat_ground_shape.png(그림자 없는 모양, 금빛 테두리용)
  Assets/MiniPandoras/Art/PixelGen/Effects/pickup_sparkle.png(바닥에 떨어진 아이템 위에서 반짝이는 별빛)
  Docs/ArtUpgrade/PixelGen/inventory_preview.png

사용: python Tools/PixelGen/inventory.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas
from pandora_loadout import GOLD, NAVY, rounded

D = 2                              # 가상 px 하나 = 2점
PARCHMENT = pg.hexes("#7a5a34", "#b08d57", "#d9bd85", "#efdcae")
PANEL = (10, 8, 364, 200)          # 창 자리(가상 화면 x, y, 너비, 높이). 아래 칸들은 창 왼쪽 위에서 잰다
STATS_BOX = (8, 28, 112, 192)      # 능력치 칸(x0, y0, x1, y1)
EQUIP_BOX = (118, 28, 356, 62)     # 장착 칸
BAG_BOX = (118, 66, 356, 127)      # 가방 칸
INFO_BOX = (118, 131, 356, 192)    # 정보 칸
CELL = 24                          # 아이템 칸 한 변
PITCH = 27                         # 아이템 칸 사이(가운데에서 가운데)
BAG_TOP_LEFT = (131, 71)           # 가방 첫 칸의 왼쪽 위(8×2 칸을 가방 칸 가운데에 둔다)
BAG_COLUMNS, BAG_ROWS = 8, 2
ICON = 20                          # 아이템 문양 한 변
INK = pg.hexes("#0a0d1c")[0]
SHADE = pg.hexes("#0b0f22", "#0e1328", "#11172f")


# ── 창 ────────────────────────────────────────────────

def inset(c, box, shade):
    """창에 새긴 칸: 바탕보다 짙은 남색, 안쪽 위·왼쪽에 그늘, 옅은 금테 한 줄."""
    x0, y0, x1, y1 = (v * D for v in box)
    body = rounded(c, x0, y0, x1, y1, 3 * D)
    c.paint(body, shade)
    c.paint(body & ~rounded(c, x0, y0 + D, x1, y1, 3 * D), SHADE[0])
    c.paint(body & pg.neighbours(~body), GOLD[0])


def panel():
    """창 바탕(가상 364×200): 정착 창과 같은 남색·금테에, 가방 칸·정보 칸을 새긴다."""
    w, h = PANEL[2] * D, PANEL[3] * D
    c = Canvas(w, h)
    body = rounded(c, 0, 0, w, h, 5 * D)
    c.paint(body, pg.quantize(np.clip(1 - c.yy / h, 0, 0.999), NAVY, dither=0.15))
    outer = body & pg.neighbours(~body)
    outer |= pg.neighbours(outer) & body
    c.paint(outer, GOLD[1])
    inner_body = rounded(c, 3 * D, 3 * D, w - 3 * D, h - 3 * D, 3 * D)
    c.paint(inner_body & pg.neighbours(~inner_body), GOLD[0])
    inset(c, STATS_BOX, SHADE[1])
    inset(c, EQUIP_BOX, SHADE[2])
    inset(c, BAG_BOX, SHADE[2])
    inset(c, INFO_BOX, SHADE[1])
    return c.image


def cell(focused):
    """아이템 칸(가상 24×24): 짙은 남색 판에 금테. 고른 칸은 밝은 금테 두 줄에 옅은 금빛이 돈다."""
    size = CELL * D
    c = Canvas(size, size)
    # 테가 그림 끝에 닿으면 바깥을 모양 밖으로 보지 못해 테가 그려지지 않으므로 한 점씩 비워 둔다.
    body = rounded(c, 1, 1, size - 1, size - 1, 2 * D)
    if focused:
        c.image[body, :3] = pg.hexes("#ffdc8c")[0]
        c.image[body, 3] = 34
        edge = body & pg.neighbours(~body)
        c.paint(edge | (pg.neighbours(edge) & body), GOLD[3])
        return c.image
    c.paint(body, NAVY[0])
    c.paint(body & ~rounded(c, 1, 1 + D, size - 1, size - 1, 2 * D), SHADE[0])
    c.paint(body & pg.neighbours(~body), GOLD[1])
    return c.image


# ── 아이템 문양 ─────────────────────────────────────────

def shade_body(c, mask, palette, direction=(-0.6, -0.8)):
    """모양 안을 빛이 오는 쪽(왼쪽 위)으로 밝게 칠한다."""
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return
    dx, dy = direction
    t = (c.xx - xs.mean()) * dx + (c.yy - ys.mean()) * dy
    t = (t - t[mask].min()) / max(1e-6, t[mask].max() - t[mask].min())
    c.paint(mask, pg.quantize(np.clip(t, 0, 0.999), palette, dither=0.12))


def bat_icon():
    """방망이(가상 20×20): 왼쪽 아래 손잡이에서 오른쪽 위로 굵어지는 나무 방망이. 손잡이에 감은 천, 끝에 박힌 못 둘."""
    size = ICON * D
    c = Canvas(size, size)
    a = np.array([7.0, 33.0])          # 손잡이 끝(점)
    b = np.array([33.0, 7.0])          # 머리 끝
    axis = (b - a) / np.linalg.norm(b - a)
    rel = np.stack([c.xx + 0.5 - a[0], c.yy + 0.5 - a[1]], axis=-1)
    along = rel @ axis
    across = np.abs(rel @ np.array([-axis[1], axis[0]]))
    length = np.linalg.norm(b - a)
    radius = 2.0 + np.clip(along / length, 0, 1) ** 1.4 * 3.6
    club = (along >= 0) & (along <= length) & (across <= radius)
    club |= c.ellipse(b[0] - axis[0] * 0.5, b[1] - axis[1] * 0.5, 5.4, 5.4)
    club |= c.ellipse(a[0], a[1], 2.6, 2.6)
    shade_body(c, club, pg.WOOD[1:])
    grip = club & (along >= 1.5) & (along <= 11)
    wrap = grip & (np.floor(along / 2.2) % 2 == 0)
    c.paint(grip, pg.hexes("#4a3a52")[0])
    c.paint(wrap, pg.hexes("#6e5a78")[0])
    for t, side in ((0.78, 1), (0.9, -1)):
        nx, ny = a + axis * length * t + np.array([-axis[1], axis[0]]) * side * (radius.max() - 0.5)
        c.paint(c.rect(round(nx) - 1, round(ny) - 1, round(nx) + 1, round(ny) + 1), pg.STONE[4])
    return pg.outline(c.image, INK)


def potion_icon():
    """회복약(가상 20×20): 둥근 병에 붉은 물약, 코르크 마개, 병목의 끈, 유리의 빛."""
    size = ICON * D
    c = Canvas(size, size)
    bottle = c.ellipse(20, 25, 11, 10) | c.rect(16, 9, 24, 17)
    c.paint(bottle, pg.hexes("#9fb4c8")[0])
    liquid = c.ellipse(20, 25, 9.5, 8.5) & (c.yy >= 19)
    shade_body(c, liquid, pg.hexes("#6a1622", "#9c2332", "#c83a44", "#e86a62"))
    c.paint(c.rect(15, 4, 25, 10), pg.hexes("#a5845a")[0])
    c.paint(c.rect(15, 4, 25, 6), pg.hexes("#c0a274")[0])
    c.paint(c.rect(15, 13, 25, 15), pg.hexes("#c9a55a")[0])
    c.paint(c.ellipse(14, 22, 2, 3) & bottle, pg.hexes("#f2f6fa")[0])
    return pg.outline(c.image, INK)


def ether_icon():
    """더스트 결정(가상 20×20): 푸른 보랏빛 결정 기둥 셋과 그 빛."""
    size = ICON * D
    c = Canvas(size, size)
    crystal = pg.hexes("#3a2f7a", "#5246a8", "#6f66d0", "#9a92ec", "#cfcaff")
    for (x, top, w, h) in ((20, 3, 7, 30), (11, 13, 5, 20), (29, 15, 5, 18)):
        shape = c.polygon([(x, top), (x + w / 2, top + 4), (x + w / 2, top + h - 2), (x, top + h), (x - w / 2, top + h - 2), (x - w / 2, top + 4)])
        shade_body(c, shape, crystal, direction=(-1.0, -0.2))
        c.paint(shape & (c.xx + 0.5 >= x) & (c.xx + 0.5 < x + 1), crystal[4])
    return pg.outline(c.image, INK)


def bat_ground(shadow=True):
    """
    맵 바닥에 떨어진 방망이(한 칸 32점 격자, 26×10점): 비스듬히 누운 방망이와 그 밑 그림자. 발밑 기준점은 아래 가운데다.
    shadow=False면 그림자 없는 방망이 모양만 그린다(bat_ground_shape). 게임이 이 모양으로 아이템 둘레의 금빛 테두리를 그린다.
    """
    w, h = 26, 10
    c = Canvas(w, h)
    if shadow:
        c.paint(c.ellipse(13, 8, 11, 1.8), pg.hexes("#1f2a1c")[0])
    a, b = np.array([3.0, 6.0]), np.array([23.0, 3.5])
    axis = (b - a) / np.linalg.norm(b - a)
    rel = np.stack([c.xx + 0.5 - a[0], c.yy + 0.5 - a[1]], axis=-1)
    along = rel @ axis
    across = np.abs(rel @ np.array([-axis[1], axis[0]]))
    length = np.linalg.norm(b - a)
    club = (along >= 0) & (along <= length) & (across <= 1.0 + np.clip(along / length, 0, 1) * 1.6)
    club |= c.ellipse(b[0], b[1], 2.4, 2.4)
    shade_body(c, club, pg.WOOD[1:])
    c.paint(club & (along >= 0.5) & (along <= 5), pg.hexes("#4a3a52")[0])
    image = pg.outline(c.image, pg.OUTLINE)
    image[(image[..., 3] > 0) & (np.arange(h)[:, None] >= h - 1)] = 0
    return image


def pickup_sparkle():
    """바닥 아이템의 반짝임(7×7점): 흰 가운데에서 위아래·양옆으로 뻗는 네 갈래 별빛. 크기를 키웠다 줄이는 것은 게임이 한다."""
    c = Canvas(7, 7)
    c.paint(c.rect(3, 0, 4, 7) | c.rect(0, 3, 7, 4), pg.hexes("#f0a030")[0])
    c.paint(c.rect(3, 1, 4, 6) | c.rect(1, 3, 6, 4), pg.hexes("#fff1a8")[0])
    c.paint(c.rect(3, 3, 4, 4), pg.hexes("#fffdf0")[0])
    return c.image


# ── 저장 ──────────────────────────────────────────────

def map_sheet(c, corners):
    """낡은 양피지 한 장: 접힌 자국 두 줄과 붉은 표시."""
    sheet = c.polygon(corners)
    shade_body(c, sheet, PARCHMENT)
    (ax, ay), (bx, by), (cx, cy), (dx, dy) = corners
    for t in (1 / 3, 2 / 3):
        c.paint(sheet & c.polygon([(ax + (bx - ax) * t - 0.6, ay + (by - ay) * t), (ax + (bx - ax) * t + 0.6, ay + (by - ay) * t),
                                   (dx + (cx - dx) * t + 0.6, dy + (cy - dy) * t), (dx + (cx - dx) * t - 0.6, dy + (cy - dy) * t)]), PARCHMENT[0])
    return sheet


def map_icon():
    """지도(가상 20×20): 접힌 양피지 위에 길과 붉은 표시."""
    c = Canvas(ICON * D, ICON * D)
    map_sheet(c, [(5, 9), (35, 5), (37, 31), (7, 35)])
    for x, y in ((11, 27), (15, 24), (19, 25), (23, 21), (26, 17)):
        c.paint(c.ellipse(x, y, 1, 1), PARCHMENT[0])
    red = pg.hexes("#b0302a")[0]
    c.line(26, 11, 32, 17, red, 2)
    c.line(32, 11, 26, 17, red, 2)
    return pg.outline(c.image, INK)


def map_ground(shadow=True):
    """맵 바닥에 떨어진 지도(26×12점). shadow=False면 금빛 테두리를 뜰 모양만 그린다."""
    c = Canvas(26, 12)
    if shadow:
        c.paint(c.ellipse(13, 9.5, 11, 2), pg.hexes("#1f2a1c")[0])
    map_sheet(c, [(3, 5), (21, 2), (24, 7), (5, 10)])
    c.paint(c.ellipse(15, 5.5, 1.2, 1.2), pg.hexes("#b0302a")[0])
    image = pg.outline(c.image, pg.OUTLINE)
    image[(image[..., 3] > 0) & (np.arange(12)[:, None] >= 11)] = 0
    return image


def serpent_eye_icon():
    """뱀신의 눈(가상 20×20): 맵의 보석 그림(웹게임 jewel)처럼 금테 고리 안의 보랏빛 보석에, 세로로 찢어진 뱀의 동공."""
    size = ICON * D
    c = Canvas(size, size)
    ring = c.ellipse(20, 20, 18, 18) & ~c.ellipse(20, 20, 16, 16)
    shade_body(c, ring, GOLD)
    for x, y in ((20, 3), (37, 20), (20, 37), (3, 20)):
        shade_body(c, c.polygon([(x, y - 3), (x + 3, y), (x, y + 3), (x - 3, y)]), GOLD)
    gem = c.polygon([(20, 6), (31, 20), (20, 34), (9, 20)])
    shade_body(c, gem, pg.hexes("#3c1470", "#6a1fb0", "#9a3ae0", "#c98af5"))
    c.paint(gem & (c.xx + c.yy < 28) & (c.xx - c.yy > 8), pg.hexes("#e9c8ff")[0])
    c.paint(c.ellipse(20, 20, 1.8, 10), pg.hexes("#12061f")[0])
    return pg.outline(c.image, INK)


def hobbit_ruby_icon():
    """호빗의 홍옥: 금 받침에 박힌 붉은 보석."""
    size = ICON * D
    c = Canvas(size, size)
    shade_body(c, c.ellipse(20, 26, 15, 9), GOLD)
    gem = c.polygon([(12, 12), (28, 12), (34, 20), (20, 34), (6, 20)])
    shade_body(c, gem, pg.hexes("#5a0c18", "#a01826", "#d8303a", "#f47c74"))
    c.paint(c.polygon([(13, 13), (20, 13), (16, 20), (8, 20)]), pg.hexes("#ffb8ac")[0])
    return pg.outline(c.image, INK)


def kings_horn_icon():
    """왕의 뿔: 금테를 두른 굽은 뿔."""
    size = ICON * D
    c = Canvas(size, size)
    horn = c.polygon([(6, 34), (14, 16), (24, 7), (35, 5), (28, 12), (22, 22), (18, 36)])
    shade_body(c, horn, pg.hexes("#5a4632", "#8a7050", "#c0a880", "#e8d8b8"))
    for y in (24, 29):
        c.paint(horn & (c.yy >= y) & (c.yy < y + 2), GOLD[2])
    return pg.outline(c.image, INK)


def guild_medal_icon():
    """모험가 훈장: 푸른 띠에 매달린 금빛 메달과 엇갈린 칼."""
    size = ICON * D
    c = Canvas(size, size)
    ribbon = c.polygon([(11, 3), (29, 3), (24, 18), (16, 18)])
    shade_body(c, ribbon, pg.hexes("#16264a", "#2a4478", "#4a6cae", "#7a9cd8"))
    c.paint(c.rect(19, 3, 21, 18), GOLD[2])
    medal = c.ellipse(20, 25, 12, 12)
    shade_body(c, medal, GOLD)
    c.paint(c.ellipse(20, 25, 8, 8), GOLD[1])
    c.line(14, 31, 26, 19, pg.hexes("#f4f0e8")[0], 2)
    c.line(26, 31, 14, 19, pg.hexes("#d8d4c8")[0], 2)
    return pg.outline(c.image, INK)


ITEMS = {"bat": bat_icon, "worldMap": map_icon, "potion": potion_icon, "ether": ether_icon, "serpentEye": serpent_eye_icon,
         "hobbitRuby": hobbit_ruby_icon, "kingsHorn": kings_horn_icon, "guildMedal": guild_medal_icon}


def paste(preview, path, x, y):
    """미리보기에 그림 하나를 창 왼쪽 위에서 잰 가상 좌표(가운데)에 얹는다."""
    image = Image.open(path)
    scale = D * pg.SCALE
    preview.alpha_composite(image, (round(x * scale - image.width / 2), round(y * scale - image.height / 2)))


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/UI/Inventory"
    out.mkdir(parents=True, exist_ok=True)
    parts = {"panel": panel(), "cell": cell(False), "cell_focus": cell(True)}
    parts.update({f"item_{item}": draw() for item, draw in ITEMS.items()})
    for name, image in parts.items():
        pg.save_png(image, out / f"{name}.png")
    items = project / "Assets/MiniPandoras/Art/PixelGen/Items"
    items.mkdir(parents=True, exist_ok=True)
    pg.save_png(bat_ground(), items / "bat_ground.png")
    pg.save_png(bat_ground(shadow=False), items / "bat_ground_shape.png")
    pg.save_png(map_ground(), items / "worldMap_ground.png")
    pg.save_png(map_ground(shadow=False), items / "worldMap_ground_shape.png")
    effects = project / "Assets/MiniPandoras/Art/PixelGen/Effects"
    effects.mkdir(parents=True, exist_ok=True)
    pg.save_png(pickup_sparkle(), effects / "pickup_sparkle.png")

    # 미리보기: 창에 가방 칸(회복약·더스트 결정·방망이, 더스트 결정을 고름)과 정보 칸의 문양을 얹는다.
    preview = Image.open(out / "panel.png").convert("RGBA")
    shown = ("item_potion.png", "item_ether.png", "item_bat.png")
    for i in range(BAG_COLUMNS * BAG_ROWS):
        x = BAG_TOP_LEFT[0] + CELL / 2 + (i % BAG_COLUMNS) * PITCH
        y = BAG_TOP_LEFT[1] + CELL / 2 + (i // BAG_COLUMNS) * PITCH
        paste(preview, out / ("cell_focus.png" if i == 1 else "cell.png"), x, y)
        if i < len(shown):
            paste(preview, out / shown[i], x, y)
    paste(preview, out / "cell.png", INFO_BOX[0] + 6 + CELL / 2, INFO_BOX[1] + 6 + CELL / 2)
    paste(preview, out / "item_ether.png", INFO_BOX[0] + 6 + CELL / 2, INFO_BOX[1] + 6 + CELL / 2)
    ground = Image.open(items / "bat_ground.png")
    preview.alpha_composite(ground.resize((ground.width * 2, ground.height * 2), Image.NEAREST), (40, 40))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/inventory_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
