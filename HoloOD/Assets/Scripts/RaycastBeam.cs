using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class RaycastBeam : Singleton<RaycastBeam> {

    VolumetricLaserBeam beam;
    private Color[] colors = new Color[4] { Color.red, Color.yellow, Color.blue, Color.green };

    [Range(1, 8)]
    public float beamSize = 1f;

    // Use this for initialization
    protected override void Awake()
    {
        base.Awake();
        beam.PreAlloc(gameObject.transform, 50);
    }

    public void shootLaser(Vector3 from, Vector3 direction, float length, float confidence, float minDetection)
    {
        //LineRenderer lr = new GameObject().AddComponent<LineRenderer>(); lr.widthMultiplier = _lineWidthMultiplier;
        // Set Material
        var color = GetLaserMaterial(confidence, minDetection);

        Ray ray = new Ray(from, direction);
        Vector3 to = from + length * direction;

        // Use this code when hit on mesh surface
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, length))
        {
            to = hit.point;
        }

        beam.GenerateBeam(Camera.main, from, to, color, beamSize);
    }

    Color GetLaserMaterial(float confidence, float minDetection)
    {
        int idx = (int)((confidence - minDetection) * colors.Length * 2);
        return colors[idx < 4 ? idx : 3];
    }


}
