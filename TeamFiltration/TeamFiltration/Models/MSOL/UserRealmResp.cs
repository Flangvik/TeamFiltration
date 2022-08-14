using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.MSOL
{
    public class UserRealmResp
    {

        public bool Adfs { get; set; } = false;
        public bool UsGovCloud { get; set; } = false;
        public bool ThirdPartyAuth { get; set; } = false;
        public string ThirdPartyAuthUrl { get; set; } = "";
    }
}
