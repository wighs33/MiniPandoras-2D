// 성벽(聖壁): 몸 앞에 빛의 방패가 곧게 서서 일렁이다가 사라진다.
import { TAU, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./holy-lib.js";

const DURATION = 1.5, RISE = 0.18, FADE = 1.25;

export const meta = {
  name: "Holy shield",
  desc: "A tall kite shield of light stands in front of the body, shimmers and fades.",
  tags: ["holy", "shield", "one-shot"],
  size: [80, 96],
  origin: [40, 84],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 2 },
};

export function create(o) {
  const C = colors(o);
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const up = ease.outBack(clamp(t / RISE), 1.6), fade = 1 - clamp((t - FADE) / (DURATION - FADE));
      if (up <= 0 || fade <= 0) return;
      const w = 24 * up, top = -74 * up, shoulder = -58 * up, foot = -8 * up;
      const outline = [[-w, top], [w, top], [w, shoulder + 26 * up], [0, foot], [-w, shoulder + 26 * up]];
      p.poly(outline, C[3], 0.34 * fade);
      p.polyline([...outline, outline[0]], C[5], fade);
      const inner = outline.map(([x, y]) => [x * 0.78, top + (y - top) * 0.82 + 4]);
      p.polyline([...inner, inner[0]], C[3], 0.8 * fade);
      p.line(0, top + 6, 0, foot - 6, C[5], 0.9 * fade);
      p.line(-w * 0.7, top + 20 * up, w * 0.7, top + 20 * up, C[5], 0.9 * fade);
      const sweep = -74 + ((t * 2.4) % 1) * 66;
      p.line(-w * 0.9, sweep, w * 0.9, sweep, C[6], 0.55 * fade);
      p.glow(0, -42, 34, C[4], 0.32 * fade);
      for (let i = 0; i < 10; i++) {
        const a = (i / 10) * TAU + t * 3, y = -40 + Math.sin(a) * 34;
        p.dot(Math.cos(a) * 28, y, C[6], fade);
      }
    },
  };
}
