Shader "ProjectWait/Storybook Toon"
{
    Properties
    {
        [MainColor] _BaseColor ("Base color", Color) = (1,1,1,1)
        _ShadeColor ("Shadow tint (multiplier)", Color) = (0.55,0.63,0.69,1)
        _MidTone ("Middle band brightness", Range(0,1)) = 0.84
        _ShadowThreshold ("Shadow band threshold", Range(-1,1)) = 0.08
        _LightThreshold ("Light band threshold", Range(-1,1)) = 0.65
        _BandSoftness ("Band edge softness", Range(0.001,0.15)) = 0.015
        _OutlineColor ("Ink color", Color) = (0.19,0.12,0.09,1)
        _OutlineWidth ("Ink width (reference pixels)", Range(0,5)) = 1.6
        _EmissionStrength ("Unlit blend", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 150
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadeColor;
            half4 _OutlineColor;
            half _MidTone, _ShadowThreshold, _LightThreshold, _BandSoftness;
            half _OutlineWidth, _EmissionStrength;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings ToonVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "ToonSurface"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            half4 ToonFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light light = GetMainLight(shadowCoord);
                half ndl = dot(normalize(input.normalWS), light.direction);
                // Three stable bands. A small derivative-aware edge prevents distant shimmer.
                half edge = max(_BandSoftness, fwidth(ndl));
                half middle = smoothstep(_ShadowThreshold-edge, _ShadowThreshold+edge, ndl);
                half lit = smoothstep(_LightThreshold-edge, _LightThreshold+edge, ndl);
                half3 tone = lerp(_ShadeColor.rgb, _MidTone.xxx, middle);
                tone = lerp(tone, half3(1,1,1), lit);
                tone *= lerp(half3(0.76,0.80,0.85), half3(1,1,1), light.shadowAttenuation);
                half3 color = _BaseColor.rgb * lerp(tone, half3(1,1,1), _EmissionStrength);
                return half4(color, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "StoryInk"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex InkVertex
            #pragma fragment InkFragment
            #pragma multi_compile_instancing
            Varyings InkVertex(Attributes input)
            {
                Varyings output = ToonVertex(input);
                // Expand in world space: nonuniform object scale does not stretch the line.
                // Reference 900px height keeps captures and headset render scales consistent.
                float distanceToEye = max(0.5, distance(GetCameraPositionWS(), output.positionWS));
                float widthWS = min(0.035, _OutlineWidth * distanceToEye / (450.0 * abs(UNITY_MATRIX_P._m11)));
                output.positionCS = TransformWorldToHClip(output.positionWS + normalize(output.normalWS)*widthWS);
                return output;
            }
            half4 InkFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(_OutlineWidth - 0.001);
                return _OutlineColor;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings ShadowVertex(Attributes input)
            {
                Varyings output = ToonVertex(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDir = normalize(_LightPosition-output.positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS,normalize(output.normalWS),lightDir));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                return output;
            }
            half4 DepthFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            half4 DepthFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}
