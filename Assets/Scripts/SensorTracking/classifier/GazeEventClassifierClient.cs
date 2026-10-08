using System;
using System.Collections;
using UnityEngine;

public class GazeEventClassifierClient : MonoBehaviour
{

    // References
    private GameObject _door;
    private GazeEventDetection _gazeEventDetection;
    private Savefile _savefile;
    private UdpSocket _udpSocket;

    // Socket
    public float sendInterval = 5f;

    // Door Open Machanism
    private int _doorOpenRequestCounter;
    private bool _doorOpenFlag;

    void Start()
    {
        _savefile = GameObject.Find("SavefileManager")?.GetComponent<Savefile>();
        this.EnsureObjectReference(ref _door, "doors 1");

        _udpSocket = GetComponent<UdpSocket>();

        if (_savefile != null && _savefile.avatarInput == AvatarInput.VARJO && GameObject.Find("XR Origin") != null)
            _gazeEventDetection = GameObject.Find("XR Origin").GetComponent<GazeEventDetection>();

        if (_gazeEventDetection == null)
        {
            // demo fallback: run detection locally without the VR rig
            _gazeEventDetection = gameObject.AddComponent<GazeEventDetection>();
            Debug.LogWarning("GazeEventClassifierClient: no GazeEventDetection on XR Origin; using a local component.");
        }

        _udpSocket.OnRx += ProcessData;

        StartCoroutine(SendDataCoroutine());
    }

    void Update()
    {
        if (_doorOpenFlag && _door != null && _door.activeSelf)
        {
            _door.SetActive(false);
            Debug.Log("Doors opened by gaze event classifier.");
        }
    }

    // Update is called once per frame
    IEnumerator SendDataCoroutine()
    {
        while (true)
        {
            _udpSocket.SendData(_gazeEventDetection.gazeEventBacklog.Serialize());
            yield return new WaitForSeconds(sendInterval);
        }
    }
    private void ProcessData(string data)
    {
        // Count up or reset open counter
        if (int.TryParse(data.Trim(), out int value) && value == 1)
            _doorOpenRequestCounter++;
        else
            _doorOpenRequestCounter = 0;

        // 3 times 1 (help wanted) so open the door
        if (_doorOpenRequestCounter >= 3)
            _doorOpenFlag = true;
    }
}
