using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{


    public class GetMembers
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<MembersObject> value { get; set; }
    }

    public class MembersObject
    {
        [JsonProperty("@odata.type")]
        public string odatatype { get; set; }
        public string id { get; set; }
        public object[] businessPhones { get; set; }
        public string displayName { get; set; }
        public string givenName { get; set; }
        public object jobTitle { get; set; }
        public string mail { get; set; }
        public object mobilePhone { get; set; }
        public object officeLocation { get; set; }
        public object preferredLanguage { get; set; }
        public string surname { get; set; }
        public string userPrincipalName { get; set; }
    }




}
