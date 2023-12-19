using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using SQLite;
using System.Linq;
using System.Collections.Generic;

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
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SendActiveDishesToPlayer(newPlayer);
    }

    private void SendActiveDishesToPlayer(Player player)
    {
        var activeDishes = LoadActiveDishes();
        string json = JsonUtility.ToJson(new SerializableDishList { dishes = activeDishes });
        photonView.RPC("UpdateDishes", player, json);
    }

    private List<Dishe> LoadActiveDishes()
    {
        return dbConnection.Table<Dishe>().Where(d => d.isAtivo).ToList();
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


