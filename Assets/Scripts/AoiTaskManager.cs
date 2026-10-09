using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// Config-driven visual-search task on top of the AoI pipeline (idea ported
/// from Supermarket-Sim's XML Experiment, modernized to JSON + AoI areas).
/// A task file lists trials; a trial completes when the raycaster's committed
/// AOI equals the target (works with feature-map areas AND with
/// wholeObjectAoi, where the target is an object name like "DemoBox_2").
/// Writes one row per trial to <sessionPrefix>-tasks.csv next to the AoI
/// recordings. Requires aoiLogging so the session folder exists.
/// </summary>
[RequireComponent(typeof(FeatureMapRaycaster))]
public class AoiTaskManager : MonoBehaviour
{
    [Serializable]
    class Task
    {
        public string target;
        public float maxSearchSec = 30f;
    }

    [Serializable]
    class TaskFile
    {
        public List<Task> trials = new List<Task>();
    }

    [SerializeField] TextAsset taskFile;  // JSON: {"trials":[{"target":"DemoBox_2","maxSearchSec":30}]}
    [SerializeField] bool autostart = true;
    [SerializeField] TMP_Text trialDisplay;   // optional VR HUD: trial x/y + current target
    [SerializeField] float interTrialDelay = 0f; // s of gap after a trial (RT validity: next trial starts with fresh gaze)
    [Header("Experiment conditions")]
    [SerializeField] bool randomizeTrials = false; // shuffle trial order
    [SerializeField] int randomSeed = 0;           // 0 = seed from device clock; seed is logged per row
    [SerializeField] FeatureMapSpawner spawner;    // optional: re-arrange products between trials
    [SerializeField] bool shuffleBetweenTrials = false;
    [SerializeField] bool heatmapPerTrial = false; // snapshot heatmaps as -trialN PNGs
    [SerializeField] AudioClip foundClip;          // optional feedback on trial end
    [SerializeField] AudioClip timeoutClip;

    FeatureMapRaycaster _raycaster;
    List<Task> _trials;
    StreamWriter _writer;
    long _trialStartMs;
    long _nextTrialAtMs;
    int _index;
    int _seedUsed;
    bool _running;

    void Awake()
    {
        _raycaster = GetComponent<FeatureMapRaycaster>();
        if (taskFile == null || string.IsNullOrEmpty(taskFile.text))
        {
            Debug.LogError("AoiTaskManager: no task file assigned.");
            return;
        }
        _trials = JsonUtility.FromJson<TaskFile>(taskFile.text)?.trials;
        if (_trials == null || _trials.Count == 0)
        {
            Debug.LogError("AoiTaskManager: task file has no trials.");
            return;
        }
        if (randomizeTrials)
        {
            _seedUsed = randomSeed != 0 ? randomSeed : Environment.TickCount;
            var rnd = new System.Random(_seedUsed);
            for (int i = _trials.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (_trials[i], _trials[j]) = (_trials[j], _trials[i]);
            }
        }
    }

    // Deferred one frame so the raycaster's Start (which creates the session
    // folder) has run, whatever the Start order between the two components.
    System.Collections.IEnumerator Start()
    {
        yield return null;
        if (autostart && _trials != null)
            Begin();
    }

    public void Begin()
    {
        if (_writer != null || _running)
            return; // already started or between trials - no double-start
        if (string.IsNullOrEmpty(_raycaster.SessionLogPath))
        {
            Debug.LogError("AoiTaskManager: no AoI session folder - enable aoiLogging on the raycaster.");
            return;
        }
        string path = Path.Combine(_raycaster.SessionLogPath,
            _raycaster.SessionPrefix + "-tasks.csv");
        _writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(true)); // utf-8-sig, like the AoI CSVs
        _writer.WriteLine("StartEpochMs;LogTime;Target;Found;SearchSec;Seed");
        _index = 0;
        StartTrial();
    }

    void StartTrial()
    {
        _trialStartMs = NowMs();
        _running = true;
        if (trialDisplay != null)
            trialDisplay.text = $"Trial {_index + 1}/{_trials.Count}\nFind: {_trials[_index].target}";
        Debug.Log($"[AoI-Task] trial {_index + 1}/{_trials.Count}: find '{_trials[_index].target}'");
    }

    void Update()
    {
        if (_nextTrialAtMs != 0 && NowMs() >= _nextTrialAtMs)
        {
            _nextTrialAtMs = 0;
            StartTrial();
            return;
        }
        if (!_running)
            return;
        Task t = _trials[_index];
        if (_raycaster.CurrentArea == t.target)
            Finish(true);
        else if ((NowMs() - _trialStartMs) / 1000f >= t.maxSearchSec)
            Finish(false);
    }

    void Finish(bool found)
    {
        Task t = _trials[_index];
        float sec = (NowMs() - _trialStartMs) / 1000f;
        _writer.WriteLine($"{NowMs()};{LogTime()};{t.target};{found};{sec.ToString("F3", CultureInfo.InvariantCulture)};{_seedUsed}");
        Debug.Log($"[AoI-Task] target '{t.target}': {(found ? $"found after {sec:F1}s" : "timeout")}");
        if (found ? foundClip != null : timeoutClip != null)
            AudioSource.PlayClipAtPoint(found ? foundClip : timeoutClip, transform.position);
        if (heatmapPerTrial)
            _raycaster.SaveHeatmapSnapshot($"-trial{_index + 1}");
        _index++;
        if (_index < _trials.Count)
        {
            if (shuffleBetweenTrials && spawner != null)
                spawner.ShuffleRow(_seedUsed + _index); // deterministic per trial index
            if (interTrialDelay > 0f)
            {
                _running = false;
                _nextTrialAtMs = NowMs() + (long)(interTrialDelay * 1000f);
                if (trialDisplay != null)
                    trialDisplay.text = $"Next trial in {interTrialDelay:F0}s";
            }
            else
            {
                StartTrial();
            }
        }
        else
        {
            Debug.Log("[AoI-Task] all trials complete");
            if (trialDisplay != null)
                trialDisplay.text = "Task complete";
            _running = false;
        }
    }

    void OnDestroy()
    {
        _writer?.Dispose();
        _writer = null;
    }

    static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    static string LogTime() => DateTime.Now.ToString("HH:mm:ss.fff");
}
