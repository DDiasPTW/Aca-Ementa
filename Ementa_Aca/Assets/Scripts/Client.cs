using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour
{
    private TcpClient tcpClient;
    private UdpClient udpClient;
    private string serverIPAddress;
    void Start()
    {
        // Start UDP client for network discovery
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        // Send broadcast message to discover server
        byte[] request = Encoding.ASCII.GetBytes("DISCOVER_SERVER_REQUEST");
        udpClient.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, 12345));

        // Listen for responses
        udpClient.BeginReceive(OnBroadcastReceived, null);
        IPEndPoint remoteEP = null;
        ConnectToServer(remoteEP.Address.ToString());
    }

    private void OnBroadcastReceived(IAsyncResult ar)
    {
        IPEndPoint remoteEP = null;
        byte[] response = udpClient.EndReceive(ar, ref remoteEP);

        string message = Encoding.ASCII.GetString(response);
        if (message == "DISCOVER_SERVER_RESPONSE")
        {
            // Connect to server using the IP address of the first server that responds
            ConnectToServer(remoteEP.Address.ToString());
        }
    }

    private void ConnectToServer(string ipAddress)
    {
        try
        {
            tcpClient = new TcpClient(ipAddress, 12345);
            Debug.Log("Connected to server");
            BeginRead();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
            // Retry connecting after 1 second
            Invoke("ConnectToServer", 1f);
        }
    }





    private void BeginRead()
    {
        byte[] buffer = new byte[1024];
        tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnRead), buffer);
    }

    private void OnRead(IAsyncResult ar)
    {
        //Debug.Log("OnRead called");
        byte[] buffer = (byte[])ar.AsyncState;
        int bytesRead = tcpClient.GetStream().EndRead(ar);
        string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        //Debug.Log("Received data: " + data);

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
                    //Debug.Log("TVDishManager instance found");
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