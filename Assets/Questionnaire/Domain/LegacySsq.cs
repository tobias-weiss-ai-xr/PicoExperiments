using System;
using System.Collections.Generic;

namespace Questionnaire
{
/// <summary>
/// Exact replication of the SSQ JSON fixture stages.
/// Used as a parity gate: SurveySchema.Parse + Items() must match.
/// </summary>
public static class LegacySsq
{
    public const int StageCount = 6;

    [Serializable]
    public class Stage
    {
        public string id;
        public string header;
        public string prompt;
        public string lowText = "Absent Feeling";
        public string mediumText = "Moderate Feeling";
        public string highText = "Extreme Feeling";
    }

    public static readonly List<Stage> Stages = new()
    {
        new Stage { id = "nausea_a",   header = "Nausea",   prompt = "Do you experience nausea (e.g., stomach pain, acid reflux, or tension to vomit)?" },
        new Stage { id = "nausea_b",   header = "Nausea",   prompt = "Do you experience dizziness (e.g., light-headedness or spinning feeling)?" },
        new Stage { id = "vestibular_a", header = "Vestibular", prompt = "Do you experience disorientation (e.g., spatial confusion or vertigo)?" },
        new Stage { id = "vestibular_b", header = "Vestibular", prompt = "Do you experience postural instability (i.e., imbalance)?" },
        new Stage { id = "oculomotor_a", header = "Oculomotor", prompt = "Do you experience a visually induced fatigue (e.g., feeling of tiredness or sleepiness)?" },
        new Stage { id = "oculomotor_b", header = "Oculomotor", prompt = "Do you experience a visually induced discomfort (e.g., eyestrain, blurred vision, or headache)?" }
    };
}
}
