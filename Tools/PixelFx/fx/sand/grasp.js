// 사박(沙縛): 땅이 일어나 모래 손이 솟고, 손가락을 오므려 발목을 움켜쥔 뒤 무너진다.
import { Particles, Lights, P, Rng, ease, clamp } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors, ring } from "./sand-lib.js";

const DURATION = 1.1, RISE = 0.3, CLOSE = 0.5, CRUMBLE = 0.8;

export const meta = {
  name: "Sand grasp",
  desc: "A hand of sand rises from the ground, closes and crumbles.",
  tags: ["sand", "bind", "one-shot"],
  size: [56, 60],
  origin: [28, 50],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 5 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 0, sparks: [C[6], C[5], C[4]] }), lights = new Lights();
  let t = 0, crumbled = false;
  return {
    update(dt) {
      t += dt;
      if (t < RISE && R.chance(0.7)) ps.spawn(P.DUST, R.range(-10, 10), -1, R.range(-14, 14), -R.range(6, 20), R.range(0.2, 0.4), C[4], R.range(1, 2));
      if (!crumbled && t > CRUMBLE) {
        crumbled = true;
        for (let i = 0; i < 26; i++) ps.spawn(P.DUST, R.range(-8, 8), -R.range(2, 28), R.range(-22, 22), R.range(4, 20), R.range(0.2, 0.32), R.chance(0.5) ? C[3] : C[4], R.range(1, 2.4));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const rise = ease.outBack(clamp(t / RISE), 1.6), fall = clamp((t - CRUMBLE) / (DURATION - CRUMBLE));
      ring(p, C, 13 * clamp(t / 0.15), t * 5, 1 - fall);
      const h = 26 * rise * (1 - fall), curl = clamp((t - RISE) / (CLOSE - RISE));
      for (let y = 0; y < h; y++) {
        const k = y / Math.max(1, h), w = 7 - 2.5 * k + Math.sin(y * 0.8 + t * 9) * 0.7;
        p.rect(-w, -y, w * 2, 1, y % 4 === 0 ? C[2] : C[3]);
        p.rect(-w + 1, -y, 3, 1, C[5]);
        p.rect(w - 1, -y, 1, 1, C[1]);
      }
      if (h >= 6) {
        for (const i of [-1.5, -0.5, 0.5, 1.5]) {
          const bx = i * 3.2, tx = i * (6.5 - 5 * curl), ty = -h - 12 + 8 * curl;
          p.beam(bx, -h + 1, tx, ty, 2.4, C[3]);
          p.line(bx - 0.6, -h + 1, tx - 0.6, ty, C[5]);
        }
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
