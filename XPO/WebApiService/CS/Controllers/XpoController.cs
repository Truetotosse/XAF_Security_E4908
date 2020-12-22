using DevExpress.Xpo.DB;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebApiService.Controllers {
    [ApiController]
    [Route("[controller]/[action]")]
    public class XpoController : ControllerBase {
        readonly WebApiDataStoreService DataStoreService;
        public XpoController(WebApiDataStoreService dataStoreService) {
            Console.WriteLine("DataStore created");
            this.DataStoreService = dataStoreService;
        }
        [HttpGet]
        public string Hello() {
            return "Use WebApiDataStoreClient to connect to this service. See also: https://docs.devexpress.com/XPO/402182/connect-to-a-data-store/transfer-data-via-rest-api";
        }
        [HttpPost]
        public Task<OperationResult<UpdateSchemaResult>> UpdateSchema([FromQuery] bool doNotCreateIfFirstTableNotExist, [FromBody] DBTable[] tables) {
            Console.WriteLine("Updated Schema");
            return DataStoreService.UpdateSchemaAsync(doNotCreateIfFirstTableNotExist, tables);
        }
        [HttpPost]
        public Task<OperationResult<SelectedData>> SelectData([FromBody] SelectStatement[] selects) {
            Console.WriteLine("Selected Data");
            return DataStoreService.SelectDataAsync(selects);
        }
        [HttpPost]
        public Task<OperationResult<ModificationResult>> ModifyData([FromBody] ModificationStatement[] dmlStatements) {
            return DataStoreService.ModifyDataAsync(dmlStatements);
        }
    }
}
