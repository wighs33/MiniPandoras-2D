// 회천(回天): 칼을 휘돌리며 땅에서 솟구친다. 붉은 원 궤적이 나선으로 올라간다.
import { Particles, P, Rng, TAU, clamp } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./berserk-lib.js";

const DURATION = 0.75, TURNS = 2.4;

export const meta = {
  name: "Spin rise",
  desc: "Blades whirl upward in a rising red spiral.",
  tags: ["berserk", "spin", "one-shot"],
  size: [96, 112],
  origin: [48, 92],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 7 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 0, sparks: [C[7], C[6], C[5]] });
  let t = 0;
  return {
    update(dt) {
      t += dt;
      if (t < 0.5 && R.chance(0.8)) ps.spawn(P.DUST, R.range(-22, 22), -R.range(0, 60), R.range(-30, 30), -R.range(20, 70), R.range(0.14, 0.3), R.chance(0.5) ? C[6] : C[4], R.range(1, 2));
      ps.update(dt);
    },
    draw(p) {
      const k = clamp(t / 0.5), fade = 1 - clamp((t - 0.5) / 0.25), n = 120;
      for (let i = 0; i <= n * k; i++) {
        const u = i / n, a = u * TURNS * TAU, y = -6 - u * 76, r = 24 - 8 * u;
        const near = Math.sin(a) > 0, age = clamp((k - u) / 0.45);
        const c = age < 0.3 ? C[7] : age < 0.6 ? C[5] : C[3];
        p.dot(Math.cos(a) * r, y + Math.sin(a) * 6, c, (near ? 1 : 0.55) * (1 - age * 0.6) * fade);
        p.dot(Math.cos(a) * r, y + Math.sin(a) * 6 + 1, C[3], 0.7 * (1 - age) * fade);
      }
      p.ellipse(0, 0, 24 * k, 8 * k, C[2], 0.5 * fade);
      ps.draw(p);
    },
  };
}
