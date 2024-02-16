using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GazeEventDetection : MonoBehaviour
{
    // Eye-tracking
    private EyeTrackingManager _eyeTracking = null;
    private Queue<GazeRecord> _gazeRecordQueue = new();

    private float _minBlinkDurationThreshold = 0.01f; // 10ms blink minimum (2 invalid observations at 200 Hz)
    private float _minFixationDurationThreshold = 0.1f; // 100ms fixation duration minimum
    private float _saccadeCombineTheshold = 0.1f; // combine two subsequent saccades within 100ms
    private float _velocityThreshold = 50.0f; // 50°/s minimal angular speed for saccades (see Holmquist p. 231)

    private List<GazeEvent> _eventList = new();
    private GazeEvent _currentGazeEvent = null;
    private GazeRecord _lastGazeRecord = new();


    // Logging
    public bool logging = false;
    private StreamWriter _writer = null;
    private string[] _logData = new string[6];
    private static readonly string[] _columnNames = { "CaptureTimestamp", "LogTime", "EventType", "DurationInSec", "Velocity", "GazeTarget" };
    [Header("Default path is Recordings in application folder.")]
    [SerializeField] private bool _useCustomLogPath = false;
    [SerializeField] private string _customLogPath = "";

    public enum EventType
    {
        Init, // At trail start
        Undefined, // After blink
        Blink,
        Fixation,
        Saccade
    }

    // Event backlog as class for serialization
    // Gaze event during and after post processing
    [Serializable]
    public class GazeEventBacklog
    {
        public List<GazeEvent> eventBacklog = new(); // Backlog for further eval of previous gaze events
    }

    public float backlogDelay = 5.0f; // Time for events to remain in event list
    public GazeEventBacklog gazeEventBacklog = new();

    // Gaze event during and after post processing
    [Serializable]
    public class GazeEvent
    {
        public EventType eventType { get; set; }
        public long start { get; set; }
        public DateTime logTime { get; set; }
        public float duration { get; set; }
        public float velocity { get; set; }
        public List<float> velocityList { get; set; }
        public string gazeTarget { get; set; }

        public GazeEvent()
        {
            start = long.MaxValue;
            logTime = DateTime.Now;
            eventType = EventType.Init;
            velocity = -1;
            velocityList = new();
            gazeTarget = "";
        }

        public GazeEvent(long _start, EventType _type)
        {
            start = _start;
            logTime = DateTime.Now;
            eventType = _type;
            velocity = -1;
            velocityList = new();
            gazeTarget = "";
        }
        public GazeEvent(long _start, DateTime _logTime, EventType _type, string _gazeTarget)
        {
            start = _start;
            logTime = _logTime;
            eventType = _type;
            velocity = -1;
            velocityList = new();
            gazeTarget = _gazeTarget;
        }
    }

    // Raw ET record extract from Varjo HMD
    public struct GazeRecord
    {
        public long captureTime;
        public DateTime logDate;
        public bool valid;
        public Vector3 hmdPosition;
        public Vector3 gazeDirection;
        public string gazeTarget;
    }
    float slowUpdateRate = 0.2f;

    private void Start()
    {
        if (TryGetComponent<EyeTrackingManager>(out _eyeTracking))
        {
            // _eyeTracking.OnGazeRecordProcessing += QueueGazeRecord;
            StartLogging();
        }
        InvokeRepeating("SlowUpdate", 0.0f, slowUpdateRate);
    }

    // private void ReceiveEyeTrackingSample(object sender, EyeTracking.OnEyeTrackingDataArgs e)
    // {
    //     GazeRecordQueue.Enqueue(e.gazeRecord);
    // }
    private void QueueGazeRecord(GazeRecord gazeRecord) => _gazeRecordQueue.Enqueue(gazeRecord);


    private void SlowUpdate()
    {
        while (_gazeRecordQueue.Count > 0)
        {
            GazeRecord currentGaze = _gazeRecordQueue.Dequeue();

            ProcessGazeEvent(currentGaze);
        }
        LogGazeEventData();
        ProcessEventBacklog();
    }

    public static float TimeDeltaInSec(GazeRecord firstRecord, GazeRecord secondRecord)
    {
        // Convert to Second: 1 sec = 1E9 nanoseconds
        return (float)((secondRecord.captureTime - firstRecord.captureTime) / 1E9F);
    }

    public static float TimeDeltaInSec(long firstStart, long secondStart)
    {
        // Convert to Second: 1 sec = 1E9 nanoseconds
        return (float)((secondStart - firstStart) / 1E9F);
    }

    public static float GetAngularVelocity(GazeRecord firstRecord, GazeRecord secondRecord, float timeDeltaInSec)
    {
        // Angular velocity in °/s (degrees per second)
        return Vector3.Angle(firstRecord.gazeDirection, secondRecord.gazeDirection) / timeDeltaInSec;
    }

    private void ProcessGazeEvent(GazeRecord currentGazeRecord)
    {
        if (_currentGazeEvent == null)
        { // Create new event object on start
            _currentGazeEvent = new GazeEvent(currentGazeRecord.captureTime, EventType.Init);
            _currentGazeEvent.start = currentGazeRecord.captureTime;
            _lastGazeRecord = currentGazeRecord;
            return;
        }

        float timeDeltaInSec = TimeDeltaInSec(_lastGazeRecord, currentGazeRecord);
        _currentGazeEvent.duration += timeDeltaInSec;

        if (_lastGazeRecord.valid)
        {
            if (currentGazeRecord.valid)
            {
                float velocity = GetAngularVelocity(_lastGazeRecord, currentGazeRecord, timeDeltaInSec);
                if (velocity > _velocityThreshold)
                { // Saccade
                    if (_currentGazeEvent.eventType == EventType.Init)
                    {
                        _currentGazeEvent.start = currentGazeRecord.captureTime;
                        _currentGazeEvent.duration = timeDeltaInSec;
                        _currentGazeEvent.eventType = EventType.Saccade;
                    }
                    else if (_currentGazeEvent.eventType == EventType.Undefined)
                    {
                        _currentGazeEvent.eventType = EventType.Saccade;
                    }
                    if (_currentGazeEvent.eventType != EventType.Saccade)
                    {
                        AddEventToQueue(_currentGazeEvent);
                        _currentGazeEvent = new GazeEvent(currentGazeRecord.captureTime, EventType.Saccade);
                    }
                    _currentGazeEvent.velocityList.Add(velocity);
                }
                else
                { // Fixation
                    if (_currentGazeEvent.eventType == EventType.Init)
                    {
                        _currentGazeEvent.start = currentGazeRecord.captureTime;
                        _currentGazeEvent.duration = timeDeltaInSec;
                        _currentGazeEvent.eventType = EventType.Fixation;
                    }
                    else if (_currentGazeEvent.eventType == EventType.Undefined)
                    {
                        _currentGazeEvent.eventType = EventType.Fixation;
                    }
                    if (_currentGazeEvent.eventType != EventType.Fixation)
                    {
                        AddEventToQueue(_currentGazeEvent);
                        _currentGazeEvent = new GazeEvent(currentGazeRecord.captureTime, currentGazeRecord.logDate, EventType.Fixation, currentGazeRecord.gazeTarget);
                    }
                }
            }
            else
            {   // last valid and current invalid
                // begin of a blink
                if (_currentGazeEvent.eventType != EventType.Init && _currentGazeEvent.eventType != EventType.Undefined)
                    AddEventToQueue(_currentGazeEvent);
                _currentGazeEvent = new GazeEvent(currentGazeRecord.captureTime, EventType.Undefined);
            }
        }
        else
        {  // last invalid
            if (currentGazeRecord.valid)
            { // last invalid and current valid
                if (_currentGazeEvent.eventType != EventType.Init)
                {
                    if (_currentGazeEvent.duration > _minBlinkDurationThreshold)
                    {
                        _currentGazeEvent.eventType = EventType.Blink; // invalid -> valid: must be blink
                        _eventList.Add(_currentGazeEvent);
                    }
                    _currentGazeEvent = new GazeEvent(currentGazeRecord.captureTime, EventType.Undefined); // type unknown
                }
            }
        }
        _lastGazeRecord = currentGazeRecord;
    }

    private void AddEventToQueue(GazeEvent currentGazeEvent)
    {
        if (currentGazeEvent.eventType == EventType.Fixation && currentGazeEvent.duration < _minFixationDurationThreshold)
            return; // Discard too short fixation

        if (currentGazeEvent.eventType == EventType.Saccade)
        {
            if (_eventList.Any()) // only if list contains an element
            {
                // combine short saccades
                GazeEvent lastGazeEvent = _eventList[_eventList.Count - 1];
                if (lastGazeEvent.eventType == EventType.Saccade && TimeDeltaInSec(lastGazeEvent.start, currentGazeEvent.start) < _saccadeCombineTheshold)
                {
                    currentGazeEvent.start = lastGazeEvent.start;
                    currentGazeEvent.duration += lastGazeEvent.duration;
                    currentGazeEvent.velocityList.Concat(lastGazeEvent.velocityList);
                    _eventList.RemoveAt(_eventList.Count - 1);
                }
            }
            currentGazeEvent.velocity = currentGazeEvent.velocityList.Average();
        }
        _eventList.Add(currentGazeEvent);
        gazeEventBacklog.eventBacklog.Add(currentGazeEvent);
    }

    private void ProcessEventBacklog()
    {
        for (int i = 0; i < gazeEventBacklog.eventBacklog.Count; i++)
        {
            GazeEvent g = gazeEventBacklog.eventBacklog[i];
            if ((DateTime.Now - g.logTime).TotalSeconds > backlogDelay)
                gazeEventBacklog.eventBacklog.RemoveAt(i);
        }
    }

    private void LogGazeEventData()
    {
        for (int i = 0; i < _eventList.Count; i++)
        {
            GazeEvent g = _eventList[i];
            _logData[0] = g.start.ToString();
            _logData[1] = g.logTime.ToString("HH:mm:ss.fff");
            _logData[2] = g.eventType.ToString();
            _logData[3] = g.duration.ToString();
            _logData[4] = g.velocity.ToString();
            _logData[5] = g.gazeTarget.ToString();
            Log(_logData);
            _eventList.RemoveAt(i);
        }
    }

    private void Log(string[] values)
    {
        if (!logging || _writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        _writer.WriteLine(line);
    }

    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        logging = true;

#if UNITY_EDITOR
        // Don't save in Assets folder when running in Unity player
        string logPath = _useCustomLogPath ? _customLogPath : Application.dataPath + "/../Recordings/";
#else
        string logPath = _useCustomLogPath ? _customLogPath : Application.dataPath + "/Recordings/";
#endif
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}h{4:00}m-{5}-gaze-events", now.Year, now.Month, now.Day, now.Hour, now.Minute, SceneManager.GetActiveScene().name);

        string path = logPath + fileName + ".csv";
        _writer = new StreamWriter(path);

        Log(_columnNames);
        Debug.Log("Log file started at: " + path);
    }

    private void StopLogging()
    {
        if (!logging)
            return;

        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }
        logging = false;
        Debug.Log("Gaze Event Logging ended");
    }

    private void OnDestroy()
    {
        StopLogging();
    }
}
