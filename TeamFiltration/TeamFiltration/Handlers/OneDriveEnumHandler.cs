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
                // Get tenant information using autodiscover
                var msolHandler = new MSOLHandler(_globalArgsHandler, "ENUM", null);
                var outlookAutoDiscover = await msolHandler.GetOutlookAutodiscover(_baseDomain);

                var tenantList = new List<string>();
                var mailList = new List<string>();
                var oneDriveList = new List<string>();

                if (outlookAutoDiscover?.Body?.GetFederationInformationResponseMessage?.Response?.Domains != null)
                {
                    // Extract tenant names
                    foreach (var domain in outlookAutoDiscover.Body.GetFederationInformationResponseMessage.Response.Domains)
                    {
                        if (domain.Contains(".onmicrosoft.com") && !domain.Contains(".mail.onmicrosoft.com"))
                        {
                            var cleanedTenant = domain.Replace(".onmicrosoft.com", "").ToLower();
                            tenantList.Add(cleanedTenant);
                        }
                        else if (domain.Contains(".mail.onmicrosoft.com"))
                        {
                            var cleanedMail = domain.Replace(".mail.onmicrosoft.com", "").ToLower();
                            mailList.Add(cleanedMail);
                        }
                    }

                    if (tenantList.Count > 0)
                    {
                        _databaseHandler.WriteLog(new Log("ENUM", $"Found {tenantList.Count()} possible tenants:"));
        
                        foreach (var tenant in tenantList)
                        {
                            _databaseHandler.WriteLog(new Log("ENUM", tenant));
                        }
     
                    }
                    else
                    {
                        _databaseHandler.WriteLog(new Log("ENUM", "Failed to find tenant name(s) using Outlook Autodiscovery! Exiting."));
                        return;
                    }

                    // Check OneDrive availability for each tenant
                    foreach (var tenant in tenantList)
                    {
                        var testHostname = $"{tenant}-my.sharepoint.com";
                        try
                        {
                            var hostEntry = await System.Net.Dns.GetHostEntryAsync(testHostname);
                            if (hostEntry != null)
                            {
                                oneDriveList.Add(tenant);
                                _databaseHandler.WriteLog(new Log("ENUM", $"Tenant URL {tenant}-my.sharepoint.com seems to resolve"));
                            }
                        }
                        catch
                        {
                            // DNS resolution failed, skip
                            continue;
                        }
                    }

                    if (oneDriveList.Count > 0)
                    {
                    
                        foreach (var oneDriveHost in oneDriveList)
                        {
                            
                        }
              

                        // Determine the primary tenant
                        if (oneDriveList.Count == 1)
                        {
                            this._tenantName = oneDriveList[0];
                        }
                        else
                        {
                            // Find matching mail record
                            var matchingMail = oneDriveList.Intersect(mailList).FirstOrDefault();
                            if (!string.IsNullOrEmpty(matchingMail))
                            {
                             
                                this._tenantName = matchingMail;
                            }
                            else
                            {
                          
                                this._tenantName = oneDriveList[0];
                            }
                            _databaseHandler.WriteLog(new Log("ENUM", $"Sharepoint URL {this._tenantName}-my.sharepoint.com will be used for validation attempts"));
                            _databaseHandler.WriteLog(new Log("ENUM", "If you do not get results, re-run and manually choose a different tenant"));
                        }
                    }
                }
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
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
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
