using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;

public class HoloPicture : TapToPlace {

    public void ApplyCapture(byte[] data, HoloLensCameraStream.Resolution size, Matrix4x4 camera2WorldMatrix, Matrix4x4 projectionMatrix)
    {
        var pictureRenderer = gameObject.GetComponent<Renderer>() as Renderer;
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

    }
}
