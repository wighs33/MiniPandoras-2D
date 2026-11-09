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
    }
    for name, rgba in outputs.items():
        Image.fromarray(rgba, "RGBA").save(kit / name)
        print((kit / name).relative_to(root).as_posix(), rgba.shape[1], "x", rgba.shape[0])
    vitals_preview(root, {name[:-4]: rgba for name, rgba in outputs.items()})


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
