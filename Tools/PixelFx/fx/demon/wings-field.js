// 필드에서 몸 뒤에 붙는 악마의 날개. 첫 장은 접힌 모습, 나머지 여덟 장은 천천히 날갯짓하는 반복이다.
// 컷인(wings-lib)은 너머의 병사를 가리지 않게 낮게 벌리지만, 필드에서는 머리 위로 솟아야 작은 인물 뒤에서도 위압감이 난다(사용자 요청 2026-11-05).
// 막은 손가락 사이 칸마다 격자(u: 손목→끝, t: 위 손가락→아래 손가락)로 칠한다. 그래야 손가락이 휘어도 명암이 막을 따라 휜다.
import { fit, hex } from "../../../ThirdParty/pixelfx/src/core/index.js";

// 밤 맵의 어둠이 덧씌워져도 보라빛이 남도록 가운데 색을 맑게 둔다.
const PALETTE = ["#0a0510", "#190a28", "#311248", "#4f1b72", "#742392", "#a82c88", "#ee3a6c", "#ffb09a"];
const OUTLINE = hex("#050308");

const WIDTH = 480, HEIGHT = 320;
const SHOULDER_FROM_TOP = 176; // CharacterAnimator.DemonWingRoot(논리 px)의 네 배와 같아야 한다.
const SHOULDER_GAP = 12;
const FPS = 8, FLAP_FRAMES = 8;

// 각도는 바깥쪽 가로가 0도, 위가 -90도, 아래가 +90도다. 손가락은 위에서 아래 순서다.
const UPPER_ARM = 74, FOREARM = 70;
const FINGERS = [132, 156, 166, 150];
const OPEN = { upperArm: -40, forearm: -68, fingers: [-6, 30, 58, 86] };
const FOLDED = { upperArm: -68, forearm: 98, fingers: [76, 84, 90, 96] };
const FINGER_BOW = 0.13; // 손가락이 바깥으로 휘는 정도
const TIP_CLAW = 12; // 막 끝을 지나 더 뻗는 뼈 끝
const BACK_DROP = 26; // 막이 등에 붙는 자리(어깨에서 아래로)

// 손가락 끝 사이의 막이 손목 쪽으로 파이는 정도. 몸 쪽 칸(마지막)이 가장 깊다.
const SCALLOPS = [0.42, 0.48, 0.5, 0.62];
// 찢어진 자리: [t, 너비, 깊이]. 칸마다 따로 둔다.
const TEARS = [
  [[0.38, 0.05, 0.16]],
  [[0.26, 0.04, 0.12], [0.68, 0.05, 0.2]],
  [[0.5, 0.06, 0.18]],
  [[0.3, 0.04, 0.14]],
];
const PANEL_LIGHT = [0.1, 0.05, 0, -0.03];
const GRID_U = [0, 0.14, 0.28, 0.42, 0.56, 0.68, 0.78, 0.86, 0.91, 0.945, 1];
const GRID_T = 64;
const RIM_FROM = 0.945; // 이 뒤의 맨 끝 줄은 불붙은 듯 밝다.

const EMBERS = 16;
const EMBER_RISE = 64;

const BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5].map((v) => (v + 0.5) / 16);

const lerp = (a, b, t) => a + (b - a) * t;
const toRad = (d) => (d * Math.PI) / 180;
const mixPoint = (a, b, t) => ({ x: lerp(a.x, b.x, t), y: lerp(a.y, b.y, t) });
const flat = (points) => points.map((q) => [q.x, q.y]);

const along = (from, degrees, length, side) => ({
  x: from.x + side * Math.cos(toRad(degrees)) * length,
  y: from.y + Math.sin(toRad(degrees)) * length,
});

const bezier = (a, control, b) => (t) => {
  const u = 1 - t;
  return { x: u * u * a.x + 2 * u * t * control.x + t * t * b.x, y: u * u * a.y + 2 * u * t * control.y + t * t * b.y };
};

/** 날갯짓 한 장의 관절 각도. 끝으로 갈수록 늦게 따라와 막이 공기를 타는 듯 보인다. */
function flapPose(beat) {
  const phase = (beat / FLAP_FRAMES) * Math.PI * 2;
  return {
    upperArm: OPEN.upperArm + 5 * Math.sin(phase),
    forearm: OPEN.forearm + 8 * Math.sin(phase - 0.7),
    fingers: OPEN.fingers.map((a, i) => a + (11 - i * 2) * Math.sin(phase - 1.4)),
  };
}

