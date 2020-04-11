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
                //LogToFile(SessionDetail.UserName + ". " + ex.Message + ex.StackTrace);
                log.Error(ex);
            }
            catch (Exception e) { LogToFile(e.Message); }
        }

        public static void LogInfo(log4net.ILog log, string message)
        {
            try
            {
                //LogToFile(message);
                log.Info(message);
            }
            catch (Exception ex) { LogToFile(ex.Message); }
        }

        public static void LogToFile(string message)
        {
            try
            {
                string m = message;
                string combinedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/log.txt");
                //using (StreamWriter w = File.AppendText(combinedPath))
                //{
                //    Log(m, w);
                //}
            }
            catch (Exception) { }
        }

        public static void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
            w.WriteLine("  :");
            w.WriteLine($"  :{logMessage}");
        }
    }
}