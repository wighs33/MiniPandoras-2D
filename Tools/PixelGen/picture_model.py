"""HD-2D 필드에 그림 한 장 대신 세우는 모형의 공통 도구: 그림을 입힌 면 목록과 맵마다 한 장인 아틀라스.

그림은 위에서 비스듬히 본 그림이라, 그림 점 (x, 줄 r)이 높이 h에 있으면 발밑은 그 점보다 h점 남쪽(줄 g = r + h)이다
(맵 바닥을 일으키는 relief와 같은 규칙). 그래서 면마다 높이만 정하면 3D 자리가 정해진다.
면은 꼭짓점 3~4개이고, 꼭짓점마다 (x, g, h)는 그림 점 좌표로 잰 자리, (u, v)는 그 면이 쓰는 그림의 점 자리다.
면의 밝기는 셰이더(_FACE_LIT)가 실제 해 방향으로 정한다. shade는 해와 상관없이 어두운 곳에 곱하는 밝기다.

맵 하나의 모형은 한 아틀라스(models.png)에 모으고, 면 목록(models.json)은 모형 이름마다 발밑 기준 점으로 적는다.
같은 그림은 한 번만 넣는다. 맵 들여오기(Editor/WebGame/UprightModels)가 Upright.Above로 세워 메시를 만든다.
"""
import json
from dataclasses import dataclass, field

import numpy as np
from PIL import Image

import pixelgen as pg

PAD = 2               # 아틀라스에서 그림 사이를 비우는 점(겹쳐 읽지 않게)
ATLAS = "models"      # 맵 폴더의 models.png · models.json
GROUND = "ground"     # 바닥 그림 대신 까는 모형의 이름(돌뱀 머리 위 …)
# 세운 그림이 뒤로 젖혀진 각도(World.Upright.LeanBackDegrees). 그림에서 되돌린 면은 모두 이만큼 젖혀 선다.
LEAN = np.deg2rad(18)


@dataclass
class Panel:
    """아틀라스에 들어갈 그림 한 장. origin은 그림의 (0, 0)이 가리키는 원본 좌표(집 그림 층이면 그 캔버스 좌표)."""
    image: np.ndarray
    origin: tuple = (0, 0)
    left: int = 0
    top: int = 0

    @property
    def size(self):
        return self.image.shape[1], self.image.shape[0]


@dataclass
class Face:
    panel: Panel
    corners: list          # [(x, g, h, u, v)], u·v는 패널 원본 좌표
    shade: float = 1.0


@dataclass
class Model:
    """모형 하나. foot는 면 좌표에서 발밑(x, g), pivot은 같은 점을 세운 그림(uprights의 image)에서 잰 자리다."""
    foot: tuple
    pivot: tuple
    house: object = None                            # 집 모형이면 houses.House
    smooth: bool = False                            # 이웃 면의 방향을 고르게 이어 둥글게 비춘다(부조)
    panels: list = field(default_factory=list)
    faces: list = field(default_factory=list)
    shared: dict = field(default_factory=dict)      # 여러 면이 함께 쓰는 새로 그린 그림(지붕창 작은 지붕)

    def panel(self, image, origin=(0, 0), crop=True):
        if crop:
            image, origin = cropped(image, origin)
        panel = Panel(image, origin)
        self.panels.append(panel)
        return panel

    def face(self, panel, corners, shade=1.0):
        self.faces.append(Face(panel, corners, shade))


def cropped(image, origin):
    """칠한 곳만 남긴 그림과 그 새 원점."""
    painted = image[..., 3] > 0
    rows, columns = np.nonzero(painted.any(axis=1))[0], np.nonzero(painted.any(axis=0))[0]
    if len(rows) == 0:
        return image[:1, :1], origin
    top, left = int(rows.min()), int(columns.min())
    return image[top:rows.max() + 1, left:columns.max() + 1], (origin[0] + left, origin[1] + top)


