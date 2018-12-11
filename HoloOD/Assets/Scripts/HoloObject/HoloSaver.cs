using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity;
using Newtonsoft.Json;
using System.IO;
#if UNITY_WSA && !UNITY_EDITOR
using Windows.Foundation;
using Rect = Windows.Foundation.Rect;
#endif

[JsonObject]
public class Holo
{
    public List<string> labels;
    public List<float> confidences;

    /// <summary>
    /// predicted rectangles. need to be converted to a windows structure for serialization
    /// </summary>
    [JsonIgnore]
    public List<UnityEngine.Rect> predictedRects;
    /// <summary>
    /// Same as predicted rectangles, but this one is serializeable
    /// </summary>
    public List<Rect> rects;

    public float[] cameraToWorldMatrix;
    public float[] projectionMatrix;

    [JsonIgnore]
    public byte [] image;

    //Vector3 - position of the quad
    public float x;
    public float y;
    public float z;
}

public class HoloSaver : Singleton<HoloSaver>
{
#if UNITY_WSA && !UNITY_EDITOR
    List<Rect> ConvertToWindowsRects(List<UnityEngine.Rect> predictedRects)
    {
        return predictedRects.Select(r => new Rect { X = r.xMin, Y = r.yMin, Height = r.height, Width = r.width}).ToList();
    }

    List<UnityEngine.Rect> ConvertToUnityRects(List<Rect> rects)
    {
        return rects.Select(r => new UnityEngine.Rect { xMin = (float) r.X, yMin = (float)r.Y, height = (float) r.Height, width = (float) r.Width }).ToList();
    }
#endif
    public void SaveHologram(Holo holo, string path)
    {
        try
        {
#if UNITY_WSA && !UNITY_EDITOR
            holo.rects = ConvertToWindowsRects(holo.predictedRects);
#endif
            string json = JsonConvert.SerializeObject(holo, Formatting.Indented);
            File.WriteAllText(path, json);

            // now the image
            string imagePath = Path.ChangeExtension(path, ".jpg");
            File.WriteAllBytes(imagePath, holo.image);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

    }

    public Holo RestoreHologram(string path)
    {
        string imagePath = Path.ChangeExtension(path, ".jpg");

        Holo holo = JsonConvert.DeserializeObject<Holo>(path);
#if UNITY_WSA && !UNITY_EDITOR
        holo.predictedRects = ConvertToUnityRects(holo.rects);
#endif
        holo.image = File.ReadAllBytes(imagePath);

        return holo;
    }
}


