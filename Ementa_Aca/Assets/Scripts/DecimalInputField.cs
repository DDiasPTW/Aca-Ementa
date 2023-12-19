using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using System.Linq;

[RequireComponent(typeof(TMP_InputField))]
public class DecimalInputField : MonoBehaviour
{
    public TMP_InputField inputField;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(string value)
    {
        // Replace periods with commas
        string newValue = value.Replace('.', ',');

        // Use regular expression to remove non-numeric characters (except period)
        newValue = Regex.Replace(newValue, @"[^0-9,]", "");

        // Ensure only one decimal point is present
        int decimalPointCount = newValue.Count(f => f == ',');
        while (decimalPointCount > 1)
        {
            int lastDecimalPointIndex = newValue.LastIndexOf(',');
            newValue = newValue.Remove(lastDecimalPointIndex, 1);
            decimalPointCount--;
        }


        // Update the input field text only if it has changed
        if (newValue != value)
        {
            inputField.text = newValue;
        }
    }
}