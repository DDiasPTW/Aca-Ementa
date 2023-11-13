using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class ServerPhoton : MonoBehaviourPunCallbacks
{
    private string dataPath;

    void Start()
    {
        dataPath = Application.persistentDataPath + "/Pratos.txt";
        PhotonNetwork.ConnectUsingSettings();
        StartCoroutine(LogPhotonStats());
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10;
#if UNITY_EDITOR
        PhotonNetwork.JoinOrCreateRoom("TESTRoom", roomOptions, TypedLobby.Default);
        Debug.Log("Created TESTRoom");
#else
        //PhotonNetwork.JoinOrCreateRoom("MainRoom", roomOptions, TypedLobby.Default);
        PhotonNetwork.JoinOrCreateRoom("TESTRoom", roomOptions, TypedLobby.Default);
        Debug.Log("Created TESTRoom");
#endif
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("New player joined: " + newPlayer.NickName);
        SendActiveDishesToPlayer(newPlayer);
    }

    private void SendActiveDishesToPlayer(Player player)
    {
        List<Dish> activeDishes = LoadActiveDishes();

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        // Send the JSON string to the specified player
        photonView.RPC("UpdateDishes", player, json);
    }
    public void UpdateDishes()
    {
        List<Dish> activeDishes = LoadActiveDishes();

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        // Send the JSON string to all connected clients
        photonView.RPC("UpdateDishes", RpcTarget.Others, json);
    }

    private List<Dish> LoadActiveDishes()
    {
        List<Dish> dishes = LoadDishesFromFile();
        return dishes.FindAll(dish => dish.isAtivo);
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

    IEnumerator LogPhotonStats()
    {
        while (true)
        {
            Debug.Log(PhotonNetwork.NetworkStatisticsToString());
            yield return new WaitForSeconds(5); // Log every 5 seconds
        }
    }


    [PunRPC]
    public void UpdateDishes(string json)
    {
        // This method is used to update the dishes on the client side
        // The client will handle updating the UI
    }
}
