using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.MSOL
{
    public class LoginAuthResponse
    {
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
        public bool isMetro2Ux { get; set; }
        public bool fFixIncorrectApiCanaryUsage { get; set; }
        public string urlNoCookies { get; set; }
        public bool fTrimChromeBssoUrl { get; set; }
        public int inlineMode { get; set; }
        public bool fShowCopyDebugDetailsLink { get; set; }
    }

    public class Locale
    {
        public string mkt { get; set; }
        public int lcid { get; set; }
    }

    public class Strings
    {
        public Desktopsso desktopsso { get; set; }
        public Mfa mfa { get; set; }
    }

    public class Desktopsso
    {
        public string authenticatingmessage { get; set; }
    }

    public class Mfa
    {
        public string setitupnow { get; set; }
    }

    public class Enums
    {
        public Clientmetricsmodes ClientMetricsModes { get; set; }
    }

    public class Clientmetricsmodes
    {
        public int None { get; set; }
        public int SubmitOnPost { get; set; }
        public int SubmitOnRedirect { get; set; }
        public int InstrumentPlt { get; set; }
    }

    public class Urls
    {
        public Instr instr { get; set; }
    }

    public class Instr
    {
        public string pageload { get; set; }
        public string dssostatus { get; set; }
    }

    public class Browser
    {
        public int ltr { get; set; }
        public int Edge { get; set; }
        public int _Win { get; set; }
        public int _M18 { get; set; }
        public int _D1 { get; set; }
        public int Full { get; set; }
        public int Win81 { get; set; }
        public int RE_Edge { get; set; }
        public B b { get; set; }
        public Os os { get; set; }
        public string V { get; set; }
    }

    public class B
    {
        public string name { get; set; }
        public int major { get; set; }
        public int minor { get; set; }
    }

    public class Os
    {
        public string name { get; set; }
        public string version { get; set; }
    }

    public class Watson
    {
        public string url { get; set; }
        public string bundle { get; set; }
        public string sbundle { get; set; }
        public string fbundle { get; set; }
        public int resetErrorPeriod { get; set; }
        public int maxCorsErrors { get; set; }
        public int maxInjectErrors { get; set; }
        public int maxErrors { get; set; }
        public int maxTotalErrors { get; set; }
        public string[] expSrcs { get; set; }
    }

    public class Loader
    {
        public string[] cdnRoots { get; set; }
        public bool logByThrowing { get; set; }
    }

    public class Serverdetails
    {
        public string slc { get; set; }
        public string dc { get; set; }
        public string ri { get; set; }
        public Ver ver { get; set; }
        public DateTime rt { get; set; }
        public int et { get; set; }
    }

    public class Ver
    {
        public int[] v { get; set; }
    }
}
