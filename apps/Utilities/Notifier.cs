using System;
using System.Collections.Generic;
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
        [Display(Name="media_player.home_2")]
        Home
    }

    public static async Task Notify(this NetDaemonApp app, Uri audio, params AudioNotificationDevice[] devices)
    {
        foreach (var device in devices)
        {
            await app.CallService("media_player", "turn_on", new
            {
                entity_id = GetAudioNotificationDeviceName(device)
            }, true);

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
        NotificationAction[]? notificationActions = null,
        string? imageUrl = null,
        params TextNotificationDevice[] devices)
    {
        await SendNotificationIfCriteriaMet(app, ttsNotificationCriteria, async () => await SendTTSNotifications(app, message));
        await SendNotificationIfCriteriaMet(app, textNotificationCriteria, async () => await SendTextNotifications(app, category, message, textNotificationCriteria, devices, notificationActions, imageUrl));
    }

    public static async Task Notify(
        this NetDaemonApp app, 
        string category, 
        string message,
        NotificationCriteria textNotificationCriteria = NotificationCriteria.Always, 
        NotificationCriteria ttsNotificationCriteria = NotificationCriteria.None, 
        params TextNotificationDevice[] devices)
    {
        await SendNotificationIfCriteriaMet(app, ttsNotificationCriteria, async () => await SendTTSNotifications(app, message));
        await SendNotificationIfCriteriaMet(app, textNotificationCriteria, async () => await SendTextNotifications(app, category, message, textNotificationCriteria, devices));
    }

    private static async Task SendNotificationIfCriteriaMet(NetDaemonApp app, NotificationCriteria notificationCriteria, Func<Task> notificationAction)
    {
        switch (notificationCriteria)
        {
            case NotificationCriteria.None:
            case NotificationCriteria.Home when !app.IsAnyoneHome():
            case NotificationCriteria.NotSleeping when app.IsAnyoneSleeping():
                await Task.CompletedTask;
                break;
            default:
                await notificationAction();
                break;
        }
    }

    private static async Task SendTTSNotifications(NetDaemonApp app, string message)
    {
        // send TTS as a text message right now until they are stable
        await SendTextNotifications(app, "TTS TEST", message, NotificationCriteria.Always,
            new[] {TextNotificationDevice.Daniel});

        //await app.CallService("media_player", "turn_on", new
        //{
        //    entity_id = GetAudioNotificationDeviceName(AudioNotificationDevice.Home)
        //}, true);

        //await app.CallService("tts", "amazon_polly_say", new
        //{
        //    entity_id = GetAudioNotificationDeviceName(AudioNotificationDevice.Home),
        //    message = message
        //});
    }

    private static async Task SendTextNotifications(NetDaemonApp app, string category, string message,
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
                var person = app.State.Single(e => e.EntityId == $"person.{device.AsString(EnumFormat.Name)}".ToLower());

                if (person.State == "not_home")
                    continue;
            }

            // todo: support iphone and lookup notification type
            await app.CallService("notify", device.AsString(EnumFormat.DisplayName, EnumFormat.Name)!, new
            {
                message = message,
                title = category,
                actions = notificationActions.Select(n => new {action = n.EventId, title = n.Title}),
                image = imageUrl
            });
        }
    }

    public class NotificationAction
    {
        public string EventId { get; set; }
        public string Title { get; set; }
    }
}