using System;
using System.Text;
using System.Threading.Tasks;
using IoTProject;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

class AzureIoTHub
{
    private static void CreateClient()
    {
        if (deviceClient == null)
        {
            // create Azure IoT Hub client from embedded connection string
            deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
        }

    }

    static DeviceClient deviceClient = null;

    //
    // Note: this connection string is specific to the device "test". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    const string deviceConnectionString = "HostName=EPDEgroup8Hanze.azure-devices.net;DeviceId=test;SharedAccessKey=KZrtUfQVHwvKY/tcreHOAnBStxLwVI7Bl+9NY+zmVnk=";

    //
    // To monitor messages sent to device "kraaa" use iothub-explorer as follows:
    //    iothub-explorer monitor-events --login HostName=EPDEgroup8Hanze.azure-devices.net;SharedAccessKey                                          Name=service;SharedAccessKey=MDNZ9Uwt0UoLUn+UXWrDuGXhf4JT4xVxHygRpzdhprU= "test"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-2017-wiki for more information on Connected Service for Azure IoT Hub

    public static async void SendDeviceToCloudMessageAsync()
    {
        ServiceDeviceStatus deviceStatus = new ServiceDeviceStatus()
        {
            SourceDeviceId = "CalvinSimulator",
            Time = DateTime.Now.ToString("O"),
            Command = "Update",
            CommandACK = "0",
            TargetDeviceId = "test"
        };

        CreateClient();

        string messageString = JsonConvert.SerializeObject(deviceStatus, Formatting.None);

        var message = new Message(Encoding.ASCII.GetBytes(messageString));

        await deviceClient.SendEventAsync(message);
        /* String Message = await ReceiveCloudToDeviceMessageAsync();
         Debug.WriteLine("Maybe I can already Receive Stuff: " + Message);*/
    }

    public static async Task<string> ReceiveCloudToDeviceMessageAsync()
    {
        CreateClient();

        while (true)
        {
            var receivedMessage = await deviceClient.ReceiveAsync();

            if (receivedMessage != null)
            {
                var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                await deviceClient.CompleteAsync(receivedMessage);
                return messageData;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public void updateLed(Boolean value)
    {

    }
}
