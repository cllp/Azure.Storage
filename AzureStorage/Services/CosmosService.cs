using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Storage.API.Interface;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Azure.Storage.API.Services
{
    public class CosmosService : ICosmosService
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<CosmosService> _logger;

        // The Azure Cosmos DB endpoint
        private string EndpointUri;
        // The primary key for the Azure Cosmos account.
        private string PrimaryKey;

        private CosmosClient cosmosClient;
        private Database database;
        private Container container;

        private string databaseId;
        private string containerId;

        public CosmosService(ILogger<CosmosService> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _appSettings = settings.Value;

            PrimaryKey = _appSettings.CosmosPrimaryKey;
            databaseId = _appSettings.CosmosDatabaseId;
            containerId = _appSettings.CosmosContainerId;
            EndpointUri = _appSettings.CosmosEndpointUrl;

            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            this.container = cosmosClient.GetContainer(databaseId, containerId);

        }

        public async Task<string> Save(JsonElement json)
        {
            try
            {
                dynamic obj = System.Text.Json.JsonSerializer.Deserialize<ExpandoObject>(json.GetRawText());

                DateTimeOffset dto;
                if (!DateTimeOffset.TryParse(json.GetProperty("observationTimestamp").GetString(), out dto))
                    throw new Exception("Could not parse " + json.GetProperty("observationTimestamp").GetString() + " to timestamp");

                var partitionKey = json.GetProperty("partitionMonth").GetString();

                dynamic dataobj = new ExpandoObject();
                dataobj.partitionMonth = json.GetProperty("partitionMonth").GetString();
                dataobj.observationTimestamp = json.GetProperty("observationTimestamp").GetString();
                dataobj.id = Guid.NewGuid().ToString(); //json.GetProperty("id").GetString();

                dataobj.deviceId = json.GetProperty("deviceId").GetInt32();
                dataobj.devEui = json.GetProperty("devEui").GetString();
                dataobj.deviceUUID = json.GetProperty("deviceUUID").GetString();
                dataobj.deviceTypeId = json.GetProperty("deviceTypeId").GetInt32();
                dataobj.deviceType = json.GetProperty("deviceType").GetString();
                dataobj.littera = json.GetProperty("littera").GetString();
                dataobj.deviceStatus = json.GetProperty("deviceStatus").GetString();
                dataobj.customer = json.GetProperty("customer").GetString();
                dataobj.kst = json.GetProperty("kst").GetString();
                dataobj.networkProvider = json.GetProperty("networkProvider").GetString();
                dataobj.fcntUp = json.GetProperty("fcntUp").GetInt32();
                dataobj.payload = json.GetProperty("payload").GetString();
                dataobj.metricsData = new ExpandoObject();
                dataobj.commData = new ExpandoObject();

                var metricsData = json.GetProperty("metricsData");
                var commData = json.GetProperty("commData");

                foreach (var prop in metricsData.EnumerateObject())
                {
                    AddProperty(dataobj.metricsData, prop.Name, prop.Value.GetString());
                }

                foreach (var prop in commData.EnumerateObject())
                {
                    AddProperty(dataobj.commData, prop.Name, prop.Value.GetString());
                }

                dataobj.transactionId = json.GetProperty("transactionId").GetString();

                ItemResponse<dynamic> andersenFamilyResponse = await this.container.CreateItemAsync<dynamic>(dataobj, new PartitionKey(partitionKey));
                Console.WriteLine(andersenFamilyResponse.StatusCode);

                return "StatusCode " + andersenFamilyResponse.StatusCode.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ex.Message + ". " + ex.StackTrace;
            }
        }

        private bool AddProperty(ExpandoObject obj, string key, object value)
        {
            var dynamicDict = obj as IDictionary<string, object>;
            if (dynamicDict.ContainsKey(key))
                return false;
            else
                dynamicDict.Add(key, value);
            return true;
        }
    }
}
