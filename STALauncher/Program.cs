using STALauncher.Properties;
using System;
using System.IO;
using System.Reflection;

namespace STALauncher
{
    internal class Program
    {
        private const string SMAPIEXE = "StardewModdingAPI.exe";
        private const string SDVEXE = "Stardew Valley.exe";
        private static ConsoleLogger logger = new ConsoleLogger();

        [STAThread]
        private static void Main(string[] args)
        {
            if (File.Exists(SMAPIEXE))
            {
                logger.Log(Resources.FINDED_SMAPI, ConsoleLogger.LogLevel.Info, Path.GetFullPath(SMAPIEXE));
                IconHelper.SetConsoleIcon(IconHelper.GetIconOf(SMAPIEXE));
                BootStrapSMAPI(args);
            }
            else
                Handle_SMAPINotFound();
        }

        private static void BootStrapSMAPI(string[] args)
        {
            try
            {
                Type program = Type.GetType("StardewModdingAPI.Program, " + "StardewModdingAPI", true);
                MethodInfo main = program.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                main.Invoke(null, new object[] { args });
            }
            catch (Exception e)
            {
                logger.Log(Resources.FAIL_BOOTSTRAP, ConsoleLogger.LogLevel.Error);
                logger.Log(e.ToString(), ConsoleLogger.LogLevel.Error);
                logger.Log(e.StackTrace, ConsoleLogger.LogLevel.Error);
                logger.Pause();
            }
        }

        private static void Handle_SMAPINotFound()
        {
            if (File.Exists(SDVEXE))
                logger.Log(Resources.WARN_SMAPINOTFOUND, ConsoleLogger.LogLevel.Warn);
            else
                logger.Log(Resources.WARN_GAMENOTFOUND, ConsoleLogger.LogLevel.Warn);
            logger.Pause();
        }
    }
}