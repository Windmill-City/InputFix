using StardewValley.SDKs;
using System;

namespace StardewValley
{
#if WINDOWS || LINUX

    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        private static SDKHelper _sdk;

        public static SDKHelper sdk
        {
            get
            {
                if (Program._sdk == null)
                {
                    Program._sdk = new SteamHelper();
                }
                return Program._sdk;
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }

#endif
}