// 차원문(바닥에 열린 틈) 그리기의 공통 부분. 열림·되풀이·닫힘 세 효과가 같은 모양을 같은 시간 축으로 그려
// 이어 붙여도 끊기지 않는다(되풀이 시간 0 = 다 열린 순간, 닫힘은 되풀이가 한 바퀴를 마친 자리에서 시작한다).
// 틈 속 나선 팔과 들쭉날쭉한 빛 테두리는 pixelfx spawn/spawn-portal(MIT, Patch)에서 가져왔고, 그것은 Sprite Fusion의
// black-hole(https://www.spritefusion.com/pixel-art-vfx/black-hole), MIT License, Copyright (c) 2026 Hugo Duprez를 고쳐 쓴 것이다.
// 되풀이가 이어지도록 테두리 흔들림을 잡음 대신 주기 함수로 바꿨다.
import { WHITE, TAU, hex, fit } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#12081f", "#2a0f4d", "#481b8c", "#7330d0", "#a35cff", "#cf9bff", "#f1dcff", "#ffffff"];
export const RX = 16; // 틈의 가로 반지름(px)
export const SY = 0.45; // 바닥에 누운 틈의 세로 비율
export const PERIOD = 1.2; // 되풀이 한 바퀴(초). 팔이 셋이라 1/3바퀴 돌면 같은 모습이다.
export const SIZE = [88, 60];
export const ORIGIN = [44, 42];
const VOID = hex("#06030b");

export const colors = (o) => fit(o.palette ?? PALETTE, 8);

// 되풀이 시간 t의 회전(틈 속 팔이 한 바퀴에 1/3바퀴)과 흔들림 위상
export const spinAt = (t) => (t / PERIOD) * (TAU / 3);
export const phaseAt = (t) => (t / PERIOD) * TAU;

// 각도 th에서 테두리가 바깥으로 나온 정도(1 둘레). 위상이 한 바퀴 돌면 같은 모양이다.
const lipAt = (th, phase) => 1 + 0.05 * Math.sin(3 * th + phase) + 0.035 * Math.sin(8 * th - 2 * phase + 1);

/** 틈: 어두운 구멍, 빛을 받은 먼 안쪽 벽, 구멍으로 감겨 드는 빛의 팔 셋. */
export function hole(p, C, rx, t, heat = 1, extraSpin = 0) {
  if (rx < 1) return;
  const ry = rx * SY, spin = spinAt(t) + extraSpin, phase = phaseAt(t);
  p.shade(-rx - 2, -ry - 2, rx + 2, ry + 2, (x, y, out) => {
    const u = x / rx, v = y / ry, q = Math.hypot(u, v), th = Math.atan2(v, u), lip = lipAt(th, phase);
    if (q > lip) return;
    const w = q / lip;
    out.c = v < 0 && w > 0.72 ? (w > 0.86 ? C[2] : C[1]) : w > 0.55 ? C[0] : VOID;
  });
  const n = Math.max(40, Math.round(rx * 5)), wind = 2.3;
  for (let i = 0; i < 3; i++)
    for (let j = 0; j <= n; j++) {
      const q = 0.14 + 0.82 * (j / n), th = spin + (i * TAU) / 3 - wind * Math.log(q), lip = lipAt(th, phase);
      const x = Math.cos(th) * q * lip * rx, y = Math.sin(th) * q * lip * ry;
      const c = q > 0.8 ? (heat > 1.1 ? WHITE : C[6]) : q > 0.55 ? C[5] : q > 0.32 ? C[4] : C[3];
      p.dot(x, y, c, q > 0.3 ? 1 : q / 0.3);
      if (q > 0.4 && j % 2 === 0) {
        const t2 = th - 0.32;
        p.dot(Math.cos(t2) * q * 0.94 * lip * rx, Math.sin(t2) * q * 0.94 * lip * ry, q > 0.7 ? C[4] : C[3], 0.85);
      }
    }
  p.glow(0, 0, rx * 0.45, C[3], 0.35 * heat);
}

/** 테두리: 들쭉날쭉한 흰 빛의 고리와 그 바깥의 어두운 금. near면 앞쪽 절반, 아니면 먼 쪽 절반. */
export function lip(p, C, rx, t, heat = 1) {
  if (rx < 1) return;
  const ry = rx * SY, phase = phaseAt(t), n = Math.ceil(rx * 7);
  for (let j = 0; j < n; j++) {
    const th = (j / n) * TAU, s = Math.sin(th), l = lipAt(th, phase);
    const x = Math.cos(th) * rx * l, y = s * ry * l;
    const flicker = Math.sin(th * 9 + phase * 2) > -0.3;
    p.dot(Math.cos(th) * (rx * l + 1.2), s * (ry * l + 1), C[1]);
    p.dot(x, y, s >= 0 ? (flicker || heat > 1.1 ? WHITE : C[6]) : flicker ? C[6] : C[5]);
    if (j % 6 === 0) p.glow(x, y, 7, C[5], (s >= 0 ? 0.3 : 0.2) * Math.min(1.4, heat));
  }
}

/** 바닥에 번진 보랏빛(틈 둘레의 빛 웅덩이). */
export function floorGlow(p, C, rx, k = 1) {
  p.glow(0, 0, rx * 1.7, C[3], 0.22 * k);
  p.glow(0, -2, rx * 0.9, C[4], 0.18 * k);
}

/**
 * 틈 둘레에서 바닥을 따라 빙글빙글 빨려 드는 빛 점. 점마다 한 바퀴(PERIOD)에 정수 번 돌아 되풀이가 이어진다.
 * shrink(0~1)는 닫힐 때 둘레를 줄인다.
 */
export function swirl(p, C, rx, t, rng, count = 14, shrink = 1) {
  for (let i = 0; i < count; i++) {
    const turns = 1 + (i % 2), offset = rng[i * 3], a0 = rng[i * 3 + 1] * TAU, bright = rng[i * 3 + 2];
    // 열리는 동안은 되풀이 시간이 음수라 나머지를 양수로 맞춘다.
    const f = ((((t / PERIOD) * turns + offset) % 1) + 1) % 1;
    const r = (1.55 - 1.25 * f) * rx * shrink, a = a0 + f * TAU * 0.9;
    const x = Math.cos(a) * r, y = Math.sin(a) * r * SY;
    p.dot(x, y, bright > 0.6 ? WHITE : bright > 0.3 ? C[6] : C[5], Math.min(1, f * 4));
    p.light(x, y, C[5], 0.25);
  }
}

/** 0~1 사이 수 n개(정해진 씨앗). 효과마다 같은 점을 쓰도록 모듈 밖에서 한 번 만든다. */
export function seeded(n, seed = 7) {
  const out = [];
  let s = seed;
  for (let i = 0; i < n; i++) {
    s = (s * 16807) % 2147483647;
    out.push(s / 2147483647);
  }
  return out;
}
