"""코드로 그리는 세운 것(맵에 서서 인물과 앞뒤가 갈리는 것): 집, 상자, 통, 우물, 수반, 울타리, 가로등.

웹게임은 이것들을 칸 그림으로 바닥에 칠했지만, 여기서는 따로 그린 그림을 맵에 세운다.
모두 점 격자(한 칸 32점)의 RGBA 배열과 발밑 기준점(점 단위)을 돌려준다. 빛은 왼쪽 위에서 오고, 테두리는 진하다.
"""
import numpy as np

import pixelgen as pg

T = pg.TILE
ROOF = pg.hexes("#3a1712", "#5c2419", "#7d3121", "#9c412a", "#b65634", "#cf7043", "#e2915f")
PLASTER = pg.hexes("#a8977a", "#c4b392", "#d9caa8", "#e9dcbd", "#f4ead0")
GLASS = pg.hexes("#1b2433", "#28374f", "#3d5677", "#7c9cc0")
IRON = pg.hexes("#1a1a20", "#2c2c35", "#44444f", "#62626e")
LAMP_GLOW = pg.hexes("#ffb347", "#ffd27a", "#fff1c4")


def canvas(width, height):
    return np.zeros((height, width, 4), dtype=np.uint8)


def fill(image, ys, xs, color):
    image[ys, xs, :3] = color
    image[ys, xs, 3] = 255


# ── 집 ────────────────────────────────────────────────

