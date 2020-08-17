using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RandomizerCore
{
    public static class Logger
    {
        static string filepath => Path.Combine(Environment.CurrentDirectory, "randomizerlog.txt");

        public static void Log(object s)
        {
            Log(s.ToString(), LogLevel.Info);
        }

        public static void LogFine(object s)
        {
            Log(s.ToString(), LogLevel.Fine);
        }

        public static void LogDebug(object s)
        {
            Log(s.ToString(), LogLevel.Debug);
        }

        public static void LogWarn(object s)
        {
            Log(s.ToString(), LogLevel.Warn);
        }

        public static void LogError(object s)
        {
            Log(s.ToString(), LogLevel.Error);
        }

        private static void Log(string s, LogLevel level)
        {
            if (level >= logLevel)
            {
                tw.WriteLine(s);
            }
        }

        public static void SetLogLevel(LogLevel level) => logLevel = level;   

        static LogLevel logLevel;
        readonly static TextWriter tw;

        static Logger()
        {
            logLevel = LogLevel.Fine;
            FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.AutoFlush = true;
            tw = TextWriter.Synchronized(sw);
        }
    }
    public enum LogLevel
    {
        Fine,
        Debug,
        Info,
        Warn,
        Error,
        Off
    }
}
