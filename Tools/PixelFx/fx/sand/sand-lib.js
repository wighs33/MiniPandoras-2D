// 모래 판도라 효과의 공통 색과 모양. 기획서의 모양 언어는 「수평으로 흐르는 입자」다.
import { fit, hex, TAU } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#2c1a0a", "#5e3f1a", "#8f6428", "#bf8f3f", "#e3bb62", "#f7dd93", "#fff4cf", "#ffffff"];
export const SHADOW = hex("#1a0f06");
export const colors = (o) => fit(o.palette ?? PALETTE, 8);

/** 바닥의 모래 고리: 안쪽이 밝고 바깥이 어둡고, 알갱이가 둘레를 돈다. */
export function ring(p, C, rx, spin, alpha = 1) {
  if (rx < 1) return;
  const ry = rx * 0.4;
  p.ellipse(0, 0, rx + 1, ry + 1, C[1], 0.8 * alpha);
  p.ellipse(0, 0, rx, ry, C[2], alpha);
  p.ellipse(0, 0, rx * 0.72, ry * 0.72, C[3], alpha);
  const n = Math.ceil(rx * 2);
  for (let i = 0; i < n; i++) {
    const th = spin + (i * TAU) / n;
    p.dot(Math.cos(th) * rx * 0.88, Math.sin(th) * ry * 0.88, i % 3 ? C[4] : C[5], alpha);
  }
}
