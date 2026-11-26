"""물건·구조물의 HD-2D 모형: 세운 그림 한 장에서 윗면을 눕히고 앞면을 세우고 두께를 준다(집은 house_model).

물건마다 어느 줄까지가 윗면인지(SPECS)만 적으면, 맵 생성기(layout.upright)가 이름을 보고 모형을 만든다.
  - block: 상자. back~front 줄은 눕는 윗면, 그 아래는 앞면, back 위로 솟은 것(등받이·책상 위 물건)은 윗면 뒤 끝에 선다.
  - heap: 쌓은 상자. 열마다 맨 위 칠한 줄부터 depth줄이 윗면이다(계단 모양이 그대로 살아난다).
  - drum: 둥근 통. 윗면은 타원이고 앞면은 그 앞 호를 따라 휜다.
  - pedestal: 외다리 둥근 탁자.
  - lathe: 돌려 깎은 것(탑·등대·기둥). 줄마다 그림 너비를 지름으로 보고 앞 반쪽을 둥글게 휜다. 뿔 지붕은 가운데로 모인다.
  - slab: 윗면을 그리지 않은 것(책장·성문·동굴 입구). 그림 실루엣을 뒤로 depth점 두껍게 한다.
  - roofed: 지붕을 접는 가판대·매표소.
  - armchair·bunk·side_chair·cart·statue·potted·floating: 그 물건만의 도형(안락의자, 이층 침대, 옆을 보는 의자, 수레, 받침 위 조각상, 화분, 떠 있는 땅).
  - fence·rail_beam: 남북으로 뻗은 가로대가 그림에서는 세로 띠라, 들보로 다시 눕힌다.
꽃·등불·깃발처럼 얇거나 잘게 얽힌 것은 모형을 만들지 않고 판으로 둔다.
좌표와 규칙은 picture_model과 같다. ground는 그림에서 발밑이 닿는 줄이다.
"""
import numpy as np

import pixelgen as pg
from picture_model import Model, standing, stretched_side

ARC = 14              # 둥근 앞면을 나누는 열 수


# ── 도형 ──────────────────────────────────────────────

def block(m, image, ground, back, front, apron=None, stand=(), depth=None):
    """
    상자. apron을 주면 다리 달린 것(탁자·긴 의자): 앞면 위 apron줄만 옆판이 있고, 그 아래 다리는 뒤에도 한 벌 더 선다.
    stand는 윗면 위에 따로 세울 것의 네모(x0, top, x1, base). depth를 주면 윗면을 그린 줄 수보다 깊게 늘인다(거의 앞에서 본 그림).
    """
    panel, left, right, top, bottom = frame(m, image)
    h = ground - front
    depth = depth or front - back
    if top < back:
        standing(m, panel, left, right, top, back, ground - depth, h)
    stretched_top(m, panel, left, right, back, front, ground, h, depth)
    standing(m, panel, left, right, front, bottom, ground, ground - bottom)
    foot = min(bottom, front + apron) if apron else bottom
    edge_l, edge_r = edges(image, front, foot)
    sides(m, panel, edge_l, edge_r, front, foot, ground, depth)
    if apron:
        standing(m, panel, left, right, foot, bottom, ground - depth, ground - bottom, 0.8)
    stand_up(m, panel, stand, h)


def heap(m, image, ground, depth):
    """쌓은 상자: 열마다 맨 위 칠한 줄부터 depth줄을 눕힌다. 높이가 달라지는 곳은 옆면으로 메운다."""
    panel, left, right, top, bottom = frame(m, image)
    for x0, x1, first in column_runs(image):
        h = ground - (first + depth)
        flat(m, panel, x0, x1, first, first + depth, h)
        standing(m, panel, x0, x1, first + depth, bottom, ground, ground - bottom)
        for x, column in ((x0, x0 + 1), (x1, x1 - 2)):
            stretched_side(m, panel, x, column, first + depth, bottom, ground, 0, h, depth)


def drum(m, image, ground, back, front, above="back", stand=()):
    """둥근 통: 윗면 타원(back~front)을 눕히고 앞면을 타원 앞 호를 따라 휜다. above는 위로 솟은 것이 설 자리(back · center)."""
    panel, left, right, top, bottom = frame(m, image)
    middle, ry = (back + front) / 2, (front - back) / 2
    edge_l, edge_r = edges(image, int(middle), int(middle) + 1)
    cx, rx = (edge_l + edge_r) / 2, (edge_r - edge_l) / 2
    h = ground - front

    def rim(x):
        return middle + ry * np.sqrt(max(0.0, 1 - ((x - cx) / rx) ** 2))

    if top < back:
        g = (middle if above == "center" else back) + h
        standing(m, panel, left, right, top, back, g, h)
    for x0, x1 in strips(left, right):
        e0, e1 = rim(x0), rim(x1)
        m.face(panel, [(x0, e0 + h, h, x0, e0), (x1, e1 + h, h, x1, e1), (x1, back + h, h, x1, back), (x0, back + h, h, x0, back)])
        m.face(panel, [(x0, e0 + h, h - (bottom - e0), x0, bottom), (x1, e1 + h, h - (bottom - e1), x1, bottom),
                       (x1, e1 + h, h, x1, e1), (x0, e0 + h, h, x0, e0)])
    stand_up(m, panel, stand, h)


def open_boat(m, image, ground, back, front):
    """작은 배: 옆으로 뜬 배는 뱃전이 휘는 통이고, 남북으로 뜬 배(위에서 본 그림)는 물 위에 눕힌다."""
    panel_height, panel_width = image.shape[:2]
    if panel_height > panel_width:
        panel, left, right, top, bottom = frame(m, image)
        flat(m, panel, left, right, top, bottom, 4)
        return
    drum(m, image, ground, back, front)


