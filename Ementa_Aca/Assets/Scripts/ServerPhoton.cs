using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ServerPhoton : MonoBehaviourPunCallbacks
{
    private List<Player> clients = new List<Player>();
    private string dataPath;

    void Start()
    {
        dataPath = Application.persistentDataPath + "/Pratos.txt";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 20;
        PhotonNetwork.JoinOrCreateRoom("MainRoom", roomOptions, TypedLobby.Default);
        //PhotonNetwork.JoinOrCreateRoom("TESTRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("New player joined: " + newPlayer.NickName);
        clients.Add(newPlayer);

        // Load dishes from file
        List<Dish> dishes = LoadDishesFromFile();

        // Filter out active dishes
        List<Dish> activeDishes = dishes.FindAll(dish => dish.isAtivo);

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        // Send the JSON string to the new player
        photonView.RPC("UpdateDishes", newPlayer, json);
    }

    private List<Dish> LoadDishesFromFile()
    {
        if (System.IO.File.Exists(dataPath))
        {
            string json = System.IO.File.ReadAllText(dataPath);
            SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);
            return loadedDishes.dishes;
        }
        return new List<Dish>();
    }

    public void SendActiveDishes()
    {
        Debug.Log("SendActiveDishes called");
        // Load dishes from file
        List<Dish> dishes = LoadDishesFromFile();

        // Filter out active dishes
        List<Dish> activeDishes = dishes.FindAll(dish => dish.isAtivo);

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        Debug.Log("Sending data: " + json);
        // Send the JSON string to all connected clients
        photonView.RPC("UpdateDishes", RpcTarget.Others, json);
    }

    [PunRPC]
    public void UpdateDishes(string json)
    {
        // This method is used to update the dishes on the client side
        // The client will handle updating the UI
    }
}
