using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Office365 Service Discovery result with one service result
    /// </summary>
    public class ServiceDiscoveryItem
    {
        [JsonProperty("serviceEndpointUri")]
        public string ServiceEndPointUri { get; set; }

        [JsonProperty("serviceResourceId")]
        public string ServiceResourceId { get; set; }

        [JsonProperty("capability")]
        public string Capability { get; set; }

        [JsonProperty("serviceApiVersion")]
        public string ServiceApiVersion { get; set; }
    }
}
