using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoySoftware.HomeAssistant.NetDaemon.Common;

[UsedImplicitly]
public class Batteries : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        // once a day check if any sensors below 25%

        Scheduler.RunDaily("12:00:00", async () =>
        {
            var lowBatteries = State.Where(e =>
                e.Attribute.device_class == "battery" && e.Attribute.unit_of_measurement == "%" &&
                e.Attribute.alert != false && e.State < 25);

            foreach (var lowBattery in lowBatteries)
            {
                await this.Notify("", $"The {lowBattery.Attribute.friendly_name} battery is low: {lowBattery.State}%",
                    Notifier.NotificationCriteria.Always
                    , Notifier.NotificationCriteria.NotSleeping, Notifier.TextNotificationDevice.Daniel);
            }
        });

        return base.InitializeAsync();
    }
}