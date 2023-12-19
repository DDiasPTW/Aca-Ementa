using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TabNavigation : MonoBehaviour
{
    public Selectable[] inputFields;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameObject current = EventSystem.current.currentSelectedGameObject;

            if (current != null)
            {
                for (int i = 0; i < inputFields.Length; i++)
                {
                    if (inputFields[i].gameObject == current)
                    {
                        int nextIndex = (i + 1) % inputFields.Length;
                        Selectable next = inputFields[nextIndex];

                        if (next != null)
                        {
                            InputField inputField = next.GetComponent<InputField>();
                            if (inputField != null) inputField.OnPointerClick(new PointerEventData(EventSystem.current));

                            EventSystem.current.SetSelectedGameObject(next.gameObject, new BaseEventData(EventSystem.current));
                            break;
                        }
                    }
                }
            }
        }
    }
}
