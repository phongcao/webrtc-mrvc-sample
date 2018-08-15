using WebRTCUWP.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UrhoUWPHoloLens
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CallPage : Page
    {
        private CallViewModel _callViewModel;

        public CallPage()
        {
            this.InitializeComponent();

            // On this page we need to start the basic initialization and handling for webrtc
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _callViewModel = new CallViewModel(CoreApplication.MainView.CoreWindow.Dispatcher);
            this.DataContext = _callViewModel;
            _callViewModel.PeerVideo = PeerVideo;

            // We don't render our own video
            //_callViewModel.SelfVideo = SelfVideo;
        }

        /// <summary>
        /// Media Failed event handler for remote/peer video.
        /// Invoked when an error occurs in peer media source.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the exception routed event.</param>
        private void PeerVideo_MediaFailed(object sender, Windows.UI.Xaml.ExceptionRoutedEventArgs e)
        {
//            if (_mainViewModel != null)
//            {
//                _mainViewModel.PeerVideo_MediaFailed(sender, e);
//            }
        }

        private void Call_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _callViewModel.CallFirstPeer();
        }
    }
}