def pedestal(m, image, ground, back, front):
    """외다리 둥근 탁자: 타원 상판을 눕히고, 다리는 상판 가운데 아래에 선다."""
    panel, left, right, top, bottom = frame(m, image)
    foot = ground - 2
    h = foot - (back + front) / 2
    if top < back:
        standing(m, panel, left, right, top, back, back + h, h)
    flat(m, panel, left, right, back, front, h)
    # 상판에 가려 그림에 없는 다리 윗부분은 다리 맨 윗줄을 늘여 잇는다.
    m.face(panel, [(left, foot, foot - front, left, front + 1.5), (right, foot, foot - front, right, front + 1.5),
                   (right, foot, h, right, front + 1.5), (left, foot, h, left, front + 1.5)], 0.8)
    standing(m, panel, left, right, front, bottom, foot, foot - bottom)


def lathe(m, image, ground, squash=1.0, radius=None, start=None, center=None, step=None, end=None, lift=0, lid=True):
    """
    돌려 깎은 것: 줄마다 그림 너비의 반을 반지름으로 보고 앞 반쪽을 휜다. radius는 몸통 반지름의 위 한도(감긴 뱀처럼 밖으로 나온 것은 옆에 붙는다).
    start 위의 것(깃대·머리)은 가운데에 세운다. squash는 앞뒤로 눌린 정도.
    end 줄까지만 깎는다(그 아래는 따로 만든 받침). lift는 그만큼 높이 올린다(받침 위에 얹은 것).
    """
    # 가운데를 정해 주지 않았으면 줄마다 칠한 폭의 가운데를 축으로 삼아 비뚤어진 윤곽(감긴 뱀·금더미)도 그대로 따른다.
    straight = radius is not None or center is not None
    panel, left, right, top, bottom = frame(m, without_outline(image if straight else filled_rows(image)))
    start = top if start is None else start
    bottom = bottom if end is None else end
    # 작은 것은 줄을 촘촘히 나눈다. 성기면 윤곽과 그림이 어긋나 가장자리가 톱니처럼 빈다.
    step = step or (2 if bottom - start <= 120 else 4)
    painted = image[..., 3] > 0
    axis = (left + right) / 2 if center is None else center

    def ring(row):
        """그 줄의 (축, 반지름)."""
        columns = np.nonzero(painted[min(row, bottom - 1)])[0]
        if len(columns) == 0:
            return axis, 0.0
        first, last = columns.min(), columns.max() + 1
        if not straight:
            return (first + last) / 2, (last - first) / 2
        r = max(abs(first - axis), abs(last - axis))
        return axis, min(r, radius) if radius else r

    rows = list(range(start, bottom, step)) + [bottom]
    rings = [ring(row) for row in rows]
    widest = max(r for _, r in rings)
    # 꼭짓점이 줄마다의 윤곽을 따라가게 폭에 견준 자리(-1~1)로 나눈다. 그림 열로 나누면 윤곽이 칸마다 꺾여 톱니가 진다.
    turns = [float(-np.cos(np.pi * k / ARC)) for k in range(ARC + 1)]

    def corner(turn, row, c, r):
        # 윤곽은 점 계단이라 그림을 한 점 안쪽에서 읽어야 가장자리가 비지 않는다.
        inner = r - min(1.2, r / 2)
        return c + turn * r, ground - squash * (widest - r * np.sqrt(max(0.0, 1 - turn * turn))), ground - row + lift, c + turn * inner, row

    for (r0, (c0, w0)), (r1, (c1, w1)) in zip(zip(rows, rings), zip(rows[1:], rings[1:])):
        for t0, t1 in zip(turns, turns[1:]):
            m.face(panel, [corner(t0, r1, c1, w1), corner(t1, r1, c1, w1), corner(t1, r0, c0, w0), corner(t0, r0, c0, w0)])
    c, r = rings[0]
    if r > 2 and lid:
        # 꼭대기가 넓으면(바위·항아리) 뚜껑을 덮는다. 맨 윗줄은 외곽선이라 그 아래 줄의 색이다.
        edge = [(t, 1) for t in turns] + [(t, -1) for t in reversed(turns[1:-1])]
        m.face(panel, [(c + t * r, ground - squash * (widest - side * r * np.sqrt(max(0.0, 1 - t * t))), ground - start + lift,
                        c + t * r * 0.7, min(start + 2.5, bottom - 0.5)) for t, side in edge])
    if radius:
        # 몸통 밖으로 나온 것(감긴 뱀)은 몸통 옆 가운데 깊이에 선 판이다.
        for x0, x1 in ((left, axis - radius), (axis + radius, right)):
            if x1 > x0:
                standing(m, panel, x0, x1, start, bottom, ground - squash * widest, ground - bottom + lift)
    if top < start:
        standing(m, panel, left, right, top, start, ground - squash * widest, ground - start + lift)


def slab(m, image, ground, depth):
    """실루엣을 뒤로 depth점 두껍게: 앞면을 세우고, 위·옆 가장자리(문구멍 안쪽도)를 가장자리 점의 색으로 늘여 메운다."""
    panel, left, right, top, bottom = frame(m, image)
    standing(m, panel, left, right, top, bottom, ground, ground - bottom)
    painted = np.pad(image[..., 3] > 0, 1)
    body = painted[1:-1, 1:-1]
    # 가장자리 한 점은 검은 외곽선이라 그 안쪽 점의 색을 늘인다.
    for x0, x1, row in horizontal_runs(body & ~painted[:-2, 1:-1]):
        v = min(row + 1.5, bottom - 0.5)
        m.face(panel, [(x0, ground, ground - row, x0, v), (x1, ground, ground - row, x1, v),
                       (x1, ground - depth, ground - row, x1, v), (x0, ground - depth, ground - row, x0, v)])
    for edge, shift, inner in ((body & ~painted[1:-1, :-2], 0, 1), (body & ~painted[1:-1, 2:], 1, -1)):
        for r0, r1, column in horizontal_runs(edge.T):
            stretched_side(m, panel, column + shift, column + inner, r0, r1, ground, ground - r1, ground - r0, depth)


