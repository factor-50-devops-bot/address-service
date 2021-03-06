﻿using AddressService.Core.Validation;
using Microsoft.Extensions.Configuration;
using System;

namespace AddressService.PostcodeLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", true)
                .Build();


            string connectionString = config.GetSection("ConnectionStrings:Application").Value;

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Connection string is null");
            }

            string postCodeFileLocation = config.GetSection("Settings:postCodeFileLocation").Value;
            if (String.IsNullOrWhiteSpace(postCodeFileLocation))
            {
                throw new Exception("Postcode file is null");
            }

            if (!Int32.TryParse(config.GetSection("Settings:batchSize").Value, out int batchSize))
            {
                throw new Exception("Can't parse batchSize setting");
            }

            if (batchSize < 1)
            {
                throw new Exception("Batch size can't be less than 1");
            }

            if (!Decimal.TryParse(config.GetSection("Settings:maxInvalidRowsPercentage").Value, out decimal maxInvalidRowsPercentage))
            {
                throw new Exception("Can't parse maxInvalidRowsPercentage setting");
            }

            // only truncate PreComputedNearestPostcodes if running the process on the AddressService
            if (!bool.TryParse(config.GetSection("Settings:isForAddressService").Value, out bool isForAddressService))
            {
                throw new Exception("Can't parse isForAddressService setting");
            }


            Console.WriteLine("Please enter option (command will run when key is pressed):");
            Console.WriteLine();
            Console.WriteLine("1 - Bulk copy postcodes into staging table ([Staging].[Postcode_Staging]) from ONS Postcode Directory csv file (download from http://geoportal.statistics.gov.uk/datasets/ons-postcode-directory-february-2020).");
            Console.WriteLine();
            Console.WriteLine("2 - Copy [Address].[Postcodes] to [Staging].[Postcodes_Switch], update/insert postcodes using staging table and switch [Staging].[Postcodes_Switch] to [Address].[Postcodes]. This also truncates the staging table ([Staging].[Postcode_Staging]) and precomputed nearby postcodes so they can be recalculated if 'isForAddressService' setting is true in appsettings.json ([Address].[PreComputedNearestPostcodes]).");
            Console.WriteLine();
            Console.WriteLine("3 - Truncate switch table ([Staging].[Postcode_Switch]).  Run when you are happy the updated postcode data has been switched correctly.");


            PostcodeLoader postcodeLoader = new PostcodeLoader(new RegexPostcodeValidator());

            ConsoleKeyInfo command = Console.ReadKey();
            Console.WriteLine();
            if (command.KeyChar == '1')
            {
                postcodeLoader.LoadPostcodesIntoStagingTable(postCodeFileLocation, connectionString, batchSize, maxInvalidRowsPercentage);
            }
            else if (command.KeyChar == '2')
            {
                postcodeLoader.LoadFromStagingTableAndSwitch(connectionString);
                postcodeLoader.TruncateStagingTable(connectionString);
                if (isForAddressService)
                {
                    postcodeLoader.TruncatePreComputedNearestPostcodes(connectionString);
                }
            }
            else if (command.KeyChar == '3')
            {
                postcodeLoader.TruncateSwitchTable(connectionString);
            }
            else
            {
                Console.WriteLine("Commmand not recognised");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
