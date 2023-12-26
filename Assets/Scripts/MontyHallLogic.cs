using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MontyHallLogic : MonoBehaviour
{
    // UI
    Transform choiceUi;
    Transform evalUi;
    Transform revealUi;
    Transform switchUi;
    [SerializeField]
    public List<Transform> doors = new List<Transform>();
    [SerializeField]
    GameObject buttonPrefab;
    List<GameObject> buttonList = new List<GameObject>();

    // Timer
    bool revealTimerIsRunning = false;
    float revealTimeRemaining = 0;
    Text revealTimer;

    // Internals
    int priceIndex;
    int choiceIndex;
    int offerIndex;
    bool makeNewOffer;
    bool createDoorButtons = true;
    int trialCount = 0;
    int winCount = 0;
    float winBlockTimer = 0f;

    void Start()
    {
        revealTimer = GameObject.Find("RevealTimer").GetComponent<Text>();
        choiceUi = GameObject.Find("ChoiceUi").transform;
        revealUi = GameObject.Find("RevealUi").transform;
        switchUi = GameObject.Find("SwitchUi").transform;
        evalUi = GameObject.Find("EvalUi").transform;
        StartGame();
    }
    void StartGame()
    {
        makeNewOffer = true;
        InitializePrice();
        ShowChoiceUi();
    }
    void InitializePrice()
    {
        priceIndex = Random.Range(0, doors.Count);
        for (int i = 0; i < doors.Count; i++)
        {
            doors[i].Find("WallFront").gameObject.SetActive(true);
            if (i == priceIndex)
            {
                doors[i].Find("Car").gameObject.SetActive(true);
                doors[i].Find("Goat").gameObject.SetActive(false);
            }
            else
            {
                doors[i].Find("Car").gameObject.SetActive(false);
                doors[i].Find("Goat").gameObject.SetActive(true);
            }
        }

    }
    void ShowChoiceUi()
    {
        choiceUi.gameObject.SetActive(true);
        revealUi.gameObject.SetActive(false);
        switchUi.gameObject.SetActive(false);
        evalUi.gameObject.SetActive(false);

        if (createDoorButtons)
        {
            Transform buttonParent = choiceUi.Find("Buttons");
            for (int i = 0; i < doors.Count; i++)
            {
                GameObject btn = GameObject.Instantiate(buttonPrefab);
                var buttonNo = i;
                btn.GetComponent<Button>().onClick.AddListener(() => StartUiDoorButtonClicked(buttonNo));
                btn.transform.SetParent(buttonParent.transform, false);
                btn.name = "Button " + (char)(65 + i);
                btn.GetComponentInChildren<Text>().text = "Door " + (char)(65 + i);
                buttonList.Add(btn);
            }
            createDoorButtons = false;
        }
    }
    void ShowRevealUi()
    {
        if (!makeNewOffer)
        {
            ShowSwitchUi();
            return;
        }

        offerIndex = Monty(); // Get offer
        makeNewOffer = false;

        doors[offerIndex].Find("WallFront").gameObject.SetActive(false);
        revealTimeRemaining = 5f;
        revealTimerIsRunning = true;

        choiceUi.gameObject.SetActive(false);
        revealUi.gameObject.SetActive(true);
        switchUi.gameObject.SetActive(false);

        // Change choice letter
        Text headerText = revealUi.Find("HeaderText").GetComponent<Text>();
        headerText.text = headerText.text.Substring(0, headerText.text.Length - 1) + (char)(65 + choiceIndex);

        // Change offer letter
        Text modalText = revealUi.Find("ModalText").GetComponent<Text>();
        modalText.text = modalText.text.Substring(0, modalText.text.Length - 1) + (char)(65 + offerIndex);
    }
    void RunRevealTimer()
    {
        if (revealTimerIsRunning)
        {
            // TODO: Play sound?

            revealTimeRemaining -= Time.deltaTime;
            revealTimer.text = $"{revealTimeRemaining:#0.0} seconds.";
            if (revealTimeRemaining <= 0)
            {
                revealTimerIsRunning = false;
                revealUi.gameObject.SetActive(false);
                doors[offerIndex].Find("WallFront").gameObject.SetActive(false);
                ShowSwitchUi();
            }
        }
    }
    void ShowSwitchUi()
    {
        choiceUi.gameObject.SetActive(false);
        revealUi.gameObject.SetActive(false);
        switchUi.gameObject.SetActive(true);

        // Change choice letter
        Text headerText = switchUi.Find("HeaderText").GetComponent<Text>();
        headerText.text = headerText.text.Substring(0, headerText.text.Length - 1) + (char)(65 + choiceIndex);

        doors[offerIndex].Find("WallFront").gameObject.SetActive(true);
        switchUi.Find("Buttons/Yes").GetComponent<Button>().onClick.AddListener(() => ShowEvalUi());
        switchUi.Find("Buttons/No").GetComponent<Button>().onClick.AddListener(() => ShowChoiceUi());
    }
    void ShowEvalUi()
    {
        choiceUi.gameObject.SetActive(false);
        revealUi.gameObject.SetActive(false);
        switchUi.gameObject.SetActive(false);
        evalUi.gameObject.SetActive(true);
        var headerText = evalUi.Find("HeaderText").GetComponent<Text>();
        headerText.text = headerText.text.Substring(0, headerText.text.Length - 1) + (char)(65 + choiceIndex); // Change choice letter

        // Reveal all doors
        foreach (Transform d in doors)
            d.Find("WallFront").gameObject.SetActive(false);

        string winText = CheckWin() ? "You won!" : "Unfortunately, you did not win!";
        Text modalText = evalUi.Find("ModalText").GetComponent<Text>();
        modalText.text = winText;

        evalUi.Find("Buttons/Repeat").GetComponent<Button>().onClick.AddListener(() => StartGame());
        evalUi.Find("Buttons/Exit").GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene("00_Menu"));
    }
    void StartUiDoorButtonClicked(int buttonNo)
    {
        Debug.Log("Choice = " + buttonNo);
        choiceIndex = buttonNo;
        ShowRevealUi();
    }
    int Monty()
    {
        // Determine Monty's choice
        List<int> options = new();
        for (int i = 0; i < doors.Count; i++)
        {
            if (i != priceIndex && i != choiceIndex)
            {
                options.Add(i);
            }
        }
        var offer = options[Random.Range(0, options.Count)];
        Debug.Log("Offer = " + offer.ToString());
        return offer;
    }
    bool CheckWin()
    {

        if (winBlockTimer <= 0) trialCount += 1;
        print("Price = " + priceIndex.ToString());
        var won = false;
        if (choiceIndex == priceIndex)
        {
            print("Won!");
            if (winBlockTimer <= 0) winCount += 1;
            won = true;
        }
        else
        {
            print("Lost!");
        }
        UpdateWinText();
        winBlockTimer = 5.0f;
        return won;
    }
    void UpdateWinText()
    {
        float winRate = (float)winCount / trialCount;
        print(winRate);
        GameObject.Find("Rate").GetComponent<TMP_Text>().text = $"{winRate:#0.00}";
    }
    void Update()
    {
        if (winBlockTimer > 0)
            winBlockTimer -= Time.deltaTime;
        RunRevealTimer();
    }
}
