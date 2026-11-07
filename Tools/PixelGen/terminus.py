"""뱀열차 종착역(station, 7장)의 집과 서쪽 끝을 고친다(집 모양이 개판이고, 레비아탄 쪽 길은 없앤다).

바닥·나무·승강장·지점은 웹게임 맵을 다시 그린 그대로이고 바뀌는 것은 둘이다.
  · 옛 그림 집 → 집 설계(houses.py)로 세운 역사와 돌을 깐 앞마당. 흙길이 마당에서 남쪽 길까지 이어진다.
    흙길을 덮던 큰 나무 둘은 어린 나무로 바꿨다.
  · 뱀길 서쪽 끝을 숲과 울타리로 막았다. 레비아탄의 바다는 이제 서쪽 부두의 문으로 간다(west_port.py). 동쪽 길(호빗 산길)은 그대로다.

사용: python Tools/PixelGen/terminus.py <프로젝트 루트>
  Assets/MiniPandoras/Art/PixelGen/Maps/station/…   맵 데이터와 그림(layout.py)
  Docs/ArtUpgrade/PixelGen/station_preview.png      미리보기
"""
import sys
from pathlib import Path

import numpy as np

import forest
import houses
import pixelgen as pg
import redraw_map
import station
import yard
from layout import SUB, MapLayout
from web_map import WEB_TREES, read

MAP_ID = "station"
OLD_HOUSE = (2, 8, 8, 12)       # 옛 집이 막던 칸(왼쪽, 위, 오른쪽, 아래)
WEST_END = 6                    # 이 열까지 뱀길이 끝나고 숲이다
ROAD_ROWS = (3, 8)              # 뱀길과 그 양쪽 울타리 줄
STATION_HALL = houses.Design("terminus_hall", 7, roof="cross", roof_material="copper", walls="brick", upper="brick", door="double", door_at=0.5,
                             shutters="green", arched_windows=True, lanterns=True, chimney_at=0.82, flowers=False, plot="cobble", seed=91)
HALL_X, HALL_GROUND = 5, 12
YARD_PATH = [(8, 13.2), (16.6, 13.2)]
OVER_PATH = {(11, 17), (14, 17)}        # 수관이 흙길을 덮는 큰 나무 자리: 어린 나무로 바꾼다
OVER_HALL = {(1, 12)}                   # 역사 벽을 가리는 나무 자리: 뺀다


def build(project):
    art = project / "Assets/MiniPandoras/Art"
    source = art / f"WebGame/Maps/{MAP_ID}"
    data = read(source / "map.json")
    tiles = read(source / "tiles.json")["tiles"]
    width, height = data["width"], data["height"]
    m = MapLayout(MAP_ID, data["name"], width, height, theme=data["theme"], music=data["music"], ambient=data["ambient"],
                  seed=sum(map(ord, MAP_ID)))

    # ── 웹게임 맵을 다시 그린 그대로 가져온다: 숲 바닥이 칠해진 칸 종류, 다시 심은 나무, 충돌 ──
    folder = art / f"PixelGen/Maps/{MAP_ID}"
    m.kinds[:] = np.array(redraw_map.replant(MAP_ID, tiles, data, folder), dtype=object)
    trees = [prop for prop in read(folder / "trees.json")["props"] if (prop["x"], prop["y"]) not in OVER_HALL]
    for tree in trees:
        if (tree["x"], tree["y"]) in OVER_PATH:
            tree["id"] = "sapling"
    m.props = [prop for prop in data["props"] if prop["id"] not in WEB_TREES] + trees
    m.solid[:] = np.array([[cell == "1" for cell in row] for row in data["collision"]], dtype=bool)
    source_tiles = np.array(tiles, dtype=object)

    # ── 옛 집과 세운 것을 바닥에서 지운다 ──
    m.kinds[np.isin(source_tiles, ("houseRoof", "houseWall", "window", "door"))] = "grass"
    m.kinds[np.isin(source_tiles, ("crate", "barrel"))] = "cobble"
    m.kinds[source_tiles == "fence"] = "grass"
    x0, y0, x1, y1 = OLD_HOUSE
    m.solid[y0 * SUB:y1 * SUB, x0 * SUB:x1 * SUB] = False
    m.kinds[np.isin(m.kinds, ("path",)) & (m.cy < 13) & (m.cx < WEST_END + 2)] = "grass"

    # ── 뱀길 서쪽 끝: 모래를 걷어 내고 숲을 심는다. 동쪽의 울타리가 뱀길을 가로막는다 ──
    road_top, road_bottom = ROAD_ROWS
    closed = (m.cx < WEST_END) & (m.cy > road_top) & (m.cy < road_bottom - 1)
    m.kinds[closed] = "grass"
    m.solid[road_top * SUB:(road_bottom - 1) * SUB, 0:WEST_END * SUB] = True
    forest.plant(m, closed, np.random.default_rng(93), low=closed & (m.cx > WEST_END - 2))
    fence_cells = [(int(x), int(y)) for y, x in zip(*np.nonzero(source_tiles == "fence"))
                   if not (x < WEST_END and y == road_top) and not (x < HALL_X + 4 and y == road_bottom - 1)]
    fence_cells += [(WEST_END, y) for y in range(road_top + 1, road_bottom - 1)]
    m.fence(fence_cells)
    for y in (4, 5, 6):
        m.stamp("bush", (WEST_END - 1, y))

    # ── 역사와 앞마당, 남쪽 길까지 이어지는 흙길 ──
    m.line("path", YARD_PATH, 1.8)
    m.house(STATION_HALL, HALL_X, HALL_GROUND)
    m.stand("terminus_clock", station.station_clock(np.random.default_rng(94)), 11.5, 8.9, block=(0.5, 0.3))
    m.stand("terminus_timetable", station.timetable(np.random.default_rng(95)), 16.0, 8.9, block=(1.2, 0.3))
    m.stand("terminus_bench", yard.bench(np.random.default_rng(96)), 19.4, 8.9, block=(1.8, 0.4))
    m.thing("crate", 13, 8)
    m.thing("barrel", 14, 8)
    m.border()
    m.keep_open(width - 1, road_top + 1, width, road_bottom - 1)
    m.keep_open(17, height - 1, 18, height)

    # ── 이야기 지점(웹게임과 같은 칸) ──
    for point in data["points"]:
        m.point(point["name"], *[(cell["x"], cell["y"]) for cell in point["cells"]])
    m.warps = list(data["warps"])
    m.lights += [dict(light) for light in data["lights"] if light["radius"] > 30]     # 승강장 가로등 빛
    return m


def main(project):
    m = build(project)
    start = m.points["arrive"][0]
    for name in ("track", "hobbit", "road"):
        for cell in m.points[name]:
            if not m.reachable(start, [cell]):
                print(f"arrive에서 {name} {cell}에 닿지 못한다")
    if not m.reachable(start, [(m.width - 1, 5)]):
        print("동쪽 길 끝에 닿지 못한다")
    if m.reachable(start, [(0, 5)]):
        print("서쪽 끝이 아직 뚫려 있다")
    print(m.publish(project))


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
