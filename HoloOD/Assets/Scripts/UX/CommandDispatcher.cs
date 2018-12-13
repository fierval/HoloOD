using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using System;

enum ObjectNames : int
{
    StartDetection = 0,
    RestoreScene,
    ForgetScene
}

public class CommandDispatcher : InteractionReceiver
{
    ObjectNames GetObject(string objectName)
    {
        int idxSpace = objectName.IndexOf(' ');
        string normalizedName = objectName;
        if (idxSpace >= 0)
        {
            normalizedName = objectName.Remove(idxSpace);
        }

       return (ObjectNames) Enum.Parse(typeof(ObjectNames), normalizedName);
    }

    protected override void InputClicked(GameObject obj, InputClickedEventData eventData)
    {
        var activation = GetObject(obj.name);
        switch (activation)
        {
            case ObjectNames.StartDetection:
                ProjectionExample.Instance.StartDetection();
                break;
            case ObjectNames.RestoreScene:
                ProjectionExample.Instance.RestoreScene();
                break;
            case ObjectNames.ForgetScene:
                ProjectionExample.Instance.ForgetScene();
                break;
            default:
                break;
        }
    }
}
