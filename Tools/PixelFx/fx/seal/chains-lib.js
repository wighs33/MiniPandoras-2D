// 4장 제이미가 카일을 묶는 봉인 사슬(사용자 요청 2026-11-05: 코드로 그린 봉인진을 pixelfx 사슬로). 땅의 봉인진에서 빛의 사슬이 솟아 인물의 몸을 감는다.
// 원점은 인물 발밑이고, 점 하나는 논리 px 반이다(effects.json의 pixelsPerUnit 32). 몸을 감는 고리는 앞쪽 절반만 그려 인물을 감싼 것처럼 보인다.
import { fit, hex } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#06282c", "#0d4a4c", "#13716d", "#27a596", "#5fd8c2", "#a8f5e0", "#e4fff4", "#ffffff"];
const OUTLINE = hex("#04191c");
const GLOW = hex("#7fffe0");

export const colors = (o) => fit(o.palette ?? PALETTE, 8);

const RING = { x: 58, y: 24 }; // 땅의 봉인진 반지름
const RUNES = 12;
const LINK = 9; // 사슬 코 하나의 길이
// 사슬이 박힌 땅의 자리와, 몸에서 닿는 자리(왼쪽 앞, 오른쪽 앞, 왼쪽 뒤, 오른쪽 뒤).
const CHAINS = [
  { from: { x: -54, y: 10 }, to: { x: -15, y: -28 }, lift: -7 },
  { from: { x: 54, y: 10 }, to: { x: 15, y: -28 }, lift: -7 },
  { from: { x: -44, y: -13 }, to: { x: -16, y: -46 }, lift: -9 },
  { from: { x: 44, y: -13 }, to: { x: 16, y: -46 }, lift: -9 },
];
const COILS = [{ y: -22, x: 16 }, { y: -36, x: 19 }, { y: -50, x: 21 }]; // 몸을 감는 고리의 높이와 반지름
const COIL_DROP = 6; // 고리가 앞으로 처지는 높이

const lerp = (a, b, t) => a + (b - a) * t;

const bezier = (a, control, b) => (t) => {
  const u = 1 - t;
  return { x: u * u * a.x + 2 * u * t * control.x + t * t * b.x, y: u * u * a.y + 2 * u * t * control.y + t * t * b.y };
};

function pathLength(path) {
  let length = 0;
  let last = path(0);
  for (let i = 1; i <= 24; i++) {
    const next = path(i / 24);
    length += Math.hypot(next.x - last.x, next.y - last.y);
    last = next;
  }
  return length;
}

function linkFace(p, at, angle, color) {
  const cs = Math.cos(angle), sn = Math.sin(angle);
  let last = null;
  for (let i = 0; i <= 12; i++) {
    const a = (i / 12) * Math.PI * 2;
    const lx = Math.cos(a) * 6, ly = Math.sin(a) * 3.6;
    const point = { x: at.x + lx * cs - ly * sn, y: at.y + lx * sn + ly * cs };
    if (last) {
      p.beam(last.x, last.y, point.x, point.y, 2, color);
    }
    last = point;
  }
}

function linkEdge(p, at, angle, color) {
  const dx = Math.cos(angle) * 4.6, dy = Math.sin(angle) * 4.6;
  p.beam(at.x - dx, at.y - dy, at.x + dx, at.y + dy, 3.6, color);
}

/** 사슬 한 가닥. reach까지만 뻗고, pulse(0~1) 자리에 빛이 흐른다. 코는 납작한 고리와 옆으로 선 고리가 번갈아 온다. */
function chain(p, C, path, reach, pulse) {
  const count = Math.max(2, Math.round(pathLength(path) / LINK));
  for (let i = 0; i < count; i++) {
    const t = i / count;
    if (t > reach) {
      break;
    }

    const at = path(t), next = path(Math.min(1, t + 0.02));
    const angle = Math.atan2(next.y - at.y, next.x - at.x);
    const lit = pulse !== null && Math.abs(((t - pulse + 1.5) % 1) - 0.5) < 0.09;
    const head = pulse === null && reach - t < 0.1;
    const color = C[lit || head ? 7 : i % 2 ? 5 : 4];
    (i % 2 ? linkEdge : linkFace)(p, at, angle, color);
    if (lit) {
      p.glow(at.x, at.y, 9, GLOW, 0.5);
    }
  }
}

function groundRing(p, C, size, turn) {
  const ellipse = (rx, ry, color) => {
    let last = null;
    for (let i = 0; i <= 56; i++) {
      const a = (i / 56) * Math.PI * 2;
      const point = { x: Math.cos(a) * rx, y: Math.sin(a) * ry };
      if (last) {
        p.line(last.x, last.y, point.x, point.y, color);
      }
      last = point;
    }
  };
  ellipse(RING.x * size, RING.y * size, C[5]);
  ellipse((RING.x - 9) * size, (RING.y - 4) * size, C[3]);
  ellipse(RING.x * 0.42 * size, RING.y * 0.42 * size, C[3]);
  for (let i = 0; i < RUNES; i++) {
    const a = ((i + turn) / RUNES) * Math.PI * 2;
    const cs = Math.cos(a), sn = Math.sin(a);
    p.line(cs * (RING.x - 8) * size, sn * (RING.y - 3) * size, cs * (RING.x - 1) * size, sn * (RING.y - 1) * size, C[6]);
  }
}

/** 몸을 감는 고리의 앞쪽 절반. */
const coilPath = (coil) => (t) => {
  const a = Math.PI * (1 - t);
  return { x: Math.cos(a) * coil.x, y: coil.y + Math.sin(a) * COIL_DROP };
};

const chainPath = (c, shake) =>
  bezier(c.from, { x: lerp(c.from.x, c.to.x, 0.5), y: lerp(c.from.y, c.to.y, 0.5) + c.lift + shake }, c.to);

/** 봉인진과 사슬 한 장. size는 봉인진 크기, reach는 사슬이 뻗은 정도, coil은 몸을 감은 정도(0~1), pulse는 흐르는 빛의 자리다(없으면 null). */
export function drawSeal(p, C, { size, turn, reach, coil, pulse, shake }) {
  groundRing(p, C, size, turn);
  for (const c of CHAINS) {
    if (reach > 0) {
      chain(p, C, chainPath(c, shake), reach, pulse);
      p.glow(c.from.x, c.from.y, 12, GLOW, 0.4);
    }
  }

  for (const c of COILS) {
    if (coil > 0) {
      chain(p, C, coilPath(c), coil, pulse);
    }
  }
}

/** 어두운 테두리를 둘러 밝은 땅에서도 사슬이 또렷하다. */
export function outline(p) {
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
