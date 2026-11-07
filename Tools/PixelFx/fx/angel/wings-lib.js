// 옆을 보고 선 인물(4·9장 카일)의 등에 붙는 깃털 날개(사용자 요청 2026-11-05: 웹게임 날개는 앞모습이라 옆으로 선 카일에게 어색하다).
// 왼쪽을 보는 몸을 기준으로 등 쪽(+x)으로 뻗는다. 가까운 날개 뒤에 먼 날개를 조금 돌려 어둡게 겹쳐 앞뒤가 보인다.
// 깃은 팔을 따라 줄지어 붙는다. spread 0은 등에 접어 내린 모습, 1은 하늘로 활짝 든 모습이고, 그 사이는 팔과 깃의 각도만 바뀐다.
import { fit, hex } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#6b6478", "#8f8a9e", "#b4afbe", "#d8d3d8", "#f1ece4", "#fffdf6", "#ffe39a", "#e0a93c"];
const OUTLINE = hex("#26222f");
const SEAM = hex("#6f6980");

export const colors = (o) => fit(o.palette ?? PALETTE, 8);

const lerp = (a, b, t) => a + (b - a) * t;
const toRad = (d) => (d * Math.PI) / 180;

// 앞날은 어깨에서 위로 솟았다가 뒤로 굽는 곡선이다. 어깨에서 잰 자리. [접힘, 편 모습]
const ARM_BEND = [{ x: 44, y: -34 }, { x: 14, y: -214 }];
const ARM_TIP = [{ x: 30, y: 56 }, { x: 150, y: -232 }];

// 깃의 층: 아래층부터 칠한다. reach는 길이 배율, count는 줄지어 붙는 수다. 깃 사이로 뒤가 뚫려 보이지 않게 촘촘히 심는다.
const LAYERS = [
  { count: 34, reach: 1, width: 15, tone: 3 },
  { count: 30, reach: 0.68, width: 15, tone: 4 },
  { count: 26, reach: 0.42, width: 14, tone: 4 },
  { count: 22, reach: 0.24, width: 13, tone: 5 },
];
const FEATHER_LONGEST = 186;
const FEATHER_BOW = 0.1; // 깃이 아래로 휘는 정도
const FAR_TURN = -13; // 먼 날개는 조금 더 세워 뒤로 돌린다.
const FAR_SHIFT = { x: -6, y: -4 };
const FAR_SHADE = 1; // 먼 날개는 팔레트를 이만큼 어둡게 쓴다.

const along = (from, degrees, length) => ({
  x: from.x + Math.cos(toRad(degrees)) * length,
  y: from.y + Math.sin(toRad(degrees)) * length,
});

const mixPoint = (a, b, t) => ({ x: lerp(a.x, b.x, t), y: lerp(a.y, b.y, t) });

/** 어깨에서 손끝까지의 앞날. at(t)는 앞날 위의 자리(0 어깨, 1 손끝)를 돌려준다. */
function armPath(shoulder, spread, k) {
  const offset = (keys) => ({ x: shoulder.x + lerp(keys[0].x, keys[1].x, spread) * k, y: shoulder.y + lerp(keys[0].y, keys[1].y, spread) * k });
  const bend = offset(ARM_BEND), tip = offset(ARM_TIP);
  return (t) => {
    const u = 1 - t;
    return { x: u * u * shoulder.x + 2 * u * t * bend.x + t * t * tip.x, y: u * u * shoulder.y + 2 * u * t * bend.y + t * t * tip.y };
  };
}

/** 깃이 뻗는 각도: 어깨 쪽은 아래로 드리우고, 손끝으로 갈수록 뒤로 눕는다. 접으면 모두 아래로 모인다. */
function featherAngle(t, spread) {
  const open = lerp(92, -14, Math.pow(t, 1.15));
  const folded = lerp(84, 92, t);
  return lerp(folded, open, spread);
}

/** 손끝 쪽 깃이 가장 길다. 접으면 몸 길이에 맞게 줄어든다. */
function featherLength(t, spread) {
  const open = lerp(0.46, 1, Math.min(1, t / 0.7)) * lerp(1, 0.8, Math.max(0, (t - 0.7) / 0.3));
  return lerp(0.38, open, spread);
}

