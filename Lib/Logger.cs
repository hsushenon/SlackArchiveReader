using System;
using System.IO;

namespace Lib
{
    public static class Logger
    {
        public static void LogError(log4net.ILog log, Exception ex)
        {
            try
            {
                log.Error(ex);
            }
            catch (Exception e) { LogToFile(e.Message); }
        }

        public static void LogInfo(log4net.ILog log, string message)
        {
            try
            {
                log.Info(message);
            }
            catch (Exception ex) { LogToFile(ex.Message); }
        }

        public static void LogToFile(string message)
        {
            try
            {
                string m = message;
                string combinedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/log.txt");
                System.IO.File.AppendAllText(combinedPath, message + Environment.NewLine);
            }
            catch (Exception) { }
        }
    }
}