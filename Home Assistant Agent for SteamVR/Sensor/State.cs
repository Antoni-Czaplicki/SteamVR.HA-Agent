using Newtonsoft.Json;
using Valve.VR;

namespace Home_Assistant_Agent_for_SteamVR.Sensor
{
    class State
    {
        public State(bool isOpenVRConnected, EDeviceActivityLevel HMDActivityLevel = EDeviceActivityLevel.k_EDeviceActivityLevel_Unknown, string? currentApplicationKey = null, string? currentApplicationName = null, Controller? rightController = null, Controller? leftController = null, string? error = null)
        {
            this.isOpenVRConnected = isOpenVRConnected;
            this.HMDActivityLevel = HMDActivityLevel;
            this.currentApplicationKey = currentApplicationKey;
            this.currentApplicationName = currentApplicationName;
            this.rightController = rightController;
            this.leftController = leftController;
            this.error = error;
        }

        [JsonProperty(PropertyName = "is_openvr_connected")]
        public bool isOpenVRConnected { get; set; } = false;

        [JsonProperty(PropertyName = "hmd_activity_level")]
        public EDeviceActivityLevel HMDActivityLevel { get; set; }

        [JsonProperty(PropertyName = "current_application_key")]
        public string? currentApplicationKey { get; set; }

        [JsonProperty(PropertyName = "current_application_name")]
        public string? currentApplicationName { get; set; }

        [JsonProperty(PropertyName = "right_controller")]
        public Controller? rightController { get; set; }

        [JsonProperty(PropertyName = "left_controller")]
        public Controller? leftController { get; set; }

        [JsonIgnore]
        public string error { get; set; } = "";
    }

    public class Controller
    {
        public Controller(bool isConnected = false, int? batteryPercentage = null, bool? isCharging = null)
        {
            this.isConnected = isConnected;
            this.batteryPercentage = batteryPercentage;
            this.isCharging = isCharging;
        }

        [JsonProperty(PropertyName = "is_connected")]
        public bool isConnected { get; set; } = false;

        [JsonProperty(PropertyName = "battery_percentage")]
        public int? batteryPercentage { get; set; }

        [JsonProperty(PropertyName = "is_charging")]
        public bool? isCharging { get; set; }
    }
}
