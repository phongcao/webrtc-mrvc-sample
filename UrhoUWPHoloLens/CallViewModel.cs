using Org.WebRtc;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Urho.SharpReality;
using WebRTCUWP.Model;
using WebRTCUWP.MVVM;
using WebRTCUWP.Signalling;
using WebRTCUWP.Utilities;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace UrhoUWPHoloLens
{
    class CallViewModel : DispatcherBindableBase
    {
        public MediaElement SelfVideo;
        public MediaElement PeerVideo;

        private MediaVideoTrack _peerVideoTrack;
        private MediaVideoTrack _selfVideoTrack;

        private bool _cameraEnabled = true;
        private bool _microphoneIsOn = false;

        private bool _isHoloLens = false;

        private RawVideoSource _rawVideoSource;

        public CallViewModel(CoreDispatcher uiDispatcher) : base(uiDispatcher)
        {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Holographic")
            {
                _isHoloLens = true;
            }

            // We don't want the PC to send video to the HoloLens
            if (!_isHoloLens)
            {
                _cameraEnabled = false;
            }

            // Display a permission dialog to request access to the microphone and camera
            WebRTC.RequestAccessForMediaCapture().AsTask().ContinueWith(antecedent =>
            {
                if (antecedent.Result)
                {
                    Initialize(uiDispatcher);
                }
                else
                {
                    RunOnUiThread(async () =>
                    {
                        var msgDialog = new MessageDialog(
                            "Failed to obtain access to multimedia devices!");
                        await msgDialog.ShowAsync();
                    });
                }
            });
        }

        /// <summary>
        /// The initializer for MainViewModel.
        /// </summary>
        /// <param name="uiDispatcher">The UI dispatcher.</param>
        public void Initialize(CoreDispatcher uiDispatcher)
        {
            WebRTC.Initialize(uiDispatcher);

            // For the HoloLens
            if (_isHoloLens)
            {
                WebRTC.SetPreferredVideoCaptureFormat(896, 504, 30);
            }

            // Pick the codec
            var videoCodecs = WebRTC.GetVideoCodecs();
            foreach (var codec in videoCodecs)
            {
                if (codec.Name == "H264")
                {
                    Conductor.Instance.VideoCodec = codec;
                    break;
                }
            }

            // Pick the bitrate
            Conductor.Instance.VideoBitrate = 512;

            var settings = ApplicationData.Current.LocalSettings;
            
            // A Peer is connected to the server event handler
            Conductor.Instance.Signaller.OnPeerConnected += (peerId, peerName) =>
            {
                RunOnUiThread(() =>
                {
                    if (Peers == null)
                    {
                        Peers = new ObservableCollection<Peer>();
                        Conductor.Instance.Peers = Peers;
                    }
                    Peers.Add(new Peer { Id = peerId, Name = peerName });
                });
            };

            // A Peer is disconnected from the server event handler
            Conductor.Instance.Signaller.OnPeerDisconnected += peerId =>
            {
                RunOnUiThread(() =>
                {
                    var peerToRemove = Peers?.FirstOrDefault(p => p.Id == peerId);
                    if (peerToRemove != null)
                        Peers.Remove(peerToRemove);
                });
            };

            // The user is Signed in to the server event handler
            Conductor.Instance.Signaller.OnSignedIn += () =>
            {
                RunOnUiThread(() =>
                {
                    IsConnected = true;
                    IsMicrophoneEnabled = true;
                    IsCameraEnabled = true;
                    IsConnecting = false;
                });
            };

            // Failed to connect to the server event handler
            Conductor.Instance.Signaller.OnServerConnectionFailure += () =>
            {
                RunOnUiThread(async () =>
                {
                    IsConnecting = false;
                    MessageDialog msgDialog = new MessageDialog("Failed to connect to server!");
                    await msgDialog.ShowAsync();
                });
            };

            // The current user is disconnected from the server event handler
            Conductor.Instance.Signaller.OnDisconnected += () =>
            {
                RunOnUiThread(() =>
                {
                    IsConnected = false;
                    IsMicrophoneEnabled = false;
                    IsCameraEnabled = false;
                    IsDisconnecting = false;
                    Peers?.Clear();
                });
            };

            LoadSettings();

            Connect();

            
            // Event handlers for managing the media streams 

            Conductor.Instance.OnAddRemoteStream += Conductor_OnAddRemoteStream;
            Conductor.Instance.OnRemoveRemoteStream += Conductor_OnRemoveRemoteStream;
            Conductor.Instance.OnAddLocalStream += Conductor_OnAddLocalStream;

            /**
            // Connected to a peer event handler
            Conductor.Instance.OnPeerConnectionCreated += () =>
            {
                RunOnUiThread(() =>
                {
                    IsReadyToConnect = false;
                    IsConnectedToPeer = true;
                    if (SettingsButtonChecked)
                    {
                        // close settings screen if open
                        SettingsButtonChecked = false;
                        ScrollBarVisibilityType = ScrollBarVisibility.Disabled;
                    }
                    IsReadyToDisconnect = false;
                    if (SettingsButtonChecked)
                    {
                        // close settings screen if open
                        SettingsButtonChecked = false;
                        ScrollBarVisibilityType = ScrollBarVisibility.Disabled;
                    }

                    // Make sure the screen is always active while on call
                    if (!_keepOnScreenRequested)
                    {
                        _keepScreenOnRequest.RequestActive();
                        _keepOnScreenRequested = true;
                    }

                    UpdateScrollBarVisibilityTypeHelper();
                });
            };

            // Connection between the current user and a peer is closed event handler
            Conductor.Instance.OnPeerConnectionClosed += () =>
            {
                RunOnUiThread(() =>
                {
                    IsConnectedToPeer = false;
                    Conductor.Instance.Media.RemoveVideoTrackMediaElementPair(_peerVideoTrack);
                    //PeerVideo.Source = null;

                    Conductor.Instance.Media.RemoveVideoTrackMediaElementPair(_selfVideoTrack);
                    //SelfVideo.Stop();
                    //SelfVideo.ClearValue(MediaElement.SourceProperty);
                    //SelfVideo.Source = null;

                    _peerVideoTrack = null;
                    _selfVideoTrack = null;
                    GC.Collect(); // Ensure all references are truly dropped.
                    IsMicrophoneEnabled = true;
                    IsCameraEnabled = true;
                    SelfVideoFps = PeerVideoFps = "";

                    // Make sure to allow the screen to be locked after the call
                    if (_keepOnScreenRequested)
                    {
                        _keepScreenOnRequest.RequestRelease();
                        _keepOnScreenRequested = false;
                    }
                    UpdateScrollBarVisibilityTypeHelper();
                });
            };

            // Ready to connect to the server event handler
            Conductor.Instance.OnReadyToConnect += () => { RunOnUiThread(() => { IsReadyToConnect = true; }); };

            // Initialize the Ice servers list
            IceServers = new ObservableCollection<IceServer>();
            NewIceServer = new IceServer();

            // Prepare to list supported audio codecs
            AudioCodecs = new ObservableCollection<CodecInfo>();
            var audioCodecList = WebRTC.GetAudioCodecs();

            // These are features added to existing codecs, they can't decode/encode real audio data so ignore them
            string[] incompatibleAudioCodecs = new string[] { "CN32000", "CN16000", "CN8000", "red8000", "telephone-event8000" };

            // Prepare to list supported video codecs
            VideoCodecs = new ObservableCollection<CodecInfo>();

            // Order the video codecs so that the stable VP8 is in front.
            var videoCodecList = WebRTC.GetVideoCodecs().OrderBy(codec =>
            {
                switch (codec.Name)
                {
                    case "VP8": return 1;
                    case "VP9": return 2;
                    case "H264": return 3;
                    default: return 99;
                }
            });

            // Load the supported audio/video information into the Settings controls
            RunOnUiThread(() =>
            {
                foreach (var audioCodec in audioCodecList)
                {
                    if (!incompatibleAudioCodecs.Contains(audioCodec.Name + audioCodec.ClockRate))
                    {
                        AudioCodecs.Add(audioCodec);
                    }
                }

                if (AudioCodecs.Count > 0)
                {
                    if (settings.Values["SelectedAudioCodecId"] != null)
                    {

                        int id = Convert.ToInt32(settings.Values["SelectedAudioCodecId"]);

                        foreach (var audioCodec in AudioCodecs)
                        {

                            int audioCodecId = audioCodec.Id;
                            if (audioCodecId == id)
                            {
                                SelectedAudioCodec = audioCodec;
                                break;
                            }
                        }
                    }
                    if (SelectedAudioCodec == null)
                    {
                        SelectedAudioCodec = AudioCodecs.First();
                    }
                }

                foreach (var videoCodec in videoCodecList)
                {
                    VideoCodecs.Add(videoCodec);
                }

                if (VideoCodecs.Count > 0)
                {
                    if (settings.Values["SelectedVideoCodecId"] != null)
                    {

                        int id = Convert.ToInt32(settings.Values["SelectedVideoCodecId"]);
                        foreach (var videoCodec in VideoCodecs)
                        {
                            int videoCodecId = videoCodec.Id;
                            if (videoCodecId == id)
                            {
                                SelectedVideoCodec = videoCodec;
                                break;
                            }
                        }
                    }
                    if (SelectedVideoCodec == null)
                    {
                        SelectedVideoCodec = VideoCodecs.First();
                    }
                }
            });
            LoadSettings();
            RunOnUiThread(() =>
            {
                OnInitialized?.Invoke();
            });*/
        }

        /// <summary>
        /// Add remote stream event handler.
        /// </summary>
        /// <param name="evt">Details about Media stream event.</param>
        private void Conductor_OnAddRemoteStream(MediaStreamEvent evt)
        {
            _peerVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
            if (_peerVideoTrack != null)
            {
                //if (!_isHoloLens)
                //{
                //    Conductor.Instance.Media.AddVideoTrackMediaElementPair(_peerVideoTrack, PeerVideo, "PEER");
                //}

                //var source = Media.CreateMedia().CreateMediaSource(_peerVideoTrack, "PEER");
                //RunOnUiThread(() =>
                //{
                //  PeerVideo.SetMediaStreamSource(source);
                //});

                // Pass the spatial coordinate system to webrtc.
                var spatialCoordinateSystem = UrhoAppView.Current.ReferenceFrame.CoordinateSystem;
                Media.SetSpatialCoordinateSystem(spatialCoordinateSystem);
            }

            IsReadyToDisconnect = true;
        }

        /// <summary>
        /// Remove remote stream event handler.
        /// </summary>
        /// <param name="evt">Details about Media stream event.</param>
        private void Conductor_OnRemoveRemoteStream(MediaStreamEvent evt)
        {
            RunOnUiThread(() =>
            {
                Conductor.Instance.Media.RemoveVideoTrackMediaElementPair(_peerVideoTrack);
                //PeerVideo.SetMediaStreamSource(null);
            });
        }


        /// <summary>
        /// Add local stream event handler.
        /// </summary>
        /// <param name="evt">Details about Media stream event.</param>
        private void Conductor_OnAddLocalStream(MediaStreamEvent evt)
        {
            _selfVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
            if (_selfVideoTrack != null)
            {
                //var source = Media.CreateMedia().CreateMediaSource(_selfVideoTrack, "SELF");
                RunOnUiThread(() =>
                {
                    if (_cameraEnabled)
                    {
                        Conductor.Instance.EnableLocalVideoStream();
                    }
                    else
                    {
                        Conductor.Instance.DisableLocalVideoStream();
                    }

                    if (_microphoneIsOn)
                    {
                        Conductor.Instance.UnmuteMicrophone();
                    }
                    else
                    {
                        Conductor.Instance.MuteMicrophone();
                    }
                });
            }
        }

        public void Connect()
        {
            new Task(() =>
            {
                Conductor.Instance.StartLogin(Ip.Value, Port.Value);
            }).Start();
        }

        private void LoadSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;

            var ip = new ValidableNonEmptyString( (string) settings.Values["IpString"]);
            var port = new ValidableIntegerString( (string) settings.Values["PortString"]);

            Ip = ip;
            Port = port;
        }

        private ValidableNonEmptyString _ip;

        /// <summary>
        /// IP address of the server to connect.
        /// </summary>
        public ValidableNonEmptyString Ip
        {
            get { return _ip; }
            set { _ip = value; }
        }

        private ValidableIntegerString _port;

        /// <summary>
        /// The port used to connect to the server.
        /// </summary>
        public ValidableIntegerString Port
        {
            get { return _port; }
            set { _port = value; }
        }

        private ObservableCollection<Peer> _peers;

        /// <summary>
        /// The list of connected peers.
        /// </summary>
        public ObservableCollection<Peer> Peers
        {
            get { return _peers; }
            set { SetProperty(ref _peers, value); }
        }

        private Peer _selectedPeer;

        /// <summary>
        /// The selected peer's info.
        /// </summary>
        public Peer SelectedPeer
        {
            get { return _selectedPeer; }
            set
            {
                SetProperty(ref _selectedPeer, value);
                //ConnectToPeerCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isMicrophoneEnabled = true;

        /// <summary>
        /// Indicator if the microphone is enabled.
        /// </summary>
        public bool IsMicrophoneEnabled
        {
            get { return _isMicrophoneEnabled; }
            set { SetProperty(ref _isMicrophoneEnabled, value); }
        }

        private bool _isCameraEnabled = true;

        /// <summary>
        /// Indicator if the camera is enabled.
        /// </summary>
        public bool IsCameraEnabled
        {
            get { return _isCameraEnabled; }
            set { SetProperty(ref _isCameraEnabled, value); }
        }

        private bool _isConnected;

        /// <summary>
        /// Indicator if the user is connected to the server.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                SetProperty(ref _isConnected, value);
                //ConnectCommand.RaiseCanExecuteChanged();
                //DisconnectFromServerCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isConnecting;

        /// <summary>
        /// Indicator if the application is in the process of connecting to the server.
        /// </summary>
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                SetProperty(ref _isConnecting, value);
                //ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isDisconnecting;

        /// <summary>
        /// Indicator if the application is in the process of disconnecting from the server.
        /// </summary>
        public bool IsDisconnecting
        {
            get { return _isDisconnecting; }
            set
            {
                SetProperty(ref _isDisconnecting, value);
                //DisconnectFromServerCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isConnectedToPeer;

        /// <summary>
        /// Indicator if the user is connected to a peer.
        /// </summary>
        public bool IsConnectedToPeer
        {
            get { return _isConnectedToPeer; }
            set
            {
                SetProperty(ref _isConnectedToPeer, value);
                //ConnectToPeerCommand.RaiseCanExecuteChanged();
               // DisconnectFromPeerCommand.RaiseCanExecuteChanged();

                //PeerConnectionHealthStats = null;
                //UpdatePeerConnHealthStatsVisibilityHelper();
                //UpdateLoopbackVideoVisibilityHelper();
            }
        }

        private bool _isReadyToConnect;

        /// <summary>
        /// Indicator if the app is ready to connect to a peer.
        /// </summary>
        public bool IsReadyToConnect
        {
            get { return _isReadyToConnect; }
            set
            {
                SetProperty(ref _isReadyToConnect, value);
                //ConnectToPeerCommand.RaiseCanExecuteChanged();
                //DisconnectFromPeerCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isReadyToDisconnect;

        /// <summary>
        /// Indicator if the app is ready to disconnect from a peer.
        /// </summary>
        public bool IsReadyToDisconnect
        {
            get { return _isReadyToDisconnect; }
            set
            {
                SetProperty(ref _isReadyToDisconnect, value);
                //DisconnectFromPeerCommand.RaiseCanExecuteChanged();
            }
        }

        public RawVideoSource RawVideoSource { get => _rawVideoSource; set => _rawVideoSource = value; }

        public void CallFirstPeer()
        {
            new Task(() =>
            {
                Conductor.Instance.ConnectToPeer(Peers.First());
            }).Start();
        }
    }
}