def roofed(m, image, ground, ridge, hinge, counter=None, slope=0.5):
    """지붕을 접는 것: hinge 줄 위 지붕은 뒤로 눕고 뒤 지붕이 이어진다. counter(back, front)는 앞에 눕는 계산대 윗면이다."""
    panel, left, right, top, bottom = frame(m, image)
    h = ground - hinge
    run, rise = (hinge - ridge) * (1 - slope), (hinge - ridge) * slope
    for eave in (ground, ground - 2 * run):
        m.face(panel, [(left, eave, h, left, hinge), (right, eave, h, right, hinge),
                       (right, ground - run, h + rise, right, ridge), (left, ground - run, h + rise, left, ridge)])
    if top < ridge:
        standing(m, panel, left, right, top, ridge, ground - run, h + rise)
    if counter:
        back, front = counter
        shelf = ground - front
        # 처마 천은 앞에, 그 아래 가게 안은 계산대 뒤에 선다.
        standing(m, panel, left, right, hinge, back, back + shelf, shelf)
        flat(m, panel, left, right, back, front, shelf)
        standing(m, panel, left, right, front, bottom, ground, ground - bottom)
        body = front
    else:
        standing(m, panel, left, right, hinge, bottom, ground, ground - bottom)
        body = hinge
    # 옆판은 지붕 아래 몸통의 양끝이다(처마는 몸통보다 넓다).
    middle = (body + bottom) // 2
    edge_l, edge_r = edges(image, middle, middle + 1)
    sides(m, panel, edge_l, edge_r, body, bottom, ground, 2 * run)


def mess_hall_table(m, image, ground):
    """긴 식탁과 앞뒤 긴 의자(그림 한 장에 셋이 있다): 의자 둘과 식탁을 따로 세운다. 높이는 그림 줄이 아니라 물건 크기로 정한다."""
    panel, left, right, top, bottom = frame(m, image)
    seat, table = 10, 20
    front_bench, body, back_bench = ground, ground - 12, ground - 12 - 24
    for g, rise, cut_top, cut_front, cut_bottom in ((back_bench, seat, top, 9, 12), (body, table, 14, 34, 43), (front_bench, seat, 43, 48, bottom)):
        depth = cut_front - cut_top
        m.face(panel, [(left, g, rise, left, cut_front), (right, g, rise, right, cut_front),
                       (right, g - depth, rise, right, cut_top), (left, g - depth, rise, left, cut_top)])
        standing(m, panel, left, right, cut_front, cut_bottom, g, rise - (cut_bottom - cut_front))


def rampart(m, image, ground, back, front, merlon, gap):
    """앞을 보는 성벽: 통로(back~front)를 눕히고 앞면을 세우며, 머론은 앞 가장자리에 하나씩 선다."""
    right = image.shape[1]
    block(m, image, ground, back, front, stand=[(x, front - 15, min(x + merlon, right), front) for x in range(0, right, merlon + gap)])


def rampart_walk(m, image, ground, height):
    """남북으로 달리는 성벽 한 칸: 그린 통로를 성벽 높이에 눕히고, 양옆 면을 가장자리 돌 색으로 땅까지 내린다."""
    panel, left, right, top, bottom = frame(m, image)
    walk = bottom - 32
    m.face(panel, [(left, ground, height, left, bottom - 1), (right, ground, height, right, bottom - 1),
                   (right, ground - 32, height, right, walk), (left, ground - 32, height, left, walk)])
    for x, column in ((left, left + 3), (right, right - 4)):
        stretched_side(m, panel, x, column, walk + 2, bottom - 2, ground, 0, height, 32)
    m.face(panel, [(left, ground, 0, left + 3, bottom - 3), (right, ground, 0, left + 6, bottom - 3),
                   (right, ground, height, left + 6, bottom - 8), (left, ground, height, left + 3, bottom - 8)])
    for x0, x1 in ((left, left + 10), (right - 10, right)):
        standing(m, panel, x0, x1, top, walk + 4, ground - 16, height)


def crowned_tower(m, image, ground, back, front):
    """머론 띠를 두른 둥근 망루: 몸통은 돌려 깎고, 꼭대기 통로는 눕히고, 머론은 앞에 선다."""
    panel, left, right, top, bottom = frame(m, image)
    h = ground - front
    flat(m, panel, left, right, back, front, h)
    lathe(m, np.where(np.arange(image.shape[0])[:, None, None] >= front, image, 0).astype(np.uint8), ground, squash=(front - back) / (right - left) * 2)
    stand_up(m, panel, [(x, front - 15, min(x + 9, right), front) for x in range(left + 1, right, 16)], h)


def ship(m, hull, masts, ground, south, side):
    """
    물에 뜬 배: 갑판은 바깥 뱃전 높이(side)에 눕고, 뱃전은 갑판 남쪽 가장자리(열마다 south 줄)를 따라 휘며, 돛대는 갑판 위에 선다.
    hull은 돛대 없는 배 그림, masts는 [(그림, 왼쪽, 위, 갑판에 닿는 줄)].
    """
    panel, left, right, top, bottom = frame(m, hull)
    columns = [x for x in range(left, right) if south[x] is not None]
    bow, stern = columns[0], columns[-1] + 1

    def rim(x):
        return south[int(min(max(x, bow), stern - 1))]

    for x0, x1 in strips(bow, stern, 28):
        e0, e1 = rim(x0), rim(x1)
        m.face(panel, [(x0, e0 + side, side, x0, e0), (x1, e1 + side, side, x1, e1), (x1, top + side, side, x1, top), (x0, top + side, side, x0, top)])
        m.face(panel, [(x0, e0 + side, 0, x0, e0 + side), (x1, e1 + side, 0, x1, e1 + side), (x1, e1 + side, side, x1, e1), (x0, e0 + side, side, x0, e0)])
    # 갑판 밖으로 나온 이물대는 갑판 높이에 눕는다.
    for x0, x1 in ((left, bow), (stern, right)):
        if x1 > x0:
            flat(m, panel, x0, x1, top, bottom - side, side)
    for image, x, y, deck in masts:
        sail = m.panel(image, origin=(x, y))
        (sx, sy), (w, hgt) = sail.origin, sail.size
        standing(m, sail, sx, sx + w, sy, sy + hgt, deck + side, side + deck - (sy + hgt))


