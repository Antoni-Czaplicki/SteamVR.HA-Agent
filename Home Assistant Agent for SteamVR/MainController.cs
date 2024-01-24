using BOLL7708;
using Newtonsoft.Json;
using Home_Assistant_Agent_for_SteamVR.Notification;
using Home_Assistant_Agent_for_SteamVR.Sensor;
using SuperSocket.WebSocket;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using Valve.VR;
using static BOLL7708.EasyOpenVRSingleton;
using SuperSocket.WebSocket.Server;

namespace Home_Assistant_Agent_for_SteamVR
{
    class MainController
    {
        //public static Dispatcher UiDispatcher { get; private set; }
        private readonly EasyOpenVRSingleton _vr = EasyOpenVRSingleton.Instance;
        private readonly SuperServer _server = new SuperServer();
        private Action<bool> _openvrStatusAction;
        private bool _steamVRConnected = false;
        private bool _steamVRShutDown = false;
        private StatusViewModel _statusViewModel;


        public MainController(StatusViewModel statusViewModel, Action<WebSocketSession, int> sessionHandler, Action<bool> openvrStatus)
        {
            _statusViewModel = statusViewModel;
            _openvrStatusAction = openvrStatus;
            InitServer(sessionHandler);
            var notificationsThread = new Thread(Worker);
            if (!notificationsThread.IsAlive) notificationsThread.Start();
            var sensorsThread = new Thread(SensorsWorker);
            if (!sensorsThread.IsAlive) sensorsThread.Start();
        }

        private void SensorsWorker()
        {
            Thread.CurrentThread.IsBackground = true;
            const uint INVALID_INDEX_VALUE = 4294967295;
            while (true)
            {
                if (_steamVRConnected)
                {
                    var runningApplicationId = _vr.GetRunningApplicationId();
                    var rightControlerIndex = _vr.GetIndexForControllerRole(ETrackedControllerRole.RightHand);
                    var leftControlerIndex = _vr.GetIndexForControllerRole(ETrackedControllerRole.LeftHand);
                    var rightController = (rightControlerIndex == INVALID_INDEX_VALUE) ? new Controller(false) : new Controller(true, (int)Math.Round(_vr.GetFloatTrackedDeviceProperty(rightControlerIndex, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float) * 100), _vr.GetBooleanTrackedDeviceProperty(rightControlerIndex, ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool));
                    var leftController = (leftControlerIndex == INVALID_INDEX_VALUE) ? new Controller(false) : new Controller(true, (int)Math.Round(_vr.GetFloatTrackedDeviceProperty(leftControlerIndex, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float) * 100), _vr.GetBooleanTrackedDeviceProperty(leftControlerIndex, ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool));
                    _server.SendMessageToAll(JsonConvert.SerializeObject(new State(true, _vr.GetTrackedDeviceActivityLevel(0), runningApplicationId, _vr.GetApplicationPropertyString(runningApplicationId, EVRApplicationProperty.Name_String), rightController, leftController)));
                } else
                {
                    _server.SendMessageToAll(JsonConvert.SerializeObject(new State(false)));
                }
                Thread.Sleep(1000);
            }
        }

        #region openvr
        private void Worker()
        {
            var initComplete = false;

            Thread.CurrentThread.IsBackground = true;
            while (true)
            {
                if (_steamVRConnected)
                {
                    if (!initComplete)
                    {
                        initComplete = true;
                        _vr.AddApplicationManifest(Windows.ApplicationModel.Package.Current.InstalledPath + "\\app.vrmanifest", "antek.steamvr_ha_agent", true);
                        _openvrStatusAction.Invoke(true);
                        RegisterEvents();
                        _vr.SetDebugLogAction((message) =>
                        {
                            _server.SendMessageToAll(JsonConvert.SerializeObject(new Response("", "Debug", message)));
                        });
                    }
                    else
                    {
                        _vr.UpdateEvents(false);
                    }
                    Thread.Sleep(250);
                }
                else
                {
                    if (!_steamVRConnected)
                    {
                        Debug.WriteLine("Initializing OpenVR...");
                        _steamVRConnected = _vr.Init();
                    }
                    Thread.Sleep(2000);
                }
                if (_steamVRShutDown) {
                    _steamVRShutDown = false;
                    initComplete = false;
                    // _vr.AcknowledgeShutdown();
                    Thread.Sleep(500); // Allow things to deinit properly
                    _vr.Shutdown();
                    _openvrStatusAction.Invoke(false);
                }
                _statusViewModel.wsServerState = _server.GetState();
            }            
        }

