Shader "FunS/Mirror-Base" {
    Properties {
        [NoScaleOffset] _LeftReflectionTex ("L", 2D) = "white" { }
        [NoScaleOffset] _RightReflectionTex ("R", 2D) = "white" { }
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FSMirrorIncluded.hlsl"

            struct Attributes {
                float4 vertex : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 vertex : SV_POSITION;
                float fogCoord : TEXCOORD0;
                float4 refPosCS : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _LeftReflectionTex;
            float4x4 _LeftReflectionProjectionMatrix;
            sampler2D _RightReflectionTex;
            float4x4 _RightReflectionProjectionMatrix;
            CBUFFER_END

            half2 CheckPlatformUV(half2 uv) {
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y;
                #endif
                return uv;
            }

            Varyings vert(Attributes IN) {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                ZERO_INITIALIZE(Varyings, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                
                GetMirrorUV_VertexInput_float(IN.vertex.xyz, _LeftReflectionProjectionMatrix, _RightReflectionProjectionMatrix, OUT.refPosCS);

                OUT.fogCoord = ComputeFogFactor(OUT.vertex.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half4 output;
                float2 uv;
                GetMirrorUV_FragInput_float(IN.refPosCS, uv);
                
                output = SampleMirrorTex(_LeftReflectionTex, _RightReflectionTex, uv);
                output.rgb = MixFog(output.rgb, IN.fogCoord);

                return output;
            }
            ENDHLSL

        }
    }
}