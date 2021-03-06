﻿using Controllers.AlbaServer;
using System;
using System.Management.Automation;
using TerritoryTools.Alba.Controllers.UseCases;

namespace TerritoryTools.Alba.PowerShell
{
    [Cmdlet(VerbsCommon.Add,"AlbaAddress")]
    public class AddAlbaAddress : AlbaConnectedCmdlet
    {
        [Parameter(
            Mandatory = false,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public AlbaAddressImport Address { get; set; }

        [Parameter]
        public int UploadDelayMs { get; set; } = 300;

        AddressImporter importer;

        protected override void BeginProcessing()
        {
            if(Languages == null || Languages.Count == 0)
            {
                throw new ArgumentNullException(nameof(Languages));
            }

            importer = new AddressImporter(
                Connection,
                UploadDelayMs,
                languages: Languages);
        }

        protected override void ProcessRecord()
        {
            try
            {
                string result = importer.Add(Address);
                
                WriteVerbose($"Result: {result}");
            }
            catch(Exception e)
            {
                throw;
                //WriteError(new ErrorRecord(e, "1", ErrorCategory.NotSpecified, null));
            }
        }
    }
}
