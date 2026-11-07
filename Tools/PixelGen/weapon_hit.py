"""무기로 맞혔을 때의 타격 이펙트. 웹게임에는 없다.

오른쪽(+x)으로 친 한 벌을 6장으로 그린다. 게임은 친 방향으로 돌리거나 뒤집어 맞은 적 앞에 겹친다.
- 0장: 번쩍. 흰 가운데에서 노랑·주황으로 바깥이 짙어지는 뾰족한 별, 짙은 테두리. 맞은 순간 전투가 몇 프레임 멈추는(히트스톱) 동안 이 장이 보인다.
- 1~2장: 별이 줄며 노래지고, 친 쪽으로 열린 충격 반달이 퍼지고, 앞쪽으로 속도선과 나무 부스러기가 튄다.
- 3~5장: 반달이 끊기며 옅어지고(디더), 부스러기가 떨어지며 줄고, 작은 반짝임이 남는다.
맵 그림과 같은 점 크기(한 칸 32점, 점 하나 = 2px)이고 64×64점 판의 가운데가 맞은 자리다.

  Assets/MiniPandoras/Art/PixelGen/Effects/bat_hit_<0~5>.png
  Docs/ArtUpgrade/PixelGen/bat_hit_preview.png

사용: python Tools/PixelGen/weapon_hit.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import Canvas

SIZE = 64                  # 판 한 변(점)
C = SIZE / 2               # 맞은 자리
FRAMES = 6
WHITE, PALE, YELLOW, GOLD, ORANGE = pg.hexes("#fffdf0", "#fff1a8", "#ffd34d", "#f0a030", "#d0601f")
EDGE = pg.hexes("#4a2012")[0]
# 별의 가시(각도°, 길이점): 친 쪽(0°)과 그 양옆이 길고, 뒤쪽은 짧다. 화면 아래가 +y다.
SPIKES = ((0, 23), (-38, 17), (38, 16), (-80, 13), (80, 14), (-130, 9), (130, 8), (180, 10))
SPIKE_WIDTH = 3.2          # 가시 밑동 반폭(점)
# 나무 부스러기(나는 각도°, 빠르기 점/장, 크기 점): 친 쪽 부채꼴로 튀고 둘은 뒤로 튄다.
CHIPS = ((-55, 6.0, 3), (-20, 7.5, 2), (8, 8.0, 3), (30, 6.5, 2), (62, 5.0, 2), (-150, 3.5, 2), (150, 3.0, 2))
CHIP_START = 5             # 부스러기가 처음 보이는 거리(점). 번쩍임 별 밑에서 나온다
GRAVITY = 1.1              # 장마다 아래로 더해지는 빠르기(점)


def star(c, scale, width_scale, core):
    """가시 별 모양: 가시마다 밑동이 넓은 삼각형과 가운데 원."""
    shape = c.ellipse(C, C, core, core)
    for degrees, length in SPIKES:
        a = np.radians(degrees)
        tip = (C + np.cos(a) * length * scale, C + np.sin(a) * length * scale)
        w = SPIKE_WIDTH * width_scale
        side = (-np.sin(a) * w, np.cos(a) * w)
        shape |= c.polygon([tip, (C + side[0], C + side[1]), (C - side[0], C - side[1])])
    return shape


def flash(scale, core, hot=True):
    """겹겹이 짙어지는 별(바깥 주황 → 금 → 노랑 → 흰 가운데)에 짙은 테두리를 두른 그림."""
    c = Canvas(SIZE, SIZE)
    c.paint(star(c, scale, 1.0, core + 1), ORANGE)
    c.paint(star(c, scale * 0.8, 0.72, core), GOLD)
    c.paint(star(c, scale * 0.58, 0.5, core * 0.8), YELLOW)
    if hot:
        c.paint(star(c, scale * 0.34, 0.32, core * 0.65) | c.ellipse(C, C, core * 0.7, core * 0.7), WHITE)
    return pg.outline(c.image, EDGE)


def shock(c, radius, thickness, gaps, fade):
    """충격 반달: 친 쪽(+x)으로 열린 반원 띠(위아래로 조금 눌림). gaps는 끊긴 자리 수, fade는 남길 점의 비율(순서 디더)."""
    d = np.hypot(c.xx + 0.5 - C, (c.yy + 0.5 - C) / 0.82)
    angle = np.degrees(np.arctan2(c.yy + 0.5 - C, c.xx + 0.5 - C))
    band = (d >= radius - thickness) & (d < radius) & (np.abs(angle) <= 100)
    if gaps:
        band &= ((angle + 100) * gaps / 200 + 0.15) % 1 > 0.25
    if fade < 1:
        band &= pg.bayer(SIZE, SIZE) < fade
    return band


def streaks(c, frame):
    """앞쪽(+x)으로 뻗는 속도선 셋: 장이 갈수록 바깥으로 밀리며 짧아진다."""
    mask = np.zeros(c.xx.shape, dtype=bool)
    for dy, start, length in ((-7, 13, 9), (1, 16, 11), (8, 12, 7)):
        x0 = C + start + frame * 4
        mask |= c.rect(x0, C + dy, x0 + max(2, length - frame * 3), C + dy + 1)
    return mask


def chip_positions(frame):
    """부스러기마다 그 장의 자리(장 0에는 아직 없다): 번쩍임 별 밑에서 나와 포물선으로 떨어진다."""
    t = frame - 0.5
    for degrees, speed, size in CHIPS:
        a = np.radians(degrees)
        reach = CHIP_START + speed * t
        yield C + np.cos(a) * reach, C + np.sin(a) * reach + 0.5 * GRAVITY * t * t, size


def over(c, layer):
    """layer의 불투명한 점을 캔버스 위에 얹는다."""
    shown = layer[..., 3] > 0
    c.image[shown] = layer[shown]


def draw_frame(frame):
    if frame == 0:
        return flash(1.0, 6)

    c = Canvas(SIZE, SIZE)
    # 충격 반달과 속도선(맨 아래).
    shocks = {1: (10, 3, 0, 1.0, PALE), 2: (13, 2, 0, 1.0, YELLOW), 3: (16, 2, 4, 0.7, GOLD), 4: (18, 1, 4, 0.4, GOLD)}
    if frame in shocks:
        radius, thickness, gaps, fade, color = shocks[frame]
        c.paint(shock(c, radius, thickness, gaps, fade), color)
    if frame in (1, 2):
        c.paint(streaks(c, frame - 1), PALE if frame == 1 else YELLOW)

    # 나무 부스러기: 나뭇결 두 빛깔에 짙은 테두리. 마지막 장에는 반만 남는다.
    chips = Canvas(SIZE, SIZE)
    for i, (x, y, size) in enumerate(chip_positions(frame)):
        if frame == FRAMES - 1 and i % 2:
            continue
        body = chips.rect(round(x), round(y), round(x) + size, round(y) + max(1, size - 1))
        chips.paint(body, pg.WOOD[3])
        chips.paint(body & (chips.yy == round(y)), pg.WOOD[4])
    over(c, pg.outline(chips.image, EDGE))

    # 별(맨 위): 1장은 조금 준 흰 별, 2장은 노란 작은 별. 3·4장은 반달 둘레의 반짝임 십자.
    if frame == 1:
        over(c, flash(0.78, 5))
    elif frame == 2:
        over(c, flash(0.5, 3.5, hot=False))
    elif frame in (3, 4):
        for x, y in ((C + 12, C - 10), (C + 16, C + 7), (C - 4, C - 12))[: 5 - frame]:
            cross = c.rect(x - 1, y, x + 2, y + 1) | c.rect(x, y - 1, x + 1, y + 2)
            c.paint(cross, WHITE if frame == 3 else PALE)
    return c.image


def preview(frames, project):
    """미리보기: 위 줄은 장마다 풀밭 바탕에, 아래 줄은 같은 장을 두 배로(점이 보이게)."""
    tile = 2 * pg.SCALE * SIZE
    sheet = Image.new("RGBA", (FRAMES * (tile + 8) + 8, tile + 16), (31, 51, 36, 255))
    for i, frame in enumerate(frames):
        big = Image.fromarray(np.repeat(np.repeat(pg.upscale(frame), 2, axis=0), 2, axis=1), "RGBA")
        sheet.alpha_composite(big, (8 + i * (tile + 8), 8))
    sheet.save(project / "Docs/ArtUpgrade/PixelGen/bat_hit_preview.png")


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Effects"
    out.mkdir(parents=True, exist_ok=True)
    frames = [draw_frame(i) for i in range(FRAMES)]
    for i, frame in enumerate(frames):
        Image.fromarray(pg.upscale(frame), "RGBA").save(out / f"bat_hit_{i}.png")
    preview(frames, project)
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
