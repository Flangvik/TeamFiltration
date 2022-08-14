using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.MSOL
{
    public class GetAuthResponse
    {
        public bool fShowPersistentCookiesWarning { get; set; }
        public string urlMsaSignUp { get; set; }
        public string urlMsaLogout { get; set; }
        public string urlOtherIdpForget { get; set; }
        public bool showCantAccessAccountLink { get; set; }
        public string urlGitHubFed { get; set; }
        public bool fShowSignInWithGitHubOnlyOnCredPicker { get; set; }
        public bool fEnableShowResendCode { get; set; }
        public int iShowResendCodeDelay { get; set; }
        public string sSMSCtryPhoneData { get; set; }
        public string failureRedirectUrl { get; set; }
        public bool fUseInlinePhoneNumber { get; set; }
        public bool fDetectBrowserCapabilities { get; set; }
        public string urlSessionState { get; set; }
        public string urlResetPassword { get; set; }
        public string urlMsaResetPassword { get; set; }
        public string urlSignUp { get; set; }
        public string urlGetCredentialType { get; set; }
        public string urlGetOneTimeCode { get; set; }
        public string urlLogout { get; set; }
        public string urlForget { get; set; }
        public string urlDisambigRename { get; set; }
        public string urlGoToAADError { get; set; }
        public string urlPIAEndAuth { get; set; }
        public bool fCBShowSignUp { get; set; }
        public bool fKMSIEnabled { get; set; }
        public int iLoginMode { get; set; }
        public bool fAllowPhoneSignIn { get; set; }
        public bool fAllowPhoneInput { get; set; }
        public bool fAllowSkypeNameLogin { get; set; }
        public int iMaxPollErrors { get; set; }
        public int iPollingTimeout { get; set; }
        public bool srsSuccess { get; set; }
        public bool fShowSwitchUser { get; set; }
        public string[] arrValErrs { get; set; }
        public string sErrorCode { get; set; }
        public string sErrTxt { get; set; }
        public string sResetPasswordPrefillParam { get; set; }
        public Onprempasswordvalidationconfig onPremPasswordValidationConfig { get; set; }
        public bool fSwitchDisambig { get; set; }
        public Ocancelpostparams oCancelPostParams { get; set; }
        public int iRemoteNgcPollingType { get; set; }
        public Ogetcredtyperesult oGetCredTypeResult { get; set; }
        public bool fUseNewNoPasswordTypes { get; set; }
        public bool fAccessPassSupported { get; set; }
        public string urlAadSignup { get; set; }
        public int iMaxStackForKnockoutAsyncComponents { get; set; }
        public bool fShowButtons { get; set; }
        public bool fIsHosted { get; set; }
        public string urlCdn { get; set; }
        public string urlPost { get; set; }
        public string urlPostAad { get; set; }
        public string urlPostMsa { get; set; }
        public string urlRefresh { get; set; }
        public string urlCancel { get; set; }
        public string urlResume { get; set; }
        public string urlHostedPrivacyLink { get; set; }
        public string urlHostedTOULink { get; set; }
        public int iPawnIcon { get; set; }
        public int iPollingInterval { get; set; }
        public string sPOST_Username { get; set; }
        public string sFT { get; set; }
        public string sFTName { get; set; }
        public string sFTCookieName { get; set; }
        public string sSessionIdentifierName { get; set; }
        public string sCtx { get; set; }
        public int iProductIcon { get; set; }
        public string urlReportPageLoad { get; set; }
        public object staticTenantBranding { get; set; }
        public Oappcobranding oAppCobranding { get; set; }
        public int iBackgroundImage { get; set; }
        public object[] arrSessions { get; set; }
        public bool fApplicationInsightsEnabled { get; set; }
        public int iApplicationInsightsEnabledPercentage { get; set; }
        public string urlSetDebugMode { get; set; }
        public string sPrefillUsername { get; set; }
        public bool fEnableCssAnimation { get; set; }
        public bool fAllowGrayOutLightBox { get; set; }
        public bool fIsRemoteNGCSupported { get; set; }
        public string urlLogin { get; set; }
        public string urlDssoStatus { get; set; }
        public bool fUseSameSite { get; set; }
        public int iAllowedIdentities { get; set; }
        public bool isGlobalTenant { get; set; }
        public int uiflavor { get; set; }
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
        public Bsso bsso { get; set; }
        public string urlNoCookies { get; set; }
        public bool fTrimChromeBssoUrl { get; set; }
        public int inlineMode { get; set; }
        public bool fShowCopyDebugDetailsLink { get; set; }
    }

    public class Onprempasswordvalidationconfig
    {
        public bool isUserRealmPrecheckEnabled { get; set; }
    }

    public class Ocancelpostparams
    {
        public string error { get; set; }
        public string error_subcode { get; set; }
        public string add_account { get; set; }
    }

    public class Ogetcredtyperesult
    {
        public string Username { get; set; }
        public string Display { get; set; }
        public int IfExistsResult { get; set; }
        public bool IsUnmanaged { get; set; }
        public int ThrottleStatus { get; set; }
        public Credentials Credentials { get; set; }
        public Estsproperties EstsProperties { get; set; }
        public string FlowToken { get; set; }
        public bool IsSignupDisallowed { get; set; }
    }

    public class Credentials
    {
        public int PrefCredential { get; set; }
        public bool HasPassword { get; set; }
    }

    public class CallMetadata
    {
        public string LongRunningTransactionPartition { get; set; }
        public string HisRegion { get; set; }
        public string HisScaleUnit { get; set; }
        public bool IsLongRunningTransaction { get; set; }
    }

    public class Estsproperties
    {
        public Usertenantbranding[] UserTenantBranding { get; set; }
        public CallMetadata CallMetadata { get; set; }
        public int DomainType { get; set; }
    }

    public class Usertenantbranding
    {
        public int Locale { get; set; }
        public string BannerLogo { get; set; }
        public string Illustration { get; set; }
        public string BoilerPlateText { get; set; }
        public bool KeepMeSignedInDisabled { get; set; }
        public bool UseTransparentLightBox { get; set; }
    }

    public class Oappcobranding
    {
    }



    public class Bsso
    {
        public int initiatePullTimeoutMs { get; set; }
        public string initiatePullTimeoutAction { get; set; }
        public string rid { get; set; }
        public States states { get; set; }
        public string nonce { get; set; }
        public int overallTimeoutMs { get; set; }
        public Telemetry telemetry { get; set; }
        public string[] redirectEndStates { get; set; }
        public Cookienames cookieNames { get; set; }
        public bool enabled { get; set; }
        public string type { get; set; }
        public string reason { get; set; }
    }

    public class States
    {
        public string START { get; set; }
        public string INPROGRESS { get; set; }
        public string END { get; set; }
        public string END_SSO { get; set; }
        public string END_USERS { get; set; }
    }

    public class Telemetry
    {
        public string type { get; set; }
        public string nonce { get; set; }
        public string[] reportStates { get; set; }
    }

    public class Cookienames
    {
        public string aadSso { get; set; }
        public string winSso { get; set; }
        public string ssoTiles { get; set; }
        public string ssoPulled { get; set; }
        public string userList { get; set; }
    }
}
