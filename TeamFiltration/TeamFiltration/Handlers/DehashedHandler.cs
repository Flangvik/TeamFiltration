using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Models.Dehashed;

namespace TeamFiltration.Handlers
{
    public class DehashedHandler
    {

        public HttpClient _dehashedClient { get; set; }

        public GlobalArgumentsHandler _teamFiltrationConfig { get; set; }


        public DehashedHandler(GlobalArgumentsHandler globalArgsHandler)
        {
            _teamFiltrationConfig = globalArgsHandler;


            // This is for debug , eg burp
            var proxy = new WebProxy
            {
                Address = new Uri(_teamFiltrationConfig.TeamFiltrationConfig.proxyEndpoint),
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
                UseProxy = globalArgsHandler.DebugMode
            };


            _dehashedClient = new HttpClient(httpClientHandler);

            //string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(globalArgsHandler.TeamFiltrationConfig.DehashedEmail + ":" + globalArgsHandler.TeamFiltrationConfig.DehashedApiKey));

            _dehashedClient.DefaultRequestHeaders.Add("Dehashed-Api-Key", globalArgsHandler.TeamFiltrationConfig.DehashedApiKey);
            _dehashedClient.DefaultRequestHeaders.Add("Accept", "application/json");

        }
        public async Task<DehashedQueryResponse> QueryDehashed(string domain, int page, int size)
        {

            var jsonData = new DehashedQueryRequest()
            {
                query = "domain:" + domain,
                page = page,
                size = size,
                wildcard = false,
                regex = false,
                de_dupe = true

            };

            var postData = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");
            var dehashedResponse = await _dehashedClient.PostAsync("https://api.dehashed.com/v2/search", postData);
            var rawData = await dehashedResponse.Content.ReadAsStringAsync();
            if (dehashedResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<Models.Dehashed.DehashedQueryResponse>(rawData);
            }
            else
            {
                Console.WriteLine($"[+] Failed to get data from Dehashed, response: {rawData}");
                return (new DehashedQueryResponse() { balance = 0,entries = new List<Entry>() {  },took = "", total = 0});
            };



        }

        public async Task<DehashedQueryResponse> FetchDomainEntries(string domain)
        {
            int page = 1;
            int size = 10000;

            //Get the first round of data
            var fetchedData = await this.QueryDehashed(domain, page, size);

            if (fetchedData.total == 0)
                return fetchedData;

            //set the vars
            var rawResultCount = fetchedData.entries.Count;
           
            //If the number of entries back is less the the total entires found by dehashed, move a page up
            while (rawResultCount >= size )
            {   //Move a page up
                page++;

                //Ask for more
                var bufferFetchedData = await this.QueryDehashed(domain, page, size);

                //Add to the list
                fetchedData.entries.AddRange(bufferFetchedData.entries);

                //Update the vars with the new count
                rawResultCount = fetchedData.entries.Count;
            }

            return fetchedData;

        }

    }
}
