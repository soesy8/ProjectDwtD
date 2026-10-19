Shader "ProjectWait/Illustrated Water"
{
    Properties
    {
        _BaseColor ("Water",Color) = (.32,.64,.69,.70)
        _ReflectionColor ("Painted window reflection",Color) = (.93,.94,.80,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ReflectionColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Authored, still reflections: readable cel shapes without a second camera.
                float x=i.world.x+.025*sin(i.world.z*22)+.012*sin(i.world.z*43);
                float pane=1-smoothstep(.20,.24,abs(frac((x+.20)*1.7)-.5));
                float cross=smoothstep(.045,.07,abs(frac(i.world.z*1.1)-.5));
                float strength=pane*cross*.26;
                half3 c=lerp(_BaseColor.rgb,_ReflectionColor.rgb,strength);
                return half4(c,_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
