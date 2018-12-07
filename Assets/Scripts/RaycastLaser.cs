using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class RaycastLaser : Singleton<RaycastLaser> {

    public float _lineWidthMultiplier = 0.05f;
    public Material _laserMaterial;
    public bool useRayCast = true;

    public void shootLaserFrom(Vector3 from, Vector3 direction, float length, Material mat=null)
    {
        LineRenderer lr = new GameObject().AddComponent<LineRenderer>(); lr.widthMultiplier = _lineWidthMultiplier;
        // Set Material
        lr.material = mat == null ? _laserMaterial : mat;
        lr.positionCount = 2;

        Ray ray = new Ray(from, direction);
        Vector3 to = from + length * direction;

        // Use this code when hit on mesh surface
        if (useRayCast)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, length))
            {
                to = hit.point;
            }
        }

        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }
}
