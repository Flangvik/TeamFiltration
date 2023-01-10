using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Models.AWS;
using TeamFiltration.Models.TeamFiltration;
using TimeZoneConverter;
using TeamFiltration.Helpers;

namespace TeamFiltration.Handlers
{
    public class DatabaseHandler
    {
        public LiteDatabase _globalDatabase { get; set; }
        //  public GlobalArgumentsHandler _globalPropertiesHandler { get; set; }
        public string DatabaseFullPath { get; set; }
        public void OnProcessExit(object sender, EventArgs e)
        {
            _globalDatabase.Checkpoint();
        }

        // //QueryEnumeratedConditionalAccess

        public List<EnumeratedConditionalAccess> QueryEnumeratedConditionalAccess(string inputData)
        {
            var collectionLink = _globalDatabase.GetCollection<EnumeratedConditionalAccess>("enumeratedconditionalaccess");
            var condAcc = collectionLink.Find(x => x.Username.Equals(inputData)).ToList();
            return condAcc;

        }

        internal void WriteEnumeratedConditionalAccess(List<EnumeratedConditionalAccess> enumeratedConditionalAccesses)
        {
            var collectionLink = _globalDatabase.GetCollection<EnumeratedConditionalAccess>("enumeratedconditionalaccess");
            collectionLink.EnsureIndex(x => x.Id, true);
            foreach (var item in enumeratedConditionalAccesses)
            {
                collectionLink.Upsert(item);
            }

        }


        public void WriteInvalidAcc(ValidAccount inputData)
        {
            var collectionLink = _globalDatabase.GetCollection<ValidAccount>("invalidaccounts");
            collectionLink.Upsert(inputData);


        }
        internal List<ValidAccount> QueryInvalidAccount()
        {
            var orders = _globalDatabase.GetCollection<ValidAccount>("invalidaccounts");
            return orders.FindAll().ToList();
        }
        public void WriteValidAcc(ValidAccount inputData)
        {
            var collectionLink = _globalDatabase.GetCollection<ValidAccount>("validaccounts");
            collectionLink.Upsert(inputData);
            // _globalDatabase.Checkpoint();

        }
        //UpdateAccount
        public void UpdateAccount(SprayAttempt inputData)
        {
            var collectionLink = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            collectionLink.Update(inputData);
            _globalDatabase.Checkpoint();

        }
        public string TokenAvailable(SprayAttempt accObject, string resource = "")
        {
            //If we are looking for a spesific resource
            if (!string.IsNullOrEmpty(resource))
            {
                var tokenFromResource = this.QueryTokens(accObject.Username, resource);
                if (Helpers.Generic.IsTokenValid(tokenFromResource?.ResponseData, accObject.DateTime))
                    return tokenFromResource.ResponseData;

            }

            PulledTokens latestPulledToken = this.QueryTokens(accObject);

            var latestValidToken = "";


            if (Helpers.Generic.IsTokenValid(accObject.ResponseData, accObject.DateTime))
                latestValidToken = accObject.ResponseData;

            if (latestPulledToken != null)
            {
                if (Helpers.Generic.IsTokenValid(latestPulledToken?.ResponseData, latestPulledToken.DateTime))
                    latestValidToken = latestPulledToken.ResponseData;
            }

            return latestValidToken;


        }

        public void DeleteFireProxEndpoint(string fireProxUrl)
        {
            var orders = _globalDatabase.GetCollection<FireProxEndpoint>("fireproxendpoints");
            orders.DeleteMany(x => x.URL.Equals(fireProxUrl));
        }
        public void WriteFireProxEndpoint(FireProxEndpoint endpointData)
        {
            endpointData.DateTime = DateTime.Now;
            var collectionLink = _globalDatabase.GetCollection<FireProxEndpoint>("fireproxendpoints");
            collectionLink.EnsureIndex(x => x.Id, true);
            collectionLink.Upsert(endpointData);


        }


        internal List<FireProxEndpoint> QueryAllFireProxEndpoints()
        {

            var orders = _globalDatabase.GetCollection<FireProxEndpoint>("fireproxendpoints");
            return orders.FindAll().ToList();
        }



        public void WriteSprayAttempt(SprayAttempt inputData, GlobalArgumentsHandler _globalPropertiesHandler)
        {
            if (inputData.Valid && _globalPropertiesHandler.Pushover)
                _globalPropertiesHandler.PushAlert("VALID CREDENTIALS FOUND", $"Username: {inputData.Username}\n Password: {inputData.Password}\n Conditional access: {inputData.ConditionalAccess}\n Disqualified: {inputData.Disqualified}");

            if (!string.IsNullOrEmpty(inputData.ResponseCode))
                if (inputData.ResponseCode.Equals("AADSTS50053") && _globalPropertiesHandler.PushoverLocked)
                    _globalPropertiesHandler.PushAlert("ACCOUNT LOCKED", $"Username: {inputData.Username}\n Password: {inputData.Password}\n Conditional access: {inputData.ConditionalAccess}\n Disqualified: {inputData.Disqualified}");


            inputData.DateTime = DateTime.Now;
            var collectionLink = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            collectionLink.EnsureIndex(x => x.Id, true);
            collectionLink.Upsert(inputData);


        }

