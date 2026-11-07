// 신성 강림(神聖降臨): 빛이 몸으로 모였다가 십자로 터지며 금빛 갑주가 입혀진다.
import { Particles, Lights, P, Rng, TAU, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./holy-lib.js";

const DURATION = 1.2, GATHER = 0.45;

export const meta = {
  name: "Knight ascend",
  desc: "Light gathers into the body, then bursts in a cross as golden armour forms.",
  tags: ["holy", "buff", "one-shot"],
  size: [144, 136],
  origin: [72, 104],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 6 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 28, sparks: [C[6], C[5], C[4]] }), lights = new Lights();
  let t = 0, burst = false;
  return {
    update(dt) {
      t += dt;
      if (t < GATHER && R.chance(0.85)) {
        const a = R.range(0, TAU), d = R.range(36, 60), T = R.range(0.2, 0.3);
        ps.spawn(P.STREAK, Math.cos(a) * d, -36 + Math.sin(a) * d, (-Math.cos(a) * d) / T, (-Math.sin(a) * d) / T, T, R.chance(0.5) ? C[5] : C[4]);
      }
      if (!burst && t > GATHER) {
        burst = true;
        lights.add(0, -36, 80, C[4], 1.1, 0.45);
        for (let i = 0; i < 44; i++) {
          const a = R.range(0, TAU), v = R.range(30, 120);
          ps.spawn(P.DUST, 0, -36, Math.cos(a) * v, Math.sin(a) * v, R.range(0.25, 0.5), R.chance(0.5) ? C[5] : C[3], R.range(1, 2.6));
        }
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      if (t < GATHER) p.glow(0, -36, 12 + 18 * (t / GATHER), C[4], 0.6);
      if (t >= GATHER) {
        const k = ease.outCubic(clamp((t - GATHER) / 0.3)), fade = 1 - clamp((t - GATHER - 0.25) / (DURATION - GATHER - 0.25));
        p.beam(0, -36 - 66 * k, 0, -36 + 34 * k, 5 * fade + 0.6, C[5], fade);
        p.beam(-52 * k, -36, 52 * k, -36, 4 * fade + 0.6, C[5], fade);
        p.line(0, -36 - 66 * k, 0, -36 + 34 * k, C[6], fade);
        p.line(-52 * k, -36, 52 * k, -36, C[6], fade);
        p.ring(0, -36, 10 + 44 * k, Math.max(0.6, 3 * fade), C[5], fade);
        p.ellipse(0, 0, 40 * k, 14 * k, C[3], 0.3 * fade);
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
