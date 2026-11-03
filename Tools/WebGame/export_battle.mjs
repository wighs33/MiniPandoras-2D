// 웹게임의 전투 표(battle/data.ts, combat/actions.ts)를 JSON으로 뽑는다.
// 두 파일은 TypeScript 모듈이라 Node(23.6 이상)가 타입만 지우고 그대로 불러온다. 수치를 손으로 옮기지 않기 위해서다.
//
// 사용: node Tools/WebGame/export_battle.mjs <웹게임 games/pandoras 폴더> <프로젝트 루트>
//   Assets/MiniPandoras/Art/WebGame/battle.json            들여오기 도구가 읽는 표
//   Assets/MiniPandoras/Tests/EditMode/Fixtures/battle_formulas.json   공식 테스트가 대조하는 웹게임 계산 결과
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const [gameDir, projectDir] = process.argv.slice(2).map((p) => resolve(p));
if (!gameDir || !projectDir) {
  console.error("사용: node export_battle.mjs <games/pandoras> <프로젝트 루트>");
  process.exit(1);
}

const load = (file) => import(pathToFileURL(join(gameDir, file)).href);
const data = await load("battle/data.ts");
const actions = await load("combat/actions.ts");

const MEMBERS = ["hero", "rang", "diel", "dielYoung", "largo", "buddy", "isabel", "aurora", "erus", "hass"];

const skills = Object.entries(data.SKILLS).map(([id, s]) => {
  const a = actions.ACTIONS[id];
  return {
    id,
    name: s.name,
    desc: s.desc,
    cost: s.cost,
    target: s.target,
    power: s.power,
    fx: s.fx,
    action: a
      ? {
          cooldown: a.cooldown,
          palette: a.palette,
          sfx: a.sfx,
          shape: a.shape,
          status: a.status ?? null,
        }
      : null,
  };
});

const enemies = Object.entries(data.ENEMIES).map(([id, e]) => ({
  id,
  name: e.name,
  sprite: e.sprite,
  hp: e.hp,
  atk: e.atk,
  def: e.def,
  spd: e.spd,
  skills: e.skills,
  skillRate: e.skillRate,
  evade: e.evade ?? 0,
  wings: Boolean(e.wings),
}));

const pandoras = data.HERO_PANDORAS.map((id) => ({ id, ...data.PANDORA_BOOK[id] }));

const pandoraSkills = Object.entries(actions.PANDORA_SKILLS).map(([pandora, pair]) => ({
  pandora,
  first: pair[0],
  second: pair[1],
}));

const members = MEMBERS.map((id) => ({
  id,
  // 주인공 이름은 플레이어가 정한다.
  name: id === "hero" ? "" : data.MEMBER_NAMES[id],
  sprite: data.MEMBER_SPRITES[id],
  summonSkill: actions.SUMMON_SKILL[id],
  summonSprite: actions.SUMMON_SPRITE[id] ?? data.MEMBER_SPRITES[id],
}));

writeJson(join(projectDir, "Assets/MiniPandoras/Art/WebGame/battle.json"), {
  skills,
  enemies,
  pandoras,
  pandoraSkills,
  members,
  summonCooldown: actions.SUMMON_COOLDOWN,
  autoEquip: data.AUTO_EQUIP,
  huntPandoras: data.HUNT_PANDORAS,
  beastDust: data.BEAST_DUST,
});

// 공식 대조용: 동료마다 장(레벨) 1~8, 깨어남 전후의 능력치와, 정해 둔 난수로 낸 피해량.
const memberStats = [];
for (const id of MEMBERS)
  for (let level = 1; level <= 8; level++)
    for (const awakened of [false, true])
      memberStats.push({ id, level, awakened, ...data.memberStats(id, level, awakened) });

const damage = [];
for (const [atk, def, power] of [
  [10, 16, 1],
  [11, 6, 1.25],
  [12, 7, 1.45],
  [16, 8, 2.3],
  [25, 80, 1],
  [46, 10, 2.2],
  [30, 17, 0],
])
  for (const roll of [0, 0.25, 0.5, 0.75, 0.999])
    damage.push({ atk, def, power, roll, result: data.computeDamage(atk, def, power, () => roll) });

writeJson(join(projectDir, "Assets/MiniPandoras/Tests/EditMode/Fixtures/battle_formulas.json"), {
  memberStats,
  damage,
});

function writeJson(path, value) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, JSON.stringify(value, null, 2) + "\n", "utf8");
  console.log("wrote", path);
}
