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
using Azure.Storage.API.Controllers;

namespace Azure.Storage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TableStorageController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<TableStorageController> _logger;
        private string connectionString;
        private string storageTable;

        public TableStorageController(ILogger<TableStorageController> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _appSettings = settings.Value;

            connectionString = _appSettings.ConnectionString;
            storageTable = _appSettings.StorageTable;
        }

        [HttpPost]
        [Route("save/")]
        public IActionResult Save(JsonElement json)
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
    }
}
