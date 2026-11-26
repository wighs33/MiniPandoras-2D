"""윤곽만 보이는 인물을 그리는 붓(타이틀의 여신, 컷인의 신과 십이장). 크게 그린 뒤 줄여 윤곽을 매끄럽게 한다."""
import math

import numpy as np
from PIL import Image, ImageDraw


def spline(points, closed, step=10):
    """점들을 지나는 부드러운 곡선."""
    p = [np.array(q, dtype=float) for q in points]
    n = len(p)
    out = []
    for i in range(n if closed else n - 1):
        a, b = p[(i - 1) % n] if closed or i > 0 else p[0], p[i]
        c, e = p[(i + 1) % n], p[(i + 2) % n] if closed or i + 2 < n else p[-1]
        for t in np.linspace(0, 1, step, endpoint=False):
            out.append(0.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - e) * t ** 2 + (3 * b - a - 3 * c + e) * t ** 3))
    if not closed:
        out.append(p[-1])
    return np.array(out)


class Brush:
    def __init__(self, width, height, seed, supersample=4):
        self.width, self.height, self.scale = width, height, supersample
        self.image = Image.new("L", (width * supersample, height * supersample), 0)
        self.draw = ImageDraw.Draw(self.image)
        self.rng = np.random.default_rng(seed)

    def polygon(self, points, fill=255):
        self.draw.polygon([(x * self.scale, y * self.scale) for x, y in points], fill=fill)

    def shape(self, points, fill=255):
        self.polygon(spline(points, True), fill)

    def ribbon(self, points, widths, fill=255):
        """굵기가 변하는 띠(팔다리, 머리 타래, 꼬리). widths는 길이를 따라 고르게 나뉜 반지름이다."""
        line = spline(points, False)
        along = np.linspace(0, 1, len(line))
        radius = np.interp(along, np.linspace(0, 1, len(widths)), widths)
        normal = np.gradient(line, axis=0)[:, ::-1] * np.array([-1, 1])
        normal /= np.maximum(np.linalg.norm(normal, axis=1, keepdims=True), 1e-6)
        self.polygon(list(line + normal * radius[:, None]) + list((line - normal * radius[:, None])[::-1]), fill)
        for (x, y), r in ((line[0], radius[0]), (line[-1], radius[-1])):
            self.ellipse(x, y, r, r, fill)

    def ellipse(self, x, y, rx, ry, fill=255):
        s = self.scale
        self.draw.ellipse([(x - rx) * s, (y - ry) * s, (x + rx) * s, (y + ry) * s], fill=fill)

    def ring(self, x, y, rx, ry, width):
        s = self.scale
        self.draw.ellipse([(x - rx) * s, (y - ry) * s, (x + rx) * s, (y + ry) * s], outline=255, width=max(1, int(width * s)))

    def line(self, a, b, width):
        self.ribbon([a, b], [width / 2, width / 2])

    def star(self, x, y, r, waist=0.2):
        """네 갈래 별."""
        points = []
        for k in range(8):
            angle = math.pi / 4 * k - math.pi / 2
            reach = r if k % 2 == 0 else r * waist
            points.append((x + math.cos(angle) * reach, y + math.sin(angle) * reach * (1.25 if k in (0, 4) else 1)))
        self.polygon(points)

    def stamp(self, mask, x, y):
        """0~255의 그림을 (x, y)에 왼쪽 위를 맞춰 겹친다."""
        s = self.scale
        size = (int(mask.width * s), int(mask.height * s))
        self.image.paste(255, (int(x * s), int(y * s)), mask.resize(size, Image.LANCZOS))

    def mask(self):
        """0~1의 그림."""
        return np.asarray(self.image.resize((self.width, self.height), Image.LANCZOS)).astype(float) / 255


def blade(b, grip, tip, width):
    g, t = np.array(grip, dtype=float), np.array(tip, dtype=float)
    u = (t - g) / np.linalg.norm(t - g)
    n = np.array([-u[1], u[0]])
    b.polygon([g - n * width, g + n * width, t - u * width * 2.2 + n * width, t, t - u * width * 2.2 - n * width])
    b.line(g - n * width * 2.8, g + n * width * 2.8, width * 0.8)
    b.line(g, g - u * 24, width * 0.7)
    b.ellipse(*(g - u * 26), width * 0.6, width * 0.6)


def spear(b, butt, tip, head=34, width=8):
    g, t = np.array(butt, dtype=float), np.array(tip, dtype=float)
    u = (t - g) / np.linalg.norm(t - g)
    n = np.array([-u[1], u[0]])
    b.line(g, t, 4.6)
    b.polygon([t + u * head, t + n * width, t - u * 8, t - n * width])


def tatter(b, left, right, drop, count=7):
    """옷자락 끝의 갈라진 조각."""
    for k in range(count):
        t = k / (count - 1)
        x = left[0] + (right[0] - left[0]) * t
        y = left[1] + (right[1] - left[1]) * t
        sway = (t - 0.5) * 26 + b.rng.uniform(-6, 6)
        b.ribbon([(x, y - 30), (x + sway * 0.4, y), (x + sway, y + drop * b.rng.uniform(0.45, 1.2))], [abs(right[0] - left[0]) / count * 0.7, 8, 0.5])


def strands(b, root, spread, sway, length, count, width, curl=26, rise=0.0):
    """머리에서 흘러내리는 머리 타래 여러 가닥. spread는 뿌리의 너비, sway는 끝이 쏠리는 쪽이다."""
    for k in range(count):
        t = k / max(1, count - 1) * 2 - 1
        x, y = root[0] + t * spread, root[1] + abs(t) * spread * 0.35
        long = length * b.rng.uniform(0.72, 1.06)
        end = x + t * spread * 1.1 + sway * b.rng.uniform(0.6, 1.15)
        bend = curl * b.rng.uniform(-1, 1)
        b.ribbon([(x, y), (x + (end - x) * 0.3 + t * 10, y + long * 0.28 - rise * long), (x + (end - x) * 0.62 + bend, y + long * 0.62 - rise * long * 0.6), (end, y + long)], [width * 0.7, width, width * 0.62, 0.5])
