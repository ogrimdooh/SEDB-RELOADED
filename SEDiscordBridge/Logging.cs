using Sandbox.ModAPI;
using System;
using System.IO;
using VRage.Utils;

namespace SEDiscordBridge
{
    public sealed class Logging
    {

        public enum LogLevel
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3,
            StackTrace = 4,
            Debug = 5
        }

        private static Logging _instance;
        public static Logging Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static Logging Load()
        {
            _instance = new Logging();
            return _instance;
        }

        public void Log(LogLevel level, string message)
        {
            if (MyLog.Default != null)
                MyLog.Default.WriteLineAndConsole($"SEDB_LITE [{level}] - {message}");
            else
            {
                switch (level)
                {
                    case LogLevel.Info:
                        SEDiscordBridgePlugin.Log.Info(message);
                        break;
                    case LogLevel.Warning:
                        SEDiscordBridgePlugin.Log.Warn(message);
                        break;
                    case LogLevel.Error:
                        SEDiscordBridgePlugin.Log.Error(message);
                        break;
                    case LogLevel.Fatal:
                        SEDiscordBridgePlugin.Log.Fatal(message);
                        break;
                    case LogLevel.StackTrace:
                        SEDiscordBridgePlugin.Log.Trace(message);
                        break;
                    case LogLevel.Debug:
                        SEDiscordBridgePlugin.Log.Debug(message);
                        break;
                    default:
                        break;
                }
            }
        }

        public void LogError(Type caller, Exception ex, string msg = null, bool fatal = false)
        {
            Log(fatal ? LogLevel.Fatal : LogLevel.Error, $"{caller?.Name}: {msg}{ex.Message}");
            Log(LogLevel.StackTrace, $"{ex.StackTrace}");
        }

        public void LogError<T>(Exception ex, string msg = null, bool fatal = false)
        {
            LogError(typeof(T), ex, msg, fatal);
        }

        public void LogWarning(Type caller, string message)
        {
            Log(LogLevel.Warning, $"{caller?.Name}: {message}");
        }

        public void LogWarning<T>(string message)
        {
            LogWarning(typeof(T), message);
        }

        public void LogInfo(Type caller, string message)
        {
            Log(LogLevel.Info, $"{caller?.Name}: {message}");
        }

        public void LogInfo<T>(string message)
        {
            LogInfo(typeof(T), message);
        }

        public void LogDebug(Type caller, string message)
        {
            Log(LogLevel.Debug, $"{caller?.Name}: {message}");
        }

        public void LogDebug<T>(string message)
        {
            LogDebug(typeof(T), message);
        }

    }

}
