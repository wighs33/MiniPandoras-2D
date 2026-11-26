"""바닥 그림의 높이(HD-2D 필드에서 벽·절벽을 일으켜 세운다).

맵 바닥 그림은 위에서 비스듬히 본 그림이다: 높이 h인 곳은 제 발밑보다 h점 위(북쪽)에 그려진다. 그래서 그림의 점마다
「발밑에서 몇 점 떠 있는지」만 알면 3D 자리가 정해진다(발밑 = 그림 줄 + h, 높이 = h). 맵 들여오기(MapRelief.cs)가
이 높이로 바닥 그림을 메시로 일으킨다: 바닥은 그대로 눕고, 벽면은 바닥 가장자리에 서고, 벽 윗면은 그 높이에 뜬다.

점마다 두 값을 적는다.
  top   점 윗변의 높이(점). 바닥 0, 벽 윗면은 벽 높이, 물·허공처럼 바닥보다 낮은 곳은 음수.
  drop  남쪽(아래)으로 한 줄 갈 때 줄어드는 높이: 0 평평한 곳, 1 세운 면(벽면·절벽면), 0.5 비탈(계단·지붕).
인물이 걷는 곳은 높이 0이어야 한다(걷기·충돌은 맵 바닥 그대로 돈다).

  Assets/MiniPandoras/Art/PixelGen/Maps/<id>/relief.png   점 하나 = 1px. R·G = top × 2 + 32768(16비트), B = drop × 2
"""
import numpy as np
from PIL import Image

FLAT, SLOPE, FACE = 0.0, 0.5, 1.0
# 바깥 절벽(terrain.cliff): 벽면이 이보다 높으면 그 위는 윗면으로 둔다(점). 카메라 쪽 절벽은 이 높이로 낮게 자른다(점).
CLIFF_HIGHEST = 80
CLIFF_FRONT = 12


class Relief:
    """맵 바닥 그림(점 격자)의 높이. 처음에는 모두 높이 0인 바닥이다."""

    def __init__(self, height_dots, width_dots):
        self.top = np.zeros((height_dots, width_dots))
        self.drop = np.zeros((height_dots, width_dots))

    def flat(self, mask, height):
        """mask인 점을 높이 height(점)의 평평한 곳으로."""
        self.top[mask] = height
        self.drop[mask] = FLAT

    def face(self, mask, top):
        """mask인 점을 세운 면으로. top은 점 윗변의 높이(점, 배열이면 그 자리 값)."""
        self.top[mask] = top[mask] if np.ndim(top) else top
        self.drop[mask] = FACE

    def slope(self, mask, top):
        """mask인 점을 비탈로(남쪽으로 두 줄에 한 점씩 낮아진다). top은 점 윗변의 높이(점, 배열이면 그 자리 값)."""
        self.top[mask] = top[mask] if np.ndim(top) else top
        self.drop[mask] = SLOPE

    def cliffs(self, area, highest=CLIFF_HIGHEST, front=CLIFF_FRONT):
        """
        terrain.cliff가 그린 바위 절벽(area 점)을 땅에서 솟은 벽으로 세운다. 열마다 이어진 절벽 띠 하나에서 위 40%는 윗면, 아래는 벽면이다
        (terrain.cliff와 같은 나눔). 벽면이 highest점보다 높으면 그 위는 윗면으로 둔다(맵 가장자리를 덮은 절벽이 하늘까지 솟지 않게).
        카메라 쪽 절벽(같은 열의 북쪽에만 땅이 있다)은 front점으로 낮게 자른다.
        """
        cut = in_front(~area)
        for x in range(area.shape[1]):
            rows = np.nonzero(area[:, x])[0]
            if len(rows) == 0:
                continue
            for run in np.split(rows, np.nonzero(np.diff(rows) > 1)[0] + 1):
                top, bottom = run[0], run[-1]
                self.drop[top:bottom + 1, x] = FLAT
                if cut[bottom, x]:
                    self.top[top:bottom + 1, x] = front
                    continue
                split = top + int((bottom - top + 1) * 0.4)
                self.top[top:bottom + 1, x] = min(bottom - split, highest)
                face_rows = np.arange(split + 1, bottom + 1)
                heights = bottom + 1 - face_rows
                standing = heights <= highest
                self.top[face_rows[standing], x] = heights[standing]
                self.drop[face_rows[standing], x] = FACE

    def is_flat(self):
        return not self.top.any() and not self.drop.any()

    def save(self, path):
        value = np.clip(np.round(self.top * 2) + 32768, 0, 65535).astype(np.uint32)
        rgb = np.stack([value >> 8, value & 255, np.round(self.drop * 2).astype(np.uint32)], axis=-1).astype(np.uint8)
        Image.fromarray(rgb, "RGB").save(path)


