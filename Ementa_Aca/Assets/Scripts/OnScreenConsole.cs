using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OnScreenConsole : MonoBehaviour
{
    public TMP_Text consoleText;
    public int maxLines = 5;

    private Queue<string> logMessages = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logMessages.Enqueue(logString);
        if (logMessages.Count > maxLines)
        {
            logMessages.Dequeue();
        }

        consoleText.text = string.Join("\n", logMessages.ToArray());
    }
}
