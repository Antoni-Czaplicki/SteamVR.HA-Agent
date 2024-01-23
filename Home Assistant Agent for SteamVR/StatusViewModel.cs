using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Home_Assistant_Agent_for_SteamVR
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        private SuperSocket.ServerState _wsServerStatus;
        private bool _steamVRStatus;
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