def armchair(m, image, ground, seat, arm, inner, depth=24):
    """
    안락의자(앞에서 본 그림): 등받이는 뒤에, 팔걸이는 양옆에 서고 앉는 자리는 뒤로 눕는다.
    seat는 방석 윗면의 (뒤, 앞) 줄, arm은 팔걸이 윗줄, inner는 팔걸이 안쪽 열(왼쪽, 오른쪽).
    """
    panel, left, right, top, bottom = frame(m, image)
    back_row, front_row = seat
    (x0, x1), h = inner, ground - front_row
    rear = ground - depth + 5
    standing(m, panel, left, right, top, back_row, rear, h)
    edge_l, edge_r = edges(image, top + 4, back_row - 6)
    sides(m, panel, edge_l, edge_r, top + 1, back_row, rear, 5, h)
    stretched_top(m, panel, edge_l + 2, edge_r - 2, top + 2, top + 4, rear, h + back_row - top - 1, 5)
    stretched_top(m, panel, x0, x1, back_row, front_row, ground, h, depth - 5)
    standing(m, panel, x0, x1, front_row, bottom, ground, ground - bottom)
    for a0, a1, wall in ((left, x0, x0), (x1, right, x1)):
        standing(m, panel, a0, a1, arm, bottom, ground, ground - bottom)
        stretched_top(m, panel, a0, a1, arm + 1, arm + 3, ground, ground - arm, depth)
        stretched_side(m, panel, wall, a0 + 2 if wall == x1 else a1 - 3, arm + 1, front_row, ground, h, ground - arm, depth)
        outer = left if wall == x0 else right
        stretched_side(m, panel, outer, outer + 2 if wall == x0 else outer - 3, arm + 1, bottom - 5, ground, ground - bottom + 5, ground - arm, depth)


def bunk(m, image, ground, decks, post, depth=34):
    """이층 침대(옆에서 본 그림): 앞판과 뒤판 사이에 요가 눕는다. decks는 요 윗면의 (위, 아래) 줄, post는 기둥 너비."""
    panel, left, right, top, bottom = frame(m, image)
    standing(m, panel, left, right, top, bottom, ground, ground - bottom)
    standing(m, panel, left, right, top, bottom, ground - depth, ground - bottom, 0.75)
    for row0, row1 in decks:
        stretched_top(m, panel, left + post, right - post, row0, row1, ground - 1, ground - row0, depth - 2)
    for x, column in ((left, left + 2), (right, right - 3)):
        for rail_row in [row for row, _ in decks] + [top + 1]:
            stretched_side(m, panel, x, column, rail_row, rail_row + 10, ground, ground - rail_row - 10, ground - rail_row, depth)


def side_chair(m, image, ground, seat, back_x, depth=14):
    """옆을 보는 의자: 앞뒤 다리 틀 두 장 사이에 앉는 자리와 등받이가 있다. seat는 앉는 자리 줄, back_x는 등받이 열(왼쪽, 오른쪽)."""
    panel, left, right, top, bottom = frame(m, image)
    for g, shade in ((ground, 1.0), (ground - depth, 0.75)):
        standing(m, panel, left, right, top, bottom, g, ground - bottom, shade)
    stretched_top(m, panel, left, right, seat, seat + 3, ground, ground - seat, depth)
    for x, column in ((back_x[0], back_x[0] + 2), (back_x[1], back_x[1] - 2)):
        stretched_side(m, panel, x, column, top + 1, seat, ground, ground - seat, ground - top - 1, depth)


def cart(m, image, ground, barrel, box, wheels, body, depth=24):
    """
    수레(옆에서 본 그림): 짐칸은 깊은 상자이고, 바퀴는 앞뒤로 한 벌씩, 통은 짐칸 가운데에 눕는다.
    barrel은 통의 네모(x0, top, x1, 짐칸에 닿는 줄), box는 짐칸의 (윗줄, 아랫줄), wheels는 바퀴 윗줄, body는 짐칸의 (왼쪽, 오른쪽) 열.
    """
    panel, left, right, top, bottom = frame(m, image)
    (box_top, box_bottom), (x0, x1) = box, body
    standing(m, panel, left, right, box_top, bottom, ground, ground - bottom)
    standing(m, panel, left, right, wheels, bottom, ground - depth, ground - bottom, 0.7)
    standing(m, panel, x0, x1, box_top, box_bottom, ground - depth, ground - box_bottom, 0.8)
    stretched_top(m, panel, x0, x1, box_top + 1, box_top + 3, ground, ground - box_top - 4, depth)
    sides(m, panel, x0, x1, box_top, box_bottom, ground, depth)
    bx0, barrel_top, bx1, barrel_base = barrel
    middle = ground - depth / 2
    turns = np.linspace(0, np.pi, 8)
    # 통은 동서로 누운 원기둥이라 남북으로 휜다.
    radius = (barrel_base - barrel_top) / 2 + 3
    for t0, t1 in zip(turns, turns[1:]):
        rows = [barrel_top + 1 + (barrel_base - barrel_top - 2) * t / np.pi for t in (t0, t1)]
        points = [(middle - radius * np.cos(t), ground - box_top - 4 + 1.5 * radius * np.sin(t) * 0.8) for t in (t0, t1)]
        m.face(panel, [(bx0, points[1][0], points[1][1], bx0 + 1, rows[1]), (bx1, points[1][0], points[1][1], bx1 - 1, rows[1]),
                       (bx1, points[0][0], points[0][1], bx1 - 1, rows[0]), (bx0, points[0][0], points[0][1], bx0 + 1, rows[0])])
    standing(m, panel, bx0, bx1, barrel_top, barrel_base, middle, ground - box_top - 4, 0.9)


