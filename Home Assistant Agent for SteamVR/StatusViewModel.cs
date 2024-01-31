using System.ComponentModel;
using Microsoft.UI.Dispatching;

namespace Home_Assistant_Agent_for_SteamVR
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        private SuperSocket.ServerState _wsServerStatus;
        private bool _steamVRStatus;
        private bool _notifyPluginStatus;
        private DispatcherQueue _dispatcherQueue;

        public StatusViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }
        public SuperSocket.ServerState wsServerState
        {
            get { return _wsServerStatus; }
            set
            {
                if (_wsServerStatus != value)
                {
                    _wsServerStatus = value;
                    OnPropertyChanged(nameof(wsServerState));
                }
            }
        }

        public bool SteamVRStatus
        {
            get { return _steamVRStatus; }
            set
            {
                if (_steamVRStatus != value)
                {
                    _steamVRStatus = value;
                    OnPropertyChanged(nameof(SteamVRStatus));
                    OnPropertyChanged(nameof(SteamVRStatusText));
                }
            }
        }
        
        public string SteamVRStatusText
        {
            get { return _steamVRStatus ? "Connected" : "Disconnected"; }
        }

        public bool NotifyPluginStatus
        {
            get { return _notifyPluginStatus; }
            set
            {
                if (_notifyPluginStatus != value)
                {
                    _notifyPluginStatus = value;
                    OnPropertyChanged(nameof(NotifyPluginStatus));
                    OnPropertyChanged(nameof(NotifyPluginStatusText));
                }
            }
        }
        
        public string NotifyPluginStatusText
        {
            get { return _notifyPluginStatus ? "Running" : "Stopped"; }
        }
        
        public bool IsNotifyPluginEnabled
        {
            get { return Settings.Default.EnableNotifyPlugin; }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}