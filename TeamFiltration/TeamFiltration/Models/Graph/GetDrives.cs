using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{

    public class GetDrives
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<DriveObject> value { get; set; }
    }

    public class DriveObject
    {
        public DateTime createdDateTime { get; set; }
        public string description { get; set; }
        public string id { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string name { get; set; }
        public string webUrl { get; set; }
        public string driveType { get; set; }
        public Createdby createdBy { get; set; }
        public Lastmodifiedby lastModifiedBy { get; set; }
        public Quota quota { get; set; }
    }


    public class Quota
    {
        public int deleted { get; set; }
        public int remaining { get; set; }
        public int total { get; set; }
        public int used { get; set; }
    }

}
