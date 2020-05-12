using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Devices : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        // once a day check if any sensors battery below 25%
        Scheduler.RunDaily("12:00:00", async () =>
        {
            var lowBatteries = State.Where(e =>
                e.Attribute.device_class == "battery" && e.Attribute.unit_of_measurement == "%" &&
                e.Attribute.alert != false && e.State < 25);

            foreach (var lowBattery in lowBatteries)
            {
                await this.Notify("Device Maintenance", $"The {lowBattery.Attribute.friendly_name} battery is low: {lowBattery.State}%",
                    Notifier.NotificationCriteria.Always
                    , Notifier.NotificationCriteria.NotSleeping, Notifier.TextNotificationDevice.Daniel);
            }
        });

        // TODO: get any entities with availability timeout

        // alert when any entities goes unavailable for more than x
        Scheduler.RunEvery(TimeSpan.FromHours(1), async () =>
        {
            var unavailableEntitiesPastTimeout = State.Where(e => e.State == null && 
                             e.Attribute.availability_timeout != null &&
                             DateTime.Now - e.LastChanged >
                                new TimeSpan(0, e.Attribute!.availability_timeout, 0));

            // TODO: store last alert time so we dont keep notifying every hour, notify once every ~ 12 hours per sensor
            foreach (var entity in unavailableEntitiesPastTimeout)
            {
                await this.Notify("Device Maintenance", $"The {entity.Attribute.friendly_name} has reached its unavailabiltiy timeout, it maye be experiencing issues: {lowBattery.State}%",
                    Notifier.NotificationCriteria.Always
                    , Notifier.NotificationCriteria.NotSleeping, Notifier.TextNotificationDevice.Daniel);
            }
        });

        return base.InitializeAsync();
    }
}