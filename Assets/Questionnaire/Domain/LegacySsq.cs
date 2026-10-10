using System.Collections.Generic;

namespace Questionnaire;

/// <summary>
/// Legacy SSQ stage list mirrored from CsqQuestionnaire.cs.
/// Use only for parity gate. New surveys should live in JSON only.
/// </summary>
public static class LegacySsq
{
    public const int StageCount = 6;

    public sealed class Stage
    {
        public string id;
        public string prompt;
        public string lowText;
        public string mediumText;
        public string highText;
    }

    public static readonly IReadOnlyList<Stage> Stages = new List<Stage>
    {
        new() { id = "nausea_a",
            prompt = "Do you experience nausea (e.g., stomach pain, acid reflux, or tension to vomit)?",
            lowText = "Absent Feeling", mediumText = "Moderate Feeling", highText = "Extreme Feeling" },
        new() { id = "nausea_b",
            prompt = "Do you experience dizziness (e.g., light-headedness or spinning feeling)?",
            lowText = "Absent Feeling", mediumText = "Moderate Feeling", highText = "Extreme Feeling" },
        new() { id = "vestibular_a",
            prompt = "Do you experience disorientation (e.g., spatial confusion or vertigo)?",
            lowText = "Absent Feeling", mediumText = "Moderate Feeling", highText = "Extreme Feeling" },
        new() { id = "vestibular_b",
            prompt = "Do you experience postural instability (i.e., imbalance)?",
            lowText = "Absent Feeling", mediumText = "Moderate Feeling", highText = "Extreme Feeling" },
        new() { id = "oculomotor_a",
            prompt = "Do you experience a visually induced fatigue (e.g., feeling of tiredness or sleepiness)?",
            lowText = "Absent Feeling", mediumText = "Moderate Feeling", highText = "Extreme Feeling" },
        new() { id = "oculomotor_b",
            prompt = "Do you experience a visually induced discomfort (e.g., eyestrain, blurred vision, or headache)?",
            lowText = "Absent Feeling", mediumText = "Moderate Feeling", highText = "Extreme Feeling" }
    };
}
