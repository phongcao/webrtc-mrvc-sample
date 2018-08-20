using Urho;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UrhoUWPDesktop
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CallPage : Page
    {
        private CallViewModel _callViewModel;
        private UrhoApplication _application;

        public CallPage()
        {
            this.InitializeComponent();
            Loaded += Page_Loaded;

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
        }

        private void Call_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _callViewModel.CallFirstPeer();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _application = UrhoSurface.Run<UrhoApplication>(
                new ApplicationOptions("Data")
                {
                    Width = UrhoApplication.VIDEO_WIDTH_LOW,
                    Height = UrhoApplication.VIDEO_HEIGHT_LOW
                });

            _callViewModel.UrhoApplication = _application;
        }
    }
}
