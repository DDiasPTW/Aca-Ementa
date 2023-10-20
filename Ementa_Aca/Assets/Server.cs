using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Server : MonoBehaviour
{
    private TcpListener tcpListener;
    private List<TcpClient> clients = new List<TcpClient>();
    private string dataPath;

    void Start()
    {
        dataPath = Application.persistentDataPath + "/Pratos.txt";
        tcpListener = new TcpListener(IPAddress.Any, 12345);
        tcpListener.Start();
        Debug.Log("Server started on " + IPAddress.Any + ":12345");
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
    }

    private void OnClientConnect(IAsyncResult ar)
    {
        TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
        Debug.Log("Client connected");

        clients.Add(tcpClient);

        // Load dishes from file
        List<Dish> dishes = LoadDishesFromFile();

        // Filter out active dishes
        List<Dish> activeDishes = dishes.Where(dish => dish.isAtivo).ToList();

        // Serialize the list of active dishes to JSON
        SerializableDishList serializableDishList = new SerializableDishList { dishes = activeDishes };
        string json = JsonUtility.ToJson(serializableDishList);

        // Send the JSON string to the client
        SendData(json, tcpClient);
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
        foreach (TcpClient client in clients)
        {
            try
            {
                SendData(json, client);
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending data to client: " + e.Message);
            }
        }
    }


    void OnDestroy()
    {
        tcpListener.Stop();
    }
}

