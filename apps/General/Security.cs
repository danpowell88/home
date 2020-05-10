using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnumsNET;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Security : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        Entities(e =>
                new List<DeviceClass> {DeviceClass.Door, DeviceClass.Garage, DeviceClass.Window}.Select(c =>
                    c.AsString(EnumFormat.DisplayName, EnumFormat.Name)).ToList().Contains(e.Attribute!.device_class) &&
                !e.Attribute?.location == "internal")
            .WhenStateChange((to, from) =>
                new List<string> {"closed", "off"}.Contains(from!.State) &&
                new List<string> {"open", "on"}.Contains(to!.State))
            .Call(async (_, to, from) =>
            {
                if (await this.TrueNowAndAfter(
                    () => State.Single(e => e.EntityId == "input_boolean.left_home").State == true,
                    new TimeSpan(0, 2, 0)))
                {
                    await this.Notify(
                        "Security",
                        $"The {to!.Attribute!.friendly_name.ToLower()} has been opened",
                        Notifier.NotificationCriteria.Always,
                        Notifier.NotificationCriteria.None,
                        Notifier.TextNotificationDevice.All);
                }
            })
            .Execute();

        return base.InitializeAsync();
    }
}