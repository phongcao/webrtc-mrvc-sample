using System;
using System.Diagnostics;
using Urho;
using WebRTCUWP.Utilities;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UrhoUWPHoloLens
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // simplest way is for this page to allow you to enter the IP of the server to connect to
            Debug.WriteLine("Is this a HoloLens: " + IsRunningOnHoloLens());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var settings = ApplicationData.Current.LocalSettings;

            if (!string.IsNullOrEmpty((string) settings.Values["IpString"]))
            {
                IpString.Text = (string)settings.Values["IpString"];
            }

            if (!string.IsNullOrEmpty((string) settings.Values["PortString"]))
            {
                PortString.Text = (string)settings.Values["PortString"];
            }   
        }

        private bool IsRunningOnHoloLens()
        {
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Holographic")
            {
                return true;
            }
            return false;
        }

        private async void ConnectCall_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Connect Call Clicked");

            // Get the IP Address in the text box
            var ipString = new ValidableNonEmptyString(IpString.Text);
            var port = new ValidableIntegerString(PortString.Text);

            var settings = ApplicationData.Current.LocalSettings;

            settings.Values["IpString"] = IpString.Text;
            settings.Values["PortString"] = PortString.Text;

            // Navigate to the next page.
            if (IsRunningOnHoloLens())
            {
                // Navigate to the immersive view (with the IP address somehow).
                var urhoView = CoreApplication.CreateNewView(
                    new UrhoAppViewSource<HoloApplication>(
                        new ApplicationOptions("Data")));

                var id = ApplicationView.GetForCurrentView().Id;

                await urhoView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    CoreWindow.GetForCurrentThread().Activate();
                    await ApplicationViewSwitcher.SwitchAsync(ApplicationView.GetForCurrentView().Id, id,
                        ApplicationViewSwitchingOptions.ConsolidateViews);
                });
            }
            else
            {
                this.Frame.Navigate(typeof(CallPage));
            }
        }
    }
}
