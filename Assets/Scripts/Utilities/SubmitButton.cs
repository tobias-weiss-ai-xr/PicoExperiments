using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubmitButton : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        TMP_Text modalText = transform.parent.parent.Find("Modal Text").GetComponent<TMP_Text>();

        btn.onClick.AddListener(() => Debug.Log("Submit pressed! Question was: " + modalText.text));
    }
}
