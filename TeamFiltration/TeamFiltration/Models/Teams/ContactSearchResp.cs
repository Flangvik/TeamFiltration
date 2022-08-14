using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{


    public class ContactSearchResp
    {
        public string type { get; set; }
        public Contact[] value { get; set; }
    }

    public class Contact
    {
        public string mail { get; set; }
        public string objectType { get; set; }
        public string sipProxyAddress { get; set; }
        public string[] smtpAddresses { get; set; }
        public bool isShortProfile { get; set; }
        public string peopleType { get; set; }
        public string peopleSubType { get; set; }
        public Phone[] phones { get; set; }
        public string responseSourceInformation { get; set; }
        public string userPrincipalName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }
        public string email { get; set; }
        public string userType { get; set; }
        public string displayName { get; set; }
        public string type { get; set; }
        public string mri { get; set; }
        public string objectId { get; set; }
        public bool isUnified { get; set; }
        public string description { get; set; }
    }

    public class Phone
    {
        public string type { get; set; }
        public string number { get; set; }
    }

  
}
