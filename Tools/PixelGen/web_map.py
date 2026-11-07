"""웹게임 맵의 출구만 바꾼다. 다시 그린 바닥·나무·세운 것은 그대로 두고 map.json만 만든다.

맵 들여오기(MapImporter)는 Art/PixelGen/Maps/<id>/map.json이 있으면 웹게임 map.json 대신 그것만 읽으므로,
다시 그린 나무(trees.json)와 세운 것(uprights.json)을 함께 넣어 둔다.
"""
import json

WEB_TREES = ("tree", "pine")


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def open_exits(data, names):
    """지점 names의 칸을 충돌에서 뺀다."""
    per = data["collisionPerTile"]
    rows = [list(row) for row in data["collision"]]
    for point in data["points"]:
        if point["name"] in names:
            for cell in point["cells"]:
                for y in range(cell["y"] * per, (cell["y"] + 1) * per):
                    rows[y][cell["x"] * per:(cell["x"] + 1) * per] = "0" * per
    data["collision"] = ["".join(row) for row in rows]


def reroute(project, map_id, warps, points=None):
    """
    맵 map_id의 출구를 warps(point, map, target, dir)로 바꾼다. 없던 출구는 더하고, 있던 지점의 출구는 갈아 끼운다.
    points(이름 → 칸 목록)는 더하거나 바꿀 지점이다(돌아오는 도착 칸 …). 출구 칸은 막혀 있어도 뚫는다.
    """
    art = project / "Assets/MiniPandoras/Art"
    folder = art / f"PixelGen/Maps/{map_id}"
    data = read(art / f"WebGame/Maps/{map_id}/map.json")
    assert not data["gates"]
    data["props"] = [prop for prop in data["props"] if prop["id"] not in WEB_TREES] + read(folder / "trees.json")["props"]
    data["uprights"] = read(folder / "uprights.json")["uprights"]
    replaced = {warp["point"] for warp in warps}
    data["warps"] = [warp for warp in data["warps"] if warp["point"] not in replaced] + list(warps)
    for name, cells in (points or {}).items():
        data["points"] = [point for point in data["points"] if point["name"] != name]
        data["points"].append({"name": name, "cells": [{"x": x, "y": y} for x, y in cells]})
    open_exits(data, replaced)
    (folder / "map.json").write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
