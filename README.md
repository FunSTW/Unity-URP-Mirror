# URP-Mirror

✨ Mirror in URP. For VR Single-Pass-Instanced Rendering.  

🛠️ VR MultiPass is currently not supported, but there may be future updates to include it.
  
 \- | all-in-one VR Device | Support
----------|---------|----------
 Meta | Quest2, QuestPro | ✅
 HTC | Focus3, Elite | ⚠️ RightEye OnBeginCameraRendering has some issue

  https://user-images.githubusercontent.com/45548858/230920499-8b557a68-15cc-4027-8623-7e5ead72179e.mp4
  
  https://user-images.githubusercontent.com/45548858/231923238-ac356498-1917-4987-86ad-73e517d4b5cf.mp4
  
# TODO 
- The reflection camera does not correctly render in Single-Pass-Instanced, it uses a two-time rendering like MuiltPass. see [bug](#bug-tracking)
- Shaderlab is invisible in PC platform.
- CullingMatrix

# Reference
- https://github.com/eventlab-projects/com.quickvr.quickbase/tree/58f6677a122678e123785097b99994080b767866/Runtime/QuickMirror

# Bug Tracking
- [Single-Pass-Instanced Rendering RenderTexture issue](https://issuetracker.unity3d.com/issues/xrsdk-urp-camera-with-a-rendertexture-does-not-render-in-stereo-in-spi-slash-multiview)
