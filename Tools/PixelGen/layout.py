"""맵을 웹게임 배치 없이 새로 짜는 도구(맵은 웹게임 방식을 따를 필요가 없다).

맵 한 장은 칸 격자에 바닥 종류를 칠하고(길·광장·개울 …), 집·나무·가로등·상자를 칸 자리에 세우고, 이야기가 쓰는 지점과
포탈을 적어서 만든다. 저장하면 웹게임 맵과 같은 형식의 map.json과 바닥 그림이 생기고, 맵 들여오기(MapImporter)는
이 map.json이 있는 맵을 웹게임 맵 대신 쓴다. 지점 이름만 웹게임 맵과 같으면 이야기 코드는 그대로 돈다.

  Assets/MiniPandoras/Art/PixelGen/Maps/<id>/map.json     맵 데이터(웹게임 형식 + decals: 집터처럼 바닥 위에 까는 그림)
  Assets/MiniPandoras/Art/PixelGen/Maps/<id>/ground.png   바닥(한 칸 64px)
  Assets/MiniPandoras/Art/PixelGen/Maps/<id>/<이름>.png   집·집터·상자 … 그림

한 맵이 이야기에 따라 달라 보이면(낡은 아지트 ↔ 회상 속 전성기의 아지트) 모습(look)을 나눈다. 모습 안에서 세운 것·집터·소품·충돌·빛은
그 모습일 때만 있고, 바닥 그림과 모습 밖에서 놓은 것은 늘 있다. 처음 적은 모습이 기본이고, 다른 모습은 이야기가 고른다.

좌표: 칸 (x, y)는 왼쪽 위가 (0, 0)이고 아래로 +y다. 집은 벽 아래 가운데를 (cx, gy)에 세운다(gy는 벽 아래가 닿는 칸 경계).
"""
import json
import math
from contextlib import contextmanager

import numpy as np
from PIL import Image

import houses
import objects
import pixelgen as pg
import terrain
from preview_map import compose

SUB = 4                         # 충돌 칸을 한 칸에 4×4로 나눈다(웹게임 collisionPerTile)
LOGICAL = 16                    # 한 칸의 논리 px
ART = pg.TILE * pg.SCALE        # 한 칸의 그림 px
SOLID_KINDS = ("water", "cliff", "rock", "sign", "bush")
LAMP_LIGHT = "#ffc870"


