// 봉인 사슬을 조인 채 되풀이하는 토막: 봉인진의 글자가 한 칸씩 돌고 사슬을 따라 빛이 흐르며 팽팽하게 떤다.
import { colors, drawSeal, outline, PALETTE } from "./chains-lib.js";

const DURATION = 1;

export const meta = {
  name: "Seal chains (loop)",
  desc: "The seal holds: runes turn, light runs along taut chains.",
  tags: ["seal", "chains", "holy", "loop"],
  size: [176, 150],
  origin: [88, 110],
  duration: DURATION,
  loop: true,
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
      const phase = (t % DURATION) / DURATION;
      drawSeal(p, C, {
        size: 1,
        turn: phase,
        reach: 1,
        coil: 1,
        pulse: phase,
        shake: Math.sin(phase * Math.PI * 4) * 1.2,
      });
      outline(p);
    },
  };
}
