using Microsoft.Extensions.Configuration;
using System;
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
                string versionMsg = $"Welcome to Slack Archive Reader version: 0.0.1.0";
                Console.WriteLine(versionMsg);
                var builder = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json");

                var configuration = builder.Build();
                string downloadFiles = configuration["DownloadFiles"];
                bool isDownloadFiles = true;
                if (!string.IsNullOrEmpty(downloadFiles) && !downloadFiles.Equals("true"))
                    isDownloadFiles = false;

                SlackReader slackReader = new SlackReader();
                slackReader.Load();
                Console.Write("Enter archive folder path: ");
                string archiveFolder = Console.ReadLine();

                //TEST code
                //archiveFolder = @"C:\Projects\My projects\SlackReaderApp\BowbazarSlackExportJun2017_Test2";
                Console.Write("Enter output folder path: ");
                string outputFolder = Console.ReadLine();
                //outputFolder = @"C:\Projects\My projects\SlackReaderApp\ou\";

                bool success = slackReader.Start(archiveFolder, outputFolder, isDownloadFiles);

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
