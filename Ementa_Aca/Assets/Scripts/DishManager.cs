using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DishManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject addDishPanel;  // Reference to the "Add Dish" panel
    public TMP_InputField dishNameInput;
    public TMP_InputField halfDosePriceInput;
    public TMP_InputField fullDosePriceInput;
    public TMP_Dropdown categoryDropdown;
    public GameObject dishPrefab;  // Your "Prato" prefab
    public Transform dishesContainer;  // This should be the transform of your Vertical Layout object
    private List<GameObject> dishUIObjects = new List<GameObject>();

    [Header("Editing References")]
    public Button editButton;  // Assign this in the Unity Inspector to your "Editar" button
    public Button saveButton;  // Assign this in the Unity Inspector to your "Save" button
    private bool isInEditMode = false;
    private List<Dish> dishes = new List<Dish>();


    private string dataPath;

    void Start()
    {
        dataPath = Application.persistentDataPath + "/dishes.txt";
        LoadDishesFromFile();
        editButton.onClick.AddListener(ToggleEditMode);
        saveButton.onClick.AddListener(SaveChanges);
        saveButton.gameObject.SetActive(false);
    }

    public void ToggleAddDishPanel()
    {
        // This function toggles the visibility of the "Add Dish" panel
        addDishPanel.SetActive(!addDishPanel.activeSelf);
    }

    public void CreateDish()
    {
        Dish newDish = new Dish
        {
        name = dishNameInput.text,
        category = categoryDropdown.options[categoryDropdown.value].text,
        priceHalfServing = float.Parse(halfDosePriceInput.text),
        priceFullServing = float.Parse(fullDosePriceInput.text),
        isSoldOut = false
        };
        dishes.Add(newDish);

        SaveDishesToFile();
        InstantiateDishUI(newDish);

        // Clear the input fields after adding a new dish
        dishNameInput.text = "";
        categoryDropdown.value = 0;
        halfDosePriceInput.text = "";
        fullDosePriceInput.text = "";;
    }

    private void ToggleEditMode()
    {
        isInEditMode = !isInEditMode; // Toggle edit mode
        saveButton.gameObject.SetActive(isInEditMode);

        foreach(GameObject dish in dishUIObjects)
        {
            TMP_InputField nameField = dish.transform.GetChild(1).GetComponent<TMP_InputField>();
            TMP_Dropdown categoryDropdown = dish.transform.GetChild(2).GetComponent<TMP_Dropdown>();
            TMP_InputField halfDoseField = dish.transform.GetChild(3).GetComponent<TMP_InputField>();
            TMP_InputField fullDoseField = dish.transform.GetChild(4).GetComponent<TMP_InputField>();

            nameField.interactable = isInEditMode;
            categoryDropdown.interactable = isInEditMode;
            halfDoseField.interactable = isInEditMode;
            fullDoseField.interactable = isInEditMode;
        }
    }

    public void SaveChanges()
    {
        isInEditMode = false;

        dishes.Clear(); // Clear the current list of dishes

        foreach (GameObject dish in dishUIObjects)
        {
            TMP_InputField nameField = dish.transform.GetChild(1).GetComponent<TMP_InputField>();
            TMP_Dropdown categoryDropdown = dish.transform.GetChild(2).GetComponent<TMP_Dropdown>();
            TMP_InputField halfDoseField = dish.transform.GetChild(3).GetComponent<TMP_InputField>();
            TMP_InputField fullDoseField = dish.transform.GetChild(4).GetComponent<TMP_InputField>();
            Toggle soldOutToggle = dish.transform.GetChild(5).GetComponent<Toggle>();

            // Update the dish list with the latest UI values
            Dish updatedDish = new Dish
            {
                name = nameField.text,
                category = categoryDropdown.options[categoryDropdown.value].text,
                priceHalfServing = float.Parse(halfDoseField.text),
                priceFullServing = float.Parse(fullDoseField.text),
                isSoldOut = soldOutToggle.isOn
            };
            dishes.Add(updatedDish);

            // Make input fields non-interactable after saving
            nameField.interactable = isInEditMode;
            categoryDropdown.interactable = isInEditMode;
            halfDoseField.interactable = isInEditMode;
            fullDoseField.interactable = isInEditMode;
        }

        // Save the updated dishes to a file
        SaveDishesToFile();

        saveButton.gameObject.SetActive(false);
    }

    public void SaveDishesToFile()
    {
        string path = Application.persistentDataPath + "/dishes.txt";
        string json = JsonUtility.ToJson(new SerializableDishList { dishes = dishes }, true); // Convert the list of dishes to JSON
        File.WriteAllText(path, json); // Write the JSON to the file
    }

    public void LoadDishesFromFile()
    {
        string path = Application.persistentDataPath + "/dishes.txt";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);

            foreach (Dish dish in loadedDishes.dishes)
            {
                // Instantiate the dish UI and set the properties
                InstantiateDishUI(dish);
            }
        }
    }

    private void InstantiateDishUI(Dish dish)
    {
        // Create new dish instance in the UI
        GameObject newDishUI = Instantiate(dishPrefab, dishesContainer);
        dishUIObjects.Add(newDishUI);

        // Set properties for the new dish instance
        TMP_InputField nameField = newDishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
        TMP_Dropdown categoryDropdown = newDishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
        TMP_InputField halfDoseField = newDishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
        TMP_InputField fullDoseField = newDishUI.transform.GetChild(4).GetComponent<TMP_InputField>();
        Toggle soldOutToggle = newDishUI.transform.GetChild(5).GetComponent<Toggle>();
        Button deleteButton = newDishUI.transform.GetChild(6).GetComponent<Button>();

        // Populate the UI with the dish data
        nameField.text = dish.name;
        categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == dish.category); 
        halfDoseField.text = dish.priceHalfServing.ToString();
        fullDoseField.text = dish.priceFullServing.ToString();
        soldOutToggle.isOn = dish.isSoldOut;

        // Disable input for the created dish details (they will only be editable after hitting "Editar")
        nameField.interactable = false;
        categoryDropdown.interactable = false;
        halfDoseField.interactable = false;
        fullDoseField.interactable = false;

        // Add delete button functionality
        deleteButton.onClick.AddListener(() =>
        {
            DeleteDish(newDishUI);
        });
    }

    public void DeleteDish(GameObject dish)
    {
        int indexToRemove = dishUIObjects.IndexOf(dish);

        if (indexToRemove >= 0 && indexToRemove < dishes.Count)
        {
        dishes.RemoveAt(indexToRemove);
        SaveDishesToFile();
        }

        dishUIObjects.Remove(dish);
        Destroy(dish);
    }
}

[System.Serializable]
public class Dish
{
    public string name;
    public string category;
    public float priceHalfServing;
    public float priceFullServing;
    public bool isSoldOut;
}

[System.Serializable]
public class SerializableList<T>
{
    public List<T> items;
}

[System.Serializable]
public class SerializableDishList
{
    public List<Dish> dishes;
}