function featherOutline(root, degrees, length, width) {
  const tip = along(root, degrees, length);
  const bend = along(mixPoint(root, tip, 0.55), degrees + 90, length * FEATHER_BOW);
  const normal = toRad(degrees + 90);
  const left = [], right = [];
  const steps = 7;
  for (let i = 0; i <= steps; i++) {
    const u = i / steps, v = 1 - u;
    const x = v * v * root.x + 2 * v * u * bend.x + u * u * tip.x;
    const y = v * v * root.y + 2 * v * u * bend.y + u * u * tip.y;
    // 밑동 가까이가 가장 넓고 끝까지 길게 좁아져 뾰족하다. 밑동이 뾰족하면 깃 사이가 비므로 뭉툭하게 둔다.
    const half = (width / 2) * Math.max(Math.pow(Math.sin(Math.PI * Math.pow(u, 0.62)), 0.7), 0.6 * (1 - u) * (1 - u));
    left.push([x + Math.cos(normal) * half, y + Math.sin(normal) * half]);
    right.push([x - Math.cos(normal) * half, y - Math.sin(normal) * half]);
  }
  return { left, right, shaft: left.map(([x, y], i) => [(x + right[i][0]) / 2, (y + right[i][1]) / 2]) };
}

function feather(p, C, root, degrees, length, width, tone) {
  const { left, right, shaft } = featherOutline(root, degrees, length, width);
  const rim = [...left, ...[...right].reverse()];
  p.poly(rim, C[tone]);
  // 아랫결은 한 단 어둡다. 깃마다 그늘이 져서 겹이 보인다.
  p.poly([...shaft, ...[...left].reverse()], C[Math.max(0, tone - 1)]);
  p.polyline(rim, SEAM);
  p.polyline(shaft.slice(0, 6), C[Math.min(5, tone + 1)]);
}

function wing(p, C, shoulder, spread, sway, k, shade) {
  const arm = armPath(shoulder, spread, k);
  for (const layer of LAYERS) {
    const tone = Math.max(0, layer.tone - shade);
    // 손끝 쪽부터 칠한다. 안쪽 깃이 바깥 깃을 덮는다.
    for (let i = layer.count - 1; i >= 0; i--) {
      const t = (i + 0.5) / layer.count;
      const degrees = featherAngle(t, spread) + sway * t;
      const length = FEATHER_LONGEST * layer.reach * featherLength(t, spread) * k;
      feather(p, C, arm(t), degrees, length, layer.width * k * lerp(1.1, 0.9, t), tone);
    }
  }

  // 앞날: 금빛 테를 두른 팔. 끝으로 갈수록 가늘다.
  const steps = 24;
  for (let i = 0; i < steps; i++) {
    const a = arm(i / steps), b = arm((i + 1) / steps);
    p.beam(a.x, a.y, b.x, b.y, Math.max(2, lerp(13, 3, i / steps) * k), C[Math.max(0, 5 - shade)]);
  }
  for (let i = 0; i < steps; i++) {
    const a = arm(i / steps), b = arm((i + 1) / steps);
    const lift = lerp(6, 1.5, i / steps) * k;
    p.line(a.x, a.y - lift, b.x, b.y - lift, C[7 - shade]);
  }
}

/** 그 자리를 축으로 돌려서 그린다. 먼 날개를 어깨를 축으로 세우는 데 쓴다. */
function turned(p, at, degrees, draw) {
  const { tx, ty, fx, cs, sn } = p;
  p.place(p.toX(at.x, at.y), p.toY(at.x, at.y), fx < 0, toRad(degrees));
  draw();
  p.tx = tx;
  p.ty = ty;
  p.fx = fx;
  p.cs = cs;
  p.sn = sn;
}

/** 먼 날개와 가까운 날개. origin은 등(어깨 높이), sway는 날갯짓에 따라 깃이 흔들리는 각도, k는 크기 배율이다. */
export function drawSideWings(p, C, spread, sway, origin, k = 1) {
  const far = { x: origin.x + FAR_SHIFT.x * k, y: origin.y + FAR_SHIFT.y * k };
  turned(p, far, FAR_TURN * spread, () => wing(p, C, { x: 0, y: 0 }, spread, sway, k, FAR_SHADE));
  wing(p, C, origin, spread, sway, k, 0);
}

/** 인물 그림처럼 어두운 테두리를 두른다. */
export function outline(p, thickness) {
  for (let pass = 0; pass < thickness; pass++) {
    const solid = p.C.slice();
    for (let y = 1; y < p.h - 1; y++) {
      for (let x = 1; x < p.w - 1; x++) {
        const i = y * p.w + x;
        if (!solid[i] && (solid[i - 1] || solid[i + 1] || solid[i - p.w] || solid[i + p.w])) {
          p.C[i] = OUTLINE;
        }
      }
    }
  }
}
