using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using System;

public enum ActionCommands : int
{
    HideToolbar = 0,
    ShowToolbar,
    RestoreScene,
    ForgetScene,
    AnalyzeScene
}

public class CommandDispatcher : InteractionReceiver, ISpeechHandler
{
    ActionCommands GetObject(string objectName)
    {
        int idxSpace = objectName.IndexOf(' ');
        string normalizedName = objectName;
        if (idxSpace >= 0)
        {
            normalizedName = objectName.Remove(idxSpace, 1);
        }

        return (ActionCommands)Enum.Parse(typeof(ActionCommands), normalizedName);
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

    public void ExecCommand(ActionCommands action)
    {
        switch (action)
        {
            case ActionCommands.HideToolbar:
                ToggleToolbar(false);
                break;

            case ActionCommands.ShowToolbar:
                ToggleToolbar(true);
                break;
            case ActionCommands.RestoreScene:
                ProjectionExample.Instance.RestoreScene();
                break;
            case ActionCommands.ForgetScene:
                ProjectionExample.Instance.ForgetScene();
                break;
            case ActionCommands.AnalyzeScene:
                ProjectionExample.Instance.StartDetection();
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
