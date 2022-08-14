using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.SharePoint
{

    public class GetItems
    {
        [JsonProperty("odata.context")]
        public string odatacontext { get; set; }
        public List<ItemObject> value { get; set; }
    }

    public class ItemObject
    {
        [JsonProperty("odata.etag")]
        public string odataetag { get; set; }
        public Createdby createdBy { get; set; }
        public DateTime createdDateTime { get; set; }
        public string eTag { get; set; }
        public string id { get; set; }
        public Lastmodifiedby lastModifiedBy { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public Parentreference parentReference { get; set; }
        public string webUrl { get; set; }
        public Contenttype contentType { get; set; }
    }

 
    public class Contenttype
    {
        public string id { get; set; }
        public string name { get; set; }
    }





}
