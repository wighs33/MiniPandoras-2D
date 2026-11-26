// HD-2D 필드의 도트 그림(바닥 그림, 세운 인물·소품·집)을 해와 빛으로 비추는 재질.
// - 알파를 잘라(cutout) 깊이를 쓰므로 앞뒤가 정렬 순서가 아니라 실제 자리로 정해지고, 해 그림자를 드리우고 받는다.
// - 법선은 늘 땅의 위쪽(-Z)이다. 세운 그림도 땅과 똑같이 밝아서, 밝기 차이는 그림자에서만 난다(같은 자리의 땅과 인물이 어긋나 보이지 않는다).
// - SpriteRenderer.color나 재질 _Color의 알파가 1보다 작으면 그만큼 화면 격자로 듬성듬성 지운다(나무·집 뒤의 인물이 비쳐 보이게, 깊이를 그대로 쓰면서. World.Occluder).
// 좌우 뒤집기는 URP의 스프라이트 셰이더처럼 unity_SpriteProps로 한다.
// 잔디·잎 덩어리처럼 카드 여러 장을 묶은 메시(_CARD_MESH)는 메시의 꼭짓점 색과 법선을 쓴다(잎 덩어리는 수관 바깥을 향해 해 쪽이 밝다).
// 부피가 있는 모형(_FACE_LIT: 집)은 꼭짓점 법선이 면의 방향이다. 밝기의 바탕은 땅과 같고, 거기에 면이 해를 얼마나 마주 보는지만 더한다(FaceShade).
// 해가 오른쪽 조금 뒤에서 비추므로 앞벽은 해를 등진다. 그대로 두면 모형이 제 앞벽에 그림자를 드리워 앞면이 통째로 어두워지므로,
// 해를 등지거나 스치는 면은 그림자를 받지 않는다. 해를 받는 면(지붕·동쪽 벽)은 굴뚝·지붕창의 그림자를 그대로 받는다.
// 휴대폰 GPU를 아끼는 두 갈래(2026-11-23, 갤럭시 Z 플립4에서 GPU가 60fps를 넘겨서):
// - _SOLID: 구멍이 없는 바닥. 알파를 자르지 않아, 앞에 선 것에 가린 자리를 GPU가 칠하기 전에 버린다(알파를 자르는 셰이더는 칠해 봐야 안다).
//   재질의 순서(renderQueue)를 잔디·소품 뒤로 두어야 가린 자리를 안다(FieldSprites.MakeSolid).
// - _VERTEX_LIT: 잔디. 빛과 그림자를 화소가 아니라 꼭짓점에서 잰다. 포기 하나가 그림자의 번진 폭보다 작아 모습이 거의 같다.
// 그림자: 세운 그림은 얇은 판이라 해가 옆에서 비추면 그림자가 실처럼 얇아진다. 그래서 그림자를 그릴 때만 판을 발밑(카드 메시는 카드마다 아랫변 가운데,
// TEXCOORD1)을 축으로 해 쪽을 보게 돌려세운다(재질의 _ShadowFacesSun). 화면에 보이는 그림은 그대로이고, 땅에는 해에서 본 온전한 실루엣이 진다.
// 둥근 것(인물·나무·바위·잎 덩어리)만 그렇게 하고, 가로로 긴 담·집·성문은 돌리면 그림자가 엉뚱하게 길어지므로 제 방향 그대로 그림자를 드리운다.
Shader "MiniPandoras/Sprite Lit 3D"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [Toggle] _ShadowFacesSun ("Shadow Faces Sun", Float) = 1
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _Color;
        half _Cutoff;
        half _ShadowFacesSun;
    CBUFFER_END

    // 땅의 위쪽. 맵은 XY 평면에 깔리고 높이는 -Z 쪽이다(World.Upright).
    static const float3 GroundUp = float3(0, 0, -1);

    float3 SpritePositionWS(float3 positionOS)
    {
        #if defined(_CARD_MESH)
            return TransformObjectToWorld(positionOS);
        #else
            return TransformObjectToWorld(float3(positionOS.xy * unity_SpriteProps.xy, positionOS.z));
        #endif
    }

    half4 SpriteColor(half4 vertexColor)
    {
        #if defined(_CARD_MESH)
            return vertexColor * _Color;
        #else
            return vertexColor * _Color * unity_SpriteColor;
        #endif
    }

    float3 SpriteNormalWS(float3 normalOS)
    {
        #if defined(_CARD_MESH) && !defined(_FACE_LIT)
            return TransformObjectToWorldNormal(normalOS);
        #else
            return GroundUp;
        #endif
    }

    // 면이 해를 이보다 덜 마주 보면(등지거나 스치면) 해 그림자를 받지 않는다.
    static const half GrazingSun = 0.1;

    // 면의 밝기. 해를 등질수록 0.6까지 어둡고 마주 볼수록 1.15까지 밝다. 해를 스치는 앞벽은 그림에 칠한 밝기 그대로다.
    half FaceShade(half facing)
    {
        const half fullyTurned = 0.7;
        return facing < 0 ? lerp(1.0h, 0.6h, saturate(-facing / fullyTurned)) : lerp(1.0h, 1.15h, saturate(facing / fullyTurned));
    }

    // 4×4 바이어 격자. 덮는 정도(알파)가 낮을수록 더 많은 화소를 지운다.
    void ClipSprite(half textureAlpha, half rendererAlpha, float4 positionCS)
    {
        #if !defined(_SOLID)
            clip(textureAlpha - _Cutoff);
            static const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            uint2 cell = uint2(positionCS.xy) % 4;
            clip(rendererAlpha - (bayer[cell.y * 4 + cell.x] + 0.5) / 16.0);
        #endif
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local _CARD_MESH
            #pragma shader_feature_local _FACE_LIT
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SOLID
            #pragma shader_feature_local _VERTEX_LIT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half fogFactor : TEXCOORD2;
                #if defined(_VERTEX_LIT)
                    half3 light : TEXCOORD1;
                #else
                    float3 positionWS : TEXCOORD1;
                    half3 normalWS : TEXCOORD3;
                #endif
                #if defined(_FACE_LIT)
                    half3 faceWS : TEXCOORD4;
                #endif
            };

            #if defined(_VERTEX_LIT)
                // UniversalFragmentBlinnPhong에서 반짝임을 뺀 것과 같은 빛: 둘레 빛 + 해(그림자) + 등불.
                half3 LightAt(float3 positionWS, half3 normalWS)
                {
                    half3 light = SampleSH(normalWS);
                    Light sun = GetMainLight(TransformWorldToShadowCoord(positionWS), positionWS, half4(1, 1, 1, 1));
                    light += LightingLambert(sun.color * (sun.distanceAttenuation * sun.shadowAttenuation), sun.direction, normalWS);
                    #if defined(_ADDITIONAL_LIGHTS) && !USE_CLUSTER_LIGHT_LOOP
                        uint count = GetAdditionalLightsCount();
                        for (uint i = 0u; i < count; i++)
                        {
                            Light lamp = GetAdditionalLight(i, positionWS);
                            light += LightingLambert(lamp.color * lamp.distanceAttenuation, lamp.direction, normalWS);
                        }
                    #endif
                    return light;
                }
            #endif

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = SpritePositionWS(input.positionOS);
                half3 normalWS = SpriteNormalWS(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = SpriteColor(input.color);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                #if defined(_VERTEX_LIT)
                    output.light = LightAt(positionWS, normalWS);
                #else
                    output.positionWS = positionWS;
                    output.normalWS = normalWS;
                #endif
                #if defined(_FACE_LIT)
                    output.faceWS = TransformObjectToWorldNormal(input.normalOS);
                #endif
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                ClipSprite(texel.a, input.color.a, input.positionCS);

                #if defined(_VERTEX_LIT)
                    return half4(MixFog(texel.rgb * input.color.rgb * input.light, input.fogFactor), 1);
                #else

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = texel.rgb * input.color.rgb;
                surface.alpha = 1;
                surface.occlusion = 1;

                #if defined(_FACE_LIT)
                    // 면의 꼭짓점 순서는 정해져 있지 않다. 보이는 쪽이 바깥이다.
                    half3 face = normalize(input.faceWS);
                    face *= dot(face, inputData.viewDirectionWS) < 0 ? -1 : 1;
                    half facing = dot(face, _MainLightPosition.xyz);
                    surface.albedo *= FaceShade(facing);
                    if (facing < GrazingSun)
                    {
                        // URP는 그림자 지도 범위 밖(z가 1 이상)이면 그림자가 없다고 본다.
                        inputData.shadowCoord.z = 1;
                    }
                #endif

                half4 color = UniversalFragmentBlinnPhong(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local _CARD_MESH
            #pragma shader_feature_local _FACE_LIT
            #pragma shader_feature_local_fragment _SOLID
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 pivotOS : TEXCOORD1;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            // 판을 축(pivotWS) 둘레로 땅의 위쪽을 축 삼아 돌려, 판의 가로 방향이 해의 가로 방향과 직각이 되게 한다(판이 해를 마주 본다).
            float3 FaceTheLight(float3 positionWS, float3 pivotWS, float3 toLight)
            {
                float3 up = GroundUp;
                float3 lightAcross = toLight - dot(toLight, up) * up;
                float3 rightWS = TransformObjectToWorldDir(float3(1, 0, 0));
                float3 rightAcross = rightWS - dot(rightWS, up) * up;
                if (dot(lightAcross, lightAcross) < 1e-4 || dot(rightAcross, rightAcross) < 1e-4)
                {
                    return positionWS;
                }

                float3 target = normalize(cross(up, normalize(lightAcross)));
                rightAcross = normalize(rightAcross);
                // 판이 뒤집혀 보이지 않게, 지금 가로 방향에 가까운 쪽으로 돈다.
                target *= dot(target, rightAcross) >= 0 ? 1 : -1;
                float cosine = dot(rightAcross, target);
                float sine = dot(cross(rightAcross, target), up);
                float3 offset = positionWS - pivotWS;
                offset = offset * cosine + cross(up, offset) * sine + up * dot(up, offset) * (1 - cosine);
                return pivotWS + offset;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                // 그림자를 그릴 때는 스프라이트의 뒤집기(unity_SpriteProps)와 색(unity_SpriteColor)이 채워지지 않아, 그대로 쓰면 그림자가 사라지거나 늘어난다.
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 toLight = normalize(_LightPosition - positionWS);
                #else
                    float3 toLight = _LightDirection;
                    #if defined(_CARD_MESH)
                        float3 pivotWS = TransformObjectToWorld(input.pivotOS);
                    #else
                        float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));
                    #endif
                    if (_ShadowFacesSun > 0.5)
                    {
                        positionWS = FaceTheLight(positionWS, pivotWS, toLight);
                    }
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, SpriteNormalWS(input.normalOS), toLight));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                ClipSprite(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a, input.color.a, input.positionCS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local _CARD_MESH
            #pragma shader_feature_local_fragment _SOLID

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(SpritePositionWS(input.positionOS));
                output.uv = input.uv;
                output.color = SpriteColor(input.color);
                return output;
            }

            half Fragment(Varyings input) : SV_Target
            {
                ClipSprite(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a, input.color.a, input.positionCS);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local _CARD_MESH
            #pragma shader_feature_local_fragment _SOLID

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(SpritePositionWS(input.positionOS));
                output.uv = input.uv;
                output.color = SpriteColor(input.color);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                ClipSprite(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a, input.color.a, input.positionCS);
                return half4(NormalizeNormalPerPixel(GroundUp), 0);
            }
            ENDHLSL
        }
    }
}
