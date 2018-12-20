using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;

public class TapToPlaceWithCamera : TapToPlace
{
    protected override void Update()
    {
        base.Update();
        if(!IsBeingPlaced)
        {
            return;
        }

        // Only if we are placing
        var renderer = gameObject.GetComponent<Renderer>();
        renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", Camera.main.cameraToWorldMatrix.inverse);
        renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", Camera.main.projectionMatrix);
    }

}
