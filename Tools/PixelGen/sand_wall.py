"""9장 라르고의 모래벽 한 칸(웹게임 raiseSandWall의 SandWallSegment 자리).

웹게임은 칸마다 모랫빛 네모를 그린다. 여기서는 위에서 비스듬히 본 다진 모래 벽 한 칸을 그린다:
위쪽 한 칸은 벽 윗면(바람 물결과 알갱이, 양옆이 부스러진 가장자리), 그 아래는 남쪽 면(가로 지층과 박힌 돌, 아래로 흘러내린 모래 자락).
남북으로 늘어선 칸을 북쪽부터 겹쳐 세우면 윗면은 이어진 한 줄이 되고 남쪽 끝 칸의 면만 보인다. 윗면 무늬는 한 칸(32점)마다 되풀이해 이음매가 없다.
게임(StageEffects.RaiseSandWall)은 그림의 위에서부터 솟은 높이만큼만 잘라 땅 높이에 세운다(벽이 땅에서 솟아오른다).

  Assets/MiniPandoras/Art/PixelGen/Stage/sand_wall.png   기준점은 아래 가운데(벽이 땅에 닿는 줄)

사용: python Tools/PixelGen/sand_wall.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg

TOP = pg.TILE                       # 윗면 높이(점) = 한 칸
FACE = 72                           # 남쪽 면 높이(점): 사람 키(약 2.4칸)에 가깝게
W, H = 44, TOP + FACE              # 폭은 한 칸보다 조금 넓게(이웃 칸에 조금 걸친다)
SANDS = pg.hexes("#5e4426", "#7c5c34", "#9a7646", "#b8925a", "#d2ae72", "#e8c88e", "#f6e0aa")
STONE = pg.hexes("#5a4c3c", "#8a7a64")
OUTLINE = np.array([34, 20, 10], dtype=np.uint8)


def build():
    rng = np.random.default_rng(29)
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    image = np.zeros((H, W, 4), dtype=np.uint8)
    # 양옆 가장자리: 줄마다 1~2점씩 들쭉날쭉 부스러졌다(윗면은 한 칸마다 되풀이).
    crumble = np.array([rng.integers(0, 3) for _ in range(H)])
    crumble[:TOP] = np.array([1, 0, 0, 1, 2, 1, 0, 0] * (TOP // 8))
    right = np.array([rng.integers(0, 3) for _ in range(H)])
    right[:TOP] = np.array([0, 1, 1, 0, 0, 2, 1, 0] * (TOP // 8))
    body = (xx >= crumble[:, None]) & (xx < W - right[:, None])

    # 윗면: 가운데가 조금 솟아 밝고, 바람 물결이 비스듬히 지나간다. 한 칸마다 이어지게 사인 무늬만 쓴다.
    top = body & (yy < TOP)
    u = (xx - W / 2) / (W / 2)
    ripple = np.sin((yy * 2 * np.pi / TOP) * 2 + xx * 0.45)
    light = 0.78 - 0.2 * u * u - 0.1 * u + 0.12 * (ripple > 0.55) - 0.1 * (ripple < -0.6)
    image[top, :3] = pg.quantize(np.clip(light, 0, 0.999), SANDS[3:], dither=0.12)[top]
    # 윗면 양옆 가장자리 한 점은 그늘(벽 두께가 보이게).
    rim = top & ((xx == crumble[:, None]) | (xx == W - right[:, None] - 1))
    image[rim, :3] = SANDS[2]
    grains = top & (np.mod(xx * 7 + yy * 13, 29) == 0)
    image[grains, :3] = SANDS[6]

    # 남쪽 면: 그늘진 다진 모래에 가로 지층, 위 가장자리는 밝은 테, 아래는 흘러내린 자락이 넓게 퍼진다.
    face = body & (yy >= TOP)
    v = (yy - TOP) / FACE
    strata = np.floor((yy - TOP + np.sin(xx * 0.4) * 1.5) / 9) % 2
    # 회상 색(흑백에 가깝다) 속에서도 나무 갑판과 갈리게 윗면은 밝게, 남쪽 면은 짙게 둔다.
    face_light = 0.36 - 0.2 * v + 0.07 * strata - 0.08 * np.abs(u) + pg.noise(H, W, 4, rng) * 0.1
    image[face, :3] = pg.quantize(np.clip(face_light, 0, 0.999), SANDS[:5], dither=0.15)[face]
    lip = face & (yy < TOP + 3)
    image[lip, :3] = SANDS[4]
    image[face & (yy == TOP + 3), :3] = SANDS[1]
    for _ in range(5):
        sx, sy = rng.integers(4, W - 6), rng.integers(TOP + 10, H - 10)
        stone = (np.hypot((xx - sx) / 2.6, (yy - sy) / 1.8) <= 1) & face
        image[stone, :3] = STONE[1]
        image[stone & (yy > sy), :3] = STONE[0]
    image[body, 3] = 255
    out = pg.outline(image, OUTLINE)
    # 윗면 위아래 끝의 테는 지운다(이웃 칸과 이어진다). 맨 북쪽 칸의 위 끝은 게임이 따로 덮지 않는다.
    out[0, :, 3] = np.where(body[0], 255, 0)
    return out


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(build()), "RGBA").save(out / "sand_wall.png")
    print(out / "sand_wall.png")


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
