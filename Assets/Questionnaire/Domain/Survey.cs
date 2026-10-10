using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Questionnaire;

[Serializable]
public sealed class Survey
{
    public string id = string.Empty;
    public string title = string.Empty;
    public int version = 1;
    public List<Section> sections = new();
}

[Serializable]
public sealed class Section
{
    public string id = string.Empty;
    public string header = string.Empty;
    public List<Item> items = new();
}

[Serializable]
public sealed class Item
{
    public string id = string.Empty;
    public string prompt = string.Empty;
    public string type = string.Empty;
    public bool required = true;
    public Dictionary<string, string> config = new();

    [JsonIgnore]
    public bool ExitAfterSubmit =>
        config != null && config.TryGetValue("exitOnSubmit", out var v) && v == "true";
}
