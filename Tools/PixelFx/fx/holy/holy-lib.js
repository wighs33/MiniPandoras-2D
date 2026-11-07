// 네오 라둔의 홀리나이트 판도라 효과의 공통 색. 기획서의 모양 언어는 「곧게 내리꽂는 빛, 방패 면」이다.
import { fit } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#3a2a10", "#7a5a18", "#c2902c", "#e6c058", "#ffe9a0", "#fff6d0", "#ffffff", "#ffffff"];
export const colors = (o) => fit(o.palette ?? PALETTE, 8);
