"""1:1 대전의 원형 투기장(arena)을 그린다(2026-11-13 사용자 결정: 대전 맵은 새 원형 투기장).

숲속 빈터에 돌을 깐 둥근 판이다. 판 가운데에 판도라의 마법진을 금빛으로 새겼고, 둘레에 등불과 깃발 기둥을 세웠다.
장애물 없이 공정한 판이라 두 사람은 판 안에서만 움직인다(판 밖은 막힌다). 지점은 왼쪽 player, 오른쪽 rival이다.
이야기 맵이 아니라서 드나드는 길이 없다.

사용: python Tools/PixelGen/arena.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/arena/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/arena_preview.png
"""
import sys
from pathlib import Path

import numpy as np

import forest
import pixelgen as pg
import terrain
from houses import Canvas, outline
from layout import MapLayout

T = pg.TILE
WIDTH, HEIGHT = 30, 20
CX, CY = 15.0, 10.0              # 판 가운데(칸)
RADIUS = 7.6                     # 돌판 반지름(칸)
PLAYER = (9, 10)
RIVAL = (20, 10)

TIMBER = pg.hexes("#2b1d16", "#4a3324", "#6b4a33", "#8c6646")
PENNANTS = (pg.hexes("#5a3a12", "#b8862e", "#e4bc5c"), pg.hexes("#2a2450", "#4e3f9a", "#8f7cff"))
CARVING = (232, 196, 120)        # 마법진 새김 빛(금빛)


def pennant(colors):
    """깃발 기둥: 나무 기둥 꼭대기에 갈라진 끝의 세로 깃발."""
    w, h = 30, 96
    c = Canvas(w, h)
    pole, ground = 8, h - 2
    c.paint(c.rect(pole - 2, 8, pole + 2, ground), TIMBER[2])
    c.paint(c.rect(pole - 2, 8, pole - 1, ground), TIMBER[3])
    c.paint(c.ellipse(pole, 7, 3.5, 3.5), colors[2])
    flag = c.rect(pole + 2, 12, pole + 18, 50) & ~c.polygon([(pole + 6, 51), (pole + 10, 42), (pole + 14, 51)])
    c.paint(flag, colors[1])
    c.paint(flag & (c.xx > pole + 14), colors[0])
    c.paint(c.rect(pole + 2, 12, pole + 18, 14), colors[2])
    c.paint(c.ellipse(pole + 10, 26, 3.5, 3.5) & flag, colors[2])
    return outline(c.image), (pole, ground)


def stone_floor(rng):
    """판(바닥에 까는 그림): 매끈한 원 안에 자갈돌을 깔고, 가장자리에 굵은 경계석을 두르고, 가운데에 판도라의 마법진을 금빛으로 새겼다."""
    radius = RADIUS * T
    size = int(np.ceil(radius * 2)) + 4
    middle = size / 2
    yy, xx = np.mgrid[0:size, 0:size]
    distance = np.hypot(xx + 0.5 - middle, yy + 0.5 - middle)
    image = np.zeros((size, size, 4), dtype=np.uint8)
    inside = distance <= radius
    image[..., :3] = terrain.cobble(size, size, rng)
    image[..., 3] = np.where(inside, 255, 0)
    # 경계석: 바깥 6점은 어두운 돌, 그 안쪽 한 줄은 빛을 받는 윗면.
    curb = inside & (distance > radius - 6)
    image[curb, :3] = pg.STONE[1]
    image[inside & (distance > radius - 7) & (distance <= radius - 6), :3] = pg.STONE[4]
    image[inside & (distance > radius - 1.5), :3] = pg.STONE[0]
    lines = terrain.magic_circle(size, size, middle, middle, radius - 2.4 * T, 1.0)
    glow = lines & inside
    image[glow, :3] = (0.45 * image[glow, :3] + 0.55 * np.array(CARVING)).astype(np.uint8)
    return image, (CX * T - middle, CY * T - middle)


def build():
    m = MapLayout("arena", "판도라 투기장", WIDTH, HEIGHT, seed=431)
    floor = np.hypot(m.cx + 0.5 - CX, m.cy + 0.5 - CY) <= RADIUS
    ring = np.hypot(m.cx + 0.5 - CX, m.cy + 0.5 - CY) <= RADIUS + 1.6
    m.kinds[ring] = "path"
    image, (left, top) = stone_floor(np.random.default_rng(433))
    m.decal("arena_floor", image, left, top)

    # 둘레: 대각선 네 곳에 등불, 위아래에 깃발 기둥 넷(금빛·보랏빛을 번갈아).
    for angle in (45, 135, 225, 315):
        x = CX + np.cos(np.radians(angle)) * (RADIUS + 0.9) - 0.5
        y = CY + np.sin(np.radians(angle)) * (RADIUS + 0.9) - 0.5
        m.lamp(int(round(x)), int(round(y)))
    for i, angle in enumerate((-110, -70, 70, 110)):
        x = CX + np.cos(np.radians(angle)) * (RADIUS + 0.8)
        y = CY + np.sin(np.radians(angle)) * (RADIUS + 0.8)
        m.stand(f"pennant_{i}", pennant(PENNANTS[i % 2]), x, y + 0.5, block=(0.6, 0.4))

    woods = ~ring
    forest.plant(m, woods & ~forest.canopy_over(ring, side=2, rows=5), np.random.default_rng(432))
    m.block_where(~floor)
    m.border()

    m.point("player", PLAYER)
    m.point("rival", RIVAL)
    return m


def main(project):
    m = build()
    if not m.reachable(PLAYER, [RIVAL]):
        print("player 칸에서 rival 칸에 닿지 못한다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
