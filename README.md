# URP-Mirror
✨ Support VR Single-Pass-Instanced Rendering 
# TODO 
- The reflection camera does not correctly render in Single-Pass-Instanced, it uses a two-time rendering like MuiltPass. [bug](#bug-tracking)
- Disable the reflection Camera when the plane is being culled or invisible.
- CullingMatrix
- Clear the RenderTexture when the mirror set disabled.
# Reference
- https://github.com/eventlab-projects/com.quickvr.quickbase/tree/58f6677a122678e123785097b99994080b767866/Runtime/QuickMirror
# Bug Tracking
- [Single-Pass-Instanced Camera target RenderTexture Only LeftEye issue](https://issuetracker.unity3d.com/issues/xrsdk-urp-camera-with-a-rendertexture-does-not-render-in-stereo-in-spi-slash-multiview)
