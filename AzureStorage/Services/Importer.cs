using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;

namespace Azure.Storage.API.Services
{
    /*
    public class Message : TableEntity {
        public int Id { get; set; }
        public string DevEui { get; set; }
    }
    */

    public class Importer
    {
        
        //static string storageconn = "DefaultEndpointsProtocol=https;AccountName=lltstorage;AccountKey=UIOxjK1hNosxf6nhIlPurYEvphYcDgiAREUa0KoHgIKWxE/qOCgSONC3dTTpizQJSHQjBwwgoYZlx8dC7EER9w==;EndpointSuffix=core.windows.net";
        //static string table1 = "Employee";
        CloudTable table;

        //public Importer(IOptions<AppSettings> settings)
        public Importer(string connectionstring, string storagetable)
        {
            CloudStorageAccount storageAcc = CloudStorageAccount.Parse(connectionstring);
            CloudTableClient tblclient = storageAcc.CreateCloudTableClient(new TableClientConfiguration());
            table = tblclient.GetTableReference(storagetable);
        }


        //public static async Task<string> InsertTableEntity(CloudTable p_tbl)
        //public string InsertTableEntity(CloudTable p_tbl)
        /*
        public string InsertTableEntity()
        {
            Message entity = new Message();
            entity.Id = 1;
            entity.RowKey = rowKey1;
            entity.PartitionKey = partitionkey;
            TableOperation insertOperation = TableOperation.InsertOrMerge((ITableEntity)entity);
            _ = table.Execute(insertOperation);
            //Console.WriteLine("Employee Added");
            return "Employee added";
        }
        */

        public string InsertBatch(IList<DynamicTableEntity> list)
        {
            TableBatchOperation l_batch = new TableBatchOperation();

            foreach (var item in list)
            {
                l_batch.Insert(item);
            }

            var result = table.ExecuteBatch(l_batch);
            //Console.WriteLine("Records Inserted");
            return "Completed " + result.Count.ToString();
        }
    }
}
