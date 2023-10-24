using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ClientPhoton : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinRoom("MainRoom");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to join room: " + message);
    }

    [PunRPC]
    public void UpdateDishes(string json)
    {
        SerializableDishList activeDishes = JsonUtility.FromJson<SerializableDishList>(json);
        UpdateUI(activeDishes.dishes);
    }

    public void UpdateUI(List<Dish> activeDishes)
    {
        Debug.Log("UpdateUI called with " + activeDishes.Count + " active dishes");
        TVDishManager tvDishManager = gameObject.GetComponent<TVDishManager>();
        if (tvDishManager != null)
        {
            tvDishManager.UpdateUI(activeDishes);
        }
        else
        {
            Debug.LogError("TVDishManager instance not found");
        }
    }
}
