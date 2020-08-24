using StardewModdingAPI;
using StardewValley;
using System.Globalization;
using System.Resources;

namespace InputFix
{
    public enum NotifyPlace
    {
        Monitor,
        GameHUD,
        MonitorAndGameHUD
    }

    public enum NotifyMoment
    {
        Immediate,
        GameLaunched,
        SaveLoaded
    }

    public class NotifyHelper
    {
        private IModHelper helper;
        private IMonitor monitor;

        public static ResourceManager resxMgr = new ResourceManager("InputFix.Properties.Resources", typeof(ModEntry).Assembly);

        public NotifyHelper(IMonitor monitor, IModHelper helper)
        {
            this.monitor = monitor;
            this.helper = helper;
        }

        public void Notify(string resxName, NotifyPlace place, NotifyMoment moment, LogLevel level = LogLevel.Info)
        {
            Notify(resxName, CultureInfo.CurrentUICulture, place, moment, level);
        }

        public void Notify(string resxName, CultureInfo culture, NotifyPlace place, NotifyMoment moment, LogLevel level = LogLevel.Info)
        {
            string text = resxMgr.GetString(resxName, culture);
            switch (moment)
            {
                case NotifyMoment.Immediate:
                    doNotify(place, text, level);
                    break;

                case NotifyMoment.GameLaunched:
                    helper.Events.GameLoop.GameLaunched += new System.EventHandler<StardewModdingAPI.Events.GameLaunchedEventArgs>((sender, e) =>
                    {
                        doNotify(place, text, level);
                    });
                    break;

                case NotifyMoment.SaveLoaded:
                    helper.Events.GameLoop.SaveLoaded += new System.EventHandler<StardewModdingAPI.Events.SaveLoadedEventArgs>((sender, e) =>
                    {
                        doNotify(place, text, level);
                    });
                    break;
            }
        }

        private void doNotify(NotifyPlace place, string text, LogLevel level)
        {
            switch (place)
            {
                case NotifyPlace.Monitor:
                    NotifyMonitor(text, level);
                    break;

                case NotifyPlace.GameHUD:
                    NotifyHUD(text);
                    break;

                case NotifyPlace.MonitorAndGameHUD:
                    NotifyHUD(text);
                    NotifyMonitor(text, level);
                    break;
            }
        }

        public void NotifyHUD(string text)
        {
            var msg = new HUDMessage(text);
            msg.noIcon = true;
            Game1.addHUDMessage(msg);
            msg.timeLeft = 500 * msg.message.Length;
        }

        public void NotifyMonitor(string text, LogLevel level = LogLevel.Info)
        {
            monitor.Log(text, level);
        }

        public void NotifyMonitorOnce(string text, LogLevel level = LogLevel.Info)
        {
            monitor.LogOnce(text, level);
        }
    }
}