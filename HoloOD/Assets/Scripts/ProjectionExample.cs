using UnityEngine;
using UnityEngine.WSA;
using HoloLensCameraStream;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using Yolo;
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
public class ProjectionExample : Singleton<ProjectionExample>, IInputClickHandler
{
    object sync = new object();
    const string FilePrefix = "Holo";

    private HoloLensCameraStream.Resolution _resolution;
    private VideoCapture videoCapture = null;
    private IntPtr spatialCoordinateSystemPtr;
    private byte[] latestImageBytes;
    private bool stopVideo;

    GameObject Label;

    private PropertyInfo videoFrameInfo;

    private RaycastLaser laser;
    private CameraParameters cameraParams;
    private bool processingFrame = false;
    private int captureNum;

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
        captureNum = GetSceneFiles().Count / 2 + 1;

        // A tap in the air means start video capture
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    /// <summary>
    /// Start detection was clicked
    /// </summary>
    public void StartDetection()
    {
        if (videoCapture == null)
        {
            CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
        }
        StartCoroutine(StartOrStopVideoMode());
    }

    void Start()
    {
        //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
        spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();

        // Set the laser
        laser = RaycastLaser.Instance;

        Label = GameObject.FindGameObjectWithTag("DetectedObjects");

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

    // This coroutine will toggle the video on/off
    private IEnumerator StartOrStopVideoMode()
    {
        yield return new WaitForSeconds(0.65f);
        stopVideo = !stopVideo;

        if (!stopVideo)
        {
            processingFrame = false;
            OnVideoCaptureCreated(videoCapture);
        }
    }

    protected override void OnDestroy()
    {
        if (videoCapture == null)
            return;

        videoCapture.FrameSampleAcquired += null;
        videoCapture.Dispose();
        base.OnDestroy();
    }

    private void OnVideoCaptureCreated(VideoCapture v)
    {
        if (v == null)
        {
            Debug.LogError("No VideoCapture found");
            return;
        }
        if (videoCapture == null)
        {
            videoCapture = v;

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
        }

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
            if (!stopVideo || processingFrame)
            {
                return;
            }

            if (!processingFrame)
            {
                processingFrame = true;
            }
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

            Matrix4x4 camera2WorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(s.camera2WorldMatrix);
            Matrix4x4 projectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(s.projectionMatrix);

            //TODO: Do we need this fancy threading?

            // Stop the video and reproject the 5 pixels
            GameObject picture = null;

            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                picture = CreateHologram(s.data, _resolution, camera2WorldMatrix, projectionMatrix);

                Vector3 inverseNormal = -camera2WorldMatrix.GetColumn(2);

                // Position the canvas object slightly in front of the real world web camera.
                Vector3 imagePosition = camera2WorldMatrix.GetColumn(3) - camera2WorldMatrix.GetColumn(2);

                picture.transform.position = imagePosition;
                picture.transform.rotation = Quaternion.LookRotation(inverseNormal, camera2WorldMatrix.GetColumn(1));

            }, false);

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
                SaveHologram(picture, s.camera2WorldMatrix, s.projectionMatrix, predictions);

