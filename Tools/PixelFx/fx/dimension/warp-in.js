// 공간왜곡 등장: 허공이 세로로 찢기며 일그러진 고리가 조여 들고, 사각 조각이 빨려 든다. 찢어진 틈이 넓어지는 순간 인물이 그 안에서 나온다.
import { Particles, Lights, P, Rng, WHITE, TAU, fit, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE } from "./rift-lib.js";

const DURATION = 1.1, OPEN = 0.45;

export const meta = {
  name: "Warp in",
  desc: "Air tears vertically, warped rings tighten, shards are pulled in; the tear widens and someone steps out.",
  tags: ["rift", "warp", "one-shot"],
  size: [96, 104],
  origin: [48, 84],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 8 },
};

export function create(o) {
  const C = fit(o.palette ?? PALETTE, 8), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 8, sparks: [WHITE, C[6], C[5]] }), lights = new Lights();
  let t = 0, burst = false;
  return {
    update(dt) {
      t += dt;
      if (t < OPEN && R.chance(0.8)) {
        const a = R.range(0, TAU), d = R.range(26, 40), T = R.range(0.16, 0.26);
        ps.spawn(P.STREAK, Math.cos(a) * d, -30 + Math.sin(a) * d, -Math.cos(a) * d / T, -Math.sin(a) * d / T, T, R.chance(0.5) ? C[6] : C[5]);
      }
      if (!burst && t > OPEN) {
        burst = true;
        lights.add(0, -30, 60, C[5], 1, 0.4);
        for (let i = 0; i < 30; i++) {
          const a = R.range(0, TAU), v = R.range(20, 90);
          ps.spawn(P.DUST, 0, -30, Math.cos(a) * v, Math.sin(a) * v, R.range(0.2, 0.4), R.chance(0.5) ? C[6] : C[3], R.range(1.5, 3));
        }
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const open = ease.outBack(clamp((t - OPEN * 0.6) / 0.25), 2), close = clamp((t - 0.72) / (DURATION - 0.72));
      const w = 11 * open * (1 - close), h = 44 * clamp(t / 0.2) * (1 - close * 0.6);
      for (let k = 0; k < 3; k++) {
        const ph = t * 12 + k * 2, s = (1 - clamp(t / OPEN)) * (32 + k * 8);
        if (s > 2) {
          for (let j = 0; j < 80; j++) {
            const th = (j / 80) * TAU, wob = 1 + 0.16 * Math.sin(th * 5 + ph);
            p.dot(Math.cos(th) * s * wob * 0.8, -30 + Math.sin(th) * s * wob * 1.1, k ? C[4] : C[6], 0.8);
          }
        }
      }
      if (h > 1) {
        for (let y = -h; y <= h; y++) {
          const q = 1 - Math.abs(y) / h, half = w * Math.sqrt(q) + 0.8 * q + Math.sin(y * 0.6 + t * 30) * 0.6 * q;
          p.rect(-half - 1, -30 + y, half * 2 + 2, 1, C[5]);
          if (half > 1) p.rect(-half, -30 + y, half * 2, 1, C[0]);
        }
        p.glowLine(0, -30 - h, 0, -30 + h, 10, C[4], 0.5 * (1 - close));
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