function fingerPath(wrist, degrees, length, side) {
  const tip = along(wrist, degrees, length, side);
  const bowed = along(mixPoint(wrist, tip, 0.55), degrees - 90, length * FINGER_BOW, side);
  return bezier(wrist, bowed, tip);
}

function skeleton(origin, side, pose) {
  const shoulder = { x: origin.x + side * SHOULDER_GAP, y: origin.y };
  const elbow = along(shoulder, pose.upperArm, UPPER_ARM, side);
  const wrist = along(elbow, pose.forearm, FOREARM, side);
  const fingers = pose.fingers.map((degrees, i) => fingerPath(wrist, degrees, FINGERS[i], side));
  const back = { x: shoulder.x, y: shoulder.y + BACK_DROP };
  return { shoulder, elbow, wrist, fingers, back };
}

/** 등을 따라가는 몸 쪽 경계: 손목 → 팔꿈치 → 어깨 → 등. */
function armPath(s) {
  return (t) => (t < 0.4 ? mixPoint(s.wrist, s.elbow, t / 0.4) : t < 0.75 ? mixPoint(s.elbow, s.shoulder, (t - 0.4) / 0.35) : mixPoint(s.shoulder, s.back, (t - 0.75) / 0.25));
}

const tearDepth = (tears, t) => Math.max(0, ...tears.map(([at, width, depth]) => depth * Math.max(0, 1 - Math.abs(t - at) / width)));

/** 막 한 칸의 격자 점: 두 손가락 사이를 잇되, 끝은 파인 곡선을 따른다. */
function panelPoint(upper, lower, edge, tears, u, t) {
  const reach = u * (1 - tearDepth(tears, t));
  const between = mixPoint(upper(reach), lower(reach), t);
  const straightEdge = mixPoint(upper(1), lower(1), t);
  const curved = edge(t);
  const bend = reach * reach;
  return { x: between.x + (curved.x - straightEdge.x) * bend, y: between.y + (curved.y - straightEdge.y) * bend };
}

/** 밝기(0~1). 끝으로 갈수록 밝고, 위 손가락 바로 밑은 그늘이며, 팽팽한 막의 주름이 희미하게 진다. */
function membraneTone(panel, u, t) {
  if (u >= RIM_FROM) {
    return 0.89;
  }

  const pleat = Math.cos(t * Math.PI * 4);
  const shadow = Math.pow(1 - t, 3);
  return 0.36 + 0.36 * u * u * u + 0.05 * pleat - 0.14 * shadow + PANEL_LIGHT[panel];
}

function toned(C, tone) {
  return (x, y) => {
    const dither = BAYER[((Math.floor(y) & 3) << 2) | (Math.floor(x) & 3)];
    const index = Math.floor(tone * 7 + dither);
    return C[Math.max(0, Math.min(7, index))];
  };
}

function membrane(p, C, panel, upper, lower, wrist) {
  const pull = SCALLOPS[panel];
  const a = upper(1), b = lower(1);
  const edge = bezier(a, mixPoint(mixPoint(a, b, 0.5), wrist, pull), b);
  const tears = TEARS[panel];
  for (let row = 0; row < GRID_U.length - 1; row++) {
    const u0 = GRID_U[row], u1 = GRID_U[row + 1];
    for (let i = 0; i < GRID_T; i++) {
      const t0 = i / GRID_T, t1 = (i + 1) / GRID_T;
      const corners = [
        panelPoint(upper, lower, edge, tears, u0, t0),
        panelPoint(upper, lower, edge, tears, u1, t0),
        panelPoint(upper, lower, edge, tears, u1, t1),
        panelPoint(upper, lower, edge, tears, u0, t1),
      ];
      p.poly(flat(corners), toned(C, membraneTone(panel, u0, (t0 + t1) / 2)));
    }
  }
}

/** 끝으로 갈수록 가늘어지는 뼈. 윗면에만 빛을 얹어 입체로 보인다. */
function taperedBone(p, path, from, to, fromWidth, toWidth, color, shine) {
  const steps = 14;
  let last = path(from);
  for (let i = 1; i <= steps; i++) {
    const next = path(lerp(from, to, i / steps));
    p.beam(last.x, last.y, next.x, next.y, lerp(fromWidth, toWidth, i / steps), color);
    last = next;
  }
  last = path(from);
  for (let i = 1; i <= steps; i++) {
    const next = path(lerp(from, to, i / steps));
    const lift = lerp(fromWidth, toWidth, i / steps) * 0.3;
    p.line(last.x, last.y - lift, next.x, next.y - lift, shine);
    last = next;
  }
}

