using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using Yolo;
using System.Linq;
using System.IO;
using System;
using HoloToolkit.Unity.InputModule;

public class HoloPicture : MonoBehaviour
{
    public Matrix4x4 camera2WorldMatrix { get; private set; }
    public Matrix4x4 projectionMatrix { get; private set; }

    // containers for rendered text and lasers
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    List<TextMesh> labels = new List<TextMesh>();

    float[] camera2WorldFloat;
    float[] projectionFloat;
    public IEnumerable<YoloBoundingBox> Predictions { get; set; }
    /// <summary>
    /// When deserializing we need head position to draw lasers
    /// </summary>
    public Vector3 HeadPos { get; private set; }

    public HoloLensCameraStream.Resolution Resolution { get; private set; }

    public const string FilePrefix = "Holo";
    private const string VideoCaptureTag = "Video Capture";

    GameObject Label;
    RaycastLaser laser;

    // when we tap to place, StartPlacing() sets the raycast layer to "Ignore Raycast"
    private int captureLayer;

    /// <summary>
    /// Is the object being placed now
    /// </summary>
    protected bool IsBeingPlaced
    {
        get
        {
            return gameObject.GetComponent<TapToPlaceWithCamera>().IsBeingPlaced;
        }
    }

    protected static HoloPicture CreateHologram(byte[] data, HoloLensCameraStream.Resolution size,
        float[] camera2WorldFloat, float[] projectionFloat, Tuple<Vector3, Quaternion> positionRotation)
    {
        var pos = positionRotation.Item1;
        var rotation = positionRotation.Item2;

        var picObj = GameObject.FindGameObjectWithTag(VideoCaptureTag).GetComponent<HoloPicture>();
        var picture = Instantiate(picObj, pos, rotation);
        picture.ApplyCapture(data, size, camera2WorldFloat, projectionFloat);

        return picture;

    }

    /// <summary>
    /// Creates a quad hologram to display image capture. Positions it in front of the camera
    /// </summary>
    /// <param name="data">Raw bytes of the image</param>
    /// <param name="camera2WorldMatrix">Camera -> World matrix</param>
    /// <param name="projectionMatrix"> Campera projection matrix</param>
    public static HoloPicture CreateHologram(byte[] data, HoloLensCameraStream.Resolution size, float[] camera2WorldFloat, float[] projectionFloat)
    {
        var positionRotation = GetPositionFromCamera(camera2WorldFloat);
        return CreateHologram(data, size, camera2WorldFloat, projectionFloat, positionRotation);
    }

    void Start()
    {
        captureLayer = 1 << LayerMask.NameToLayer("Ignore Raycast") | 1 << LayerMask.NameToLayer("Capture");
        laser = RaycastLaser.Instance;
        Label = GameObject.FindGameObjectWithTag("DetectedObjects");
        Predictions = new List<YoloBoundingBox>();
    }

    void Update()
    {
        if (!IsBeingPlaced)
        {
            return;
        }

        camera2WorldMatrix = Camera.main.cameraToWorldMatrix;
        projectionMatrix = Camera.main.projectionMatrix;
        HeadPos = Camera.main.transform.position;
        DisplayPredictions();
    }

    void ApplyCapture(byte[] data, HoloLensCameraStream.Resolution size, float[] camera2WorldFloat, float[] projectionFloat, bool setPostion = false)
    {
        this.camera2WorldFloat = camera2WorldFloat;
        this.projectionFloat = projectionFloat;

        this.camera2WorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(camera2WorldFloat);
        this.projectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(projectionFloat);

        var pictureRenderer = gameObject.GetComponent<Renderer>();
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

        this.Resolution = new HoloLensCameraStream.Resolution(pictureTexture.width, pictureTexture.height);
        this.HeadPos = Camera.main.transform.position;

        // time to enable tap-to-place
        pictureRenderer.enabled = true;
    }

    static Tuple<Vector3, Quaternion> GetPositionFromCamera(float[] camera2WorldFloat)
    {
        Matrix4x4 camera2WorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(camera2WorldFloat);

        Vector3 inverseNormal = -camera2WorldMatrix.GetColumn(2);

        // Position the canvas object slightly in front of the real world web camera.
        Vector3 position = camera2WorldMatrix.GetColumn(3) - camera2WorldMatrix.GetColumn(2);
        var rotation = Quaternion.LookRotation(inverseNormal, camera2WorldMatrix.GetColumn(1));

        return new Tuple<Vector3, Quaternion>(position, rotation);
    }

