using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Azure.Storage.API.Interface
{
    public interface ICosmosService
    {
        Task<string> Save(JsonElement json);
    }
}
