using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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

        public static bool IsTokenValid(string access_token, DateTime retrived)
        {
            try
            {
                var jsonToken = JsonConvert.DeserializeObject<BearerTokenResp>(access_token);

                if (jsonToken?.access_token == null)
                    return false;


                var dateTime = Helpers.Generic.UnixTimeStampToDateTime(Convert.ToInt32(jsonToken.expires_on));

                if (dateTime >= DateTime.Now.AddMinutes(2))
                    return true;

                var refreshTokenInt = Convert.ToInt32(jsonToken.refresh_token_expires_in);
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



        public static string GetTenantId(string access_token)
        {

            JwtSecurityTokenHandler jwsSecHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecToken = jwsSecHandler.ReadJwtToken(access_token);

            jwtSecToken.Payload.TryGetValue("tid", out var tidObject);
            return tidObject?.ToString();


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

                //return inputPath.Replace(Path.DirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString());
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
        public static List<(string uri, string clientId)> GetResc()
        {
            return new List<(string uri, string clientId)> {
            //From Teams
            ("https://graph.windows.net","1fec8e78-bce4-4aaf-ab1b-5451cc387264"),
            ("https://api.spaces.skype.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"),
            ("https://outlook.office365.com/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"),
            ("https://management.core.windows.net/", "1fec8e78-bce4-4aaf-ab1b-5451cc387264"),
            ("https://graph.microsoft.com/","1fec8e78-bce4-4aaf-ab1b-5451cc387264"),

            //From outlook
            ("https://api.office.net","d3590ed6-52b3-4102-aeff-aad2292ab01c"),
            ("https://outlook.office365.com/","d3590ed6-52b3-4102-aeff-aad2292ab01c"),
            ("https://dataservice.o365filtering.com/","d3590ed6-52b3-4102-aeff-aad2292ab01c"),
            ("https://outlook.office.com","d3590ed6-52b3-4102-aeff-aad2292ab01c"),

            //OneDrive
         //   ("https://clients.config.office.net/","d3590ed6-52b3-4102-aeff-aad2292ab01c"),
            ("https://onedrive.live.com/","d3590ed6-52b3-4102-aeff-aad2292ab01c"),
          //  ("https://wns.windows.com","d3590ed6-52b3-4102-aeff-aad2292ab01c"),

            //oneNote
            ("https://api.office.net","ab9b8c07-8f02-4f72-87fa-80105867a763"),
            ("https://onedrive.live.com/","ab9b8c07-8f02-4f72-87fa-80105867a763"),
         //   ("https://clients.config.office.net","ab9b8c07-8f02-4f72-87fa-80105867a763"),
         //   ("https://wns.windows.com","ab9b8c07-8f02-4f72-87fa-80105867a763")
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

        public static bool TimeToSleep(string startTime, string stopTime)
        {


            var StarTime = DateTime.ParseExact(startTime,
                             "HH:mm",
                             new CultureInfo("en-US"));

            var StopTime = DateTime.ParseExact(stopTime,
                         "HH:mm",
                         new CultureInfo("en-US"));

            if (IsBewteenTwoDates(DateTime.Now, StarTime, StopTime))
                return false;

            return true;

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

            var months = ci.DateTimeFormat.MonthNames;

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
            //Passwords need to be English
            CultureInfo ci = new CultureInfo("en-US");

            var months = ci.DateTimeFormat.MonthNames;

            var passwords = new List<string>() { };

            passwords.Add("P@ssword123456");
            passwords.Add("password@12345");
            passwords.Add("Password@12345");

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

            errorCodes.Add("AADSTS50079", ("VALID BUT MFA (79)", true, false, true));
            errorCodes.Add("AADSTS50076", ("VALID BUT MFA (76)", true, false, true));

            errorCodes.Add("AADSTS50053", ("LOCKED", false, false, false));
            errorCodes.Add("AADSTS50057", ("DISABLED", false, true, false));

            errorCodes.Add("AADSTS50055", ("EXPIRED", false, true, false));
            errorCodes.Add("AADSTS50128", ("INVALID TENANT", false, true, false));
            errorCodes.Add("AADSTS50059", ("INVALID TENANT", false, true, false));


            errorCodes.Add("AADSTS53003", ("BLOCKED BY ACCESS POLICY", true, false, true));
            errorCodes.Add("AADSTS50158", ("BLOCKED BY ACCESS POLICY", true, false, true));

            return errorCodes;
        }



        public static (string Uri, string clientId) RandomO365Res()
        {
            var o365ResList = GetResc();

            return o365ResList[(new Random()).Next(0, o365ResList.Count())];
        }

        public static (string Uri, string clientId) RandomO365Res(string Uri, string clientId)
        {
            var o365ResList = GetResc().Where(x => !x.uri.Equals(Uri) && !x.clientId.Equals(clientId)).ToList();

            return o365ResList[(new Random()).Next(0, o365ResList.Count())];
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }


    }
}
