// 쌍검난무(雙劍亂舞): 쌍검이 앞을 그물처럼 연달아 긋는다. 자국이 차례로 그어졌다가 사라진다.
import { Particles, Lights, P, Rng, clamp } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors, slash } from "./berserk-lib.js";

const DURATION = 0.7, CUTS = [[-30, -34, 30, 6, 8], [30, -36, -30, 4, -8], [-34, -14, 34, -18, 7], [-22, 2, 26, -40, -9], [22, 4, -26, -42, 9]];

export const meta = {
  name: "Twin slash",
  desc: "Twin blades cut a net of crossing red slashes in front.",
  tags: ["berserk", "melee", "one-shot"],
  size: [104, 88],
  origin: [52, 60],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 2 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 20, sparks: [C[7], C[6], C[5]] }), lights = new Lights();
  let t = 0, cut = 0;
  return {
    update(dt) {
      t += dt;
      if (cut < CUTS.length && t > cut * 0.08) {
        cut += 1;
        lights.add(0, -16, 30, C[4], 0.7, 0.14);
        for (let i = 0; i < 6; i++) ps.spawn(P.DUST, R.range(-22, 22), -R.range(4, 34), R.sign() * R.range(20, 70), -R.range(0, 50), R.range(0.14, 0.26), R.chance(0.5) ? C[6] : C[4], R.range(1, 2));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      CUTS.forEach(([x0, y0, x1, y1, bow], i) => {
        const start = i * 0.08, k = clamp((t - start) / 0.07), fade = 1 - clamp((t - start - 0.12) / 0.22);
        if (k > 0 && fade > 0) slash(p, C, x0, y0, x1, y1, bow, k, fade);
      });
      ps.draw(p);
      lights.draw(p);
    },
  };
}
