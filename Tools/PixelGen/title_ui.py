"""타이틀 화면의 부품. 뒤의 무대는 Unity에서 3D로 세우고(Editor/Stages), 창 틀·버튼 같은 부품은 ui_kit.py, 로고는 Tools/Logo가 그린다.

메뉴는 HUD의 마름모 버튼(hud_buttons.py)과 같은 말을 쓴다: 양 끝이 마름모인 띠, 마름모 구슬, 왼쪽 위에서 오는 빛.
주 버튼만 빛나는 판이고, 나머지는 판 없이 글과 마름모 표시뿐이라 무대를 가리지 않는다.

  Assets/MiniPandoras/Art/PixelGen/UI/Title/
    primary.png       주 버튼: 놋쇠 테의 빛나는 띠, 양 끝에 판도라 빛 보석(150×36 가상 px)
    primary_small.png 챕터 선택의 시작 버튼(112×28)
    primary_glow.png  주 버튼 둘레로 번지는 빛(숨 쉬듯 밝기가 바뀐다)
    shine.png         주 버튼 위를 지나가는 빛줄기
    bullet.png        메뉴 글 앞의 마름모 표시, bullet_full.png 눌린 표시
    line.png          메뉴 글 아래의 가는 선(오른쪽으로 도트가 성기게 흩어진다)
    side_shade.png    메뉴 뒤에 깔리는 옆 그늘(부드러운 알파, 선형 필터)
    plate_shade.png   가운데가 고르게 짙은 둥근 판. 「지난 스토리」 창의 안내 글과 대전 시작 띠 뒤에 깐다

사용: python Tools/PixelGen/title_ui.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from ui_kit import NIGHT

D = 2                       # 가상 px 하나 = 2점
PRIMARY = (150, 36)         # 주 버튼의 크기(가상 px)
PRIMARY_SMALL = (112, 28)   # 챕터 선택의 시작 버튼
GLOW = 10                   # 주 버튼 둘레로 빛이 번지는 폭(가상 px)
INK = (38, 22, 52)
BRASS_LIGHT = (255, 238, 190)
BRASS = (214, 160, 78)
BRASS_DARK = (120, 70, 36)
FILL = pg.hexes("#d89a52", "#e9b974", "#f5d79c", "#fdecc4", "#fffaea").astype(float)
GEM = pg.hexes("#3a1c8c", "#6a44e0", "#a78bff", "#f1ecff")
LANTERN = (247, 212, 173)


def night_alpha(alpha):
    rgba = np.zeros((*alpha.shape, 4), np.uint8)
    rgba[..., :3] = NIGHT
    rgba[..., 3] = (np.clip(alpha, 0, 1) * 255).round().astype(np.uint8)
    return rgba


def plate_shade():
    """가운데가 고르게 짙고 가장자리 3할에서만 사라지는 둥근 판(160×48점). 글자 뒤에 깔아 뒤 그림이 밝아도 읽히게 한다."""
    w, h = 160, 48
    y, x = np.mgrid[0:h, 0:w]
    dx = np.abs(x - (w - 1) / 2) / (w / 2)
    dy = np.abs(y - (h - 1) / 2) / (h / 2)
    d = np.maximum(dx, dy * 0.85 + dx * 0.15)
    t = np.clip((1 - d) / 0.3, 0, 1)
    return night_alpha(t * t * (3 - 2 * t))


def side_shade():
    """왼쪽이 짙고 오른쪽으로 사라지는 그늘(64×16점, 늘려 쓴다)."""
    w, h = 64, 16
    t = 1 - np.arange(w) / (w - 1)
    return night_alpha(np.repeat((t * t * (3 - 2 * t))[None] * 0.72, h, axis=0))


def ribbon_field(width, height):
    """띠의 가운데에서 잰 좌표와, 양 끝이 45도로 뾰족한 띠의 가장자리에서 안쪽으로 잰 거리(점)."""
    w, h = width * D, height * D
    y, x = np.mgrid[0:h, 0:w]
    x = x - (w - 1) / 2
    y = y - (h - 1) / 2
    return x, y, np.minimum(h / 2 - np.abs(y), w / 2 - np.abs(x) - np.abs(y))


def ribbon(size, gem_reach):
    """놋쇠 테를 두른 빛나는 띠. 양 끝에 네 면으로 깎은 보석을 박는다."""
    width, height = size
    x, y, inside = ribbon_field(width, height)
    rgba = np.zeros((*x.shape, 4), np.uint8)
    lit = (y < 0) & (x < width * D / 2 - height)
    bands = ((0, 2, INK, INK), (2, 5, BRASS_LIGHT, BRASS), (5, 7, BRASS_DARK, BRASS_DARK))
    for start, end, light, dark in bands:
        band = (inside >= start) & (inside < end)
        rgba[band & lit] = (*light, 255)
        rgba[band & ~lit] = (*dark, 255)
    body = inside >= 7
    half = height * D / 2 - 7
    level = np.clip(0.5 - y / (2 * half), 0, 1) * (len(FILL) - 1)
    # 가운데로 갈수록 밝다.
    level = level * 0.8 + (1 - np.abs(x) / (width * D / 2)) * 0.9
    step = np.clip(np.round(level + (pg.bayer(*x.shape) - 0.5) * 0.9).astype(int), 0, len(FILL) - 1)
    rgba[body] = np.dstack([FILL[step], np.full(x.shape, 255)])[body]
    rgba[body & (inside < 8.5) & (y < 0)] = (255, 253, 244, 255)
    rgba[body & (inside < 8.5) & (y > 0)] = (190, 122, 60, 255)
    for side in (-1, 1):
        gem(rgba, x - side * (width * D / 2 - height - 2), y, gem_reach)
    return rgba


def gem(rgba, x, y, size):
    """네 면으로 깎은 마름모 보석."""
    reach = np.abs(x) + np.abs(y)
    rgba[reach < size] = (*BRASS_DARK, 255)
    rgba[reach < size - 2] = (*INK, 255)
    face = np.where(y < 0, np.where(x < 0, 3, 2), np.where(x < 0, 1, 0))
    stone = reach < size - 4
    rgba[stone, :3] = GEM[face][stone]
    rgba[(np.abs(x + 2) < 1.2) & (np.abs(y + 2) < 1.2)] = (255, 255, 255, 255)


def primary_glow():
    """띠의 윤곽에서 멀어질수록 옅어지는 빛. 띠의 아무 점까지의 가장 가까운 거리를 잰다."""
    width, height = PRIMARY
    x, y, inside = ribbon_field(width + GLOW * 2, height + GLOW * 2)
    ribbon = inside >= GLOW * D
    edge = ribbon & ~(np.roll(ribbon, 1, 0) & np.roll(ribbon, -1, 0) & np.roll(ribbon, 1, 1) & np.roll(ribbon, -1, 1))
    ys, xs = np.nonzero(edge)
    distance = np.full(x.shape, 1e9)
    grid_y, grid_x = np.mgrid[0:x.shape[0], 0:x.shape[1]]
    for ey, ex in zip(ys[::3], xs[::3]):
        distance = np.minimum(distance, np.hypot(grid_x - ex, grid_y - ey))
    strength = np.where(ribbon, 1, np.clip(1 - distance / (GLOW * D), 0, 1) ** 2)
    rgba = np.zeros((*x.shape, 4), np.uint8)
    rgba[..., :3] = (255, 214, 130)
    rgba[..., 3] = (strength * 190).astype(np.uint8)
    return rgba


def shine():
    """비스듬한 빛줄기(40×36 가상 px)."""
    w, h = 40 * D, PRIMARY[1] * D
    y, x = np.mgrid[0:h, 0:w]
    along = x - w / 2 + (y - h / 2) * 0.55
    strength = np.maximum(np.exp(-(along / 7) ** 2), 0.6 * np.exp(-((along - 20) / 3) ** 2))
    rgba = np.full((h, w, 4), 255, np.uint8)
    rgba[..., 3] = (strength * 190).astype(np.uint8)
    return rgba


def bullet(full):
    n = 10 * D
    y, x = np.mgrid[0:n, 0:n]
    inside = n / 2 - (np.abs(x - (n - 1) / 2) + np.abs(y - (n - 1) / 2))
    rgba = np.zeros((n, n, 4), np.uint8)
    rgba[inside >= 0] = (*INK, 255)
    rgba[(inside >= 2) & (inside < 5)] = (*LANTERN, 255)
    rgba[inside >= 5] = (*LANTERN, 255) if full else (*INK, 255)
    if full:
        rgba[inside >= 7] = (255, 250, 236, 255)
    return rgba


def line():
    w = 110 * D
    x = np.arange(w)
    keep = (pg.bayer(2, w) < 1 - x / w).astype(np.uint8)
    rgba = np.zeros((2, w, 4), np.uint8)
    rgba[..., :3] = LANTERN
    rgba[..., 3] = keep * 190
    return rgba


def main(root):
    title = root / "Assets/MiniPandoras/Art/PixelGen/UI/Title"
    title.mkdir(parents=True, exist_ok=True)
    pictures = {
        "plate_shade.png": plate_shade(), "side_shade.png": side_shade(), "primary.png": ribbon(PRIMARY, 13), "primary_small.png": ribbon(PRIMARY_SMALL, 10), "primary_glow.png": primary_glow(), "shine.png": shine(),
        "bullet.png": bullet(False), "bullet_full.png": bullet(True), "line.png": line(),
    }
    for name, rgba in pictures.items():
        Image.fromarray(rgba.astype(np.uint8), "RGBA").save(title / name)
        print((title / name).relative_to(root).as_posix(), rgba.shape[1], "x", rgba.shape[0])


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
