using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeamFiltration.Handlers;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Modules;

namespace TeamFiltration
{

    class Program
    {

        static void PrintUsage()
        {
            Console.WriteLine("Usage:\n");
            Console.WriteLine("   --outpath     Output path to store database and exfiltrated information (Needed for all modules)\n");
            Console.WriteLine("   --config      Local path to your TeamFiltration.json configuration file, if not provided will load from the current path\n");
            Console.WriteLine("   --exfil       Load the exfiltration module\n");
            Console.WriteLine("         --username            Override to target a given username that does not exist in the database");
            Console.WriteLine("         --password            Override to target a given password that does not exist in the database");
            Console.WriteLine("         --tokens              Override to target a (file with newline seperated JWT tokens|single JWT| , seperated JWT tokens) and perfom exfiltration");
            Console.WriteLine("         --roadtools           Override to target a the default JSON structure found in ROADTools auth file (.roadtools_auth)");
            Console.WriteLine("         --cookie-dump         Override to target a given account using it's refresh-cookie-collection\n");
            Console.WriteLine("         --all                 Exfiltrate information from ALL SSO resources (Graph, OWA, SharePoint, OneDrive, Teams)");
            Console.WriteLine("         --aad                 Exfiltrate information from Graph API (domain users and groups)");
            Console.WriteLine("         --teams               Exfiltrate information from Teams API (files, chatlogs, attachments, contactlist)");
            Console.WriteLine("         --teams-db            Exfiltrate cookies and authentication tokens from an exfiltrated Teams database");
            Console.WriteLine("         --onedrive            Exfiltrate information from OneDrive/SharePoint API (accessible SharePoint files and the users entire OneDrive directory)");
            Console.WriteLine("         --owa                 Exfiltrate information from the Outlook REST API (The last 2k emails, both sent and received) ");
            Console.WriteLine("               --owa-limit          Set the max amount of emails to exfiltrate, default is 2k.");
            Console.WriteLine("         --jwt-tokens          Dump all gathered JSON formated JTW-tokens for SSO resources (MsGraph,AdGraph, Outlook, SharePoint, OneDrive, Teams)\n");

            Console.WriteLine("   --spray       Load the spraying module\n");
            Console.WriteLine("         --aad-sso             Use SecureWorks's Azure Active Directory password brute-forcing technique when spraying");
            Console.WriteLine("         --us-cloud            When spraying companies attached to US Tenants (https://login.microsoftonline.us/)\n");

            Console.WriteLine("         --passwords           Path to a list of passwords, common weak-passwords will be generated if not supplied");
            Console.WriteLine("         --exclude             Path to a list of emails to exclude from spraying");
            Console.WriteLine("         --seasons-only        Password genersated for spraying will only be based on seasons");
            Console.WriteLine("         --months-only         Password generated for spraying will only be based on months");
            Console.WriteLine("         --common-only         Spray with the top 20 most common passwords");
            Console.WriteLine("         --shuffle-passwords   Shuffle the passwordlist before spraying");
            Console.WriteLine("         --shuffle-users       Shuffle the target userlist before spraying");
            Console.WriteLine("         --shuffle-regions     Shuffle FireProx regions when spraying\n");
            Console.WriteLine("         --auto-exfil          If valid login is found, auto start the exfil module\n");


            Console.WriteLine("         --sleep-min           Minimum minutes to sleep between each full rotation of spraying default=60");
            Console.WriteLine("         --sleep-max           Maximum minutes to sleep between each full rotation of spraying default=100");
            Console.WriteLine("         --jitter              Seconds between each individual authentication attempt. default=0");
            Console.WriteLine("         --time-window         Defines a time windows where spraying should accour, in the military time format <12:00-19:00>");
            Console.WriteLine("         --push                Get Pushover notifications when valid credentials are found (requires pushover keys in config)");
            Console.WriteLine("         --push-locked         Get Pushover notifications when an sprayed account gets locked (requires pushover keys in config)");
            Console.WriteLine("         --force               Force the spraying to proceed even if there is less the <sleep> time since the last attempt\n");

            Console.WriteLine("   --enum        Load the enumeration module\n");
            Console.WriteLine("         --domain              Domain to perfom enumeration against, names pulled from statistically-likely-usernames if not provided with --usernames");
            Console.WriteLine("         --usernames           Path to a list of usernames to enumerate (emails)");
            Console.WriteLine("         --tenant-info         Enumerates tenant and domain information (Based on Invoke-AADIntReconAsOutsider from AADInternal)");
            Console.WriteLine("         --dehashed            Use the dehashed submodule in order to enumerate emails from a basedomain");
            Console.WriteLine("         --validate-msol       Validate that the given o365 accounts exists using the public GetCredentialType method (Very RateLimited - Slow 20 e/s)");
            Console.WriteLine("         --validate-teams      Validate that the given o365 accounts exists using the Teams API method (Recommended - Super Fast 300 e/s)");
            Console.WriteLine("         --validate-login      Validate that the given o365 accounts by attemping to login (Noisy - triggers logins - Fast 100 e/s)\n");

            Console.WriteLine("   --backdoor        Loads the interactive backdoor module\n");
            Console.WriteLine("   --database        Loads the interactive database browser module\n");
            Console.WriteLine("   --debug           Proxy all outgoing HTTP requests through the proxy specified in the config\n");

            Console.WriteLine("   Examples:\n");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --spray --sleep-min 120 --sleep-max 200 --push --shuffle-users --shuffle-regions");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --spray --push-locked --months-only --exclude C:\Clients\2021\FooBar\Exclude_Emails.txt");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --spray --passwords C:\Clients\2021\FooBar\Generic\Passwords.txt --time-window 13:00-22:00");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --exfil --cookie-dump C:\\CookieData.txt --all");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --exfil --aad ");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --exfil --tokens C:\\OutputTokens.txt --onedrive --owa");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --exfil --teams --owa --owa-limit 5000");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --debug --exfil --onedrive");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --enum --validate-teams");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --enum --validate-msol --usernames C:\Clients\2021\FooBar\OSINT\Usernames.txt");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --backdoor");
            Console.WriteLine(@"        --outpath C:\Clients\2023\FooBar\TFOutput --config myCustomConfig.json --database");

            Environment.Exit(1);

        }

        static async Task Main(string[] args)
        {
            string asci = @"
  ╓╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╖
 ╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬┤                              ╟╬╬╜╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬╡                              │      ╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬╡                              ││      ╙╬╬╜╘ └╙╜╬╬╬╬╬╬
╬╬╬╬╡         ╓╥╥╬╬╬╬╬╬╥╥╖         ││       │        ╬╬╬╬╬
╬╬╬╬╡     ╓╬╫╬╜╜┘      ╙╜╜╬╫╬┐     ││       ││       └╬╬╬╬
╬╬╬╬┤    ╬╬╜╙╩╬╖╓          ╙╬╬╬    ││       ││        ╬╬╬╬
╬╬╬╬┤   ╬╜      ╙╬╫╖╖ ╓      ╙╬╖   ││      ├││        ╬╬╬╬
╬╬╬╬┤  ╬╬     ╓╖   ╙╬╬╬╬╬╬╦    ╬╬  │┌    ╓╬┤││       ╓╬╬╬╬
╬╬╬╬┤ ╓╬┤     ╬╬╬   ╬╬╬╬╬╬╬╬╜╜╜╬╬╖ ╟╬╬╬╬╬╬╬╬╬╕      ┌╬╬╬╬╬
╬╬╬╬┤ ╬╬┤     ╙╩┘   ╙╬╬╬╬╬╩    ╟╬╬ ╙╜╜╜╜╜╜╜╜╜╬╬╖╖╖╦╬╬╬╬╬╬╬
╬╬╬╬┤ ╬╬┤                      ╟╬╬ ││         ╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬┤  ╬╬   ╦╖            ╗╖   ╬╬  ││         │       ╬╬╬╬
╬╬╬╬┤  └╬┐   ╙╬╖╖      ╓╬╬╜   ╓╬┘  ││         │       ╬╬╬╬
╬╬╬╬┤   └╬╖    ╙╩╨╬╬╬╩╨╜╜   ╒╬╬    ││         │       ╬╬╬╬
╬╬╬╬┤    ╙╬╬╬╖           ┌╖╫╬╜┘    ││         │       ╬╬╬╬
╬╬╬╬┤       ╙╩╬╬╬╥╥╥╥╥╥╫╬╬╜╜       ││         │       ╬╬╬╬
╬╬╬╬┤            ╙╙╜╜╜╛            ││         │       ╬╬╬╬
╬╬╬╬┤                              ││         │    ╓╖╬╬╬╬╬
╬╬╬╬┤                              ││         ╬╦╦╬╬╬╬╬╬╬╬╬
╬╬╬╬┤                              ││     ╓╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬┤                              ╬╬╬╖╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
 └╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╜
   ╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜╜
";

            Console.WriteLine(asci);
            Console.WriteLine("[♥] TeamFiltration V3.5.4 PUBLIC, created by @Flangvik at @TrustedSec");
            Console.WriteLine($"[+] Args parsed {string.Join(' ', args)}");



            if (args.Length == 0)
                PrintUsage();
            else
            {

                if (args.Contains("--help"))
                    PrintUsage();

                else if (args.Contains("--spray"))
                    await Spray.SprayAsync(args);

                else if (args.Contains("--exfil"))
                    await Exfiltrate.ExfiltrateAsync(args);

                else if (args.Contains("--enum"))
                    await Enumerate.EnumerateAsync(args);

                else if (args.Contains("--backdoor"))
                    await Backdoor.BackdoorAsync(args);

                else if (args.Contains("--database"))
                    Database.DatabaseStart(args);
                
            }

        }


    }
}
