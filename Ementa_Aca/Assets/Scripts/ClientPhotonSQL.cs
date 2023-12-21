using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using SQLite;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ClientPhotonSQL : MonoBehaviourPunCallbacks
{
    private SQLiteConnection dbConnection;
    private bool isTryingToConnect = false;
    private float retryInterval = 5f; // Time in seconds before retrying to connect
    private int maxConnectionAttempts = 3;
    private int currentAttempt = 0;

    void Start()
    {
        dbConnection = new SQLiteConnection(Application.persistentDataPath + "/dishes.db");
        isTryingToConnect = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinRoom("MAINRoom");
    }

    private void TryJoiningRoom()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.JoinRoom("MAINRoom");
        Debug.Log("trying to join MAINRoom");
        isTryingToConnect = true;
        currentAttempt++;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined MAINRoom");
        isTryingToConnect = false;
        currentAttempt = 0;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join room failed: " + message + ". Attempt " + currentAttempt);
        StartCoroutine(RetryJoiningRoom());
    }

    private IEnumerator RetryJoiningRoom()
    {
        yield return new WaitForSeconds(retryInterval);
        if (isTryingToConnect)
        {
            Debug.Log("Trying to join room");
            TryJoiningRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected from Photon: " + cause);
        isTryingToConnect = false;
        currentAttempt = 0;
    }

    [PunRPC]
    public void UpdateDishes(string json)
    {
        Debug.Log("Received dishes JSON: " + json);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Received empty JSON string in UpdateDishes");
            return;
        }

        try
        {
            SerializableDishList receivedDishes = JsonConvert.DeserializeObject<SerializableDishList>(json);
            if (receivedDishes == null || receivedDishes.dishes == null || receivedDishes.dishes.Count == 0)
            {
                Debug.LogError("Deserialized dishes list is null or empty");
                return;
            }

            UpdateUI(receivedDishes.dishes);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error deserializing JSON: " + ex.Message);
        }
    }

    private void UpdateUI(List<Dishe> activeDishes)
    {
        Debug.Log("UpdateUI called with " + activeDishes.Count + " active dishes");
        TVDishManagerSQL tvDishManager = gameObject.GetComponent<TVDishManagerSQL>();
        if (tvDishManager != null)
        {
            tvDishManager.UpdateUI(activeDishes);
        }
        else
        {
            Debug.LogError("TVDishManager instance not found");
        }
    }


    [System.Serializable]
    public class SerializableDishList
    {
        public List<Dishe> dishes;
    }
}
