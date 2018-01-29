using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IoTProject
{
    class MyAzureClass
    {
        public static async void ListenForMessages()
        {
            while (true)
            {
                string str = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
                if (str == "Update")
                {
                    MainPage main = new MainPage();
                    main.RefreshList();
                }
            }
        }

        public CloudTable GetCloudTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=group8;AccountKey=FTPP2o14jNuGOI+YizAdfeWNQWXA4ult7M4ngYx6k8R0Hsxj/EeE1uASuQancc2dvvJksI7uf/jpx8QWgipu6Q==;EndpointSuffix=core.windows.net");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("Test");
            return table;
        }
        public async Task<AzureDevices> AddDeviceToCloud(string device, bool isChecked)
        {
            // Retrieve the storage account from the connection string.

            AzureDevices deviceStatus = new AzureDevices();
            deviceStatus.DeviceId = device;
            deviceStatus.Status = isChecked;
            deviceStatus.AssignRowKey();
            deviceStatus.AssignPartitionKey();

            var table = GetCloudTable();

            AzureDevices deviceRetrieve = await AzureDevices.RetrieveRecord(table, device);
            if (deviceRetrieve == null)
            {
                TableOperation tableOperation = TableOperation.Insert(deviceStatus);
                await table.ExecuteAsync(tableOperation);
                Debug.WriteLine("Record inserted");
            }
            else
            {
                Debug.WriteLine("Record exists");
            }
            return deviceRetrieve;
        }

        public async Task<List<AzureDevices>> GetDevices()
        {
            var list = new List<AzureDevices>();
            var table = GetCloudTable();
            TableContinuationToken token = null;
            TableQuery<AzureDevices> tableQuery = new TableQuery<AzureDevices>();
            var queriedTable = await table.ExecuteQuerySegmentedAsync(tableQuery, token);

            foreach (AzureDevices devices in queriedTable)
            {
                list.Add(devices);
            }
            return list;
        }

        public async void DeleteRecordinTable(string DeviceId)
        {
            var table = GetCloudTable();
            AzureDevices device = await AzureDevices.RetrieveRecord(table, DeviceId);
            if (device != null)
            {
                TableOperation tableOperation = TableOperation.Delete(device);
                await table.ExecuteAsync(tableOperation);
                Debug.WriteLine("Record deleted");
            }
            else
            {
                Debug.WriteLine("Record does not exists");
            }
        }

        public async void UpdateRecordInTable(AzureDevices device)
        {
            var table = GetCloudTable();

            AzureDevices devices = await AzureDevices.RetrieveRecord(table, device.DeviceId);
            if (devices != null)
            {
                TableOperation tableOperation = TableOperation.Replace(device);
                await table.ExecuteAsync(tableOperation);
                Console.WriteLine("Record updated");
            }
            else
            {
                Console.WriteLine("Record does not exists");
            }
        }
    }

    public class AzureDevices : TableEntity
    {
        private string deviceId;
        private bool status;
        public void AssignRowKey()
        {
            this.RowKey = deviceId;
        }
        public void AssignPartitionKey()
        {
            this.PartitionKey = "CalvinsDevices";
        }
        public string DeviceId
        {
            get { return deviceId; }
            set { deviceId = value; }
        }
        public bool Status
        {
            get { return status; }
            set { status = value; }
        }

        public async static Task<AzureDevices> RetrieveRecord(CloudTable table, string deviceid)
        {
            TableOperation tableOperation = TableOperation.Retrieve<AzureDevices>("CalvinsDevices", deviceid);
            TableResult tableResult = await table.ExecuteAsync(tableOperation);
            return tableResult.Result as AzureDevices;
        }

        public async void DeleteRecord(CloudTable table, string deviceid)
        {
            AzureDevices DeleteAzureDevice = await RetrieveRecord(table, deviceid);
            if (DeleteAzureDevice != null)
            {
                TableOperation tableOperation = TableOperation.Delete(DeleteAzureDevice);
                await table.ExecuteAsync(tableOperation);
                Debug.WriteLine("Record deleted");
            }
            else
            {
                Debug.WriteLine("Record does not exists");
            }
        }


    }

    public class ServiceDeviceStatus
    {
        public string Time { get; set; }
        public string SourceDeviceId { get; set; }
        public string TargetDeviceId { get; set; }
        public string Command { get; set; }
        public string CommandACK { get; set; }
    }


}
