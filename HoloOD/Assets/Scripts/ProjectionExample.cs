using UnityEngine;
using UnityEngine.WSA;
using HoloLensCameraStream;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.IO;
using Application = UnityEngine.Application;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Media.Capture.Frames;
using Windows.Media;
using Windows.Graphics.Imaging;
#endif

/// <summary>
/// In this example, we back-project to the 3D world 5 pixels, which are the principal point and the image corners,
/// using the extrinsic parameters and projection matrices.
/// Whereas the app is running, if you tap on the image, this set of points is reprojected into the world.
/// </summary>
public class ProjectionExample : Singleton<ProjectionExample>
{
    object sync = new object();
    private HoloLensCameraStream.Resolution _resolution;
    private VideoCapture videoCapture = null;
    private IntPtr spatialCoordinateSystemPtr;
    private byte[] latestImageBytes;

    private PropertyInfo videoFrameInfo;

    private RaycastLaser laser;
    private CameraParameters cameraParams;
    private bool processingFrame = false;
    private int captureNum;

    private List<Tuple<HoloPicture, List<LineRenderer>, List<GameObject>>> pictureCollection = new List<Tuple<HoloPicture, List<LineRenderer>, List<GameObject>>>();
    // This struct store frame related data
    private class SampleStruct
    {
        public float[] camera2WorldMatrix, projectionMatrix;
        public byte[] data;
    }

    protected override void Awake()
    {
        base.Awake();

        videoFrameInfo = typeof(VideoCaptureSample).GetTypeInfo().DeclaredProperties.Where(x => x.Name == "videoFrame").Single();
        captureNum = GetSceneFiles().Count() / 2 + 1;

        // A tap in the air means start video capture
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    /// <summary>
    /// Start detection was clicked.
    /// This will create a new video capture every time
    /// </summary>
    public void StartDetection()
    {
        CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
    }

    void Start()
    {
        //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
        spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();

#if UNITY_WSA && !UNITY_EDITOR
        LoadModel();
#endif

    }

#if UNITY_WSA && !UNITY_EDITOR
    async void LoadModel()
    {
        if (!await ObjectDetector.Instance.LoadModel() || ObjectDetector.Instance.model == null)
        {
            throw new ApplicationException("could not load model");
        }
    }
#endif

    protected override void OnDestroy()
    {
        if (videoCapture == null)
            return;

        videoCapture.FrameSampleAcquired += null;
        videoCapture.Dispose();
        base.OnDestroy();
    }

    // Cannot be called on multiple threads!
    private void OnVideoCaptureCreated(VideoCapture v)
    {
        if (v == null)
        {
            Debug.LogError("No VideoCapture found");
            return;
        }

        videoCapture = v;
        processingFrame = false;

        //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(spatialCoordinateSystemPtr);

        _resolution = CameraStreamHelper.Instance.GetLowestResolution();
        float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);

        videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

        cameraParams = new CameraParameters();
        cameraParams.cameraResolutionHeight = _resolution.height;
        cameraParams.cameraResolutionWidth = _resolution.width;
        cameraParams.frameRate = Mathf.RoundToInt(frameRate);
        cameraParams.pixelFormat = CapturePixelFormat.BGRA32;

        videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);
    }

    private void OnVideoModeStarted(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            Debug.LogWarning("Could not start video mode.");
            return;
        }

        Debug.Log("Video capture started.");
    }

#if UNITY_WSA && !UNITY_EDITOR
    private async void OnFrameSampleAcquired(VideoCaptureSample sample)
#else
    private void OnFrameSampleAcquired(VideoCaptureSample sample)
#endif
    {
        lock (sync)
        {
            if (processingFrame)
            {
                return;
            }

            processingFrame = true;
        }

        // surrounded with try/finally because we need to dispose of the sample
        try
        {
            // Allocate byteBuffer
            if (latestImageBytes == null || latestImageBytes.Length < sample.dataLength)
                latestImageBytes = new byte[sample.dataLength];

            // Fill frame struct 
            SampleStruct s = new SampleStruct();
            sample.CopyRawImageDataIntoBuffer(latestImageBytes);
            s.data = latestImageBytes;

            // Get the cameraToWorldMatrix and projectionMatrix
            if (!sample.TryGetCameraToWorldMatrix(out s.camera2WorldMatrix) || !sample.TryGetProjectionMatrix(out s.projectionMatrix))
                return;

            HoloPicture picture = null;

            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                picture = HoloPicture.CreateHologram(s.data, _resolution, s.camera2WorldMatrix, s.projectionMatrix);
            }, true);


            videoCapture.StopVideoModeAsync(onVideoModeStopped);
            IList<Yolo.YoloBoundingBox> predictions = null;

#if UNITY_WSA && !UNITY_EDITOR
            VideoFrame videoFrame = (VideoFrame)videoFrameInfo.GetValue(sample);

            if (videoFrame?.SoftwareBitmap == null)
            {
                return;
            }
            SoftwareBitmap softwareBitmap = videoFrame.SoftwareBitmap;

            if (softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                videoFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
            }

            predictions = await ObjectDetector.Instance.AnalyzeImage(videoFrame);
            if (predictions?.Count == 0)
            {
                return;
            }

#endif
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                picture.Predictions = predictions;
                SaveHologram(picture);
                picture.DisplayPredictions();
            }, true);

        }
        finally
        {
            sample.Dispose();
        }
    }

    /// <summary>
    /// Serializes the current object with predictions
    /// </summary>
    /// <param name="predictions"></param>
    private void SaveHologram(HoloPicture picture)
    {
        picture.SaveHologram(captureNum++);
    }

    IEnumerable<string> GetSceneFiles()
    {
        return Directory.GetFiles(Application.persistentDataPath, $"{HoloPicture.FilePrefix}*.*");
    }

    public void RestoreScene()
    {
        var files = GetSceneFiles().Where(p => Path.GetExtension(p) == ".json").ToList();
        files.ForEach(f => RestoreHologram(f));
    }

    /// <summary>
    /// Simply delete all stored files
    /// </summary>
    public void ForgetScene()
    {
        var files = GetSceneFiles().ToList();
        files.ForEach(f => File.Delete(f));
        pictureCollection.ForEach(p => {
            p.Item1.gameObject.SetActive(false);
            p.Item2.ForEach(lr => lr.enabled = false);
            p.Item3.ForEach(l => l.SetActive(false));
        });

        pictureCollection.Clear();
        captureNum = 0;
    }

    private HoloPicture RestoreHologram(string path)
    {
        var picture = HoloPicture.RestoreHologram(path);

        picture.DisplayPredictions();

        return picture;
    }

    private void onVideoModeStopped(VideoCaptureResult result)
    {
        CameraStreamHelper.Instance.CloseVideoCapture();
        videoCapture = null;

        Debug.Log("Video Mode Stopped");
    }
}
