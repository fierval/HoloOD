using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yolo;
using System;
using HoloToolkit.Unity;

#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning;
#endif

public  class ObjectDetector : Singleton<ObjectDetector>
{
    [Range(0f, .99f)]
    public float DetectionThreshold;

#if UNITY_WSA && !UNITY_EDITOR

    public  IList<YoloBoundingBox> Predictions { get; private set; }
    

    // Model instantiation goes here
#if SDK_1809
    public  TinyYoloV2O12Model model;
     readonly Uri modelFile = new Uri("ms-appx:///Data/StreamingAssets/Tiny-YOLOv2O12.onnx");
#else
    public  TinyYoloV2O1Model model;
     readonly Uri modelFile = new Uri("ms-appx:///Data/StreamingAssets/Tiny-YOLOv2.onnx");
#endif
     YoloWinMlParser parser = new YoloWinMlParser();

    public  async Task<bool> LoadModel()
    {
        try
        {
            StorageFile imageRecoModelFile = await StorageFile.GetFileFromApplicationUriAsync(modelFile);
            if (imageRecoModelFile == null)
            {
                Debug.Log("Could not read model file");
                return false;
            }
            
#if SDK_1809
            model = await TinyYoloV2O12Model.CreateFromStreamAsync(imageRecoModelFile);
#else
            model = await TinyYoloV2O1Model.CreateTinyYoloV2O1Model(imageRecoModelFile);
#endif
            if(model == null)
            {
                Debug.Log("Could not instantiate model");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Could not get file: {e.Message}");
            throw;
        }
        return true;
    }


    public  async Task<IList<YoloBoundingBox>> AnalyzeImage(VideoFrame videoFrame)
    {
        // This appears to be the right way to handle background tasks.
        // We return to the main thread as fast as we can, and wait for the next call to the Update()
        // to advance our processing
#if SDK_1809
        TinyYoloV2O12Input input = new TinyYoloV2O12Input { image = ImageFeatureValue.CreateFromVideoFrame(videoFrame) };
#else
        TinyYoloV2O1ModelInput input = new TinyYoloV2O1ModelInput { image = videoFrame };
#endif
        var dims = GetDimensionsFromVideoFrame(videoFrame);
        int width = dims.Item1;
        int height = dims.Item2;

        var predictions = await model.EvaluateAsync(input).ConfigureAwait(false);
#if SDK_1809
        var boxes = parser.ParseOutputs(predictions.grid.GetAsVectorView().ToArray(), width, height, DetectionThreshold);
#else
        var boxes = parser.ParseOutputs(predictions.grid.ToArray(), width, height, DetectionThreshold);
#endif
        boxes = boxes.Where(b => b.Confidence >= DetectionThreshold).ToList();

        // normalize coordinates
        boxes = parser.NonMaxSuppress(boxes);
        return boxes.ToList();
    }

    private  Tuple<int, int> GetDimensionsFromVideoFrame(VideoFrame videoFrame)
    {
        int width = 0, height =0;

        if (videoFrame.SoftwareBitmap != null)
        {
            width = videoFrame.SoftwareBitmap.PixelWidth;
            height = videoFrame.SoftwareBitmap.PixelHeight;
        }
        else if (videoFrame.Direct3DSurface != null)
        {
            width = videoFrame.Direct3DSurface.Description.Width;
            height = videoFrame.Direct3DSurface.Description.Height;
        }
        return new Tuple<int, int>(width, height);
    }
#endif

}
