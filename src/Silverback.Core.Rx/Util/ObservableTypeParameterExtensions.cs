// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;
using System.Reflection;

namespace Silverback.Util;

/// <summary>
///     Adds some extension methods to the <see cref="IObservable{T}" /> that gets the target type as
///     <see cref="Type" /> instead of generic type parameter.
/// </summary>
internal static class ObservableTypeParameterExtensions
{
    public static IObservable<object> OfType(this IObservable<object> source, Type type) =>
        typeof(Observable)
                .GetMethod("OfType", BindingFlags.Static | BindingFlags.Public)
                ?.MakeGenericMethod(type)
                .Invoke(null, new object[] { source })
            as IObservable<object> ?? Observable.Empty<object>();
}
