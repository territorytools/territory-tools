﻿using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;
using TerritoryTools.Alba.Controllers.UseCases;

namespace TerritoryTools.Alba.Cli
{
    [Verb("download-addresses", HelpText = "Download addresses using your account")]
    public class DownloadAddressesOptions
    {
        [Option("filepath", Required = true, HelpText = "Input file path")]
        [Value(0)]
        public string FilePath { get; set; }

        [Value(1)]
        public int AccountId { get; set; }
        
        [Usage(ApplicationAlias = "alba")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                    new Example("Download example", new DownloadAddressesOptions { FilePath = "file.csv" })
                };
            }
        }

        public void DownloadAddresses(string filePath, int accountId)
        {
            var client = Program.AlbaClient();

            client.Authenticate(Program.GetCredentials());

            var useCase = new DownloadAddressExport(client);

            useCase.SaveAs(filePath, accountId);
        }
    }
}