    /// <summary>
    /// Serialize the current object to storage
    /// </summary>
    /// <param name="predictions">predictions</param>
    /// <param name="captureNum">integer to make the file unique</param>
    public void SaveHologram(int captureNum)
    {
        var texture = GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex") as Texture2D;
        var imageBytes = texture.GetRawTextureData();

        var holoObj = new Holo()
        {
            confidences = Predictions.Select(p => p.Confidence).ToList(),
            labels = Predictions.Select(p => p.Label).ToList(),
            predictedRects = Predictions.Select(p => p.Rect).ToList(),
            image = imageBytes,
            cameraToWorldMatrix = camera2WorldFloat,
            projectionMatrix = projectionFloat,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            qx = transform.rotation.x,
            qy = transform.rotation.y,
            qz = transform.rotation.z,
            qw = transform.rotation.w,

            width = texture.width,
            height = texture.height,

            headX = HeadPos.x,
            headY = HeadPos.y,
            headZ = HeadPos.z
        };

        string path = Path.Combine(Application.persistentDataPath, $"{FilePrefix}{captureNum++}.json");

        HoloSaver.Instance.SaveHologram(holoObj, path);
    }

    public static HoloPicture RestoreHologram(string path)
    {
        var holo = HoloSaver.Instance.RestoreHologram(path);
        var resolution = new HoloLensCameraStream.Resolution(holo.width, holo.height);

        Vector3 position = new Vector3(holo.x, holo.y, holo.z);
        Quaternion rotation = new Quaternion(holo.qx, holo.qy, holo.qz, holo.qw);

        var picture = CreateHologram(holo.image, resolution, holo.cameraToWorldMatrix, holo.projectionMatrix);

        picture.Predictions =
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

        picture.HeadPos = new Vector3(holo.headX, holo.headY, holo.headZ);
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
    private IEnumerable<Tuple<string, float, Vector3>> GetRectCentersInWorldCoordinates()
    {

        foreach (var p in Predictions)
        {
            var centerX = p.X + p.Width / 2;
            var centerY = p.Y + p.Height / 2;
            var direction = LocatableCameraUtils.PixelCoordToWorldCoord(camera2WorldMatrix, projectionMatrix, Resolution, new Vector2(centerX, centerY));
            yield return new Tuple<string, float, Vector3>(p.Label, p.Confidence, direction);
        }
    }

    void DestroyPredictionVisuals()
    {
        lineRenderers.Zip(labels, (line, label) => new { line, label })
            .ToList()
            .ForEach(o =>
            {
                DestroyImmediate(o.label);
                DestroyImmediate(o.line);
            });

        lineRenderers.Clear();
        labels.Clear();
    }

    public void DisplayPredictions()
    {
        var shootingDirections = GetRectCentersInWorldCoordinates();

        DestroyPredictionVisuals();

        // position text
        foreach (var labelConfidenceDirection in shootingDirections)
        {
            // decompose the tuple and get the goodies
            string labelText = labelConfidenceDirection.Item1;
            float confidence = labelConfidenceDirection.Item2;
            Vector3 direction = labelConfidenceDirection.Item3;

            // shoot the laser
            var labelParent = Instantiate(Label);
            var label = labelParent.GetComponent<TextMesh>();

            label.text = $"{labelText}: {confidence: 0.00}";
            RaycastHit objHitInfo;

            label.transform.position = direction;
            var distance = 10f; /* Vector3.Distance(HeadPos, direction) */
            
            if (Physics.Raycast(HeadPos, direction, out objHitInfo, distance, captureLayer))
            {
                label.transform.position = objHitInfo.point;
                Debug.Log("Raycast hit for the label");
            }

            label.transform.rotation = gameObject.transform.rotation;

            var lr = laser.shootLaser(HeadPos, direction, distance, confidence, ObjectDetector.Instance.DetectionThreshold, captureLayer);

            lineRenderers.Add(lr);
            labels.Add(label);
        }
    }
}
