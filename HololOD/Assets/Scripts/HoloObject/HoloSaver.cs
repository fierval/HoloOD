using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity;
using Newtonsoft.Json;
using System.IO;

[JsonObject]
public class Holo
{
    public string [] labels;
    public float [] confidences;
    public Rect [] rects;
    public byte[] image;
}

public class HoloSaver : Singleton<HoloSaver>
{
    public void SaveHolograms(List<Holo> holos, string path)
    {
        string json = JsonConvert.SerializeObject(holos, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public List<Holo> RestoreHolograms(string path)
    {
        List<Holo> holograms = JsonConvert.DeserializeObject<List<Holo>>(path);
        return holograms;
    }
}


