using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Models.Teams;
using TeamFiltration.Helpers;


namespace TeamFiltration.Handlers
{
    class TeamsHandler
    {
        public static HttpClient _teamsClient;
        public static GlobalArgumentsHandler _globalArgsHandler;

        // APAC =  Asia-Pacific
        // EMEA = EU
        // AMER = US

        public static string TeamsRegion = "amer";

        public TeamsHandler(BearerTokenResp getBearToken, GlobalArgumentsHandler teamFiltrationConfig)
        {
            _globalArgsHandler = teamFiltrationConfig;
            var proxy = new WebProxy
            {
                Address = new Uri($"http://127.0.0.1:8080"),
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
                UseProxy = _globalArgsHandler.DebugMode
            };


            _teamsClient = new HttpClient(httpClientHandler);
            _teamsClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {getBearToken.access_token}");
            /*
            teamsClient.DefaultRequestHeaders.Add("x-client-current-telemetry", " 2|29,1|,,,,,");
            teamsClient.DefaultRequestHeaders.Add("x-client-CPU", "x86");
            teamsClient.DefaultRequestHeaders.Add("x-client-Ver", "2.0.4");
            teamsClient.DefaultRequestHeaders.Add("x-client-brkrver", "3.3.9");
            teamsClient.DefaultRequestHeaders.Add("x-client-DM", "SM-G955F");
            teamsClient.DefaultRequestHeaders.Add("x-client-OS", "25");
            teamsClient.DefaultRequestHeaders.Add("x-app-ver", "1416/1.0.0.2021012201");
            teamsClient.DefaultRequestHeaders.Add("x-client-SKU", "MSAL.Android");
            teamsClient.DefaultRequestHeaders.Add("x-app-name", "com.microsoft.teams");
            */
            _teamsClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Teams/1.3.00.30866 Chrome/80.0.3987.165 Electron/8.5.1 Safari/537.36");
            _teamsClient.DefaultRequestHeaders.Add("x-ms-client-caller", "x-ms-client-caller");
            _teamsClient.DefaultRequestHeaders.Add("x-ms-client-version", "27/1.0.0.2021011237");
            _teamsClient.DefaultRequestHeaders.Add("Referer", "https://teams.microsoft.com/_");
            _teamsClient.DefaultRequestHeaders.Add("ClientInfo", "os=Android; osVer=7.1.2; proc=x86; lcid=en-US; deviceType=2; country=US; clientName=microsoftteams; clientVer=1416/1.0.0.2021012201; utcOffset=+01:00");
        }


        public async Task<TeamsFileResp> GetRecentFiles(string module)
        {

            var recentFilesUrl = $"https://teams.microsoft.com/api/mt/{TeamsRegion}/beta/me/recent/files?";
            var recentFilesReq = await _teamsClient.PollyGetAsync(recentFilesUrl);
            var recentFilesResp = await recentFilesReq.Content.ReadAsStringAsync();
            var recentFilesRespData = JsonConvert.DeserializeObject<TeamsFileResp>(recentFilesResp);
            return recentFilesRespData;

        }


        public async Task<AccountPropResp> GetAccountInfo()
        {

            var enumAccountUrl = "https://teams.microsoft.com/v1/users/ME/properties";
            var enumAccountReq = await _teamsClient.PollyGetAsync(enumAccountUrl, 0);
            var enumAccountResp = await enumAccountReq.Content.ReadAsStringAsync();
            var enumAccountDataResp = JsonConvert.DeserializeObject<AccountPropResp>(enumAccountResp);

            return enumAccountDataResp;
        }

        public async Task<List<GetTenatsResp>> GetTenantsInfo()
        {

            var enumTenantUrl = $"https://teams.microsoft.com/api/mt/{TeamsRegion}/beta/users/tenants";
            var enumTenantReq = await _teamsClient.PollyGetAsync(enumTenantUrl, 0);
            var enumTenantResp = await enumTenantReq.Content.ReadAsStringAsync();
            var enumTenantRespData = JsonConvert.DeserializeObject<List<GetTenatsResp>>(enumTenantResp);

            return enumTenantRespData;
        }



        public async Task<SkypeTokenResp> SetSkypeToken()
        {

            var getSkypeTokenUrl = "https://authsvc.teams.microsoft.com/v1.0/authz";
            var getSkypeTokenReq = await _teamsClient.PollyPostAsync(getSkypeTokenUrl, null);
            //var getSkypeTokenReq = await _teamsClient.PostAsync(getSkypeTokenUrl, null);
            var getSkypeTokenResp = await getSkypeTokenReq.Content.ReadAsStringAsync();
            var getSkypeTokenDataResp = JsonConvert.DeserializeObject<SkypeTokenResp>(getSkypeTokenResp);

            _teamsClient.DefaultRequestHeaders.Add("Authentication", "skypetoken=" + getSkypeTokenDataResp.tokens.skypeToken);
            _teamsClient.DefaultRequestHeaders.Add("X-Skypetoken", getSkypeTokenDataResp.tokens.skypeToken);

            return getSkypeTokenDataResp;


        }

        public async Task<ChatLogResp> GetChatLogs(Conversation chatResp)
        {

            var fetchChatLogsReq = await _teamsClient.PollyGetAsync(chatResp.messages);
            var fetchChatLogsResp = await fetchChatLogsReq.Content.ReadAsStringAsync();
            var fetchChatLogsDataResp = JsonConvert.DeserializeObject<ChatLogResp>(fetchChatLogsResp);
            return fetchChatLogsDataResp;
        }

