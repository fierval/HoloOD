using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using System;

public enum ObjectNames : int
{
    HideToolbar = 0,
    ShowToolbar,
    RestoreScene,
    ForgetScene
}

public class CommandDispatcher : InteractionReceiver, ISpeechHandler
{
    ObjectNames GetObject(string objectName)
    {
        int idxSpace = objectName.IndexOf(' ');
        string normalizedName = objectName;
        if (idxSpace >= 0)
        {
            normalizedName = objectName.Remove(idxSpace, 1);
        }

        return (ObjectNames)Enum.Parse(typeof(ObjectNames), normalizedName);
    }

    protected override void InputClicked(GameObject obj, InputClickedEventData eventData)
    {
        if (obj == null)
        {
            return;
        }

        var activation = GetObject(obj.name);
        ExecCommand(activation);
    }

    void ToggleToolbar(bool state)
    {
        var go = GameObject.FindGameObjectWithTag("MainToolBar");
        go.GetComponentsInChildren<Renderer>().Where(c => c != null).ToList().ForEach(c => c.enabled = state);
    }

    public void ExecCommand(ObjectNames action)
    {
        switch (action)
        {
            case ObjectNames.HideToolbar:
                ToggleToolbar(false);
                break;

            case ObjectNames.ShowToolbar:
                ToggleToolbar(true);
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

    void ISpeechHandler.OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        var action = GetObject(eventData.RecognizedText);

        ExecCommand(action);
    }
}
