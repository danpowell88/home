using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using EnumsNET;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class Notifier
{
    public enum TextNotificationDevice
    {
        [Display(Name="mobile_app_daniel_s10")]
        Daniel,
        Marissa
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
                entity_id = device.AsString(EnumFormat.DisplayName, EnumFormat.Name),
                media_content_id = audio.ToString(),
                media_content_type = "music"
            });
        }
        // todo: get volume before, raise volume, set volume back to previous
    }

    public static async Task Notify(this NetDaemonApp app, string category, string message, params TextNotificationDevice[] devices)
    {
        foreach (var device in devices)
        {
            await app.CallService("notify", device.AsString(EnumFormat.DisplayName, EnumFormat.Name)!, new
            {
                message = message,
                title = category
            });
        }
    }
}