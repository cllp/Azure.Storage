using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Storage.API.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Azure.Storage.API;

namespace StorageAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<StorageController> _logger;
        private string connectionString;
        private string storageTable;

        // The Azure Cosmos DB endpoint
        private string EndpointUri;
        // The primary key for the Azure Cosmos account.
        private string PrimaryKey;

        private CosmosClient cosmosClient;
        private Database database;
        private Container container;

        private string databaseId;
        private string containerId;

        public StorageController(ILogger<StorageController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _appSettings = settings.Value;

            connectionString = _appSettings.ConnectionString;
            storageTable = _appSettings.StorageTable;

            PrimaryKey = _appSettings.CosmosPrimaryKey;
            databaseId = _appSettings.CosmosDatabaseId;
            containerId = _appSettings.CosmosContainerId;
            EndpointUri = _appSettings.CosmosEndpointUrl;

            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            this.container = cosmosClient.GetContainer(databaseId, containerId);

        }

        [HttpPost]
        //[Authorize]
        [AllowAnonymous]
        [Route("save/")]
        public async Task<IActionResult> SaveAsync(JsonElement json)
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
                
                return new ContentResult { Content = "StatusCode " + andersenFamilyResponse.StatusCode.ToString(), StatusCode = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, ex.Message + ex.StackTrace);
            }  
        }

        bool AddProperty(ExpandoObject obj, string key, object value)
        {
            var dynamicDict = obj as IDictionary<string, object>;
            if (dynamicDict.ContainsKey(key))
                return false;
            else
                dynamicDict.Add(key, value);
            return true;
        }


        /*
        [HttpPost]
        [Route("save_table/")]
        public IActionResult PostSavePage(JsonElement json)
        {
            var batch = new List<DynamicTableEntity>();

            foreach (var item in json.EnumerateArray())
            {
                DynamicTableEntity entity = new DynamicTableEntity();

                DateTimeOffset dto;
                if (!DateTimeOffset.TryParse(item.GetProperty("commTimestamp").GetString(), out dto))
                    throw new Exception("Could not parse " + item.GetProperty("commTimestamp").GetString() + " to timestamp");

                var partitionKey = item.GetProperty("PartitionKey").GetString();
                var rowKey = item.GetProperty("RowKey").GetString();
                entity.PartitionKey = partitionKey;
                entity.RowKey = rowKey;
                entity.Timestamp = dto;

                foreach (var prop in item.EnumerateObject())
                {
                    var propType = prop.Value.GetType();

                    if (prop.Name.Equals("commTimestamp")) {
                        DateTimeOffset commTimestamp;
                        var dtresult = prop.Value.TryGetDateTimeOffset(out commTimestamp);
                        entity.Properties.Add(prop.Name, new EntityProperty(commTimestamp));
                    }
                    else { 

                        switch (prop.Value.ValueKind)
                        {
                            case JsonValueKind.Null:
                                //result = null;
                                break;
                            case JsonValueKind.Number:
                                //propvalue = prop.Value.GetUInt32();
                                entity.Properties.Add(prop.Name, new EntityProperty(prop.Value.GetDouble()));
                                break;
                            case JsonValueKind.False:
                                //result = false;
                                break;
                            case JsonValueKind.True:
                                //result = true;
                                break;
                            case JsonValueKind.Undefined:
                                //result = null;
                                break;
                            case JsonValueKind.String:
                                //propvalue = prop.Value.GetString();
                                entity.Properties.Add(prop.Name, new EntityProperty(prop.Value.GetString()));
                                break;
                            case JsonValueKind.Object:
                                entity.Properties.Add(prop.Name, new EntityProperty(prop.Value.GetRawText()));
                                //entity.Properties.Add(prop.Name, new EntityProperty(prop.Value.GetString()));
                                break;
                            case JsonValueKind.Array:
                                entity.Properties.Add(prop.Name, new EntityProperty(prop.Value.GetRawText()));
                                break;
                        }
                    }

                }

                batch.Add(entity);
            }

            try
            {
                Importer import = new Importer(connectionString, storageTable);
                var result = import.InsertBatch(batch);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message + ex.StackTrace);
            }
        }
        */


    }

    
}
