using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using NetDaemon.Common.Reactive;

public class Vacuum : NetDaemonRxApp
{
    private readonly Dictionary<string, List<int>> _roomMapping =
        new Dictionary<string, List<int>>
        {
            {"office", new List<int> {21}},
            {"toilet", new List<int> {23}},
            {"hallway", new List<int> {24}},
            {"bath", new List<int> {3}},
            {"media", new List<int> {17}},
            {"gym", new List<int> {19}},
            {"marissasoffice", new List<int> {5}},
            {"entry", new List<int> {22}},
            {"living", new List<int> {16}},
            {"dining", new List<int> {18}},
            {"guest", new List<int> {2}},
            {"master", new List<int> {25}},
            {"ensuite", new List<int> {26}},
            {"laundry", new List<int> {27}},
            {"kitchen", new List<int> {20}}
        };

    public override void Initialize()
    {
        _roomMapping.Add("tiles",
            _roomMapping["toilet"]
                .Union(_roomMapping["hallway"])
                .Union(_roomMapping["bath"])
                .Union(_roomMapping["entry"])
                .Union(_roomMapping["living"])
                .Union(_roomMapping["dining"])
                .Union(_roomMapping["laundry"])
                .Union(_roomMapping["kitchen"])
                .ToList());


        Entity("input_select.vacuum_reset_consumable")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "Select Input")
            .Subscribe(s =>
            {
                var con = s.New.State switch
                {
                    "Main Brush" => "main_brush_work_time",
                    "Side Brush" => "side_brush_work_time",
                    "Sensors" => "sensor_dirty_time",
                    "Filter" => "filter_work_time",
                    _ => ""
                };

                CallService("vacuum", "send_command", new
                {
                    entity_id = "vacuum.xiaomi_vacuum_cleaner",
                    command = "reset_consumable",
                    @params = new List<string> {con}
                });


                Entity("input_select.vacuum_reset_consumable").SetOption("Select Input");
            });


        EventChanges
            .Where(e => e.Event == "ifttt_webhook_received" && e.Data!.action == "vacuum")
            .Subscribe(s => { CleanRoom(s.Data!.room); });

        Entity("input_select.vacuum_room")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "Select Input")
            .Subscribe(s =>
            {
                CleanRoom(s.New.State);
                Entity("input_select.vacuum_reset_consumable").SetOption("Select Input");
            });


        Entity("group.family")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "home" && s.New.State == "not_home")
            .NDSameStateFor(TimeSpan.FromMinutes(60))
            .Subscribe(_ =>
            {
                if (State("input_boolean.vacuumed_today")!.State == "off" &&
                    State("vacuum.xiaomi_vacuum_cleaner")!.State == "docked")
                {
                    this.Notify(
                        "Vacuum",
                        "Would you like to vacuum the house?",
                        Notifier.NotificationCriteria.Always,
                        Notifier.NotificationCriteria.None,
                        new[]
                        {
                            new Notifier.NotificationAction("vacuum_house", "Yes"),
                            new Notifier.NotificationAction("vacuum_tiles", "Tiles"),
                            new Notifier.NotificationAction("vacuum_no", "No"),
                            new Notifier.NotificationAction("vacuum_silence", "Silence")
                        },
                        Notifier.TextNotificationDevice.All);
                }
            });

        Entity("group.family")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .Subscribe(_ =>
            {
                if (State("vacuum.xiaomi_vacuum_cleaner")!.State != "docked")
                {
                    CallService("vacuum", "return_to_base", new
                    {
                        entity_id = "vacuum.xiaomi_vacuum_cleaner"
                    });
                }
            });

        Entity("vacuum.xiaomi_vacuum_cleaner")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "cleaning" && s.New.State == "returning")
            .Subscribe(s =>
            {
                if (s.New!.Attribute!.cleaned_area > 50)
                {
                    Entity("input_boolean.vacuumed_today").TurnOn();
                }
            });

        EventChanges.Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "vacuum_house")
            .Subscribe(_ => CallService("vacuum", "start", new {entity_id = "vacuum.xiaomi_vacuum_cleaner"}));
            

        EventChanges.Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "vacuum_tiles")
            .Subscribe(_ => CleanRoom("tiles"));

        EventChanges.Where(e => e.Event == "mobile_app_notification_action" && e.Data!.action == "vacuum_silence")
            .Subscribe(_ =>   Entity("input_boolean.vacuumed_today").TurnOn());


        RunDaily("00:00:00", () => Entity("input_boolean.vacuumed_today").TurnOff());

        base.Initialize();
    }

    private void CleanRoom(string room)
    {
        var sanitizeRoom = room.ToLower().RemoveNonAlphaCharacters()
            .Replace("the", string.Empty)
            .Replace("room", string.Empty);

        var roomMappingName = _roomMapping.Keys.FirstOrDefault((k => sanitizeRoom.Contains(k)));

        if (roomMappingName != null)
        {
             CallService("vacuum", "send_command", new
            {
                entity_id = "vacuum.xiaomi_vacuum_cleaner",
                command = "app_segment_clean",
                @params = _roomMapping[roomMappingName]
            });
        }
    }
}