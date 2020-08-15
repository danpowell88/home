using System;
using System.Reactive.Linq;
using daemonapp.Utilities;
using NetDaemon.Common.Reactive;

public class Presence : NetDaemonRxApp
{
    public override void Initialize()
    {
        Entity("group.family")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "home" && s.New.State == "not_home")
            .NDSameStateFor(new TimeSpan(0, 0, 10, 0))
            .Subscribe(_ => Entity("input_boolean.left_home").TurnOn());

        Entity("group.family")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .Subscribe(_ => Entity("input_boolean.left_home").TurnOff());

        Entity("input_boolean.left_home")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "off" && s.New.State == "on")
            .Subscribe(_ =>
            {
                LogHelper.Log(this, nameof(Presence), "Everyone left home");
                this.TurnEverythingOff();
            });

        Entity("person.marissa")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .NDSameStateFor(new TimeSpan(0, 1, 0))
            .Subscribe(s =>
            {
                LogHelper.Log(this, nameof(Presence), "Marissa arrived home notification");

                var daniel = State("person.daniel")!;

                Action Notify() => () => this.Notify(
                    "Presence",
                    "Marissa has arrived home",
                    Notifier.NotificationCriteria.Always,
                    Notifier.NotificationCriteria.None,
                    Notifier.TextNotificationDevice.Daniel);

                // Been home for some time
                if (DateTime.Now - daniel.LastChanged >= new TimeSpan(0, 2, 0) && daniel.State == "home")
                {
                    Notify();
                }
                else
                {
                    this.ExecuteIfTrueNowAndAfter(() => daniel.State == "not_home", new TimeSpan(0, 2, 0), Notify());
                }

            });
            

        base.Initialize();
    }
}