        public async Task<UserConversationsResp> GetConversations()
        {
            //TODO: Find the US eq for "no.ng.msg"
            var getConversationUrl = "https://no.ng.msg.teams.microsoft.com/v1/users/ME/conversations";
            var getConversationsReq = await _teamsClient.PollyGetAsync(getConversationUrl);
            var getConversationResp = await getConversationsReq.Content.ReadAsStringAsync();
            var getConversationDataResp = JsonConvert.DeserializeObject<UserConversationsResp>(getConversationResp);


            return getConversationDataResp;
        }

        public async Task<WorkingWithResp> GetWorkingWithList(string tenantId)
        {
            var workingWithListUrl = $"https://teams.microsoft.com/api/mt/{TeamsRegion}/beta/users/8:orgid:{tenantId}/workingWith?enableEnhancedSearch=false&enableGuest=true";
            var workingWithListReq = await _teamsClient.PollyGetAsync(workingWithListUrl);
            var workingWithDataResp = JsonConvert.DeserializeObject<WorkingWithResp>(await workingWithListReq.Content.ReadAsStringAsync());

            return workingWithDataResp;
        }

        public async Task<(bool isValid, string objectId, TeamsExtSearchRep responseObject)> EnumUser(string username)
        {

            int failedCount = 0;

        failedResp:
            //TODO:Add logic to select FireProx endpoint based on current location 
            var enumUserUrl = _globalArgsHandler.TeamFiltrationConfig.TeamsEnumFireProxEndpoints[(new Random()).Next(0, _globalArgsHandler.TeamFiltrationConfig.TeamsEnumFireProxEndpoints.Count())] + $"{TeamsRegion}/beta/users/{username}/externalsearchv3";
            var enumUserReq = await _teamsClient.PollyGetAsync(enumUserUrl);
    


            if (enumUserReq.IsSuccessStatusCode)
            {

                //We got an 200 OK response
                var userResp = await enumUserReq.Content.ReadAsStringAsync();

                //Indication of valid JSOn response
                if (userResp.Contains("tenantId"))
                {
                    //get the object
                    List<TeamsExtSearchRep> responeObject = JsonConvert.DeserializeObject<List<TeamsExtSearchRep>>(userResp);
                    //Console.WriteLine(JsonConvert.SerializeObject(responeObject, Formatting.Indented));
                    //Any size
                    if (responeObject.Count() > 0)
                    {

                        if (
                            //Check that the TenantID is not null
                            responeObject.FirstOrDefault().tenantId != null

                            //Check that the coExistenceMode is not Unknown
                            && !responeObject.FirstOrDefault().featureSettings.coExistenceMode.Equals("Unknown")

                            //Check that the Display != Equals email. 
                            && !responeObject.FirstOrDefault().displayName.Equals(username)

                            //Check that the UPN matches the email your are looking for
                            && responeObject.FirstOrDefault().userPrincipalName.ToLower().Equals(username.ToLower())
                            )
                        {
                            return (true, responeObject.FirstOrDefault().objectId, responeObject.FirstOrDefault());
                        }
                    }
                }
                return (false, "", null);

            }
            else if (enumUserReq.StatusCode.Equals(HttpStatusCode.Forbidden))
            {
                //If we get the forbidden error response, we can assume it's valid!
                return (true, Guid.NewGuid().ToString(), null);
            }
            else if (enumUserReq.StatusCode.Equals(HttpStatusCode.InternalServerError))
            {
                failedCount++;
                if (failedCount > 2)
                    return (false, "", null);
                else
                    goto failedResp;
            }
            return (false, "", null);

        }

        public async Task<(byte[] byteArray, string fileName)> DownloadAttachment(string url, string fileName)
        {

            var downloadAttachReq = await _teamsClient.PollyGetAsync(url);
            var attachBytes = await downloadAttachReq.Content.ReadAsByteArrayAsync();

            return (attachBytes, fileName);
        }

        public async Task<ContactSearchResp> GetContactList(string searchTerm = "@")
        {
            var enumContactListUrl = $"https://teams.microsoft.com/api/mt/{TeamsRegion}/beta/users/searchV2?includeDLs=true&includeBots=true&enableGuest=true&source=newChat&skypeTeamsInfo=true";
            var enumContactListReq = await _teamsClient.PollyPostAsync(enumContactListUrl, new StringContent(searchTerm, Encoding.UTF8, "application/json"));
            var enumContactListDataResp = JsonConvert.DeserializeObject<ContactSearchResp>(await enumContactListReq.Content.ReadAsStringAsync());

            return enumContactListDataResp;
        }

        public async Task<FilesAvailabilityResp> GetSharePointInfo()
        {

            var enumSharePointUrl = $"https://teams.microsoft.com/api/mt/{TeamsRegion}/beta/me/files/availability";
            var enumSharePoinReq = await _teamsClient.PollyPostAsync(enumSharePointUrl, null, 0);
            var enumSharePointRes = JsonConvert.DeserializeObject<FilesAvailabilityResp>(await enumSharePoinReq.Content.ReadAsStringAsync());

            return enumSharePointRes;
        }
    }
}
