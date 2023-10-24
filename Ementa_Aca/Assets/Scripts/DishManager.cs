using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

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
            Debug.LogError("Dish name cannot be empty!");
        }

        // Try to parse the half dose price
        float.TryParse(halfDosePriceInput.text, out float halfDosePrice);

        // Try to parse the full dose price
        float.TryParse(fullDosePriceInput.text, out float fullDosePrice);

        Dish newDish = new Dish
        {
        nome = dishNameInput.text,
        categoria = categoryDropdown.options[categoryDropdown.value].text,
        precoMeia = halfDosePrice,
        precoDose = fullDosePrice,
        Esgotado = false,
        isAtivo = false
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

        // Clear the old UI elements
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        // Instantiate the UI elements for the updated dishes
        foreach (Dish dish in dishes)
        {
            InstantiateDishUI(dish);
        }
    }

    public void LoadDishesFromFile()
    {
        string path = Application.persistentDataPath + "/Pratos.txt";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);
            dishes = loadedDishes.dishes;

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

        // Set properties for the new dish instance
        TMP_InputField nameField = newDishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
        TMP_Dropdown categoryDropdown = newDishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
        TMP_InputField halfDoseField = newDishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
        TMP_InputField fullDoseField = newDishUI.transform.GetChild(4).GetComponent<TMP_InputField>();
        Toggle soldOutToggle = newDishUI.transform.GetChild(5).GetComponent<Toggle>();
        Toggle ativoToggle = newDishUI.transform.GetChild(6).GetComponent<Toggle>();
        Button deleteButton = newDishUI.transform.GetChild(7).GetComponent<Button>();

        soldOutToggle.onValueChanged.AddListener((isOn) =>
        {
            dish.Esgotado = isOn;
            SaveDishesToFile(); // Save immediately after change

            ServerPhoton server = gameObject.GetComponent<ServerPhoton>();
            if (server != null)
            {
                server.SendActiveDishes();  // Send the updated dish status to all connected clients
            }
        });


        ativoToggle.onValueChanged.AddListener((isOn) =>
        {
            dish.isAtivo = isOn;
            SaveDishesToFile(); // Save immediately after change

            ServerPhoton server = gameObject.GetComponent<ServerPhoton>();
            if (server != null)
            {
                server.SendActiveDishes();  // Send the updated list of active dishes to all connected clients
            }
        });


        // Populate the UI with the dish data
        nameField.text = dish.nome;
        categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == dish.categoria); 
        halfDoseField.text = dish.precoMeia.ToString();
        fullDoseField.text = dish.precoDose.ToString();
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
    public void DeleteDish(GameObject dish)
    {
        int indexToRemove = dishUIObjects.IndexOf(dish);

        Toggle soldOutToggle = dish.transform.GetChild(5).GetComponent<Toggle>();
        Toggle ativoToggle = dish.transform.GetChild(6).GetComponent<Toggle>();

        // Remove listeners
        soldOutToggle.onValueChanged.RemoveAllListeners();
        ativoToggle.onValueChanged.RemoveAllListeners();

        if (indexToRemove >= 0 && indexToRemove < dishes.Count)
        {
            dishes.RemoveAt(indexToRemove);
            SaveDishesToFile();
        }

        dishUIObjects.Remove(dish);
        //Debug.Log("Removing... " + dish.name);
        Destroy(dish);
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

        dishes.Clear(); // Clear the current list of dishes

        foreach (GameObject dish in dishUIObjects)
        {
            TMP_InputField nameField = dish.transform.GetChild(1).GetComponent<TMP_InputField>();
            TMP_Dropdown categoryDropdown = dish.transform.GetChild(2).GetComponent<TMP_Dropdown>();
            TMP_InputField halfDoseField = dish.transform.GetChild(3).GetComponent<TMP_InputField>();
            TMP_InputField fullDoseField = dish.transform.GetChild(4).GetComponent<TMP_InputField>();
            Toggle esgotadoToggle = dish.transform.GetChild(5).GetComponent<Toggle>();
            Toggle ativoToggle = dish.transform.GetChild(6).GetComponent<Toggle>();

            // Update the dish list with the latest UI values
            Dish updatedDish = new Dish
            {
                nome = nameField.text,
                categoria = categoryDropdown.options[categoryDropdown.value].text,
                precoMeia = float.Parse(halfDoseField.text),
                precoDose = float.Parse(fullDoseField.text),
                Esgotado = esgotadoToggle.isOn,
                isAtivo = ativoToggle.isOn
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

        // Load the updated dishes from the file
        LoadDishesFromFile();

        ServerPhoton server = gameObject.GetComponent<ServerPhoton>();
        if (server != null)
        {
            server.SendActiveDishes();  // Send the updated dish status to all connected clients
        }
        saveButton.gameObject.SetActive(false);
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
        string query = searchInputField.text.ToLower().Trim(); // Get the search query and convert to lowercase

        // Filter the dishes based on the search query
        List<Dish> filteredDishes = dishes.Where(dish => dish.nome.ToLower().Contains(query)).ToList();

        // Clear the current UI objects
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        // Display only the filtered dishes
        foreach (var dish in filteredDishes)
        {
            InstantiateDishUI(dish);
        }
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
        // Wait for 0.5 seconds
        yield return new WaitForSeconds(0.5f);

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

        // Re-instantiate the UI objects based on the sorted dishes list
        foreach (var dish in dishes)
        {
            InstantiateDishUI(dish);
        }
    }

    #endregion

}

[System.Serializable]
public class Dish
{
    public string nome;
    public string categoria;
    public float precoMeia;
    public float precoDose;
    public bool Esgotado;
    public bool isAtivo;

    //!Adicionar depois?
    //public bool novidade = false;
    //public bool popular = false;
    //public bool promo = false;
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

