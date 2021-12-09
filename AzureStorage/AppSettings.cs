using System;
namespace Azure.Storage.API
{
    public class AppSettings
    {
        public string ConnectionString { get; set; }
        public string StorageTable { get; set; }
        public string CosmosPrimaryKey { get; set; }
        public string CosmosDatabaseId { get; set; }
        public string CosmosContainerId { get; set; }
        public string CosmosEndpointUrl { get; set; }
    }
}
