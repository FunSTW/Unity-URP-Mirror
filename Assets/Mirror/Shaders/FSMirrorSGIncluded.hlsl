#ifndef FSMIRRORSGINCLUDED_INCLUDED
#define FSMIRRORSGINCLUDED_INCLUDED

/* ----------- Vert ----------- */
void GetMirrorUV_VertexInput_float(float3 posOS, float4x4 LVP, float4x4 RVP, out float4 posSS) {
    #ifdef SHADERGRAPH_PREVIEW
        posSS = 1.0;
    #else
        #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            if (unity_StereoEyeIndex) {
                posSS = ComputeScreenPos(mul(mul(RVP, UNITY_MATRIX_M), float4(posOS, 1.0)));
            } else {
                posSS = ComputeScreenPos(mul(mul(LVP, UNITY_MATRIX_M), float4(posOS, 1.0)));
            }
        #else
            posSS = ComputeScreenPos(mul(mul(LVP, UNITY_MATRIX_M), float4(posOS, 1.0)));
        #endif
    #endif
}
void GetMirrorUV_VertexInput_half(float3 posOS, float4x4 LVP, float4x4 RVP, out float4 posSS) {
    #ifdef SHADERGRAPH_PREVIEW
        posSS = 1.0;
    #else
        #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            if (unity_StereoEyeIndex) {
                posSS = ComputeScreenPos(mul(mul(RVP, UNITY_MATRIX_M), float4(posOS, 1.0)));
            } else {
                posSS = ComputeScreenPos(mul(mul(LVP, UNITY_MATRIX_M), float4(posOS, 1.0)));
            }
        #else
            posSS = ComputeScreenPos(mul(mul(LVP, UNITY_MATRIX_M), float4(posOS, 1.0)));
        #endif
    #endif
}

/* ----------- Frag ----------- */
void GetMirrorUV_FragInput_float(float4 posSS, out float2 uv) {
    uv = posSS.xy / posSS.w;
    #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1 - uv.y;
    #endif
}
void GetMirrorUV_FragInput_half(float4 posSS, out half2 uv) {
    uv = posSS.xy / posSS.w;
    #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1 - uv.y;
    #endif
}

void SampleMirrorTex_float(UnityTexture2D lTex, UnityTexture2D rTex, float2 uv, out float3 color) {
    #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        if (unity_StereoEyeIndex) {
            color = tex2D(rTex, uv);
        } else {
            color = tex2D(lTex, uv);
        }
    #else
        color = tex2D(lTex, uv);
    #endif
}
void SampleMirrorTex_half(UnityTexture2D lTex, UnityTexture2D rTex, half2 uv, out half3 color) {
    #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        if (unity_StereoEyeIndex) {
            color = tex2D(rTex, uv);
        } else {
            color = tex2D(lTex, uv);
        }
    #else
        color = tex2D(lTex, uv);
    #endif
}

#endif //FSMIRRORSGINCLUDED_INCLUDED