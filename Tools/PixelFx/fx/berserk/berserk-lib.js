// 하스 로첸의 광폭화 효과의 공통 색과 베기 자국. 쌍검의 붉은 궤적이다.
import { fit } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#1a0508", "#4a0a10", "#8a1418", "#c8281e", "#f0562a", "#ffa04a", "#ffe0a0", "#ffffff"];
export const colors = (o) => fit(o.palette ?? PALETTE, 8);

/** 초승달처럼 가운데가 굵고 끝이 뾰족한 베기 자국. (x0,y0)에서 (x1,y1)까지 bow만큼 휘고, k(0~1)까지 그린다. */
export function slash(p, C, x0, y0, x1, y1, bow, k, fade = 1) {
  const n = 28, dx = x1 - x0, dy = y1 - y0, len = Math.hypot(dx, dy) || 1, nx = -dy / len, ny = dx / len;
  for (let i = 0; i <= n * k; i++) {
    const u = i / n, b = Math.sin(u * Math.PI);
    const x = x0 + dx * u + nx * bow * b, y = y0 + dy * u + ny * bow * b, w = 3.4 * b;
    p.line(x - nx * w, y - ny * w, x + nx * w, y + ny * w, C[3], fade);
    p.line(x - nx * w * 0.5, y - ny * w * 0.5, x + nx * w * 0.5, y + ny * w * 0.5, C[5], fade);
    p.dot(x, y, C[7], fade);
  }
}
