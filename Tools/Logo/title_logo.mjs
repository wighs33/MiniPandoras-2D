// 타이틀 로고 「미니 판도라즈」와, 그 「판」으로 만든 안드로이드 앱 아이콘을 그린다(2026-11-15).
//
// 컨셉은 판도라의 상자다. 「판」의 ㅍ이 상자 모양이라 윗획을 뚜껑 삼아 오른쪽을 들어 올리고, 그 틈에서 빛이 새어 나와
// 일곱 판도라(차원·악마·불꽃·모래·뱀·얼음·바람)의 색 별을 흩뿌리며 글자 위를 흘러간다. 「즈」의 ㅡ는 꼬리가 되어 감겨 오른다.
// 「MINI」는 영어로, 로마 대문자(Cinzel)로 「판」 왼쪽 위에 작게 얹는다(2026-11-15 사용자 결정: 미니는 영어로).
//
// 글자 모양은 Noto Serif KR Bold(Assets/ThirdParty/NotoSerifKR)와 Cinzel(Assets/ThirdParty/Cinzel, 둘 다 SIL OFL 1.1)의
// 윤곽을 다각형으로 읽어 쓰고, 뚜껑·꼬리만 고쳐 붙인다(로고는 글꼴로 그린 그림이라 글꼴 파일을 바꾸거나 나눠 주는 것이 아니다).
// 효과(먹 테두리·보랏빛 번짐·빛)는 SVG 필터로 주고, Chromium으로 찍어 투명 PNG로 쓴다.
//
// 사용: node Tools/Logo/title_logo.mjs <node_modules 폴더> <프로젝트 루트> [--chrome <chrome 실행 파일>] [--out <png>]
//   node_modules 폴더에는 fontkit, polygon-clipping, playwright-core가 있어야 한다.
//   --chrome을 비우면 playwright-core가 설치한 Chromium을 쓴다.
//   → Assets/MiniPandoras/Art/UI/Title/logo.png   (투명 여백을 잘라 내고 가로세로를 4의 배수로 맞춘다)
//   → Assets/MiniPandoras/Art/UI/AppIcon/icon.png, icon_background.png, icon_foreground.png (1024×1024)
//     앱 아이콘은 뚜껑이 열린 「판」과 일곱 판도라 빛을 밤하늘에 둔다. 안드로이드 적응형 아이콘은 런처가 바깥을 잘라 내므로
//     앞 그림(foreground)은 가운데 지름 66% 원 안에 들어가게 작게, 예전 방식 아이콘(icon)은 바탕과 합쳐 크게 그린다.
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const [modulesDir, projectDir] = process.argv.slice(2);
if (!modulesDir || !projectDir) {
  console.error('사용: node title_logo.mjs <node_modules 폴더> <프로젝트 루트> [--chrome <exe>]');
  process.exit(1);
}
const option = name => { const i = process.argv.indexOf(name); return i > 0 ? process.argv[i + 1] : undefined; };
const chromePath = option('--chrome');
const require = createRequire(path.join(path.resolve(modulesDir), 'index.js'));
const fontkit = require('fontkit');
const clipping = require('polygon-clipping');
const pc = clipping.default || clipping;
const { chromium } = require('playwright-core');

const OUT = option('--out') ?? path.join(projectDir, 'Assets/MiniPandoras/Art/UI/Title/logo.png');
const hangulFont = fontkit.openSync(path.join(projectDir, 'Assets/ThirdParty/NotoSerifKR/NotoSerifKR-Bold.otf'));
const latinFont = fontkit.openSync(path.join(projectDir, 'Assets/ThirdParty/Cinzel/Cinzel-wght.ttf')).getVariation({ wght: 900 });

// ---------- 다각형 ----------
// 모양은 polygon-clipping의 MultiPolygon: [다각형[고리[점[x, y]]]]. 좌표는 y가 아래로 커진다.

function bbox(ring) {
  let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
  for (const [x, y] of ring) {
    x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y);
  }
  return [x0, y0, x1, y1];
}

const rect = (x0, y0, x1, y1) => [[[x0, y0], [x1, y0], [x1, y1], [x0, y1], [x0, y0]]];

