"""3장 마을 순찰병 배치에 주인공이 지나갈 틈이 남는지 검사한다.

병사를 town.GUARDS 그대로 걷게 하고, 어귀(arrive)에서 숲길(forest)까지 들키지 않고 가는 길을 시간까지 넣어 찾는다.
주인공이 칸 안 어디에 있든 시야에 닿으면 들킨 것으로 넉넉히 보고, 조이스틱이 서툰 사람도 지나가게 실제(12프레임)보다 느린 걸음으로도 찾는다.
출발 시각을 여러 번 바꿔 보므로, 틈이 어쩌다 한 번 나는 배치는 실패로 나온다.

    python Tools/PixelGen/town_guards.py <프로젝트 루트>

맵은 Art/PixelGen/Maps/town/map.json을 읽는다. 시야 수치는 GuardSight.cs, 걸음 수치는 Chapter3.cs·Exploration.cs와 같아야 한다.
게임의 병사는 걸음마다 한두 프레임씩 밀려서, 여기서 찾은 길을 시간표대로 걸으면 게임과 어긋난다. 틈이 있는지만 본다.
"""
import argparse
import json
import math
import sys
from pathlib import Path

from town import GUARDS

# GuardSight.cs
SIGHT_RANGE = 7.0
SIGHT_HALF_ANGLE = 28.0
FRAMES_TO_TURN_AROUND = 45
TURN_DEGREES_PER_FRAME = 180.0 / FRAMES_TO_TURN_AROUND
# Chapter3.cs
GUARD_FRAMES_PER_TILE = 30
# 게임의 주인공은 한 칸에 12프레임쯤 걷는다.
HERO_SPEEDS = (12, 16, 20)
DEPARTURES = range(0, 1800, 150)
SEARCH_SECONDS = 240

STEPS = {"U": (0, -1), "D": (0, 1), "L": (-1, 0), "R": (1, 0)}
HEADINGS = {"U": -90.0, "D": 90.0, "L": 180.0, "R": 0.0}
HERO_SPOTS = ((-0.3, -0.35), (0.3, -0.35), (-0.3, 0.3), (0.3, 0.3), (0.0, 0.0))
FOOT_LIFT = 15 / 16
PIXEL = 1 / 16


class Town:
    def __init__(self, map_json):
        data = json.loads(Path(map_json).read_text(encoding="utf-8"))
        self.width, self.height = data["width"], data["height"]
        self.per_tile = data["collisionPerTile"]
        self.collision = data["collision"]
        points = {point["name"]: point["cells"] for point in data["points"]}
        self.start = (points["arrive"][0]["x"], points["arrive"][0]["y"])
        self.goals = {(cell["x"], cell["y"]) for cell in points["forest"]}
        self.clear_sight = {}

    def solid(self, x, y):
        column, row = int(x * self.per_tile), int(y * self.per_tile)
        inside = 0 <= row < len(self.collision) and 0 <= column < len(self.collision[0])
        return not inside or self.collision[row][column] == "1"

    def blocked_cell(self, cell):
        """FootMover.IsBlocked: 발밑 상자(가로 ±5px, 위로 1~6px)가 막힌 곳에 닿는지."""
        x, y = cell[0] + 0.5, cell[1] + FOOT_LIFT
        return any(self.solid(x + dx * PIXEL, y - dy * PIXEL) for dx in (-5, 5) for dy in (1, 6))

    def sees(self, eye, point):
        key = (eye, point)
        if key not in self.clear_sight:
            distance = math.dist(eye, point)
            steps = max(1, int(distance / (PIXEL * 2)))
            self.clear_sight[key] = not any(
                self.solid(eye[0] + (point[0] - eye[0]) * i / steps, eye[1] + (point[1] - eye[1]) * i / steps)
                for i in range(1, steps + 1))
        return self.clear_sight[key]


