// 컷인에서 펼쳐지는 악마의 날개. 등에 접혀 있다가 넘칠 듯 활짝 펴지고 자리 잡는다. 몸은 그리지 않는다(컷인이 디엘 뒷모습 위에 겹친다).
import { clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { colors, drawWings, PALETTE } from "./wings-lib.js";

const SHOULDER_ABOVE_FEET = 70;
const SCALE = 0.85;
const OPEN_TIME = 0.72;

export const meta = {
  name: "Demon wings (unfurl)",
  desc: "Two bat wings folded on the back snap open past full spread and settle.",
  tags: ["demon", "wings", "shadow", "one-shot"],
  size: [372, 176],
  origin: [186, 168],
  duration: 1,
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
      const spread = ease.outBack(clamp(t / OPEN_TIME), 1.6);
      drawWings(p, C, spread, { x: 0, y: -SHOULDER_ABOVE_FEET }, SCALE);
    },
  };
}