function rotateAbout(shape, cx, cy, degrees, dx = 0, dy = 0) {
  const a = degrees * Math.PI / 180, c = Math.cos(a), s = Math.sin(a);
  return shape.map(poly => poly.map(ring => ring.map(([x, y]) =>
    [cx + (x - cx) * c - (y - cy) * s + dx, cy + (x - cx) * s + (y - cy) * c + dy])));
}

const toPath = shape => shape.map(poly => poly.map(ring =>
  'M' + ring.map(([x, y]) => x.toFixed(2) + ' ' + y.toFixed(2)).join('L') + 'Z').join('')).join('');

/** 세로선 x에서 다각형 고리가 차지하는 위·아래 끝. */
function spanAt(ring, x) {
  const ys = [];
  for (let i = 0; i < ring.length - 1; i++) {
    const [ax, ay] = ring[i], [bx, by] = ring[i + 1];
    if ((ax - x) * (bx - x) <= 0 && ax !== bx) {
      ys.push(ay + (by - ay) * (x - ax) / (bx - ax));
    }
  }
  return [Math.min(...ys), Math.max(...ys)];
}

// ---------- 글자 윤곽 ----------

/** 글리프 하나를 조각(이어진 덩어리)들의 MultiPolygon으로. 글꼴 단위를 size px로 바꾸고 (ox, baseline)에 놓는다. */
function glyphShape(font, glyph, size, ox, baseline) {
  const k = size / font.unitsPerEm;
  const at = (x, y) => [ox + x * k, baseline - y * k];
  const rings = [];
  let ring = null, last = [0, 0];
  for (const { command, args: a } of glyph.path.commands) {
    if (command === 'moveTo') {
      if (ring) rings.push(ring);
      ring = [at(a[0], a[1])]; last = [a[0], a[1]];
    } else if (command === 'lineTo') {
      ring.push(at(a[0], a[1])); last = [a[0], a[1]];
    } else if (command === 'quadraticCurveTo') {
      for (let i = 1; i <= 8; i++) {
        const t = i / 8, u = 1 - t;
        ring.push(at(u * u * last[0] + 2 * u * t * a[0] + t * t * a[2], u * u * last[1] + 2 * u * t * a[1] + t * t * a[3]));
      }
      last = [a[2], a[3]];
    } else if (command === 'bezierCurveTo') {
      for (let i = 1; i <= 12; i++) {
        const t = i / 12, u = 1 - t;
        ring.push(at(u * u * u * last[0] + 3 * u * u * t * a[0] + 3 * u * t * t * a[2] + t * t * t * a[4],
          u * u * u * last[1] + 3 * u * u * t * a[1] + 3 * u * t * t * a[3] + t * t * t * a[5]));
      }
      last = [a[4], a[5]];
    } else if (command === 'closePath' && ring) {
      rings.push(ring); ring = null;
    }
  }
  if (ring) rings.push(ring);
  const closed = rings.filter(r => r.length > 2).map(r => [...r, r[0]]);
  // 글꼴 윤곽은 바깥과 구멍의 감기는 방향이 반대다(어느 쪽이 바깥인지는 CFF와 TrueType이 서로 반대).
  // 가장 큰 고리는 늘 바깥이므로 그 넓이 부호로 나눠, 바깥은 합치고 구멍은 뺀다.
  const area = r => r.slice(1).reduce((s, p, i) => s + r[i][0] * p[1] - p[0] * r[i][1], 0);
  const outerSign = Math.sign(area(closed.reduce((a, b) => (Math.abs(area(b)) > Math.abs(area(a)) ? b : a))));
  const outers = closed.filter(r => Math.sign(area(r)) === outerSign), holes = closed.filter(r => Math.sign(area(r)) !== outerSign);
  let shape = pc.union(...outers.map(r => [r]));
  if (holes.length) shape = pc.difference(shape, ...holes.map(r => [r]));
  return { pieces: shape.map(poly => [poly]), advance: glyph.advanceWidth * k };
}

