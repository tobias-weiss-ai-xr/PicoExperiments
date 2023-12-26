using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpSocket : MonoBehaviour
{
    [SerializeField] string IP = "127.0.0.1"; // local host
    [SerializeField] int rxPort = 8000; // port to receive data from Python on
    [SerializeField] int txPort = 8001; // port to send data to Python on

    public event ProcessData OnRx;
    public delegate void ProcessData(string data);


    // Create necessary UdpClient objects
    UdpClient client;
    IPEndPoint remoteEndPoint;
    Thread receiveThread; // Receiving Thread

    public void SendData(string message) // Use to send data to Python
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    void Awake()
    {
        // Create remote endpoint (to Matlab) 
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), txPort);

        // Create local client
        client = new UdpClient(rxPort);

        // local endpoint define (where messages are received)
        // Create a new thread for reception of incoming messages
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // Initialize (seen in comments window)
        print("UDP Comms Initialised");
    }

    // Receive data, update packets received
    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                OnRx?.Invoke(text);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    //Prevent crashes - close clients and threads properly!
    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();

        client.Close();
    }

}
