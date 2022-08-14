using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{
   


    public class WorkingWithResp
    {
        public string type { get; set; }
        public ContactObject[] value { get; set; }
    }

    public class ContactObject
    {
        public string userPrincipalName { get; set; }
        public string email { get; set; }
        public bool isShortProfile { get; set; }
        public string displayName { get; set; }
        public string mri { get; set; }
        public string objectId { get; set; }
    }

}
