using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{


    public class SubSitesResp
    {
        public List<SubSite> subSites { get; set; }
    }

    public class SubSite
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string webUrl { get; set; }
    }

}
