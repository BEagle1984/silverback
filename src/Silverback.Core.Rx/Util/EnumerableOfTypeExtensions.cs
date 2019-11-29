// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reflection;

namespace Silverback.Util
{
    // TODO: Test
    internal static class ObservableOfTypeExtensions
    {
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static IObservable<object> OfType(this IObservable<object> source, Type type) =>
            typeof(Observable)
                    .GetMethod("OfType", BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] {source})
                as IObservable<object> ?? Observable.Empty<object>();
    }
}