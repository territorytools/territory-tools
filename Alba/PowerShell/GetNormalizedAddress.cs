﻿using Controllers.AlbaServer;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using TerritoryTools.Common.AddressParser.Smart;

namespace TerritoryTools.Alba.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "NormalizedAddresses")]
    [OutputType(typeof(NormalizedAddress))]
    public class GetNormalizedAddress : PSCmdlet
    {
        Parser parser;

        [Parameter(
           //Mandatory = true,
           Position = 0,
           ValueFromPipeline = true,
           ValueFromPipelineByPropertyName = true)]
        public string Address { get; set; }

        [Parameter(
           //Mandatory = true,
           Position = 1,
           ValueFromPipeline = true,
           ValueFromPipelineByPropertyName = true)]
        public AlbaAddressImport AddressImport { get; set; }

        [Parameter]
        public List<string> Cities { get; set; }

        protected override void BeginProcessing()
        {
            var validRegions = Region.Split(Region.Defaults);
            var streetTypes = StreetType.Split(StreetType.Defaults);
            var mapStreetTypes = StreetType.Map(StreetType.Defaults);
            var prefixStreetTypes = StreetType.Split(StreetType.PrefixDefaults);
            parser = new Parser(validRegions, Cities, streetTypes, mapStreetTypes, prefixStreetTypes);
        }

        protected override void ProcessRecord()
        {
            var normalized = new NormalizedAddress();

            try
            {
                parser.Normalize = true;
                string text = Address;
                if(AddressImport != null)
                {
                    text = AddressImport.ToAddressString();
                }

                Address parsed = parser.Parse(text);
                
                normalized = new NormalizedAddress
                {
                    Original = text,
                    StreetNamePrefix = parsed.Street.Name.NamePrefix,
                    StreetNumber = parsed.Street.Number,
                    StreetNumberFraction = parsed.Street.NumberFraction,
                    DirectionalPrefix = parsed.Street.Name.DirectionalPrefix,
                    StreetTypePrefix = parsed.Street.Name.StreetTypePrefix,
                    StreetName = parsed.Street.Name.Name,
                    StreetType = parsed.Street.Name.StreetType,
                    DirectionalSuffix = parsed.Street.Name.DirectionalSuffix,
                    UnitType = parsed.Unit.Type,
                    UnitNumber = parsed.Unit.Number,
                    City = parsed.City.Name,
                    Region = parsed.Region.Code,
                    PostalCode = parsed.Postal.Code,
                    PostalCodeExtra = parsed.Postal.Extra,
                    Errors = parsed.ErrorMessage
                };
            }
            catch(Exception e)
            {
                WriteError(new ErrorRecord(e, "1", ErrorCategory.NotSpecified, null));
            }
               

            WriteObject(normalized);
        }
    }

    public class NormalizedAddress
    {
        public string Original { get; set; }
        public string StreetNamePrefix { get; set; }
        public string StreetNumber { get; set; }
        public string StreetNumberFraction { get; set; }
        public string DirectionalPrefix { get; set; }
        public string StreetTypePrefix { get; set; }
        public string StreetName { get; set; }
        public string StreetType { get; set; }
        public string DirectionalSuffix { get; set; }
        public string UnitType { get; set; }
        public string UnitNumber { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string PostalCodeExtra { get; set; }
        public string Errors { get; internal set; }
    }
}
