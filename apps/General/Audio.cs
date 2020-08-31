using System;
using System.Reactive.Linq;
using daemonapp.Utilities;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Audio : NetDaemonRxApp
{
    public override void Initialize()
    {
        Entity("group.family")
            .StateChangesFiltered()
            .Synchronize()
            .Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .Subscribe(_ => this.SetTTSVolume());

        Entity("binary_sensor.bed_occupancy")
            .StateChangesFiltered()
            .Synchronize()
            .Subscribe(_ =>
            {
                LogHelper.Log(this,nameof(Audio), "Settings TTS volume due to bed occupancy");
                this.SetTTSVolume();
            });


        this.SetTTSVolume();

         base.Initialize();
    }
}