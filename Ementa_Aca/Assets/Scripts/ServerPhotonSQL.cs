using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using SQLite;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ServerPhotonSQL : MonoBehaviourPunCallbacks
{
    private SQLiteConnection dbConnection;

    void Start()
    {
        dbConnection = new SQLiteConnection(Application.persistentDataPath + "/dishes.db");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 5 };
        PhotonNetwork.JoinOrCreateRoom("MAINRoom", roomOptions, TypedLobby.Default);
        Debug.Log("Created MAINRoomRoom");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        SendUpdatedDishesToAllClients();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SendActiveDishesToPlayer(newPlayer);
        Debug.Log("New user joined: " + newPlayer.NickName);
    }

    private void SendActiveDishesToPlayer(Player player)
    {
        var activeDishes = LoadActiveDishes();
        string json = JsonConvert.SerializeObject(new SerializableDishList { dishes = activeDishes });
        photonView.RPC("UpdateDishes", player, json);
    }



    private List<Dishe> LoadActiveDishes()
    {
        var activeDishes = dbConnection.Table<Dishe>().Where(d => d.isAtivo).ToList();
        foreach (var dish in activeDishes)
        {
            Debug.Log($"Dish: {dish.nome}, Active: {dish.isAtivo}");
        }
        return activeDishes;
    }

    [PunRPC]
    public void ReceiveUpdatedDishes(string json)
    {
        // This method receives updated dishes from the DishManagerSQL
        // and sends them to all clients
        photonView.RPC("UpdateDishes", RpcTarget.All, json);
    }

    // public void SendUpdatedDishesToAllClients()
    // {
    //     var activeDishes = LoadActiveDishes();
    //     if (activeDishes == null || activeDishes.Count == 0)
    //     {
    //         Debug.LogError("No active dishes to send");
    //         return;
    //     }

    //     try
    //     {
    //         SerializableDishList dishList = new SerializableDishList { dishes = activeDishes };
    //         string json = JsonConvert.SerializeObject(dishList);
    //         Debug.Log("Sending updated dishes: " + json);
    //         photonView.RPC("UpdateDishes", RpcTarget.All, json);
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.LogError("Error serializing dishes: " + ex.Message);
    //     }
    // }

    public void SendUpdatedDishesToAllClients()
{
    var allDishes = dbConnection.Table<Dishe>().ToList();
    string json = JsonConvert.SerializeObject(new SerializableDishList { dishes = allDishes });
    photonView.RPC("UpdateDishes", RpcTarget.All, json);
}



    [PunRPC]
    public void UpdateDishes(string json)
    {
        // Client-side UI update logic goes here
    }

    [System.Serializable]
    public class SerializableDishList
    {
        public List<Dishe> dishes;
    }
}