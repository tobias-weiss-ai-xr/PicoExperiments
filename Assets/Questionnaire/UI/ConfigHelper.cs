using Questionnaire;
using System.Collections.Generic;

namespace Questionnaire.UI
{
    /// <summary>Helper for safe config dictionary access.</summary>
    public static class ConfigHelper
    {
        public static string Get(this Dictionary<string, string> config, string key, string defaultValue = "")
            => config != null && config.TryGetValue(key, out var v) ? v : defaultValue;
    }
}