def statue(m, image, ground, figure, front, depth=26):
    """받침 위 조각상: 받침은 깊은 상자이고(figure~front 줄이 윗면), 조각상은 받침 가운데에 둥글게 깎아 세운다."""
    panel, left, right, top, bottom = frame(m, image)
    h = ground - front
    edge_l, edge_r = edges(image, front, bottom)
    stretched_top(m, panel, edge_l + 1, edge_r - 1, front, front + 1, ground, h, depth)
    standing(m, panel, left, right, front, bottom, ground, ground - bottom)
    sides(m, panel, edge_l, edge_r, front, bottom, ground, depth)
    body = np.where(np.arange(image.shape[0])[:, None, None] < figure, image, 0).astype(np.uint8)
    lathe(m, body, ground - 4, squash=0.75, end=figure, lift=h - (ground - 4 - figure))


def potted(m, image, ground, pot):
    """화분: 화분은 둥글게 깎고, 풀은 가운데에서 세 방향으로 엇갈려 세운다."""
    lower = np.where(np.arange(image.shape[0])[:, None, None] >= pot, image, 0).astype(np.uint8)
    lathe(m, lower, ground, squash=0.8)
    panel, left, right, top, bottom = frame(m, image)
    cx, middle = (left + right) / 2, ground - 0.4 * (right - left)
    half = (right - left) / 2
    for turn in (0.0, np.pi / 3, -np.pi / 3):
        dx, dg = half * np.cos(turn), half * np.sin(turn)
        rows = (top, pot + 2)
        m.face(panel, [(cx - dx, middle - dg, ground - rows[1], left, rows[1]), (cx + dx, middle + dg, ground - rows[1], right, rows[1]),
                       (cx + dx, middle + dg, ground - rows[0], right, rows[0]), (cx - dx, middle - dg, ground - rows[0], left, rows[0])])


def floating(m, image, ground, surface, end, shadow):
    """허공에 뜬 땅 조각: 풀 덮인 윗면은 눕고 흙은 아래로 뾰족하게 깎인다. 땅에는 그림자가 눕는다. surface는 윗면의 (뒤, 앞) 줄."""
    panel, left, right, top, bottom = frame(m, image)
    back, front = surface
    center = (shadow + bottom) / 2
    h = center - (back + front) / 2
    m.face(panel, [(left, front + h, h, left, front), (right, front + h, h, right, front), (right, back + h, h, right, back), (left, back + h, h, left, back)])
    rock = np.where((np.arange(image.shape[0])[:, None, None] >= front) & (np.arange(image.shape[0])[:, None, None] < end), image, 0).astype(np.uint8)
    squash = (front - back) / (right - left)
    reach = squash * (right - left) / 2
    lathe(m, rock, center + reach, squash=squash, end=end, lid=False)
    flat(m, panel, left, right, shadow, bottom, 0.5)


def lying(m, image, ground, h=3):
    """땅에 놓인 납작한 것(모닥불 자리, 비늘판): 그림 그대로 눕힌다."""
    panel, left, right, top, bottom = frame(m, image)
    flat(m, panel, left, right, top, bottom, h)


def bunch(m, image, ground, cuts):
    """나란히 선 둥근 것들(항아리): cuts로 나눈 열 구간마다 따로 깎는다."""
    columns = np.arange(image.shape[1])[None, :, None]
    for x0, x1 in zip(cuts, cuts[1:]):
        lathe(m, np.where((columns >= x0) & (columns < x1), image, 0).astype(np.uint8), ground, squash=0.9)


def rail_beam(m, image, ground, post=22):
    """남북으로 이어진 난간 한 칸: 그림에서 세로 띠인 들보를 기둥 높이(post)에 눕히고, 기둥은 남쪽 끝에 선다."""
    panel, left, right, top, bottom = frame(m, image)
    front = ground - post
    if top < front - 4:
        flat(m, panel, left, right, top, front, post)
        edge_l, edge_r = edges(image, top + 1, top + 2)
        for x, column in ((edge_l, edge_l + 1), (edge_r, edge_r - 2)):
            stretched_side(m, panel, x, column, top + 1, top + 5, ground, post - 4, post, front - top)
    if bottom > front + 4:
        post_top = max(top, front)
        standing(m, panel, left, right, post_top, bottom, ground, ground - bottom)
        edge_l, edge_r = edges(image, post_top + 3, bottom)
        sides(m, panel, edge_l, edge_r, post_top, bottom, ground, edge_r - edge_l - 1)


def fence(mask, pivot):
    """나무 울타리 한 칸(objects.fence와 같은 자리): 기둥 하나와, 이어진 쪽(왼 1, 오 2, 위 4, 아래 8)으로 뻗는 가로대 둘."""
    cx, ground = pivot
    m = Model(foot=tuple(pivot), pivot=tuple(pivot))
    wood = np.zeros((8, 16, 4), dtype=np.uint8)
    wood[..., 3] = 255
    wood[..., :3] = pg.WOOD[2]
    wood[:2, :, :3] = pg.WOOD[4]
    wood[2:4, :, :3] = pg.WOOD[3]
    wood[7, :, :3] = pg.WOOD[1]
    panel = m.panel(wood, crop=False)
    half = pg.TILE // 2
    box(m, panel, cx - 2.5, cx + 2.5, ground - 5, ground, 0, 20)
    for height in (14, 7):
        for bit, x0, x1 in ((1, cx - half, cx - 2), (2, cx + 2, cx + half)):
            if mask & bit:
                box(m, panel, x0, x1, ground - 4, ground - 2, height - 3, height)
        # 남쪽 가로대는 남쪽 칸의 울타리가 북쪽으로 뻗어 채운다.
        if mask & 4:
            box(m, panel, cx - 1, cx + 1, ground - pg.TILE, ground - 5, height - 3, height)
    return m


# ── 도형을 짜는 조각 ─────────────────────────────────────

def frame(m, image):
    """그림을 패널로 넣고 칠한 범위(왼쪽, 오른쪽, 위, 아래)를 돌려준다."""
    panel = m.panel(image)
    (left, top), (w, h) = panel.origin, panel.size
    return panel, left, left + w, top, top + h


def flat(m, panel, x0, x1, top, bottom, h):
    """그림 top~bottom 줄을 높이 h에 눕힌다."""
    m.face(panel, [(x0, bottom + h, h, x0, bottom), (x1, bottom + h, h, x1, bottom), (x1, top + h, h, x1, top), (x0, top + h, h, x0, top)])


