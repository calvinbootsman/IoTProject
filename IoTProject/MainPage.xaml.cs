using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
        public static string DeviceName = "CalvinSimulator";
        List<AzureDevices> list = new List<AzureDevices>();
        AzureDevices MainDevice = new AzureDevices
        {
            DeviceId = DeviceName,
            Status = false
        };
        public MainPage()
        {
            this.InitializeComponent();
            var sender = new object();
            var e = new RoutedEventArgs();
            GetDevicesButton_Click(sender, e);
            ListenForMessages();

            MyAzureClass myAzureClass = new MyAzureClass();
            Task.Run(async () => { MainDevice = await myAzureClass.AddDeviceToCloud(MainDevice.DeviceId, MainDevice.Status); }).GetAwaiter().GetResult();
            //AsyncContext.Run(() => );
            while (MainDevice.PartitionKey == null) { }
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

        private void DeviceStatusButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            
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

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

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
