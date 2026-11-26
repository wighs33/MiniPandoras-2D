// 타이틀 무대의 잔잔한 물. 거울 카메라(Stages.WaterMirror)가 찍은 그림을 잔물결로 흔들어 비춘다.
// 멀수록(비스듬할수록) 또렷이 비치고, 가까울수록 물빛이 짙다. 발밑에서 동심원이 퍼진다.
Shader "MiniPandoras/Stage/Water"
{
    Properties
    {
        [HDR] _Deep ("Deep", Color) = (0.01, 0.02, 0.06, 1)
        [HDR] _Tint ("Reflection Tint", Color) = (0.8, 0.85, 1, 1)
        [HDR] _Glint ("Glint", Color) = (0.6, 0.8, 1, 1)
        [NoScaleOffset] _MirrorTex ("Mirror", 2D) = "black" {}
        _Wave ("Wave", Float) = 0.012
        _Scale ("Wave Scale", Float) = 0.5
        _Near ("Near Reflection", Range(0, 1)) = 0.35
        _RippleAt ("Ripple At (x, y, strength)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "StageNoise.hlsl"

            TEXTURE2D(_MirrorTex);
            SAMPLER(sampler_MirrorTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep, _Tint, _Glint;
                float4 _RippleAt;
                float _Wave, _Scale, _Near;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                float2 onStage : TEXCOORD2;
            };

            Varyings Vertex(float3 positionOS : POSITION)
            {
                Varyings output;
                output.onStage = positionOS.xy;
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.onStage * _Scale;
                float t = _Time.y;
                float2 wave = float2(
                    StageNoise(p * float2(1.1, 3.2) + float2(t * 0.22, t * 0.31)) + StageNoise(p * float2(2.7, 7.1) - float2(t * 0.35, t * 0.2)) - 1,
                    StageNoise(p * float2(1.3, 3.6) - float2(t * 0.18, t * 0.27)) + StageNoise(p * float2(3.1, 8.3) + float2(t * 0.3, t * 0.4)) - 1);

                float2 fromFeet = input.onStage - _RippleAt.xy;
                float reach = length(fromFeet);
                float ring = sin(reach * 5.2 - t * 2.4) * exp(-reach * 0.75) * _RippleAt.z;
                wave += normalize(fromFeet + 1e-4) * ring;

                float3 toCamera = GetCameraPositionWS() - input.positionWS;
                float distanceFade = saturate(6 / max(1, length(toCamera)));
                // 무대의 위는 -Z다.
                float facing = saturate(-normalize(toCamera).z);
                float mirrorAmount = lerp(1, _Near, smoothstep(0.05, 0.5, facing));

                float2 screen = GetNormalizedScreenSpaceUV(input.positionCS);
                screen.y = 1 - screen.y;
                screen += wave * float2(_Wave * 0.6, _Wave) * lerp(0.35, 1, distanceFade);
                half3 mirror = SAMPLE_TEXTURE2D(_MirrorTex, sampler_MirrorTex, saturate(screen)).rgb;

                half3 color = lerp(_Deep.rgb, mirror * _Tint.rgb, mirrorAmount);
                float glint = pow(saturate(wave.x * 0.5 + wave.y * 0.5 + 0.35), 10) * 3;
                color += _Glint.rgb * (glint * (0.2 + dot(mirror, half3(0.4, 0.4, 0.4))) + saturate(ring) * 0.12 * _RippleAt.z);
                return half4(MixFog(color, input.fogFactor), 1);
            }
            ENDHLSL
        }
    }
}