def stretched_top(m, panel, x0, x1, top, bottom, g, h, depth):
    """그림 top~bottom 줄을 앞 가장자리 g에서 뒤로 depth점 늘여 높이 h에 눕힌다."""
    m.face(panel, [(x0, g, h, x0, bottom), (x1, g, h, x1, bottom), (x1, g - depth, h, x1, top), (x0, g - depth, h, x0, top)])


def box(m, panel, x0, x1, back, front, low, high):
    """나무 무늬(위 두 줄이 윗면, 나머지가 옆면)를 입힌 상자. 카메라에서 보이는 윗면·앞면·양옆만 만든다."""
    w, h = panel.size
    m.face(panel, [(x0, front, high, 0.5, 2), (x1, front, high, w - 0.5, 2), (x1, back, high, w - 0.5, 0.5), (x0, back, high, 0.5, 0.5)])
    for (ax, ag), (bx, bg) in (((x0, front), (x1, front)), ((x0, back), (x0, front)), ((x1, back), (x1, front))):
        m.face(panel, [(ax, ag, low, 0.5, h - 0.5), (bx, bg, low, w - 0.5, h - 0.5), (bx, bg, high, w - 0.5, 2.5), (ax, ag, high, 0.5, 2.5)])


def sides(m, panel, edge_l, edge_r, top, bottom, ground, depth, lift=0):
    """앞면 양끝에서 북쪽으로 depth점 옆판. 가장자리 한 점은 외곽선이라 그 안쪽 열을 늘인다. lift는 아래 끝의 높이(0이면 앞면 그림 그대로)."""
    low = lift if lift else ground - bottom
    for x, column in ((edge_l, edge_l + 1), (edge_r, edge_r - 2)):
        stretched_side(m, panel, x, column, top, bottom, ground, low, low + bottom - top, depth)


def stand_up(m, panel, rects, h):
    """높이 h인 윗면 위에 그림의 네모(x0, top, x1, base)를 세운다."""
    for x0, top, x1, base in rects:
        standing(m, panel, x0, x1, top, base, base + h, h)


def edges(image, top, bottom):
    """그 줄들에서 칠한 왼쪽 끝과 오른쪽 끝."""
    columns = np.nonzero((image[top:bottom, :, 3] > 0).any(axis=0))[0]
    return int(columns.min()), int(columns.max()) + 1


def strips(left, right, count=ARC):
    """left~right를 나눈 열 구간들."""
    xs = np.linspace(left, right, count + 1)
    return list(zip(xs, xs[1:]))


def filled_rows(image):
    """줄마다 칠한 곳 사이의 빈 점을 왼쪽 이웃 색으로 메운 그림(둥글게 깎으면 그 틈이 구멍으로 보인다)."""
    out = image.copy()
    for row in out:
        columns = np.nonzero(row[:, 3])[0]
        if len(columns):
            for x in range(columns[0] + 1, columns[-1]):
                if not row[x, 3]:
                    row[x] = row[x - 1]
    return out


