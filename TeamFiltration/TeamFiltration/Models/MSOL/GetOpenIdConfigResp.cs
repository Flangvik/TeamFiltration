using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Models.MSOL
{


    public class GetOpenIdConfigResp
    {
        public string token_endpoint { get; set; }
        public string[] token_endpoint_auth_methods_supported { get; set; }
        public string jwks_uri { get; set; }
        public string[] response_modes_supported { get; set; }
        public string[] subject_types_supported { get; set; }
        public string[] id_token_signing_alg_values_supported { get; set; }
        public string[] response_types_supported { get; set; }
        public string[] scopes_supported { get; set; }
        public string issuer { get; set; }
        public bool microsoft_multi_refresh_token { get; set; }
        public string authorization_endpoint { get; set; }
        public string device_authorization_endpoint { get; set; }
        public bool http_logout_supported { get; set; }
        public bool frontchannel_logout_supported { get; set; }
        public string end_session_endpoint { get; set; }
        public string[] claims_supported { get; set; }
        public string check_session_iframe { get; set; }
        public string userinfo_endpoint { get; set; }
        public string kerberos_endpoint { get; set; }
        public string tenant_region_scope { get; set; }
        public string cloud_instance_name { get; set; }
        public string cloud_graph_host_name { get; set; }
        public string msgraph_host { get; set; }
        public string rbac_url { get; set; }
    }

    
}
