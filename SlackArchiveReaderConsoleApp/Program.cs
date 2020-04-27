using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]

namespace SlackArchiveReaderConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string versionMsg = $"Welcome to Slack Archive Reader version: 0.0.1.1";
                Console.WriteLine(versionMsg);
                var builder = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json");

                var configuration = builder.Build();
                string downloadFiles = configuration["DownloadFiles"];
                bool isDownloadFiles = true;
                if (!string.IsNullOrEmpty(downloadFiles) && !downloadFiles.Equals("true"))
                    isDownloadFiles = false;

                string startDate = configuration["StartDate"];
                DateTime startd = DateTime.MinValue;
                DateTime endd = DateTime.MaxValue;
                if (!string.IsNullOrEmpty(startDate))
                {
                    if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startd))
                    {
                        Console.Write("Incorrect start date set in the appsettings.json config file. Set date in yyyy-mm-dd " +
                            "format, or keep as empty if there is no start date. Press Enter key to exit ");
                        Console.ReadLine();
                        return;
                    }
                }

                string endDate = configuration["EndDate"];
                if (!string.IsNullOrEmpty(endDate))
                {
                    if (!DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endd))
                    {
                        Console.Write("Incorrect end date set in the appsettings.json config file. Set date in yyyy-mm-dd " +
                            "format, or keep as empty if there is no end date. Press Enter key to exit ");
                        Console.ReadLine();
                        return;
                    }
                }

                SlackReader slackReader = new SlackReader();
                slackReader.Load();
                Console.Write("Enter archive folder path: ");
                string archiveFolder = Console.ReadLine();

                Console.Write("Enter output folder path: ");
                string outputFolder = Console.ReadLine();
                bool success = slackReader.Start(archiveFolder, outputFolder, isDownloadFiles, startd, endd);

                if (success)
                {
                    Console.Write("Archiving completed. Press Enter key to exit ");
                }
                else
                {
                    Console.Write("Error while archiving, check log for more information. Press Enter key to exit ");
                }
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                Console.Write("Error while archiving, check log for more information. Press Enter key to exit ");
                Console.ReadLine();
            }
        }
    }
}