class Guard:
    """한 병사가 프레임마다 어디서 어디를 보는지. Exploration.UpdateResidents와 같은 순서로 걷는다."""

    def __init__(self, town, start, route):
        self.town = town
        self.moves = [part[0] for part in route.split() for _ in range(int(part[1:]))]
        first_lap = self.walk(start, "D", 90.0)
        self.lead_in, self.lap = first_lap[0], self.walk(start, self.moves[-1], first_lap[1])[0]
        self.seen_at = {}

    def walk(self, start, facing, heading):
        frames = []
        x, y = start

        def stand(look):
            nonlocal heading
            gap = (HEADINGS[look] - heading + 180) % 360 - 180
            heading = HEADINGS[look] if abs(gap) <= TURN_DEGREES_PER_FRAME else heading + math.copysign(TURN_DEGREES_PER_FRAME, gap or 1)

        for move in self.moves:
            if move != facing:
                facing = move
                for _ in range(FRAMES_TO_TURN_AROUND + 1):
                    stand(facing)
                    frames.append((x + 0.5, y + FOOT_LIFT, heading))

            dx, dy = STEPS[move]
            for frame in range(GUARD_FRAMES_PER_TILE + 1):
                stand(facing)
                done = min(1.0, frame / GUARD_FRAMES_PER_TILE)
                frames.append((x + 0.5 + dx * done, y + FOOT_LIFT + dy * done, heading))
            x, y = x + dx, y + dy

        return frames, heading

    def pose(self, frame):
        if frame < len(self.lead_in):
            return self.lead_in[frame]
        return self.lap[(frame - len(self.lead_in)) % len(self.lap)]

    def seen_cells(self, frame):
        x, y, heading = self.pose(frame)
        key = (round(x * 8), round(y * 8), round(heading / 4))
        if key not in self.seen_at:
            self.seen_at[key] = self.cells_in_sight((round(x, 3), round(y, 3)), heading)
        return self.seen_at[key]

    def cells_in_sight(self, eye, heading):
        reach = int(SIGHT_RANGE) + 2
        seen = set()
        for cell_y in range(int(eye[1]) - reach, int(eye[1]) + reach):
            for cell_x in range(int(eye[0]) - reach, int(eye[0]) + reach):
                spots = ((cell_x + 0.5 + dx, cell_y + 0.6 + dy) for dx, dy in HERO_SPOTS)
                if any(self.in_fan(eye, heading, spot) for spot in spots):
                    seen.add((cell_x, cell_y))
        return seen

    def in_fan(self, eye, heading, spot):
        gap_x, gap_y = spot[0] - eye[0], spot[1] - eye[1]
        if gap_x * gap_x + gap_y * gap_y > SIGHT_RANGE * SIGHT_RANGE:
            return False
        off = (math.degrees(math.atan2(gap_y, gap_x)) - heading + 180) % 360 - 180
        return abs(off) <= SIGHT_HALF_ANGLE and self.town.sees(eye, spot)


def fastest_way(town, guards, frames_per_tile, departure):
    """그 시각에 어귀를 떠나 숲길까지 가는 가장 빠른 칸 순서. 없으면 None."""
    reached = {town.start: None}
    history = [reached]
    for beat in range(int(SEARCH_SECONDS * 60 / frames_per_tile)):
        first = departure + beat * frames_per_tile
        seen = set()
        for frame in range(first, first + frames_per_tile + 1, 3):
            for guard in guards:
                seen |= guard.seen_cells(frame)

        following = {}
        for cell in reached:
            if cell in seen:
                continue
            for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                target = (cell[0] + dx, cell[1] + dy)
                if target not in seen and target not in following and (dx == dy == 0 or not town.blocked_cell(target)):
                    following[target] = cell

        history.append(following)
        reached = following
        arrived = town.goals & following.keys()
        if arrived:
            way = [arrived.pop()]
            for earlier in reversed(history[1:]):
                way.append(earlier[way[-1]])
            return way[::-1]

    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("project", type=Path)
    args = parser.parse_args()

    town = Town(args.project / "Assets/MiniPandoras/Art/PixelGen/Maps/town/map.json")
    guards = [Guard(town, start, route) for start, route in GUARDS]
    print(f"병사 {len(guards)}명, 한 바퀴 {[round(len(guard.lap) / 60, 1) for guard in guards]}초")

    failed = False
    for frames_per_tile in HERO_SPEEDS:
        ways = [fastest_way(town, guards, frames_per_tile, departure) for departure in DEPARTURES]
        seconds = [(len(way) - 1) * frames_per_tile / 60 for way in ways if way]
        failed |= len(seconds) < len(ways)
        print(f"한 칸에 {frames_per_tile}프레임: 출발 {len(ways)}번 가운데 {len(seconds)}번 통과, {min(seconds, default=0):.1f}~{max(seconds, default=0):.1f}초")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