        private void RegisterEvents() {
            _vr.RegisterEvent(EVREventType.VREvent_Quit, (data) => {
                _steamVRConnected = false;
                _steamVRShutDown = true;
            });
        }

        private void PostNotification(WebSocketSession session, Payload payload)
        {
            // Overlay
            Session.OverlayHandles.TryGetValue(session, out ulong overlayHandle);
            if (overlayHandle == 0L) {
                if (Session.OverlayHandles.Count >= 32) Session.OverlayHandles.Clear(); // Max 32, restart!
                overlayHandle = _vr.InitNotificationOverlay(payload.basicTitle);
                Session.OverlayHandles.TryAdd(session, overlayHandle);
            }

            // Image
            Debug.WriteLine($"Overlay handle {overlayHandle} for '{payload.basicTitle}'");
            Debug.WriteLine($"Image Hash: {CreateMD5(payload.imageData)}");
            NotificationBitmap_t bitmap = new NotificationBitmap_t();
            try
            {
                Bitmap bmp = null;
                if (payload.imageData?.Length > 0) {
                    var imageBytes = Convert.FromBase64String(payload.imageData);
                    bmp = new Bitmap(new MemoryStream(imageBytes));
                } else if(payload.imagePath.Length > 0)
                {
                    bmp = new Bitmap(payload.imagePath);
                }
                if (bmp != null) {
                    Debug.WriteLine($"Bitmap size: {bmp.Size.ToString()}");
                    bitmap = BitmapUtils.NotificationBitmapFromBitmap(bmp, true);
                    bmp.Dispose();
                }
            }
            catch (Exception e)
            {
                var message = $"Image Read Failure: {e.Message}";
                Debug.WriteLine(message);
                _server.SendMessage(session, JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, "Error", message)));
            }
            // Broadcast
            if(overlayHandle != 0)
            {
                GC.KeepAlive(bitmap);
                _vr.EnqueueNotification(overlayHandle, payload.basicMessage, bitmap);
            }
        }

        private void PostImageNotification(string sessionId, Payload payload)
        {
            // TODO: Forward to external plugin
        }

        #endregion

        private void InitServer(Action<WebSocketSession, int> sessionHandler)
        {
            _server.SessionHandler = sessionHandler;
            _server.MessageReceievedAction = (session, payloadJson) =>
            {
                if (!Session.Sessions.ContainsKey(session.SessionID)) {
                    Session.Sessions.TryAdd(session.SessionID, session);
                }
                var payload = new Payload();
                try { payload = JsonConvert.DeserializeObject<Payload>(payloadJson); }
                catch (Exception e) {
                    var message = $"JSON Parsing Exception: {e.Message}";
                    Debug.WriteLine(message);
                    _server.SendMessage(session, JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, "Error", message)));
                }
                // Debug.WriteLine($"Payload was received: {payloadJson}");
                if (payload.customProperties.enabled)
                {
                    PostImageNotification(session.SessionID, payload);
                }
                else if (payload.basicMessage?.Length > 0)
                {
                    PostNotification(session, payload);
                }
                else {
                    _server.SendMessage(session, JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, "Error", "Payload appears to be missing data.")));
                }
            };
        }
        
        public void Start()
        {
            _server.StartAsync(Settings.Default.Port);
        }

        public void SetPort(int port)
        {
            _server.RestartAsync(port);
        }

        public void Shutdown()
        {
            _openvrStatusAction = (status) => { };
            _server.ResetActions();
            _steamVRShutDown = true;
            _server.StopAsync();
        }

        public static string CreateMD5(string input) // https://stackoverflow.com/a/24031467
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
