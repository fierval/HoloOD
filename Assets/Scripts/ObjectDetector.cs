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
    public static IList<YoloBoundingBox> Predictions { get; private set; }
    public static float DetectionThreshold { get; set; }
    public static int CameraWidth { get; set; }
    public static int CameraHeight { get; set; }


    // Model instantiation goes here
#if UNITY_WSA && !UNITY_EDITOR
#if SDK_1809
    static TinyYoloV2O12Model model;
    static readonly Uri modelFile = new Uri("ms-appx:///Data/StreamingAssets/Tiny-YOLOv2O12.onnx");
#else
    static TinyYoloV2O1Model model;
    static readonly Uri modelFile = new Uri("ms-appx:///Data/StreamingAssets/Tiny-YOLOv2.onnx");
#endif
    static YoloWinMlParser parser = new YoloWinMlParser();

    static ObjectDetector()
    {
        CameraWidth = CameraHeight = 0;
        LoadModel().Wait();
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

    /// <summary>
    /// Create VideoFrame used by Windows.AI as intput to the model
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="shouldResize">Should we resize it (default: false)</param>
    /// <param name="width">Pixel width of the target (default: 413)</param>
    /// <param name="height">Pixel height of the target (default: 413)</param>
    /// <returns></returns>
    public static async Task<VideoFrame> CreateFromJpegFile(string fileName, bool shouldResize = false, uint width = 413, uint height = 413)
    {
        SoftwareBitmap softwareBitmap;
        StorageFile imageFile = await StorageFile.GetFileFromPathAsync(fileName);
        var transform = new BitmapTransform() { ScaledWidth = width, ScaledHeight = height, InterpolationMode = BitmapInterpolationMode.Cubic };

        using (IRandomAccessStream stream = await imageFile.OpenReadAsync())
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            if (!shouldResize)
            {
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            }
            else
            {
                var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
                var pixels = pixelData.DetachPixelData();


                var wBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                await wBitmap.SetSourceAsync(stream);
                softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(wBitmap.PixelBuffer, BitmapPixelFormat.Bgra8, (int)width, (int)height, BitmapAlphaMode.Premultiplied);
            }
        }

        return VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
    }
#endif


#if UNITY_WSA && !UNITY_EDITOR
    public static async void AnalyzeImage(VideoFrame videoFrame)
    {
        // This appears to be the right way to handle background tasks.
        // We return to the main thread as fast as we can, and wait for the next call to the Update()
        // to advance our processing
#if SDK_1809
        TinyYoloV2O12Input input = new TinyYoloV2O12Input { image = ImageFeatureValue.CreateFromVideoFrame(videoFrame) };
#else
        TinyYoloV2O1ModelInput input = new TinyYoloV2O1ModelInput { image = videoFrame };
#endif
        var predictions = await model.EvaluateAsync(input).ConfigureAwait(false);
#if SDK_1809
        var boxes = parser.ParseOutputs(predictions.grid.GetAsVectorView().ToArray(), CameraWidth, CameraHeight, DetectionThreshold);
#else
        var boxes = parser.ParseOutputs(predictions.grid.ToArray(), CameraWidth, CameraHeight, DetectionThreshold);
#endif
        // normalize coordinates
        if (boxes.Count == 0)
        {
            Predictions = null;
        }

        boxes = parser.NonMaxSuppress(boxes);
        Predictions = boxes.Select(b => new YoloBoundingBox()
        {
            X = b.X / CameraWidth,
            Y = b.Y / CameraHeight,
            Width = b.Width / CameraWidth,
            Height = b.Height / CameraHeight,
            Confidence = b.Confidence,
            Label = b.Label
        }).ToList();
    }
#endif

}
