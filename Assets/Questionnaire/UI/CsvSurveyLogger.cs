using Questionnaire;
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Questionnaire.UI
{
    /// <summary>
    /// Logs survey responses to Recordings/<timestamp>-<scene>-questionnaire-<surveyId>.csv
    /// with the canonical header defined in the contract.
    /// </summary>
    public sealed class CsvSurveyLogger : ISurveyLogger
    {
        private StreamWriter _writer;
        private string _path;
        private int _flushCounter;
        private const int FlushInterval = 10;
        private string _participantId = "";
        private string _scene = "";

        public string Path => _path;

        public void Open(string participantId, string scene, string surveyId)
        {
            _participantId = participantId ?? "";
            _scene = scene ?? SceneManager.GetActiveScene().name;

            string dir = System.IO.Path.Combine(Application.persistentDataPath, "Recordings");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            DateTime now = DateTime.Now;
            string timestamp = now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            string filename = $"{timestamp}-{_scene}-questionnaire-{surveyId}.csv";
            _path = System.IO.Path.Combine(dir, filename);

            _writer = new StreamWriter(_path, append: false);
            _writer.WriteLine(Header);
            _writer.Flush();
        }

        private const string Header = 
            "SubmittedAtEpochMs;LogTime;SurveyId;SurveyVersion;SectionId;ItemId;Type;Value;DurationSec;ParticipantId";

        public void Log(in SurveyResponse response)
        {
            if (_writer == null) return;

            long nowEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string logTime = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

            string line = BuildCsvLine(
                nowEpochMs.ToString(CultureInfo.InvariantCulture),
                logTime,
                CsvSafe.Make(response.SurveyId),
                response.SurveyVersion.ToString(CultureInfo.InvariantCulture),
                CsvSafe.Make(response.SectionId),
                CsvSafe.Make(response.ItemId),
                CsvSafe.Make(response.Type),
                CsvSafe.Make(response.Value),
                response.DurationSec.ToString("F3", CultureInfo.InvariantCulture),
                CsvSafe.Make(_participantId)
            );

            _writer.WriteLine(line);
            _flushCounter++;
            if (_flushCounter >= FlushInterval)
            {
                _writer.Flush();
                _flushCounter = 0;
            }
        }

        private static string BuildCsvLine(params string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
                fields[i] = fields[i].Replace("\r", "").Replace("\n", " ");
            return string.Join(";", fields);
        }

        public void Close()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
                _writer = null;
            }
            _flushCounter = 0;
        }

        ~CsvSurveyLogger() => Close();
    }
}
