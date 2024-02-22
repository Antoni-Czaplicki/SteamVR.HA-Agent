using Newtonsoft.Json;

namespace Home_Assistant_Agent_for_SteamVR;

internal class Event(string eventType, string eventData)
{
    [JsonProperty(PropertyName = "type")] public string Type = "event";

    [JsonProperty(PropertyName = "event_type")]
    public string EventType = eventType;

    [JsonProperty(PropertyName = "event_data")]
    public string EventData = eventData;
}