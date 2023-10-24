using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class TVDishManager : MonoBehaviourPunCallbacks
{
    [Header("Layout References")]
    public Transform outrosLayout;
    public Transform peixeLayout;
    public Transform carneLayout;

    [Header("Prefab Reference")]
    public GameObject dishTVPrefab;

    [Header("Data File Path")]
    private string dataPath;

    private List<Dish> dishes = new List<Dish>();

    void Start()
    {
        Debug.Log("TVDishManager Start method called");
    }

    //private void LoadDishesFromFile()
    //{
    //    if (System.IO.File.Exists(dataPath))
    //    {
    //        string json = System.IO.File.ReadAllText(dataPath);
    //        SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);
    //        dishes = loadedDishes.dishes;
    //    }
    //}

    [PunRPC]
    public void UpdateUI(List<Dish> activeDishes)
    {
        Debug.Log("UpdateUI called with " + activeDishes.Count + " active dishes");
        // Update the list of dishes
        dishes = activeDishes;

        // Display the dishes
        DisplayDishes(dishes);
    }

    private void DisplayDishes(List<Dish> dishes)
    {
        Debug.Log("DisplayDishes called with " + dishes.Count + " dishes");
        // Clear the current display
        foreach (Transform child in outrosLayout)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in peixeLayout)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in carneLayout)
        {
            Destroy(child.gameObject);
        }

        // Display the dishes in the respective layout groups
        foreach (Dish dish in dishes)
        {
            Debug.Log("Displaying dish: " + dish.nome + ", isAtivo: " + dish.isAtivo + " ,esgotado:" + dish.Esgotado);
            if (dish.isAtivo)
            {
                InstantiateDishUI(dish);
            }
        }
    }

    private void InstantiateDishUI(Dish dish)
    {
        GameObject newDishUI = Instantiate(dishTVPrefab);
        TMP_Text nameText = newDishUI.transform.GetChild(1).GetComponent<TMP_Text>();
        TMP_Text halfPriceText = newDishUI.transform.GetChild(2).GetComponent<TMP_Text>();
        TMP_Text fullPriceText = newDishUI.transform.GetChild(3).GetComponent<TMP_Text>();

        nameText.text = dish.nome.ToUpper();
        halfPriceText.text = dish.precoMeia == 0 ? "" : dish.precoMeia.ToString("F2");
        fullPriceText.text = dish.precoDose == 0 ? "" : dish.precoDose.ToString("F2");

        if (dish.Esgotado)
        {
            nameText.color = Color.gray;
            nameText.fontStyle = TMPro.FontStyles.Strikethrough;
            halfPriceText.color = Color.gray;
            halfPriceText.fontStyle = TMPro.FontStyles.Strikethrough;
            fullPriceText.color = Color.gray;
            fullPriceText.fontStyle = TMPro.FontStyles.Strikethrough;
        }

        Transform parentLayout = GetParentLayout(dish.categoria);
        newDishUI.transform.SetParent(parentLayout, false);
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
}