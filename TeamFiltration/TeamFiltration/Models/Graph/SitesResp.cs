using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{

    public class SitesResp
    {
        public string odatacontext { get; set; }
        public DateTime createdDateTime { get; set; }
        public string description { get; set; }
        public string id { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string name { get; set; }
        public string webUrl { get; set; }
        public string displayName { get; set; }
        public Root root { get; set; }
        public Sitecollection siteCollection { get; set; }
    }

    public class Root
    {
    }

    public class Sitecollection
    {
        public string hostname { get; set; }
    }



}