def without_outline(image):
    """
    둘레 외곽선을 그 안쪽 색으로 덮은 그림. 돌려 깎은 것의 둥근 윗부분은 거의 눕기 때문에,
    한 점 굵기 외곽선을 그대로 두면 뒤로 길게 늘어나 검은 털처럼 보인다.
    """
    out = image.copy()
    for _ in range(2):
        painted = np.pad(out[..., 3] > 0, 1)
        edge = painted[1:-1, 1:-1] & ~(painted[:-2, 1:-1] & painted[1:-1, :-2] & painted[1:-1, 2:])
        dark = out[..., :3].astype(int).sum(axis=2) < 120
        for y, x in zip(*np.nonzero(edge & dark)):
            inward = 1 if x < out.shape[1] / 2 else -1
            for dy, dx in ((2, 0), (2, 2 * inward), (0, 2 * inward), (3, 3 * inward)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < out.shape[0] and 0 <= xx < out.shape[1] and out[yy, xx, 3] and not dark[yy, xx]:
                    out[y, x, :3] = out[yy, xx, :3]
                    break
    return out


def column_runs(image):
    """맨 위 칠한 줄이 같은 열들의 구간 (x0, x1, 줄). 외곽선·거스러미로 두 점까지 어긋난 열은 이웃에 붙인다."""
    painted = image[..., 3] > 0
    runs = []
    for x in range(painted.shape[1]):
        if not painted[:, x].any():
            continue
        top = int(np.nonzero(painted[:, x])[0].min())
        if runs and runs[-1][1] == x and abs(runs[-1][2] - top) <= 2:
            runs[-1] = (runs[-1][0], x + 1, min(runs[-1][2], top))
        else:
            runs.append((x, x + 1, top))
    return runs


def horizontal_runs(mask):
    """참인 점이 가로로 이어진 구간 (x0, x1, 줄)."""
    runs = []
    for row in range(mask.shape[0]):
        columns = np.nonzero(mask[row])[0]
        for group in np.split(columns, np.nonzero(np.diff(columns) > 1)[0] + 1):
            if len(group):
                runs.append((int(group[0]), int(group[-1]) + 1, row))
    return runs


# ── 물건마다의 도형 ─────────────────────────────────────

BENCH = (block, dict(back=20, front=25, apron=3))
SHELF = (slab, dict(depth=14))
PILE = (heap, dict(depth=7))
CHEST = (block, dict(back=4, front=15))
TEA = (block, dict(back=15, front=28, apron=2))
DESK_WITH_LEGS = (block, dict(back=15, front=31, apron=2))
ROUND_TABLE = (pedestal, dict(back=7, front=26))
WRECK = (slab, dict(depth=16))
FOOTLOCKER = (block, dict(back=5, front=10))
BED = (block, dict(back=18, front=69))
CARGO = (block, dict(back=7, front=20))
FLOWER_BOX = (block, dict(back=20, front=24))
RAMPART = (rampart, dict(back=15, front=37, merlon=10, gap=8))
OPEN_BOAT = (open_boat, dict(back=4, front=29))
SMALL_STALL = (roofed, dict(ridge=7, hinge=27, counter=(40, 48)))
ARMCHAIR = (armchair, dict(seat=(27, 37), arm=19, inner=(9, 29)))
STOOL = (pedestal, dict(back=1, front=10))
ROCK = (lathe, dict(squash=0.6))
SPIKE = (lathe, dict(squash=0.8))
POTTED = (potted, dict(pot=35))
NOTICE = (slab, dict(depth=7))
LOGS = (slab, dict(depth=26))
CART = (cart, dict(barrel=(10, 0, 33, 14), box=(12, 27), wheels=23, body=(4, 44)))
LUGGAGE = (slab, dict(depth=18))
NETS = (lathe, dict(squash=0.45))
HULK = (open_boat, dict(back=7, front=29))
BONES = (slab, dict(depth=6))
RAIL = (rail_beam, {})
STONE_FOUNTAIN = (drum, dict(back=42, front=98, stand=[(33, 22, 64, 73)]))

# 세운 그림 이름(칸 자리 꼬리를 뗀 것) → (도형, 줄 수). 「맵/이름」은 그 맵의 것만 다르게.
SPECS = {
    "crate": (block, dict(back=0, front=7)), "crates": PILE, "pier_crates_a": PILE, "pier_crates_b": PILE, "port_crates": PILE,
    "barrel": (drum, dict(back=0, front=6)), "barrels": (heap, dict(depth=6)), "pier_barrels_a": (heap, dict(depth=6)),
    "stump": (drum, dict(back=2, front=9)), "road_stump": (drum, dict(back=2, front=9)),
    "feast_table": (drum, dict(back=17, front=45, stand=[(30, 8, 54, 35)])),
    "well": (drum, dict(back=31, front=41, above="center")),
    "fountain": (drum, dict(back=64, front=97, stand=[(37, 16, 78, 84)])), "sap_fountain": STONE_FOUNTAIN,
    "town/fountain": (drum, dict(back=42, front=118, stand=[(49, 23, 78, 82)])),
    "table_west": ROUND_TABLE, "table_east": ROUND_TABLE, "table_dorm": ROUND_TABLE, "table_kitchen": ROUND_TABLE, "round_table": ROUND_TABLE,
    "table": (mess_hall_table, {}), "hideout/table": ROUND_TABLE, "reading_table": (mess_hall_table, {}),
    "desk": (block, dict(back=14, front=24)), "desk_room": (block, dict(back=22, front=32, apron=2)),
    "counting_desk": DESK_WITH_LEGS, "reception": DESK_WITH_LEGS,
    "supper_table": TEA, "tea_table": TEA, "workbench": (block, dict(back=22, front=30, apron=2)),
    "altar": (block, dict(back=12, front=19)), "stone_bench": (block, dict(back=4, front=10, apron=6)),
    "footlocker": FOOTLOCKER, "footlocker_room": FOOTLOCKER, "footlocker_upper": FOOTLOCKER,
    "treasure_chest": CHEST, "chest_loot": CHEST, "open_chest": (block, dict(back=18, front=33)),
    "hay_a": (slab, dict(depth=16)), "hay_b": (slab, dict(depth=16)),
    "bed": BED, "bed_bare": BED, "child_bed": (block, dict(back=16, front=60)), "bed_quilt": (block, dict(back=1, front=42)),
    "nightstand": (block, dict(back=19, front=23)), "counter": (block, dict(back=5, front=20)), "desk_sheet": (block, dict(back=2, front=17)),
    "cargo": CARGO, "port_cargo_a": CARGO, "port_cargo_b": CARGO, "flower_box_left": FLOWER_BOX, "flower_box_right": FLOWER_BOX,
    "bench": BENCH, "bench_port": BENCH, "bench_upper": BENCH, "bench_back": BENCH, "bench_west": BENCH, "bench_east": BENCH, "terminus_bench": BENCH,
    "bookshelf": SHELF, "bookshelf_upper": SHELF, "bookshelf_hall": SHELF, "bookshelf_office": SHELF, "shelf_counter_left": SHELF,
    "shelf_counter_right": SHELF, "shelf_sheet": SHELF, "wardrobe": SHELF, "wardrobe_room": SHELF, "wardrobe_empty": SHELF, "dresser": SHELF,
    "display_case": SHELF, "seed_case": SHELF,
    "pillar": (lathe, dict(squash=0.6)), "urn": (lathe, {}), "jewel_pedestal": (lathe, dict(squash=0.6, start=27)),
    "bollard": (lathe, {}), "port_bollard_a": (lathe, {}), "port_bollard_b": (lathe, {}),
    "mushroom": (lathe, dict(squash=0.35)), "big_shroom": (lathe, dict(squash=0.35)), "lighthouse": (lathe, dict(squash=0.5, start=60)),
    "giant_trunk": (lathe, dict(squash=0.35)), "serpent_tower": (lathe, dict(squash=0.7, radius=66)), "tower_cone": (lathe, dict(start=24)),
    "tower_crown": (crowned_tower, dict(back=15, front=38)),
    "stall": (roofed, dict(ridge=4, hinge=44, counter=(71, 80))), "treeMarket/stall": SMALL_STALL, "westSands/stall": SMALL_STALL,
    "ticket_booth": (roofed, dict(ridge=24, hinge=51)),
    "gate_open": (slab, dict(depth=26)), "gate_closed": (slab, dict(depth=26)),
    "wall_north": RAMPART, "wall_west": RAMPART, "wall_east": RAMPART, "side_wall": (rampart_walk, dict(height=88)),
    "stone_wall": (block, dict(back=0, front=15)), "cave_mouth": (slab, dict(depth=24)), "sea_cave": (slab, dict(depth=24)),
    "iron_gate": (slab, dict(depth=10)), "sea_gate": (slab, dict(depth=14)),
    "coast_wreck_a": WRECK, "coast_wreck_b": WRECK, "wreck_a": WRECK, "wreck_b": WRECK, "wreck_c": WRECK, "wreck_den": WRECK, "port_wreck": WRECK,
    "lifeboat": OPEN_BOAT, "boat": OPEN_BOAT, "skiff": (open_boat, dict(back=6, front=32)),
    "armchair": ARMCHAIR, "armchair_left": ARMCHAIR, "armchair_right": ARMCHAIR, "treeHall/bench": ARMCHAIR,
    "armchair_sheet": (block, dict(back=17, front=22, depth=22)), "bunk_bed": (bunk, dict(decks=((13, 22), (43, 52)), post=5)),
    "chair_left": (side_chair, dict(seat=17, back_x=(1, 6))), "chair_right": (side_chair, dict(seat=17, back_x=(13, 18))),
    "tea_chair": (side_chair, dict(seat=17, back_x=(1, 6))),
    "stool": STOOL, "stool_0a": STOOL, "stool_0b": STOOL, "stool_1a": STOOL, "stool_1b": STOOL, "stool_2a": STOOL, "stool_2b": STOOL, "stool_a": STOOL,
    "stool_dorm_a": STOOL, "stool_dorm_b": STOOL, "stool_kitchen_a": STOOL, "stool_kitchen_b": STOOL, "stool_room": STOOL,
    "stool_east_0a": STOOL, "stool_east_0b": STOOL, "stool_east_1a": STOOL, "stool_east_1b": STOOL,
    "stool_west_0a": STOOL, "stool_west_0b": STOOL, "stool_west_1a": STOOL, "stool_west_1b": STOOL,
    "statue": (statue, dict(figure=61, front=68)), "throne": (block, dict(back=64, front=72, depth=26)), "bone_throne": (block, dict(back=60, front=84)),
    "brazier": (drum, dict(back=6, front=19)), "weapon_barrel": (drum, dict(back=34, front=41, above="center")),
    "bucket": (drum, dict(back=0, front=6)), "fish": (drum, dict(back=1, front=9)), "chopping_block": (drum, dict(back=14, front=22, above="center")),
    "capstan": (lathe, dict(squash=0.9, start=11, radius=12)), "helm": (slab, dict(depth=6)), "firewood": (block, dict(back=12, front=16, depth=14)),
    "beach_rock": ROCK, "sand_rock": ROCK, "sea_rock": ROCK, "boulder": ROCK, "ring_stone": (slab, dict(depth=14)),
    "stalagmite": SPIKE, "depth_pillar": SPIKE, "cave_pillar_a": SPIKE, "cave_pillar_b": SPIKE, "root_pillar": (lathe, dict(squash=0.7)),
    "gold_bars": (heap, dict(depth=5)), "gold_pile": (lathe, dict(squash=0.5)), "loot_heap": (slab, dict(depth=16)), "sack_kitchen": (lathe, dict(squash=0.8)),
    "planter": POTTED, "planter_dorm": POTTED, "planter_east": POTTED, "planter_hall": POTTED, "planter_office": POTTED, "planter_room": POTTED,
    "planter_upper": POTTED, "planter_west": POTTED,
    "quest_board": NOTICE, "notice_board": NOTICE, "recruit_board": NOTICE, "port_notice": NOTICE, "timetable": NOTICE, "terminus_timetable": NOTICE,
    "station_board": NOTICE, "station_clock": NOTICE, "terminus_clock": NOTICE, "coast_sign": (slab, dict(depth=4)),
    "archery_target": (slab, dict(depth=6)), "target_a": (slab, dict(depth=6)), "target_b": (slab, dict(depth=6)), "weapon_rack": (slab, dict(depth=5)),
    "dummy": (lathe, dict(radius=14)), "armor_stand": (lathe, dict(radius=13)), "rose_arch": (slab, dict(depth=12)),
    "doormat_lamp": (slab, dict(depth=9)), "lantern_nook": (slab, dict(depth=9)),
    "woodpile": LOGS, "spare_timber": LOGS, "luggage": LUGGAGE, "port_luggage": LUGGAGE, "port_luggage_b": LUGGAGE,
    "cart_wreck": CART, "hand_cart": CART, "port_cart": CART,
    "nets": NETS, "coast_nets": NETS, "port_nets": NETS, "port_nets_a": NETS, "port_nets_b": NETS,
    "port_skiff": HULK, "port_skiff_b": HULK, "wreck_boat_a": HULK, "wreck_boat_b": HULK,
    "ribs": BONES, "ribs_road": BONES, "dune_ribs": BONES, "campfire": (lying, {}), "iron_scale": (lying, dict(h=5)),
    "pots_house": (bunch, dict(cuts=(0, 16, 42))), "pots_plaza": (bunch, dict(cuts=(0, 16, 28, 54))),
    "twist_piece": (floating, dict(surface=(1, 24), end=72, shadow=112)), "twist_piece_low": (floating, dict(surface=(1, 24), end=56, shadow=72)),
    "bridge_rail": RAIL, "bridge_new_rail": RAIL, "bridge_rail_35_end": RAIL, "bridge_rail_38_end": RAIL, "pier_rail_a": RAIL, "pier_rail_b": RAIL,
}


def base_name(name):
    """세운 그림 이름에서 칸 자리 꼬리(crate_12_5)를 뗀 이름."""
    return name.rstrip("0123456789_.")


def build(map_id, name, image, pivot):
    """이름에 맞는 도형으로 만든 모형. 판으로 둘 것이면 None."""
    base = base_name(name)
    spec = SPECS.get(f"{map_id}/{base}", SPECS.get(base))
    if spec is None:
        return None
    shape, options = spec
    model = Model(foot=tuple(pivot), pivot=tuple(pivot))
    shape(model, image, int(round(pivot[1])), **options)
    return model


def build_ship(layers, pivot):
    """맨 배(harbor_props.ship_layers)의 모형."""
    model = Model(foot=tuple(pivot), pivot=tuple(pivot))
    ship(model, layers["hull"], layers["masts"], pivot[1], layers["south"], layers["side"])
    return model
