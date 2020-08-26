using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace STALauncher
{
    internal class Program
    {
        private const string SMAPIEXE = "StardewModdingAPI.exe";
        private const string SDVEXE = "Stardew Valley.exe";
        public static ConsoleLogger logger = new ConsoleLogger();

        [STAThread]
        private static void Main(string[] args)
        {
            if (File.Exists(SMAPIEXE))
            {
                logger.LogTrans("FINDED_SMAPI", ConsoleLogger.LogLevel.Info, Path.GetFullPath(SMAPIEXE));
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
                logger.LogTrans("FAIL_BOOTSTRAP", ConsoleLogger.LogLevel.Error);
                logger.Log(e.ToString(), ConsoleLogger.LogLevel.Error);
                logger.Log(e.StackTrace, ConsoleLogger.LogLevel.Error);
                logger.Pause();
            }
        }

        private static void Handle_SMAPINotFound()
        {
            string text;
            if (File.Exists(SDVEXE))
                text = logger.Trans("WARN_SMAPINOTFOUND");
            else
                text = logger.Trans("WARN_GAMENOTFOUND");
            logger.Log(text, ConsoleLogger.LogLevel.Warn);
            MainWindow window = new MainWindow();
            window.Notice.Content = text;
            window.Show(); window.Activate();
            Application.Run();
        }
    }
}