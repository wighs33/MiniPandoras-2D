"""HD-2D 필드의 집 모형: 층으로 나눠 그린 집(houses.draw → House)을 닫힌 3D 모형(그림을 입힌 면 목록)으로 만든다.

집 그림은 위에서 비스듬히 본 그림이라, 그림 점 (x, 줄 r)이 높이 h에 있으면 발밑은 그 점보다 h점 남쪽(줄 g = r + h)이다
(맵 바닥을 일으키는 relief와 같은 규칙). 그래서 면마다 높이만 정하면 3D 자리가 정해진다.
  - 앞벽(wall·gable·wing_gable·ladder 층): 벽 아래 줄(ground)에 선 판. h = ground − r.
  - 옆으로 긴 박공지붕(slope): 처마 줄에서 접혀 뒤로 눕는다. 그림 한 줄 오를 때마다 높이 SLOPE점, 북쪽 1 − SLOPE점.
    양끝은 그림처럼 안으로 비스듬한 세모 박공(hip)이고, 용마루 뒤로 같은 길이의 뒤 지붕이 내려간다.
  - 앞이 세모인 지붕(planes): 박공선 높이 그대로 북쪽으로 물러나는 두 면. 그림에는 앞쪽 조금만 있어 모형에는 집 깊이만큼 다시 그린다.
  - 평지붕(flat): 벽 꼭대기 높이에 누운 판.
  - 굴뚝·쇠 굴뚝(상자), 지붕창(옆벽과 작은 지붕), 바람개비, 길드 탑(상자와 뿔 지붕): 지붕 위 그 자리에 선다.
그림에 없는 옆벽·지붕 끝 박공은 같은 재료로 한 장씩 새로 그리고, 뒤 지붕은 앞 지붕 그림을 다시 쓴다(그림이 메모리를 덜 쓰게).
면의 밝기는 셰이더(_FACE_LIT)가 실제 해 방향으로 정하므로, 여기서 동·서 명암을 칠하지 않는다.
북쪽을 보는 면(뒷벽·뒤 박공)은 카메라가 늘 남쪽에 있어 보이지 않으므로 만들지 않는다. 지붕 속으로 들어간 면(지붕창 옆벽 …)은
깊이 검사가 지붕 면에서 잘라 준다.

모형의 좌표와 아틀라스는 picture_model이 다룬다.
"""
import numpy as np

import houses
from houses import ADOBE, PLINTH, ROOFS, STONE, STORY, WOOD, Canvas, cover, outline
from picture_model import Model, standing, stretched_side

SLOPE = 0.5               # 옆으로 긴 박공지붕이 그림 한 줄에 오르는 높이(점)
FRONT_LAYERS = ("wall", "gable", "wing_gable", "ladder")
GAP = WOOD[1]             # 앞박공 판자와 세모 벽 사이 틈을 메우는 색(판자 그늘)
DORMER_DEPTH = 38         # 지붕창 옆벽·작은 지붕이 지붕 속까지 들어가는 깊이(점)
SUNK = 10                 # 굴뚝은 지붕 속으로 이만큼 더 내린다. 앞이 세모인 지붕은 옆으로 기울어 굴뚝 아래쪽 한 귀가 뜬다.


