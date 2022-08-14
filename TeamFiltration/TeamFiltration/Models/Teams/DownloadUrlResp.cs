using Newtonsoft.Json;

namespace TeamFiltration.Models.Teams
{
   
    public class DownloadUrlResp
    {
        [JsonProperty("@odata.context")]
        public string OdataContext { get; set; }
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }
        [JsonProperty("@odata.id")]
        public string OdataId { get; set; }
        [JsonProperty("@odata.editLink")]
        public string OdataEditLink { get; set; }
        [JsonProperty("@content.downloadUrl")]
        public string ContentDownloadUrl { get; set; }
    }
}
