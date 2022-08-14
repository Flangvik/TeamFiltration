using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{

    /*
     * 
     * 
     * {"odata.metadata":"https://graph.windows.net/75bbd831-230c-4b8e-ba6a-c9517320c1b1/$metadata#directoryObjects","odata.nextLink
     * */



    public class GroupsRespAAD
    {
        [JsonProperty("odata.metadata")]
        public string odatametadata { get; set; }
        [JsonProperty("odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<Value> value { get; set; }
    }

    public class Value
    {
        [JsonProperty("odata.type")]
        public string odatatype { get; set; }
        public string objectType { get; set; }
        public string description { get; set; }
        public string objectId { get; set; }
        public string mailNickname { get; set; }
        public string onPremisesSamAccountName { get; set; }
    }

   
}
