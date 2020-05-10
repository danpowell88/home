using System;
using JoySoftware.HomeAssistant.NetDaemon.Common;

public static class IFluentInputSelectExtensions
{
    public static IFluentExecuteAsync SetOption<T>(this IFluentInputSelect select, T theEnum) where T : Enum
    {
        return select.SetOption(theEnum.ToString("F"));
    }
}