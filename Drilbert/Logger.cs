using System;
using System.IO;

namespace Drilbert;

public static class Logger
{
    private static StreamWriter logFileWriter = null;

    static Logger()
    {
        string logPath = Constants.rootPath + "/log.txt";

        try
        {
            if (File.Exists(logPath))
                File.Move(logPath, Constants.rootPath + "/log.old.txt", true);

            FileStream logFile = File.Open(logPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            logFileWriter = new StreamWriter(logFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed opening log file " + logPath);
            Console.WriteLine(e.ToString());
        }
    }

    public static void log(string message)
    {
        string formatted = (((double)Time.getMs()) / 1000.0).ToString("#.000").PadLeft(9) + ": " + message;
        Console.WriteLine(formatted);

        try
        {
            logFileWriter.WriteLine(formatted);
            logFileWriter.Flush();
        }
        catch { }
    }
}