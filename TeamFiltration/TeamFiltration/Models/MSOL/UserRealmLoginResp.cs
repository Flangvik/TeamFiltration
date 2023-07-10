using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Models.MSOL
{
    public class UserRealmLoginRespPretty
    {

        public string NameSpaceType { get; set; }
        public string DomainName { get; set; }
        public string FederationBrandName { get; set; }
        public string CloudInstanceName { get; set; }

        public static explicit operator UserRealmLoginRespPretty(UserRealmLoginResp v)
        {
            return new UserRealmLoginRespPretty()
            {
                DomainName = v.DomainName,
                CloudInstanceName = v.CloudInstanceName,
                FederationBrandName = v.FederationBrandName,
                NameSpaceType = v.NameSpaceType
            };
        }
    }

    public class UserRealmLoginResp
    {
        public int State { get; set; }
        public int UserState { get; set; }
        public string Login { get; set; }
        public string NameSpaceType { get; set; }
        public string DomainName { get; set; }
        public string FederationBrandName { get; set; }
        public string CloudInstanceName { get; set; }
        public string CloudInstanceIssuerUri { get; set; }
    }

}