        public void WriteLog(Log inputLog, bool printLog = true, bool WriteLine = true)
        {
            //TimeZoneInfo

            //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            TimeZoneInfo easternZone = TZConvert.GetTimeZoneInfo("Eastern Standard Time");

            if (printLog)
                if (string.IsNullOrEmpty(inputLog.Prefix))
                    if (WriteLine)
                        Console.WriteLine($"[{inputLog.Module}] { TimeZoneInfo.ConvertTimeFromUtc(inputLog.Timestamp.ToUniversalTime(), easternZone)} EST {inputLog.Message}");
                    else
                        Console.Write($"\r[{inputLog.Module}] { TimeZoneInfo.ConvertTimeFromUtc(inputLog.Timestamp.ToUniversalTime(), easternZone)} EST {inputLog.Message}");
                else
                {
                    var msgPartOne = inputLog.Message.Split("=>")[0];
                    var msgPartTwo = inputLog.Message.Split("=>")[1];
                    var message = msgPartOne.PadRight(60) + "=>" + msgPartTwo;
                    if (WriteLine)
                        Console.WriteLine($"[{inputLog.Module}] {inputLog.Prefix} { TimeZoneInfo.ConvertTimeFromUtc(inputLog.Timestamp.ToUniversalTime(), easternZone)} EST {message}");
                    else
                        Console.Write($"\r[{inputLog.Module}] {inputLog.Prefix} { TimeZoneInfo.ConvertTimeFromUtc(inputLog.Timestamp.ToUniversalTime(), easternZone)} EST {message}");
                }


            // var collectionLink = _globalDatabase.GetCollection<Log>("logs");
            // collectionLink.EnsureIndex(x => x.Id, true);
            // collectionLink.Insert(inputLog);

        }
        public DatabaseHandler(string[] args)
        {

            var OutPutPath = args.GetValue("--outpath");

            if (string.IsNullOrEmpty(OutPutPath))
            {
                Console.WriteLine("[+] Your are missing the mandatory --outpath argument, please define it!");
                Environment.Exit(0);
            }
            else
            {
                if (Path.HasExtension(OutPutPath))
                {
                    Console.WriteLine("[+] The --outpath argument is a FOLDER path, not file. Correct it and try again!");
                    Environment.Exit(0);
                }

            }

            Directory.CreateDirectory(OutPutPath);
            var fullPath = Path.Combine(OutPutPath, @"TeamFiltration.db");
            DatabaseFullPath = fullPath;

            _globalDatabase = new LiteDatabase(fullPath);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }

        //QueryValidAccount
        internal List<ValidAccount> QueryValidAccount()
        {
            var orders = _globalDatabase.GetCollection<ValidAccount>("validaccounts");
            return orders.FindAll().ToList();
        }
        internal List<SprayAttempt> QueryValidLogins()
        {
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.Valid == true).ToList();
        }



        internal bool QueryComboHash(string comboHash)
        {
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Exists(x => x.ComboHash.Equals(comboHash));
        }


        internal List<SprayAttempt> QueryDisqualified()
        {
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.Disqualified == true).ToList();
        }

        internal List<SprayAttempt> QueryAllSprayAttempts()
        {

            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.FindAll().ToList();
        }



        internal List<SprayAttempt> QuerySprayAttempts(int minutes)
        {
            var timeSearch = DateTime.Now.AddMinutes(-minutes);
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.DateTime >= timeSearch && x.AADSSO == false).ToList();
        }


        internal List<SprayAttempt> QueryCombos(string password)
        {
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.Password.Equals(password)).ToList();
        }

        internal PulledTokens QueryTokens(string Username, string resource)
        {
            var pulledTokens = _globalDatabase.GetCollection<PulledTokens>("tokens");
            return pulledTokens.Find(x => x.Username.ToLower().Equals(Username.ToLower()) && x.ResourceUri.StartsWith(resource)).OrderByDescending(x => x.DateTime).FirstOrDefault();
        }

        internal PulledTokens QueryTokens(SprayAttempt account)
        {
            var pulledTokens = _globalDatabase.GetCollection<PulledTokens>("tokens");
            return pulledTokens.Find(x => x.Username.Equals(account.Username)).OrderByDescending(x => x.DateTime).FirstOrDefault();
        }

        internal void WriteToken(PulledTokens account)
        {
            account.DateTime = DateTime.Now;
            var collectionLink = _globalDatabase.GetCollection<PulledTokens>("tokens");
            collectionLink.EnsureIndex(x => x.Id, true);
            collectionLink.Upsert(account);
            _globalDatabase.Checkpoint();
        }


    }

}