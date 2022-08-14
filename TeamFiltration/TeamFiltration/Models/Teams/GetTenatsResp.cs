using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{



    public class GetTenatsResp
    {
        public string tenantId { get; set; }
        public string tenantName { get; set; }
        public string userId { get; set; }
        public bool isInvitationRedeemed { get; set; }
        public string countryLetterCode { get; set; }
        public string userType { get; set; }
        public string tenantType { get; set; }
    }


}
