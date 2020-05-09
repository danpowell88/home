using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public class Presence : NetDaemonApp
{
    public override Task InitializeAsync()
    {
        Entity("group.family")
            .WhenStateChange(from: "home", to: "not_home")
            .AndNotChangeFor(new TimeSpan(0, 0, 10, 0))
            .UseEntity("input_boolean.left_home")
            .TurnOn();

        Entity("group.family")
            .WhenStateChange(from: "not_home", to: "home")
            .UseEntity("input_boolean.left_home")
            .TurnOff();

        Entity("input_boolean.left_home")
            .WhenStateChange(from: "off", to: "on")
            .Call(async (_,__,___) => await this.TurnEverythingOff());

        Entity("person.marissa")
            .WhenStateChange(from: "not_home", to: "home")
            .AndNotChangeFor(new TimeSpan(0, 5, 0))
            .Call(async (_, __, ___) => await this.Notify("Presence", "Marissa has arrived home",
                Notifier.TextNotificationDevice.Daniel));

        return base.InitializeAsync();
    }
}