/** 글을 글꼴의 짝 간격(커닝)대로 늘어놓은 조각들. tracking은 글자 사이에 더하는 px. */
function textShape(font, text, size, ox, baseline, tracking = 0) {
  const run = font.layout(text);
  const k = size / font.unitsPerEm;
  const pieces = [];
  let pen = ox;
  run.glyphs.forEach((glyph, i) => {
    pieces.push(...glyphShape(font, glyph, size, pen + run.positions[i].xOffset * k, baseline).pieces);
    pen += run.positions[i].xAdvance * k + tracking;
  });
  return { pieces, width: pen - tracking - ox };
}

// ---------- 붓 획(꼬리, 빛 리본) ----------

function cubicAt([p0, p1, p2, p3], t) {
  const u = 1 - t;
  return [0, 1].map(i => u * u * u * p0[i] + 3 * u * u * t * p1[i] + 3 * u * t * t * p2[i] + t * t * t * p3[i]);
}

/** 3차 곡선을 이어 붙인 길을 같은 간격(step px)의 점으로. */
function sampleCurve(segments, step = 2) {
  const dense = [];
  for (const seg of segments) {
    for (let i = dense.length ? 1 : 0; i <= 60; i++) dense.push(cubicAt(seg, i / 60));
  }
  const length = [0];
  for (let i = 1; i < dense.length; i++) length.push(length[i - 1] + Math.hypot(dense[i][0] - dense[i - 1][0], dense[i][1] - dense[i - 1][1]));
  const total = length.at(-1), count = Math.max(20, Math.round(total / step)), points = [];
  for (let k = 0, j = 0; k <= count; k++) {
    const d = total * k / count;
    while (j < dense.length - 2 && length[j + 1] < d) j++;
    const f = (d - length[j]) / Math.max(1e-6, length[j + 1] - length[j]);
    points.push([dense[j][0] + (dense[j + 1][0] - dense[j][0]) * f, dense[j][1] + (dense[j + 1][1] - dense[j][1]) * f]);
  }
  return { points, total };
}

const smoothstep = (a, b, t) => { const x = Math.min(1, Math.max(0, (t - a) / (b - a))); return x * x * (3 - 2 * x); };

/** 길(segments)을 따라 폭이 변하는 붓 획. widths는 [길 위 위치(0~1), 폭] 목록이고 그 사이는 부드럽게 잇는다. */
function brush(segments, widths) {
  const { points } = sampleCurve(segments);
  const widthAt = t => {
    for (let i = 1; i < widths.length; i++) {
      if (t <= widths[i][0]) return widths[i - 1][1] + (widths[i][1] - widths[i - 1][1]) * smoothstep(widths[i - 1][0], widths[i][0], t);
    }
    return widths.at(-1)[1];
  };
  const left = [], right = [];
  points.forEach((p, i) => {
    const a = points[Math.max(0, i - 1)], b = points[Math.min(points.length - 1, i + 1)];
    const len = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1;
    const tx = (b[0] - a[0]) / len, ty = (b[1] - a[1]) / len;
    const half = widthAt(i / (points.length - 1)) / 2;
    left.push([p[0] - ty * half, p[1] + tx * half]);
    right.push([p[0] + ty * half, p[1] - tx * half]);
  });
  const ring = [...left, ...right.reverse()];
  return [[[...ring, ring[0]]]];
}

// ---------- 판도라즈 ----------

const SIZE = 300;
const TRACKING = -14;
// 글자마다 조금씩 띄우고 기울여 박자를 준다: [dx, dy, 회전(도)]
const LILT = { 판: [0, 6, -2], 도: [0, -4, 1.5], 라: [0, 3, -1.2], 즈: [0, -5, 1.8] };
const letters = {};
let pen = 0;
for (const ch of '판도라즈') {
  const { pieces, advance } = glyphShape(hangulFont, hangulFont.layout(ch).glyphs[0], SIZE, pen, 300);
  const [dx, dy, degrees] = LILT[ch];
  letters[ch] = pieces.map(piece => rotateAbout(piece, pen + advance / 2, 190, degrees, dx, dy));
  pen += advance + TRACKING;
}

