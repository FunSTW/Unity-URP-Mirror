# 🚧URP-Mirror🚧
🚧WIP🚧  
✨ Mirror in URP. Support VR Single-Pass-Instanced Rendering.

  https://user-images.githubusercontent.com/45548858/230920499-8b557a68-15cc-4027-8623-7e5ead72179e.mp4
# TODO 
- The reflection camera does not correctly render in Single-Pass-Instanced, it uses a two-time rendering like MuiltPass. [bug](#bug-tracking)
- MuiltPass rendering look not correctly.
- Shaderlab is invisible in PC platform.
- Disable the reflection Camera when the plane is being culled or invisible.
- CullingMatrix
- Disable mutual recursion with reflective cameras
# Reference
- https://github.com/eventlab-projects/com.quickvr.quickbase/tree/58f6677a122678e123785097b99994080b767866/Runtime/QuickMirror
# Bug Tracking
- [Single-Pass-Instanced Camera target RenderTexture Only LeftEye issue](https://issuetracker.unity3d.com/issues/xrsdk-urp-camera-with-a-rendertexture-does-not-render-in-stereo-in-spi-slash-multiview)
