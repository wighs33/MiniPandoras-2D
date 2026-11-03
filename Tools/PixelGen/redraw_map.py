"""웹게임 맵 한 장의 바닥과 나무를 코드로 다시 그린다. 맵 배치·충돌·지점은 웹게임 그대로이고 그림만 바뀐다.

사용: python Tools/PixelGen/redraw_map.py <프로젝트 루트> <맵 id …>
  읽음: Assets/MiniPandoras/Art/WebGame/Maps/<id>/tiles.json   (Tools/WebGame/export_tiles.mjs)
  씀:   Assets/MiniPandoras/Art/PixelGen/Maps/<id>/ground.png   바닥 한 장(한 칸 64px, 웹게임 ground.png와 같은 크기)
        Assets/MiniPandoras/Art/PixelGen/Props/<소품>_<n>.png   나무 여러 벌(웹게임 소품과 같은 크기·기준점)
        Assets/MiniPandoras/Art/PixelGen/Props/props.json       소품마다 벌 수와 기준점
        Assets/MiniPandoras/Art/PixelGen/Backdrops/<배경>.png   맵 밖 배경(26×15칸, 카메라를 따라다닌다)
        Assets/MiniPandoras/Art/PixelGen/Backdrops/void_floor.png 프롤로그 눈을 뜨는 장면의 바닥(차원공간 돌섬을 누운 눈높이에서 본 모습)
        Assets/MiniPandoras/Art/PixelGen/Maps/<id>/uprights.json, <이름>.png  맵에 세우는 것(집·상자·통·우물·수반·울타리).
              웹게임 uprights와 같은 형식이다: 왼쪽·위(그림 px), 발밑(논리 px).
        Assets/MiniPandoras/Art/PixelGen/Maps/<id>/trees.json  숲을 다시 심은 맵의 나무(웹게임 나무 대신 쓴다, REPLANT)
맵 들여오기(MapImporter)는 다시 그린 바닥이 있는 맵에 이 그림을 쓴다.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import backdrop
import forest
import objects
import pixelgen as pg
import props
import terrain
import trees

# 소품마다 몇 벌을 그릴지와 그리는 함수. 크기·기준점은 웹게임 소품(논리 px)과 같게 해 맵 배치가 그대로 맞는다.
PROPS = {
    "tree": (props.tree, 4, (70, 88), (35, 87.5)),
    "pine": (props.pine, 4, (56, 96), (28, 95.5)),
    # 숲을 여러 종류로 꾸미는 나무(사용자 요청 2026-11-03, trees.py). 새로 짠 맵(layout.py)이 심는다.
    "oak": (trees.oak, 3, (84, 96), (42, 95.5)),
    "maple": (trees.maple, 3, (64, 84), (32, 83.5)),
    "birch": (trees.birch, 3, (48, 92), (24, 91.5)),
    "spruce": (trees.spruce, 3, (48, 112), (24, 111.5)),
    "sapling": (trees.sapling, 3, (30, 44), (15, 43.5)),
    # 사막 마을(8장 힐리아 서부, west_sands.py)
    "palm": (trees.palm, 3, (60, 88), (30, 87.5)),
    "cactus": (trees.cactus, 3, (24, 40), (12, 39.5)),
    "lamp": (objects.lamp, 1, (22, 58), (11, 57.5)),
}
DRAWERS = {"outdoor": terrain.draw_outdoor, "void": terrain.draw_void}
BACKDROPS = {"void": backdrop.void}
# 웹게임 나무 대신 숲 구역을 다시 심는 맵(사용자 요청 2026-11-03)과 그 무리 비율. 정원은 침엽수 없이, 모래땅은 활엽수만.
GARDEN = (forest.BIRCHES, forest.BIRCHES, forest.BROADLEAF)
REPLANT = {
    "airosRoad": None, "caveRoad": None, "forest": None, "outskirts": None, "port": None, "station": None,
    "fairyGarden": GARDEN, "fairyNight": GARDEN, "hiddenGarden": GARDEN,
    "westSands": (forest.BROADLEAF, forest.BROADLEAF, forest.BROADLEAF),
}
# 같은 곳의 낮·밤 맵은 같은 숲이어야 한다: 심는 씨앗을 맞춘다.
PLANT_KEY = {"fairyNight": "fairyGarden"}
# 맵 바닥을 그 칸에 누운 눈높이에서 본 그림(눈을 뜨는 장면): 맵 id → (그림 이름, 눈의 칸 자리)
EYE_LEVEL = {"void": ("void_floor", (10.5, 6.6))}


def save(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    mode = "RGBA" if image.shape[2] == 4 else "RGB"
    Image.fromarray(pg.upscale(image), mode).save(path)


def redraw_props(out):
    entries = []
    for prop_id, (draw, variants, size, anchor) in PROPS.items():
        width, height = size[0] * 2, size[1] * 2
        for n in range(variants):
            sprite, _ = draw(np.random.default_rng(1000 + n * 17 + len(prop_id)), width, height)
            save(sprite, out / f"{prop_id}_{n}.png")
        entries.append({"id": prop_id, "variants": variants, "width": size[0], "height": size[1], "anchorX": anchor[0], "anchorY": anchor[1]})
    (out / "props.json").write_text(json.dumps({"props": entries}, indent=1), encoding="utf-8")


class Grove:
    """forest.plant가 쓰는 맵 자리: 칸 종류(숲 바닥을 칠한다)와 심은 나무. 다시 그린 맵의 충돌은 웹게임 그대로라 막는 칸을 적지 않는다."""

    def __init__(self, kinds):
        self.kinds = kinds
        self.height, self.width = kinds.shape
        self.props = []

    def tree(self, x, y, kind):
        self.props.append({"id": kind, "x": x, "y": y})


def replant(map_id, tiles, data, folder):
    """
    웹게임 나무 대신 숲 구역을 다시 심는다: 웹게임 충돌에서 통째로 막힌 풀밭 칸(웹게임 숲)에 forest.plant로 여러 종류를 섞어 심고
    그 칸 바닥을 숲 그늘로 바꾼다. 큰 수관이 숲 사이 좁은 길(forest.lanes)을 덮을 자리에는 어린 나무를 드문드문 심고
    나머지는 덤불로 채워 숲 사이 길이 묻히지 않으면서 막힌 곳임이 보이게 한다.
    심은 나무는 그 맵 폴더의 trees.json에 적는다(맵 들여오기가 웹게임 나무 대신 쓴다).
    돌려주는 값: 숲 그늘을 칠한 칸 종류.
    """
    kinds = np.array(tiles, dtype=object)
    per, height, width = data["collisionPerTile"], data["height"], data["width"]
    solid = np.array([[v == "1" for v in row] for row in data["collision"]]).reshape(height, per, width, per)
    region = solid.all(axis=(1, 3)) & (kinds == "grass")
    low = region & forest.canopy_over(forest.lanes(~solid.any(axis=(1, 3))), side=1, rows=3)
    grove = Grove(kinds)
    mixes = REPLANT[map_id] or (forest.CONIFERS, forest.BIRCHES, forest.BROADLEAF)
    rng = np.random.default_rng(sum(map(ord, PLANT_KEY.get(map_id, map_id))))
    trunks = {(x, y) for x, y, _ in forest.plant(grove, region, rng, mixes, low=low)}
    for y, x in zip(*np.nonzero(low)):
        if (x, y) not in trunks and rng.random() < 0.6:
            grove.kinds[y, x] = "bush"
    folder.mkdir(parents=True, exist_ok=True)
    (folder / "trees.json").write_text(json.dumps({"props": grove.props}, indent=1), encoding="utf-8")
    return grove.kinds.tolist()


def stand_objects(tiles, map_id, folder):
    """맵에 세울 것을 그려 그 맵 폴더에 두고 자리를 uprights.json에 적는다. 같은 그림(울타리 모양)은 한 번만 쓴다."""
    entries, written = [], set()
    for name, image, left, top, foot in objects.place(tiles, seed=sum(map(ord, map_id)) + 7):
        if name not in written:
            save(image, folder / f"{name}.png")
            written.add(name)
        entries.append({"image": name, "left": int(left * pg.SCALE), "top": int(top * pg.SCALE), "foot": foot / 2})
    (folder / "uprights.json").write_text(json.dumps({"uprights": entries}, indent=1), encoding="utf-8")


def main(project, map_ids):
    art = project / "Assets/MiniPandoras/Art"
    redraw_props(art / "PixelGen/Props")
    for name, draw in BACKDROPS.items():
        save(draw(), art / f"PixelGen/Backdrops/{name}.png")
    for map_id in map_ids:
        source = art / f"WebGame/Maps/{map_id}"
        tiles = json.loads((source / "tiles.json").read_text(encoding="utf-8"))
        data = json.loads((source / "map.json").read_text(encoding="utf-8"))
        theme = data["theme"]
        kinds = tiles["tiles"]
        if map_id in REPLANT:
            kinds = replant(map_id, kinds, data, art / f"PixelGen/Maps/{map_id}")
        ground = DRAWERS[theme](kinds, seed=sum(map(ord, map_id)))
        save(ground, art / f"PixelGen/Maps/{map_id}/ground.png")
        stand_objects(tiles["tiles"], map_id, art / f"PixelGen/Maps/{map_id}")
        if map_id in EYE_LEVEL:
            name, eye = EYE_LEVEL[map_id]
            save(backdrop.eye_level_floor(ground, eye), art / f"PixelGen/Backdrops/{name}.png")
        print(map_id, ground.shape[1] * pg.SCALE, "x", ground.shape[0] * pg.SCALE)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), sys.argv[2:])
