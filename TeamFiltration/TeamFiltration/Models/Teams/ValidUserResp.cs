using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{


 
    public class ValidUserResp
    {
        public string tenantId { get; set; }
        public bool isShortProfile { get; set; }
        public bool accountEnabled { get; set; }
        public Featuresettings featureSettings { get; set; }
        public string userPrincipalName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }
        public string email { get; set; }
        public string displayName { get; set; }
        public string type { get; set; }
        public string mri { get; set; }
        public string objectId { get; set; }
    }

    public class Featuresettings
    {
        public string coExistenceMode { get; set; }
    }


}
