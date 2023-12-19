using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System;
using System.IO;
using SQLite;

public class DishManagerSQL : MonoBehaviourPunCallbacks
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
    private List<Dishe> dishes = new List<Dishe>();

    [Header("Other")]
    //ordering
    public Button activeTitleButton;
    public Button nameButton;
    //searching
    public TMP_InputField searchInputField;
    private Coroutine searchCoroutine; //add some delay
    private string currentSearchQuery = "";
    private bool isSortByActiveStatusAscending = true;
    private bool isSortByAlphaAscending = false;


    [Header("SQL")]
    private SQLiteConnection dbConnection;


    void Start()
    {
        Debug.Log(Application.persistentDataPath);
        InitializeDatabase();
        LoadDishesFromDatabase();

        createButton.onClick.AddListener(CreateDish);
        isInEditMode = false;
        //editing and saving
        editButton.onClick.AddListener(ToggleEditMode);
        saveButton.onClick.AddListener(SaveChanges);
        saveButton.gameObject.SetActive(false);
        //reordering
        activeTitleButton.onClick.AddListener(SortByActiveStatus);
        nameButton.onClick.AddListener(SortByAlpha);
        //searching
        searchInputField.onValueChanged.AddListener(delegate { StartDelayedSearch(); });

    }

    void OnApplicationQuit()
    {
        if (dbConnection != null)
        {
            dbConnection.Close();
            dbConnection = null;
        }
    }

    void InitializeDatabase()
    {
        string databasePath = Path.Combine(Application.persistentDataPath, "dishes.db");
        dbConnection = new SQLiteConnection(databasePath);

        dbConnection.CreateTable<Dishe>();
    }

    void LoadDishesFromDatabase()
    {
        dishes = dbConnection.Table<Dishe>().ToList();
        foreach (var dish in dishes)
        {
            InstantiateDishUI(dish);
        }
    }
    // New methods for database operations
    void SaveDishToDatabase(Dishe dish)
    {
        dbConnection.InsertOrReplace(dish);
    }

    void DeleteDishFromDatabase(string dishId)
    {
        dbConnection.Delete<Dishe>(dishId);
    }

    public void CreateDish()
    {
        if (string.IsNullOrWhiteSpace(dishNameInput.text))
        {
            return;
        }

        // Use 0 as default value if the input is invalid or empty
        float.TryParse(halfDosePriceInput.text, out float halfDosePrice);
        float.TryParse(fullDosePriceInput.text, out float fullDosePrice);

        Dishe newDish = new Dishe
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

        SaveDishToDatabase(newDish);
        InstantiateDishUI(newDish);

        ClearInputFields();
    }


    private void ClearInputFields()
    {
        dishNameInput.text = "";
        categoryDropdown.value = 0;
        halfDosePriceInput.text = "";
        fullDosePriceInput.text = "";
    }

    public void ToggleEditMode()
    {
        isInEditMode = !isInEditMode;
        saveButton.gameObject.SetActive(isInEditMode);

        // Enable/Disable input fields based on edit mode
        ToggleInputFields(isInEditMode);
    }

    public void SaveChanges()
    {
        isInEditMode = false;
        saveButton.gameObject.SetActive(false);

        foreach (GameObject dishUI in dishUIObjects)
        {
            Dishe updatedDish = GetDishFromUI(dishUI);
            SaveDishToDatabase(updatedDish);
        }

        searchInputField.text = ""; // Clear search field
        LoadDishesFromDatabase(); // Reload all dishes from the database
        RefreshDishUI(); // Refresh the UI
    }

    public void DeleteDish(GameObject dishUI)
    {
        string dishId = dishUI.name;
        DeleteDishFromDatabase(dishId);

        dishUIObjects.Remove(dishUI);
        Destroy(dishUI);
    }

    private void ToggleInputFields(bool enable)
    {
        foreach (GameObject dishUI in dishUIObjects)
        {
            TMP_InputField nameField = dishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
            TMP_Dropdown categoryDropdown = dishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
            TMP_InputField halfDoseField = dishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
            TMP_InputField fullDoseField = dishUI.transform.GetChild(4).GetComponent<TMP_InputField>();

            nameField.interactable = enable;
            categoryDropdown.interactable = enable;
            halfDoseField.interactable = enable;
            fullDoseField.interactable = enable;
        }
    }

    private Dishe GetDishFromUI(GameObject dishUI)
    {
        string dishID = dishUI.name;
        TMP_InputField nameField = dishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
        TMP_Dropdown categoryDropdown = dishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
        TMP_InputField halfDoseField = dishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
        TMP_InputField fullDoseField = dishUI.transform.GetChild(4).GetComponent<TMP_InputField>();
        Toggle naHoraToggle = dishUI.transform.GetChild(5).GetComponent<Toggle>();
        Toggle novidadeToggle = dishUI.transform.GetChild(6).GetComponent<Toggle>();
        Toggle esgotadoToggle = dishUI.transform.GetChild(7).GetComponent<Toggle>();
        Toggle ativoToggle = dishUI.transform.GetChild(8).GetComponent<Toggle>();

        return new Dishe
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

    private void InstantiateDishUI(Dishe dish)
    {
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

        // Set the UI elements with dish data
        nameField.text = dish.nome;
        categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == dish.categoria);
        halfDoseField.text = dish.precoMeia.ToString();
        fullDoseField.text = dish.precoDose.ToString();
        naHoraToggle.isOn = dish.naHora;
        novidadeToggle.isOn = dish.novidade;
        soldOutToggle.isOn = dish.Esgotado;
        ativoToggle.isOn = dish.isAtivo;
         
        // Disable input for the created dish details
        nameField.interactable = false;
        categoryDropdown.interactable = false;
        halfDoseField.interactable = false;
        fullDoseField.interactable = false;

        // Add listeners for toggles and delete button
        naHoraToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "naHora", isOn); });
        novidadeToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "novidade", isOn); });
        soldOutToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "Esgotado", isOn); });
        ativoToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "isAtivo", isOn); });
        deleteButton.onClick.AddListener(() => { DeleteDish(newDishUI); });
    }

    private void UpdateDishInDatabase(Dishe dish, string property, bool value)
    {
        // Fetch the dish from the database
        Dishe dishToUpdate = dbConnection.Table<Dishe>().FirstOrDefault(d => d.id == dish.id);
        if (dishToUpdate != null)
        {
            // Update the specified property
            switch (property)
            {
                case "naHora":
                    dishToUpdate.naHora = value;
                    break;
                case "novidade":
                    dishToUpdate.novidade = value;
                    break;
                case "Esgotado":
                    dishToUpdate.Esgotado = value;
                    break;
                case "isAtivo":
                    dishToUpdate.isAtivo = value;
                    break;
            }

            // Save the updated dish back to the database
            dbConnection.Update(dishToUpdate);
            RefreshDishUI();
        }
    }


    public void SortByActiveStatus()
    {
        if (isSortByActiveStatusAscending)
            dishes.Sort((a, b) => a.isAtivo.CompareTo(b.isAtivo));
        else
            dishes.Sort((a, b) => b.isAtivo.CompareTo(a.isAtivo));

        isSortByActiveStatusAscending = !isSortByActiveStatusAscending; // Toggle the sort order
        RefreshDishUI(dishes);
    }


    public void SortByAlpha()
    {
        if (isSortByAlphaAscending)
            dishes.Sort((a, b) => string.Compare(a.nome, b.nome));
        else
            dishes.Sort((a, b) => string.Compare(b.nome, a.nome));

        isSortByAlphaAscending = !isSortByAlphaAscending; // Toggle the sort order
        RefreshDishUI(dishes);
    }



    private void RefreshDishUI()
    {
        // Clear the current UI objects
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        // Reload dishes from the database
        LoadDishesFromDatabase(); // This will repopulate 'dishes' list and UI
    }


    private IEnumerator DelayedSearch()
    {
        yield return new WaitForSeconds(0.3f); // Delay before search
        SearchDishes();
    }


    private void StartDelayedSearch()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
        }
        searchCoroutine = StartCoroutine(DelayedSearch());
    }


    private void SearchDishes()
    {
        string query = searchInputField.text.ToLower().Trim();
        var filteredDishes = string.IsNullOrEmpty(query)
            ? dishes
            : dishes.Where(dish => dish.nome.ToLower().Contains(query)).ToList();

        RefreshDishUI(filteredDishes);
    }

    private void RefreshDishUI(List<Dishe> sortedDishes = null)
    {
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        var dishesToShow = sortedDishes ?? dishes; // Use sorted list if provided
        foreach (var dish in dishesToShow)
        {
            InstantiateDishUI(dish);
        }
    }

}

[Table("dishes")]
public class Dishe
{
    [PrimaryKey]
    public string id { get; set; }
    public string nome { get; set; }
    public string categoria { get; set; }
    public float precoMeia { get; set; }
    public float precoDose { get; set; }
    public bool Esgotado { get; set; }
    public bool isAtivo { get; set; }
    public bool novidade { get; set; }
    public bool naHora { get; set; }
}

