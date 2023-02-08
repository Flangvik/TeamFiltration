using Nager.PublicSuffix;
using Newtonsoft.Json;
using PushoverClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TeamFiltration.Helpers;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Handlers
{

    public class GlobalArgumentsHandler
    {
        public string OutPutPath { get; set; }

        public Config TeamFiltrationConfig { get; set; }
        public bool DebugMode { get; set; }
        public bool UsCloud { get; set; }
        public bool AADSSO { get; set; }
        public bool PushoverLocked { get; set; }
        public bool Pushover { get; set; }
        public bool AWSFireProx { get; set; } = false;
        public int OwaLimit { get; set; }
        private Pushover _pushClient { get; set; }
        public AWSHandler _awsHandler { get; set; }
        private DatabaseHandler _databaseHandler { get; set; }
        private DomainParser _domainParser { get; set; }
        public string[] AWSRegions { get; set; } = { "us-east-1", "us-west-1", "us-west-2", "ca-central-1", "eu-central-1", "eu-west-1", "eu-west-2", "eu-west-3", "eu-north-1" };



        public GlobalArgumentsHandler(string[] args, DatabaseHandler databaseHandler, bool exfilModule = false)
        {

            _domainParser = new DomainParser(new WebTldRuleProvider());

            OutPutPath = args.GetValue("--outpath");
            this.AADSSO = args.Contains("--aad-sso");

            _databaseHandler = databaseHandler;

            var teamFiltrationConfigPath = args.GetValue("--config");
            if (string.IsNullOrEmpty(teamFiltrationConfigPath) && File.Exists("TeamFiltrationConfig.json"))
            {
                teamFiltrationConfigPath = "TeamFiltrationConfig.json";
            }
            else if (!File.Exists(teamFiltrationConfigPath))
            {
                if (!exfilModule)
                {
                    Console.WriteLine("[+] Could not find teamfiltration config, provide a config path using  with --config");
                    return;
                }

            }
            else
            {
                var configText = File.ReadAllText(teamFiltrationConfigPath);
                TeamFiltrationConfig = JsonConvert.DeserializeObject<Config>(configText);
            }




            string OwaLimitString = args.GetValue("--owa-limit");


            Int32.TryParse(OwaLimitString, out var LocalOwaLimit);
            if (LocalOwaLimit > 0)
                OwaLimit = LocalOwaLimit;
            else
                OwaLimit = 2000;



            PushoverLocked = args.Contains("--push-locked");
            Pushover = args.Contains("--push");
            UsCloud = args.Contains("--us-cloud");
            DebugMode = args.Contains("--debug");
            

            // Set default user agent if missing
            if (string.IsNullOrEmpty(TeamFiltrationConfig?.UserAgent))
                TeamFiltrationConfig.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Teams/1.3.00.30866 Chrome/80.0.3987.165 Electron/8.5.1 Safari/537.36";

            try
            {
                if (!string.IsNullOrEmpty(TeamFiltrationConfig?.PushoverUserKey) && !string.IsNullOrEmpty(TeamFiltrationConfig?.PushoverAppKey))
                    _pushClient = new Pushover(TeamFiltrationConfig.PushoverAppKey);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"[!] Failed to create Pushover client, bad API keys? -> {ex}");
            }


            //Do AWS FireProx generation checks
            if (!string.IsNullOrEmpty(TeamFiltrationConfig?.AWSSecretKey) && !string.IsNullOrEmpty(TeamFiltrationConfig?.AWSAccessKey))
            {
                Console.WriteLine("[+] AWS SecretKey and AccessKey found, FireProx endpoint will be automagically created for each spray-rotation");
                //If have a strong feeling doing it this way might be a crappy idea, but let's try 
                AWSFireProx = true;
                if (string.IsNullOrEmpty(TeamFiltrationConfig?.AWSSessionKey))
                {
                    _awsHandler = new AWSHandler(this.TeamFiltrationConfig.AWSAccessKey, this.TeamFiltrationConfig.AWSSecretKey, databaseHandler);
                }
                else
                {
                    Console.WriteLine("[+] AWS Session Key found, this will be used to login to AWS");
                    _awsHandler = new AWSHandler(this.TeamFiltrationConfig.AWSAccessKey, this.TeamFiltrationConfig.AWSSecretKey, this.TeamFiltrationConfig.AWSSessionKey, databaseHandler);
                }

            }

        }
        public void PushAlert(string title, string message)
        {
            if (_pushClient != null)
            {
                if (Pushover || PushoverLocked)
                {
                    try
                    {
                        PushResponse response = _pushClient.Push(
                            title,
                            message,
                            TeamFiltrationConfig.PushoverUserKey
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[!] Pushover message failed, error: {ex}");
                    }
                }
            }

        }
        public string GetBaseUrl(string region = "US")
        {
            return "https://login.microsoftonline.com/common/oauth2/token";
        }

        public (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl) GetFireProxURLObject(string url, int regionCounter)
        {
            var currentRegion = this.AWSRegions[regionCounter];
            var domainBase = _domainParser.Parse(url);
            (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint) awsEndpoint = _awsHandler.CreateFireProxEndPoint(url, domainBase.Domain, currentRegion).GetAwaiter().GetResult();
            return (awsEndpoint.Item1, awsEndpoint.Item2, $"https://{awsEndpoint.Item1.RestApiId}.execute-api.{currentRegion}.amazonaws.com/fireprox/");


        }
        /*
        public string GetFireProxURL(string url, int regionCounter)
        {

            if (AWSFireProx)
            {
                var currentRegion = this.AWSRegions[regionCounter];
                (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint) awsEndpoint = _awsHandler.CreateFireProxEndPoint(url, "microsoftonlineUSTemp", currentRegion).GetAwaiter().GetResult();
                return $"https://{awsEndpoint.Item1.RestApiId}.execute-api.{currentRegion}.amazonaws.com/fireprox/";
            }
            else
            {
                Console.WriteLine("[+] Missing AWS SecretKey and AccessKey, unable to create needed FireProx endpoint, exiting..");
                Environment.Exit(0);
                return "";
            }

    


        }
        */
        private string EnsurePathChar(string outPutPath)
        {
            foreach (var invalidChar in Path.GetInvalidPathChars())
            {
                outPutPath = outPutPath.Replace(invalidChar, Path.DirectorySeparatorChar);
            }
            return outPutPath;
        }
    }
}
