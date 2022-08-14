using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.MSOL
{

    public class LoginMFAAuthResponse
    {
        public Arruserproof[] arrUserProofs { get; set; }
        public bool fHideIHaveCodeLink { get; set; }
        public Operauthpollinginterval oPerAuthPollingInterval { get; set; }
        public bool fProofIndexedByType { get; set; }
        public string urlBeginAuth { get; set; }
        public string urlEndAuth { get; set; }
        public int iSAMode { get; set; }
        public int iTrustedDeviceCheckboxConfig { get; set; }
        public int iMaxPollAttempts { get; set; }
        public int iPollingTimeout { get; set; }
        public float iPollingBackoffInterval { get; set; }
        public string sTrustedDeviceCheckboxName { get; set; }
        public string sAuthMethodInputFieldName { get; set; }
        public int iSAOtcLength { get; set; }
        public int iTotpOtcLength { get; set; }
        public bool fShowViewDetailsLink { get; set; }
        public int iMaxStackForKnockoutAsyncComponents { get; set; }
        public bool fShowButtons { get; set; }
        public bool fIsHosted { get; set; }
        public string urlCdn { get; set; }
        public string urlPost { get; set; }
        public string urlPostMsa { get; set; }
        public string urlCancel { get; set; }
        public string urlHostedPrivacyLink { get; set; }
        public string urlHostedTOULink { get; set; }
        public int iPawnIcon { get; set; }
        public int iPollingInterval { get; set; }
        public string sPOST_Username { get; set; }
        public string sFT { get; set; }
        public string sFTName { get; set; }
        public string sCtx { get; set; }
        public string urlReportPageLoad { get; set; }
        public Dynamictenantbranding[] dynamicTenantBranding { get; set; }
        public object staticTenantBranding { get; set; }
        public Oappcobranding oAppCobranding { get; set; }
        public int iBackgroundImage { get; set; }
        public bool fApplicationInsightsEnabled { get; set; }
        public int iApplicationInsightsEnabledPercentage { get; set; }
        public string urlSetDebugMode { get; set; }
        public string sPrefillUsername { get; set; }
        public bool fEnableCssAnimation { get; set; }
        public bool fAllowGrayOutLightBox { get; set; }
        public bool fIsRemoteNGCSupported { get; set; }
        public bool fUseSameSite { get; set; }
        public bool isGlobalTenant { get; set; }
        public string urlFidoHelp { get; set; }
        public string urlFidoLogin { get; set; }
        public bool fIsFidoSupported { get; set; }
        public int scid { get; set; }
        public int hpgact { get; set; }
        public int hpgid { get; set; }
        public string pgid { get; set; }
        public string apiCanary { get; set; }
        public string canary { get; set; }
        public string correlationId { get; set; }
        public string sessionId { get; set; }
        public Locale locale { get; set; }
        public int slMaxRetry { get; set; }
        public bool slReportFailure { get; set; }
        public Strings strings { get; set; }
        public Enums enums { get; set; }
        public Urls urls { get; set; }
        public Browser browser { get; set; }
        public Watson watson { get; set; }
        public Loader loader { get; set; }
        public Serverdetails serverDetails { get; set; }
        public string country { get; set; }
        public bool fBreakBrandingSigninString { get; set; }
        public bool fFixIncorrectApiCanaryUsage { get; set; }
        public string urlNoCookies { get; set; }
        public bool fTrimChromeBssoUrl { get; set; }
        public int inlineMode { get; set; }
        public bool fShowCopyDebugDetailsLink { get; set; }
    }

    public class Operauthpollinginterval
    {
        public float PhoneAppNotification { get; set; }
    }

    public class Arruserproof
    {
        public string authMethodId { get; set; }
        public string data { get; set; }
        public string display { get; set; }
        public bool isDefault { get; set; }
        public bool isLocationAware { get; set; }
        public bool PhoneAppNotificationNumberMatching { get; set; }
        public bool PhoneAppNotificationLocation { get; set; }
        public bool PhoneAppNotificationApplicationName { get; set; }
    }

    public class Dynamictenantbranding
    {
        public int Locale { get; set; }
        public string BannerLogo { get; set; }
        public string TileLogo { get; set; }
        public string TileDarkLogo { get; set; }
        public string Illustration { get; set; }
        public string BackgroundColor { get; set; }
        public string BoilerPlateText { get; set; }
        public bool KeepMeSignedInDisabled { get; set; }
        public bool UseTransparentLightBox { get; set; }
    }

}
