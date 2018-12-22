using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderBetweenTwoPoints : MonoBehaviour {
    [SerializeField]
    private Transform cylinderPrefab;

    private GameObject leftSphere;
    private GameObject rightSphere;
    private GameObject cylinder;

    private void Start () {
        leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftSphere.transform.position = new Vector3(-1, 0, 0);
        rightSphere.transform.position = new Vector3(1, 0, 0);

        InstantiateCylinder(cylinderPrefab, leftSphere.transform.position, rightSphere.transform.position);
    }

    private void Update () {
        leftSphere.transform.position = new Vector3(-1, -2f * Mathf.Sin(Time.time), 0);
        rightSphere.transform.position = new Vector3(1, 2f * Mathf.Sin(Time.time), 0);

        UpdateCylinderPosition(cylinder, leftSphere.transform.position, rightSphere.transform.position);
    }

    private void InstantiateCylinder(Transform cylinderPrefab, Vector3 beginPoint, Vector3 endPoint)
    {
        cylinder = Instantiate<GameObject>(cylinderPrefab.gameObject, Vector3.zero, Quaternion.identity);
        UpdateCylinderPosition(cylinder, beginPoint, endPoint);
    }

    private void UpdateCylinderPosition(GameObject cylinder, Vector3 beginPoint, Vector3 endPoint)
    {
        Vector3 offset = endPoint - beginPoint;
        Vector3 position = beginPoint + (offset / 2.0f);

        cylinder.transform.position = position;
        cylinder.transform.LookAt(beginPoint);
        Vector3 localScale = cylinder.transform.localScale;
        localScale.z = (endPoint - beginPoint).magnitude;
        cylinder.transform.localScale = localScale;
    }
}
