using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Vacuum : NetDaemonApp
{
    private readonly Dictionary<string, List<int>> _roomMapping =
        new Dictionary<string, List<int>>
        {
            {"office", new List<int> {1}},
            {"toilet", new List<int> {1}},
            {"bath", new List<int> {1}},
            {"media", new List<int> {1}},
            {"gym", new List<int> {1}},
            {"marissasoffice", new List<int> {1}},
            {"entry", new List<int> {1}},
            {"living", new List<int> {1}},
            {"dining", new List<int> {1}},
            {"guest", new List<int> {1}},
            {"master", new List<int> {1}},
            {"laundry", new List<int> {1}},
            {"kitchen", new List<int> {1}},
            {"tiles", new List<int> {1}}
        };

    public override Task InitializeAsync()
    {
        Entity("input_select.vacuum_reset_consumable")
            .WhenStateChange(from: "Select Input")
            .Call(async (_, to, ___) =>
            {
                var con = to!.State switch
                {
                    "Main Brush" => "main_brush_work_time",
                    "Side Brush" => "side_brush_work_time",
                    "Sensors" => "sensor_dirty_time",
                    "Filter" => "filter_work_time",
                    _ => ""
                };

                await CallService("vacuum", "send_command", new
                {
                    entity_id = "vacuum.xiaomi_vacuum_cleaner",
                    command = "reset_consumable",
                    @params = new List<string> { con }
                });

                await InputSelect("input_select.vacuum_reset_consumable").SetOption("Select Input").ExecuteAsync();
            })
            .Execute();

        Events(e => e.EventId == "ifttt_webhook_received" && e.Data!.action == "vacuum")
            .Call(async (ev, data) => { await CleanRoom(data!.room); })
            .Execute();

        Entity("input_select.vacuum_room")
            .WhenStateChange(from: "Select Input")
            .Call(async (entityId, to, from) =>
            {
                await CleanRoom(to!.State);
                await InputSelect("input_select.vacuum_room").SetOption("Select Input").ExecuteAsync();
            })
            .Execute();

        Entity("group.family")
            .WhenStateChange(from: "not_home", to: "home")
            .AndNotChangeFor(TimeSpan.FromMinutes(60))
            .Call(async (_, __, ___) =>
            {
                if (GetState("input_boolean.vacuumed_today")!.State == "off")
                {
                    await this.Notify(
                        "Vacuum",
                        "Would you like to vacuum the house?",
                        Notifier.NotificationCriteria.Always,
                        Notifier.NotificationCriteria.None,
                        new[]
                        {
                            new Notifier.NotificationAction("vacuum_house", "Yes"),
                            new Notifier.NotificationAction("vacuum_tiles", "Tiles"),
                            new Notifier.NotificationAction("vacuum_no", "No")
                        },
                        Notifier.TextNotificationDevice.All);
                }
            })
            .Execute();

        Entity("vacuum.xiaomi_vacuum_cleaner")
            .WhenStateChange(from: "cleaning", to: "returning")
            .Call(async (entityId, to, from) =>
            {
                if (to!.Attribute!.cleaned_area > 50)
                {
                    await Entity("input_boolean.vacuumed_today").TurnOn().ExecuteAsync();
                }
            })
            .Execute();

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "vacuum_house")
            .Call(async (_, __) =>
            {
                await CallService("vacuum", "start", new { entity_id = "vacuum.xiaomi_vacuum_cleaner" });
            })
            .Execute();

        Events(e => e.EventId == "mobile_app_notification_action" && e.Data!.action == "vacuum_tiles")
            .Call(async (_, __) => { await CleanRoom("tiles"); })
            .Execute();

        Scheduler.RunDaily("00:00:00",
            async () => await Entity("input_boolean.vacuumed_today").TurnOff().ExecuteAsync());

        return base.InitializeAsync();
    }

    public async Task CleanRoom(string room)
    {
        var sanitizeRoom = room.RemoveNonAlphaCharacters().Replace("room", string.Empty);

        var roomMappingName = _roomMapping.Keys.FirstOrDefault(k => sanitizeRoom.Contains(k));

        if (roomMappingName != null)
        {
            await CallService("vacuum", "send_command", new
            {
                entity_id = "vacuum.xiaomi_vacuum_cleaner",
                command = "app_segment_clean",
                @params = _roomMapping[roomMappingName]
            });
        }
    }
}