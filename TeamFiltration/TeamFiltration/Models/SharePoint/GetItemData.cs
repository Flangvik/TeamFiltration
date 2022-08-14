using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.SharePoint
{


    public class GetItemData
    {
        [JsonProperty("odata.context")]
        public string odatacontext { get; set; }
        
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

        [JsonProperty("fields@odata.navigationLink")]
        public string fieldsodatanavigationLink { get; set; }
        public Fields fields { get; set; }
    }

   

    public class Fields
    {
        [JsonProperty("odata.etag")]
        public string odataetag { get; set; }
        public int ID { get; set; }
        public string ContentType { get; set; }
        public string FileLeafRef { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Created { get; set; }
        public string AuthorLookupId { get; set; }
        public string EditorLookupId { get; set; }
        public string _UIVersionString { get; set; }
        public bool Attachments { get; set; }
        public string Edit { get; set; }
        public string LinkTitleNoMenu { get; set; }
        public string LinkTitle { get; set; }
        public string ItemChildCount { get; set; }
        public string FolderChildCount { get; set; }
        public string _ComplianceFlags { get; set; }
        public string _ComplianceTag { get; set; }
        public string _ComplianceTagWrittenTime { get; set; }
        public string _ComplianceTagUserId { get; set; }
    }

    
}
