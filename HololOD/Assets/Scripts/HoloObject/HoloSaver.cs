using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity;
using Newtonsoft.Json;
using System.IO;
using Windows.Foundation;
using Rect = Windows.Foundation.Rect;

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

    [JsonIgnore]
    public byte[] image;

    //Vector3 - position of the quad
    public float x;
    public float y;
    public float z;
}

public class HoloSaver : Singleton<HoloSaver>
{
    List<Rect> ConvertRects(List<UnityEngine.Rect> predictedRects)
    {
        return predictedRects.Select(r => new Rect { X = r.xMin, Y = r.yMin, Height = r.height, Width = r.width}).ToList();
    }
    public void SaveHologram(Holo holo, string path)
    {
        try
        {
            holo.rects = ConvertRects(holo.predictedRects);
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

    public List<Holo> RestoreHolograms(string path)
    {
        List<Holo> holograms = JsonConvert.DeserializeObject<List<Holo>>(path);
        return holograms;
    }
}


