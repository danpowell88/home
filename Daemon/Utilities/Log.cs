using NetDaemon.Common.Reactive;

namespace daemonapp.Utilities
{
    public static class LogHelper
    {
        public static void Log(this NetDaemonRxApp app, string name, string automationName)
        {
            app.CallService("logbook", "log", new { domain = "automation", name, message = $"{automationName} triggered" });
        }
    }
}
