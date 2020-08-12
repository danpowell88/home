﻿using System;
using System.Reactive.Linq;
using JetBrains.Annotations;
using NetDaemon.Common.Reactive;

[UsedImplicitly]
public class Audio : NetDaemonRxApp
{
    public override void Initialize()
    {
        Entity("group.family")
            .StateChangesFiltered()
            .Where(s => s.Old.State == "not_home" && s.New.State == "home")
            .Subscribe(_ => this.SetTTSVolume());

        Entity("binary_sensor.bed_occupancy")
            .StateChangesFiltered()
            .Subscribe(_ => this.SetTTSVolume());

         base.Initialize();
    }
}