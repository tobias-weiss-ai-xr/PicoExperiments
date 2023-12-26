using System;
using System.Collections;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FtTest : MonoBehaviour
{
    private FtManager ftManager;
    private FileWriter fileWriter;
    private Button buttonSad;
    private Button buttonHappy;
    void Start()
    {
        ftManager = GetComponent<FtManager>();
        fileWriter = GetComponent<FileWriter>();
        buttonSad = GameObject.Find("ButtonSad").GetComponent<Button>();
        buttonHappy = GameObject.Find("ButtonHappy").GetComponent<Button>();
        buttonSad.onClick.AddListener(delegate { ftManager.SendValues("sad"); });
        buttonHappy.onClick.AddListener(delegate { ftManager.SendValues("happy"); });
    }


}