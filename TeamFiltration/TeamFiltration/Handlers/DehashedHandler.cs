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

            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(globalArgsHandler.TeamFiltrationConfig.DehashedEmail + ":" + globalArgsHandler.TeamFiltrationConfig.DehashedApiKey));

            _dehashedClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encoded);
            _dehashedClient.DefaultRequestHeaders.Add("Accept", "application/json");

        }
        public async Task<QueryResponse> QueryDehashed(string domain, int page, int size)
        {

            var dehashedResponse = await _dehashedClient.GetAsync("https://api.dehashed.com/search?query=domain:" + domain + "&size=" + size + "&page=" + page);
            var rawData = await dehashedResponse.Content.ReadAsStringAsync();
            if (dehashedResponse.IsSuccessStatusCode)
            {


                return JsonConvert.DeserializeObject<Models.Dehashed.QueryResponse>(rawData);
            }
            else
            {
                Console.WriteLine($"[+] Failed to get data from Dehashed, response: {rawData}");
                return (new QueryResponse() { balance = 0, entries = new List<Entry>() { }, success = false, took = "", total = 0, });
            };



        }

        public async Task<QueryResponse> FetchDomainEntries(string domain)
        {
            int page = 1;
            int size = 10000;

            //Get the first round of data
            var fetchedData = await this.QueryDehashed(domain, page, size);

            if (fetchedData.success == false)
                Environment.Exit(0);

            //set the vars
            var rawResultCount = fetchedData.entries.Count;
            page++;

            //If the results back matches the sized we asked for, we need to fetch more until it does no longer match!
            while (rawResultCount == size)
            {
                //Ask for more
                var bufferFetchedData = await this.QueryDehashed(domain, page, size);

                if (bufferFetchedData.success == false)
                    return fetchedData;

                //Add to the list
                fetchedData.entries.AddRange(bufferFetchedData.entries);

                //Update the vars 
                rawResultCount = fetchedData.entries.Count;
                page++;
            }

            return fetchedData;

        }

    }
}
