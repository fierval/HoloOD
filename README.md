# On-Device Object Detection with Hololens

The application should be build with Windows 10 build 1809, October Creators update. The same update should be deployed on HoloLens.

## Requirements
1. Windows 10 build 1809 (Octobere 2018 Update)
2. Unity 2017.4.16
3. Visual Studio 2017, .NET 4.6
4. Mixed Reality Toolkit, version 2017.4.3.0

## Setup
1. Make sure your local git supports LFS (Large File Storage)
2. Clone this repository. If using Tortoise Git, check the LFS box.
3. Clone [Mixed Reality Toolkit](https://github.com/Microsoft/MixedRealityToolkit-Unity)
4. `git checkout 2017.4.3.0` from the Mixed Reality Toolkit directory
5. Copy `Assets\MixedRealityToolkit-SDK`, `Assets\MixedRealityToolkit-Examples`, `Assets\MixedRealityToolkit-Preview` to the `Assets` directory under `HoloOD\HoloOD`
6. Follow the instructions in [HoloLensCameraStream README](https://github.com/fierval/HoloOD/tree/master/HoloLensCameraStream) to build the camera capture plugin
7. Open the `HoloOD` subdirectory in Unity editor, make sure all HoloLens related settings are in place, open `Detecor` scene and build.

[ONNX v1.2 version](https://onnxzoo.blob.core.windows.net/models/opset_7/tiny_yolov2/tiny_yolov2.tar.gz) of the model used for this application is supported by Windows 10 version 1809, October Update and the corresponding SDK. Since the app supports the previous version of Windows 10 (1803), a macro SDK_1809 is defined under PlayerSettings -> OtherSettings -> Scripting 
