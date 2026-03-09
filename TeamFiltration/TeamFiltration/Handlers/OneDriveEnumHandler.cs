using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Handlers
{
    public class OneDriveEnumHandler
    {

        public static HttpClient _teamsClient;
        public GlobalArgumentsHandler _globalArgsHandler;
        private DatabaseHandler _databaseHandler { get; set; }

        public string _tenantName { get; set; }
        public string _sharePointEndpoint { get; set; }
        public string _baseDomain { get; set; }

        public OneDriveEnumHandler(GlobalArgumentsHandler teamFiltrationConfig, string tenantName, string sharepointEndpoint, string _baseDomain, DatabaseHandler databaseHandler)
        {

            this._tenantName = tenantName;
            this._sharePointEndpoint = sharepointEndpoint;
            this._baseDomain = _baseDomain;
            this._globalArgsHandler = teamFiltrationConfig;
            this._databaseHandler = databaseHandler;
        }

        public async Task enumTenantName()
        {
            try
            {
                var msolHandler = new MSOLHandler(_globalArgsHandler, "ENUM", null);

                // Step 1: get tenant ID from OpenID config (used as a second ACS lookup key)
                var openIdConfig = await msolHandler.GetOpenIdConfig(_baseDomain);
                var tenantId = openIdConfig?.authorization_endpoint?.Split('/')?[3];

                // Step 2: query ACS with both the domain and the tenant GUID, union results
                var acsDomainsFromDomain = await msolHandler.GetAcsDomains(_baseDomain);
                var acsDomainsFromTenantId = !string.IsNullOrEmpty(tenantId)
                    ? await msolHandler.GetAcsDomains(tenantId)
                    : new List<string>();

                var allDomains = acsDomainsFromDomain
                    .Union(acsDomainsFromTenantId)
                    .Distinct()
                    .ToList();

                // Step 3: find .onmicrosoft.com candidates (excluding .mail.onmicrosoft.com)
                var tenantList = allDomains
                    .Where(d => d.EndsWith(".onmicrosoft.com") && !d.Contains(".mail.onmicrosoft.com"))
                    .Select(d => d.Replace(".onmicrosoft.com", ""))
                    .ToList();

                var mailList = allDomains
                    .Where(d => d.EndsWith(".mail.onmicrosoft.com"))
                    .Select(d => d.Replace(".mail.onmicrosoft.com", ""))
                    .ToList();

                if (tenantList.Count == 0)
                {
                    _databaseHandler.WriteLog(new Log("ENUM", "Failed to find tenant name(s) via ACS metadata! Exiting."));
                    return;
                }

                _databaseHandler.WriteLog(new Log("ENUM", $"Found {tenantList.Count} possible tenant(s) via ACS:"));
                foreach (var tenant in tenantList)
                    _databaseHandler.WriteLog(new Log("ENUM", tenant));

                // Step 4: DNS-probe each candidate to confirm OneDrive/SharePoint resolves
                var oneDriveList = new List<string>();
                foreach (var tenant in tenantList)
                {
                    var testHostname = $"{tenant}-my.sharepoint.com";
                    try
                    {
                        var hostEntry = await System.Net.Dns.GetHostEntryAsync(testHostname);
                        if (hostEntry != null)
                        {
                            oneDriveList.Add(tenant);
                            _databaseHandler.WriteLog(new Log("ENUM", $"{testHostname} resolves — candidate confirmed"));
                        }
                    }
                    catch
                    {
                        // DNS resolution failed for this candidate, skip
                    }
                }

                if (oneDriveList.Count == 0)
                {
                    _databaseHandler.WriteLog(new Log("ENUM", "No OneDrive-my SharePoint hostnames resolved. Cannot continue."));
                    return;
                }

                // Step 5: pick winner — prefer the one that matches the mail subdomain
                if (oneDriveList.Count == 1)
                {
                    this._tenantName = oneDriveList[0];
                }
                else
                {
                    var matchingMail = oneDriveList.Intersect(mailList).FirstOrDefault();
                    this._tenantName = !string.IsNullOrEmpty(matchingMail) ? matchingMail : oneDriveList[0];
                    _databaseHandler.WriteLog(new Log("ENUM", "If you do not get results, re-run and manually choose a different tenant"));
                }

                _databaseHandler.WriteLog(new Log("ENUM", $"Using {this._tenantName}-my.sharepoint.com for validation"));
            }
            catch (Exception ex)
            {
                _databaseHandler.WriteLog(new Log("ENUM", $"Failed to enumerate tenant name: {ex.Message}"));
                throw new Exception($"Failed to enumerate tenant name: {ex.Message}", ex);
            }
        }
        public async Task<bool> ValidateO365Account(string username)
        {
         

            var proxy = new WebProxy
            {
                Address = new Uri(_globalArgsHandler.TeamFiltrationConfig.proxyEndpoint),
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
                SslProtocols = SslProtocols.None,
                UseProxy = false
            };

            using (var clientHttp = new HttpClient(httpClientHandler))
            {
                clientHttp.Timeout = new TimeSpan(0, 0, 8);

                var user_url = $"https://{_tenantName}-my.{_sharePointEndpoint}/personal/{username.Replace('@','_').Replace('.','_')}/_layouts/15/onedrive.aspx";
              
                
                var postAsyncReq = await clientHttp.SendAsync(new HttpRequestMessage(HttpMethod.Head, user_url));

                //If statuscod is ['404', '301', '302']: return false
                if (postAsyncReq.StatusCode == HttpStatusCode.NotFound || postAsyncReq.StatusCode == HttpStatusCode.Redirect || postAsyncReq.StatusCode == HttpStatusCode.RedirectKeepVerb)
                {
                    return false;
                }
                //else if statuscode is ['401', '403']: return true
                else if (postAsyncReq.StatusCode == HttpStatusCode.Unauthorized || postAsyncReq.StatusCode == HttpStatusCode.Forbidden)
                {
                    return true;
                }
            }

            return false;

        }

    }
}
