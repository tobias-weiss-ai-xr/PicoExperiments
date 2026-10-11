using Questionnaire;
using System;
using System.Globalization;
using System.IO;

namespace Questionnaire
{
    /// <summary>
    /// Pure C# CSV logger for surveys. No Unity dependencies.
    /// Use this for server-side or headless survey running.
    /// </summary>
    public sealed class SimpleCsvLogger : ISurveyLogger
    {
        private StreamWriter _writer;
        private string _path;
        private readonly string _participantId;
        private readonly string _scene;
        private int _flushCounter;
        private const int FlushInterval = 10;

        public SimpleCsvLogger(string directory, string surveyId, string participantId = "", string scene = "")
        {
            _participantId = participantId ?? "";
            _scene = scene ?? "headless";

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            _path = Path.Combine(directory, $"{timestamp}-{_scene}-questionnaire-{surveyId}.csv");
        }

        private const string Header = 
            "SubmittedAtEpochMs;LogTime;SurveyId;SurveyVersion;SectionId;ItemId;Type;Value;DurationSec;ParticipantId";

        public void Open(string participantId, string scene, string surveyId)
        {
            _writer = new StreamWriter(_path, append: false);
            _writer.WriteLine(Header);
            _writer.Flush();
        }

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
                fields[i] = fields[i].Replace("\r", " ").Replace("\n", " ");
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

        ~SimpleCsvLogger() => Close();
    }
}
