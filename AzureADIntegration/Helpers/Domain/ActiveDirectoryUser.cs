using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureADIntegration.Helpers.Domain
{
    public class Manager
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }
        public string userPrincipalName { get; set; }
    }

    public class Value
    {
        public string userPrincipalName { get; set; }
        public Manager manager { get; set; }
    }

    public class Root
    {
        [JsonProperty("@odata.context")]
        public string OdataContext { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string OdataNextLink { get; set; }
        public List<Value> value { get; set; }
    }

}
