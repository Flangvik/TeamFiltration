using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.OWA
{


    public class AllEmailsResp
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<EmailResp> value { get; set; }
    }

    public class EmailObject
    {
        [JsonProperty("@odata.id")]
        public string odataid { get; set; }
        [JsonProperty("@odata.etag")]
        public string odataetag { get; set; }
        public string Id { get; set; }  
        public string Subject { get; set; }
     
    }


}
