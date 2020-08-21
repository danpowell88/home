using System;
using NetDaemon.Common.Reactive;

public static class AppExtensions
{
    public static void SetOption<T>(this RxEntity entity, T theEnum) where T : Enum
    {
        entity.CallService("select_option", new {option = theEnum.ToString("F")});
    }

    public static void SetOption(this RxEntity entity, string option) 
    {
        entity.CallService("select_option", new { option = option });
    }
}