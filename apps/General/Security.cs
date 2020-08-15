using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using daemonapp.Utilities;
using EnumsNET;
using NetDaemon.Common.Reactive;

public class Security : NetDaemonRxApp
{
    public override void Initialize()
    {
        Entities(e =>
                new List<DeviceClass> {DeviceClass.Door, DeviceClass.Garage, DeviceClass.Window}.Select(c =>
                    c.AsString(EnumFormat.DisplayName, EnumFormat.Name)).ToList().Contains(e.Attribute!.device_class) &&
                !e.Attribute?.location == "internal")
            .StateChangesFiltered()
            .Where(s =>
                new List<string> {"closed", "off"}.Contains(s.Old!.State) &&
                new List<string> {"open", "on"}.Contains(s.New.State))
            .Subscribe(s =>
            {
                LogHelper.Log(this, nameof(Security), "Security check for open entry ways");

                this.ExecuteIfTrueNowAndAfter(() => State("input_boolean.left_home")!.State == true,
                    new TimeSpan(0, 2, 0), () =>
                        this.Notify(
                            "Security",
                            $"The {s.New.State == s.New!.Attribute!.friendly_name.ToLower()} has been opened",
                            Notifier.NotificationCriteria.Always,
                            Notifier.NotificationCriteria.Home,
                            Notifier.TextNotificationDevice.All));
            });

        base.Initialize();
    }
}