def build(house):
    """집 모형(picture_model.Model)."""
    model = Model(foot=(house.x0 + (house.x1 - house.x0) // 2, house.ground), pivot=house.pivot, house=house)
    rng = np.random.default_rng(house.design.seed + 900)
    main = next(part for part in house.parts if part["layer"] == "roof")
    add_front(model, main)
    for part in house.parts:
        kind = part["kind"]
        if kind == "slope":
            add_slope(model, part, rng)
        elif kind == "planes":
            add_planes(model, part, rng)
        elif kind == "flat":
            add_flat(model, part, rng)
        elif kind == "stack":
            add_stack(model, part)
        elif kind == "dormer":
            add_dormer(model, part, rng)
        elif kind == "card":
            add_card(model, part)
        elif kind == "tower":
            add_tower(model, part)
    return model


# ── 높이 ──────────────────────────────────────────────

def slope_at(house, part, r):
    """옆으로 긴 박공지붕의 그림 줄 r이 놓이는 (g, h). 처마 접는 줄에서 앞벽과 만난다."""
    eave = house.ground - part["hinge"]
    h = eave + SLOPE * (part["hinge"] - r)
    return r + h, h


def rake_row(part, x):
    """앞이 세모인 지붕에서 x 열의 박공선 줄(지붕 면의 앞 끝)."""
    cx = (part["left"] + part["right"]) / 2
    edge = part["left"] if x <= cx else part["right"]
    return part["eave"] + (part["apex"] - part["eave"]) * (x - edge) / (cx - edge)


def surface(house, x, r):
    """지붕 위 그림 점 (x, r)의 (g, h): 그 점을 맨 위에 칠한 지붕 면을 따른다. 지붕이 아니면 앞벽."""
    for part in reversed(house.parts):
        layer = house.canvas.layers[part["layer"]]
        if part["kind"] not in ("slope", "planes", "flat") or layer[r, x, 3] == 0:
            continue
        if part["kind"] == "slope":
            return slope_at(house, part, r)
        h = house.ground - (rake_row(part, x) if part["kind"] == "planes" else part["front"])
        return r + h, h
    return house.ground, house.ground - r


def clip_profile(house, main, x):
    """앞벽 층을 이 줄 위로는 지운다(지붕이 덮던 벽 윗부분이 접힌 지붕 앞에 서지 않게). 지붕 밖 열은 지우지 않는다."""
    if not main["left"] - 1 <= x <= main["right"] + 1:
        return -1
    if main["kind"] == "slope":
        return main["hinge"]
    if main["kind"] == "planes":
        return rake_row(main, x)
    return main["front"]


# ── 앞벽 ──────────────────────────────────────────────

def add_front(model, main):
    """앞벽 판: 벽(지붕 아래로 잘라 낸 것)과 박공벽·사다리를 겹친 그림을 벽 아래 줄에 세운다."""
    house = model.house
    canvas = house.canvas
    height, width = canvas.yy.shape
    out = np.zeros((height, width, 4), dtype=np.uint8)
    profile = np.array([clip_profile(house, main, x) for x in range(width)])
    for name, layer in canvas.layers.items():
        if name == "wall":
            layer = np.where((canvas.yy < profile[None, :])[..., None], 0, layer).astype(np.uint8)
        elif name == "roof" and main["kind"] == "flat":
            # 평지붕 판의 앞 두께는 앞벽과 함께 선다.
            layer = np.where((canvas.yy < main["front"])[..., None], 0, layer).astype(np.uint8)
        elif name not in FRONT_LAYERS:
            continue
        painted = layer[..., 3] > 0
        out[painted] = layer[painted]
    out = outline(out)
    # 앞박공 판자와 세모 벽 사이 틈: 그림에서는 바닥이 비쳤지만 모형에서는 집 속이 보이므로 판자 그늘로 메운다.
    for x in range(house.x0, house.x1):
        top = max(0, int(np.ceil(profile[x])))
        hole = out[top:house.ground, x, 3] == 0
        out[top:house.ground, x][hole] = np.append(GAP, 255)
    panel = model.panel(out)
    ground = house.ground
    (left, top), (w, h) = panel.origin, panel.size
    bottom, right = top + h, left + w
    model.face(panel, [(left, ground, ground - bottom, left, bottom), (right, ground, ground - bottom, right, bottom),
                       (right, ground, ground - top, right, top), (left, ground, ground - top, left, top)])


def add_side_walls(model, depth, height, rng):
    """옆벽 둘: 벽 양끝(외곽선 바깥 가장자리)에서 북쪽으로 depth점, 높이 height점. 그림 한 장을 함께 쓴다."""
    house = model.house
    ground = house.ground
    panel = model.panel(side_wall(house.design, depth, height, rng), crop=False)
    for x, west in ((house.x0 - 1, True), (house.x1 + 1, False)):
        # 밖에서 보면 서쪽 벽은 남쪽(앞)이 오른쪽, 동쪽 벽은 왼쪽이다.
        front_u, back_u = (depth, 0) if west else (0, depth)
        model.face(panel, [(x, ground, 0, front_u, height), (x, ground - depth, 0, back_u, height),
                           (x, ground - depth, height, back_u, 0), (x, ground, height, front_u, 0)])


# ── 지붕 ──────────────────────────────────────────────

def add_slope(model, part, rng):
    """옆으로 긴 박공지붕: 그린 앞 지붕을 접고, 양끝 세모 박공과 뒤 지붕, 옆벽을 더한다."""
    house = model.house
    ground = house.ground
    panel = model.panel(outline(house.canvas.layers[part["layer"]]))
    (left, top), (w, _) = panel.origin, panel.size
    right = left + w
    hinge, ridge, lean = part["hinge"], part["ridge"], part["lean"]
    g_top, h_top = slope_at(house, part, top)
    g_hinge, h_eave = slope_at(house, part, hinge)
    model.face(panel, [(left, g_hinge, h_eave, left, hinge), (right, g_hinge, h_eave, right, hinge),
                       (right, g_top, h_top, right, top), (left, g_top, h_top, left, top)])

    # 용마루와 처마 모서리(외곽선 한 점 바깥)
    g_ridge, h_ridge = slope_at(house, part, ridge)
    span = int(round(2 * (ground - g_ridge)))   # 집 깊이: 뒤 지붕이 앞 지붕과 같은 길이
    back = ground - span
    eave_l, eave_r = part["left"] - 1, part["right"] + 1
    ridge_l, ridge_r = eave_l + lean, eave_r - lean
    # 뒤 지붕은 앞 지붕과 모양(처마가 넓고 용마루가 좁은 사다리꼴)이 같아 그 그림을 다시 쓴다.
    model.face(panel, [(ridge_l, g_ridge, h_ridge, ridge_l, ridge), (ridge_r, g_ridge, h_ridge, ridge_r, ridge),
                       (eave_r, back, h_eave, eave_r, hinge), (eave_l, back, h_eave, eave_l, hinge)])

    slant = int(np.hypot(lean, h_ridge - h_eave))
    hip = model.panel(hip_panel(part["material"], span, slant, rng), crop=False)
    for x_eave, x_ridge in ((eave_l, ridge_l), (eave_r, ridge_r)):
        model.face(hip, [(x_eave, ground, h_eave, 0, slant), (x_eave, back, h_eave, span, slant), (x_ridge, g_ridge, h_ridge, span / 2, 0)])
    add_side_walls(model, span, int(h_eave), rng)


def add_planes(model, part, rng):
    """
    앞이 세모인 지붕의 두 면을 집 깊이만큼 다시 그려 박공선 높이로 물린다. 집 지붕이면 옆벽도 더한다.
    입구 위로 나온 세모 지붕(wing_roof)은 본 지붕 용마루가 닿는 깊이까지 물리고(그보다 낮으면 본 지붕 속으로 숨는다), 옆벽은 없다.
    """
    house = model.house
    ground = house.ground
    walls_h, roof_h = houses.heights(house.design)
    main = part["layer"] == "roof"
    if main:
        depth = int(min(walls_h + roof_h * 0.5, house.x1 - house.x0))
    else:
        slope = next(p for p in house.parts if p["kind"] == "slope")
        g_ridge, h_ridge = slope_at(house, slope, slope["ridge"])
        apex_h = ground - part["apex"]
        # 꼭대기가 본 용마루보다 낮으면 앞 지붕 면에 묻히는 깊이까지, 높으면 집 끝까지 간다.
        full = 2 * (ground - g_ridge)
        depth = int(min(full, (apex_h - (ground - slope["hinge"])) * (1 - SLOPE) / SLOPE + 4)) if apex_h <= h_ridge else int(full)

    left, right, eave, apex = part["left"], part["right"], part["eave"], part["apex"]
    cx = (left + right) / 2
    # 지붕 면만 다시 그릴 캔버스: 점 x는 left − 1부터, 줄은 (apex − depth − 3)부터.
    ox, oy = left - 1, apex - depth - 3
    planes = Canvas(right - left + 3, eave - oy + 3)
    houses.gable_planes(planes, left - ox, right - ox, eave - oy, apex - oy, depth, part["material"], rng, lights=(0, 0))
    panel = model.panel(outline(planes.image), origin=(ox, oy), crop=False)
    h_edge, h_apex = ground - eave, ground - apex
    for edge in (left, right):
        model.face(panel, [(edge, ground, h_edge, edge, eave), (cx, ground, h_apex, cx, apex),
                           (cx, ground - depth, h_apex, cx, apex - depth), (edge, ground - depth, h_edge, edge, eave - depth)])
    if main:
        add_side_walls(model, depth, int(ground - rake_row(part, house.x0 - 1)), rng)


def add_flat(model, part, rng):
    """평지붕: 지붕판 윗면을 벽 꼭대기 높이에 눕히고 옆벽을 더한다(앞 두께는 앞벽 판에 있다)."""
    house = model.house
    layer = house.canvas.layers[part["layer"]]
    top_rows = np.where((house.canvas.yy < part["front"])[..., None], layer, 0).astype(np.uint8)
    panel = model.panel(outline(top_rows))
    (left, top), (w, _) = panel.origin, panel.size
    right, front = left + w, part["front"]
    h = house.ground - front
    model.face(panel, [(left, front + h, h, left, front), (right, front + h, h, right, front),
                       (right, top + h, h, right, top), (left, top + h, h, left, top)])
    add_side_walls(model, front - top, int(h), rng)


# ── 지붕 위에 선 것 ─────────────────────────────────────

def skirt(model, panel, x0, x1, row, g, h, sunk):
    """세운 판 아래로 그림 row 줄을 늘여 sunk점 더 내린 띠(기운 지붕 위에서 판 아래가 뜨지 않게)."""
    v = row + 0.5
    model.face(panel, [(x0, g, h - sunk, x0, v), (x1, g, h - sunk, x1, v), (x1, g, h, x1, v), (x0, g, h, x0, v)])


def add_stack(model, part):
    """굴뚝(돌·쇠): 지붕에 닿는 자리에 선 판과 옆면 둘, 위 테두리를 눕힌 윗면. 아래는 지붕 속까지 내린다."""
    house = model.house
    x, base, body, half = part["x"], part["base"], part["body"], part["half"]
    g, h = surface(house, x, base)
    panel = model.panel(outline(house.canvas.layers[part["layer"]]))
    (left, top), (w, _) = panel.origin, panel.size
    standing(model, panel, left, left + w, top, base, g, h)
    skirt(model, panel, x - half, x + half, base - 1, g, h, SUNK)
    depth = 2 * half
    h_top = h + base - body
    for side_x, column in ((x - half, x - half), (x + half, x + half - 1)):
        stretched_side(model, panel, side_x, column, body, base, g, h - SUNK, h_top, depth)
    model.face(panel, [(x - half, g, h_top, x - half, body + 2), (x + half, g, h_top, x + half, body + 2),
                       (x + half, g - depth, h_top, x + half, body - 3), (x - half, g - depth, h_top, x - half, body - 3)], 0.7)


def add_dormer(model, part, rng):
    """지붕창: 지붕 위 그 자리에 선 판, 옆벽 둘, 작은 맞배지붕 두 면(지붕 속으로 들어간 곳은 깊이 검사가 자른다)."""
    house = model.house
    x, base = part["x"], part["base"]
    g, h = surface(house, x, base)
    panel = model.panel(outline(house.canvas.layers[part["layer"]]))
    (left, top), (w, hgt) = panel.origin, panel.size
    standing(model, panel, left, left + w, top, top + hgt, g, h - (top + hgt - base))
    # houses.dormer: 벽은 x ± 12, 작은 지붕 처마는 base − 18줄(x ± 18), 꼭대기는 base − 36줄.
    for side_x in (x - 12, x + 12):
        stretched_side(model, panel, side_x, side_x - (side_x > x), base - 18, base, g, h - 4, h + 18, DORMER_DEPTH)
    if "dormer_roof" not in model.shared:
        model.shared["dormer_roof"] = model.panel(dormer_roof_panel(part["material"], DORMER_DEPTH, rng), crop=False)
    roof = model.shared["dormer_roof"]
    for edge, u in ((x - 18, 0), (x + 18, 36)):
        model.face(roof, [(edge, g, h + 18, u, DORMER_DEPTH), (x, g, h + 36, 18, DORMER_DEPTH),
                          (x, g - DORMER_DEPTH, h + 36, 18, 0), (edge, g - DORMER_DEPTH, h + 18, u, 0)])


def add_card(model, part):
    """바람개비처럼 얇은 것: 지붕 위 그 자리에 선 판 하나."""
    house = model.house
    g, h = surface(house, part["x"], part["base"])
    panel = model.panel(outline(house.canvas.layers[part["layer"]]))
    (left, top), (w, hgt) = panel.origin, panel.size
    standing(model, panel, left, left + w, top, top + hgt, g, h - (top + hgt - part["base"]))


def add_tower(model, part):
    """길드 탑: 몸통은 앞벽 줄에 선 상자, 뿔 지붕은 몸통 가운데 위로 모이는 네모뿔, 꼭대기 깃대는 그 위에 선 판."""
    house = model.house
    ground = house.ground
    left, width, body_top, cone = part["left"], part["width"], part["top"], part["cone"]
    panel = model.panel(outline(house.canvas.layers[part["layer"]]))
    base = body_top + 1                          # 뿔 지붕 아래 테두리 밑줄
    standing(model, panel, left - 4, left + width + 4, base, ground + 1, ground, -1)
    depth = width + 2
    for side_x, column in ((left - 1, left), (left + width + 1, left + width - 1)):
        stretched_side(model, panel, side_x, column, base, ground, ground, 0, ground - base, depth)
    h_base = ground - base
    middle = ground - depth // 2
    cx = left + width / 2
    apex = (cx, middle, h_base + cone + 1, cx, body_top - cone)
    front_l, front_r = (left - 7, ground + 7, h_base, left - 7, base), (left + width + 7, ground + 7, h_base, left + width + 7, base)
    back_g = middle - (ground + 7 - middle)
    model.face(panel, [front_l, front_r, apex])
    model.face(panel, [(left - 7, back_g, h_base, left - 7, base), (left - 7, ground + 7, h_base, cx, base), apex])
    model.face(panel, [(left + width + 7, ground + 7, h_base, cx, base), (left + width + 7, back_g, h_base, left + width + 7, base), apex])
    flag_top = body_top - cone - 19
    standing(model, panel, cx - 3, cx + 18, flag_top, body_top - cone + 1, middle, h_base + cone)


# ── 모형에만 있는 면 그리기 ──────────────────────────────

def side_wall(design, depth, height, rng):
    """그림에 없는 옆벽: 앞벽과 같은 재료로 층마다 칠하고, 깊으면 층마다 창 하나, 아래 돌 받침, 위 처마 그늘."""
    c = Canvas(depth, height)
    for story in range(design.stories):
        kind = design.upper if story > 0 and design.upper else design.walls
        y1 = height - story * STORY
        y0 = y1 - STORY if story < design.stories - 1 else 0
        c.paint(c.rect(0, y0, depth, y1), houses.wall_texture(kind, height, depth, rng))
        if kind == "timber" and y1 - y0 >= STORY:
            houses.timber_frame(c, 0, y1 - STORY, depth, STORY)
    if depth >= 64:
        for story in range(design.stories):
            houses.window(c, depth // 2, height - (story + 1) * STORY + 16, design, wide=20, tall=26,
                          arched=design.arched_windows and story == 0)
    if design.walls == "adobe":
        c.paint(c.rect(0, height - PLINTH, depth, height), ADOBE[2])
    else:
        c.paint(c.rect(0, height - PLINTH, depth, height), STONE[3])
        c.paint(c.rect(0, height - PLINTH, depth, height - PLINTH + 1), STONE[5])
        c.paint(c.rect(0, height - 1, depth, height), STONE[1])
    shade = np.clip(0.5 + c.yy * 0.045, 0.5, 1.0)
    c.image[..., :3] = (c.image[..., :3] * shade[..., None]).astype(np.uint8)
    return c.image


def hip_panel(material, span, slant, rng):
    """지붕 끝 세모 박공: 아래 처마가 span점, 꼭대기는 가운데."""
    c = Canvas(span, slant)
    face = c.polygon([(0, slant), (span, slant), (span / 2, 0)])
    cover(c, face, material, 0, slant, rng)
    c.paint(c.rect(0, slant - 3, span, slant) & face, ROOFS[material][1])
    return outline(c.image)


def dormer_roof_panel(material, depth, rng):
    """지붕창의 작은 맞배지붕 두 면(왼쪽 18점, 오른쪽 18점)."""
    c = Canvas(36, depth)
    cover(c, c.rect(0, 0, 36, depth), material, 0, depth, rng)
    return c.image