def house(columns, roof_rows, wall_rows, windows, doors, rng):
    """
    반목조 집 한 채. 칸 수(가로 columns, 지붕 줄 roof_rows, 벽 줄 wall_rows)는 웹게임 맵의 집 칸과 같다.
    windows·doors는 창·문이 있는 열(집 왼쪽에서 센 칸)이다. 기준점은 벽 아래 가운데(집이 땅에 닿는 줄).
    """
    width, roof_h, wall_h = columns * T, roof_rows * T, wall_rows * T
    image = canvas(width, roof_h + wall_h)
    top = roof_h
    # 벽: 회반죽 바탕에 나무 기둥·들보, 아래는 돌 받침
    grain = pg.noise(wall_h, width, 6, rng)
    plaster = PLASTER[2 + (grain > 0.62).astype(int) - (grain < 0.3).astype(int)]
    image[top:, :, :3] = plaster
    image[top:, :, 3] = 255
    beam = pg.BARK[2]
    for y in (top, top + 1, top + wall_h // 2, top + wall_h - 8, top + wall_h - 7):
        image[y, :, :3] = beam
    for x in list(range(0, width, T)) + [width - 3]:
        image[top:top + wall_h - 6, x:x + 3, :3] = beam
        image[top:top + wall_h - 6, x, :3] = pg.BARK[3]
    # 창 사이 벽에는 비스듬한 버팀목
    for column in range(columns):
        if column in windows or column in doors:
            continue
        x0 = column * T + 3
        for k in range(T - 6):
            y = top + 2 + int(k * (wall_h // 2 - 2) / (T - 6))
            image[y:y + 2, x0 + k, :3] = beam
    # 돌 받침
    base = top + wall_h - 6
    image[base:, :, :3] = pg.STONE[2]
    image[base, :, :3] = pg.STONE[4]
    for x in range(0, width, 7):
        image[base + 1:, x, :3] = pg.STONE[1]
    # 지붕 아래 그늘
    image[top + 2:top + 5, :, :3] = (image[top + 2:top + 5, :, :3] * 0.7).astype(np.uint8)
    for column in windows:
        window(image, column * T + T // 2, top + wall_h // 4 + 2)
    for column in doors:
        door(image, column * T + T // 2, top + wall_h)
    roof(image, width, roof_h, rng)
    return pg.outline(image), (width // 2, roof_h + wall_h - 1)


def roof(image, width, height, rng):
    """기와 지붕: 어긋나게 겹친 기와 줄, 꼭대기 용마루는 밝고 처마 끝은 어둡다."""
    row_h = 6
    for y in range(height):
        row = y // row_h
        within = y % row_h
        offset = (row % 2) * 4
        depth = y / height
        for x in range(width):
            tile_x = (x + offset) % 8
            level = 4 - int(depth * 2.2)
            if within >= row_h - 2:
                level -= 1
            if tile_x == 0:
                level -= 1
            if within == 0:
                level += 1
            fill(image, y, x, ROOF[int(np.clip(level, 0, 6))])
    # 용마루와 처마
    fill(image, slice(0, 3), slice(0, width), ROOF[5])
    fill(image, 3, slice(0, width), ROOF[2])
    fill(image, slice(height - 3, height), slice(0, width), ROOF[1])


def window(image, cx, cy):
    """나무틀 창: 짙은 유리에 빛 한 줄, 십자 창살, 아래 창턱."""
    w, h = 14, 12
    x0, y0 = cx - w // 2, cy - h // 2
    fill(image, slice(y0, y0 + h), slice(x0, x0 + w), pg.WOOD[1])
    fill(image, slice(y0 + 2, y0 + h - 2), slice(x0 + 2, x0 + w - 2), GLASS[1])
    fill(image, slice(y0 + 2, y0 + 5), slice(x0 + 2, x0 + 6), GLASS[3])
    fill(image, slice(y0 + 2, y0 + h - 2), cx, pg.WOOD[2])
    fill(image, cy, slice(x0 + 2, x0 + w - 2), pg.WOOD[2])
    fill(image, slice(y0 + h, y0 + h + 2), slice(x0 - 1, x0 + w + 1), pg.WOOD[3])


def door(image, cx, bottom):
    """윗부분이 둥근 나무문: 세로 판자, 쇠 손잡이, 앞의 돌 디딤."""
    w, h = 16, 26
    x0, y0 = cx - w // 2, bottom - h - 2
    for y in range(y0, bottom - 2):
        for x in range(x0, x0 + w):
            u = (x - x0 + 0.5) / w * 2 - 1
            if y < y0 + 6 and (y - y0 - 6) ** 2 / 36 + u * u > 1:
                continue
            plank = (x - x0) % 4 == 0
            fill(image, y, x, pg.WOOD[1] if plank else pg.WOOD[2 if u > 0.3 else 3])
    fill(image, slice(bottom - 14, bottom - 12), x0 + w - 4, IRON[3])
    fill(image, slice(bottom - 2, bottom), slice(x0 - 2, x0 + w + 2), pg.STONE[3])


def rock_door(rng):
    """절벽에 낸 문: 나무 문틀과 회반죽 둘레 안의 둥근 나무문, 아래 돌 디딤. 한 칸 너비, 기준점은 칸 아래 가운데."""
    w, h = T, 44
    image = canvas(w, h)
    fill(image, slice(4, h), slice(1, w - 1), PLASTER[2])
    fill(image, slice(2, 6), slice(0, w), pg.BARK[2])
    fill(image, slice(2, h - 2), slice(0, 3), pg.BARK[2])
    fill(image, slice(2, h - 2), slice(w - 3, w), pg.BARK[1])
    door(image, w // 2, h)
    return pg.outline(image), (w // 2, h - 1)


# ── 작은 것 ─────────────────────────────────────────────

def crate(rng):
    """나무 상자: 밝은 윗면과 판자 앞면, 앞면에 X자 버팀."""
    w, h, lid = 24, 24, 7
    image = canvas(w, h)
    fill(image, slice(0, lid), slice(0, w), pg.WOOD[4])
    for x in range(0, w, 6):
        fill(image, slice(0, lid), x, pg.WOOD[3])
    fill(image, slice(lid, h), slice(0, w), pg.WOOD[2])
    for y in range(lid, h, 5):
        fill(image, y, slice(0, w), pg.WOOD[1])
    for k in range(h - lid):
        x = int(k * (w - 1) / (h - lid - 1))
        fill(image, lid + k, slice(max(0, x - 1), x + 1), pg.WOOD[3])
        fill(image, lid + k, slice(max(0, w - 2 - x), w - x), pg.WOOD[3])
    fill(image, slice(lid, h), slice(0, 2), pg.WOOD[3])
    fill(image, slice(lid, h), slice(w - 2, w), pg.WOOD[1])
    return pg.outline(image), (w // 2, h - 1)


def barrel(rng):
    """나무 통: 둥근 몸통의 세로 널빤지, 쇠테 둘, 위 뚜껑."""
    w, h = 20, 26
    image = canvas(w, h)
    for y in range(h):
        bulge = 1 - abs((y - h * 0.55) / (h * 0.6)) ** 2 * 0.15
        half = (w / 2 - 1) * bulge
        for x in range(w):
            u = (x - w / 2 + 0.5) / half
            if abs(u) > 1:
                continue
            level = 3 if u < -0.4 else 2 if u < 0.4 else 1
            if int((u + 1) * 4) != int((u + 1) * 4 - 0.15):
                level -= 1
            fill(image, y, x, pg.WOOD[max(0, level)])
    for y in (7, h - 7):
        fill(image, slice(y, y + 2), slice(1, w - 1), IRON[2])
        fill(image, y, slice(1, w - 1), IRON[3])
    yy, xx = np.mgrid[0:6, 0:w]
    lid = ((xx - w / 2 + 0.5) / (w / 2 - 1)) ** 2 + ((yy - 2.5) / 3) ** 2 < 1
    image[:6][lid, :3] = pg.WOOD[4]
    image[:6][lid, 3] = 255
    return pg.outline(image), (w // 2, h - 1)


def well(rng):
    """돌우물: 둥근 돌담 안의 어두운 물, 양쪽 나무 기둥과 작은 지붕, 매달린 두레박."""
    w, h = 36, 56
    image = canvas(w, h)
    cx = w / 2
    # 돌담(원통)
    wall_top, wall_bottom = h - 20, h - 1
    for y in range(wall_top, wall_bottom + 1):
        for x in range(2, w - 2):
            u = (x - cx + 0.5) / (cx - 2)
            brick = ((y - wall_top) // 5 + (x // 6)) % 2
            level = 4 if u < -0.4 else 3 if u < 0.3 else 2
            if (y - wall_top) % 5 == 0 or (x + ((y - wall_top) // 5) * 3) % 6 == 0:
                level = 1
            fill(image, y, x, pg.STONE[level - brick * 0])
    yy, xx = np.mgrid[0:h, 0:w]
    rim = ((xx - cx + 0.5) / (cx - 2)) ** 2 + ((yy - wall_top) / 5) ** 2
    image[rim < 1, :3] = pg.STONE[4]
    image[rim < 1, 3] = 255
    inner = ((xx - cx + 0.5) / (cx - 6)) ** 2 + ((yy - wall_top) / 3) ** 2 < 1
    image[inner, :3] = pg.WATER[0]
    # 기둥과 지붕
    for x in (4, w - 7):
        fill(image, slice(10, wall_top + 2), slice(x, x + 3), pg.WOOD[2])
        fill(image, slice(10, wall_top + 2), x, pg.WOOD[3])
    fill(image, slice(12, 14), slice(6, w - 6), pg.WOOD[1])
    for y in range(0, 11):
        half = int(4 + y * 1.4)
        fill(image, y, slice(int(cx - half), int(cx + half)), ROOF[2 if y < 9 else 1])
    fill(image, slice(14, 22), int(cx), pg.WOOD[1])
    fill(image, slice(22, 27), slice(int(cx) - 3, int(cx) + 3), pg.WOOD[3])
    return pg.outline(image), (w // 2, h - 1)


def basin(columns, rows, water_cells, rng):
    """
    돌 수반: 칸 묶음(가로 columns × 세로 rows) 둘레의 돌 테두리 안에 물이 고였다. water_cells는 물이 있는 (열, 줄)이다.
    낮아서 앞면은 몇 점만 보인다. 기준점은 아래 가운데.
    """
    width, depth = columns * T, rows * T
    face = 6
    image = canvas(width, depth + face)
    # 테두리 돌: 줄마다 어긋나게 쌓은 돌 토막, 토막 사이는 어둡고 윗가장자리는 밝다.
    for y in range(depth):
        for x in range(width):
            course = y // 8
            seam = y % 8 == 0 or (x + course * 5) % 12 == 0
            image[y, x, :3] = pg.STONE[1] if seam else pg.STONE[4] if y % 8 == 1 else pg.STONE[3]
            image[y, x, 3] = 255
    for column, row in water_cells:
        x0, y0 = column * T + 2, row * T + 2
        image[y0:y0 + T - 4, x0:x0 + T - 4, :3] = pg.WATER[3]
        image[y0:y0 + 3, x0:x0 + T - 4, :3] = pg.STONE[1]
        image[y0 + 6:y0 + 7, x0 + 4:x0 + 12, :3] = pg.WATER[5]
    image[depth:, :, :3] = pg.STONE[2]
    image[depth:, :, 3] = 255
    image[depth, :, :3] = pg.STONE[1]
    image[0, :, :3] = pg.STONE[5]
    return pg.outline(image), (width // 2, depth + face - 1)


def fountain(size, rng):
    """
    광장 분수: 위에서 비스듬히 본 둥근 돌 수반(윗면 테두리와 앞쪽 옆면), 안에 물결, 가운데 기둥 위 작은 그릇에서
    물이 흘러내린다. size는 차지하는 칸 수(가로·세로 같음). 기준점은 아래 가운데.
    """
    width = size * T
    rim_h = int(width * 0.62)
    side = 10
    lift = 40
    height = lift + rim_h + side
    image = canvas(width, height)
    yy, xx = np.mgrid[0:height, 0:width]
    cx, cy = (width - 1) / 2, lift + rim_h / 2
    rx, ry = width / 2 - 1, rim_h / 2 - 1

    def paint(mask, color):
        image[mask, :3] = color
        image[mask, 3] = 255

    def ellipse(rx_, ry_, oy=0.0):
        return ((xx - cx) / rx_) ** 2 + ((yy - cy - oy) / ry_) ** 2 <= 1

    outer, wall = ellipse(rx, ry), ellipse(rx, ry, side)
    front = wall & ~outer & (yy > cy)
    paint(front, pg.STONE[2])
    paint(front & (xx % 9 == 0), pg.STONE[1])
    paint(front & ~ellipse(rx, ry, side - 2), pg.STONE[1])
    paint(outer, pg.STONE[4])
    angle = np.arctan2((yy - cy) / ry, (xx - cx) / rx)
    paint(outer & (np.cos(angle * 7) > 0.97), pg.STONE[2])
    paint(outer & (yy < cy) & (xx < cx) & ~ellipse(rx - 2, ry - 2), pg.STONE[5])
    pool = ellipse(rx - 7, ry - 6)
    paint(pool, pg.WATER[3])
    paint(pool & ~ellipse(rx - 9, ry - 8, 2), pg.WATER[1])
    ring = np.hypot((xx - cx) / (rx - 7), (yy - cy) / (ry - 6))
    paint(pool & (np.abs(ring - 0.6) < 0.04) & (rng.random((height, width)) < 0.7), pg.WATER[5])
    for _ in range(14):
        x, y = int(rng.integers(cx - rx + 12, cx + rx - 12)), int(rng.integers(cy - ry + 10, cy + ry - 8))
        if pool[y, x]:
            paint((yy == y) & (xx >= x) & (xx < x + 3), pg.WATER[6])
    # 가운데 기둥과 위 그릇, 그릇 가장자리에서 흘러내리는 물
    bowl_cy = lift - 6
    column = (np.abs(xx - cx) <= 4) & (yy >= bowl_cy) & (yy <= cy + 2)
    paint(column, pg.STONE[3])
    paint(column & (xx < cx - 1), pg.STONE[5])
    paint(column & (xx > cx + 2), pg.STONE[1])
    bowl = ((xx - cx) / 13) ** 2 + ((yy - bowl_cy) / 4.5) ** 2 <= 1
    under = ((xx - cx) / 13) ** 2 + ((yy - bowl_cy - 3) / 4.5) ** 2 <= 1
    paint(under & ~bowl, pg.STONE[2])
    paint(bowl, pg.STONE[4])
    paint(((xx - cx) / 10) ** 2 + ((yy - bowl_cy) / 3) ** 2 <= 1, pg.WATER[4])
    for sx in (-13, 12):
        x = int(cx + sx)
        fall = (xx == x) & (yy > bowl_cy) & (yy < cy - 2)
        paint(fall, pg.WATER[5])
        paint(fall & (yy % 3 == 0), pg.WATER[6])
        paint((yy >= int(cy) - 3) & (yy < int(cy)) & (np.abs(xx - x) <= 2), pg.WATER[6])
    paint((np.abs(xx - cx) <= 1) & (yy >= bowl_cy - 10) & (yy < bowl_cy), pg.WATER[6])
    return pg.outline(image), (width // 2, height - 1)


def fence(mask):
    """
    나무 울타리 한 칸: 가운데 기둥에서 이어진 쪽(왼 1, 오 2, 위 4, 아래 8)으로 가로대가 뻗는다.
    기둥 높이만큼 칸 위로 솟고, 기준점은 칸 아래 가운데.
    """
    height = T + 14
    image = canvas(T, height)
    cx, ground = T // 2, T - 6 + 14
    post_top = ground - 20
    for left, right in ((mask & 1, 0), (mask & 2, 1)):
        if not left:
            continue
        xs = slice(0, cx) if right == 0 else slice(cx, T)
        for y in (post_top + 4, post_top + 11):
            fill(image, slice(y, y + 2), xs, pg.WOOD[3])
            fill(image, y + 2, xs, pg.WOOD[1])
    if mask & 4:
        fill(image, slice(0, post_top + 4), slice(cx - 1, cx + 1), pg.WOOD[3])
    if mask & 8:
        fill(image, slice(post_top + 4, height), slice(cx - 1, cx + 1), pg.WOOD[3])
    fill(image, slice(post_top, ground), slice(cx - 2, cx + 2), pg.WOOD[2])
    fill(image, slice(post_top, ground), cx - 2, pg.WOOD[3])
    fill(image, post_top, slice(cx - 2, cx + 2), pg.WOOD[4])
    return pg.outline(image), (cx, ground)


def lamp(rng, width=44, height=116):
    """쇠 가로등: 받침, 가는 기둥, 따뜻한 유리 등, 뾰족한 갓. 기준점은 받침 아래 가운데(웹게임 lamp와 같은 칸)."""
    image = canvas(width, height)
    cx = width // 2
    fill(image, slice(height - 8, height), slice(cx - 7, cx + 7), IRON[2])
    fill(image, slice(height - 8, height - 6), slice(cx - 7, cx + 7), IRON[3])
    fill(image, slice(30, height - 8), slice(cx - 2, cx + 2), IRON[1])
    fill(image, slice(30, height - 8), cx - 2, IRON[3])
    fill(image, slice(26, 30), slice(cx - 6, cx + 6), IRON[2])
    fill(image, slice(10, 26), slice(cx - 8, cx + 8), IRON[1])
    fill(image, slice(12, 25), slice(cx - 6, cx + 6), LAMP_GLOW[1])
    fill(image, slice(14, 22), slice(cx - 3, cx + 3), LAMP_GLOW[2])
    fill(image, slice(12, 25), cx, IRON[1])
    for y in range(2, 10):
        half = y + 1
        fill(image, y, slice(cx - half, cx + half), IRON[2 if y < 8 else 1])
    fill(image, slice(0, 2), slice(cx - 1, cx + 1), IRON[3])
    return pg.outline(image), (cx, height - 1)


# ── 맵에 세우기 ─────────────────────────────────────────

HOUSE_KINDS = ("houseRoof", "houseWall", "window", "door")


def groups(kinds, members):
    """members에 드는 칸끼리 상하좌우로 이어진 묶음(칸 좌표 목록)."""
    rows, cols = kinds.shape
    inside = np.isin(kinds, members)
    seen = np.zeros_like(inside)
    found = []
    for r, c in zip(*np.nonzero(inside)):
        if seen[r, c]:
            continue
        stack, cells = [(r, c)], []
        seen[r, c] = True
        while stack:
            y, x = stack.pop()
            cells.append((y, x))
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < rows and 0 <= nx < cols and inside[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        found.append(cells)
    return found


def place(tiles, seed):
    """
    칸 격자에서 세울 것을 찾아 그림과 자리를 정한다. 돌려주는 값: (이름, 점 격자 RGBA, 왼쪽 점, 위 점, 발밑 점 y) 목록.
    발밑 점 y는 인물과 앞뒤를 가르는 줄이다(그보다 위에 선 인물은 뒤에 그린다).
    """
    kinds = np.array(tiles, dtype=object)
    rows, cols = kinds.shape
    rng = np.random.default_rng(seed)
    placed = []
    for n, cells in enumerate(groups(kinds, HOUSE_KINDS)):
        r0, r1 = min(r for r, _ in cells), max(r for r, _ in cells)
        c0, c1 = min(c for _, c in cells), max(c for _, c in cells)
        roof_rows = sum(1 for r in range(r0, r1 + 1) if all(kinds[r, c] == "houseRoof" for c in range(c0, c1 + 1) if kinds[r, c] in HOUSE_KINDS))
        windows = sorted({c - c0 for r, c in cells if kinds[r, c] == "window"})
        doors = sorted({c - c0 for r, c in cells if kinds[r, c] == "door"})
        if roof_rows == 0:
            # 지붕 없이 문만 있으면 절벽에 낸 문이다(요정 정원의 동굴 입구).
            image, pivot = rock_door(rng)
            foot_x, foot_y = (c0 + c1 + 1) * T // 2, (r1 + 1) * T - 1
            placed.append((f"door_{n}", image, foot_x - pivot[0], foot_y - pivot[1], (r1 + 1) * T))
            continue
        image, _ = house(c1 - c0 + 1, roof_rows, r1 - r0 + 1 - roof_rows, windows, doors, rng)
        placed.append((f"house_{n}", image, c0 * T, r0 * T, (r1 + 1) * T))
    for n, cells in enumerate(groups(kinds, ("basin",))):
        r0, r1 = min(r for r, _ in cells), max(r for r, _ in cells)
        c0, c1 = min(c for _, c in cells), max(c for _, c in cells)
        water = [(c - c0, r - r0) for r in range(r0, r1 + 1) for c in range(c0, c1 + 1) if kinds[r, c] == "water"]
        image, _ = basin(c1 - c0 + 1, r1 - r0 + 1, water, rng)
        placed.append((f"basin_{n}", image, c0 * T, r0 * T, (r1 + 1) * T))
    makers = {"crate": crate, "barrel": barrel, "well": well}
    for r, c in zip(*np.nonzero(np.isin(kinds, list(makers) + ["fence"]))):
        kind = kinds[r, c]
        if kind == "fence":
            mask = sum(bit for bit, (dy, dx) in ((1, (0, -1)), (2, (0, 1)), (4, (-1, 0)), (8, (1, 0)))
                       if 0 <= r + dy < rows and 0 <= c + dx < cols and kinds[r + dy, c + dx] == "fence")
            image, pivot = fence(mask)
            name = f"fence_{mask}"
        else:
            image, pivot = makers[kind](np.random.default_rng(seed + r * 97 + c))
            name = f"{kind}_{r}_{c}"
        foot_x, foot_y = c * T + T // 2, (r + 1) * T - 1
        placed.append((name, image, foot_x - pivot[0], foot_y - pivot[1], (r + 1) * T))
    return placed
