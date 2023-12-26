using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GazeEventClassifierClient : MonoBehaviour
{

    // References
    private GameObject _door;
    private GazeEventDetection _gazeEventDetection;
    private Savefile _savefile;
    private UdpSocket _udpSocket;

    // Socket
    public float sendInterval = 5f;
    public float updateInterval = 1f;

    // Door Open Machanism
    private int _doorOpenRequestCounter;
    private bool _doorOpenFlag;

    void Start()
    {
        _savefile = GameObject.Find("SavefileManager").GetComponent<Savefile>();
        this.EnsureObjectReference(ref _door, "doors 1");

        _udpSocket = GetComponent<UdpSocket>();

        if (_savefile.avatarInput == AvatarInput.VARJO)
            _gazeEventDetection = GameObject.Find("XR Origin").GetComponent<GazeEventDetection>();

        // demo
        _gazeEventDetection = gameObject.AddComponent<GazeEventDetection>();

        _udpSocket.OnRx += ProcessData;

        StartCoroutine(UpdateEventBacklog());
        StartCoroutine(SendDataCoroutine());
    }

    void Update()
    {
        if (_doorOpenFlag && _door.activeSelf)
        {
            _door.SetActive(false);
            Debug.Log("Doors opened by gaze event classifier.");
        }
    }

    IEnumerator UpdateEventBacklog()
    {
        while (true)
        {
            //demo 
            //emits one empty gaze event every second
            GazeEventDetection.GazeEvent g = new GazeEventDetection.GazeEvent();
            _gazeEventDetection.gazeEventBacklog.eventBacklog.Add(g);
            // print(_gazeEventDetection.gazeEventBacklog.Serialize());
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Update is called once per frame
    IEnumerator SendDataCoroutine()
    {
        while (true)
        {
            // _udpSocket.SendData(_gazeEventDetection.gazeEventBacklog.Serialize().ToString());
            yield return new WaitForSeconds(sendInterval);
        }
    }
    private void ProcessData(string data)
    {
        // print("> > " + data);

        // Count up or reset open counter
        if (int.Parse(data) == 1)
            _doorOpenRequestCounter++;
        else
            _doorOpenRequestCounter = 0;

        // 3 times 1 (help wanted) so open the door
        if (_doorOpenRequestCounter >= 3)
            _doorOpenFlag = true;
    }
}
