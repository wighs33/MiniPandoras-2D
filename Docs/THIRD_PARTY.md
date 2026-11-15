# 외부 코드와 그림 고지

게임에 들어가는 외부 코드·그림과 그 라이선스. 출시판의 크레딧 화면에도 같은 내용을 넣는다.

## 글꼴 (모두 SIL Open Font License 1.1)

| 글꼴 | 쓰는 곳 | 출처 | 파일 |
|---|---|---|---|
| Pretendard | 버튼과 본문 등 제목 밖의 모든 글 | https://github.com/orioncactus/pretendard | `Assets/ThirdParty/Pretendard` |
| 갈무리(Galmuri) 2.40.4 | 사망 화면의 「사망했습니다」 한 줄(2026-11-09에 나머지 글에서는 뺐다). 11을 18 크기(1.5배)로 쓴다 | https://github.com/quiple/galmuri | `Assets/ThirdParty/Galmuri` |
| Noto Serif KR Bold | 타이틀 로고와 제목(웹게임 타이틀과 같은 명조) | https://github.com/notofonts/noto-cjk (`Serif/SubsetOTF/KR`) | `Assets/ThirdParty/NotoSerifKR` |
| Cinzel(가변, wght 900으로 씀) | 타이틀 로고의 「MINI」(그림으로 구워 넣고 글꼴 파일은 빌드에 넣지 않는다) | https://github.com/NDISCOVER/Cinzel (google/fonts `ofl/cinzel`, 커밋 `c30b221beb0c`) | `Assets/ThirdParty/Cinzel` |

- OFL은 글꼴을 게임에 넣어 팔아도 되고, 글꼴 파일만 따로 팔면 안 된다. 원문은 각 폴더의 `LICENSE.txt`이고, 출시판 크레딧에 글꼴 이름과 라이선스를 적는다.
- TMP 글꼴 에셋은 `Assets/MiniPandoras/Art/Fonts`에 있다. 갈무리와 명조는 지금 동적 아틀라스라 원본 글꼴 파일이 빌드에 들어간다(갈무리11 약 14MB 중 일부, 명조 약 8MB). 갈무리는 한 줄만 쓰니 웹 빌드 전에 그 글자만 담은 정적 아틀라스로 바꾸면 원본 파일을 빼도 된다. 웹 빌드 전에 쓰는 글자만 담은 정적 아틀라스로 바꾼다.

## 로고 도구(`Tools/Logo/title_logo.mjs`)

- 게임에는 구운 로고 그림(`Art/UI/Title/logo.png`)만 들어가고 도구와 그 의존 코드는 싣지 않는다.
- 의존: fontkit(MIT, 글꼴 윤곽 읽기), polygon-clipping(MIT, 획 합치고 자르기), playwright-core(Apache-2.0, Chromium으로 SVG를 찍기). 이 저장소에 두지 않고, 그 셋이 든 node_modules 폴더를 도구에 인자로 준다.

## PrimeTween

- 무엇: 메모리를 할당하지 않는 트윈(움직임) 라이브러리. 창을 열고 닫는 움직임(`UI.WindowMotion`)에 쓴다(2026-11-08).
- 출처: https://github.com/KyryloKuzyk/PrimeTween, 판 1.5.2. Copyright Kyrylo Kuzyk 2023.
- 설치: NPM 패키지(`Packages/manifest.json`의 scoped registry `https://registry.npmjs.org/`, 범위 `com.kyrylokuzyk`). 소스를 이 저장소에 두지 않는다.
- 라이선스: MIT가 아니라 PrimeTween 자체 라이선스다. 무료·상업 게임에 써도 되고, 공개 저장소의 프로젝트도 NPM 패키지로 설치하면 된다. PrimeTween의 소스나 .tgz를 다른 결과물에 넣어 배포하거나, 되팔거나, 자기 것이라고 하면 안 된다. 원문: https://github.com/KyryloKuzyk/PrimeTween/blob/main/LICENSE

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

## PixelOE

- 무엇: 큰 그림을 도트 그림으로 바꾸는 도구. 칸마다 대비를 살려 한 색을 고른다. 게임에 들어가는 것은 바꾼 그림뿐이고 도구 코드는 싣지 않는다.
- 출처: https://github.com/KohakuBlueleaf/PixelOE (PyPI `pixeloe` 1.0.0). 이 저장소 밖의 가상환경(`E:/ArtTools/venv-pixeloe`)에 설치해 쓴다.
- 쓰는 곳: 나리온(`Tools/WebGame/narion.py`, 2026-11-09). 웹게임 원화 `narion-mount.png`를 게임 크기(논리 px 하나 = 4px)와 2부 타이틀 크기(3/4)로 바꾼다.
- 라이선스: Apache-2.0.
