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

            struct appdata {
                half4 vertex : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                half4 vertex : SV_POSITION;
                float fogCoord : TEXCOORD0;
                half4 refPosCS : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _LeftReflectionTex;
            half4x4 _LeftReflectionProjectionMatrix;
            sampler2D _RightReflectionTex;
            half4x4 _RightReflectionProjectionMatrix;
            CBUFFER_END

            v2f vert(appdata v) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                ZERO_INITIALIZE(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                
                GetMirrorUV_VertexInput_half(v.vertex.xyz, _LeftReflectionProjectionMatrix, _RightReflectionProjectionMatrix, o.refPosCS);

                o.fogCoord = ComputeFogFactor(o.vertex.z);
                return o;
            }

            half2 CheckPlatformUV(half2 uv) {
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y;
                #endif
                return uv;
            }

            half4 frag(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 output;
                half2 uv;
                GetMirrorUV_FragInput_half(i.refPosCS, uv);
                
                output = SampleMirrorTex(_LeftReflectionTex, _RightReflectionTex, uv);
                return output;
            }
            ENDHLSL

        }
    }
    }