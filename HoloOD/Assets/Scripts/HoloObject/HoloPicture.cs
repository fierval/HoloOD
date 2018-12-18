using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using Yolo;
using System.Linq;
using System.IO;

public class HoloPicture : TapToPlace
{

    public Matrix4x4 camera2WorldMatrix { get; private set; }
    public Matrix4x4 projectionMatrix { get; private set; }
    float[] camera2WorldFloat;
    float[] projectionFloat;
    public IEnumerable<YoloBoundingBox> Predictions { get; set; }
    /// <summary>
    /// When deserializing we need head position to draw lasers
    /// </summary>
    public Vector3 HeadPos { get; private set; }

    public HoloLensCameraStream.Resolution Resolution { get; private set; }

    const string FilePrefix = "Holo";

    public void ApplyCapture(byte[] data, HoloLensCameraStream.Resolution size, float[] camera2WorldFloat, float[] projectionFloat, bool setPostion = false)
    {
        this.camera2WorldFloat = camera2WorldFloat;
        this.projectionFloat = projectionFloat;

        this.camera2WorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(camera2WorldFloat);
        this.projectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(projectionFloat);

        // original scale is 0
        gameObject.transform.localScale = Vector3.one;

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

        if (setPostion)
        {
            SetPositionFromCamera();
        }

    }

    void SetPositionFromCamera()
    {
        Vector3 inverseNormal = -camera2WorldMatrix.GetColumn(2);

        // Position the canvas object slightly in front of the real world web camera.
        Vector3 position = camera2WorldMatrix.GetColumn(3) - camera2WorldMatrix.GetColumn(2);
        var rotation = Quaternion.LookRotation(inverseNormal, camera2WorldMatrix.GetColumn(1));

        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;

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

    public void RestoreHologram(string path)
    {
        var holo = HoloSaver.Instance.RestoreHologram(path);
        Resolution = new HoloLensCameraStream.Resolution(holo.width, holo.height);

        Vector3 position = new Vector3(holo.x, holo.y, holo.z);
        Quaternion rotation = new Quaternion(holo.qx, holo.qy, holo.qz, holo.qw);

        ApplyCapture(holo.image, Resolution, holo.cameraToWorldMatrix, holo.projectionMatrix);
        transform.position = position;
        transform.rotation = rotation;
        
        Predictions =
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

        HeadPos = new Vector3(holo.headX, holo.headY, holo.headZ);
    }
}
