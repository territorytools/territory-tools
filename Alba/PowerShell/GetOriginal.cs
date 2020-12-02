﻿using Controllers.AlbaServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using TerritoryTools.Common.AddressParser.Smart;

namespace PowerShell
{
    [Cmdlet(VerbsCommon.Get,"Original")]
    [OutputType(typeof(DuplicatedAddress))]
    public class GetOriginal : PSCmdlet
    {
       
        Parser parser;
        List<ParsedAddress> parsedMasterList;
        List<string> errors = new List<string>();

        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public AlbaAddressImport Address { get; set; }

        [Parameter]
        public List<AlbaAddressImport> MasterList { get; set; }

        [Parameter]
        public List<string> Cities { get; set; }

        [Parameter]
        public SwitchParameter IncludeSelf { get; set; }

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
            var validRegions = Region.Split(Region.Defaults);
            var streetTypes = StreetType.Split(StreetType.Defaults);
            var prefixStreetTypes = StreetType.Split(StreetType.PrefixDefaults);
            var mapStreetTypes = StreetType.Map(StreetType.Defaults);
            parser = new Parser(validRegions, Cities, streetTypes, mapStreetTypes, prefixStreetTypes);
            parser.Normalize = true;

            WriteVerbose($"Loaded Cities: {Cities.Count}");
            WriteVerbose($"Loaded Street Types:{streetTypes.Count}");

            parsedMasterList = new List<ParsedAddress>();
            errors = new List<string>();
            foreach (var master in MasterList)
            {
                string text = $"{master.Address}, {master.Suite}, {master.City}, {master.Province} {master.Postal_code}";
                //WriteVerbose($"MasterIn: {text}");
                var parsed = parser.Parse(text);
                //WriteVerbose($"MasterOut: {parsed}");
                parsedMasterList.Add(new ParsedAddress() { Address = parsed, AlbaAddressImport = master });
            }

            WriteVerbose($"Loaded Master List Addresses: {parsedMasterList.Count}");

            WriteVerbose("Sorting Master List of Addresses by Address_ID so the lowest, or oldest, record is treated as the original.");
            parsedMasterList = parsedMasterList.OrderBy(a => a.AlbaAddressImport.Address_ID).ToList();
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            try
            {
                string text = $"{Address.Address}, {Address.Suite}, {Address.City}, {Address.Province} {Address.Postal_code}";
                var parsed = parser.Parse(text);
                WriteVerbose($"Parsing text: {text} -> {parsed}");
                if (!string.IsNullOrWhiteSpace(parsed.FailedAddress))
                {
                    errors.Add($"{parsed.ErrorMessage}: {parsed.FailedAddress} ");
                }

                foreach(var master in parsedMasterList)
                {
                    //WriteVerbose($"Checking Master: {master.Address.ToString()}");
                    if (master.Address.SameAs(parsed))
                    {
                        WriteVerbose($"ORIGINAL FOUND: {Address.ToString()}");
                        var duplicate = new DuplicatedAddress
                        {
                            Original = master.AlbaAddressImport,
                            Duplicate = Address
                        };

                        WriteObject(duplicate);
                        break;
                    }
                }
            }
            catch(Exception)
            {
                throw;
            }
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
            WriteVerbose("ERRORS:");
            foreach(string error in errors)
            {
                WriteVerbose(error);
            }
        }
    }

    public class DuplicatedAddress
    {
        public AlbaAddressImport Duplicate { get; set; }
        public AlbaAddressImport Original { get; set; }
    }
}
