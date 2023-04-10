using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.MSOL
{


    public class GetCredentialTypeResp
    {
        public string Username { get; set; }
        public string Display { get; set; }
        public int IfExistsResult { get; set; }
        public bool IsUnmanaged { get; set; }
        public int ThrottleStatus { get; set; }
        public GetCredentialTypeCredentials Credentials { get; set; }
        public GetCredentialTypeEstsproperties EstsProperties { get; set; }
        public string FlowToken { get; set; }
        public bool IsSignupDisallowed { get; set; }
        public string apiCanary { get; set; }
    }

    public class GetCredentialTypeCredentials
    {
        public int PrefCredential { get; set; }
        public bool HasPassword { get; set; }
        public object RemoteNgcParams { get; set; }
        public object FidoParams { get; set; }
        public object SasParams { get; set; }
        public object CertAuthParams { get; set; }
        public object GoogleParams { get; set; }
        public object FacebookParams { get; set; }
    }

    public class GetCredentialTypeEstsproperties
    {
        public GetCredentialUsertenantbranding[] UserTenantBranding { get; set; }
        public int DomainType { get; set; }
    }

    public class GetCredentialUsertenantbranding
    {
        public int Locale { get; set; }
        public string BannerLogo { get; set; }
        public string TileLogo { get; set; }
        public string TileDarkLogo { get; set; }
        public string BackgroundColor { get; set; }
        public string BoilerPlateText { get; set; }
        public bool KeepMeSignedInDisabled { get; set; }
        public bool UseTransparentLightBox { get; set; }
    }



    public class GetCredCredentials
    {
        public int PrefCredential { get; set; }
        public bool HasPassword { get; set; }
        public object RemoteNgcParams { get; set; }
        public object FidoParams { get; set; }
        public object SasParams { get; set; }
        public object CertAuthParams { get; set; }
        public object GoogleParams { get; set; }
        public object FacebookParams { get; set; }
    }



}