class MapLayout:
    def __init__(self, map_id, name, width, height, theme="outdoor", music="", weather=None, ambient=None, background=None, seed=1):
        self.id, self.name, self.width, self.height = map_id, name, width, height
        self.theme, self.music, self.weather, self.ambient, self.background = theme, music, weather, ambient, background
        self.seed = seed
        self.kinds = np.full((height, width), "grass", dtype=object)
        self.solid = np.zeros((height * SUB, width * SUB), dtype=bool)
        self.open = np.zeros((height * SUB, width * SUB), dtype=bool)
        self.props, self.uprights, self.decals, self.lights, self.warps = [], [], [], [], []
        self.gates, self.fires = [], []
        # 바닥을 직접 그리는 맵(실내 …)은 여기에 그리는 함수를 둔다. 없으면 칸 종류로 야외 바닥을 그린다.
        self.paint_ground = None
        self.points = {}
        self.blocked_points = set()
        self.images = {}
        self.look_solid = {}        # 모습 이름(적은 순서) → 그 모습일 때만 막히는 충돌 칸
        self.drawing_look = None
        yy, xx = np.mgrid[0:height, 0:width]
        self.cy, self.cx = yy + 0.5, xx + 0.5

    # ── 바닥 ───────────────────────────────────────────

    def rect(self, kind, x0, y0, x1, y1):
        self.kinds[y0:y1, x0:x1] = kind

    def disc(self, kind, cx, cy, radius):
        self.kinds[(self.cx - cx) ** 2 + (self.cy - cy) ** 2 <= radius ** 2] = kind

    def line(self, kind, points, width, only=None):
        """꺾은선을 따라 너비(칸)만큼 칠한다. only가 있으면 그 종류 칸만 바꾼다."""
        mask = self.near_line(points, width / 2)
        if only is not None:
            mask &= np.isin(self.kinds, only)
        self.kinds[mask] = kind

    def near_line(self, points, reach):
        distance = np.full(self.kinds.shape, np.inf)
        for (ax, ay), (bx, by) in zip(points, points[1:]):
            dx, dy = bx - ax, by - ay
            t = np.clip(((self.cx - ax) * dx + (self.cy - ay) * dy) / max(1e-9, dx * dx + dy * dy), 0, 1)
            distance = np.minimum(distance, np.hypot(self.cx - (ax + t * dx), self.cy - (ay + t * dy)))
        return distance <= reach

    def stamp(self, kind, *cells):
        """한 칸짜리 바닥 장식(bush, rock, sign, flowers)."""
        for x, y in cells:
            self.kinds[y, x] = kind

    # ── 모습 ───────────────────────────────────────────

    @contextmanager
    def look(self, name):
        """이 안에서 세운 것·집터·소품·충돌·빛은 그 모습일 때만 있다. 처음 적은 모습이 기본 모습이다."""
        self.look_solid.setdefault(name, np.zeros_like(self.solid))
        self.drawing_look = name
        try:
            yield
        finally:
            self.drawing_look = None

    def tagged(self, entry):
        """모습 안에서 놓는 것에 그 모습 이름을 붙인다."""
        if self.drawing_look:
            entry["look"] = self.drawing_look
        return entry

    # ── 충돌 ───────────────────────────────────────────

    def solid_layer(self):
        return self.look_solid[self.drawing_look] if self.drawing_look else self.solid

    def block(self, x0, y0, x1, y1):
        """칸 단위 네모(소수 가능)를 막는다. 충돌 칸(1/4칸)으로 반올림한다."""
        self.solid_layer()[round(y0 * SUB):round(y1 * SUB), round(x0 * SUB):round(x1 * SUB)] = True

    def block_where(self, cells):
        """칸 격자 mask가 참인 칸을 모두 막는다(지나갈 수 없는 숲 …)."""
        self.solid_layer()[:] |= np.kron(cells, np.ones((SUB, SUB), dtype=bool))

    def keep_open(self, x0, y0, x1, y1):
        """바닥 종류(물 …)나 테두리와 상관없이 지나갈 수 있게 둔다(출구, 다리)."""
        self.open[round(y0 * SUB):round(y1 * SUB), round(x0 * SUB):round(x1 * SUB)] = True

    def border(self, thickness=1):
        """맵 가장자리를 막는다. 출구는 keep_open으로 연다."""
        self.block(0, 0, self.width, thickness)
        self.block(0, self.height - thickness, self.width, self.height)
        self.block(0, 0, thickness, self.height)
        self.block(self.width - thickness, 0, self.width, self.height)

    # ── 세운 것 ─────────────────────────────────────────

    def upright(self, name, image, foot_x_dots, foot_y_dots, pivot, foot_logical):
        """그림 한 장을 세운다. 발밑(점)에 그림의 기준점을 맞춘다. foot_logical은 인물과 앞뒤를 가르는 줄(논리 px)."""
        self.images[name] = image
        self.uprights.append(self.tagged({"image": name, "left": int(round((foot_x_dots - pivot[0]) * pg.SCALE)),
                                          "top": int(round((foot_y_dots - pivot[1]) * pg.SCALE)), "foot": foot_logical}))

    def house(self, design, cx, gy):
        """
        집 한 채와 집터를 세운다. 충돌은 벽 너비 × (벽 높이 + 지붕 절반)이라 지붕 윗부분 뒤로는 걸어 들어가 가려진다.
        돌려주는 값: 문 앞 칸.
        """
        image, pivot = houses.build(design)
        yard, yard_pivot = houses.plot(design)
        foot_x, foot_y = cx * pg.TILE, gy * pg.TILE
        self.upright(design.name, image, foot_x, foot_y, pivot, gy * LOGICAL)
        self.images[design.name + "_plot"] = yard
        self.decals.append(self.tagged({"image": design.name + "_plot", "left": int(round((foot_x - yard_pivot[0]) * pg.SCALE)),
                                        "top": int(round((foot_y - yard_pivot[1]) * pg.SCALE))}))
        walls_h, roof_h = houses.heights(design)
        depth = (walls_h + roof_h * 0.5) / pg.TILE
        half = design.columns / 2
        self.block(cx - half, gy - depth, cx + half, gy)
        if design.guild:
            self.block(cx + half, gy - (walls_h + 40) / pg.TILE, cx + half + 46 / pg.TILE, gy)
        door_x = cx + (design.door_at - 0.5) * design.columns
        if design.lanterns and not design.worn:
            door_half = houses.DOORS[design.door][0] / 2 + 5
            for side in (-1, 1):
                lx = (door_x * pg.TILE + side * (door_half + 9)) / 2
                self.light(lx, gy * LOGICAL - (houses.STORY - 24) / 2, 30)
        if design.lit:
            self.light(cx * LOGICAL, gy * LOGICAL - 24, design.columns * 7, "#ffb45a")
        return int(door_x), gy

    def stand(self, name, piece, x, ground, block=None):
        """소품 (그림, 기준점)을 세운다. x는 가운데(칸), ground는 발밑이 닿는 칸 경계. block은 막을 (너비, 깊이) 칸."""
        image, pivot = piece
        self.upright(name, image, x * pg.TILE, ground * pg.TILE, pivot, ground * LOGICAL)
        if block:
            w, d = block
            self.block(x - w / 2, ground - d, x + w / 2, ground)

    def decal(self, name, image, left_dots, top_dots):
        """바닥이나 벽면 위에 까는 그림(양탄자, 먼지, 거미줄, 창을 막은 판자 …). 인물·세운 것보다 늘 아래다."""
        self.images[name] = image
        self.decals.append(self.tagged({"image": name, "left": int(round(left_dots * pg.SCALE)),
                                        "top": int(round(top_dots * pg.SCALE))}))

    def tree(self, x, y, kind="tree"):
        self.props.append(self.tagged({"id": kind, "x": x, "y": y}))
        self.block(x, y, x + 1, y + 1)

    def lamp(self, x, y):
        self.props.append(self.tagged({"id": "lamp", "x": x, "y": y}))
        self.block(x + 0.25, y + 0.5, x + 0.75, y + 1)
        self.light(x * LOGICAL + 8, (y + 1) * LOGICAL - 44, 30)

    def thing(self, kind, x, y):
        """한 칸에 서는 물건: crate, barrel, well."""
        maker = {"crate": objects.crate, "barrel": objects.barrel, "well": objects.well}[kind]
        image, pivot = maker(np.random.default_rng(self.seed + y * 97 + x))
        self.upright(f"{kind}_{x}_{y}", image, x * pg.TILE + pg.TILE // 2, (y + 1) * pg.TILE - 1, pivot, (y + 1) * LOGICAL)
        self.block(x, y, x + 1, y + 1)

    def fountain(self, x0, y0, size):
        """둥근 돌 분수. size×size칸을 차지한다(왼쪽 위 칸 x0, y0)."""
        image, pivot = objects.fountain(size, np.random.default_rng(self.seed + 5))
        bottom = y0 + size
        self.upright(f"fountain_{x0}_{y0}", image, (x0 + size / 2) * pg.TILE, bottom * pg.TILE - 1, pivot, bottom * LOGICAL)
        self.block(x0, y0 + 0.5, x0 + size, bottom)

    def fence(self, cells):
        """울타리 칸들. 이웃한 울타리 쪽으로 가로대가 이어진다."""
        cell_set = set(cells)
        for x, y in cells:
            mask = sum(bit for bit, (dx, dy) in ((1, (-1, 0)), (2, (1, 0)), (4, (0, -1)), (8, (0, 1))) if (x + dx, y + dy) in cell_set)
            image, pivot = objects.fence(mask)
            self.upright(f"fence_{mask}", image, x * pg.TILE + pg.TILE // 2, (y + 1) * pg.TILE - 1, pivot, (y + 1) * LOGICAL)
            self.block(x, y + 0.5, x + 1, y + 1)

    def gate(self, point, opened, closed, foot_x_dots, foot_y_dots, pivot, foot_logical):
        """
        이야기가 닫을 수 있는 문(맵 들여오기의 MapGate). 열린 그림을 세우고 닫힌 그림은 gates에 적는다(두 그림은 크기가 같다).
        닫히면 지점 point의 칸이 막힌다.
        """
        self.upright(f"{point}_open", opened, foot_x_dots, foot_y_dots, pivot, foot_logical)
        self.uprights[-1]["gate"] = point
        self.images[f"{point}_closed"] = closed
        entry = dict(self.uprights[-1], image=f"{point}_closed")
        del entry["gate"]
        self.gates.append(dict(entry, point=point))

    def fire(self, x, y, kind="fire"):
        """칸 (x, y)에 타는 불꽃(웹게임 fire·torch 타일, 맵 들여오기가 움직이는 불꽃을 얹는다)."""
        self.fires.append(self.tagged({"x": x, "y": y, "kind": kind}))

    def reachable(self, start, goals, look=None):
        """start 칸에서 막힌 칸을 피해 goals 가운데 한 칸에 걸어서 닿을 수 있는지(위아래·양옆으로만 걷는다)."""
        solid = self.collision(look)
        free = ~solid.reshape(self.height, SUB, self.width, SUB).any(axis=(1, 3))
        for x, y in [start] + list(goals):
            free[y, x] = True
        seen, frontier = {tuple(start)}, [tuple(start)]
        targets = {tuple(goal) for goal in goals}
        while frontier:
            x, y = frontier.pop()
            if (x, y) in targets:
                return True
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < self.width and 0 <= ny < self.height and free[ny, nx] and (nx, ny) not in seen:
                    seen.add((nx, ny))
                    frontier.append((nx, ny))
        return False

    def route_is_clear(self, start, route, look=None):
        """순찰 길("R6 L6" 같은 웹게임 걷기 글)이 막힌 칸을 지나지 않는지."""
        solid = self.collision(look)
        x, y = start
        steps = {"R": (1, 0), "L": (-1, 0), "U": (0, -1), "D": (0, 1)}
        for move in route.split():
            dx, dy = steps[move[0]]
            for _ in range(int(move[1:])):
                x, y = x + dx, y + dy
                if solid[y * SUB:(y + 1) * SUB, x * SUB:(x + 1) * SUB].any():
                    return False
        return True

    def light(self, x, y, radius, color=LAMP_LIGHT, flicker=False):
        """빛(논리 px)."""
        self.lights.append(self.tagged({"x": round(x, 1), "y": round(y, 1), "radius": radius, "color": color, "flicker": flicker}))

    # ── 이야기 지점 ──────────────────────────────────────

    def point(self, name, *cells, blocked=False):
        """이야기가 쓰는 지점. blocked는 막힌 칸이어도 되는 지점(표지판처럼 마주 보고 말을 거는 것)."""
        self.points.setdefault(name, []).extend(cells)
        if blocked:
            self.blocked_points.add(name)

    def warp(self, point, target_map, target, direction):
        self.warps.append({"point": point, "map": target_map, "target": target, "dir": direction})
        for x, y in self.points[point]:
            self.keep_open(x, y, x + 1, y + 1)

    # ── 저장 ───────────────────────────────────────────

    def collision(self, look=None):
        """막힌 충돌 칸. look을 주면 그 모습일 때만 막히는 칸도 더한다."""
        solid = self.solid.copy()
        for kind in SOLID_KINDS:
            cells = np.kron(self.kinds == kind, np.ones((SUB, SUB), dtype=bool))
            solid |= cells
        solid &= ~self.open
        # 모습 안에서 막은 칸은 열어 둔 자리보다 앞선다(끊어진 다리는 그 모습에서만 막힌다).
        return solid | self.look_solid[look] if look else solid

    def check(self):
        """지점 칸이 막혀 있으면 알린다(이야기가 그 칸에 인물을 세운다). 모습이 있으면 모습마다 본다."""
        problems = []
        for look in list(self.look_solid) or [None]:
            solid = self.collision(look)
            for name, cells in self.points.items():
                if name in self.blocked_points:
                    continue
                for x, y in cells:
                    if solid[y * SUB:(y + 1) * SUB, x * SUB:(x + 1) * SUB].any():
                        problems.append(f"{name} ({x}, {y})" + (f" [{look}]" if look else ""))
        return problems

    def save(self, project):
        folder = project / f"Assets/MiniPandoras/Art/PixelGen/Maps/{self.id}"
        folder.mkdir(parents=True, exist_ok=True)
        ground = self.paint_ground() if self.paint_ground else terrain.draw_outdoor(self.kinds.tolist(), seed=self.seed)
        # 허공에 뜬 섬처럼 바닥 밖이 투명한 맵은 RGBA로 저장한다(맵 배경이 비친다).
        Image.fromarray(pg.upscale(ground), "RGBA" if ground.shape[2] == 4 else "RGB").save(folder / "ground.png")
        for name, image in self.images.items():
            Image.fromarray(pg.upscale(image), "RGBA").save(folder / f"{name}.png")
        solid = self.collision()
        rng = np.random.default_rng(self.seed)
        water = [{"x": int(x), "y": int(y), "kind": "water"} for y, x in zip(*np.nonzero(self.kinds == "water"))]
        glints = [{"x": round((cell["x"] + rng.random()) * LOGICAL, 1), "y": round((cell["y"] + rng.random()) * LOGICAL, 1),
                   "phase": round(float(rng.random() * math.tau), 3)} for cell in water if rng.random() < 0.35]
        data = {
            "id": self.id, "name": self.name, "theme": self.theme, "music": self.music,
            "width": self.width, "height": self.height, "tilePixels": LOGICAL, "artScale": 4, "collisionPerTile": SUB,
            "collision": ["".join("1" if v else "0" for v in row) for row in solid],
            "props": self.props,
            "points": [{"name": name, "cells": [{"x": x, "y": y} for x, y in cells]} for name, cells in self.points.items()],
            "warps": self.warps, "uprights": self.uprights, "decals": self.decals, "gates": self.gates,
            "animated": water + self.fires, "glints": glints, "lights": self.lights,
            "background": self.background, "ambient": self.ambient, "weather": self.weather,
            "looks": [{"name": name, "collision": ["".join("1" if v else "0" for v in row) for row in solid_cells]}
                      for name, solid_cells in self.look_solid.items()],
        }
        (folder / "map.json").write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
        return folder, ground

    def publish(self, project):
        """
        저장하고 미리보기(Docs/ArtUpgrade/PixelGen/<id>_preview.png)를 만든다. 막힌 지점이 있으면 알린다.
        모습이 있으면 기본 모습이 <id>_preview.png, 나머지가 <id>_<모습>_preview.png다.
        """
        problems = self.check()
        if problems:
            print("막힌 지점:", ", ".join(problems))
        folder, _ = self.save(project)
        looks = list(self.look_solid) or [None]
        outputs = []
        for index, look in enumerate(looks):
            suffix = "" if index == 0 else f"_{look}"
            out = project / f"Docs/ArtUpgrade/PixelGen/{self.id}{suffix}_preview.png"
            compose(project, folder, look).save(out)
            outputs.append(str(out))
        return "\n".join(outputs)
