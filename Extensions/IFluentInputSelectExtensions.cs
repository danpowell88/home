using System;
using NetDaemon.Common.Fluent;

public static class IFluentInputSelectExtensions
{
    public static IFluentExecuteAsync SetOption<T>(this IFluentInputSelect select, T theEnum) where T : Enum
    {
        return select.SetOption(theEnum.ToString("F"));
    }
}