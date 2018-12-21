using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using System;
using HoloToolkit.Unity;

public enum ActionCommands : int
{
    ToggleToolbar = 0,
    RestoreScene,
    ForgetScene,
    AnalyzeScene
}

public class CommandDispatcher : InteractionReceiver, ISpeechHandler
{
    float toolbarMinDegrees = 180f;
    float toolbarMaxDegrees = 180f;

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

    void ToggleToolbar()
    {
        var go = GameObject.FindGameObjectWithTag("MainToolBar");
        var solver = go.GetComponent<SolverRadialView>();

        float temp = solver.MinViewDegrees;
        solver.MinViewDegrees = toolbarMinDegrees;
        toolbarMinDegrees = temp;

        temp = solver.MaxDistance;
        solver.MaxDistance = toolbarMaxDegrees;
        toolbarMaxDegrees = temp;

        solver.SolverUpdate();
    }

    public void ExecCommand(ActionCommands action)
    {
        switch (action)
        {
            case ActionCommands.ToggleToolbar:
                ToggleToolbar();
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
