using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeDisplay : MonoBehaviour
{
    public TMP_Text timeText;

    void Update()
    {
        timeText.text = System.DateTime.Now.ToString("HH:mm:ss");
    }
}