                DisplayPredictions(camera2WorldMatrix, projectionMatrix, picture, predictions, _resolution, Camera.main.transform.position);
            }, false);

        }
        finally
        {
            sample.Dispose();
        }
    }

    private void DisplayPredictions(Matrix4x4 camera2WorldMatrix, Matrix4x4 projectionMatrix, GameObject picture, IList<YoloBoundingBox> predictions, HoloLensCameraStream.Resolution size, Vector3 headPos)
    {
        var shootingDirections = GetRectCentersInWorldCoordinates(camera2WorldMatrix, projectionMatrix, size, predictions);

        // position text
        foreach (var labelConfidenceDirection in shootingDirections)
        {
            // decompose the tuple and get the goodies
            string labelText = labelConfidenceDirection.Item1;
            float confidence = labelConfidenceDirection.Item2;
            Vector3 direction = labelConfidenceDirection.Item3;

            // shoot the laser
            var label = Instantiate(Label).GetComponent<TextMesh>();

            label.text = $"{labelText}: {confidence: 0.00}";
            RaycastHit objHitInfo;

            label.transform.position = direction;

            if (Physics.Raycast(headPos, direction, out objHitInfo, 10.0f))
            {
                label.transform.position = objHitInfo.point;
            }

            label.transform.rotation = picture.transform.rotation;

            laser.shootLaser(headPos, direction, 10.0f, confidence, ObjectDetector.Instance.DetectionThreshold);
        }
    }

    /// <summary>
    /// Creates a quad hologram to display image capture
    /// </summary>
    /// <param name="data">Raw bytes of the image</param>
    /// <param name="camera2WorldMatrix">Camera -> World matrix</param>
    /// <param name="projectionMatrix"> Campera projection matrix</param>
    private GameObject CreateHologram(byte[] data, HoloLensCameraStream.Resolution size, Matrix4x4 camera2WorldMatrix, Matrix4x4 projectionMatrix)
    {
        GameObject picture = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var pictureRenderer = picture.GetComponent<Renderer>() as Renderer;
        pictureRenderer.material = new Material(Shader.Find("AR/HolographicImageBlend"));
        var pictureTexture = new Texture2D(size.width, size.height, TextureFormat.BGRA32, false);

        // Upload bytes to texture
        pictureTexture.LoadRawTextureData(data);
        pictureTexture.wrapMode = TextureWrapMode.Clamp;
        pictureTexture.Apply();

        // Set material parameters
        pictureRenderer.sharedMaterial.SetTexture("_MainTex", pictureTexture);
        pictureRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", camera2WorldMatrix.inverse);
        pictureRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
        pictureRenderer.sharedMaterial.SetFloat("_VignetteScale", 0f);
        return picture;
    }

    /// <summary>
    /// Serializes the current object with predictions
    /// </summary>
    /// <param name="picture"></param>
    /// <param name="predictions"></param>
    private void SaveHologram(GameObject picture, float[] camera2WorldMatrix, float[] projectionMatrix, IList<YoloBoundingBox> predictions)
    {
        var camPos = Camera.main.transform.position;

        var texture = picture.GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D;
        var holoObj = new Holo()
        {
            confidences = predictions.Select(p => p.Confidence).ToList(),
            labels = predictions.Select(p => p.Label).ToList(),
            predictedRects = predictions.Select(p => p.Rect).ToList(),
            image = latestImageBytes,
            cameraToWorldMatrix = camera2WorldMatrix,
            projectionMatrix = projectionMatrix,
            x = picture.transform.position.x,
            y = picture.transform.position.y,
            z = picture.transform.position.z,
            qx = picture.transform.rotation.x,
            qy = picture.transform.rotation.y,
            qz = picture.transform.rotation.z,
            qw = picture.transform.rotation.w,

            width = _resolution.width,
            height = _resolution.height,

            headX = camPos.x,
            headY = camPos.y,
            headZ = camPos.z
        };

        string path = Path.Combine(Application.persistentDataPath, $"{FilePrefix}{captureNum++}.json");

        HoloSaver.Instance.SaveHologram(holoObj, path);
    }

    List<string> GetSceneFiles()
    {
        return Directory.GetFiles(Application.persistentDataPath, $"{FilePrefix}*.*").ToList();
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
        var files = GetSceneFiles();
        files.ForEach(f => File.Delete(f));
    }

    private GameObject RestoreHologram(string path)
    {
        var holo = HoloSaver.Instance.RestoreHologram(path);
        Matrix4x4 projectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(holo.projectionMatrix);
        Matrix4x4 camera2WorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(holo.cameraToWorldMatrix);
        var resolution = new HoloLensCameraStream.Resolution(holo.width, holo.height);

        // restore the picture
        GameObject picture = CreateHologram(holo.image, resolution, camera2WorldMatrix, projectionMatrix);

        Vector3 picturePos = new Vector3(holo.x, holo.y, holo.z);
        picture.transform.position = picturePos;

        Quaternion pictureRotation = new Quaternion(holo.qx, holo.qy, holo.qz, holo.qw);
        picture.transform.rotation = pictureRotation;

        var predictions =
            Enumerable.Range(0, holo.predictedRects.Count)
            .Select(i => new YoloBoundingBox()
            {
                Label = holo.labels[i],
                Confidence = holo.confidences[i],
                X = holo.predictedRects[i].xMin,
                Y = holo.predictedRects[i].yMin,
                Width = holo.predictedRects[i].width,
                Height = holo.predictedRects[i].height,
            });

        DisplayPredictions(camera2WorldMatrix, projectionMatrix, picture, predictions.ToList(), resolution, new Vector3(holo.headX, holo.headY, holo.headZ));
        return picture;

    }

    /// <summary>
    /// Return a tuple of Label, Confidence, and point to shoot a ray in the world coordinate
    /// Of a detection
    /// </summary>
    /// <param name="camera2WorldMatrix">Camera-to-world matrix</param>
    /// <param name="projectionMatrix">Projection matrix</param>
    /// <param name="predictions">List of predictions</param>
    /// <returns></returns>
    private IEnumerable<Tuple<string, float, Vector3>> GetRectCentersInWorldCoordinates(Matrix4x4 camera2WorldMatrix, Matrix4x4 projectionMatrix, HoloLensCameraStream.Resolution size, IList<YoloBoundingBox> predictions)
    {

        foreach (var p in predictions)
        {
            var centerX = p.X + p.Width / 2;
            var centerY = p.Y + p.Height / 2;
            var direction = LocatableCameraUtils.PixelCoordToWorldCoord(camera2WorldMatrix, projectionMatrix, size, new Vector2(centerX, centerY));
            yield return new Tuple<string, float, Vector3>(p.Label, p.Confidence, direction);
        }
    }

    private void onVideoModeStopped(VideoCaptureResult result)
    {
        Debug.Log("Video Mode Stopped");
    }

    void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
    {
        StartDetection();
    }
}
