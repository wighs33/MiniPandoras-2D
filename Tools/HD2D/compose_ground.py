"""HD-2D 시험 장면의 땅 그림을 PixelLab 풀·흙길 왕(Wang) 타일로 짠다(Editor/Prototypes/HD2DTestBuilder).

칸의 네 모서리 점마다 풀(upper)인지 흙길(lower)인지 정하고, 네 모서리가 맞는 타일을 골라 붙인다.
흙길은 화면 아래에서 위로 굽어 올라가는 길과 왼쪽으로 갈라지는 샛길이다. 1칸 = 32도트 = 1m.

사용: python Tools/HD2D/compose_ground.py <Unity 프로젝트 루트>
  Assets/MiniPandoras/Art/HD2DTest/Ground/ground.png          땅 그림
  Assets/MiniPandoras/Art/HD2DTest/Ground/ground_layout.json  칸마다 풀(1)·흙길(0), 아래(남)에서 위(북)로
"""
import json
import math
import random
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
SHEET = HERE / "pixellab" / "meadow_path_sheet.png"
META = HERE / "pixellab" / "meadow_path_meta.json"
WIDTH, HEIGHT = 24, 18     # 칸
TILE = 32
PATH_HALF_WIDTH = 0.9      # 길 가운데에서 흙인 거리(칸). 참고 게임처럼 좁게, 단 0.7이면 모서리 점이 빠져 길이 끊긴다

# PixelLab 타일의 풀은 푸른 초록, 흙은 짙은 노랑이라 참고 화면(노란 연두 풀밭, 잿빛 베이지 흙길)과 색이 멀다.
# 타일의 무늬는 두고 색만 참고 화면을 밝기 순으로 뽑은 색으로 바꾼다. 풀은 그 위에 잔디 포기(draw_lawn.py)가 덮여 밑색 노릇만 한다.
REFERENCE_COLOURS = {
    # 풀: 어두운 것부터
    "#164439": "#2f4415", "#1d5e44": "#3f5a1c", "#29743e": "#55722a", "#2d7e3d": "#607d29",
    "#378841": "#6a8630", "#409244": "#729035", "#4e9e47": "#7e9838", "#5daa4c": "#89a140",
    # 타일마다 같은 자리에 박힌 밝은 점은 바둑판처럼 되풀이돼 보여서 풀색으로 지운다(꽃은 잔디 포기가 흩뿌린다).
    "#86b15b": "#9bae4d", "#eaefa5": "#7e9838",
    # 흙길: 밝은 것부터
    "#f7bd47": "#e6d6ae", "#e7ce8d": "#cec09e", "#dcb473": "#c2b495", "#d0a365": "#afa388",
    "#c4925b": "#a89a80", "#b07f51": "#978b74", "#a46f4b": "#878574", "#86573d": "#6e6a5c",
}


def path_centre_x(z):
    """아래(z=0)에서 위로 올라가며 오른쪽으로 굽는 큰길의 가운데 x."""
    return 11.5 + 3.2 * math.sin(z / 6.0) + z * 0.28


def is_dirt(x, z):
    """모서리 점 (x, z)가 흙길인지. z는 아래에서 위로 센다."""
    if abs(x - path_centre_x(z)) < PATH_HALF_WIDTH:
        return True
    # 왼쪽으로 갈라지는 샛길: z 7 부근에서 왼쪽 끝으로 완만하게 내려간다.
    branch_z = 7.4 - (11.5 - x) * 0.18
    return x < path_centre_x(7.4) and abs(z - branch_z) < 0.8


def recolour(image):
    def rgb(hex_colour):
        return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))

    table = {rgb(old): rgb(new) for old, new in REFERENCE_COLOURS.items()}
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if (r, g, b) in table:
                pixels[x, y] = table[(r, g, b)] + (a,)


def main(project):
    meta = json.loads(META.read_text(encoding="utf-8"))
    sheet = Image.open(SHEET).convert("RGBA")
    tiles = {}
    for tile in meta["tileset_data"]["tiles"]:
        c = tile["corners"]
        key = tuple(c[k] == "upper" for k in ("NW", "NE", "SW", "SE"))
        box = tile["bounding_box"]
        tiles.setdefault(key, []).append(sheet.crop((box["x"], box["y"], box["x"] + box["width"], box["y"] + box["height"])))

    random.seed(5)
    grass = [[not is_dirt(x, z) for x in range(WIDTH + 1)] for z in range(HEIGHT + 1)]
    image = Image.new("RGBA", (WIDTH * TILE, HEIGHT * TILE))
    cells = []
    for z in range(HEIGHT):
        for x in range(WIDTH):
            # 그림은 위가 북쪽이라 z가 큰 칸을 위에 그린다. 모서리는 북서·북동·남서·남동 순서.
            key = (grass[z + 1][x], grass[z + 1][x + 1], grass[z][x], grass[z][x + 1])
            tile = random.choice(tiles[key])
            image.paste(tile, (x * TILE, (HEIGHT - 1 - z) * TILE))
            cells.append(1 if all(key) else 0)

    recolour(image)
    out = Path(project) / "Assets/MiniPandoras/Art/HD2DTest/Ground"
    out.mkdir(parents=True, exist_ok=True)
    image.save(out / "ground.png")
    (out / "ground_layout.json").write_text(json.dumps({"width": WIDTH, "height": HEIGHT, "grass": cells}), encoding="utf-8")
    print(out / "ground.png", image.size)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else ".")
