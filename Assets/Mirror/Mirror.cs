using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace FunS
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    public class Mirror : MonoBehaviour
    {
        #region private variable
        [SerializeField] private Camera m_camera;
        [Space]
        [SerializeField, Range(0.1f, 1.0f)] private float m_screenScaleFactor = 0.5f;
        [SerializeField, Range(0.3f, 100f)] private float m_distance = 10f;
        [SerializeField] private LayerMask m_refCameraCullingMask = int.MaxValue;
        [Header("Require")]
        [SerializeField] private MeshRenderer m_refPlane;

        private Transform m_cameraTrans;
        private Camera m_refCamera;
        private Transform m_refCameraTrans;
        private Transform m_refPlaneTrans;
        private Material m_refPlaneMaterial;
        private float m_screenScaleFactorTemp = 0.5f;
        private bool m_isReadyToRender;
        private bool m_isRendering;
        private RenderTexture m_leftReflectionRenderTexture;
        private RenderTexture m_rightReflectionRenderTexture;

        private readonly int k_LeftReflectionProjectionMatrixID = Shader.PropertyToID("_LeftReflectionProjectionMatrix");
        private readonly int k_RightReflectionProjectionMatrixID = Shader.PropertyToID("_RightReflectionProjectionMatrix");
        private readonly int k_LeftReflectionTexID = Shader.PropertyToID("_LeftReflectionTex");
        private readonly int k_RightReflectionTexID = Shader.PropertyToID("_RightReflectionTex");
        #endregion

        #region public field
        public float ScreenScaleFactor
        {
            set => m_screenScaleFactor = Mathf.Clamp(value, 0.1f, 1.0f);
            get => m_screenScaleFactor;
        }
        public virtual bool IsCameraXRUsage
        {
            get => XRSettings.enabled && m_camera.stereoEnabled;
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
            StartCoroutine(WaitForVRReady());
        }
        private IEnumerator WaitForVRReady()
        {
            while (RenderingScreenSize.x == 0) yield return null;

            m_refPlaneMaterial = m_refPlane.material;
            m_refPlaneTrans = m_refPlane.transform;
            m_screenScaleFactorTemp = m_screenScaleFactor;
            CreateReflectionCamera();
            CreateRefCameraRenderTexture();
            m_isReadyToRender = true;
        }
        private void OnDestroy()
        {
            if (m_leftReflectionRenderTexture)
                Destroy(m_leftReflectionRenderTexture);
            if (m_rightReflectionRenderTexture)
                Destroy(m_leftReflectionRenderTexture);

            if (m_refCameraTrans)
                Destroy(m_refCameraTrans.gameObject);
        }
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }
        private void OnBeginCameraRendering(ScriptableRenderContext SRC, Camera camera)
        {
            if (camera != m_camera || !IsRequireReady() || !ArrowRender())
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
        protected virtual bool ArrowRender() => m_isReadyToRender;
        protected virtual void RenderRefIection(ScriptableRenderContext SRC)
        {
            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(m_refPlaneTrans.position, GetPlaneNormal());

            if (m_screenScaleFactorTemp != m_screenScaleFactor)
            {
                m_screenScaleFactorTemp = m_screenScaleFactor;
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

            UpdateMirrorCameraSetting();
        }
        protected virtual Vector3 GetPlaneNormal() => -m_refPlaneTrans.forward;
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
            m_refCamera.forceIntoRenderTexture = true;
            var data = m_refCamera.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
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
            if (targetRT == null)
            {
                targetRT = new RenderTexture(size.x, size.y, 16, RenderTextureFormat.Default);
            }
            else
            {
                targetRT.Release();
                targetRT.height = size.y;
                targetRT.width = size.x;
            }
            targetRT.anisoLevel = 0;
            targetRT.antiAliasing = 1;
            targetRT.Create();
            targetRT.name = $"[RefCam]{name}:{(stereoscopicEye == Camera.StereoscopicEye.Left ? "Left" : "Right")}";
        }
        private void UpdateMirrorCameraSetting()
        {
            m_refCamera.clearFlags = m_camera.clearFlags;
            m_refCamera.backgroundColor = m_camera.backgroundColor;
            m_refCamera.aspect = m_camera.aspect;
            //m_refCamera.nearClipPlane = Mathf.Max(m_camera.nearClipPlane, 0.1f);
            //m_refCamera.farClipPlane = Mathf.Min(m_camera.farClipPlane, m_distance);
            //Deselect masks in src that are not selected in dst.
            m_refCameraCullingMask &= m_camera.cullingMask;
            m_refCamera.cullingMask = m_refCameraCullingMask;

            m_refCamera.orthographic = m_camera.orthographic;
            m_refCamera.orthographicSize = m_camera.orthographicSize;
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
        private Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal)
        {
            Vector3 cpos = worldToCameraMatrix.MultiplyPoint(pos);
            Vector3 cnormal = worldToCameraMatrix.MultiplyVector(normal).normalized;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }
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

        #region DEBUG
#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [ShowInInspector] private bool IsCameraXRUsageShow => m_camera && IsCameraXRUsage;
        [ShowInInspector] private Vector2Int CalculateScreenSizeShow => CalculateScreenSize();
        [Title("Left", titleAlignment: TitleAlignments.Centered)]
        [HorizontalGroup("ReadOnly"), HideLabel, PreviewField(128, ObjectFieldAlignment.Center)]
        [ShowInInspector] private RenderTexture LRTShow => m_leftReflectionRenderTexture;
        [Title("Right", titleAlignment: TitleAlignments.Centered)]
        [HorizontalGroup("ReadOnly"), HideLabel, PreviewField(128, ObjectFieldAlignment.Center)]
        [ShowInInspector] private RenderTexture RRTShow => m_rightReflectionRenderTexture;
#endif
        //private void OnDrawGizmos()
        //{
        //    if (!Application.isPlaying || !m_camera || !m_refCamera) return;
        //    DrawCameraFrustum(m_camera, new Color(0, 1, 0, 0.7f));
        //    DrawCameraFrustum(m_refCamera, new Color(1, 0, 0, 0.7f));
        //}
        //private void DrawCameraFrustum(Camera c, Color color)
        //{
        //    var oriColor = Gizmos.color;
        //    var oriMat = Gizmos.matrix;

        //    Gizmos.color = color;
        //    Gizmos.matrix = Matrix4x4.TRS(c.transform.position, c.transform.rotation, Vector3.one);

        //    Gizmos.DrawLine(Vector3.zero, Vector3.forward * c.farClipPlane);
        //    Gizmos.DrawFrustum(Vector3.zero, c.fieldOfView, c.farClipPlane, c.nearClipPlane, c.aspect);
        //    Gizmos.DrawFrustum(Vector3.zero, c.fieldOfView, c.farClipPlane, 0, c.aspect);

        //    Gizmos.color = oriColor;
        //    Gizmos.matrix = oriMat;
        //}
#endif
        #endregion
    }
}