// 성역(聖域): 발밑에 네모 진이 그어지고 네 모서리와 둘레에서 빛줄기가 곧게 솟는다.
import { TAU, clamp } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./holy-lib.js";

const DURATION = 2.4, OPEN = 0.35, CLOSE = 2.0, RX = 56, RY = 26;

export const meta = {
  name: "Sanctuary",
  desc: "A square seal draws itself on the floor; straight beams of light rise around it.",
  tags: ["holy", "zone", "one-shot"],
  size: [136, 112],
  origin: [68, 72],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 4 },
};

const corner = (i, k) => {
  const a = (i / 4) * TAU + TAU / 8;
  return [Math.cos(a) * RX * Math.SQRT2 * k, Math.sin(a) * RY * Math.SQRT2 * k];
};

export function create(o) {
  const C = colors(o);
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const open = clamp(t / OPEN), fade = 1 - clamp((t - CLOSE) / (DURATION - CLOSE));
      if (open <= 0 || fade <= 0) return;
      const outer = [0, 1, 2, 3].map((i) => corner(i, open));
      p.poly(outer, C[3], 0.16 * fade);
      p.polyline([...outer, outer[0]], C[5], fade);
      const inner = [0, 1, 2, 3].map((i) => corner(i, open * 0.7));
      p.polyline([...inner, inner[0]], C[4], 0.8 * fade);
      p.ellipse(0, 0, 18 * open, 8 * open, C[3], 0.28 * fade);
      for (let i = 0; i < 12; i++) {
        const a = (i / 12) * TAU + t * 0.8;
        p.dot(Math.cos(a) * RX * 0.86 * open, Math.sin(a) * RY * 0.86 * open, C[6], fade);
      }
      for (let i = 0; i < 4; i++) {
        const [x, y] = corner(i, open), h = (34 + 8 * Math.sin(t * 7 + i)) * open;
        p.beam(x, y, x, y - h, 2.6, C[4], 0.7 * fade);
        p.line(x, y, x, y - h - 6, C[6], 0.9 * fade);
        p.glowLine(x, y, x, y - h, 8, C[4], 0.3 * fade);
      }
    },
  };
}
