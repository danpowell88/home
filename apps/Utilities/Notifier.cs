using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EnumsNET;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class Notifier
{
    public enum TextNotificationDevice
    {
        [Display(Name="mobile_app_daniel_s10")]
        Daniel,
        Marissa,
        All,
    }

    public enum NotificationCriteria
    {
        None,
        Always,
        Home,
        NotSleeping
    }

    public enum AudioNotificationDevice
    {
        [Display(Name="media_player.home_2")]
        Home
    }

    public static async Task Notify(this NetDaemonApp app, Uri audio, params AudioNotificationDevice[] devices)
    {
        foreach (var device in devices)
        {
            await app.CallService("media_player", "play_media", new
            {
                entity_id = GetAudioNotificationDeviceName(device),
                media_content_id = audio.ToString(),
                media_content_type = "music"
            });
        }
        // todo: get volume before, raise volume, set volume back to previous
    }

    private static string GetAudioNotificationDeviceName(AudioNotificationDevice device)
    {
        return device.AsString(EnumFormat.DisplayName, EnumFormat.Name)!;
    }

    public static async Task Notify(
        this NetDaemonApp app, 
        string category, 
        string message,
        NotificationCriteria textNotificationCriteria = NotificationCriteria.Always, 
        NotificationCriteria ttsNotificationCriteria = NotificationCriteria.None, 
        params TextNotificationDevice[] devices)
    {
        // todo: TTS

        SendNotificationIfCriteriaMet(app, ttsNotificationCriteria, () => );
        SendNotificationIfCriteriaMet(app, textNotificationCriteria, async () => await SendTextNotifications(app, category, message, textNotificationCriteria, devices));

    }

    private static void SendNotificationIfCriteriaMet(NetDaemonApp app, NotificationCriteria ttsNotificationCriteria, Action notificationAction)
    {
        if (
            ttsNotificationCriteria != NotificationCriteria.None ||
            ttsNotificationCriteria == NotificationCriteria.Home && app.IsAnyoneHome() ||
            ttsNotificationCriteria == NotificationCriteria.Always ||
            ttsNotificationCriteria == NotificationCriteria.NotSleeping && !app.IsAnyoneSleeping())
        {
            notificationAction();
        }
    }

    private static async Task SendTextNotifications(NetDaemonApp app, string category, string message,
        NotificationCriteria textNotificationCriteria, TextNotificationDevice[] devices)
    {
        var effectiveDevices = devices.ToList();

        if (devices.Contains(TextNotificationDevice.All))
        {
            effectiveDevices = Enums.GetValues<TextNotificationDevice>().Where(d => d != TextNotificationDevice.All)
                .ToList();
        }

        foreach (var device in effectiveDevices)
        {
            if (textNotificationCriteria == NotificationCriteria.Home)
            {
                var person = app.State.Single(e => e.EntityId == $"person.{device.AsString(EnumFormat.Name)}".ToLower());

                if (person.State == "not_home")
                    continue;
            }

            await app.CallService("notify", device.AsString(EnumFormat.DisplayName, EnumFormat.Name)!, new
            {
                message = message,
                title = category
            });
        }
    }
}