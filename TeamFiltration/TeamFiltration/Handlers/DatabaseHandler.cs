using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamFiltration.Helpers;
using TeamFiltration.Models.AWS;
using TeamFiltration.Models.TeamFiltration;
using TimeZoneConverter;

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

        internal void WriteEnumeratedConditionalAccess(EnumeratedConditionalAccess enumeratedConditionalAccesses)
        {
            var collectionLink = _globalDatabase.GetCollection<EnumeratedConditionalAccess>("enumeratedconditionalaccess");
            collectionLink.EnsureIndex(x => x.Id, true);
            collectionLink.Upsert(enumeratedConditionalAccesses);


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

        public void DeleteValidAcc(string username)
        {
            var collectionLink = _globalDatabase.GetCollection<ValidAccount>("validaccounts");
            collectionLink.DeleteMany(x => x.Username.Equals(username));
            // _globalDatabase.Checkpoint();

        }

        public void WriteValidAcc(ValidAccount inputData)
        {
            var collectionLink = _globalDatabase.GetCollection<ValidAccount>("validaccounts");
            collectionLink.Upsert(inputData);
            // _globalDatabase.Checkpoint();

        }
        //UpdateAccount
        public bool UpdateAccount(SprayAttempt inputData)
        {
            var collectionLink = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            var result = collectionLink.Update(inputData);
            _globalDatabase.Checkpoint();
            return result;

        }

        
        public List<PulledTokens> TokensAvailable(SprayAttempt accObject)
        {


            /*
            //If we are looking for a spesific resource
            if (!string.IsNullOrEmpty(resource))
            {
                PulledTokens tokenFromResource = this.QueryTokens(accObject.Username, resource);
                if (Helpers.Generic.IsTokenValid(tokenFromResource?.ResponseData, accObject.DateTime))
                    returnList.Add(tokenFromResource.ResponseData);
            }
            */
            List<PulledTokens> returnList = new List<PulledTokens> { };

            List<PulledTokens> latestPulledToken = this.QueryTokens(accObject);

            if (latestPulledToken.Count() > 0)
            {
                foreach (var item in latestPulledToken)
                {
                    if (Helpers.Generic.IsTokenValid(item?.ResponseData, item.DateTime))
                        returnList.Add(item);

                }

            }

            /*
            if (Helpers.Generic.IsTokenValid(accObject.ResponseData, accObject.DateTime))
                returnList.Add(accObject.);
            */

            return returnList.ToList();


        }

        public void DeleteFireProxEndpoint(string RestApiId)
        {
            var orders = _globalDatabase.GetCollection<FireProxEndpoint>("fireproxendpoints");
            orders.DeleteMany(x => x.RestApiId.ToLower().Equals(RestApiId.ToLower()));
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
                        Console.WriteLine($"[{inputLog.Module}] {inputLog.Prefix.PadRight(12)} { TimeZoneInfo.ConvertTimeFromUtc(inputLog.Timestamp.ToUniversalTime(), easternZone)} EST {message}");
                    else
                        Console.Write($"\r[{inputLog.Module}] {inputLog.Prefix.PadRight(12)} { TimeZoneInfo.ConvertTimeFromUtc(inputLog.Timestamp.ToUniversalTime(), easternZone)} EST {message}");
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

        internal SprayAttempt QueryValidLogin(string username)
        {
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.Valid == true && x.Username.ToLower().Equals(username)).ToList().FirstOrDefault();
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
        internal List<string> QueryAllCombos()
        {

            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.FindAll().Select(x => x.Username.ToLower() + ":" + x.Password).ToList();
        }
        internal List<string> QueryAllComboHash()
        {

            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.FindAll().Select(x => x.ComboHash).ToList();
        }


        internal List<SprayAttempt> QuerySprayAttempts(int minutes)
        {
            var timeSearch = DateTime.Now.AddMinutes(-minutes);
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.DateTime >= timeSearch).ToList();
        }


        internal List<SprayAttempt> QueryCombos(string password)
        {
            var orders = _globalDatabase.GetCollection<SprayAttempt>("sprayattempts");
            return orders.Find(x => x.Password.Equals(password)).ToList();
        }

        internal List<PulledTokens> QueryToken(string Username, string resource)
        {
            var pulledTokens = _globalDatabase.GetCollection<PulledTokens>("tokens");
            var resourceHost = new Uri(resource).Host;
            return pulledTokens.Find(x => x.Username.ToLower().Equals(Username.ToLower()) && x.ResourceUri.Contains(resourceHost)).OrderByDescending(x => x.DateTime).ToList();
        }
        

        internal bool DeleteToken(PulledTokens token)
        {
            var pulledTokens = _globalDatabase.GetCollection<PulledTokens>("tokens");
            return pulledTokens.Delete(token.Id);
        }


        internal List<PulledTokens> QueryTokens(SprayAttempt account)
        {
            var pulledTokens = _globalDatabase.GetCollection<PulledTokens>("tokens");
            return pulledTokens.Find(x => x.Username.Equals(account.Username)).OrderByDescending(x => x.DateTime).ToList();
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