// 봉인 사슬이 솟는 토막: 땅에 봉인진이 번지고, 사슬이 솟아 몸에 닿으면 몸을 감으며 조인다. 끝 장이 되풀이 토막의 첫 장과 이어진다.
import { clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { colors, drawSeal, outline, PALETTE } from "./chains-lib.js";

const DURATION = 1;

export const meta = {
  name: "Seal chains (open)",
  desc: "A seal ring spreads, light chains shoot up and wrap the body.",
  tags: ["seal", "chains", "holy", "one-shot"],
  size: [176, 150],
  origin: [88, 110],
  duration: DURATION,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

export function create(o) {
  const C = colors(o);
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const seconds = Math.min(t, DURATION);
      drawSeal(p, C, {
        size: ease.outBack(clamp(seconds / 0.3), 1.2),
        turn: 0,
        reach: ease.outQuad(clamp((seconds - 0.15) / 0.4)),
        coil: clamp((seconds - 0.5) / 0.35),
        pulse: null,
        shake: 0,
      });
      outline(p);
    },
  };
}
