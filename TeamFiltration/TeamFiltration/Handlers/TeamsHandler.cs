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
                Address = new Uri(_globalArgsHandler.TeamFiltrationConfig.proxyEndpoint),
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

            _teamsClient.DefaultRequestHeaders.Add("User-Agent", teamFiltrationConfig.TeamFiltrationConfig.UserAgent);
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



        public async Task<(SkypeTokenResp, SkypeErrorRespons)> SetSkypeToken()
        {

            var getSkypeTokenUrl = "https://authsvc.teams.microsoft.com/v1.0/authz";
            var getSkypeTokenReq = await _teamsClient.PollyPostAsync(getSkypeTokenUrl, null);
            var getSkypeTokenResp = await getSkypeTokenReq.Content.ReadAsStringAsync();

            if (getSkypeTokenReq.IsSuccessStatusCode)
            {
                var getSkypeTokenDataResp = JsonConvert.DeserializeObject<SkypeTokenResp>(getSkypeTokenResp);

                _teamsClient.DefaultRequestHeaders.Add("Authentication", "skypetoken=" + getSkypeTokenDataResp.tokens.skypeToken);
                _teamsClient.DefaultRequestHeaders.Add("X-Skypetoken", getSkypeTokenDataResp.tokens.skypeToken);

                return (getSkypeTokenDataResp, null);
            }
            else
            {
                var getSkypeErrorResp = JsonConvert.DeserializeObject<SkypeErrorRespons>(getSkypeTokenResp);
                return (null, getSkypeErrorResp);
            }

        }

        public async Task<ChatLogResp> GetChatLogs(Conversation chatResp)
        {

            var fetchChatLogsReq = await _teamsClient.PollyGetAsync(chatResp.messages);
            var fetchChatLogsResp = await fetchChatLogsReq.Content.ReadAsStringAsync();
            var fetchChatLogsDataResp = JsonConvert.DeserializeObject<ChatLogResp>(fetchChatLogsResp);
            return fetchChatLogsDataResp;
        }

        public async Task<UserConversationsResp> GetThreads(string meetingId)
        {

            var getConversationUrl = $"https://{TeamsRegion}.ng.msg.teams.microsoft.com/v1/threads/{meetingId}?view=msnp24Equivalent";
            var getConversationsReq = await _teamsClient.PollyGetAsync(getConversationUrl);
            var getConversationResp = await getConversationsReq.Content.ReadAsStringAsync();
            var getConversationDataResp = JsonConvert.DeserializeObject<UserConversationsResp>(getConversationResp);
            return getConversationDataResp;
        }


        public async Task<UserConversationsResp> GetConversations()
        {

            var getConversationUrl = $"https://{TeamsRegion}.ng.msg.teams.microsoft.com/v1/users/ME/conversations";
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

        public async Task<(bool isValid, string objectId, TeamsExtSearchRep responseObject, Outofofficenote Outofofficenote)> EnumUser(string username, string enumUserUrl)
        {
            Outofofficenote Outofofficenote = new Outofofficenote() { };
            int failedCount = 0;

        failedResp:
            //TODO:Add logic to select FireProx endpoint based on current location 
            var enumUserReq = await _teamsClient.GetAsync(enumUserUrl + $"{TeamsRegion}/beta/users/{username}/externalsearchv3");
            if (enumUserReq.IsSuccessStatusCode)
            {

                //We got an 200 OK response
                var userResp = await enumUserReq.Content.ReadAsStringAsync();


                //Indication of valid JSOn response
                if (userResp.Contains("tenantId"))
                {
                    //get the object
                    List<TeamsExtSearchRep> usersFoundObject = JsonConvert.DeserializeObject<List<TeamsExtSearchRep>>(userResp);

                    //Any size
                    if (usersFoundObject.Count() > 0)
                    {
                        foreach (var responeObject in usersFoundObject)
                        {


                            if (
                                //Check that the TenantID is not null
                                responeObject.tenantId != null

                                //Check that the coExistenceMode is not Unknown
                                && !responeObject.featureSettings.coExistenceMode.Equals("Unknown")

                                //Check that the Display != Equals email OR that the UPN = userPrincipalName
                                && (!responeObject.displayName.Equals(username) || responeObject.userPrincipalName.ToLower().Equals(username.ToLower())))
                            {

                                try
                                {

                                    //Check the user presence
                                    HttpResponseMessage getUserPresence = await _teamsClient.PollyPostAsync(
                                        $"https://presence.teams.microsoft.com/v1/presence/getpresence/",

                                        new StringContent(
                                            "[{ \"mri\":\"" + responeObject.mri + "\"}]"
                                            , Encoding.UTF8
                                            , "application/json"
                                            )
                                        );


                                    var getPresenceObject = JsonConvert.DeserializeObject<List<GetPresenceResp>>(await getUserPresence.Content.ReadAsStringAsync());

                                    if (getPresenceObject.FirstOrDefault()?.presence?.calendarData?.isOutOfOffice != null)
                                    {
                                        Outofofficenote = getPresenceObject.FirstOrDefault()?.presence?.calendarData.outOfOfficeNote;
                                    }
                                }
                                catch (Exception ex)
                                {


                                }

                                return (true, responeObject.objectId, responeObject, Outofofficenote);
                            }
                        }
                    }
                }
                return (false, "", null, null);

            }
            else if (enumUserReq.StatusCode.Equals(HttpStatusCode.Forbidden))
            {
                //We got an 200 OK response
                var userResp = await enumUserReq.Content.ReadAsStringAsync();

                if (userResp.Equals("{\"errorCode\":\"Forbidden\"}"))
                    //As of 24.04.2023 - Seems like MS have patched this.
                    //return (false, "", null, null);
                    return (true, "", null, null);
            }
            else if (enumUserReq.StatusCode.Equals(HttpStatusCode.InternalServerError))
            {
                failedCount++;
                if (failedCount > 2)
                    return (false, "", null, null);
                else
                    goto failedResp;
            }
            return (false, "", null, null);

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
