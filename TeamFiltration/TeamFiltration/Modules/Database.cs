using ConsoleTables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TeamFiltration.Handlers;
using TeamFiltration.Models.TeamFiltration;
using TeamFiltration.Helpers;
using TeamFiltration.Models.AWS;
using System.Threading;

namespace TeamFiltration.Modules
{
    public class Database
    {
        public static string ToCsv<T>(string separator, List<T> objectlist)
        {
            Type t = objectlist.FirstOrDefault().GetType();
            var fields = t.GetProperties();

            string header = String.Join(separator, fields.Select(f => f.Name).ToArray());

            StringBuilder csvdata = new StringBuilder();
            csvdata.AppendLine(header);

            foreach (var o in objectlist)
                csvdata.AppendLine(ToCsvFields(separator, fields, o));

            return csvdata.ToString();
        }

        public static string ToCsvFields(string separator, PropertyInfo[] fields, object o)
        {
            StringBuilder linie = new StringBuilder();

            foreach (var f in fields)
            {
                if (linie.Length > 0)
                    linie.Append(separator);

                var x = f.GetValue(o);

                if (x != null)
                    linie.Append(x.ToString());
            }

            return linie.ToString();
        }


        public static DatabaseHandler _dataBaseHandler { get; set; }

        public static void DatabaseStart(string[] args)
        {
            _dataBaseHandler = new DatabaseHandler(args);

            var _globalProperties = new GlobalArgumentsHandler(args, _dataBaseHandler);




            Console.WriteLine($"[+] Attempting to load database file {_dataBaseHandler.DatabaseFullPath}");


            var rootExfilFolder = Path.Combine(_globalProperties.OutPutPath, "Exfiltration");
            while (true)
            {
                try
                {

                    //Hardcoded and rather shitty "interactive" menu
                    Console.WriteLine("[+] Available commands:\n");

                    Console.WriteLine($"    show <emails|creds|attempts|summary|fireprox>\n    export <emails|creds|attempts|summary> <csv|json> <path>\n    delete <*|fireprox-id> <fireprox-region>\n    exit");

                    Console.WriteLine();
                    Console.Write("[?] CMD #> ");

                    var selection = Console.ReadLine().Split(' ');

                    List<object> dataOut = null;
                    string formattedDataOut = "";

                    //Get the correct data source
                    if (selection.Contains("emails"))
                        dataOut = _dataBaseHandler.QueryValidAccount().Select(x => (object)x).ToList();

                    if (selection.Contains("creds"))
                        dataOut = _dataBaseHandler.QueryValidLogins().Select(x => (object)x).ToList();

                    if (selection.Contains("attempts"))
                        dataOut = _dataBaseHandler.QueryAllSprayAttempts().Select(x => (object)x).ToList();

                    if (selection.Contains("fireprox"))
                        dataOut = _dataBaseHandler.QueryAllFireProxEndpoints().Select(x => (object)x).ToList();

                    if (selection.Contains("summary"))
                    {
                        //We need to make a usefull datastructure for this
                        var sprayAttempts = _dataBaseHandler.QueryAllSprayAttempts().Select(x => x).ToList();

                        var spraySummaryList = new List<SpraySummary>() { };
                        foreach (var passwordGroups in sprayAttempts.GroupBy(x => x.Password))
                        {
                            var sortedpasswordGroups = passwordGroups.OrderByDescending(x => x.DateTime);
                            spraySummaryList.Add(new SpraySummary()
                            {
                                Password = passwordGroups.FirstOrDefault().Password,
                                SuccesCount = passwordGroups.Where(x => x.Valid).Count(),
                                TotalCount = passwordGroups.Count(),
                                StartTime = sortedpasswordGroups.Last().DateTime,
                                StopTime = sortedpasswordGroups.First().DateTime,


                            });

                        }
                        dataOut = spraySummaryList.Select(x => (object)x).ToList();
                    }


                    //Format based on o
                    if (selection.Contains("csv"))
                        formattedDataOut = ToCsv(";", dataOut);

                    if (selection.Contains("json"))
                        formattedDataOut = JsonConvert.SerializeObject(dataOut, Formatting.Indented);

                    if (selection[0].ToLower().Equals("show"))
                    {
                        if (dataOut.Count() > 0)
                        {

                            if (dataOut.FirstOrDefault().GetType().Equals(typeof(SprayAttempt)))
                                ConsoleTable
                                   .From<SprayAttemptPretty>(dataOut.Select(x => (SprayAttemptPretty)((SprayAttempt)x)))
                                   .Configure(o => o.NumberAlignment = Alignment.Right)
                                   .Write(Format.Alternative);
                            else if (dataOut.FirstOrDefault().GetType().Equals(typeof(ValidAccount)))
                                ConsoleTable
                                .From<ValidAccount>(dataOut.Select(x => (ValidAccount)x))
                                .Configure(o => o.NumberAlignment = Alignment.Right)
                                .Write(Format.Alternative);
                            else if (dataOut.FirstOrDefault().GetType().Equals(typeof(SpraySummary)))
                                ConsoleTable
                                .From<SpraySummary>(dataOut.Select(x => (SpraySummary)x))
                                .Configure(o => o.NumberAlignment = Alignment.Right)
                                .Write(Format.Alternative);
                            else if (dataOut.FirstOrDefault().GetType().Equals(typeof(SprayAttempt)))
                                ConsoleTable
                                .From<SprayAttempt>(dataOut.Select(x => (SprayAttempt)x))
                                .Configure(o => o.NumberAlignment = Alignment.Right)
                                .Write(Format.Alternative);
                            else if (dataOut.FirstOrDefault().GetType().Equals(typeof(FireProxEndpoint)))
                                ConsoleTable
                                .From<FireProxEndpoint>(dataOut.Select(x => (FireProxEndpoint)x))
                                .Configure(o => o.NumberAlignment = Alignment.Right)
                                .Write(Format.Alternative);
                        }
                        else
                        {
                            Console.WriteLine("[+] No entries!");
                        };

                    }
                    else if (selection[0].ToLower().Equals("export"))
                    {
                        var outPath = selection[selection.Length - 1];

                        //If the path supplied has spaces, we need to fix that
                        if (selection.Length > 3)
                        {
                            outPath = "";
                            bool addSpaces = false;
                            for (int i = 3; i < selection.Length; i++)
                            {
                                if (!addSpaces)
                                {
                                    addSpaces = true;
                                    outPath += selection[i];
                                }
                                else
                                    outPath += " " + selection[i];
                            }
                        }


                        File.WriteAllText(outPath, formattedDataOut);
                    }
                    else if (selection[0].ToLower().Equals("delete"))
                    {
                        //Thanks Chris!
                        if (_globalProperties.TeamFiltrationConfig == null)
                        {
                            Console.WriteLine("[!] In order to delete a fireprox endpoint you must provide the configuration file using '--config'");
                            Environment.Exit(0);
                        }
                        if (string.IsNullOrEmpty(_globalProperties.TeamFiltrationConfig?.AWSAccessKey) || string.IsNullOrEmpty(_globalProperties.TeamFiltrationConfig?.AWSSecretKey))
                        {
                            Console.WriteLine("[!] Missing AWSAccessKey, AWSSecretKey, and/or AWSSessionToken, must be provided in the configuration file using '--config'");
                            Environment.Exit(0);
                        }

                        if (selection[1].ToLower().Equals("*"))
                        {
                            //Delete ALL fireprox instances found in the DB
                            var fireproxEndpoints = _dataBaseHandler.QueryAllFireProxEndpoints().Select(x => x).ToList();
                            foreach (var fireproxEndpoint in fireproxEndpoints)
                            {
                                //There must be a way to bul delete endpoints?
                                _globalProperties._awsHandler.DeleteFireProxEndpoint(fireproxEndpoint.RestApiId, fireproxEndpoint.Region).GetAwaiter().GetResult();
                                Thread.Sleep(1000);
                            }

                        }

                        else if (selection[1].Length == 10)
                        {
                            //delete fireprox instance based on ID and region
                            if (Amazon.RegionEndpoint.GetBySystemName(selection[2]) != null)
                                _globalProperties._awsHandler.DeleteFireProxEndpoint(selection[1].ToLower(), selection[2]).GetAwaiter().GetResult();
                            else
                                Console.WriteLine("[!] Failed to parse region, example: 'delete aabbccddee us-west-1'");
                        }
                    }
                    else if (selection[0].ToLower().Equals("exit"))
                    {

                        Environment.Exit(0);


                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine("[+] Failed to parse command, are you sure the syntax is correct?");
                }

            }

        }
    }
}
