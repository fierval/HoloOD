using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity;
using Yolo;
using System;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning;
#endif

public static class ObjectDetector
{
    public static float DetectionThreshold { get; set; }

#if UNITY_WSA && !UNITY_EDITOR

    public static IList<YoloBoundingBox> Predictions { get; private set; }
    

    // Model instantiation goes here
#if SDK_1809
    public static TinyYoloV2O12Model model;
    static readonly Uri modelFile = new Uri("ms-appx:///Data/StreamingAssets/Tiny-YOLOv2O12.onnx");
#else
    public static TinyYoloV2O1Model model;
    static readonly Uri modelFile = new Uri("ms-appx:///Data/StreamingAssets/Tiny-YOLOv2.onnx");
#endif
    static YoloWinMlParser parser = new YoloWinMlParser();

    static ObjectDetector()
    {
       
    }

    public static async Task<bool> LoadModel()
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


    public static async Task<IList<YoloBoundingBox>> AnalyzeImage(VideoFrame videoFrame)
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
        // normalize coordinates
        if (boxes.Count == 0)
        {
            Predictions = null;
        }

        boxes = parser.NonMaxSuppress(boxes);
        return boxes.Select(b => new YoloBoundingBox()
        {
            X = b.X / width,
            Y = b.Y / height,
            Width = b.Width / width,
            Height = b.Height / height,
            Confidence = b.Confidence,
            Label = b.Label
        }).ToList();
    }

    private static Tuple<int, int> GetDimensionsFromVideoFrame(VideoFrame videoFrame)
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
