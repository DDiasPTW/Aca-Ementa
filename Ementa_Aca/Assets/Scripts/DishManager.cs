using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System;

public class DishManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject addDishPanel; 
    public TMP_InputField dishNameInput;
    public TMP_InputField halfDosePriceInput;
    public TMP_InputField fullDosePriceInput;
    public TMP_Dropdown categoryDropdown;
    public GameObject dishPrefab; 
    public Transform dishesContainer;  
    private List<GameObject> dishUIObjects = new List<GameObject>();
    public Button createButton;
    [Header("Editing References")]
    public Button editButton; 
    public Button saveButton;  
    private bool isInEditMode = false;
    private List<Dish> dishes = new List<Dish>();

    [Header("Saving")]
    private string dataPath;

    [Header("Other")]
    //ordering
    public Button activeTitleButton;
    public Button nameButton;
    //searching
    public TMP_InputField searchInputField;
    private Coroutine searchCoroutine; //add some delay
    private string currentSearchQuery = "";

    void Start()
    {
        dataPath = Application.persistentDataPath + "/Pratos.txt";
        LoadDishesFromFile();
        createButton.onClick.AddListener(CreateDish);
        //editing and saving
        editButton.onClick.AddListener(ToggleEditMode);
        saveButton.onClick.AddListener(SaveChanges);
        saveButton.gameObject.SetActive(false);
        //reordering
        activeTitleButton.onClick.AddListener(SortByActiveStatus);
        nameButton.onClick.AddListener(SortByAlpha);
        currentNameSortState = AlphaSortState.Alpha;
        //searching
        searchInputField.onValueChanged.AddListener(delegate { StartDelayedSearch(); });
    }

    #region Creating/Loading/Deleting
    public void ToggleAddDishPanel()
    {
        // This function toggles the visibility of the "Add Dish" panel
        addDishPanel.SetActive(!addDishPanel.activeSelf);
    }
    public void CreateDish()
    {
        // Check if the dish name is empty
        if (string.IsNullOrWhiteSpace(dishNameInput.text))
        {
            return;
        }

        // Try to parse the half dose price
        //float.TryParse(halfDosePriceInput.text, out float halfDosePrice);
        if (float.TryParse(halfDosePriceInput.text, out float halfDosePrice))
        {
            halfDosePrice = (float)Math.Round(halfDosePrice, 2); // Round to 2 decimal places
        }
        // Try to parse the full dose price
        //float.TryParse(fullDosePriceInput.text, out float fullDosePrice);
        if (float.TryParse(fullDosePriceInput.text, out float fullDosePrice))
        {
            fullDosePrice = (float)Math.Round(fullDosePrice, 2); // Round to 2 decimal places
        }



        Dish newDish = new Dish
        {
        id = Guid.NewGuid().ToString(),
        nome = dishNameInput.text,
        categoria = categoryDropdown.options[categoryDropdown.value].text,
        precoMeia = halfDosePrice,
        precoDose = fullDosePrice,
        Esgotado = false,
        isAtivo = false,
        novidade = false,
        naHora = false
        };
        dishes.Add(newDish);

        SaveDishesToFile();
        InstantiateDishUI(newDish);

        // Clear the input fields after adding a new dish
        dishNameInput.text = "";
        categoryDropdown.value = 0;
        halfDosePriceInput.text = "";
        fullDosePriceInput.text = "";
    }

    public void SaveDishesToFile()
    {
        //string path = Application.persistentDataPath + "/Pratos.txt";
        string json = JsonUtility.ToJson(new SerializableDishList { dishes = dishes }, true);
        File.WriteAllText(dataPath, json);

        // Send the updated dishes to all clients
        photonView.RPC("UpdateDishes", RpcTarget.Others, json);
    }

    [PunRPC]
    public void UpdateDishes(string json)
    {
        SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);
        dishes = loadedDishes.dishes;

        RefreshDishUI();
    }
    public void LoadDishesFromFile()
    {
        string path = Application.persistentDataPath + "/Pratos.txt";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);
            dishes = loadedDishes.dishes;

            foreach (Dish dish in dishes)
            {
                if (string.IsNullOrEmpty(dish.id))
                {
                    dish.id = Guid.NewGuid().ToString(); // Assign a new unique ID
                }
            }

            // Clear the old UI elements
            foreach (var dishUI in dishUIObjects)
            {
                Destroy(dishUI);
            }
            dishUIObjects.Clear();


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
        newDishUI.name = dish.id;
        // Set properties for the new dish instance
        TMP_InputField nameField = newDishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
        TMP_Dropdown categoryDropdown = newDishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
        TMP_InputField halfDoseField = newDishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
        TMP_InputField fullDoseField = newDishUI.transform.GetChild(4).GetComponent<TMP_InputField>();
        Toggle naHoraToggle = newDishUI.transform.GetChild(5).GetComponent<Toggle>();
        Toggle novidadeToggle = newDishUI.transform.GetChild(6).GetComponent<Toggle>();
        Toggle soldOutToggle = newDishUI.transform.GetChild(7).GetComponent<Toggle>();
        Toggle ativoToggle = newDishUI.transform.GetChild(8).GetComponent<Toggle>();
        Button deleteButton = newDishUI.transform.GetChild(9).GetComponent<Button>();

        #region Toggles
        naHoraToggle.onValueChanged.AddListener((isOn) =>
        {
            dish.naHora = isOn;
            SaveDishesToFile(); // Save and update all clients
        });

        novidadeToggle.onValueChanged.AddListener((isOn) =>
        {
            dish.novidade = isOn;
            SaveDishesToFile(); // Save and update all clients
        });

        soldOutToggle.onValueChanged.AddListener((isOn) =>
        {
            dish.Esgotado = isOn;
            SaveDishesToFile(); // Save and update all clients
        });

        ativoToggle.onValueChanged.AddListener((isOn) =>
        {
            dish.isAtivo = isOn;
            SaveDishesToFile(); // Save and update all clients
        });
        #endregion


        // Populate the UI with the dish data
        nameField.text = dish.nome;
        categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == dish.categoria); 
        halfDoseField.text = dish.precoMeia.ToString();
        fullDoseField.text = dish.precoDose.ToString();
        naHoraToggle.isOn = dish.naHora;
        novidadeToggle.isOn = dish.novidade;
        soldOutToggle.isOn = dish.Esgotado;
        ativoToggle.isOn = dish.isAtivo;

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
    public void DeleteDish(GameObject dishUI)
    {
        // Get the dish ID from the UI object
        string dishId = dishUI.name;

        // Find the dish with this ID
        var dishToRemove = dishes.FirstOrDefault(d => d.id == dishId);
        if (dishToRemove != null)
        {
            dishes.Remove(dishToRemove);
            SaveDishesToFile();
        }

        dishUIObjects.Remove(dishUI);
        Destroy(dishUI);
    }
    #endregion

    #region Edit
    private void ToggleEditMode()
    {
        isInEditMode = !isInEditMode; // Toggle edit mode
        saveButton.gameObject.SetActive(isInEditMode);

        foreach (GameObject dish in dishUIObjects)
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

        // Update each dish in the dishes list based on the UI
        for (int i = 0; i < dishUIObjects.Count; i++)
        {
            GameObject dishUI = dishUIObjects[i];
            Dish updatedDish = GetDishFromUI(dishUI);

            // Find and update the dish in the dishes list
            var dishToUpdate = dishes.FirstOrDefault(d => d.id == updatedDish.id);
            if (dishToUpdate != null)
            {
                dishToUpdate.UpdateDetails(updatedDish);
            }
        }

        // Save the updated dishes to a file
        SaveDishesToFile();

        // Reapply the search filter or refresh the UI
        if (!string.IsNullOrEmpty(currentSearchQuery))
        {
            SearchDishes();
        }
        else
        {
            RefreshDishUI();
        }

        saveButton.gameObject.SetActive(false);
    }

    private Dish GetDishFromUI(GameObject dishUI)
    {
        // Extract dish information from the UI elements and return a new Dish object
        string dishID = dishUI.name;
        TMP_InputField nameField = dishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
        TMP_Dropdown categoryDropdown = dishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
        TMP_InputField halfDoseField = dishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
        TMP_InputField fullDoseField = dishUI.transform.GetChild(4).GetComponent<TMP_InputField>();
        Toggle naHoraToggle = dishUI.transform.GetChild(5).GetComponent<Toggle>();
        Toggle novidadeToggle = dishUI.transform.GetChild(6).GetComponent<Toggle>();
        Toggle esgotadoToggle = dishUI.transform.GetChild(7).GetComponent<Toggle>();
        Toggle ativoToggle = dishUI.transform.GetChild(8).GetComponent<Toggle>();

        return new Dish
        {
            id = dishID,
            nome = nameField.text,
            categoria = categoryDropdown.options[categoryDropdown.value].text,
            precoMeia = float.Parse(halfDoseField.text),
            precoDose = float.Parse(fullDoseField.text),
            Esgotado = esgotadoToggle.isOn,
            isAtivo = ativoToggle.isOn,
            novidade = novidadeToggle.isOn,
            naHora = naHoraToggle.isOn
        };
    }
    #endregion

    #region Other

    private enum SortState
    {
        Ascending,
        Descending
    }
    private SortState currentSortState = SortState.Ascending;
    public void SortByActiveStatus()
    {
        switch (currentSortState)
        {
            case SortState.Ascending:
                dishes.Sort((a, b) => a.isAtivo.CompareTo(b.isAtivo));
                currentSortState = SortState.Descending;
                break;

            case SortState.Descending:
                dishes.Sort((a, b) => b.isAtivo.CompareTo(a.isAtivo));
                currentSortState = SortState.Ascending;
                break;
        }
        // Refresh the UI to reflect the sorted order
        RefreshDishUI();
    }

    private enum AlphaSortState
    {
        Alpha,
        NonAlpha
    }
    private AlphaSortState currentNameSortState = AlphaSortState.Alpha;
    public void SortByAlpha()
    {
        switch (currentNameSortState)
        {
            case AlphaSortState.Alpha:
                dishes.Sort((a, b) => string.Compare(a.nome, b.nome));
                currentNameSortState = AlphaSortState.NonAlpha;
                break;

            case AlphaSortState.NonAlpha:
                dishes.Sort((a, b) => string.Compare(b.nome, a.nome));
                currentNameSortState = AlphaSortState.Alpha;
                break;
        }

        // Refresh the UI to reflect the sorted order
        RefreshDishUI();
    }

    public void SearchDishes()
    {
        string query = searchInputField.text.ToLower().Trim();
        currentSearchQuery = query;

        // Filter the dishes based on the search query
        List<Dish> filteredDishes = dishes.Where(dish => dish.nome.ToLower().Contains(query)).ToList();

        // Refresh the UI with filtered dishes
        RefreshDishUI(filteredDishes);
    }


    private void StartDelayedSearch()
    {
        // If there's an ongoing search coroutine, stop it
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
        }

        // Start a new search coroutine with a delay
        searchCoroutine = StartCoroutine(DelayedSearch());
    }
    private IEnumerator DelayedSearch()
    {
        // Wait for 0.3 seconds
        yield return new WaitForSeconds(0.3f);

        // Execute the search
        SearchDishes();
    }
    private void RefreshDishUI()
    {
        // Clear the current UI objects
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        // Re-instantiate the UI objects based on the dishes list
        foreach (var dish in dishes)
        {
            InstantiateDishUI(dish);
        }
    }


    private void RefreshDishUI(List<Dish> filteredDishes)
    {
        // Clear the current UI objects
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        // Re-instantiate the UI objects based on the filtered dishes list
        foreach (var dish in filteredDishes)
        {
            InstantiateDishUI(dish);
        }
    }

    #endregion

}

[System.Serializable]
public class Dish
{
    public string id; // Unique identifier for each dish
    public string nome;
    public string categoria;
    public float precoMeia;
    public float precoDose;
    public bool Esgotado;
    public bool isAtivo;
    public bool novidade = false;
    public bool naHora = false;

    // Constructor to initialize a new dish with a unique ID
    public Dish()
    {
        id = Guid.NewGuid().ToString(); // Generates a unique ID
    }

    // Method to update dish details
    public void UpdateDetails(Dish updatedDish)
    {
        nome = updatedDish.nome;
        categoria = updatedDish.categoria;
        precoMeia = updatedDish.precoMeia;
        precoDose = updatedDish.precoDose;
        Esgotado = updatedDish.Esgotado;
        isAtivo = updatedDish.isAtivo;
        novidade = updatedDish.novidade;
        naHora = updatedDish.naHora;
    }
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

