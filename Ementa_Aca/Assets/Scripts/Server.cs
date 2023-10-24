using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Server : MonoBehaviour
{
    private TcpListener tcpListener;
    private UdpClient udpServer;
    private List<TcpClient> clients = new List<TcpClient>();
    private string dataPath;
    private DishManager dM;


    void Start()
    {
        dataPath = Application.persistentDataPath + "/Pratos.txt";
        // Start TCP server
        tcpListener = new TcpListener(IPAddress.Any, 12345);
        tcpListener.Start();
        Debug.Log("Server started on " + NetworkUtils.GetLocalIPAddress() + ":12345");

        // Start listening for client connections
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);

        // Start UDP server for network discovery
        udpServer = new UdpClient(12345);
        StartListeningForBroadcasts();
    }



    private void StartListeningForBroadcasts()
    {
        udpServer.BeginReceive(OnBroadcastReceived, null);
    }

    private void OnBroadcastReceived(IAsyncResult ar)
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = udpServer.EndReceive(ar, ref remoteEP);
        string message = Encoding.ASCII.GetString(data);
        if (message == "DISCOVER_SERVER_REQUEST")
        {
            byte[] response = Encoding.ASCII.GetBytes("DISCOVER_SERVER_RESPONSE");
            udpServer.Send(response, response.Length, remoteEP);
        }

        // Continue listening for broadcasts
        StartListeningForBroadcasts();
    }

    private void OnClientConnect(IAsyncResult ar)
    {
        TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
        Debug.Log("Client connected");
        //dM.editButton.interactable = false;
        clients.Add(tcpClient);

        dM.SaveDishesToFile();

        // Load dishes from file
        List<Dish> dishes = LoadDishesFromFile();

        // Filter out active dishes
        List<Dish> activeDishes = dishes.Where(dish => dish.isAtivo).ToList();

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        // Send the JSON string to the client
        SendData(json, tcpClient);

        SendActiveDishes();

        // Start listening for the next client
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
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

    private void SendData(string data, TcpClient client)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);
        client.GetStream().Write(bytes, 0, bytes.Length);
    }

    public void SendActiveDishes()
    {
        Debug.Log("SendActiveDishes called");
        // Load dishes from file
        List<Dish> dishes = LoadDishesFromFile();

        // Filter out active dishes
        List<Dish> activeDishes = dishes.Where(dish => dish.isAtivo).ToList();

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        Debug.Log("Sending data: " + json);
        // Send the JSON string to all connected clients
        for (int i = clients.Count - 1; i >= 0; i--)
        {
            TcpClient client = clients[i];
            try
            {
                SendData(json, client);
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending data to client: " + e.Message);
                // Remove the client from the list
                clients.RemoveAt(i);
                // Close the client connection
                client.Close();
            }
        }
    }
    void OnDestroy()
    {
        tcpListener.Stop();
    }
}

