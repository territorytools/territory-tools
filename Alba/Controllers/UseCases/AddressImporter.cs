﻿using Controllers.AlbaServer;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using TerritoryTools.Alba.Controllers.AlbaServer;

namespace TerritoryTools.Alba.Controllers.UseCases
{
    public class AddressImporter
    {
        private AlbaConnection client;
        private int msDelay;
        List<AlbaLanguage> languages;

        public AddressImporter(
            AlbaConnection client, 
            int msDelay, 
            string languageFilePath)
        {
            if(string.IsNullOrWhiteSpace(languageFilePath))
            {
                throw new ArgumentNullException(nameof(languageFilePath));
            }

            this.client = client;
            this.msDelay = msDelay;
            languages = LanguageDownloader.LoadLanguagesFrom(languageFilePath);
        }

        public string Update(AlbaAddressImport address)
        {
            if (client.BasePath == null)
            {
                throw new UserException("You are not logged on to Alba.  Please Logon.");
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            AlbaAddressSave save = Convert(address);

            var url = RelativeUrlBuilder.UpdateAddress(save);
            var resultString = client.DownloadString(url);

            return resultString;
        }

        public string Add(AlbaAddressImport address)
        {
            if (client.BasePath == null)
            {
                throw new UserException("You are not logged on to Alba.  Please Logon.");
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Thread.Sleep(msDelay);

            AlbaAddressSave save = Convert(address);

            var url = RelativeUrlBuilder.AddAddress(save);
            var resultString = client.DownloadString(url);

            return resultString;
        }

        public void AddFrom(string path)
        {
            if (client.BasePath == null)
            {
                throw new UserException("You are not logged on to Alba.  Please Logon.");
            }

            if (string.IsNullOrWhiteSpace(path))
                return;
 
            using (var reader = new StreamReader(path))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToLower();
                var addresses = csv.GetRecords<AlbaAddressImport>();
                foreach (var address in addresses)
                {
                    Thread.Sleep(msDelay);

                    var save = Convert(address);
                    
                    // AddAddress for new addresses, SaveAddress for existing
                    var saveUrl = RelativeUrlBuilder.AddAddress(save);
                    var resultString = client.DownloadString(saveUrl);

                    // TODO: Need to geocode
                }
            }
        }

        AlbaAddressSave Convert(AlbaAddressImport address)
        {
            int languageId = languages
                .First(l => string.Equals(
                    l.Name,
                    address.Language,
                    StringComparison.OrdinalIgnoreCase))
                .Id;

            int statusId = AddressStatusText.Status[address.Status];

            var save = new AlbaAddressSave
            {
                Address_ID = address.Address_ID,
                Territory_ID = address.Territory_ID,
                LanguageId = languageId,
                StatusId = statusId,
                Name = address.Name,
                Suite = address.Suite,
                Address = address.Address,
                City = address.City,
                Province = address.Province,
                Postal_code = address.Postal_code,
                Country = address.Country,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                Telephone = address.Telephone,
                Notes = address.Notes,
                Notes_private = address.Notes_private
            };

            return save;
        }
    }
}