// 판: 따로 떨어진 조각인 ㅍ의 윗획이 뚜껑이다. 왼쪽 끝을 경첩 삼아 오른쪽을 들어 올린다.
const pan = letters['판'];
const lidIndex = pan.findIndex(piece => { const b = bbox(piece[0][0]); return b[3] - b[1] < 45 && b[1] < 130; });
const lidClosed = pan[lidIndex];
const lidBox = bbox(lidClosed[0][0]);
pan[lidIndex] = rotateAbout(lidClosed, lidBox[0] + 8, lidBox[3] - 4, -11, 0, -20);
const gap = [lidBox[0] + (lidBox[2] - lidBox[0]) * 0.68, lidBox[3] - 20];   // 뚜껑이 가장 많이 벌어진 곳

// 즈: ㅡ의 오른쪽 맺음을 잘라 내고, 자른 자리의 굵기·기울기 그대로 길게 뻗어 감겨 오르는 꼬리로 잇는다.
const jeu = letters['즈'];
const barIndex = jeu.findIndex(piece => bbox(piece[0][0])[1] > 220);
const bar = jeu[barIndex];
const barBox = bbox(bar[0][0]);
const cutX = barBox[2] - 52;
const x0 = cutX - 60, x1 = cutX - 1;
const [top0, bottom0] = spanAt(bar[0][0], x0), [top1, bottom1] = spanAt(bar[0][0], x1);
const y0 = (top0 + bottom0) / 2, y1 = (top1 + bottom1) / 2, w1 = bottom1 - top1;
const along = x => y1 + (y1 - y0) / (x1 - x0) * (x - x1);
const tailPath = [
  [[x0, y0], [x0 + 20, along(x0 + 20)], [x1 - 20, along(x1 - 20)], [x1, y1]],
  [[x1, y1], [x1 + 50, along(x1 + 50)], [x1 + 110, y1 - 4], [x1 + 150, y1 - 16]],
  [[x1 + 150, y1 - 16], [x1 + 196, y1 - 30], [x1 + 222, y1 - 70], [x1 + 208, y1 - 108]],
  [[x1 + 208, y1 - 108], [x1 + 198, y1 - 134], [x1 + 166, y1 - 138], [x1 + 158, y1 - 114]],
];
const joinAt = Math.hypot(x1 - x0, y1 - y0) / sampleCurve(tailPath).total;   // 꼬리 길이 중 자른 자리까지의 몫
const tail = brush(tailPath, [[0, (bottom0 - top0) * 0.92], [joinAt, w1], [joinAt + 0.15, w1 * 0.86], [0.55, w1 * 0.6], [0.82, w1 * 0.3], [1, 0.5]]);
jeu[barIndex] = pc.union(pc.difference(bar, rect(cutX, barBox[1] - 20, barBox[2] + 20, barBox[3] + 20)), tail);

// MINI: 「판」 왼쪽, 윗부분 높이에 작게. 글자 사이를 벌린 로마 대문자를 판도라즈처럼 살짝 기울인다.
const mini = textShape(latinFont, 'MINI', 84, 0, 0, 12);
const miniCenter = [-142, 98];
const miniPieces = mini.pieces.map(piece => rotateAbout(piece, mini.width / 2, -36, -7, miniCenter[0] - mini.width / 2, miniCenter[1] + 36));

const letterPath = [...Object.values(letters).flat(), ...miniPieces].map(toPath).join('');

// ---------- 빛 ----------

// 일곱 판도라의 색: 차원, 악마, 불꽃, 모래, 뱀, 얼음, 바람
const PANDORA_COLORS = ['#a294ff', '#ff5d9a', '#ff8a3d', '#f2c24f', '#62d474', '#80e6f7', '#ffffff'];

// 틈에서 오른쪽 위로 피어올라 글자 위를 흘러가는 빛 리본
const ribbonPath = [
  [[gap[0], gap[1]], [gap[0] + 30, gap[1] - 70], [gap[0] + 110, gap[1] - 110], [gap[0] + 230, gap[1] - 96]],
  [[gap[0] + 230, gap[1] - 96], [gap[0] + 380, gap[1] - 80], [gap[0] + 520, gap[1] - 150], [gap[0] + 700, gap[1] - 118]],
  [[gap[0] + 700, gap[1] - 118], [gap[0] + 820, gap[1] - 98], [gap[0] + 900, gap[1] - 60], [gap[0] + 960, gap[1] - 70]],
];
const ribbon = toPath(brush(ribbonPath, [[0, 4], [0.08, 26], [0.3, 20], [0.7, 10], [1, 0]]));
const ribbonCore = toPath(brush(ribbonPath, [[0, 2], [0.08, 7], [0.4, 5], [0.85, 1.5], [1, 0]]));
const ribbonPoints = sampleCurve(ribbonPath, 4).points;
const pointOnRibbon = t => ribbonPoints[Math.round(t * (ribbonPoints.length - 1))];

