using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{

    public class TeamsFileResp
    {
        public string skipToken { get; set; }
        public string type { get; set; }
        public Value[] value { get; set; }
    }

    public class Value
    {
        public object[] versions { get; set; }
        public object[] activities { get; set; }
        public object[] sharedWithUsers { get; set; }
        public DateTime lastModifiedTime { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string objectId { get; set; }
        public string objectUrl { get; set; }
        public string openInWindowFileUrl { get; set; }
        public string teamDisplayName { get; set; }
        public Siteinfo siteInfo { get; set; }
    }

    public class Siteinfo
    {
        public string siteUrl { get; set; }
    }

}
