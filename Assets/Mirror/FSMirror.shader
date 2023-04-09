Shader "FunS/Mirror-Base" {
    Properties {
        [NoScaleOffset] _LeftReflectionTex ("L", 2D) = "white" { }
        [NoScaleOffset] _RightReflectionTex ("R", 2D) = "white" { }
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+100" }
        LOD 100

        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata {
                half4 vertex : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                UNITY_FOG_COORDS(1)
                half4 vertex : SV_POSITION;
                half4 leftPosCS : TEXCOORD0;
                #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                    half4 rightPosCS : TEXCOORD1;
                #endif

                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _LeftReflectionTex;
            half4x4 _LeftReflectionProjectionMatrix;

            #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                sampler2D _RightReflectionTex;
                half4x4 _RightReflectionProjectionMatrix;
            #endif
            CBUFFER_END

            v2f vert(appdata v) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.leftPosCS = ComputeScreenPos(mul(mul(_LeftReflectionProjectionMatrix, UNITY_MATRIX_M), v.vertex));
                #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                    o.rightPosCS = ComputeScreenPos(mul(mul(_RightReflectionProjectionMatrix, UNITY_MATRIX_M), v.vertex));
                #endif

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            half2 CheckPlatformUV(half2 uv) {
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y;
                #endif
                return uv;
            }

            fixed4 frag(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 output;
                
                //https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/HLSLSupport.cginc
                #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                    if (unity_StereoEyeIndex) {
                        half2 rightUV = CheckPlatformUV(i.rightPosCS.xy / i.rightPosCS.w);
                        output = tex2D(_RightReflectionTex, rightUV);
                    } else {
                        half2 leftUV = CheckPlatformUV(i.leftPosCS.xy / i.leftPosCS.w);
                        output = tex2D(_LeftReflectionTex, leftUV);
                    }
                #else
                    half2 leftUV = CheckPlatformUV(i.leftPosCS.xy / i.leftPosCS.w);
                    output = tex2D(_LeftReflectionTex, leftUV);
                #endif

                UNITY_APPLY_FOG(i.fogCoord, output);

                return output;
            }
            ENDCG

        }
    }
}
