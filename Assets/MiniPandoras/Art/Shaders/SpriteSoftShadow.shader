// 인물의 그림을 흐린 그림자로 그린다. 모양은 그림의 알파에서, 색과 진하기는 SpriteRenderer.color에서 온다.
// CharacterAnimator가 몸 그림을 그대로 베껴 넣고, 땅에 눕히는 것은 부모 Transform이 한다(FieldSceneBuilder).
// URP의 Sprite-Unlit-Default를 그대로 따르고 조각 셰이더만 바꿨다(뒤집기·인스턴싱이 같게 동작하도록).
Shader "MiniPandoras/Sprite Soft Shadow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Blur ("그림자 끝의 흐림 (그림 크기에 견준 비율, 가로·세로)", Vector) = (0.11, 0.1, 0, 0)
        _NearBlur ("발치의 흐림 (끝에 견준 비율)", Range(0, 1)) = 0.2
        _FarFade ("그림자 끝의 옅어짐", Range(0, 1)) = 0.55
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment SoftShadowFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float2 _Blur;
                float _NearBlur;
                half _FarFade;
            CBUFFER_END

            // 흐림의 안쪽 고리 4점과 바깥 고리 8점. 인물은 화면에서 작아서 이 정도면 계단이 보이지 않는다.
            static const int TapCount = 12;
            static const float2 Taps[TapCount] =
            {
                float2(0.5, 0.0), float2(0.0, 0.5), float2(-0.5, 0.0), float2(0.0, -0.5),
                float2(1.0, 0.0), float2(0.7, 0.7), float2(0.0, 1.0), float2(-0.7, 0.7),
                float2(-1.0, 0.0), float2(-0.7, -0.7), float2(0.0, -1.0), float2(0.7, -0.7)
            };

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 SoftShadowFragment(Varyings input) : SV_Target
            {
                // 그림의 위쪽(uv.y가 큰 쪽)이 땅에서는 그림자의 먼 끝이다.
                float away = saturate(input.uv.y);
                // 실제 그림자처럼 발치는 또렷하고 멀수록 번진다. 고르게 흐리면 가는 다리가 사라져 그림자가 발에서 떨어져 보인다.
                float2 reach = lerp(_NearBlur, 1.0, away) * _Blur;
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                for (int tap = 0; tap < TapCount; tap++)
                {
                    alpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + Taps[tap] * reach).a;
                }

                alpha /= TapCount + 1;
                return half4(input.color.rgb, alpha * (1.0 - _FarFade * away) * input.color.a);
            }
            ENDHLSL
        }
    }
}
