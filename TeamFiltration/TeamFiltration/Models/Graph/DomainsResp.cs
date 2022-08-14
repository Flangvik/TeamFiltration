using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{


    public class DomainsResp
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<DomainObject> value { get; set; }
    }

    public class DomainObject
    {
        public string authenticationType { get; set; }
        public object availabilityStatus { get; set; }
        public string id { get; set; }
        public bool isAdminManaged { get; set; }
        public bool isDefault { get; set; }
        public bool isInitial { get; set; }
        public bool isRoot { get; set; }
        public bool isVerified { get; set; }
        public string[] supportedServices { get; set; }
        public object state { get; set; }
        public int? passwordValidityPeriodInDays { get; set; }
        public int? passwordNotificationWindowInDays { get; set; }
    }

}
