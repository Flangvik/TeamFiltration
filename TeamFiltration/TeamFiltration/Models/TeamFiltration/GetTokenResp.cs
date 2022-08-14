using System;
using System.Collections.Generic;
using System.Text;
using TeamFiltration.Models.MSOL;

namespace TeamFiltration.Models.TeamFiltration
{
    public class GetTokenResp
    {
        public LoginMFAAuthResponse MFAResponse { get; set; }
        public BearerTokenResp TokenResp { get; set; }
        public LoginErrorAuthResponse ErrorResp { get; set; }
    }
}