def walls(floor, face_height, cap, front=None):
    """
    바닥과 그 둘레 벽. floor는 바닥 점(높이 0), face_height는 세운 벽면인 점의 바닥에서 잰 높이(그 점 윗변, 1부터; 벽면이 아니면 0),
    나머지는 높이 cap의 벽 윗면이다. 동굴 바위, 방 사이 벽, 둥근 방의 벽이 모두 이 꼴이다.
    front를 주면 카메라 쪽 벽(같은 열의 북쪽에만 바닥이 있는 벽)은 그 높이로 낮게 자른다. 높으면 방 남쪽을 가린다(HD-2D 실내의 단면).
    """
    relief = Relief(*floor.shape)
    relief.flat(~floor, cap)
    if front is not None:
        relief.flat(in_front(floor) & (face_height == 0), front)
    relief.face(face_height > 0, face_height)
    relief.flat(floor, 0)
    return relief


def in_front(floor, reach=40, divider=24):
    """
    바닥이 아닌 점 가운데 같은 열의 북쪽에는 바닥이 있고 남쪽에는 없는 점(방의 카메라 쪽 벽).
    양옆 reach점 안에 모두 바닥이 있는 점이 세로로 divider줄 넘게 이어지면 두 방 사이 칸막이라 빼고 높게 둔다.
    짧게 이어진 것은 벽이 바닥 쪽으로 조금 튀어나온 곳이다. 이것을 높게 두면 벽 앞에 기둥이 하나 선다.
    """
    above = np.zeros_like(floor)
    above[1:] = np.logical_or.accumulate(floor, axis=0)[:-1]
    below = np.zeros_like(floor)
    below[:-1] = np.logical_or.accumulate(floor[::-1], axis=0)[::-1][1:]
    left = np.zeros_like(floor)
    right = np.zeros_like(floor)
    for k in range(1, reach + 1):
        left[:, k:] |= floor[:, :-k]
        right[:, :-k] |= floor[:, k:]
    between = ~floor & left & right
    return ~floor & above & ~below & ~tall_runs(between, divider)


def tall_runs(mask, length):
    """세로로 length줄 넘게 이어진 점만 남긴다."""
    down, up = np.zeros(mask.shape, dtype=int), np.zeros(mask.shape, dtype=int)
    for y in range(mask.shape[0]):
        down[y] = np.where(mask[y], (down[y - 1] if y else 0) + 1, 0)
    for y in range(mask.shape[0] - 1, -1, -1):
        up[y] = np.where(mask[y], (up[y + 1] if y + 1 < mask.shape[0] else 0) + 1, 0)
    return mask & (down + up - 1 > length)


def standing(mask, highest=None):
    """
    mask의 열마다 이어진 띠 하나를 그 띠 아래 끝 땅에서 솟은 벽면으로 세운다(윗면 없이 그린 벽: 고목의 나무껍질 절벽 …).
    highest를 주면 그보다 높은 곳은 그 높이의 윗면으로 둔다.
    """
    heights = Relief(*mask.shape)
    for x in range(mask.shape[1]):
        rows = np.nonzero(mask[:, x])[0]
        if len(rows) == 0:
            continue
        for run in np.split(rows, np.nonzero(np.diff(rows) > 1)[0] + 1):
            top = run[-1] + 1 - run
            if highest is not None:
                heights.top[run[top > highest], x] = highest
                run, top = run[top <= highest], top[top <= highest]
            heights.top[run, x] = top
            heights.drop[run, x] = FACE
    return heights


def overlay(base, extra):
    """extra에서 높이가 있는 점(바닥 0이 아닌 점)을 base에 덮어쓴다."""
    marked = (extra.top != 0) | (extra.drop != 0)
    base.top[marked] = extra.top[marked]
    base.drop[marked] = extra.drop[marked]
    return base


def island(top, solid, gap=3):
    """
    허공에 뜬 섬. 윗면(top 점)은 바닥 높이이고, 열마다 윗면 아래 끝에서 이어지는 불투명한 점(solid: 매달린 흙·바위·수정과 그 외곽선)은
    섬 가장자리에서 아래로 늘어진 면이다(높이 음수). 몇 점짜리 틈은 건너 잇는다(남기면 바위 점이 허공 판에 납작하게 그려진다).
    그 밖의 점은 바닥 높이 그대로 둔다(섬 밖의 허공, 멀리 떨어져 떠 있는 조각).
    """
    heights = Relief(*top.shape)
    for x in np.nonzero(top.any(axis=0))[0]:
        bottom = np.nonzero(top[:, x])[0].max()
        last, y = bottom, bottom + 1
        while y < top.shape[0] and y - last <= gap + 1 and not top[y, x]:
            if solid[y, x]:
                last = y
            y += 1
        rows = np.arange(bottom + 1, last + 1)
        heights.top[rows, x] = -(rows - bottom - 1)
        heights.drop[rows, x] = FACE
    return heights


def faces_above(floor, height):
    """바닥 북쪽 가장자리 바로 위 height점을 벽면으로 잡은 높이(바닥에서 잰 점 윗변 높이). 그 사이에 다른 바닥이 끼면 거기서 끊는다."""
    face_height = np.zeros(floor.shape, dtype=int)
    taken = floor.copy()
    for k in range(1, height + 1):
        above = np.zeros_like(floor)
        above[:-k] = floor[k:]
        cell = above & ~taken
        face_height[cell] = k
        taken |= cell
    return face_height
