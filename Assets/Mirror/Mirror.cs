using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace FunS
{
    public class Mirror : MonoBehaviour
    {
        #region private variable
        [SerializeField] private Camera m_camera;
        [Header("Rendering")]
        [SerializeField] private LayerMask m_mirrorRenderCullingMask = int.MaxValue;
        [SerializeField, Range(0.1f, 1.0f)] private float m_screenScaleFactor = 0.5f;
        //[SerializeField, Range(0.3f, 100f)] private float m_distance = 10f;
        [SerializeField] private MSAASamples m_msaa = MSAASamples.MSAA4x;
        [SerializeField] private bool m_useShadow;
        //[SerializeField] private Color m_fadeColor = new Color(0.16f, 0.53f, 0.06f);

        [Header("Require")]
        [SerializeField] private MeshRenderer m_refPlane;

        private Transform m_cameraTrans;
        private Camera m_refCamera;
        private Transform m_refCameraTrans;
        private UniversalAdditionalCameraData m_refCameraUrpData;
        private Transform m_refPlaneTrans;
        private Material m_refPlaneMaterial;
        private bool m_isReadyToRender;
        private bool m_isRendering;
        private RenderTexture m_leftReflectionRenderTexture;
        private RenderTexture m_rightReflectionRenderTexture;

        private float m_screenScaleFactorTemp;
        private MSAASamples m_msaaTemp;
        private bool m_useMipMapTemp;
        //private static List<Camera> s_mirrorRenderingReflectionCamera = new List<Camera>();

        private readonly int k_LeftReflectionProjectionMatrixID = Shader.PropertyToID("_LeftReflectionProjectionMatrix");
        private readonly int k_RightReflectionProjectionMatrixID = Shader.PropertyToID("_RightReflectionProjectionMatrix");
        private readonly int k_LeftReflectionTexID = Shader.PropertyToID("_LeftReflectionTex");
        private readonly int k_RightReflectionTexID = Shader.PropertyToID("_RightReflectionTex");
        //private readonly int k_FadeColorID = Shader.PropertyToID("_FadeColor");
        #endregion

        #region public field
        public float ScreenScaleFactor
        {
            set => m_screenScaleFactor = value;
            get => m_screenScaleFactor;
        }
        public MSAASamples MSAA
        {
            set => m_msaa = value;
            get => m_msaa;
        }
        public bool UseShadow
        {
            set => m_useShadow = value;
            get => m_useShadow;
        }
        public virtual bool IsCameraXRUsage
        {
            get => XRSettings.enabled && m_camera.stereoEnabled;
        }
        public virtual bool IsMultiPass
        {
            get => XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass;
        }
        public bool IsRendering
        {
            get => m_isRendering;
        }
        public Vector2Int RenderingScreenSize
        {
            get
            {
                Vector2Int usageSize = GetUsageScreenSize();
                usageSize.x = (int)(m_screenScaleFactor * usageSize.x);
                usageSize.y = (int)(m_screenScaleFactor * usageSize.y);
                return usageSize;
            }
        }
        #endregion

        #region mono
        private void Awake()
        {
            if (m_camera == null) m_camera = Camera.main;
            m_refPlaneMaterial = m_refPlane.material;
            m_refPlaneTrans = m_refPlane.transform;
            m_screenScaleFactorTemp = m_screenScaleFactor;
            m_msaaTemp = m_msaa;
            StartCoroutine(WaitXRReady());
        }
        private IEnumerator WaitXRReady()
        {
            while (RenderingScreenSize.x == 0) yield return null;
            CreateReflectionCamera();
            CreateRefCameraRenderTexture();
            m_isReadyToRender = true;
        }
        private void OnDestroy()
        {
            if (m_leftReflectionRenderTexture)
                DestroyImmediate(m_leftReflectionRenderTexture);
            if (m_rightReflectionRenderTexture)
                DestroyImmediate(m_rightReflectionRenderTexture);

            if (m_refCameraTrans)
                DestroyImmediate(m_refCameraTrans.gameObject);
        }
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (m_leftReflectionRenderTexture)
                DestroyImmediate(m_leftReflectionRenderTexture);
            m_leftReflectionRenderTexture = null;
            if (m_rightReflectionRenderTexture)
                DestroyImmediate(m_rightReflectionRenderTexture);
            m_rightReflectionRenderTexture = null;
            m_isRendering = false;
            //s_mirrorRenderingReflectionCamera.Remove(m_refCamera);
        }
        private void OnBeginCameraRendering(ScriptableRenderContext SRC, Camera camera)
        {
            if (camera != m_camera) return;
            if (!IsRequireReady() || !ArrowRender())
            {
                m_isRendering = false;
                return;
            }
            m_isRendering = true;
            RenderRefIection(SRC);
        }
        #endregion

        #region protected method
        protected virtual bool IsRequireReady() =>
            m_refCamera != null
            && m_refPlaneMaterial != null
            && m_refPlane != null;
        protected virtual bool ArrowRender() => m_isReadyToRender/* && Vector3.Dot(GetPlaneNormal(),m_cameraTrans.forward) < 0.0f*/;
        protected virtual Vector3 GetPlaneNormal() => -m_refPlaneTrans.forward;
        protected virtual void RenderRefIection(ScriptableRenderContext SRC)
        {
            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(m_refPlaneTrans.position, GetPlaneNormal());

            if (m_leftReflectionRenderTexture == null
                || m_msaaTemp != m_msaa
                || m_screenScaleFactorTemp != m_screenScaleFactor)
            {
                m_msaaTemp = m_msaa;
                m_screenScaleFactorTemp = m_screenScaleFactor = Mathf.Clamp(m_screenScaleFactor, 0.1f, 1.0f); ;

                CreateRefCameraRenderTexture();
            }

            //https://github.com/eventlab-projects/com.quickvr.quickbase/blob/ad510ec90049e463eb897da65459fa65630d4e54/Runtime/QuickMirror/QuickMirrorReflection_v2.cs#L83
            /* Position */
            Vector3 camToPlane = m_cameraTrans.position - m_refPlaneTrans.position;
            Vector3 reflectionCamToPlane = Vector3.Reflect(camToPlane, GetPlaneNormal());
            Vector3 camPosRS = transform.position + reflectionCamToPlane;
            m_refCameraTrans.position = camPosRS;

            /* Rotation */
            Vector3 reflectionFwd = Vector3.Reflect(m_cameraTrans.forward, GetPlaneNormal());
            Vector3 reflectionUp = Vector3.Reflect(m_cameraTrans.up, GetPlaneNormal());
            Quaternion q = Quaternion.LookRotation(reflectionFwd, reflectionUp);
            m_refCameraTrans.rotation = q;

            SetupReflectionCameraProp(m_camera);

            if (!IsCameraXRUsage)
            {
                SetupReflectionCameraMatrix(Camera.MonoOrStereoscopicEye.Mono, reflectionMatrix);
                RenderReflectionCamera(SRC);
            }
            else
            {
                SetupReflectionCameraMatrix(Camera.MonoOrStereoscopicEye.Left, reflectionMatrix);
                RenderReflectionCamera(SRC);
                SetupReflectionCameraMatrix(Camera.MonoOrStereoscopicEye.Right, reflectionMatrix);
                RenderReflectionCamera(SRC);
            }
        }
        protected virtual void SetupReflectionCameraProp(Camera src)
        {
            m_refCamera.clearFlags = src.clearFlags;
            m_refCamera.backgroundColor = src.backgroundColor;
            m_refCamera.aspect = src.aspect;
            //m_refCamera.nearClipPlane = Mathf.Max(m_camera.nearClipPlane, 0.1f);
            //m_refCamera.farClipPlane = m_distance;
            //Deselect masks in src that are not selected in dst.
            m_mirrorRenderCullingMask &= src.cullingMask;
            m_refCamera.cullingMask = m_mirrorRenderCullingMask;

            m_refCamera.orthographic = src.orthographic;
            m_refCamera.orthographicSize = src.orthographicSize;
            //m_refCamera.allowMSAA
            m_refCameraUrpData.renderShadows = m_useShadow;
        }
        #endregion

        #region private method
        private void CreateReflectionCamera()
        {
            m_cameraTrans = m_camera.transform;
            GameObject refCamGO = new GameObject($"[RefCam]{name}");
            m_refCameraTrans = refCamGO.transform;
            m_refCameraTrans.SetParent(transform.parent);
            m_refCamera = refCamGO.AddComponent<Camera>();
            m_refCamera.enabled = false;
            m_refCamera.stereoTargetEye = StereoTargetEyeMask.None;
            m_refCamera.forceIntoRenderTexture = true;
            m_refCameraUrpData = m_refCamera.GetUniversalAdditionalCameraData();
            m_refCameraUrpData.requiresColorOption = CameraOverrideOption.Off;
            m_refCameraUrpData.requiresDepthOption = CameraOverrideOption.Off;
        }
        private void CreateRefCameraRenderTexture()
        {
            CreateRenderTexture(Camera.StereoscopicEye.Left);
            m_refPlaneMaterial.SetTexture(k_LeftReflectionTexID, m_leftReflectionRenderTexture);

            if (IsCameraXRUsage)
            {
                CreateRenderTexture(Camera.StereoscopicEye.Right);
                m_refPlaneMaterial.SetTexture(k_RightReflectionTexID, m_rightReflectionRenderTexture);
            }
        }
        private void CreateRenderTexture(Camera.StereoscopicEye stereoscopicEye)
        {
            //Mono,Left = LeftTex,Right = RightTex
            ref RenderTexture targetRT = ref stereoscopicEye == Camera.StereoscopicEye.Right
                ? ref m_rightReflectionRenderTexture
                : ref m_leftReflectionRenderTexture;

            Vector2Int size = RenderingScreenSize;

            bool recreate = targetRT == null || targetRT.antiAliasing != (int)m_msaa;
            if (recreate)
            {
                if (targetRT) DestroyImmediate(targetRT);
                targetRT = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.Default);
            }
            else if (targetRT.height != size.y || targetRT.width != size.x)
            {
                targetRT.Release();
                targetRT.height = size.y;
                targetRT.width = size.x;
            }

            targetRT.antiAliasing = (int)m_msaa;
            targetRT.anisoLevel = 0;
            if (m_msaa == MSAASamples.None)
            {
                targetRT.depth = 16;
            }

            if (!targetRT.IsCreated())
                targetRT.Create();

            targetRT.name = $"[RefCam]{name} {(stereoscopicEye == Camera.StereoscopicEye.Left ? "Left" : "Right")}:{this.GetInstanceID()}";
        }
        private void SetupReflectionCameraMatrix(Camera.MonoOrStereoscopicEye eye, Matrix4x4 reflectionMatrix)
        {
            bool isMono = eye == Camera.MonoOrStereoscopicEye.Mono;

            //https://github.com/eventlab-projects/com.quickvr.quickbase/blob/ad510ec90049e463eb897da65459fa65630d4e54/Runtime/QuickMirror/QuickMirrorReflection_v2.cs#L141
            m_refCamera.worldToCameraMatrix = isMono ? m_camera.worldToCameraMatrix : m_camera.GetStereoViewMatrix((Camera.StereoscopicEye)eye);
            m_refCamera.worldToCameraMatrix *= reflectionMatrix;

            m_refCamera.projectionMatrix = isMono ? m_camera.projectionMatrix : m_camera.GetStereoProjectionMatrix((Camera.StereoscopicEye)eye);
            Vector4 clipPlane = CameraSpacePlane(m_refCamera.worldToCameraMatrix, m_refPlaneTrans.position, GetPlaneNormal());
            m_refCamera.projectionMatrix = m_refCamera.CalculateObliqueMatrix(clipPlane);

            if (eye != Camera.MonoOrStereoscopicEye.Right)
            {
                m_refCamera.targetTexture = m_leftReflectionRenderTexture;
                m_refPlaneMaterial.SetMatrix(k_LeftReflectionProjectionMatrixID, m_refCamera.projectionMatrix * m_refCamera.worldToCameraMatrix);
            }
            else
            {
                m_refCamera.targetTexture = m_rightReflectionRenderTexture;
                m_refPlaneMaterial.SetMatrix(k_RightReflectionProjectionMatrixID, m_refCamera.projectionMatrix * m_refCamera.worldToCameraMatrix);
            }
        }
        private void RenderReflectionCamera(ScriptableRenderContext SRC)
        {
            GL.invertCulling = true;
            UniversalRenderPipeline.RenderSingleCamera(SRC, m_refCamera);
            GL.invertCulling = false;
        }
        private Vector2Int GetUsageScreenSize()
        {
            return IsCameraXRUsage
                ? new Vector2Int(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight)
                : new Vector2Int(Screen.width, Screen.height);
        }
        //https://danielilett.com/2019-12-18-tut4-3-matrix-matching/
        private Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal)
        {
            Plane p = new Plane(normal, pos);
            Vector4 clipPlane = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
            Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(worldToCameraMatrix)) * clipPlane;
            return clipPlaneCameraSpace;
        }
        #endregion

        #region private static method
        //private static bool IsReflectionCamera(Camera camera)
        //{
        //    foreach (var cam in s_mirrorRenderingReflectionCamera)
        //        if (cam == camera)
        //            return true;
        //    return false;
        //}
        #endregion

        #region ref
        //https://github.com/eventlab-projects/com.quickvr.quickbase/blob/ad510ec90049e463eb897da65459fa65630d4e54/Runtime/QuickMirror/QuickMirrorReflection_v2.cs#L184
        // Calculates reflection matrix around the given plane
        protected virtual Matrix4x4 CalculateReflectionMatrix(Vector3 pivot, Vector3 facingNormal)
        {
            float d = -Vector3.Dot(facingNormal, pivot);
            Vector4 plane = new Vector4(facingNormal.x, facingNormal.y, facingNormal.z, d);

            Matrix4x4 reflectionMat = Matrix4x4.zero;

            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }
        #endregion
    }
}