using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EnumsNET;
using NetDaemon.Common.Reactive;

public static class Notifier
{
    public enum TextNotificationDevice
    {
        [Display(Name="mobile_app_daniel_s10")]
        Daniel,
        //Marissa,
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
        [Display(Name="media_player.home")]
        Home,
        [Display(Name = "media_player.kitchen_assistant")]
        Kitchen
    }

    public static void Notify(this NetDaemonRxApp app, Uri audio, decimal? volume = null,params AudioNotificationDevice[] devices)
    {
        foreach (var device in devices)
        {
            app.CallService("media_player", "turn_on", new
            {
                entity_id = GetAudioNotificationDeviceName(device)
            });

            app.CallService("media_player", "volume_set", new
            {
                entity_id = GetAudioNotificationDeviceName(device),
                volume_level = volume ?? GetVolume(app)
            });

            app.CallService("media_player", "play_media", new
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

    public static void Notify(
        this NetDaemonRxApp app,
        string category,
        string message,
        NotificationCriteria textNotificationCriteria,
        NotificationCriteria ttsNotificationCriteria,
        NotificationAction[]? notificationActions,
        string? imageUrl,
        params TextNotificationDevice[] devices)
    {
        SendNotificationIfCriteriaMet(app, ttsNotificationCriteria, () => SendTTSNotifications(app, message));
        SendNotificationIfCriteriaMet(app, textNotificationCriteria, () => SendTextNotifications(app, category, message, textNotificationCriteria, devices, notificationActions, imageUrl));
    }

    public static void Notify(
        this NetDaemonRxApp app, 
        string category, 
        string message,
        NotificationCriteria textNotificationCriteria = NotificationCriteria.Always, 
        NotificationCriteria ttsNotificationCriteria = NotificationCriteria.None, 
        params TextNotificationDevice[] devices)
    {
        Notify(app, category, message, textNotificationCriteria, ttsNotificationCriteria, null, null, devices);
    }

    public static void Notify(
        this NetDaemonRxApp app,
        string category,
        string message,
        NotificationCriteria textNotificationCriteria,
        NotificationCriteria ttsNotificationCriteria,
        NotificationAction[] notificationActions,
        params TextNotificationDevice[] devices)
    {
        Notify(app, category, message, textNotificationCriteria, ttsNotificationCriteria, notificationActions, null, devices);
    }

    private static void SendNotificationIfCriteriaMet(NetDaemonRxApp app, NotificationCriteria notificationCriteria, Action notificationAction)
    {
        switch (notificationCriteria)
        {
            case NotificationCriteria.None:
            case NotificationCriteria.Home when !app.IsAnyoneHome():
            case NotificationCriteria.NotSleeping when app.IsAnyoneSleeping():
                break;
            default:
                notificationAction();
                break;
        }
    }

    private static void SendTTSNotifications(NetDaemonRxApp app, string message)
    {
        var ttsEnabled = app.State("input_boolean.tts_enabled")!.State;

        if (ttsEnabled == "on")
        {
            app.CallService("media_player", "turn_on", new
            {
                entity_id = GetAudioNotificationDeviceName(AudioNotificationDevice.Home)
            });

            SetTTSVolume(app);

            app.CallService("tts", "amazon_polly_say", new
            {
                entity_id = GetAudioNotificationDeviceName(AudioNotificationDevice.Home),
                message = message
            });
        }
        else
        {
            SendTextNotifications(app, "TTS TEST", message, NotificationCriteria.Always, new[] {TextNotificationDevice.Daniel});
        }
    }

    private static void SendTextNotifications(NetDaemonRxApp app, string category, string message,
        NotificationCriteria textNotificationCriteria, TextNotificationDevice[] devices, NotificationAction[]? notificationActions = null, string? imageUrl = null)
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
                var person = app.State($"person.{device.AsString(EnumFormat.Name)}".ToLower());

                if (person!.State == "not_home")
                    continue;
            }

            // todo: support iphone and lookup notification type
            app.CallService("notify", device.AsString(EnumFormat.DisplayName, EnumFormat.Name)!, new
            {
                message = $"{DateTime.Now:t}:{message}",
                title = category,
                data = new
                {
                    ttl = 0,
                    priority = "high",
                    actions = notificationActions?.Select(n => new {action = n.EventId, title = n.Title}),
                    image = imageUrl
                }
            });
        }
    }

    public static void SetTTSVolume(this NetDaemonRxApp app)
    {
        var deviceName = GetAudioNotificationDeviceName(AudioNotificationDevice.Home);

        if (app.State(deviceName)!.State != "playing")
        {
            app.SetVolume(GetVolume(app), deviceName);
        }
    }

    private static decimal GetVolume(NetDaemonRxApp app)
    {
        if (app.IsAnyoneSleeping())
            return 0.3M;

        if (DateTime.Now.Hour > 0 && DateTime.Now.Hour <= 8)
            return 0.3M;
        if (DateTime.Now.Hour > 8 && DateTime.Now.Hour <= 20)
            return 1M;
        if (DateTime.Now.Hour > 20 && DateTime.Now.Hour <= 23)
            return 0.3M;

        return 0.5M;
    }

    public class NotificationAction
    {
        public NotificationAction(string eventId, string title)
        {
            EventId = eventId;
            Title = title;
        }

        public string EventId { get; set; }
        public string Title { get; set; }
    }
}