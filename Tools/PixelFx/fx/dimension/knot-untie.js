// 매듭 풀림: 고리가 조여 한 점이 되었다가 튕겨 나가며 끊기고, 사각 조각이 사방으로 흩어진다.
import { Particles, Lights, P, Rng, WHITE, TAU, fit, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE } from "./rift-lib.js";

const DURATION = 0.9, SNAP = 0.28;

export const meta = {
  name: "Rift knot untied",
  desc: "The rings tighten to a point, snap outward; square shards scatter.",
  tags: ["rift", "knot", "one-shot"],
  size: [112, 88],
  origin: [56, 52],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 6 },
};

export function create(o) {
  const C = fit(o.palette ?? PALETTE, 8), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 40, sparks: [WHITE, C[6], C[5]] }), lights = new Lights();
  let t = 0, snapped = false;
  return {
    update(dt) {
      t += dt;
      if (!snapped && t > SNAP) {
        snapped = true;
        lights.add(0, -8, 56, C[5], 1, 0.4);
        for (let i = 0; i < 46; i++) {
          const a = R.range(0, TAU), v = R.range(30, 130);
          ps.spawn(P.DUST, 0, -8, Math.cos(a) * v, Math.sin(a) * v * 0.6, R.range(0.25, 0.5), R.chance(0.5) ? C[6] : C[4], R.range(1.5, 3.2));
        }
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      if (t < SNAP) {
        const k = 1 - ease.inCubic(t / SNAP), rx = 16 * k + 2;
        p.ring(0, -8, rx, 1.4, C[6]);
        p.ring(0, -8, rx * 0.7, 1, C[5]);
        p.glow(0, -8, 12 + 8 * (1 - k), C[5], 0.7);
      } else {
        const k = clamp((t - SNAP) / (DURATION - SNAP)), r = 10 + 44 * ease.outCubic(k);
        p.ring(0, -8, r, Math.max(0.6, 3 * (1 - k)), C[6], 1 - k);
        if (k < 0.2) p.rect(-2, -10, 4, 4, WHITE);
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
