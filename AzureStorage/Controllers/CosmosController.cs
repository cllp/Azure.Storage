using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Storage.API.Interface;
using Azure.Storage.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Azure.Storage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CosmosController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<CosmosService> _logger;
        private readonly ICosmosService _cosmos;

        public CosmosController(ICosmosService cosmos, ILogger<CosmosService> logger, IOptions<AppSettings> settings)
        {
            _cosmos = cosmos;
            _logger = logger;
            _appSettings = settings.Value;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("get/")]
        public IActionResult Get()
        {
            return new ContentResult { Content = "this is what I get!", StatusCode = 200 }; 
        }

        [HttpPost]
        //[Authorize]
        [AllowAnonymous]
        [Route("save/")]
        public async Task<IActionResult> Save(JsonElement json)
        {
            try
            {
                var result = await _cosmos.Save(json);
                return new ContentResult { Content = "Cosmos " + result.ToString(), StatusCode = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, ex.Message + ex.StackTrace);
            }
        }

    }
}

