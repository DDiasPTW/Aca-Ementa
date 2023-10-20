using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour
{
    private TcpClient tcpClient;

    void Start()
    {
        tcpClient = new TcpClient(/*NetworkUtils.GetLocalIPAddress()*/"127.0.0.1", 12345);
        Debug.Log("Connected to server");
        BeginRead();
    }

    private void BeginRead()
    {
        byte[] buffer = new byte[1024];
        tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), buffer);
    }

    private void OnRead(IAsyncResult ar)
    {
        Debug.Log("OnRead called");
        byte[] buffer = (byte[])ar.AsyncState;
        int bytesRead = tcpClient.GetStream().EndRead(ar);
        string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Debug.Log("Received data: " + data);

        // Deserialize the received data to get the list of active dishes
        try
        {
            SerializableDishList activeDishes = JsonUtility.FromJson<SerializableDishList>(data);
            // Update the UI to show only the active dishes
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                TVDishManager tvDishManager = gameObject.GetComponent<TVDishManager>();
                if (tvDishManager != null)
                {
                    Debug.Log("TVDishManager instance found");
                    Debug.Log("Calling UpdateUI with " + activeDishes.dishes.Count + " active dishes");
                    tvDishManager.UpdateUI(activeDishes.dishes);
                }
                else
                {
                    Debug.LogError("TVDishManager instance not found");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Error deserializing data: " + ex.Message);
        }

        BeginRead();
    }



    void OnDestroy()
    {
        tcpClient.Close();
    }
}

public class NetworkUtils
{
    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
}