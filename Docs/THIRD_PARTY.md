# 외부 코드와 그림 고지

게임에 들어가는 외부 코드·그림과 그 라이선스. 출시판의 크레딧 화면에도 같은 내용을 넣는다.

## pixelfx

- 무엇: 코드로 그리는 도트 이펙트 모음. 효과를 `Tools/PixelFx/export_pixelfx.mjs`로 스프라이트 시트로 뽑아 쓴다(`Art/PixelFx`).
- 출처: https://github.com/patch-gg/pixelfx (커밋 `a841596`, 0.3.0). 내려받은 저장소는 `Tools/ThirdParty/pixelfx`에 두고 이 저장소에는 올리지 않는다.
- 쓰는 효과: 빛의 기둥 `holy/holy-pillar`(4장). 프롤로그의 차원문은 pixelfx 그리기 재료(`src/core`)로 우리가 만든 효과(`Tools/PixelFx/fx/dimension`)이고, 틈 속 나선 팔과 빛 테두리는 `spawn/spawn-portal`에서 가져왔다. 그 부분은 Sprite Fusion의 black-hole(Hugo Duprez, MIT)을 고쳐 쓴 것이다.
- 라이선스: MIT. 아래 고지를 함께 싣는다. 저장소의 Patch Pixel 폰트(`gallery/fonts`)는 MIT가 아니라 쓰지 않는다.
- pixelfx 안의 일부(`src/core/sim.js`, 파일 첫머리에 적힌 효과)는 Sprite Fusion의 효과(Hugo Duprez, MIT)를, `src/core/act.js`는 Hollow Crown(rehan-remade, MIT)을 고쳐 썼다.

```
MIT License

Copyright (c) 2026 Patch
Copyright (c) 2026 Hugo Duprez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
