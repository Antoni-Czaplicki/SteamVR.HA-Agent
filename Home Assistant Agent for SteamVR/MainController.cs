using BOLL7708;
using Newtonsoft.Json;
using Home_Assistant_Agent_for_SteamVR.Notification;
using Home_Assistant_Agent_for_SteamVR.Sensor;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public NamedPipeServerStream PipeServer = new NamedPipeServerStream("HomeAssistantAgentPipe",
            PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);


        public MainController(StatusViewModel statusViewModel, Action<WebSocketSession, int> sessionHandler,
            Action<bool> openvrStatus)
        {
            _statusViewModel = statusViewModel;
            _openvrStatusAction = openvrStatus;
            InitServer(sessionHandler);
            var notificationsThread = new Thread(Worker);
            if (!notificationsThread.IsAlive) notificationsThread.Start();
            var sensorsThread = new Thread(SensorsWorker);
            if (!sensorsThread.IsAlive) sensorsThread.Start();
            PipeServer.BeginWaitForConnection(new AsyncCallback((ar) =>
            {
                Debug.WriteLine("Pipe connected");
                _statusViewModel.NotifyPluginStatus = true;
                ReadFromPipe();
            }), null);
        }

        private void SensorsWorker()
        {
            Thread.CurrentThread.IsBackground = true;
            const uint INVALID_INDEX_VALUE = 4294967295;
            while (true)
            {
                if (_steamVRShutDown) return;
                if (_steamVRConnected)
                {
                    var runningApplicationId = _vr.GetRunningApplicationId();
                    var rightControllerIndex = _vr.GetIndexForControllerRole(ETrackedControllerRole.RightHand);
                    var leftControllerIndex = _vr.GetIndexForControllerRole(ETrackedControllerRole.LeftHand);
                    var rightController = (rightControllerIndex == INVALID_INDEX_VALUE)
                        ? new Controller(false)
                        : new Controller(true,
                            (int)Math.Round(_vr.GetFloatTrackedDeviceProperty(rightControllerIndex,
                                ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float) * 100),
                            _vr.GetBooleanTrackedDeviceProperty(rightControllerIndex,
                                ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool));
                    var leftController = (leftControllerIndex == INVALID_INDEX_VALUE)
                        ? new Controller(false)
                        : new Controller(true,
                            (int)Math.Round(_vr.GetFloatTrackedDeviceProperty(leftControllerIndex,
                                ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float) * 100),
                            _vr.GetBooleanTrackedDeviceProperty(leftControllerIndex,
                                ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool));
                    _server.SendMessageToAll(JsonConvert.SerializeObject(new State(true,
                        _vr.GetTrackedDeviceActivityLevel(0), runningApplicationId,
                        _vr.GetApplicationPropertyString(runningApplicationId, EVRApplicationProperty.Name_String),
                        rightController, leftController)));
                }
                else
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
                        _vr.AddApplicationManifest(
                            Windows.ApplicationModel.Package.Current.InstalledPath + "\\app.vrmanifest",
                            "antek.steamvr_ha_agent", true);
                        _openvrStatusAction.Invoke(true);
                        RegisterEvents();
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

                if (_steamVRShutDown)
                {
                    if (!_steamVRConnected) return;
                    try
                    {
                        _vr.AcknowledgeShutdown();
                        Thread.Sleep(500); // Allow things to deinit properly
                        _vr.Shutdown();
                        _openvrStatusAction.Invoke(false);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"OpenVR Shutdown Error: {e.Message}");
                    }

                    return;
                }

                _statusViewModel.wsServerState = _server.GetState();
            }
        }

        private void RegisterEvents()
        {
            _vr.RegisterEvent(EVREventType.VREvent_Quit, (data) =>
            {
                _steamVRConnected = false;
                _steamVRShutDown = true;
            });
        }

        private void PostNotification(WebSocketSession session, Payload payload)
        {
            // Overlay
            Session.OverlayHandles.TryGetValue(session, out ulong overlayHandle);
            if (overlayHandle == 0L)
            {
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
                if (payload.imageData?.Length > 0)
                {
                    var imageBytes = Convert.FromBase64String(payload.imageData);
                    bmp = new Bitmap(new MemoryStream(imageBytes));
                }
                else if (payload.imagePath.Length > 0)
                {
                    bmp = new Bitmap(payload.imagePath);
                }

                if (bmp != null)
                {
                    Debug.WriteLine($"Bitmap size: {bmp.Size.ToString()}");
                    bitmap = BitmapUtils.NotificationBitmapFromBitmap(bmp, true);
                    bmp.Dispose();
                }
            }
            catch (Exception e)
            {
                var message = $"Image Read Failure: {e.Message}";
                Debug.WriteLine(message);
                _server.SendMessage(session,
                    JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, false, "image_read_error",
                        message)));
            }

            // Broadcast
            if (overlayHandle != 0)
            {
                GC.KeepAlive(bitmap);
                _vr.EnqueueNotification(overlayHandle, payload.basicMessage, bitmap);
            }
        }

        private void PostImageNotification(string sessionId, Payload payload)
        {
            if (!Settings.Default.EnableNotifyPlugin)
            {
                _server.SendMessage(Session.Sessions[sessionId],
                    JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, false,
                        "notify_plugin_disabled", "Notify plugin is not enabled")));
            }

            // TODO: Forward to external plugin
            if (PipeServer.IsConnected)
            {
                try
                {
                    Debug.WriteLine($"Pipe connected, writing to pipe...");
                    var payloadWithSessionId = new PayloadWithSessionId(payload, sessionId);
                    PipeServer.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payloadWithSessionId)));
                    PipeServer.Flush();
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Pipe write error: {e.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Pipe not connected");
            }
        }

        #endregion

        private void InitServer(Action<WebSocketSession, int> sessionHandler)
        {
            _server.SessionHandler = sessionHandler;
            _server.MessageReceivedAction = (session, payloadJson) =>
            {
                if (!Session.Sessions.ContainsKey(session.SessionID))
                {
                    Session.Sessions.TryAdd(session.SessionID, session);
                }

                var payload = new Payload();
                try
                {
                    payload = JsonConvert.DeserializeObject<Payload>(payloadJson);
                }
                catch (Exception e)
                {
                    var message = $"JSON Parsing Exception: {e.Message}";
                    Debug.WriteLine(message);
                    _server.SendMessage(session,
                        JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, false,
                            "json_parse_error", message)));
                }

                if (payload.type == "notification")
                {
                    if (payload.customProperties.enabled)
                    {
                        PostImageNotification(session.SessionID, payload);
                    }
                    else if (payload.basicMessage?.Length > 0)
                    {
                        PostNotification(session, payload);
                    }
                    else
                    {
                        _server.SendMessage(session,
                            JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, false,
                                "invalid_notification",
                                "Custom notification is not enabled and basic message is empty")));
                    }
                }
                else if (payload.type == "command")
                {
                    if (payload.command.StartsWith("vibrate_controller_"))
                    {
                        switch (payload.command)
                        {
                            case "vibrate_controller_right":
                                TriggerRepeatedHapticPulseInController(ETrackedControllerRole.RightHand, 50000, 50000,
                                    10);
                                break;
                            case "vibrate_controller_left":
                                TriggerRepeatedHapticPulseInController(ETrackedControllerRole.LeftHand, 50000, 50000,
                                    10);
                                break;
                            case "vibrate_controller_both":
                                TriggerRepeatedHapticPulseInController(ETrackedControllerRole.RightHand, 50000, 50000,
                                    10);
                                TriggerRepeatedHapticPulseInController(ETrackedControllerRole.LeftHand, 50000, 50000,
                                    10);
                                break;
                        }
                    }
                }
                else
                {
                    _server.SendMessage(session,
                        JsonConvert.SerializeObject(new Response(payload.customProperties.nonce, false, "invalid_type",
                            "Invalid payload type")));
                }

                return Task.CompletedTask;
            };
        }

        private async Task TriggerRepeatedHapticPulseInController(ETrackedControllerRole role, ushort duration,
            int pause, int repeat)
        {
            for (int i = 0; i < repeat; i++)
            {
                _vr.TriggerHapticPulseInController(role, duration);
                await Task.Delay(pause);
            }
        }

        public async void Start()
        {
            await _server.StartAsync(Settings.Default.Port);
        }

        public async void SetPort(int port, int oldPort)
        {
            if (port != oldPort)
            {
                _server.SendMessageToAll(JsonConvert.SerializeObject(new Event("port_changed", port.ToString())));
            }

            await _server.RestartAsync(port);
        }

        public async void Shutdown()
        {
            if (PipeServer.IsConnected)
            {
                await PipeServer.WriteAsync(Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new PayloadWithSessionId(new Payload() { type = "exit" }, ""))));
                await PipeServer.FlushAsync();
            }

            PipeServer.Close();
            await PipeServer.DisposeAsync();
            _openvrStatusAction = (status) => { };
            _server.ResetActions();
            _steamVRShutDown = true;
            await _server.StopAsync();
        }


        private async Task ReadFromPipe()
        {
            byte[] buffer = new byte[1024 * 1024];

            try
            {
                while (true)
                {
                    if (!PipeServer.IsConnected)
                    {
                        Debug.WriteLine("Pipe disconnected");
                        _statusViewModel.NotifyPluginStatus = false;
                        break;
                    }

                    var readTask = PipeServer.ReadAsync(buffer, 0, buffer.Length);
                    var result = await readTask;

                    if (readTask.IsCompletedSuccessfully)
                    {
                        if (result == 0)
                        {
                            PipeServer.Disconnect();
                            Debug.WriteLine("Pipe disconnected");
                            _statusViewModel.NotifyPluginStatus = false;
                            break;
                        }

                        string message = Encoding.UTF8.GetString(buffer, 0, result);
                        OnPipeMessageReceived(message);
                    }
                    else
                    {
                        Debug.WriteLine("Pipe read error");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Pipe read error: {e.Message}");
            }
            finally
            {
                PipeServer.BeginWaitForConnection(new AsyncCallback((ar) =>
                {
                    Debug.WriteLine("Pipe connected");
                    _statusViewModel.NotifyPluginStatus = true;
                    ReadFromPipe();
                }), null);
            }

            Debug.WriteLine("Pipe read complete");
        }

        private void OnPipeMessageReceived(string message)
        {
            try
            {
                var responseWithSessionId = JsonConvert.DeserializeObject<ResponseWithSessionId>(message);
                if (responseWithSessionId.sessionID == null ||
                    _server.IsSessionConnected(responseWithSessionId.sessionID))
                {
                    _server.SendMessageToAll(JsonConvert.SerializeObject(responseWithSessionId.response));
                }
                else
                {
                    _server.SendMessage(Session.Sessions[responseWithSessionId.sessionID],
                        JsonConvert.SerializeObject(responseWithSessionId.response));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Pipe message error: {e.Message}");
            }
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