function sparkle(x, y, r, color, turn) {
  const k = r * 0.2, c = Math.cos(turn), s = Math.sin(turn);
  const tips = [[0, -r], [k, -k], [r * 0.72, 0], [k, k], [0, r * 0.9], [-k, k], [-r * 0.72, 0], [-k, -k]];
  const d = 'M' + tips.map(([px, py]) => (x + px * c - py * s).toFixed(1) + ' ' + (y + px * s + py * c).toFixed(1)).join('L') + 'Z';
  return `<circle cx="${x.toFixed(1)}" cy="${y.toFixed(1)}" r="${(r * 1.5).toFixed(1)}" fill="${color}" opacity="0.5" filter="url(#blur6)"/>`
    + `<path d="${d}" fill="${color}"/><circle cx="${x.toFixed(1)}" cy="${y.toFixed(1)}" r="${(r * 0.26).toFixed(1)}" fill="#fff"/>`;
}

const sparkles = [0.15, 0.27, 0.39, 0.51, 0.64, 0.77, 0.9].map((t, i) => {
  const [x, y] = pointOnRibbon(t);
  const side = i % 2 ? -1 : 1;
  return sparkle(x, y + side * (10 + i * 2), 21 - i * 1.6, PANDORA_COLORS[i], side * -0.15);
}).join('');

let dust = '';
for (let i = 0; i < 26; i++) {
  const [x, y] = pointOnRibbon((i * 0.618) % 1);
  dust += `<circle cx="${(x + Math.sin(i * 12.9) * 34).toFixed(1)}" cy="${(y + Math.cos(i * 7.3) * 30).toFixed(1)}" r="${(1.6 + (i % 3) * 1.1).toFixed(1)}" fill="#fff6dc" opacity="${(0.5 + (i % 4) * 0.12).toFixed(2)}"/>`;
}

// 틈에서 위로 퍼지는 빛살
let rays = '';
for (const [degrees, len, w, opacity] of [[-118, 150, 16, 0.5], [-100, 200, 22, 0.7], [-82, 175, 20, 0.6], [-64, 140, 14, 0.45], [-136, 110, 10, 0.35]]) {
  const a = degrees * Math.PI / 180, ex = gap[0] + Math.cos(a) * len, ey = gap[1] + Math.sin(a) * len;
  const nx = -Math.sin(a) * w, ny = Math.cos(a) * w;
  rays += `<path d="M${gap[0] - nx * 0.2} ${gap[1] - ny * 0.2}L${ex - nx} ${ey - ny}L${ex + nx} ${ey + ny}L${gap[0] + nx * 0.2} ${gap[1] + ny * 0.2}Z" fill="url(#ray)" opacity="${opacity}"/>`;
}

