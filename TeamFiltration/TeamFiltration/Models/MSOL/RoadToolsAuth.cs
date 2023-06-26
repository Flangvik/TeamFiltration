using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Models.MSOL
{

    public class RoadToolsAuth
    {
        public string tokenType { get; set; }
        public string expiresOn { get; set; }
        public string tenantId { get; set; }
        public string _clientId { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string idToken { get; set; }
    }

}