def plumb(x, g, rise):
    """
    땅 자리 (x, g)에서 곧게(젖히지 않고) rise점 올라간 자리의 면 좌표 (x, g, h). 음수면 땅 아래다.
    비스듬히 본 그림이 아니라 위에서 본 그림·새로 짠 조각(돌뱀 머리의 옆면)에 쓴다.
    """
    return x, g + rise * np.tan(LEAN), rise / np.cos(LEAN)


def standing(model, panel, x0, x1, top, bottom, g, h_bottom, shade=1.0):
    """그림 (x0~x1, top~bottom)을 발밑 줄 g에 세운 판으로. 그림 줄 bottom이 높이 h_bottom이다."""
    model.face(panel, [(x0, g, h_bottom, x0, bottom), (x1, g, h_bottom, x1, bottom),
                       (x1, g, h_bottom + bottom - top, x1, top), (x0, g, h_bottom + bottom - top, x0, top)], shade)


def stretched_side(model, panel, x, column, top, bottom, g, h_bottom, h_top, depth, shade=1.0):
    """세운 판의 옆면: x 자리에서 북쪽으로 depth점, 높이 h_bottom~h_top. 그림 column 열의 top~bottom 줄을 옆으로 늘여 칠한다."""
    u = column + 0.5
    model.face(panel, [(x, g, h_bottom, u, bottom), (x, g - depth, h_bottom, u, bottom),
                       (x, g - depth, h_top, u, top), (x, g, h_top, u, top)], shade)


def write(folder, models):
    """모형들의 그림을 아틀라스 한 장에 모으고 면 목록을 적는다."""
    atlas = pack([panel for model in models.values() for panel in model.panels])
    Image.fromarray(pg.upscale(atlas), "RGBA").save(folder / f"{ATLAS}.png")
    data = {"size": [atlas.shape[1], atlas.shape[0]],
            "models": [{"name": name, "pivot": [float(v) for v in model.pivot], "smooth": model.smooth, "faces": exported(model)} for name, model in models.items()]}
    (folder / f"{ATLAS}.json").write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def pack(panels):
    """그림들을 높은 것부터 선반에 늘어놓은 아틀라스(RGBA 점, 정사각형에 가깝게). 각 패널의 left·top을 채운다. 똑같은 그림은 자리를 함께 쓴다."""
    unique = {}
    for panel in panels:
        unique.setdefault((panel.image.shape, panel.image.tobytes()), []).append(panel)
    placed = sorted(unique.values(), key=lambda same: -same[0].size[1])
    area = sum((same[0].size[0] + PAD) * (same[0].size[1] + PAD) for same in placed)
    width = int(max(max(same[0].size[0] for same in placed) + PAD * 2, np.sqrt(area) * 1.1))
    x = y = PAD
    shelf = 0
    for same in placed:
        w, h = same[0].size
        if x + w + PAD > width:
            x, y, shelf = PAD, y + shelf + PAD, 0
        for panel in same:
            panel.left, panel.top = x, y
        x += w + PAD
        shelf = max(shelf, h)
    atlas = np.zeros((y + shelf + PAD, width, 4), dtype=np.uint8)
    for same in placed:
        panel = same[0]
        w, h = panel.size
        atlas[panel.top:panel.top + h, panel.left:panel.left + w] = panel.image
    return atlas


def exported(model):
    """면 목록을 발밑에서 잰 점과 아틀라스 점 자리로 옮긴 것."""
    faces = []
    for face in model.faces:
        panel = face.panel
        points, uv = [], []
        for x, g, h, u, v in face.corners:
            points += [round(float(x - model.foot[0]), 2), round(float(g - model.foot[1]), 2), round(float(h), 2)]
            uv += [round(float(u - panel.origin[0] + panel.left), 2), round(float(v - panel.origin[1] + panel.top), 2)]
        faces.append({"points": points, "uv": uv, "shade": face.shade})
    return faces
