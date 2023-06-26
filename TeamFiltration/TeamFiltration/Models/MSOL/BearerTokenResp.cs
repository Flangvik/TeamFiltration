using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Models.MSOL
{
    public class BearerTokenResp
    {
        public string token_type { get; set; }
        public string scope { get; set; }
        public string expires_in { get; set; }
        public string ext_expires_in { get; set; }
        public string expires_on { get; set; }
        public string not_before { get; set; }
        public string resource { get; set; }
        public string pwd_url { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int refresh_token_expires_in { get; set; }
        public string foci { get; set; }
        public string id_token { get; set; }

        public static explicit operator BearerTokenResp(PulledTokens v)
        {
            if (v != null)
                return JsonConvert.DeserializeObject<BearerTokenResp>(v.ResponseData);
            return null;
        }

        public static explicit operator BearerTokenResp(RoadToolsAuth v)
        {
            if (v != null)
                return new BearerTokenResp()
                {
                    access_token = v.accessToken,
                    refresh_token = v.refreshToken,
                    expires_in = v.expiresOn,
                    expires_on = v.expiresOn,
                    id_token = v.idToken,
                };
            return null;
        }
    }

}