// ---------- 앱 아이콘 ----------
// 같은 「판」 조각(뚜껑이 열린 상자)을 쓰고, 빛은 아이콘 안에 들어가게 짧게 다시 그린다.
const ICON_DIR = path.join(projectDir, 'Assets/MiniPandoras/Art/UI/AppIcon');
const ICON_PX = 1024;
const panPath = letters['판'].map(toPath).join('');
const iconArc = [
  [[gap[0], gap[1]], [gap[0] + 18, gap[1] - 80], [gap[0] + 120, gap[1] - 138], [gap[0] + 196, gap[1] - 92]],
];
const iconArcPoints = sampleCurve(iconArc, 3).points;
const iconSparkles = [0.12, 0.26, 0.4, 0.54, 0.68, 0.82, 0.96].map((t, i) => {
  const [x, y] = iconArcPoints[Math.round(t * (iconArcPoints.length - 1))];
  return sparkle(x, y, 17 - i * 0.9, PANDORA_COLORS[i], (i % 2 ? 1 : -1) * 0.15);
}).join('');
const iconRibbon = toPath(brush(iconArc, [[0, 3], [0.1, 16], [0.5, 10], [1, 0]]));
let iconRays = '';
for (const [degrees, len, w, opacity] of [[-112, 100, 13, 0.55], [-94, 130, 17, 0.75], [-76, 112, 15, 0.6], [-130, 80, 9, 0.4]]) {
  const a = degrees * Math.PI / 180, ex = gap[0] + Math.cos(a) * len, ey = gap[1] + Math.sin(a) * len;
  const nx = -Math.sin(a) * w, ny = Math.cos(a) * w;
  iconRays += `<path d="M${gap[0] - nx * 0.2} ${gap[1] - ny * 0.2}L${ex - nx} ${ey - ny}L${ex + nx} ${ey + ny}L${gap[0] + nx * 0.2} ${gap[1] + ny * 0.2}Z" fill="url(#ray)" opacity="${opacity}"/>`;
}
// 아이콘의 가운데: 세로는 「판」과 빛 전체의 가운데, 가로는 「판」을 가운데에 두고 별 줄 쪽으로 조금 민다
const panBox = letters['판'].map(piece => bbox(piece[0][0])).reduce((a, b) => [Math.min(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[2], b[2]), Math.max(a[3], b[3])]);
const iconTop = Math.min(panBox[1], ...iconArcPoints.map(p => p[1])) - 20;
const iconCenter = [(panBox[0] + panBox[2]) / 2 + 40, (iconTop + panBox[3]) / 2];

// 밤하늘 바탕: 가운데가 보랏빛으로 밝고 둘레로 어두워지며, 작은 별이 흩어진다(별 자리는 정해진 수열이라 늘 같다).
function iconBackground(view) {
  const [vx, vy] = [iconCenter[0] - view / 2, iconCenter[1] - view / 2];
  let stars = '';
  for (let i = 0; i < 46; i++) {
    const x = vx + ((i * 0.754877) % 1) * view, y = vy + ((i * 0.569840 + 0.13) % 1) * view;
    stars += `<circle cx="${x.toFixed(1)}" cy="${y.toFixed(1)}" r="${(view / 600 * (1 + (i % 3) * 0.7)).toFixed(2)}" fill="#f3e9d8" opacity="${(0.25 + (i % 5) * 0.12).toFixed(2)}"/>`;
  }
  return `<radialGradient id="sky" gradientUnits="userSpaceOnUse" cx="${iconCenter[0]}" cy="${iconCenter[1] - view * 0.06}" r="${view * 0.72}">
      <stop offset="0" stop-color="#3a3478"/><stop offset="0.45" stop-color="#1c2550"/><stop offset="1" stop-color="#081421"/></radialGradient>
    <rect x="${vx}" y="${vy}" width="${view}" height="${view}" fill="url(#sky)"/>${stars}
    <circle cx="${iconCenter[0]}" cy="${iconCenter[1]}" r="${view * 0.3}" fill="#6a58f0" opacity="0.22" filter="url(#blur40)"/>`;
}

function iconForeground() {
  return `<path d="${iconRibbon}" fill="url(#ribbon)" opacity="0.6"/>
    <g style="mix-blend-mode:screen" filter="url(#blur3)">${iconRays}</g>
    <g filter="url(#ink)"><path d="${panPath}" fill="url(#fill)" filter="url(#bevel)"/></g>
    <path d="${toPath(lidClosed)}" fill="url(#gapLight)" filter="url(#blur3)"/>
    <path d="${panPath}" fill="url(#fill)" filter="url(#bevel)"/>
    <ellipse cx="${gap[0]}" cy="${gap[1]}" rx="120" ry="36" fill="url(#gapGlow)" style="mix-blend-mode:screen" opacity="0.9"/>
    ${iconSparkles}`;
}

