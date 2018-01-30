using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTProject
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static string DeviceName = "Device1";
        List<AzureDevices> list = new List<AzureDevices>();
        AzureDevices MainDevice = new AzureDevices
    
        {
            DeviceId = "Device1",
            Status = false
        };

        public  GpioPin ledPin;
         public  GpioPin buttonPin;
        private const int LED_PIN1 = 6;

        private const int BUTTON_PIN = 5;
        public GpioPinValue ledPinValue = GpioPinValue.High;
   

        Dictionary<string, int> LEDS = new Dictionary<string, int>();

        public MainPage()
        {
            this.InitializeComponent();


            InitGPIO();

            var sender = new object();
            var e = new RoutedEventArgs();
            GetDevicesButton_Click(sender, e);
            ListenForMessages();


            MyAzureClass myAzureClass = new MyAzureClass();
            // MainDevice = myAzureClass.AddDeviceToCloud(MainDevice.DeviceId, MainDevice.Status).Result;
            // Task.Run(async () => { MainDevice = await myAzureClass.AddDeviceToCloud(MainDevice.DeviceId, MainDevice.Status); }).GetAwaiter().GetResult();
            //AsyncContext.Run(() => );
            //while (MainDevice.PartitionKey == null) { }
            MainDevice.AssignRowKey();
            MainDevice.AssignPartitionKey();

            DeviceStatusButton.IsEnabled = true;
           
        }

        private async void AddDeviceBtn_Click(object sender, RoutedEventArgs e)
        {
            MyAzureClass myAzureClass = new MyAzureClass();
            var AddedDevice = await myAzureClass.AddDeviceToCloud(AddDeviceBox.Text, StatusCheck.IsChecked.Value);
            list.Add(AddedDevice);
            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = list;
        }

        private async void GetDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            MyAzureClass myAzureClass = new MyAzureClass();
            list = await myAzureClass.GetDevices();
            foreach (AzureDevices device in list)
            {
                Debug.WriteLine("Device ID: " + device.DeviceId);
            }
            DeviceList.ItemsSource = list;
        }

        private void DeleteDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            //int Index = DeviceList.SelectedIndex;
            AzureDevices SelectedDevice = (AzureDevices)DeviceList.SelectedItems[0];
            MyAzureClass myAzureClass = new MyAzureClass();
            myAzureClass.DeleteRecordinTable(SelectedDevice.DeviceId);

            var index = DeviceList.Items.IndexOf(DeviceList.SelectedItem);
            list.RemoveAt(index);
            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = list;
        }

        private void UpdateDevice_Click(object sender, RoutedEventArgs e)
        {
            AzureDevices SelectedDevice = (AzureDevices)DeviceList.SelectedItems[0];
            if (TrueRadio.IsChecked.Value) SelectedDevice.Status = true; else { SelectedDevice.Status = false; }

            var index = DeviceList.Items.IndexOf(DeviceList.SelectedItem);
            list.RemoveAt(index);
            list.Insert(index, SelectedDevice);
            /* DeviceList.ItemsSource = null;
             DeviceList.ItemsSource = list;*/

            MyAzureClass myAzureClass = new MyAzureClass();
            myAzureClass.UpdateRecordInTable(SelectedDevice);
            AzureIoTHub.SendDeviceToCloudMessageAsync();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            AzureIoTHub.SendDeviceToCloudMessageAsync();
            var message = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
            Debug.WriteLine("Message: " + message);
        }

        public async void RefreshList()
        {
            MyAzureClass myAzureClass = new MyAzureClass();
            list = await myAzureClass.GetDevices();
            var AnyDeviceHigh = false;
            foreach (AzureDevices x in list)
            {
                if (x.Status)
                {
                    AnyDeviceHigh = true;
                }
            }
            updateLED(AnyDeviceHigh);
            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = list;
        }

        public async void ListenForMessages()
        {
            while (true)
            {
                string str = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
                if (str == "Update")
                {
                    Debug.WriteLine("Received: " + str);
                    RefreshList();
                   // UpdateLeds(str);
                }
                Debug.WriteLine("Received: " + str);
            }
        }


        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceList.Items.IndexOf(DeviceList.SelectedItem) > -1)
            {
                UpdateDevice.IsEnabled = true;
                DeleteDeviceButton.IsEnabled = true;

            }
            else
            {
                UpdateDevice.IsEnabled = false;
                DeleteDeviceButton.IsEnabled = false;
            }
        }

        private void DeviceStatusButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MainDevice.Status = true;
            MyAzureClass myAzureClass = new MyAzureClass();
            myAzureClass.UpdateRecordInTable(MainDevice);
            AzureIoTHub.SendDeviceToCloudMessageAsync();
            Debug.WriteLine("Button Pressed");
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
           

            buttonPin = gpio.OpenPin(BUTTON_PIN);
            if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                buttonPin.SetDriveMode(GpioPinDriveMode.Input);

            

            // ledPin = gpio.OpenPin(LED_PIN1);
            //  ledPin.Write(GpioPinValue.High);
            // ledPin.SetDriveMode(GpioPinDriveMode.Output);


            // Set a debounce timeout to filter out switch bounce noise from a button press
            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            buttonPin.ValueChanged += buttonPin_ValueChangedAsync;
            ledPin = gpio.OpenPin(LED_PIN1);

            ledPin.SetDriveMode(GpioPinDriveMode.Output);
            ledPin.Write(GpioPinValue.Low);

        }


        public void updateLED(Boolean state)
        {
            if (!state)
            {
                ledPin.Write(GpioPinValue.High);
            }
            else
            {
                ledPin.Write(GpioPinValue.Low);
            }
        }


        private async void buttonPin_ValueChangedAsync(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            MyAzureClass myAzureClass = new MyAzureClass();

            // toggle the state of the LED every time the button is pressed
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                ledPinValue = (ledPinValue == GpioPinValue.Low) ?
                    GpioPinValue.High : GpioPinValue.Low;
                // ledPin.Write(ledPinValue);
                MainDevice.Status = Convert.ToBoolean(ledPinValue);
                 myAzureClass.UpdateRecordInTable(MainDevice);
                 AzureIoTHub.SendDeviceToCloudMessageAsync();

            }

        }

        private void DeviceStatusButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (DeviceStatusButton.IsEnabled == false)
            {
                MainDevice.Status = false;
                MyAzureClass myAzureClass = new MyAzureClass();
                myAzureClass.UpdateRecordInTable(MainDevice);
                AzureIoTHub.SendDeviceToCloudMessageAsync();
                Debug.WriteLine("Button Released");
            }
        }

        private void DeviceStatus_Clicked(object sender, RoutedEventArgs e)
        {
            if (DeviceStatusButton.IsEnabled)
            {
                if (DeviceStatusButton.Content.ToString() == "Not Clicked")
                {
                    MainDevice.Status = true;
                    MyAzureClass myAzureClass = new MyAzureClass();
                    myAzureClass.UpdateRecordInTable(MainDevice);
                    AzureIoTHub.SendDeviceToCloudMessageAsync();
                    Debug.WriteLine("Button Pressed");
                    DeviceStatusButton.Content = "Clicked";
                }
                else
                {
                    MainDevice.Status = false;
                    MyAzureClass myAzureClass = new MyAzureClass();
                    myAzureClass.UpdateRecordInTable(MainDevice);
                    AzureIoTHub.SendDeviceToCloudMessageAsync();
                    Debug.WriteLine("Button Released");
                    DeviceStatusButton.Content = "Not Clicked";
                }
            }
        }
    }
}
public class DeviceStatus : TableEntity
{
    public DeviceStatus(string device, string status)
    {
        this.PartitionKey = device;
        this.RowKey = status;
    }
    public DeviceStatus() { }
}
