using UnityEngine;
using TMPro;
using SQLite;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using Newtonsoft.Json;

public class TVDishManagerSQL : MonoBehaviour
{
    [Header("Layout References")]
    public Transform outrosLayout;
    public Transform peixeLayout;
    public Transform carneLayout;

    [Header("Prefab Reference")]
    public GameObject dishTVPrefab;
    public GameObject layouts;

    [Header("Database Connection")]
    private SQLiteConnection dbConnection;

    public Color novidadeColor;

    void Start()
    {
        dbConnection = new SQLiteConnection(Application.persistentDataPath + "/dishes.db");
    }

    public void DisplayDishes(List<Dishe> dishes)
    {
        Debug.Log("DisplayDishes called with " + dishes.Count + " dishes");

        // Clear the current display
        ClearLayouts();

        // Display the dishes in the respective layout groups
        foreach (Dishe dish in dishes)
        {
            if (dish.isAtivo) // Check if the dish is active before displaying
            {
                InstantiateDishUI(dish);
            }
        }
    }

    private void ClearLayouts()
    {
        ClearLayout(outrosLayout);
        ClearLayout(peixeLayout);
        ClearLayout(carneLayout);
    }

    private void ClearLayout(Transform layout)
{
    foreach (Transform child in layout)
    {
        if (child.gameObject != layouts) // Skip the 'Layouts' GameObject
        {
            Destroy(child.gameObject);
        }
    }
}


    public void UpdateUI(List<Dishe> activeDishes)
    {
        ClearLayouts();
        DisplayDishes(activeDishes);
    }


    private List<Dishe> DeserializeDishesManually(string json)
    {
        try
        {
            SerializableDishList dishList = JsonConvert.DeserializeObject<SerializableDishList>(json);
            return dishList != null ? dishList.dishes : new List<Dishe>();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error deserializing JSON: " + ex.Message);
            return new List<Dishe>();
        }
    }


    private void InstantiateDishUI(Dishe dish)
    {
        GameObject newDishUI = Instantiate(dishTVPrefab);

        TMP_Text nameText = newDishUI.transform.GetChild(1).GetComponent<TMP_Text>();
        TMP_Text halfPriceText = newDishUI.transform.GetChild(2).GetComponent<TMP_Text>();
        TMP_Text fullPriceText = newDishUI.transform.GetChild(3).GetComponent<TMP_Text>();
        GameObject naHoraImage = newDishUI.transform.GetChild(4).gameObject;

        Transform parentLayout = GetParentLayout(dish.categoria);
        // If the dish is in the "Outros" category and there's at least one child (excluding the Layouts GameObject)
        if (parentLayout == outrosLayout && outrosLayout.childCount > 0)
        {
            // Insert the new dish UI before the last child in 'outrosLayout'
            int lastChildIndex = outrosLayout.childCount - 1;
            newDishUI.transform.SetParent(outrosLayout, false);
            newDishUI.transform.SetSiblingIndex(lastChildIndex);
        }
        else
        {
            // For other categories, just set the parent normally
            newDishUI.transform.SetParent(parentLayout, false);
        }


        nameText.text = dish.nome.ToUpper();
        halfPriceText.text = dish.precoMeia == 0 ? "" : dish.precoMeia.ToString("F2");
        fullPriceText.text = dish.precoDose == 0 ? "" : dish.precoDose.ToString("F2");
        naHoraImage.SetActive(dish.naHora);

        if (dish.Esgotado)
        {
            SetTextStyles(nameText, halfPriceText, fullPriceText, Color.gray, TMPro.FontStyles.Strikethrough);
        }
        else if (dish.novidade)
        {
            SetTextStyles(nameText, halfPriceText, fullPriceText, novidadeColor, TMPro.FontStyles.Normal);
        }
    }

    private void SetTextStyles(TMP_Text nameText, TMP_Text halfPriceText, TMP_Text fullPriceText, Color color, TMPro.FontStyles style)
    {
        nameText.color = color;
        nameText.fontStyle = style;
        halfPriceText.color = color;
        halfPriceText.fontStyle = style;
        fullPriceText.color = color;
        fullPriceText.fontStyle = style;
    }

    private Transform GetParentLayout(string categoria)
    {
        switch (categoria)
        {
            case "Outros":
                return outrosLayout;
            case "Peixe":
                return peixeLayout;
            case "Carne":
                return carneLayout;
            default:
                return outrosLayout; // Default to "Outros" if category is unknown
        }
    }

    [System.Serializable]
    public class SerializableDishList
    {
        public List<Dishe> dishes;
    }
}
