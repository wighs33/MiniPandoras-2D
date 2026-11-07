"""소품이 땅에 드리우는 흐린 그림자를 맵(모습)마다 한 장으로 굽는다.

사용: python Tools/PixelGen/map_shadows.py <프로젝트 루트> [맵 id ...]
  Assets/MiniPandoras/Art/PixelGen/Maps/<id>/cast_shadow.png          모습이 없는 맵
  Assets/MiniPandoras/Art/PixelGen/Maps/<id>/cast_shadow_<모습>.png   모습마다 한 장
  Assets/MiniPandoras/Art/UI/Shadow.png                               인물 발밑의 옅은 타원
맵 id를 주지 않으면 모든 맵을 굽는다. 소품 배치가 바뀌면 다시 돌리고 Unity에서 MiniPandoras/Import Maps를 돌린다.

왜 소품마다 그림자를 달지 않고 한 장으로 굽는가:
  - 숲처럼 그림자끼리 겹치는 곳이 얼룩지지 않는다(겹치면 진한 쪽만 남는다).
  - 나무 수백 그루가 있어도 게임에서는 그림 한 장이라 프레임이 떨어지지 않는다.
  - 흐린 그림이라 맵 그림의 1/8 크기로 충분하다.
"""
import sys
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

import preview_map

MAPS = "Assets/MiniPandoras/Art/PixelGen/Maps"
FOOT_SHADOW = "Assets/MiniPandoras/Art/UI/Shadow.png"

SHRINK = 8              # 맵 그림의 몇 분의 1로 구울지(MapImporter.CastShadowShrink와 같다)
WORK_SHRINK = 2         # 흐리기 전에 그리는 크기. 그림 px마다 그리면 큰 맵에서 오래 걸린다
BLUR = 7                # 흐린 정도(맵 그림 px)
SHADE = (16, 14, 40)    # 그림자 색. 새까만 것보다 푸른 기가 돌아야 땅색과 어울린다
STRENGTH = 0.42         # 가장 진한 곳이 눈에 보이는 진하기
LINEAR_GAMMA = 2.2      # 게임은 선형 색공간에서 섞어서, 같은 진하기로 보이려면 불투명도를 더 높여야 한다
FADE = 0.45             # 그림자 끝이 발치보다 얼마나 옅어지는지
SOLID_ALPHA = 127       # 이보다 투명한 점(그림에 함께 그린 발밑 그림자)은 그림자를 드리우지 않는다
TALLEST = 340           # 이보다 높은 곳(집 지붕)은 그림자를 더 늘이지 않는다(맵 그림 px)


@dataclass(frozen=True)
class Light:
    """세운 것의 높이 1이 땅에서 얼마나 내려오고(squash) 오른쪽으로 가는지(lean)."""
    squash: float
    lean: float


SUN = Light(squash=0.28, lean=0.22)     # 인물 그림자도 이 방향이다(CharacterShadow 값)
LAMP = Light(squash=0.14, lean=0.08)    # 천장 등불: 지붕 아래에서는 짧게 진다
ROOFED_THEMES = ("interior", "cave")

# 땅이나 물에 누운 것, 머리 위에 걸린 것, 벽에 붙은 것은 세운 물건이 아니라서 그림자를 드리우지 않는다.
NO_SHADOW = ("skiff", "boat", "ship_", "nets", "glow_patch", "wall_shrooms", "wall_glow", "bunting", "fish",
             "doormat", "wall_north", "wall_west", "wall_east", "side_wall", "gate")


def casts_shadow(name):
    return not any(word in name for word in NO_SHADOW)


def cast(canvas, sprite, foot, left, top, light):
    """그림을 발 줄을 축으로 땅에 눕혀 canvas에 넣는다. 겹치면 진한 쪽만 남는다."""
    rows, columns = np.nonzero(np.array(sprite)[..., 3] > SOLID_ALPHA)
    foot_row = foot * preview_map.ART_SCALE
    heights = np.minimum(foot_row - (top + rows), TALLEST)
    above_ground = heights >= 0
    if not above_ground.any():
        return

    heights, columns = heights[above_ground], columns[above_ground]
    y = ((foot_row + heights * light.squash) / WORK_SHRINK).astype(int)
    x = ((left + columns + heights * light.lean) / WORK_SHRINK).astype(int)
    inside = (x >= 0) & (x < canvas.shape[1]) & (y >= 0) & (y < canvas.shape[0])
    darkness = 1 - FADE * heights / max(heights.max(), 1)
    np.maximum.at(canvas, (y[inside], x[inside]), darkness[inside])


def bake(project, folder, look, light):
    """이 모습의 그림자 한 장. 드리울 것이 없으면 None이다."""
    ground = Image.open(folder / "ground.png")
    canvas = np.zeros((ground.height // WORK_SHRINK, ground.width // WORK_SHRINK), np.float32)
    for foot, sprite, left, top, name in preview_map.placed(project, folder, look):
        if casts_shadow(name):
            cast(canvas, sprite, foot, left, top, light)

    if not canvas.any():
        return None

    solid = Image.fromarray((canvas * 255).astype(np.uint8))
    # 눕히면서 벌어진 점 사이를 메운 뒤 흐린다.
    soft = solid.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(BLUR / WORK_SHRINK))
    small = soft.resize((ground.width // SHRINK, ground.height // SHRINK), Image.BOX)
    alpha = 255 * (1 - (1 - np.array(small, np.float32) / 255 * STRENGTH) ** LINEAR_GAMMA)
    image = np.empty((*alpha.shape, 4), np.uint8)
    image[..., :3] = SHADE
    image[..., 3] = alpha.astype(np.uint8)
    return Image.fromarray(image)


def bake_map(project, folder):
    data = preview_map.map_data(project, folder)
    light = LAMP if data.get("theme") in ROOFED_THEMES else SUN
    looks = [look["name"] for look in data.get("looks", [])]
    for look in looks or [None]:
        image = bake(project, folder, look, light)
        if image is not None:
            image.save(folder / (f"cast_shadow_{look}.png" if look else "cast_shadow.png"))


def foot_shadow(project):
    """인물 발밑의 옅은 타원(64×16). 가장자리가 0까지 부드럽게 사라진다."""
    y, x = np.mgrid[0:16, 0:64].astype(np.float32)
    distance = np.hypot((x + 0.5 - 32) / 32, (y + 0.5 - 8) / 8)
    fade = np.clip(1 - distance, 0, 1)
    image = np.empty((16, 64, 4), np.uint8)
    image[..., :3] = (10, 8, 20)
    image[..., 3] = (fade * fade * (3 - 2 * fade) * 255).astype(np.uint8)
    Image.fromarray(image).save(project / FOOT_SHADOW)


def main(project, map_ids):
    folders = [project / MAPS / map_id for map_id in map_ids] or sorted(path for path in (project / MAPS).iterdir() if path.is_dir())
    for folder in folders:
        if (folder / "ground.png").exists():
            bake_map(project, folder)
            print(folder.name)

    foot_shadow(project)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), sys.argv[2:])
