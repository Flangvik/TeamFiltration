using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.SharePoint
{



    public class GetLists
    {
        [JsonProperty("odata.context")]
        public string odatacontext { get; set; }
        public List<ListObject> value { get; set; }
    }

    public class ListObject
    {
        [JsonProperty("odata.etag")]
        public string odataetag { get; set; }
        public Createdby createdBy { get; set; }
        public DateTime createdDateTime { get; set; }
        public string description { get; set; }
        public string eTag { get; set; }
        public string id { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string name { get; set; }
        public Parentreference parentReference { get; set; }
        public string webUrl { get; set; }
        public string color { get; set; }
        public string displayName { get; set; }
        public string icon { get; set; }
        public List list { get; set; }
        public Lastmodifiedby lastModifiedBy { get; set; }
    }

    public class Createdby
    {
        public User user { get; set; }
    }

    public class User
    {
        public string displayName { get; set; }
        public string email { get; set; }
        public string id { get; set; }
    }

    public class Parentreference
    {
        public string siteId { get; set; }
    }

    public class List
    {
        public bool contentTypesEnabled { get; set; }
        public bool hidden { get; set; }
        public string template { get; set; }
        public string type { get; set; }
    }

    public class Lastmodifiedby
    {
        public User1 user { get; set; }
    }

    public class User1
    {
        public string email { get; set; }
        public string id { get; set; }
        public string displayName { get; set; }
    }




  

}
