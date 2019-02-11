# On-Device Object Detection with Hololens

The application should be build with Windows 10 build 1809, October Creators update. The same update should be deployed on HoloLens.

## Setup
1. Make sure your local git supports LFS (Large File Storage)
2. Clone this repository. If using Tortoise Git, check the LFS box.
3. Clone [Mixed Reality Toolkit](https://github.com/Microsoft/MixedRealityToolkit-Unity)
4. `git checkout 2017.4.3.0` from the Mixed Reality Toolkit directory
5. Copy `Assets\MixedRealityToolkit-SDK`, `Assets\MixedRealityToolkit-Examples`, `Assets\MixedRealityToolkit-Preview` to the `Assets` directory under `HoloOD\HoloOD`
6. Follow the instructions in [HoloLensCameraStream README](https://github.com/fierval/HoloOD/tree/master/HoloLensCameraStream) to build the camera capture plugin
7. Open the `HoloOD` subdirectory in Unity editor, make sure all HoloLens related settings are in place, open `Detecor` scene and build.