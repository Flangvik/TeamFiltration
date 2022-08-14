using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Helpers
{
    public static class PollyHelper
    {
        public static bool DebugMsg = false;
        public static async Task<HttpResponseMessage> PollyPostAsync(this HttpClient httpClient, string uri, HttpContent httpContent, int retyPolicy = 3)
        {
            var response = await Policy
           .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
           .WaitAndRetryAsync(retyPolicy, i => TimeSpan.FromSeconds(3), (result, timeSpan, retryCount, context) => { if (DebugMsg) Console.WriteLine($"[+] GET  {new Uri(uri).Host}  FAILED, retry attemp number {retryCount}"); })
           .ExecuteAsync(() => httpClient.PostAsync(uri, httpContent));

            return response;

        }

        public static async Task<HttpResponseMessage> PollyGetAsync(this HttpClient httpClient, string uri, int retyPolicy = 3)
        {
            var response = await Policy
           .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
           .WaitAndRetryAsync(retyPolicy, i => TimeSpan.FromSeconds(3), (result, timeSpan, retryCount, context) => { if (DebugMsg) Console.WriteLine($"[+] GET {new Uri(uri).Host} FAILED, retry attemp number {retryCount}"); })
           .ExecuteAsync(() => httpClient.GetAsync(uri));

            return response;

        }
    }
}
