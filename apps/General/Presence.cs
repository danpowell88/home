using System;
using System.Linq;
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
            .TurnOn()
            .Execute();

        Entity("group.family")
            .WhenStateChange(from: "not_home", to: "home")
            .UseEntity("input_boolean.left_home")
            .TurnOff()
            .Execute();

        Entity("input_boolean.left_home")
            .WhenStateChange(from: "off", to: "on")
            .Call(async (_,__,___) => await this.TurnEverythingOff())
            .Execute();

        Entity("person.marissa")
            .WhenStateChange(from: "not_home", to: "home")
            .AndNotChangeFor(new TimeSpan(0, 1, 0))
            .Call(async (_, __, ___) =>
            {

                var daniel = State.Single(e => e.EntityId == "person.daniel");

                async Task Notify() => await this.Notify(
                    "Presence", 
                    "Marissa has arrived home",
                    Notifier.NotificationCriteria.Always,
                    Notifier.NotificationCriteria.None,
                    Notifier.TextNotificationDevice.Daniel);

                // Been home for some time
                if (___.LastChanged - daniel.LastChanged >= new TimeSpan(0, 2, 0) && daniel.State == "home")
                {
                    await Notify();
                }
                else if (await this.TrueNowAndAfter(() => daniel.State == "not_home", new TimeSpan(0, 2, 0)))
                {
                    await Notify();
                }

            })
            .Execute();

        return base.InitializeAsync();
    }
}