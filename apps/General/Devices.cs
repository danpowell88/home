using System;
using System.Collections.Generic;
using System.Linq;
using daemonapp.Utilities;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Devices : NetDaemonRxApp
{
    private const string TimeoutNotifications = "AvailabilityTimeoutNotifcations";

    public override void Initialize()
    {
        if (GetData<Dictionary<string, DateTime>>(TimeoutNotifications) == null)
        {
            SaveData(TimeoutNotifications, new Dictionary<string, DateTime>());
        }

        // once a day check if any sensors battery below 25%
        RunDaily("12:00:00", () =>
        {
            LogHelper.Log(this, nameof(Devices), "Low battery check");

            var lowBatteries =
                States.Where(e =>
                {
                    if (e.Attribute!.device_class == "battery" &&
                        e.Attribute.unit_of_measurement == "%" &&
                        e.Attribute.alert != false)
                    {
                        var parse = long.TryParse(e.State.ToString(), out long value);

                        if (parse)
                            return value < 25;
                    }

                    return false;
                }).ToList();

            foreach (var lowBattery in lowBatteries)
            {
                this.Notify("Device Maintenance", $"The {lowBattery.Attribute!.friendly_name} battery is low: {lowBattery.State}%",
                   Notifier.NotificationCriteria.Always
                   , Notifier.NotificationCriteria.None, Notifier.TextNotificationDevice.Daniel);
            }
        });

        // alert when any entities goes unavailable for more than the defined time, only alert once every 6 hours
        //RunEvery(TimeSpan.FromMinutes(60), async () =>
        //{
        //    var unavailableEntitiesPastTimeout = States.Where(e => e.State is null
        //                                                          && e.Attribute!.availability_timeout != null &&
        //                                                          DateTime.Now - e.LastChanged >
        //                                                          new TimeSpan(0, e.Attribute!.availability_timeout, 0)).s.New.State ==List();

        //    Log(LogLevel.Information, "Found {count} entities post availability timeout",
        //        unavailableEntitiesPastTimeout.Count);

        //    var notifications = GetData<Dictionary<string, DateTime>>(TimeoutNotifications);

        //    foreach (var entity in unavailableEntitiesPastTimeout
        //        .Where(entity => notifications is null ||
        //                         !notifications.ContainsKey(entity.EntityId) ||
        //                         DateTime.Now - notifications[entity.EntityId] > TimeSpan.FromHours(6)))
        //    {
        //        this.Notify("Device Maintenance",
        //           $"The {entity.Attribute?.friendly_name ?? entity.EntityId} has reached its availability timeout, it may be experiencing issues",
        //           Notifier.NotificationCriteria.Always
        //           , Notifier.NotificationCriteria.None, Notifier.TextNotificationDevice.Daniel);

        //        notifications![entity.EntityId] = DateTime.Now;
        //    }

        //    SaveData(TimeoutNotifications, notifications);
        //});

        base.Initialize();
    }
}