/** view: 아이콘 한 변에 담을 디자인 단위. 클수록 그림이 작아진다. */
function iconSvg(view, layers) {
  const vx = iconCenter[0] - view / 2, vy = iconCenter[1] - view / 2;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${ICON_PX}" height="${ICON_PX}" viewBox="${vx} ${vy} ${view} ${view}">${DEFS}
    ${layers.includes('background') ? iconBackground(view) : ''}${layers.includes('foreground') ? iconForeground() : ''}</svg>`;
}

// ---------- 그리기 ----------
const VIEW = { x: -330, y: -170, w: 1700, h: 560 };
const SCALE = 1.25;
const DEFS = `<defs>
  <linearGradient id="fill" gradientUnits="userSpaceOnUse" x1="0" y1="40" x2="0" y2="340">
    <stop offset="0" stop-color="#ffffff"/><stop offset="0.38" stop-color="#fff3dd"/>
    <stop offset="0.72" stop-color="#f4c98c"/><stop offset="1" stop-color="#d08a45"/>
  </linearGradient>
  <linearGradient id="ribbon" gradientUnits="userSpaceOnUse" x1="${gap[0]}" y1="0" x2="${gap[0] + 960}" y2="0">
    <stop offset="0" stop-color="#fffbe9"/><stop offset="0.25" stop-color="#ffe7b0" stop-opacity="0.85"/>
    <stop offset="0.7" stop-color="#c9bcff" stop-opacity="0.55"/><stop offset="1" stop-color="#a294ff" stop-opacity="0"/>
  </linearGradient>
  <linearGradient id="ray" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#fff6d6"/><stop offset="1" stop-color="#ffd27a" stop-opacity="0"/></linearGradient>
  <linearGradient id="gapLight" gradientUnits="userSpaceOnUse" x1="${lidBox[0]}" y1="0" x2="${lidBox[2]}" y2="0">
    <stop offset="0.12" stop-color="#fff4d0" stop-opacity="0"/><stop offset="0.5" stop-color="#fff4d0" stop-opacity="0.75"/><stop offset="1" stop-color="#ffffff"/>
  </linearGradient>
  <radialGradient id="gapGlow"><stop offset="0" stop-color="#ffffff"/><stop offset="0.3" stop-color="#fff0c4" stop-opacity="0.9"/><stop offset="1" stop-color="#ffb347" stop-opacity="0"/></radialGradient>
  <filter id="blur3" x="-0.5" y="-1" width="2" height="3"><feGaussianBlur stdDeviation="3"/></filter>
  <filter id="blur6" x="-1" y="-1" width="3" height="3"><feGaussianBlur stdDeviation="6"/></filter>
  <filter id="blur10" x="-0.5" y="-1" width="2" height="3"><feGaussianBlur stdDeviation="10"/></filter>
  <!-- 글자 면: 윗가장자리는 빛, 아랫가장자리는 그늘 -->
  <filter id="bevel" x="-5%" y="-10%" width="110%" height="120%">
    <feOffset in="SourceAlpha" dy="3.2" result="down"/>
    <feComposite in="SourceAlpha" in2="down" operator="out" result="top"/>
    <feGaussianBlur in="top" stdDeviation="0.8" result="topSoft"/>
    <feFlood flood-color="#ffffff"/><feComposite in2="topSoft" operator="in" result="light"/>
    <feOffset in="SourceAlpha" dy="-4" result="up"/>
    <feComposite in="SourceAlpha" in2="up" operator="out" result="bottom"/>
    <feFlood flood-color="#9b5a26" flood-opacity="0.85"/><feComposite in2="bottom" operator="in" result="shade"/>
    <feMerge><feMergeNode in="SourceGraphic"/><feMergeNode in="shade"/><feMergeNode in="light"/></feMerge>
  </filter>
  <!-- 먹 테두리, 보랏빛 번짐, 아래 그림자 -->
  <filter id="ink" x="-15%" y="-30%" width="130%" height="160%">
    <feMorphology in="SourceAlpha" operator="dilate" radius="6" result="thick"/>
    <feFlood flood-color="#1d1230"/><feComposite in2="thick" operator="in" result="rim"/>
    <feGaussianBlur in="thick" stdDeviation="13" result="blur"/>
    <feFlood flood-color="#6a58f0" flood-opacity="0.72"/><feComposite in2="blur" operator="in" result="glow"/>
    <feOffset in="thick" dy="9" result="drop"/><feGaussianBlur in="drop" stdDeviation="4" result="dropSoft"/>
    <feFlood flood-color="#0a0614" flood-opacity="0.65"/><feComposite in2="dropSoft" operator="in" result="shadow"/>
    <feMerge><feMergeNode in="glow"/><feMergeNode in="shadow"/><feMergeNode in="rim"/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>
  <filter id="blur40" x="-1" y="-1" width="3" height="3"><feGaussianBlur stdDeviation="40"/></filter>
</defs>`;
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${VIEW.w * SCALE}" height="${VIEW.h * SCALE}" viewBox="${VIEW.x} ${VIEW.y} ${VIEW.w} ${VIEW.h}">${DEFS}
<path d="${ribbon}" fill="url(#ribbon)" opacity="0.55" filter="url(#blur10)"/>
<path d="${ribbon}" fill="url(#ribbon)" opacity="0.5"/>
<path d="${ribbonCore}" fill="#fffdf4" opacity="0.85"/>
<g style="mix-blend-mode:screen" filter="url(#blur3)">${rays}</g>
<g filter="url(#ink)"><path d="${letterPath}" fill="url(#fill)" filter="url(#bevel)"/></g>
<!-- 틈의 빛: 닫혀 있던 뚜껑 자리를 먹 테두리 위에 칠하고, 글자 면을 다시 덮어 틈에만 남긴다 -->
<path d="${toPath(lidClosed)}" fill="url(#gapLight)" filter="url(#blur3)"/>
<path d="${letterPath}" fill="url(#fill)" filter="url(#bevel)"/>
<ellipse cx="${gap[0]}" cy="${gap[1]}" rx="150" ry="44" fill="url(#gapGlow)" style="mix-blend-mode:screen" opacity="0.9"/>
${dust}
${sparkles}
</svg>`;

// ---------- 찍기 ----------
// 투명 바탕으로 찍은 뒤, 같은 페이지의 캔버스에서 투명 여백을 잘라 내고 가로세로를 4의 배수로 맞춘다(텍스처 압축 단위).
const browser = await chromium.launch(chromePath ? { executablePath: chromePath } : {});
const page = await browser.newPage();
await page.setContent(`<!doctype html><html><body style="margin:0;background:transparent">${svg}</body></html>`);
const shot = await page.locator('svg').screenshot({ omitBackground: true });
const trimmed = await page.evaluate(async base64 => {
  const image = new Image();
  image.src = 'data:image/png;base64,' + base64;
  await image.decode();
  const source = document.createElement('canvas');
  source.width = image.width; source.height = image.height;
  const ctx = source.getContext('2d');
  ctx.drawImage(image, 0, 0);
  const { data, width, height } = ctx.getImageData(0, 0, image.width, image.height);
  let x0 = width, y0 = height, x1 = 0, y1 = 0;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] > 2) { x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); }
    }
  }
  const w = Math.ceil((x1 - x0 + 1) / 4) * 4, h = Math.ceil((y1 - y0 + 1) / 4) * 4;
  const out = document.createElement('canvas');
  out.width = w; out.height = h;
  out.getContext('2d').drawImage(source, x0, y0, w, h, 0, 0, w, h);
  return out.toDataURL('image/png').split(',')[1];
}, shot.toString('base64'));
fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, Buffer.from(trimmed, 'base64'));
console.log('로고:', OUT);

// 앱 아이콘 셋. 앞 그림은 지름 66% 원 안에 들도록 넓게(view 860), 합친 아이콘은 크게(view 620) 담는다.
const iconDir = option('--icon-out') ?? ICON_DIR;
fs.mkdirSync(iconDir, { recursive: true });
for (const [name, view, layers, transparent] of [
  ['icon.png', 620, ['background', 'foreground'], false],
  ['icon_background.png', 860, ['background'], false],
  ['icon_foreground.png', 860, ['foreground'], true],
]) {
  await page.setContent(`<!doctype html><html><body style="margin:0;background:transparent">${iconSvg(view, layers)}</body></html>`);
  await page.locator('svg').screenshot({ path: path.join(iconDir, name), omitBackground: transparent });
  console.log('아이콘:', path.join(iconDir, name));
}
await browser.close();
