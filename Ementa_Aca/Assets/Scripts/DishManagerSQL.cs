using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using SQLite;
using System;
using System.Linq;
using System.IO;

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
    public Button activeTitleButton;
    public Button nameButton;
    public TMP_InputField searchInputField;
    private Coroutine searchCoroutine;
    private string currentSearchQuery = "";
    private bool isSortByActiveStatusAscending = true;
    private bool isSortByAlphaAscending = false;

    [Header("SQL")]
    private SQLiteConnection dbConnection;
    private ServerPhotonSQL svPhoton;

    void Start()
    {
        svPhoton = GetComponent<ServerPhotonSQL>();
        Debug.Log(Application.persistentDataPath);
        InitializeDatabase();
        LoadDishesFromDatabase();

        createButton.onClick.AddListener(CreateDish);
        isInEditMode = false;
        editButton.onClick.AddListener(ToggleEditMode);
        saveButton.onClick.AddListener(SaveChanges);
        saveButton.gameObject.SetActive(false);
        activeTitleButton.onClick.AddListener(SortByActiveStatus);
        nameButton.onClick.AddListener(SortByAlpha);
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
        svPhoton.SendUpdatedDishesToAllClients();
        InstantiateDishUI(newDish);
        ClearInputFields();
        SendUpdatedDishesToServer();
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
            svPhoton.SendUpdatedDishesToAllClients();
        }

        searchInputField.text = "";
        LoadDishesFromDatabase();
        RefreshDishUI();
        SendUpdatedDishesToServer();
    }

    private void SendUpdatedDishesToServer()
    {
        var allDishes = dbConnection.Table<Dishe>().ToList();
        string json = JsonUtility.ToJson(new SerializableDishList { dishes = allDishes });
        photonView.RPC("ReceiveUpdatedDishes", RpcTarget.Others, json);
    }

    public void DeleteDish(GameObject dishUI)
    {
        string dishId = dishUI.name;
        DeleteDishFromDatabase(dishId);
        dishUIObjects.Remove(dishUI);
        Destroy(dishUI);
        SendUpdatedDishesToServer();
        svPhoton.SendUpdatedDishesToAllClients();
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

        TMP_InputField nameField = newDishUI.transform.GetChild(1).GetComponent<TMP_InputField>();
        TMP_Dropdown categoryDropdown = newDishUI.transform.GetChild(2).GetComponent<TMP_Dropdown>();
        TMP_InputField halfDoseField = newDishUI.transform.GetChild(3).GetComponent<TMP_InputField>();
        TMP_InputField fullDoseField = newDishUI.transform.GetChild(4).GetComponent<TMP_InputField>();
        Toggle naHoraToggle = newDishUI.transform.GetChild(5).GetComponent<Toggle>();
        Toggle novidadeToggle = newDishUI.transform.GetChild(6).GetComponent<Toggle>();
        Toggle soldOutToggle = newDishUI.transform.GetChild(7).GetComponent<Toggle>();
        Toggle ativoToggle = newDishUI.transform.GetChild(8).GetComponent<Toggle>();
        Button deleteButton = newDishUI.transform.GetChild(9).GetComponent<Button>();

        nameField.text = dish.nome;
        categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == dish.categoria);
        halfDoseField.text = dish.precoMeia.ToString();
        fullDoseField.text = dish.precoDose.ToString();
        naHoraToggle.isOn = dish.naHora;
        novidadeToggle.isOn = dish.novidade;
        soldOutToggle.isOn = dish.Esgotado;
        ativoToggle.isOn = dish.isAtivo;

        nameField.interactable = false;
        categoryDropdown.interactable = false;
        halfDoseField.interactable = false;
        fullDoseField.interactable = false;

        naHoraToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "naHora", isOn); });
        novidadeToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "novidade", isOn); });
        soldOutToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "Esgotado", isOn); });
        ativoToggle.onValueChanged.AddListener(isOn => { UpdateDishInDatabase(dish, "isAtivo", isOn); });
        deleteButton.onClick.AddListener(() => { DeleteDish(newDishUI); });
    }

    private void UpdateDishInDatabase(Dishe dish, string property, bool value)
    {
        Dishe dishToUpdate = dbConnection.Table<Dishe>().FirstOrDefault(d => d.id == dish.id);
        if (dishToUpdate != null)
        {
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

            dbConnection.Update(dishToUpdate);
            RefreshDishUI();
        }
        svPhoton.SendUpdatedDishesToAllClients();
    }

    public void SortByActiveStatus()
    {
        // Sort dishes based on active status but include all dishes
        dishes.Sort((a, b) => isSortByActiveStatusAscending ? a.isAtivo.CompareTo(b.isAtivo) : b.isAtivo.CompareTo(a.isAtivo));
        isSortByActiveStatusAscending = !isSortByActiveStatusAscending; // Toggle the sort order

        RefreshDishUI(dishes); // Refresh the UI with the sorted list
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

    private void RefreshDishUI(List<Dishe> updatedDishes = null)
    {
        // Clear the current UI objects
        foreach (var dishUI in dishUIObjects)
        {
            Destroy(dishUI);
        }
        dishUIObjects.Clear();

        // Reload dishes from the database if updatedDishes is null
        var dishesToShow = updatedDishes ?? dbConnection.Table<Dishe>().ToList();
        foreach (var dish in dishesToShow)
        {
            InstantiateDishUI(dish);
        }
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

    [System.Serializable]
    public class SerializableDishList
    {
        public List<Dishe> dishes;
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

