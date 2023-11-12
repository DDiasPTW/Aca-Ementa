using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class ClientPhoton : MonoBehaviourPunCallbacks
{
    private bool isTryingToConnect = false;
    private float retryInterval = 5f; //time in seconds before attempting to retry to connect again

    private int maxConnectionAttempts = 3; // Maximum number of attempts to connect to a room until it loads from localFile
    private int currentAttempt = 0;

    void Start()
    {
        isTryingToConnect = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinRoom("MainRoom");
        //PhotonNetwork.JoinRoom("TESTRoom");
    }

    private void TryJoiningRoom()
    {
        if (!PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.JoinRoom("MainRoom");
        //PhotonNetwork.JoinRoom("TESTRoom");
        isTryingToConnect = true;
        currentAttempt++;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + "MainRoom");
        isTryingToConnect = false;
        currentAttempt = 0;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join room failed: " + message + ". Attempt " + currentAttempt);

        if (currentAttempt >= maxConnectionAttempts)
        {
            // If the maximum number of attempts has been reached, load from the local file
            Debug.Log("Failed to connect to room after " + maxConnectionAttempts + " attempts. Loading dishes from local file.");
            LoadDishesFromLocalFile();
        }

        // Always retry after a delay
        StartCoroutine(RetryJoiningRoom());
    }

    private void LoadDishesFromLocalFile()
    {
        string path = Application.persistentDataPath + "/Pratos.txt";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            SerializableDishList loadedDishes = JsonUtility.FromJson<SerializableDishList>(json);
            UpdateUI(loadedDishes.dishes);
        }
        else
        {
            Debug.LogError("Local dishes file not found.");
        }
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected from Photon: " + cause);
        isTryingToConnect = false;
        currentAttempt = 0;
    }

    private IEnumerator RetryJoiningRoom()
    {
        yield return new WaitForSeconds(retryInterval); // Wait for the specified interval
        if (isTryingToConnect)
        {
            Debug.Log("Trying to join room");
            TryJoiningRoom(); // Try to join the room again
        }
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
