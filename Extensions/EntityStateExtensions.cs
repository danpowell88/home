using System;
using System.Reactive.Linq;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

public static class EntityStateExtensions
{
    public static IObservable<(EntityState Old, EntityState New)> StateChangesFiltered(this RxEntity entity)
    {
        // not from unavailable to something
        // not from something to unavailable
        // not to and from the same status
        return entity.StateChanges.Where(s =>
            s.Old.State != null &&
            s.New.State != null && // may need to check for "unknown"
            s.Old.State != s.New.State);
    }

    public static IObservable<(EntityState Old, EntityState New)> StateAllChangesFiltered(this RxEntity entity)
    {
        // not from unavailable to something
        // not from something to unavailable
        // not to and from the same status
        return entity.StateAllChanges.Where(s =>
            s.Old.State != null &&
            s.New.State != null);
    }

    public static IObservable<TSource> FilterDistinctUntilChanged<TSource>(this IObservable<TSource> source,
        Func<TSource, bool> criteria)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.Where(criteria).DistinctUntilChanged(criteria);
    }
}