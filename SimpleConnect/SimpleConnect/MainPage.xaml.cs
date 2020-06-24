using Buttplug.Client;
using Buttplug.Core.Logging;
using Buttplug.Server.Managers.XamarinBluetoothManager;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace SimpleConnect
{
    public partial class MainPage : ContentPage
    {
        private ButtplugClient _client;
        private ButtplugClientDevice _device;
        private Task _initTask;

        public MainPage()
        {
            InitializeComponent();
            _initTask = Task.Run(async () =>
            {
                var connector = new ButtplugEmbeddedConnector("Example Server");
                connector.Server.AddDeviceSubtypeManager<Buttplug.Server.DeviceSubtypeManager>(aLogger => new XamarinBluetoothManager(new ButtplugLogManager()));
                _client = new ButtplugClient("Example Client", connector);
                await _client.ConnectAsync();
                Device.BeginInvokeOnMainThread(() =>
                {
                    btnSync.IsEnabled = true;
                });
                _client.DeviceAdded += HandleDeviceAdded;
                _client.DeviceRemoved += HandleDeviceRemoved;
                btnSync.IsEnabled = true;
            });
            _initTask.Wait();
        }

        async void HandleDeviceAdded(object aObj, DeviceAddedEventArgs aArgs)
        {
            _device = aArgs.Device;
            Debug.WriteLine($"Found device {_device.Name}");
            await _client.StopScanningAsync();
            Device.BeginInvokeOnMainThread(() =>
            {
                btnVibrate.IsEnabled = true;
            });

        }

        void HandleDeviceRemoved(object aObj, DeviceRemovedEventArgs aArgs)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                btnVibrate.IsEnabled = false;
            });

            Debug.WriteLine($"Device connected: {aArgs.Device.Name}");
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var t = Task.Run(() => ScanForDevices());
        }

        async Task ScanForDevices()
        {
            try
            {
                // Android likes to prompt on use
                // UWP does seem to care
                // iOS prompts on app start
                if (Device.RuntimePlatform == "Android")
                {
                    var status = await Device.InvokeOnMainThreadAsync(async () =>
                        await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>());
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Device.InvokeOnMainThreadAsync(async () =>
                            await Permissions.RequestAsync<Permissions.LocationWhenInUse>());
                    }
                    if (status != PermissionStatus.Granted)
                    {
                        Debug.WriteLine("Cannot scan for devices without Location permissions.");
                        return;
                    }
                }

                Debug.WriteLine(
                    "Scanning for devices until key is pressed. Found devices will be printed to console.");
                await _client.StartScanningAsync();
            }
            catch (PermissionException pe)
            {
                // If we set up the manifest wrong, UWP throws
                Debug.WriteLine(pe.Message);
            }

        }

        private async void Vibrate_Clicked(object sender, EventArgs e)
        {
            await _device.SendVibrateCmd(0.5);
            btnStop.IsEnabled = true;
            btnVibrate.IsEnabled = false;
        }

        private async void Stop_Clicked(object sender, EventArgs e)
        {
            await _device.SendVibrateCmd(0.0);
            btnStop.IsEnabled = false;
            btnVibrate.IsEnabled = true;
        }
    }
}