function wing(p, C, side, origin, pose) {
  const s = skeleton(origin, side, pose);
  const lastFinger = s.fingers.length - 1;

  // 몸 쪽 칸부터 칠한다. 바깥 칸이 위에 겹친다.
  membrane(p, C, lastFinger, s.fingers[lastFinger], armPath(s), s.wrist);
  for (let i = lastFinger - 1; i >= 0; i--) {
    membrane(p, C, i, s.fingers[i], s.fingers[i + 1], s.wrist);
  }

  // 손가락: 첫째만 앞날이라 밝고, 나머지는 어두운 능선이다. 끝은 막을 지나 발톱처럼 더 뻗는다.
  s.fingers.forEach((finger, i) => {
    const beyond = along(finger(1), Math.atan2(finger(1).y - finger(0.96).y, side * (finger(1).x - finger(0.96).x)) * 180 / Math.PI, TIP_CLAW, side);
    const reach = (t) => (t <= 1 ? finger(t) : mixPoint(finger(1), beyond, t - 1));
    taperedBone(p, reach, 0, 2, i === 0 ? 6 : 4, 1, C[1], i === 0 ? C[6] : C[3]);
  });

  // 팔: 손가락보다 굵고 앞날에 빛이 얹힌다.
  taperedBone(p, (t) => mixPoint(s.shoulder, s.elbow, t), 0, 1, 11, 8, C[1], C[4]);
  taperedBone(p, (t) => mixPoint(s.elbow, s.wrist, t), 0, 1, 8, 6, C[1], C[6]);

  // 손목의 큰 갈고리와 팔꿈치의 가시.
  const hook = bezier(s.wrist, along(s.wrist, -96, 20, side), along(s.wrist, -60, 34, side));
  taperedBone(p, hook, 0, 1, 6, 1, C[2], C[7]);
  const spur = bezier(s.elbow, along(s.elbow, 118, 9, side), along(s.elbow, 150, 20, side));
  taperedBone(p, spur, 0, 1, 5, 1, C[1], C[5]);
}

/** 날개를 따라 피어오르는 불씨. 위치는 장마다 정해져 있어 반복이 끊기지 않는다. */
function embers(p, C, origin, beat) {
  for (let i = 0; i < EMBERS; i++) {
    const age = (beat / FLAP_FRAMES + i * 0.381) % 1;
    const side = i % 2 ? 1 : -1;
    const x = origin.x + side * (70 + ((i * 53) % 130)) + Math.sin(age * 6 + i) * 6;
    const y = origin.y + 40 - ((i * 37) % 90) - age * EMBER_RISE;
    const size = age < 0.55 ? 3 : 2;
    p.rect(Math.round(x), Math.round(y), size, size, age < 0.35 ? C[7] : age < 0.7 ? C[6] : C[4]);
  }
}

/** 인물 그림처럼 어두운 테두리를 두른다. 작은 화면에서 날개가 배경과 갈라져 보인다. */
function outline(p, color, thickness) {
  for (let pass = 0; pass < thickness; pass++) {
    const solid = p.C.slice();
    for (let y = 1; y < p.h - 1; y++) {
      for (let x = 1; x < p.w - 1; x++) {
        const i = y * p.w + x;
        if (!solid[i] && (solid[i - 1] || solid[i + 1] || solid[i - p.w] || solid[i + p.w])) {
          p.C[i] = color;
        }
      }
    }
  }
}

export const meta = {
  name: "Demon wings (field)",
  desc: "Raised demon wings for the field sprite: one folded frame, then a slow flap loop.",
  tags: ["demon", "wings", "sprite"],
  size: [WIDTH, HEIGHT],
  origin: [WIDTH / 2, HEIGHT],
  duration: FLAP_FRAMES / FPS,
  loop: false,
  fps: FPS,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

export function create(o) {
  const C = fit(o.palette ?? PALETTE, 8);
  const origin = { x: 0, y: SHOULDER_FROM_TOP - HEIGHT };
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const frame = Math.round(t * FPS);
      const pose = frame === 0 ? FOLDED : flapPose(frame - 1);
      wing(p, C, -1, origin, pose);
      wing(p, C, 1, origin, pose);
      outline(p, OUTLINE, 2);
      if (frame > 0) {
        embers(p, C, origin, frame - 1);
      }
    },
  };
}
