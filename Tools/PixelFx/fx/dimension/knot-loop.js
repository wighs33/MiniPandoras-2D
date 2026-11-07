// 비틀린 공간의 매듭: 바닥의 공간이 묶여 고리 둘이 엇갈려 돌고, 사각 조각이 그 둘레에 떠 있다. 한 바퀴를 이어 되풀이한다.
import { WHITE, TAU, fit } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE } from "./rift-lib.js";

const PERIOD = 1.6;

export const meta = {
  name: "Rift knot",
  desc: "A knot in space: two rings cross and turn, cracks glow, square shards hover.",
  tags: ["rift", "knot", "loop"],
  size: [72, 64],
  origin: [36, 40],
  duration: PERIOD,
  loop: true,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

export function create(o) {
  const C = fit(o.palette ?? PALETTE, 8);
  let t = 0;
  const ring = (p, ph, tilt, rx, ry, colour, near) => {
    for (let j = 0; j < 90; j++) {
      const th = (j / 90) * TAU, front = Math.sin(th + ph) > 0;
      if (front !== near) continue;
      const x = Math.cos(th + ph) * rx, y = Math.sin(th + ph) * ry;
      p.dot(x * Math.cos(tilt) - y * Math.sin(tilt), x * Math.sin(tilt) + y * Math.cos(tilt) - 8, colour);
    }
  };
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const ph = (t / PERIOD) * TAU;
      p.ellipse(0, 0, 22, 9, C[1], 0.55);
      p.ellipse(0, 0, 13, 5, C[0], 0.8);
      for (let i = 0; i < 6; i++) {
        const a = (i * TAU) / 6 + 0.4, l = 17 + 3 * Math.sin(ph * 2 + i * 2);
        p.line(Math.cos(a) * 6, Math.sin(a) * 2.6, Math.cos(a) * l, Math.sin(a) * l * 0.42, C[4], 0.8);
        p.line(Math.cos(a) * 6, Math.sin(a) * 2.6, Math.cos(a + 0.2) * l * 0.7, Math.sin(a + 0.2) * l * 0.3, C[6], 0.45);
      }
      ring(p, ph, 0.5, 15, 6, C[5], false);
      ring(p, -ph, -0.5, 13, 5, C[4], false);
      p.glow(0, -8, 12, C[4], 0.5 + 0.12 * Math.sin(ph));
      p.rect(-1.5, -9.5, 3, 3, WHITE);
      ring(p, ph, 0.5, 15, 6, C[6], true);
      ring(p, -ph, -0.5, 13, 5, C[5], true);
      for (let i = 0; i < 9; i++) {
        const a = (i * TAU) / 9 + ph / 2, r = 20 + (i % 3) * 3, s = 1 + (i % 3);
        const x = Math.cos(a) * r, y = -12 + Math.sin(a) * r * 0.45 + Math.sin(ph + i) * 2;
        p.rect(x, y, s, s, i % 2 ? C[5] : C[3]);
      }
    },
  };
}
