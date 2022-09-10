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
        public bool PushoverLocked { get; set; }
        public bool Pushover { get; set; }
        public int OwaLimit { get; set; }
        public Pushover PushClient { get; set; }

        public GlobalArgumentsHandler(string[] args, bool exfilModule = false)
        {
            OutPutPath = args.GetValue("--outpath");

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

            if (string.IsNullOrEmpty(OutPutPath))
            {
                Console.WriteLine("[+] Your are missing the mandatory --outpath argument, please define it!");
                Environment.Exit(0);
            }
            else
            {
                if (Path.HasExtension(OutPutPath))
                {
                    Console.WriteLine("[+] The --outpath argument is a FOLDER path, not file. Correct it and try again!");
                    Environment.Exit(0);
                }

            }

            try
            {
                if (!string.IsNullOrEmpty(TeamFiltrationConfig?.PushoverUserKey) && !string.IsNullOrEmpty(TeamFiltrationConfig?.PushoverAppKey))
                    PushClient = new Pushover(TeamFiltrationConfig.PushoverAppKey);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"[!] Failed to create Pushover client, bad API keys? -> {ex}");
            }
         
        }
        public void PushAlert(string title, string message)
        {
            if (PushClient != null)
            {
                if (Pushover || PushoverLocked)
                {
                    try
                    {
                        PushResponse response = PushClient.Push(
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

        public string GetFireProxURL(string region = "US")
        {
            

            if (UsCloud)
                return TeamFiltrationConfig.MsolFireProxEndpointsUs.Where(x => x.ToLower().Contains(("." + region + "-").ToLower())).FirstOrDefault();

            return TeamFiltrationConfig.MsolFireProxEndpoints.Where(x => x.ToLower().Contains(("." + region + "-").ToLower())).FirstOrDefault();
        }
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
