using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    private TextMeshProUGUI timeText;
    public float TimeRemaining = 300;
    public bool TimerIsRunning = false;
    private void Start()
    {
        timeText = GetComponent<TextMeshProUGUI>();
    }

    public void StartTimer()
    {
        TimerIsRunning = true;
    }

    void Update()
    {
        if (TimerIsRunning)
        {
            if (TimeRemaining > 0)
            {
                TimeRemaining -= Time.deltaTime;
                DisplayTime(TimeRemaining);
            }
            else
            {
                Debug.Log("Time has run out!");
                TimeRemaining = 0;
                TimerIsRunning = false;
                timeText.color = Color.gray;
            }
        }
    }
    void DisplayTime(float timeToDisplay, float blinkStartTime = 20)
    {
        if (TimeRemaining <= blinkStartTime && (int)TimeRemaining % 2 == 0)
            timeText.color = Color.red;
        else
            timeText.color = Color.white;

        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
