// 틸트 시프트: 화면 가운데 띠는 또렷하게 두고 위아래로 갈수록 흐리게 해서 미니어처를 내려다보는 느낌을 낸다(World.TiltShift).
// 흐림은 가운데 화소와 푸아송 원반 12점의 평균이다. 띠 안은 원래 화소를 그대로 돌려준다(모바일에서 대부분의 화소가 한 번만 읽힌다).
// 흐림 반지름은 1080줄 화면 기준 화소라 화면 해상도가 달라도 같은 정도로 보인다.
Shader "Hidden/MiniPandoras/TiltShift"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "TiltShift"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _FocusCenter;
            float _FocusRange;
            float _Fade;
            float _MaxBlur;

            static const int Taps = 12;
            static const float2 Disk[Taps] =
            {
                float2(-0.326, -0.406), float2(-0.840, -0.074), float2(-0.696, 0.457), float2(-0.203, 0.621),
                float2(0.962, -0.195), float2(0.473, -0.480), float2(0.519, 0.767), float2(0.185, -0.893),
                float2(0.507, 0.064), float2(0.896, 0.412), float2(-0.322, -0.933), float2(-0.792, -0.598)
            };

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 sharp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float amount = saturate((abs(uv.y - _FocusCenter) - _FocusRange) / _Fade);
                if (amount <= 0.0)
                {
                    return sharp;
                }

                float2 radius = amount * _MaxBlur * (_ScreenParams.y / 1080.0) * _BlitTexture_TexelSize.xy;
                half4 sum = sharp;
                for (int i = 0; i < Taps; i++)
                {
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + Disk[i] * radius);
                }

                return sum / (Taps + 1);
            }
            ENDHLSL
        }
    }
}
