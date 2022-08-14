using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Helpers;
using TeamFiltration.Models.Graph;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Handlers
{
    class GraphHandler
    {



        private HttpClient _graphClient;
        public GraphHandler(BearerTokenResp getBearToken, string username, bool debugMode = false)
        {
            // This is for debug , eg burp
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
                UseProxy = debugMode
            };

            _graphClient = new HttpClient(httpClientHandler);
            _graphClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {getBearToken.access_token}");


        }
       
        public async Task<GetMembers> GetGroupMembersMsGraph(string groupId)
        {
         
            //TODO: Implement paging for the grap ms API
            var getGroupMemReq = await _graphClient.PollyGetAsync($"https://graph.microsoft.com/v1.0/groups/{groupId}/members");
            var getGroupMemResp = await getGroupMemReq.Content.ReadAsStringAsync();
            var getGroupMemDataResp = JsonConvert.DeserializeObject<GetMembers>(getGroupMemResp);

            return getGroupMemDataResp;
        }
        public async Task<DomainIdResp> GetDomainMsGraph(string domainId)
        {
            //TODO: Implement paging for the grap ms API
            var GetDomainsMsGraphReq = await _graphClient.PollyGetAsync("https://graph.microsoft.com/v1.0/domains/" + domainId);
            var GetDomainsMsGraphResp = await GetDomainsMsGraphReq.Content.ReadAsStringAsync();
            var GetDomainsMsGraphDataResp = JsonConvert.DeserializeObject<DomainIdResp>(GetDomainsMsGraphResp);

            return GetDomainsMsGraphDataResp;
        }
        public async Task<DomainsResp> GetDomainsMsGraph()
        {

            var GetDomainsMsGraphReq = await _graphClient.PollyGetAsync("https://graph.microsoft.com/v1.0/domains");
            var GetDomainsMsGraphResp = await GetDomainsMsGraphReq.Content.ReadAsStringAsync();
            var GetDomainsMsGraphDataResp = JsonConvert.DeserializeObject<DomainsResp>(GetDomainsMsGraphResp);

            bool fetchedAll = false;
            if (string.IsNullOrEmpty(GetDomainsMsGraphDataResp.odatanextLink))
                fetchedAll = true;

            while (!fetchedAll)
            {

                var GetDomainsMsGraphReqNextLink = await _graphClient.PollyGetAsync(GetDomainsMsGraphDataResp.odatanextLink);
                var GetDomainsMsGraphReqNext = await GetDomainsMsGraphReqNextLink.Content.ReadAsStringAsync();
                var GetDomainsMsGraphRespNext = JsonConvert.DeserializeObject<DomainsResp>(GetDomainsMsGraphReqNext);
                GetDomainsMsGraphDataResp.value.AddRange(GetDomainsMsGraphRespNext.value);
                GetDomainsMsGraphDataResp.odatanextLink = GetDomainsMsGraphRespNext.odatanextLink;

                if (string.IsNullOrEmpty(GetDomainsMsGraphDataResp.odatanextLink))
                    fetchedAll = true;
            }
            return GetDomainsMsGraphDataResp;
        }
        public async Task<GroupsResp> GetGroupsMsGraph()
        {

            var getGroupsMsGraphReq = await _graphClient.PollyGetAsync("https://graph.microsoft.com/v1.0/groups?$top=800&$select=mailNickname,id,displayName");
            var getGroupsMsGraphResp = await getGroupsMsGraphReq.Content.ReadAsStringAsync();
            var getGroupsMsGraphDataResp = JsonConvert.DeserializeObject<GroupsResp>(getGroupsMsGraphResp);

            bool fetchedAll = false;
            if (string.IsNullOrEmpty(getGroupsMsGraphDataResp.odatanextLink))
                fetchedAll = true;


            while (!fetchedAll)
            {

                var getGroupsMsGraphReqNext = await _graphClient.PollyGetAsync(getGroupsMsGraphDataResp.odatanextLink);
                var getGroupsMsGraphRespNext = await getGroupsMsGraphReqNext.Content.ReadAsStringAsync();
                var getGroupsMsGraphRespDataNext = JsonConvert.DeserializeObject<GroupsResp>(getGroupsMsGraphRespNext);
                getGroupsMsGraphDataResp.Value.AddRange(getGroupsMsGraphRespDataNext.Value);
                getGroupsMsGraphDataResp.odatanextLink = getGroupsMsGraphRespDataNext.odatanextLink;

                if (string.IsNullOrEmpty(getGroupsMsGraphDataResp.odatanextLink))
                    fetchedAll = true;

            }
            return getGroupsMsGraphDataResp;
        }
        public async Task<UsersResp> GetUsersMsGraph()
        {

            var getUsersMsGraphReq = await _graphClient.PollyGetAsync("https://graph.microsoft.com/v1.0/users");
            var getUsersMsGraphResp = await getUsersMsGraphReq.Content.ReadAsStringAsync();
            var getUsersMsGraphDataResp = JsonConvert.DeserializeObject<UsersResp>(getUsersMsGraphResp);

            bool fetchedAll = false;
            if (string.IsNullOrEmpty(getUsersMsGraphDataResp.odatanextLink))
                fetchedAll = true;

            while (!fetchedAll)
            {

                var getUsersMsGraphNextReq = await _graphClient.PollyGetAsync(getUsersMsGraphDataResp.odatanextLink);
                var getUsersMsGraphRespNext = await getUsersMsGraphNextReq.Content.ReadAsStringAsync();
                var getUsersMsGraphDataRespNext = JsonConvert.DeserializeObject<UsersResp>(getUsersMsGraphRespNext);
                getUsersMsGraphDataResp.value.AddRange(getUsersMsGraphDataRespNext.value);
                getUsersMsGraphDataResp.odatanextLink = getUsersMsGraphDataRespNext.odatanextLink;

                if (string.IsNullOrEmpty(getUsersMsGraphDataResp.odatanextLink))
                    fetchedAll = true;
            }
            return getUsersMsGraphDataResp;
        }
        public async Task<DomainRespAAD> GetDomainsAdGraph(string tenantId)
        {
            bool fetchedAll = false;

            var GetDomainsAdGraphReq = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/domains?&api-version=1.6");
            var GetDomainsAdGraphResp = await GetDomainsAdGraphReq.Content.ReadAsStringAsync();
            var GetDomainsAdGraphDataResp = JsonConvert.DeserializeObject<DomainRespAAD>(GetDomainsAdGraphResp);

         
            if (string.IsNullOrEmpty(GetDomainsAdGraphDataResp.odatanextLink))
                fetchedAll = true;

            while (!fetchedAll)
            {
                var GetDomainsAdGrahSkipToken = GetDomainsAdGraphDataResp.odatanextLink.Split("skiptoken=")[1];
                var GetDomainsAdGraphReqNext = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/domains?&api-version=1.6&$skiptoken=" + GetDomainsAdGrahSkipToken);
                var GetDomainsAdGraphRespNext = await GetDomainsAdGraphReqNext.Content.ReadAsStringAsync();
                var GetDomainsAdGraphRespDataNext = JsonConvert.DeserializeObject<DomainRespAAD>(GetDomainsAdGraphRespNext);

                GetDomainsAdGraphDataResp.value.AddRange(GetDomainsAdGraphRespDataNext.value);
                GetDomainsAdGraphDataResp.odatanextLink = GetDomainsAdGraphRespDataNext.odatanextLink;

                if (string.IsNullOrEmpty(GetDomainsAdGraphDataResp.odatanextLink))
                    fetchedAll = true;
            }
            return GetDomainsAdGraphDataResp;
        }
        public async Task<GroupsRespAAD> GetGroupsAdGraph(string tenantId)
        {

            var getGroupsAdGraphReq = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/groups?&api-version=1.6&$top=800&$select=objectType,description,objectId,displayName,mailNickname,onPremisesSamAccountName");
            var getGroupsAdGraphResp = await getGroupsAdGraphReq.Content.ReadAsStringAsync();
            var getGroupsAdGraphDataResp = JsonConvert.DeserializeObject<GroupsRespAAD>(getGroupsAdGraphResp);

            bool fetchedAll = false;
            if (string.IsNullOrEmpty(getGroupsAdGraphDataResp.odatanextLink))
                fetchedAll = true;


            while (!fetchedAll)
            {
                var getGroupsAdGraphNextLink = getGroupsAdGraphDataResp.odatanextLink.Split("skiptoken=")[1];
                var getGroupsAdGraphReqNext = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/groups?&api-version=1.6&$top=800&$select=objectType,description,objectId,displayName,mailNickname,onPremisesSamAccountName&$skiptoken=" + getGroupsAdGraphNextLink);
                var getGroupsAdGraphRespNext = await getGroupsAdGraphReqNext.Content.ReadAsStringAsync();
                var getGroupsAdGraphRespDataNext = JsonConvert.DeserializeObject<GroupsRespAAD>(getGroupsAdGraphRespNext);
                getGroupsAdGraphDataResp.value.AddRange(getGroupsAdGraphRespDataNext.value);
                getGroupsAdGraphDataResp.odatanextLink = getGroupsAdGraphRespDataNext.odatanextLink;

                if (string.IsNullOrEmpty(getGroupsAdGraphDataResp.odatanextLink))
                    fetchedAll = true;

            }
            return getGroupsAdGraphDataResp;
        }
        public async Task<GetMembersAAD> GetGroupMembersAdGraph(string groupId, string tenantId)
        {
            bool fetchedAll = false;

            var getGroupMemReq = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/groups/{groupId}/members?&api-version=1.6&$select=userPrincipalName,mail,mailNickname,objectId,displayName,facsimileTelephoneNumber,department,mobile");
            var getGroupMemResp = await getGroupMemReq.Content.ReadAsStringAsync();
            var getGroupMemDataResp = JsonConvert.DeserializeObject<GetMembersAAD>(getGroupMemResp);

            if (string.IsNullOrEmpty(getGroupMemDataResp.odatanextLink))
                fetchedAll = true;

            while (!fetchedAll)
            {
                var getGroupMemSkipToken = getGroupMemDataResp.odatanextLink.Split("skiptoken=")[1];
                var getGroupMemReqNext = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/groups/{groupId}/members?&api-version=1.6&$select=userPrincipalName,mail,mailNickname,objectId,displayName,facsimileTelephoneNumber,department,mobile&$skiptoken=" + getGroupMemSkipToken);
                var getGroupMemRespNext = await getGroupMemReqNext.Content.ReadAsStringAsync();
                var getGroupMemDataRespNext = JsonConvert.DeserializeObject<GetMembersAAD>(getGroupMemRespNext);

                getGroupMemDataResp.value.AddRange(getGroupMemDataRespNext.value);
                getGroupMemDataResp.odatanextLink = getGroupMemDataRespNext.odatanextLink;

                if (string.IsNullOrEmpty(getGroupMemDataResp.odatanextLink))
                    fetchedAll = true;
            }

            return getGroupMemDataResp;
        }
        public async Task<UserRespAAD> GetUsersAdGraph(string tenantId)
        {

            var getUsersAdGraph = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/users?&api-version=1.6");
            var getUsersAdGraphResp = await getUsersAdGraph.Content.ReadAsStringAsync();
            var getUsersAdGraphDataResp = JsonConvert.DeserializeObject<UserRespAAD>(getUsersAdGraphResp);

            bool fetchedAll = false;
            if (string.IsNullOrEmpty(getUsersAdGraphDataResp.odatanextLink))
                fetchedAll = true;

            while (!fetchedAll)
            {

                var getUsersAdGraphSkipToken = getUsersAdGraphDataResp.odatanextLink.Split("skiptoken=")[1];
                var getUsersAdGraphReqNext = await _graphClient.PollyGetAsync($"https://graph.windows.net/{tenantId}/users?&api-version=1.6&$skiptoken=" + getUsersAdGraphSkipToken);
                var getUsersAdGraphRespNext = await getUsersAdGraphReqNext.Content.ReadAsStringAsync();
                var getUsersAdGraphDataRespNext = JsonConvert.DeserializeObject<UserRespAAD>(getUsersAdGraphRespNext);
                getUsersAdGraphDataResp.value.AddRange(getUsersAdGraphDataRespNext.value);
                getUsersAdGraphDataResp.odatanextLink = getUsersAdGraphDataRespNext.odatanextLink;

                if (string.IsNullOrEmpty(getUsersAdGraphDataResp.odatanextLink))
                    fetchedAll = true;
            }
            return getUsersAdGraphDataResp;
        }

      

    }
}
