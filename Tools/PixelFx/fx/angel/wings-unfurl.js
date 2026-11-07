// 카일이 강림하는 컷인에서 펼쳐지는 천사의 날개. 등에 접혀 있다가 활짝 펴지며 깃털이 흩날린다. 몸은 그리지 않는다(컷인이 카일 뒤에 겹쳐, 날개가 등에서 나온다).
// 컷인의 점 하나는 가상 px 반이다(effects.json의 pixelsPerUnit 32). 그래야 깃이 인물 그림만큼 곱게 보인다.
import { clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { colors, drawSideWings, outline, PALETTE } from "./wings-lib.js";

const WIDTH = 560, HEIGHT = 480, BODY_X = 90, FEET_FROM_BOTTOM = 40;
const SHOULDER_ABOVE_FEET = 114;
const BACK_OFFSET = 16;
const SCALE = 0.8;
const OPEN_TIME = 0.7;
const FEATHERS = 22;
const FEATHER_LIFE = 0.9;

export const meta = {
  name: "Angel wings (unfurl)",
  desc: "Side-view feathered wings snap open and shed loose feathers.",
  tags: ["angel", "wings", "one-shot"],
  size: [WIDTH, HEIGHT],
  origin: [BODY_X, HEIGHT - FEET_FROM_BOTTOM],
  duration: 1.25,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

/** 날개에서 떨어져 나와 팔랑이며 흩어지는 깃털 하나. i마다 자리가 정해져 있다. */
function looseFeather(p, C, i, t) {
  const age = (t - OPEN_TIME * 0.45 - (i % 7) * 0.03) / FEATHER_LIFE;
  if (age <= 0 || age >= 1) {
    return;
  }

  const angle = -1.25 + ((i * 37) % 100) / 100 * 1.9;
  const reach = 150 + ((i * 53) % 140);
  const x = BACK_OFFSET + 40 + Math.cos(angle) * reach * (0.45 + age * 0.75);
  const y = -SHOULDER_ABOVE_FEET - 60 + Math.sin(angle) * reach * (0.45 + age * 0.55) + age * age * 90;
  const tilt = i * 1.7 + age * 6;
  const dx = Math.cos(tilt) * 6, dy = Math.sin(tilt) * 6;
  const nx = -dy * 0.35, ny = dx * 0.35;
  p.poly([[x - dx, y - dy], [x + nx, y + ny], [x + dx, y + dy], [x - nx, y - ny]], C[i % 2 ? 5 : 4], 1 - age * age);
}

export function create(o) {
  const C = colors(o);
  const origin = { x: BACK_OFFSET, y: -SHOULDER_ABOVE_FEET };
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const spread = ease.outBack(clamp(t / OPEN_TIME), 1.4);
      drawSideWings(p, C, spread, 0, origin, SCALE);
      outline(p, 2);
      for (let i = 0; i < FEATHERS; i++) {
        looseFeather(p, C, i, t);
      }
    },
  };
}
