using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{
    public class DomainIdResp
    {
        public string authenticationType { get; set; }
        public string availabilityStatus { get; set; }
        public string id { get; set; }
        public bool isAdminManaged { get; set; }
        public bool isDefault { get; set; }
        public bool isInitial { get; set; }
        public bool isRoot { get; set; }
    }

}
