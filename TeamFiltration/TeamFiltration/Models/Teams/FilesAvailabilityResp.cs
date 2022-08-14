using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{
    public class FilesAvailabilityResp
    {
        public string personalSiteUrl { get; set; }
        public string personalRootFolderUrl { get; set; }
        public DateTime lastModifiedTime { get; set; }
    }
}
