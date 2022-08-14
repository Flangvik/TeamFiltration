using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.OWA
{


    public class AttachResp
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public bool HasAttachments { get; set; }
        public Value[] value { get; set; }
    }

    public class Value
    {
        [JsonProperty("@odata.context")]
        public string odatatype { get; set; }

        [JsonProperty("@odata.id")]
        public string odataid { get; set; }

        [JsonProperty("@odata.readLink")]
        public string odatareadLink { get; set; }

        [JsonProperty("@odata.mediaContentType")]
        public string odatamediaContentType { get; set; }
        public string Id { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public int Size { get; set; }
        public bool IsInline { get; set; }
        public string ContentId { get; set; }
        public object ContentLocation { get; set; }
        public string ContentBytes { get; set; }
    }

  
}
