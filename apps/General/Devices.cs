using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[UsedImplicitly]
public class Devices : NetDaemonApp
{
    private const string TimeoutNotifications = "AvailabilityTimeoutNotifcations";

    public override async Task InitializeAsync()
    {
        if (await GetDataAsync<Dictionary<string, DateTime>>(TimeoutNotifications) == null)
        {
            await SaveDataAsync(TimeoutNotifications,new Dictionary<string, DateTime>());
        }

        // once a day check if any sensors battery below 25%
        Scheduler.RunDaily("12:00:00", async () =>
        {
            var lowBatteries = State.Where(e =>
                e.Attribute!.device_class == "battery" && e.Attribute.unit_of_measurement == "%" &&
                e.Attribute.alert != false && e.State < 25);

            foreach (var lowBattery in lowBatteries)
            {
                await this.Notify("Device Maintenance", $"The {lowBattery.Attribute!.friendly_name} battery is low: {lowBattery.State}%",
                    Notifier.NotificationCriteria.Always
                    , Notifier.NotificationCriteria.None, Notifier.TextNotificationDevice.Daniel);
            }
        });

        // alert when any entities goes unavailable for more than the defined time, only alert once every 6 hours
        Scheduler.RunEvery(TimeSpan.FromMinutes(60), async () =>
        {
            var unavailableEntitiesPastTimeout = State.Where(e => e.State is null
                                                                  && e.Attribute!.availability_timeout != null &&
                                                                  DateTime.Now - e.LastChanged >
                                                                  new TimeSpan(0, e.Attribute!.availability_timeout, 0)).ToList();

            Log(LogLevel.Information, "Found {count} entities post availability timeout",
                unavailableEntitiesPastTimeout.Count);

            var notifications = await GetDataAsync<Dictionary<string, DateTime>>(TimeoutNotifications);

            foreach (var entity in unavailableEntitiesPastTimeout
                .Where(entity => notifications is null || 
                                 !notifications.ContainsKey(entity.EntityId) || 
                                 DateTime.Now - notifications[entity.EntityId] > TimeSpan.FromHours(6)))
            {
                await this.Notify("Device Maintenance",
                    $"The {entity.Attribute?.friendly_name ?? entity.EntityId} has reached its availability timeout, it may be experiencing issues",
                    Notifier.NotificationCriteria.Always
                    , Notifier.NotificationCriteria.None, Notifier.TextNotificationDevice.Daniel);

                notifications![entity.EntityId] = DateTime.Now;
            }

            await SaveDataAsync(TimeoutNotifications, notifications);
        });

        await base.InitializeAsync();
    }
}